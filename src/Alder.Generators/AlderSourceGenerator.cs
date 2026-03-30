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

    // Each invocation receives ONE class declaration that has at least one [AlderRegistered].
    // ForAttributeWithMetadataName groups all attributes on the same class into one call.
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

        var registrations = ImmutableArray.CreateBuilder<TypeRegistrationModel>();
        var seenTypes = new HashSet<string>();

        foreach (var attr in ctx.Attributes)
        {
            if (attr.ConstructorArguments.Length != 1)
                continue;

            var typeArg = attr.ConstructorArguments[0];
            if (typeArg.Value is not INamedTypeSymbol registeredType)
                continue;

            var typeFullName = GetFullyQualifiedName(registeredType);
            if (!seenTypes.Add(typeFullName))
                continue;

            var registration = ExtractTypeRegistration(registeredType, typeFullName);
            registrations.Add(registration);
        }

        if (registrations.Count == 0)
            return null;

        return (contextNamespace, contextClassName, registrations.ToImmutable());
    }

    private static TypeRegistrationModel ExtractTypeRegistration(INamedTypeSymbol type, string typeFullName)
    {
        var metadataClassName = SanitizeIdentifier(typeFullName) + "Metadata";

        var isClosedGeneric = type is { IsGenericType: true, IsUnboundGenericType: false };

        var properties = ImmutableArray.CreateBuilder<PropertyModel>();
        var fields = ImmutableArray.CreateBuilder<FieldModel>();
        var constructors = ImmutableArray.CreateBuilder<ConstructorModel>();
        var indexers = ImmutableArray.CreateBuilder<IndexerModel>();
        var methods = ImmutableArray.CreateBuilder<MethodModel>();

        foreach (var member in type.GetMembers())
        {
            if (member.DeclaredAccessibility != Accessibility.Public)
                continue;

            switch (member)
            {
                case IPropertySymbol { IsIndexer: true } prop:
                    if (prop.Parameters.Length == 1)
                    {
                        indexers.Add(new IndexerModel(
                            GetFullyQualifiedTypeName(prop.Parameters[0].Type),
                            GetFullyQualifiedTypeName(prop.Type),
                            prop.GetMethod != null,
                            prop.SetMethod is { DeclaredAccessibility: Accessibility.Public }));
                    }
                    break;

                case IPropertySymbol prop:
                    var canRead = prop.GetMethod is { DeclaredAccessibility: Accessibility.Public };
                    var canWrite = prop.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };
                    properties.Add(new PropertyModel(
                        prop.Name,
                        GetFullyQualifiedTypeName(prop.Type),
                        canRead,
                        canWrite,
                        prop.IsStatic));
                    break;

                case IFieldSymbol field when !field.IsImplicitlyDeclared || type.IsTupleType:
                    fields.Add(new FieldModel(
                        field.Name,
                        GetFullyQualifiedTypeName(field.Type),
                        field.IsReadOnly || field.IsConst,
                        field.IsStatic));
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Constructor } method:
                    if (method.Parameters.Any(p =>
                        p.Type.TypeKind == TypeKind.Pointer ||
                        p.Type.TypeKind == TypeKind.FunctionPointer ||
                        p.Type.IsRefLikeType))
                        break;
                    var ctorParams = ImmutableArray.CreateBuilder<ParameterModel>();
                    foreach (var param in method.Parameters)
                    {
                        ctorParams.Add(new ParameterModel(
                            param.Name,
                            GetFullyQualifiedTypeName(param.Type)));
                    }
                    constructors.Add(new ConstructorModel(ctorParams.ToImmutable()));
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    if (method.IsGenericMethod)
                        break;
                    if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                        break;
                    if (method.Parameters.Any(p =>
                        p.RefKind != RefKind.None ||
                        p.IsParams ||
                        p.Type.TypeKind == TypeKind.Pointer ||
                        p.Type.TypeKind == TypeKind.FunctionPointer ||
                        p.Type.IsRefLikeType))
                        break;
                    var methodParams = ImmutableArray.CreateBuilder<ParameterModel>();
                    foreach (var param in method.Parameters)
                    {
                        methodParams.Add(new ParameterModel(
                            param.Name,
                            GetFullyQualifiedTypeName(param.Type)));
                    }
                    methods.Add(new MethodModel(
                        method.Name,
                        GetFullyQualifiedTypeName(method.ReturnType),
                        methodParams.ToImmutable(),
                        method.IsStatic,
                        method.ReturnsVoid));
                    break;
            }
        }

        return new TypeRegistrationModel(
            typeFullName,
            metadataClassName,
            isClosedGeneric,
            properties.ToImmutable(),
            fields.ToImmutable(),
            constructors.ToImmutable(),
            indexers.ToImmutable(),
            methods.ToImmutable());
    }

    private static void Emit(SourceProductionContext spc,
        ImmutableArray<(string ContextNamespace, string ContextClassName, ImmutableArray<TypeRegistrationModel> Registrations)> entries)
    {
        if (entries.IsDefaultOrEmpty)
            return;

        // Group by context class (namespace + name) in case ForAttributeWithMetadataName
        // delivers separate entries for the same class across partial declarations.
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

            var extensionSource = ExtensionMethodEmitter.Emit(allRegistrations);
            if (extensionSource != null)
                combined.AppendLine(extensionSource);

            var contextModel = new ContextModel(ns, className, allRegistrations);
            combined.AppendLine(ContextEmitter.Emit(contextModel, delegateSource != null, extensionSource != null));

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

    private static string GetFullyQualifiedTypeName(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named ? GetFullyQualifiedName(named) : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// FullyQualifiedFormat renders ValueTuple&lt;T1,T2&gt; as (T1, T2) tuple syntax,
    /// which is invalid in new-expressions and typeof. This helper detects ValueTuple
    /// and constructs the generic name manually.
    /// </summary>
    private static string GetFullyQualifiedName(INamedTypeSymbol type)
    {
        if (type is { IsTupleType: true, TupleUnderlyingType: { } underlying })
            type = underlying;

        if (type.IsGenericType && type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "System"
            && type.OriginalDefinition.Name.StartsWith("ValueTuple"))
        {
            var typeArgs = string.Join(", ", type.TypeArguments.Select(
                t => t is INamedTypeSymbol named ? GetFullyQualifiedName(named) : t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            return $"global::System.ValueTuple<{typeArgs}>";
        }

        if (type.IsGenericType && type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "System"
            && type.OriginalDefinition.Name == "Nullable" && type.TypeArguments.Length == 1)
        {
            var inner = type.TypeArguments[0] is INamedTypeSymbol named
                ? GetFullyQualifiedName(named)
                : type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return $"global::System.Nullable<{inner}>";
        }

        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    internal static string SanitizeIdentifier(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
            else
                sb.Append('_');
        }

        // Strip leading "global__" prefix for cleaner names
        var result = sb.ToString();
        if (result.StartsWith("global__"))
            result = result.Substring("global__".Length);

        return result;
    }
}
