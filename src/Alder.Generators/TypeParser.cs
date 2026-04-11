using System.Collections.Generic;
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
        var isClosedGeneric = type is { IsGenericType: true, IsUnboundGenericType: false };
        var isValueType = type.IsValueType;

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
                    if (HasUnsafeParameters(method))
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
                        break; // Generic methods handled separately by ExpandGenericMethods
                    if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                        break;
                    if (HasUnsafeParameters(method) || HasRefParameters(method))
                        break;
                    var methodParams = ImmutableArray.CreateBuilder<ParameterModel>();
                    foreach (var param in method.Parameters)
                    {
                        var isDelegate = IsDelegateType(param.Type);
                        methodParams.Add(new ParameterModel(
                            param.Name,
                            GetFullyQualifiedTypeName(param.Type),
                            param.IsParams,
                            isDelegate,
                            isDelegate ? ExtractDelegateSignature(param.Type) : null));
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
            isValueType,
            properties.ToImmutable(),
            fields.ToImmutable(),
            constructors.ToImmutable(),
            indexers.ToImmutable(),
            methods.ToImmutable());
    }

    /// <summary>
    /// Candidate result types used when expanding generic method shapes that require a materialized <c>TResult</c>.
    /// Value types need rooted instantiations for AOT dispatch, and <see cref="string"/> plus <see cref="object"/>
    /// cover the common reference-type cases without opening the door to unbounded expansion.
    /// </summary>
    private static readonly SpecialType[] ResultSpecialTypes =
    {
        SpecialType.System_Int32, SpecialType.System_Int64,
        SpecialType.System_Double, SpecialType.System_Single, SpecialType.System_Decimal,
        SpecialType.System_Boolean, SpecialType.System_String, SpecialType.System_Object,
    };

    /// <summary>
    /// Resolves the Roslyn symbols that back <see cref="ResultSpecialTypes"/>.
    /// Call once per compilation and reuse the result for expansion.
    /// </summary>
    public static ImmutableArray<INamedTypeSymbol> ResolveResultTypeSymbols(Compilation compilation)
    {
        var result = ImmutableArray.CreateBuilder<INamedTypeSymbol>(ResultSpecialTypes.Length + 1);
        foreach (var st in ResultSpecialTypes)
        {
            var symbol = compilation.GetSpecialType(st);
            if (symbol != null && symbol.SpecialType != SpecialType.None)
                result.Add(symbol);
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Expands generic methods into closed instantiations that the generated dispatch layer can address directly.
    /// Roslyn performs the constraint checks, so the generator only records combinations that the compiler accepts.
    /// </summary>
    public static TypeRegistrationModel ExpandGenericMethods(
        INamedTypeSymbol typeSymbol,
        TypeRegistrationModel registration,
        ImmutableArray<INamedTypeSymbol> elementTypeSymbols,
        ImmutableArray<INamedTypeSymbol> resultTypeSymbols,
        Compilation compilation)
    {
        var expanded = ImmutableArray.CreateBuilder<MethodModel>(registration.Methods.Length + 64);
        expanded.AddRange(registration.Methods);

        var ienumerableOpen = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)?.OriginalDefinition;

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary, IsGenericMethod: true } method)
                continue;
            if (method.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (method.IsExtensionMethod)
                continue;
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                continue;
            if (HasUnsafeParameters(method) || HasRefParameters(method))
                continue;
            if (HasExcludedParameterTypes(method))
                continue;

            // Dispatch generation only works when the closed type arguments can be inferred from the call shape.
            // Methods such as Cast<TResult>(IEnumerable) therefore stay on the reflection path.
            if (!HasTypeParameterInParameters(method))
                continue;

            // Expansion pools are chosen from the role each type parameter plays in the public signature.
            var pools = ClassifyTypeParameterPools(method, ienumerableOpen, elementTypeSymbols, resultTypeSymbols);
            ExpandCombinations(method, pools, 0, new ITypeSymbol[pools.Length], expanded);
        }

        return new TypeRegistrationModel(
            registration.TypeFullName,
            registration.MetadataClassName,
            registration.IsClosedGeneric,
            registration.IsValueType,
            registration.Properties,
            registration.Fields,
            registration.Constructors,
            registration.Indexers,
            expanded.ToImmutable());
    }

    /// <summary>
    /// Chooses an expansion pool for each type parameter based on how it appears in the method signature.
    /// Parameters that behave like enumerable element types expand against the element pool.
    /// All remaining parameters expand against the result pool.
    /// </summary>
    private static ImmutableArray<INamedTypeSymbol>[] ClassifyTypeParameterPools(
        IMethodSymbol method,
        INamedTypeSymbol? ienumerableOpen,
        ImmutableArray<INamedTypeSymbol> elementPool,
        ImmutableArray<INamedTypeSymbol> resultPool)
    {
        var isElement = new bool[method.TypeParameters.Length];

        foreach (var param in method.Parameters)
            MarkEnumerableTypeParams(param.Type, method.TypeParameters, ienumerableOpen, isElement);

        var pools = new ImmutableArray<INamedTypeSymbol>[method.TypeParameters.Length];
        for (var i = 0; i < pools.Length; i++)
            pools[i] = isElement[i] ? elementPool : resultPool;
        return pools;
    }

    /// <summary>
    /// Walks a type tree looking for <c>IEnumerable&lt;T&gt;</c> occurrences whose <c>T</c> is one of the method type parameters.
    /// </summary>
    private static void MarkEnumerableTypeParams(
        ITypeSymbol type,
        ImmutableArray<ITypeParameterSymbol> typeParams,
        INamedTypeSymbol? ienumerableOpen,
        bool[] isElement)
    {
        switch (type)
        {
            case INamedTypeSymbol { IsGenericType: true } named:
                if (ienumerableOpen != null &&
                    SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, ienumerableOpen) &&
                    named.TypeArguments[0] is ITypeParameterSymbol enumTp)
                {
                    MarkTypeParam(enumTp, typeParams, isElement);
                }
                foreach (var arg in named.TypeArguments)
                    MarkEnumerableTypeParams(arg, typeParams, ienumerableOpen, isElement);
                break;

            case IArrayTypeSymbol { ElementType: ITypeParameterSymbol arrTp }:
                // T[] implements IEnumerable<T>
                MarkTypeParam(arrTp, typeParams, isElement);
                break;

            case IArrayTypeSymbol arr:
                MarkEnumerableTypeParams(arr.ElementType, typeParams, ienumerableOpen, isElement);
                break;
        }
    }

    private static void MarkTypeParam(
        ITypeParameterSymbol tp,
        ImmutableArray<ITypeParameterSymbol> typeParams,
        bool[] isElement)
    {
        for (var i = 0; i < typeParams.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(typeParams[i], tp))
            {
                isElement[i] = true;
                return;
            }
        }
    }

    private static void ExpandCombinations(
        IMethodSymbol method,
        ImmutableArray<INamedTypeSymbol>[] pools,
        int depth,
        ITypeSymbol[] current,
        ImmutableArray<MethodModel>.Builder expanded)
    {
        if (depth == pools.Length)
        {
            if (TryConstructMethod(method, current, out var model))
                expanded.Add(model);
            return;
        }

        foreach (var type in pools[depth])
        {
            current[depth] = type;
            ExpandCombinations(method, pools, depth + 1, current, expanded);
        }
    }

    /// <summary>
    /// Attempts to construct a closed generic method with the given type arguments.
    /// Uses Roslyn's Construct() which validates type parameter constraints.
    /// Returns false if the constraints aren't satisfied.
    /// </summary>
    private static bool TryConstructMethod(
        IMethodSymbol openMethod,
        ITypeSymbol[] typeArgs,
        out MethodModel model)
    {
        model = default;

        for (var i = 0; i < openMethod.TypeParameters.Length; i++)
        {
            if (!SatisfiesConstraints(openMethod.TypeParameters[i], typeArgs[i]))
                return false;
        }

        var closedMethod = openMethod.Construct(typeArgs);

        var parameters = ImmutableArray.CreateBuilder<ParameterModel>();
        foreach (var param in closedMethod.Parameters)
        {
            var isDelegate = IsDelegateType(param.Type);
            parameters.Add(new ParameterModel(
                param.Name,
                GetFullyQualifiedTypeName(param.Type),
                param.IsParams,
                isDelegate,
                isDelegate ? ExtractDelegateSignature(param.Type) : null));
        }

        var genericTypeArgs = ImmutableArray.CreateBuilder<string>();
        foreach (var arg in typeArgs)
            genericTypeArgs.Add(GetFullyQualifiedTypeName(arg));

        model = new MethodModel(
            closedMethod.Name,
            GetFullyQualifiedTypeName(closedMethod.ReturnType),
            parameters.ToImmutable(),
            closedMethod.IsStatic,
            closedMethod.ReturnsVoid,
            genericTypeArgs.ToImmutable());
        return true;
    }

    /// <summary>
    /// Checks whether a concrete type satisfies all constraints of a type parameter.
    /// Uses Roslyn's actual type information, not string matching.
    /// </summary>
    private static bool SatisfiesConstraints(ITypeParameterSymbol typeParam, ITypeSymbol concreteType)
    {
        if (typeParam.HasValueTypeConstraint && !concreteType.IsValueType)
            return false;
        if (typeParam.HasReferenceTypeConstraint && concreteType.IsValueType)
            return false;
        if (typeParam.HasConstructorConstraint)
        {
            if (concreteType is INamedTypeSymbol named)
            {
                var hasCtor = named.InstanceConstructors.Any(c =>
                    c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
                if (!hasCtor && !concreteType.IsValueType)
                    return false;
            }
        }

        foreach (var constraint in typeParam.ConstraintTypes)
        {
            if (!ImplementsOrExtends(concreteType, constraint))
                return false;
        }
        return true;
    }

    private static bool ImplementsOrExtends(ITypeSymbol type, ITypeSymbol constraint)
    {
        if (SymbolEqualityComparer.Default.Equals(type, constraint))
            return true;

        // For generic constraints like INumber<T>, construct the closed form with the concrete type
        // (e.g., check if int implements INumber<int>)
        if (constraint is INamedTypeSymbol { IsGenericType: true } genericConstraint)
        {
            var constructedConstraint = TryConstructConstraint(genericConstraint, type);
            if (constructedConstraint != null)
            {
                foreach (var iface in type.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, genericConstraint.OriginalDefinition))
                        return true;
                }
                for (var current = type.BaseType; current != null; current = current.BaseType)
                {
                    if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, genericConstraint.OriginalDefinition))
                        return true;
                }
                return false;
            }
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, constraint))
                return true;
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, constraint))
                return true;
        }

        return false;
    }

    private static INamedTypeSymbol? TryConstructConstraint(INamedTypeSymbol genericConstraint, ITypeSymbol concreteType)
    {
        // For constraints like INumberBase<TOther> where TOther is the type param being checked,
        // check if the concrete type implements INumberBase<ConcreteType>
        var typeArgs = genericConstraint.TypeArguments;
        var newArgs = new ITypeSymbol[typeArgs.Length];
        for (var i = 0; i < typeArgs.Length; i++)
        {
            newArgs[i] = typeArgs[i] is ITypeParameterSymbol ? concreteType : typeArgs[i];
        }
        return genericConstraint.OriginalDefinition.Construct(newArgs);
    }

    private static bool HasTypeParameterInParameters(IMethodSymbol method)
    {
        foreach (var param in method.Parameters)
        {
            if (ReferencesAnyTypeParameter(param.Type, method.TypeParameters))
                return true;
        }
        return false;
    }

    private static bool ReferencesAnyTypeParameter(ITypeSymbol type, ImmutableArray<ITypeParameterSymbol> typeParams)
    {
        if (type is ITypeParameterSymbol tp)
            return typeParams.Any(p => SymbolEqualityComparer.Default.Equals(p, tp));
        if (type is IArrayTypeSymbol arr)
            return ReferencesAnyTypeParameter(arr.ElementType, typeParams);
        if (type is INamedTypeSymbol named && named.IsGenericType)
            return named.TypeArguments.Any(a => ReferencesAnyTypeParameter(a, typeParams));
        return false;
    }

    private static bool HasUnsafeParameters(IMethodSymbol method) =>
        method.Parameters.Any(p =>
            p.Type.TypeKind == TypeKind.Pointer ||
            p.Type.TypeKind == TypeKind.FunctionPointer ||
            p.Type.IsRefLikeType);

    private static bool HasRefParameters(IMethodSymbol method) =>
        method.Parameters.Any(p => p.RefKind != RefKind.None);

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

    internal static DelegateSignature? ExtractDelegateSignature(ITypeSymbol type)
    {
        if (!IsDelegateType(type) || type is not INamedTypeSymbol { IsGenericType: true, DelegateInvokeMethod: { } invoke })
            return null;

        var def = ((INamedTypeSymbol)type).OriginalDefinition.ToDisplayString();
        var isAction = def.StartsWith("System.Action");
        var isFunc = def.StartsWith("System.Func");
        if (!isAction && !isFunc)
            return null;

        var paramTypes = ImmutableArray.CreateBuilder<string>();
        foreach (var p in invoke.Parameters)
            paramTypes.Add(GetFullyQualifiedTypeName(p.Type));

        var returnType = isAction ? "void" : GetFullyQualifiedTypeName(invoke.ReturnType);
        return new DelegateSignature(paramTypes.ToImmutable(), returnType, isAction);
    }

    private static bool HasExcludedParameterTypes(IMethodSymbol method)
    {
        foreach (var param in method.Parameters)
        {
            if (param.Type is not INamedTypeSymbol named)
                continue;
            var def = named.OriginalDefinition;
            var ns = def.ContainingNamespace?.ToDisplayString() ?? "";
            if (ns == "System.Collections.Generic" && (def.Name == "IComparer" || def.Name == "IEqualityComparer"))
                return true;
            if (ns == "System.Linq.Expressions" && def.Name == "Expression")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Discovers extension methods on a type (e.g., Enumerable) from the Roslyn compilation.
    /// Classifies each method by signature pattern for dispatch code generation.
    /// </summary>
    internal static ImmutableArray<ExtensionMethodModel> DiscoverExtensionMethods(INamedTypeSymbol extensionType)
    {
        var result = ImmutableArray.CreateBuilder<ExtensionMethodModel>();
        var seen = new HashSet<string>();

        foreach (var member in extensionType.GetMembers())
        {
            if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary, IsExtensionMethod: true, IsStatic: true } method)
                continue;
            if (method.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                continue;
            if (HasRefParameters(method))
                continue;

            // Only handle methods where the first (this) param is exactly IEnumerable<T> or T[].
            // Methods taking IOrderedEnumerable<T> (ThenBy) or other derived interfaces
            // need specific source types and fall through to reflection.
            var thisParam = method.Parameters[0];
            if (!IsDirectEnumerableParam(thisParam.Type))
                continue;

            if (HasExcludedParameterTypes(method))
                continue;

            var extraParams = method.Parameters.Skip(1).ToArray();
            var kind = ClassifyExtensionMethod(extraParams);

            // Unique key: methodName + param signature to distinguish overloads.
            // Select(Func<T,R>) vs Select(Func<T,int,R>) both have 1 extra param
            // but different delegate arities, so both must be discovered.
            var key = $"{method.Name}/{string.Join(",", extraParams.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))}";
            if (!seen.Add(key))
                continue;

            var paramModels = ImmutableArray.CreateBuilder<ExtensionParamModel>();
            foreach (var p in extraParams)
            {
                var isDelegate = IsDelegateType(p.Type);
                var delegateInfo = isDelegate ? ExtractDelegateSignature(p.Type) : null;
                paramModels.Add(new ExtensionParamModel(
                    GetFullyQualifiedTypeName(p.Type),
                    isDelegate,
                    p.Type is ITypeParameterSymbol,
                    delegateInfo));
            }

            result.Add(new ExtensionMethodModel(method.Name, kind, paramModels.ToImmutable()));
        }

        return result.ToImmutable();
    }

    private static ExtensionMethodKind ClassifyExtensionMethod(IParameterSymbol[] extraParams)
    {
        if (extraParams.Length == 0)
            return ExtensionMethodKind.NoArg;

        if (extraParams.Length == 1)
        {
            var paramType = extraParams[0].Type;
            if (IsDelegateType(paramType))
            {
                // Delegates with complex return types (e.g., IEnumerable<TResult>)
                // need explicit type args and can't be dispatched with simple type checks
                if (paramType is INamedTypeSymbol { DelegateInvokeMethod: { } invoke } &&
                    invoke.ReturnType is INamedTypeSymbol { IsGenericType: true })
                    return ExtensionMethodKind.Complex;
                return ExtensionMethodKind.SingleDelegate;
            }
            if (IsDirectEnumerableParam(paramType))
                return ExtensionMethodKind.SingleEnumerable;
            return ExtensionMethodKind.SingleValue;
        }

        return ExtensionMethodKind.Complex;
    }

    private static bool IsDirectEnumerableParam(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            if (named.OriginalDefinition.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T)
                return false;
            var elementType = named.TypeArguments[0];
            // Accept type parameters (generic methods like Where<T>) and concrete value types
            // (non-generic numeric aggregates like Sum(IEnumerable<int>)). Reject constructed
            // generics like Nullable<int> or KeyValuePair<K,V> because dispatch handles
            // non-nullable value types only.
            return elementType is ITypeParameterSymbol
                || (elementType.IsValueType && elementType is not INamedTypeSymbol { IsGenericType: true });
        }
        return type is IArrayTypeSymbol;
    }

    internal static string GetFullyQualifiedTypeName(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named ? GetFullyQualifiedName(named) : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
