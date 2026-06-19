using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Alder.Generators.Model;
using Microsoft.CodeAnalysis;

namespace Alder.Generators;

/// <summary>
/// Extracts Roslyn symbol information into the generator model types used by Alder's AOT pipeline.
/// Keeping this logic isolated from the source-generation entry point makes the transformation testable on its own.
/// </summary>
internal static class TypeParser
{
    public static TypeRegistrationModel ExtractTypeRegistration(INamedTypeSymbol type)
    {
        var typeFullName = GetFullyQualifiedName(type);
        return ExtractTypeRegistration(type, typeFullName);
    }

    public static TypeRegistrationModel ExtractTypeRegistration(INamedTypeSymbol type, string typeFullName)
    {
        var metadataClassName = SanitizeIdentifier(typeFullName) + "Metadata";
        var isValueType = type.IsValueType;
        var originalDefinitionNamespace = type.IsGenericType ? type.OriginalDefinition.ContainingNamespace?.ToDisplayString() : null;
        var originalDefinitionMetadataName = type.IsGenericType ? type.OriginalDefinition.MetadataName : null;
        var typeArguments = type.IsGenericType
            ? type.TypeArguments.Select(CreateTypeArgumentModel).ToImmutableArray()
            : ImmutableArray<TypeArgumentModel>.Empty;

        if (HasTrimOrAotUnsafeAttribute(type))
        {
            return new TypeRegistrationModel(
                typeFullName,
                originalDefinitionNamespace,
                originalDefinitionMetadataName,
                typeArguments,
                metadataClassName,
                isValueType,
                ImmutableArray<PropertyModel>.Empty,
                ImmutableArray<FieldModel>.Empty,
                ImmutableArray<ConstructorModel>.Empty,
                ImmutableArray<IndexerModel>.Empty,
                ImmutableArray<MethodModel>.Empty);
        }

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
                {
                    if (prop.Parameters.Length == 1 &&
                        !IsUnsupportedDispatchType(prop.Parameters[0].Type) &&
                        !IsUnsupportedDispatchType(prop.Type))
                    {
                        var propertyUnsafe = HasTrimOrAotUnsafeAttribute(prop);
                        var canRead = !propertyUnsafe &&
                                      prop.GetMethod is { DeclaredAccessibility: Accessibility.Public } getMethod &&
                                      !HasTrimOrAotUnsafeAttribute(getMethod);
                        var canWrite = !propertyUnsafe &&
                                       prop.SetMethod is { DeclaredAccessibility: Accessibility.Public } setMethod &&
                                       !HasTrimOrAotUnsafeAttribute(setMethod);
                        if (!canRead && !canWrite)
                            break;

                        indexers.Add(new IndexerModel(
                            GetFullyQualifiedTypeName(prop.Parameters[0].Type),
                            GetFullyQualifiedTypeName(prop.Type),
                            canRead,
                            canWrite));
                    }
                    break;
                }

                case IPropertySymbol prop:
                {
                    var propertyUnsafe = HasTrimOrAotUnsafeAttribute(prop) ||
                                         IsUnsupportedDispatchType(prop.Type);
                    var canRead = !propertyUnsafe &&
                                  prop.GetMethod is { DeclaredAccessibility: Accessibility.Public } getMethod &&
                                  !HasTrimOrAotUnsafeAttribute(getMethod);
                    var canWrite = !propertyUnsafe &&
                                   prop.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false } setMethod &&
                                   !HasTrimOrAotUnsafeAttribute(setMethod);
                    if (!canRead && !canWrite)
                        break;

                    properties.Add(new PropertyModel(
                        prop.Name,
                        GetFullyQualifiedTypeName(prop.Type),
                        canRead,
                        canWrite,
                        prop.IsStatic));
                    break;
                }

                case IFieldSymbol field when (!field.IsImplicitlyDeclared || type.IsTupleType) &&
                                             !IsUnsupportedDispatchType(field.Type):
                    fields.Add(new FieldModel(
                        field.Name,
                        GetFullyQualifiedTypeName(field.Type),
                        field.IsReadOnly || field.IsConst,
                        field.IsStatic));
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Constructor } method:
                    if (HasTrimOrAotUnsafeAttribute(method))
                        break;
                    if (HasUnsafeParameters(method))
                        break;
                    var ctorParams = ImmutableArray.CreateBuilder<ParameterModel>();
                    foreach (var param in method.Parameters)
                    {
                        ctorParams.Add(new ParameterModel(
                            param.Name,
                            GetFullyQualifiedTypeName(param.Type),
                            GetFullyQualifiedParameterTypeName(param)));
                    }
                    constructors.Add(new ConstructorModel(ctorParams.ToImmutable()));
                    break;

                case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                    if (HasTrimOrAotUnsafeAttribute(method))
                        break;
                    if (method.IsGenericMethod)
                        break;
                    if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                        break;
                    if (!method.ReturnsVoid && IsUnsupportedDispatchType(method.ReturnType))
                        break;
                    if (HasUnsafeParameters(method) || HasUnsupportedByRefParameters(method) || HasDelegateParameters(method))
                        break;
                    var methodParams = ImmutableArray.CreateBuilder<ParameterModel>();
                    foreach (var param in method.Parameters)
                    {
                        methodParams.Add(new ParameterModel(
                            param.Name,
                            GetFullyQualifiedTypeName(param.Type),
                            GetFullyQualifiedParameterTypeName(param),
                            param.RefKind == RefKind.Out,
                            param.IsParams));
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
            originalDefinitionNamespace,
            originalDefinitionMetadataName,
            typeArguments,
            metadataClassName,
            isValueType,
            properties.ToImmutable(),
            fields.ToImmutable(),
            constructors.ToImmutable(),
            indexers.ToImmutable(),
            methods.ToImmutable());
    }

    private static TypeArgumentModel CreateTypeArgumentModel(ITypeSymbol type)
    {
        var isNullableValueType = type is INamedTypeSymbol named &&
                                  named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        return new TypeArgumentModel(
            GetFullyQualifiedTypeName(type),
            type.IsValueType,
            !type.IsValueType || isNullableValueType);
    }

    private static bool HasDelegateParameters(IMethodSymbol method) =>
        method.Parameters.Any(p => IsDelegateType(p.Type));

    private static bool HasUnsafeParameters(IMethodSymbol method) =>
        method.Parameters.Any(p => IsUnsupportedDispatchType(p.Type));

    private static bool IsUnsupportedDispatchType(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer ||
        type.IsRefLikeType;

    private static bool HasUnsupportedByRefParameters(IMethodSymbol method) =>
        method.Parameters.Any(p => p.RefKind is RefKind.Ref or RefKind.In);

    private static bool HasTrimOrAotUnsafeAttribute(ISymbol? symbol)
    {
        if (symbol == null)
            return false;

        foreach (var attribute in symbol.GetAttributes())
        {
            var fullName = attribute.AttributeClass?.ToDisplayString();
            if (fullName is
                "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute" or
                "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute" or
                "System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDelegateType(ITypeSymbol type)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (current.SpecialType == SpecialType.System_Delegate ||
                current.ToDisplayString() == "System.MulticastDelegate")
                return true;
        }
        return false;
    }

    internal static string GetFullyQualifiedTypeName(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named ? GetFullyQualifiedName(named) : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    internal static string GetFullyQualifiedParameterTypeName(IParameterSymbol parameter)
    {
        var typeName = GetFullyQualifiedTypeName(parameter.Type);
        if (!parameter.Type.IsValueType &&
            (parameter.RefKind == RefKind.Out || ParameterCanBeNull(parameter)) &&
            !typeName.EndsWith("?", StringComparison.Ordinal))
        {
            return typeName + "?";
        }

        return typeName;
    }

    private static bool ParameterCanBeNull(IParameterSymbol parameter)
    {
        if (parameter.NullableAnnotation == NullableAnnotation.Annotated ||
            parameter.Type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        foreach (var attribute in parameter.GetAttributes())
        {
            var fullName = attribute.AttributeClass?.ToDisplayString();
            if (fullName is "System.Diagnostics.CodeAnalysis.MaybeNullAttribute" or
                "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute" or
                "System.Diagnostics.CodeAnalysis.AllowNullAttribute")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// FullyQualifiedFormat renders ValueTuple&lt;T1,T2&gt; as (T1, T2) tuple syntax,
    /// which is invalid in new-expressions and typeof. Detects ValueTuple
    /// and constructs the generic name manually.
    /// </summary>
    internal static string GetFullyQualifiedName(INamedTypeSymbol type)
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

        var result = sb.ToString();
        if (result.StartsWith("global__"))
            result = result.Substring("global__".Length);

        return result;
    }
}
