using System.Collections.Generic;
using System.Collections.Immutable;
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

        // ForAttributeWithMetadataName has already filtered to AlderRegisteredAttribute.
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

        var registrations = ImmutableArray.CreateBuilder<TypeRegistrationModel>();

        foreach (var (symbol, fullName) in typeEntries)
        {
            registrations.Add(TypeParser.ExtractTypeRegistration(symbol, fullName));
        }

        return new ContextRegistrations(contextNamespace, contextClass.Name, registrations.ToImmutable());
    }

    private static void Emit(SourceProductionContext spc, ImmutableArray<ContextRegistrations> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        var byContext = new Dictionary<string, (string Namespace, string ClassName, List<TypeRegistrationModel> Registrations)>();

        foreach (var entry in entries)
        {
            var key = entry.Namespace + "." + entry.ClassName;
            if (!byContext.TryGetValue(key, out var existing))
            {
                existing = (entry.Namespace, entry.ClassName, new List<TypeRegistrationModel>());
                byContext[key] = existing;
            }
            existing.Registrations.AddRange(entry.Registrations);
        }

        foreach (var kvp in byContext)
        {
            var (ns, className, registrationList) = kvp.Value;
            var allRegistrations = registrationList.ToImmutableArray();

            var combined = new StringBuilder();
            combined.AppendLine("// <auto-generated/>");
            combined.AppendLine("#nullable enable");
            // Generated dispatch intentionally emits exhaustive switch tables and reflection bridges
            // that trigger analyzer/compiler noise not actionable for consumers of emitted code.
            combined.AppendLine("#pragma warning disable CS0162, CS0618, CS8620, SYSLIB0051");
            combined.AppendLine();
            foreach (var registration in allRegistrations)
                combined.AppendLine(TypeMetadataEmitter.Emit(registration));

            var contextModel = new ContextModel(ns, className, allRegistrations);
            combined.AppendLine(ContextEmitter.Emit(contextModel));
            combined.AppendLine();
            combined.AppendLine("#pragma warning restore CS0162, CS0618, CS8620, SYSLIB0051");

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
