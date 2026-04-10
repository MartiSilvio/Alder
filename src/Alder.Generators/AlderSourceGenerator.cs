using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Alder.Generators.Emitters;
using Alder.Generators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alder.Generators;

[Generator]
public sealed class AlderSourceGenerator : IIncrementalGenerator
{
    private const string BaseContextFullName = "Alder.Aot.AlderTypeContext";
    private const string BuiltInContextFullName = "Alder.Aot.AlderBuiltInContext";
    private const string RegisteredAttributeFullName = "Alder.Aot.AlderRegisteredAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var userContexts = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                RegisteredAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractFromAttributeContext(ctx))
            .Where(static r => r.HasValue)
            .Select(static (r, _) => r!.Value);

        var builtInContext = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null } classDecl
                    && classDecl.Identifier.Text == "AlderBuiltInContext",
                transform: static (ctx, _) => ExtractBuiltInContext(ctx))
            .Where(static r => r.HasValue)
            .Select(static (r, _) => r!.Value);

        var all = userContexts.Collect().Combine(builtInContext.Collect());

        context.RegisterSourceOutput(all, static (spc, combined) =>
        {
            var (user, builtIn) = combined;
            var merged = user.AddRange(builtIn);
            Emit(spc, merged);
        });
    }

    private static ContextRegistrations? ExtractFromAttributeContext(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol contextClass)
            return null;
        if (!DerivesFrom(contextClass, BaseContextFullName))
            return null;
        if (contextClass.ToDisplayString() == BuiltInContextFullName)
            return null;

        var typeEntries = new List<(INamedTypeSymbol Symbol, string FullName)>();
        var seenTypes = new HashSet<string>();

        // ctx.Attributes is pre-filtered to AlderRegisteredAttribute by ForAttributeWithMetadataName
        CollectRegisteredTypes(ctx.Attributes, typeEntries, seenTypes);

        return BuildRegistrations(contextClass, typeEntries, ctx.SemanticModel.Compilation);
    }

    private static ContextRegistrations? ExtractBuiltInContext(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not ClassDeclarationSyntax)
            return null;

        var contextClass = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
        if (contextClass == null)
            return null;

        var builtInSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName(BuiltInContextFullName);
        if (builtInSymbol == null || !SymbolEqualityComparer.Default.Equals(contextClass, builtInSymbol))
            return null;

        var compilation = ctx.SemanticModel.Compilation;
        var typeEntries = new List<(INamedTypeSymbol Symbol, string FullName)>();
        var seenTypes = new HashSet<string>();

        foreach (var symbol in BuiltInTypeCatalog.Resolve(compilation))
        {
            var fullName = TypeParser.GetFullyQualifiedName(symbol);
            if (seenTypes.Add(fullName))
                typeEntries.Add((symbol, fullName));
        }

        CollectRegisteredTypes(contextClass.GetAttributes(), typeEntries, seenTypes);

        return BuildRegistrations(contextClass, typeEntries, compilation);
    }

    private static void CollectRegisteredTypes(
        IEnumerable<AttributeData> attributes,
        List<(INamedTypeSymbol Symbol, string FullName)> typeEntries,
        HashSet<string> seenTypes)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.ToDisplayString() != RegisteredAttributeFullName)
                continue;
            if (attr.ConstructorArguments.Length != 1)
                continue;
            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol registeredType)
                continue;

            var typeFullName = TypeParser.GetFullyQualifiedName(registeredType);
            if (!seenTypes.Add(typeFullName))
                continue;

            typeEntries.Add((registeredType, typeFullName));
        }
    }

    private static ContextRegistrations? BuildRegistrations(
        INamedTypeSymbol contextClass,
        List<(INamedTypeSymbol Symbol, string FullName)> typeEntries,
        Compilation compilation)
    {
        if (typeEntries.Count == 0)
            return null;

        var contextNamespace = contextClass.ContainingNamespace.IsGlobalNamespace
            ? ""
            : contextClass.ContainingNamespace.ToDisplayString();

        var elementTypes = CollectElementTypeSymbols(typeEntries, compilation);
        var resultTypes = TypeParser.ResolveResultTypeSymbols(compilation);

        var registrations = ImmutableArray.CreateBuilder<TypeRegistrationModel>();
        var extensionMethods = ImmutableArray<ExtensionMethodModel>.Empty;

        foreach (var (symbol, fullName) in typeEntries)
        {
            var reg = TypeParser.ExtractTypeRegistration(symbol, fullName);
            reg = TypeParser.ExpandGenericMethods(symbol, reg, elementTypes, resultTypes, compilation);
            registrations.Add(reg);

            if (symbol.IsStatic && symbol.MightContainExtensionMethods)
                extensionMethods = extensionMethods.AddRange(TypeParser.DiscoverExtensionMethods(symbol));
        }

        return new ContextRegistrations(contextNamespace, contextClass.Name, registrations.ToImmutable(), extensionMethods);
    }

    private static ImmutableArray<INamedTypeSymbol> CollectElementTypeSymbols(
        List<(INamedTypeSymbol Symbol, string FullName)> typeEntries,
        Compilation compilation)
    {
        var result = new List<INamedTypeSymbol>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var (symbol, _) in typeEntries)
        {
            if (symbol.IsValueType && !symbol.IsGenericType && seen.Add(symbol))
                result.Add(symbol);
        }

        // Reference types (string, object) are excluded. MakeGenericMethod handles them
        // at runtime via shared generics. Object-rooting for canonical forms (__Canon)
        // is handled separately by TypeRoots, not by method expansion.

        return result.ToImmutableArray();
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<ContextRegistrations> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        var byContext = new Dictionary<string, (string Namespace, string ClassName, List<TypeRegistrationModel> Registrations, List<ExtensionMethodModel> ExtensionMethods)>();

        foreach (var entry in entries)
        {
            var key = entry.Namespace + "." + entry.ClassName;
            if (!byContext.TryGetValue(key, out var existing))
            {
                existing = (entry.Namespace, entry.ClassName, new List<TypeRegistrationModel>(), new List<ExtensionMethodModel>());
                byContext[key] = existing;
            }
            existing.Registrations.AddRange(entry.Registrations);
            if (!entry.ExtensionMethods.IsDefaultOrEmpty)
                existing.ExtensionMethods.AddRange(entry.ExtensionMethods);
        }

        foreach (var kvp in byContext)
        {
            var (ns, className, registrationList, extensionMethodList) = kvp.Value;
            var allRegistrations = registrationList.ToImmutableArray();
            var allExtensionMethods = extensionMethodList.ToImmutableArray();

            var combined = new StringBuilder();
            foreach (var registration in allRegistrations)
                combined.AppendLine(TypeMetadataEmitter.Emit(registration));

            var typeRoots = TypeMetadataEmitter.EmitTypeRoots(allRegistrations);
            if (typeRoots != null)
                combined.AppendLine(typeRoots);

            var (extensionSource, extensionDelegates) = ExtensionDispatchEmitter.Emit(allRegistrations, allExtensionMethods);
            var hasExtensionDispatch = extensionSource != null;
            if (hasExtensionDispatch)
                combined.AppendLine(extensionSource);

            var delegateSource = DelegateFactoryEmitter.Emit(allRegistrations, extensionDelegates);
            if (delegateSource != null)
                combined.AppendLine(delegateSource);

            var contextModel = new ContextModel(ns, className, allRegistrations);
            combined.AppendLine(ContextEmitter.Emit(contextModel, delegateSource != null, hasExtensionDispatch));

            spc.AddSource(className + ".g.cs", combined.ToString());
        }
    }

    private static bool DerivesFrom(INamedTypeSymbol type, string baseFullName)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == baseFullName)
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
