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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var registrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Alder.Aot.AlderRegisteredAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ExtractRegistrations(ctx))
            .Where(static r => r.HasValue)
            .Select(static (r, _) => r!.Value);

        var collected = registrations.Collect();

        context.RegisterSourceOutput(collected, static (spc, entries) => Emit(spc, entries));
    }

    private static (string ContextNamespace, string ContextClassName, ImmutableArray<TypeRegistrationModel> Registrations)?
        ExtractRegistrations(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol contextClass)
            return null;

        if (!DerivesFrom(contextClass, BaseContextFullName))
            return null;

        var contextNamespace = contextClass.ContainingNamespace.IsGlobalNamespace
            ? ""
            : contextClass.ContainingNamespace.ToDisplayString();
        var contextClassName = contextClass.Name;

        // First pass: collect all registered type symbols
        var typeEntries = new List<(INamedTypeSymbol Symbol, string FullName)>();
        var seenTypes = new HashSet<string>();

        foreach (var attr in ctx.Attributes)
        {
            if (attr.ConstructorArguments.Length != 1)
                continue;
            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol registeredType)
                continue;

            var typeFullName = TypeParser.GetFullyQualifiedName(registeredType);
            if (!seenTypes.Add(typeFullName))
                continue;

            typeEntries.Add((registeredType, typeFullName));
        }

        if (typeEntries.Count == 0)
            return null;

        // Collect element types for generic expansion (value types + string + object)
        var compilation = ctx.SemanticModel.Compilation;
        var elementTypes = CollectElementTypeSymbols(typeEntries, compilation);
        var resultTypes = TypeParser.ResolveResultTypeSymbols(compilation);

        // Second pass: extract registrations and expand generic methods
        var registrations = ImmutableArray.CreateBuilder<TypeRegistrationModel>();
        foreach (var (symbol, fullName) in typeEntries)
        {
            var reg = TypeParser.ExtractTypeRegistration(symbol, fullName);
            reg = TypeParser.ExpandGenericMethods(symbol, reg, elementTypes, resultTypes);
            registrations.Add(reg);
        }

        return (contextNamespace, contextClassName, registrations.ToImmutable());
    }

    private static ImmutableArray<INamedTypeSymbol> CollectElementTypeSymbols(
        List<(INamedTypeSymbol Symbol, string FullName)> typeEntries,
        Compilation compilation)
    {
        var result = new List<INamedTypeSymbol>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        // Value types from registrations
        foreach (var (symbol, _) in typeEntries)
        {
            if (symbol.IsValueType && !symbol.IsGenericType && seen.Add(symbol))
                result.Add(symbol);
        }

        // Always include string and object
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        if (stringType != null && seen.Add(stringType)) result.Add(stringType);
        if (objectType != null && seen.Add(objectType)) result.Add(objectType);

        return result.ToImmutableArray();
    }

    private static void Emit(SourceProductionContext spc,
        ImmutableArray<(string ContextNamespace, string ContextClassName, ImmutableArray<TypeRegistrationModel> Registrations)> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        var byContext = new Dictionary<string, (string Namespace, string ClassName, List<TypeRegistrationModel> Registrations)>();

        foreach (var entry in entries)
        {
            var key = entry.ContextNamespace + "." + entry.ContextClassName;
            if (!byContext.TryGetValue(key, out var existing))
            {
                existing = (entry.ContextNamespace, entry.ContextClassName, new List<TypeRegistrationModel>());
                byContext[key] = existing;
            }
            existing.Registrations.AddRange(entry.Registrations);
        }

        foreach (var kvp in byContext)
        {
            var (ns, className, registrationList) = kvp.Value;
            var allRegistrations = registrationList.ToImmutableArray();

            var combined = new StringBuilder();
            foreach (var registration in allRegistrations)
                combined.AppendLine(TypeMetadataEmitter.Emit(registration));

            var typeRoots = TypeMetadataEmitter.EmitTypeRoots(allRegistrations);
            if (typeRoots != null)
                combined.AppendLine(typeRoots);

            var delegateSource = DelegateFactoryEmitter.Emit(allRegistrations);
            if (delegateSource != null)
                combined.AppendLine(delegateSource);

            var contextModel = new ContextModel(ns, className, allRegistrations);
            combined.AppendLine(ContextEmitter.Emit(contextModel, delegateSource != null));

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
