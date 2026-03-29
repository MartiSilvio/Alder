using System.Collections.Immutable;
using System.Reflection;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Text;

namespace Alder.Binding;

internal sealed partial class Binder
{
    private static BoundLiteralExpr BindTypeReference(TypeReferenceExpr typeReference, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeReference.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
    }

    private static BoundLiteralExpr BindTypeof(TypeofExpr typeofExpr, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeofExpr.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
    }

    private static BoundLiteralExpr BindDefault(DefaultExpr defaultExpr, BindingContext context)
    {
        if (defaultExpr.TypeToken == null)
            return new BoundLiteralExpr(null, BoundType.Unknown);

        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(defaultExpr.TypeToken.Value.Lexeme);
        var value = TypeHelpers.GetDefaultValue(resolvedType);
        return new BoundLiteralExpr(value, new BoundType(resolvedType));
    }

    private static BoundExpr BindIdentifier(IdentifierExpr identifier, BindingContext context)
    {
        var name = identifier.Name.Lexeme;

        if (context.RuntimeContext.Functions.ContainsKey(name) ||
            context.RuntimeContext.Modules.ContainsKey(name))
        {
            return new BoundIdentifierExpr(name, BoundType.Unknown);
        }

        if (context.TryGetLocal(name, out var localType, out var localId))
            return new BoundIdentifierExpr(name, localType, localId);

        context.TryGetVariableType(name, out var staticType);

        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
        return new BoundIdentifierExpr(name, staticType);
    }

    private BoundExpr BindCollectionExpr(CollectionExpr collectionExpr, BindingContext context, Type? targetType = null)
    {
        var elements = collectionExpr.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();

        if (targetType != null)
            return CreateTargetTypedCollection(elements, targetType);

        if (context.LanguageMode == LanguageMode.Standard)
            throw new AlderException(DiagnosticDescriptors.NoTargetTypeForCollectionExpression, collectionExpr.Span, null, null);

        return CreateInferredArrayCollection(elements, collectionExpr.Span);
    }

    private BoundExpr BindImplicitArrayCreation(ImplicitArrayCreationExpr implicitArray, BindingContext context)
    {
        var elements = implicitArray.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();

        var elementType = FindBestCommonType(elements)
            ?? throw new AlderException(DiagnosticDescriptors.NoBestTypeForImplicitArray, implicitArray.Span, null, null);

        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundCollectionCreationExpr(elements, elementType, CollectionKind.Array, null, new BoundType(arrayType));
    }

    private static BoundCollectionCreationExpr CreateTargetTypedCollection(ImmutableArray<BoundExpr> elements, Type targetType)
    {
        var elementType = GetCollectionElementType(targetType)!;
        var kind = targetType.IsArray ? CollectionKind.Array : CollectionKind.TargetTypedCollection;
        return new BoundCollectionCreationExpr(elements, elementType, kind, targetType, new BoundType(targetType));
    }

    private BoundCollectionCreationExpr CreateInferredArrayCollection(ImmutableArray<BoundExpr> elements, TextSpan span)
    {
        var elementType = FindBestCommonType(elements) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        var elementMemberTypes = InferCommonElementMemberTypes(elements);
        var staticType = elementMemberTypes != null
            ? (BoundType)new BoundStructuralType(arrayType, elementMemberTypes)
            : new BoundType(arrayType);
        return new BoundCollectionCreationExpr(elements, elementType, CollectionKind.InferredArray, null, staticType);
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static ImmutableDictionary<string, Type>? InferCommonElementMemberTypes(ImmutableArray<BoundExpr> elements)
    {
        if (elements.Length == 0)
            return null;

        var first = elements[0].StaticType.MemberTypes;
        if (first == null)
            return null;

        for (var i = 1; i < elements.Length; i++)
        {
            var other = elements[i].StaticType.MemberTypes;
            if (other == null || other.Count != first.Count)
                return null;

            foreach (var kvp in first)
            {
                if (!other.TryGetValue(kvp.Key, out var otherType) || otherType != kvp.Value)
                    return null;
            }
        }

        return first;
    }

    private BoundSpreadExpr BindSpread(SpreadExpr spread, BindingContext context)
    {
        var expression = Bind(spread.Expression, context);
        var elementType = InferElementType(expression.StaticType.ClrType);
        return new BoundSpreadExpr(expression, new BoundType(elementType));
    }

    private BoundObjectLiteralExpr BindObjectLiteral(ObjectLiteralExpr objectLiteral, BindingContext context)
    {
        var properties = objectLiteral.Properties
            .Select(property =>
            {
                var (key, value) = property;
                if (key.Type == TokenType.DotDot && value is SpreadExpr spread)
                {
                    return new BoundObjectLiteralProperty(
                        PropertyName: null,
                        Value: Bind(spread.Expression, context),
                        IsSpread: true);
                }

                return new BoundObjectLiteralProperty(
                    PropertyName: key.Lexeme,
                    Value: Bind(value, context),
                    IsSpread: false);
            })
            .ToImmutableArray();

        var hasSpread = properties.Any(static p => p.IsSpread);
        var staticType = hasSpread
            ? new BoundType(typeof(System.Dynamic.ExpandoObject))
            : new BoundStructuralType(
                typeof(System.Dynamic.ExpandoObject),
                properties
                    .Where(static p => p.PropertyName != null)
                    .ToImmutableDictionary(static p => p.PropertyName!, static p => p.Value.StaticType.ClrType));
        return new BoundObjectLiteralExpr(properties, staticType);
    }

    private BoundSliceExpr BindSlice(SliceExpr slice, BindingContext context)
    {
        var target = Bind(slice.Target, context);
        var start = slice.Start != null ? Bind(slice.Start, context) : null;
        var end = slice.End != null ? Bind(slice.End, context) : null;
        var step = slice.Step != null ? Bind(slice.Step, context) : null;
        var sliceType = InferSliceType(target.StaticType.ClrType);
        return new BoundSliceExpr(target, start, end, step, new BoundType(sliceType));
    }

    private BoundObjectCreationExpr BindObjectCreation(ObjectCreationExpr objectCreation, BindingContext context)
    {
        var arguments = objectCreation.Arguments
            .Select(argument => Bind(argument, context))
            .ToImmutableArray();
        var initializerEntries = objectCreation.Initializer != null
            ? [
                ..objectCreation.Initializer.Entries
                    .Select(entry => new BoundInitializerEntry(
                        entry.PropertyName,
                        Bind(entry.Value, context),
                        entry.IndexerKey != null ? Bind(entry.IndexerKey, context) : null))
            ]
            : ImmutableArray<BoundInitializerEntry>.Empty;
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(objectCreation.TypeName);
        var staticType = resolvedType != null ? new BoundType(resolvedType) : BoundType.Unknown;
        return new BoundObjectCreationExpr(objectCreation.TypeName, arguments, initializerEntries, staticType);
    }

    private BoundArrayAllocationExpr BindTypedArrayCreation(TypedArrayCreationExpr typedArrayCreation, BindingContext context)
    {
        var size = Bind(typedArrayCreation.Size, context);
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayCreation.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundArrayAllocationExpr(elementType, [size], new BoundType(arrayType));
    }

    private BoundCollectionCreationExpr BindTypedArrayLiteral(TypedArrayLiteralExpr typedArrayLiteral, BindingContext context)
    {
        var elements = typedArrayLiteral.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayLiteral.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType);
        return new BoundCollectionCreationExpr(elements, elementType, CollectionKind.Array, null, new BoundType(arrayType));
    }

    private BoundTupleExpr BindTuple(TupleExpr tupleExpr, BindingContext context)
    {
        var elements = tupleExpr.Elements
            .Select(element => Bind(element.Expression, context))
            .ToImmutableArray();
        var names = tupleExpr.Elements
            .Select(static element => element.Name)
            .ToImmutableArray();
        var tupleType = CreateTupleStaticType(elements.Select(static element => element.StaticType.ClrType).ToArray());
        return new BoundTupleExpr(elements, names, new BoundType(tupleType));
    }

    private BoundArrayAllocationExpr BindMultiDimTypedArrayCreation(
        MultiDimTypedArrayCreationExpr multiDimTypedArrayCreation,
        BindingContext context)
    {
        var sizes = multiDimTypedArrayCreation.Sizes
            .Select(size => Bind(size, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(multiDimTypedArrayCreation.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType, multiDimTypedArrayCreation.Sizes.Count);
        return new BoundArrayAllocationExpr(elementType, sizes, new BoundType(arrayType));
    }

    private BoundMultiDimArrayInitExpr BindMultiDimArrayInit(
        MultiDimArrayInitExpr multiDimArrayInit,
        BindingContext context)
    {
        var explicitSizes = multiDimArrayInit.ExplicitSizes?
            .Select(size => Bind(size, context))
            .ToImmutableArray();
        var flatValues = multiDimArrayInit.FlatValues
            .Select(value => Bind(value, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(multiDimArrayInit.ElementTypeName) ?? typeof(object);
        var arrayType = RuntimeArrayFactory.GetArrayType(elementType, multiDimArrayInit.Rank);
        return new BoundMultiDimArrayInitExpr(
            elementType,
            multiDimArrayInit.Rank,
            explicitSizes,
            flatValues,
            multiDimArrayInit.InferredDimensions,
            new BoundType(arrayType));
    }

    private BoundExpr BindMultiDimIndexAccess(
        MultiDimIndexAccessExpr multiDimIndexAccess,
        BindingContext context)
    {
        var target = Bind(multiDimIndexAccess.Object, context);
        var indices = multiDimIndexAccess.Indices
            .Select(index => Bind(index, context))
            .ToImmutableArray();
        var targetType = target.StaticType.ClrType;
        var (resolved, elementType) = TryBindMultiDimIndex(targetType, indices.Length, context);

        if (resolved != null)
            return new BoundResolvedMultiDimIndexAccessExpr(
                target, indices, resolved.Value.TargetType, resolved.Value.IsArray, resolved.Value.Indexer,
                multiDimIndexAccess.NullSafe, new BoundType(elementType));

        return new BoundDynamicMultiDimIndexAccessExpr(target, indices, multiDimIndexAccess.NullSafe, new BoundType(elementType));
    }

    private BoundMultiDimIndexAssignExpr BindMultiDimIndexAssign(
        MultiDimIndexAssignExpr multiDimIndexAssign,
        BindingContext context)
    {
        var target = Bind(multiDimIndexAssign.Object, context);
        var indices = multiDimIndexAssign.Indices
            .Select(index => Bind(index, context))
            .ToImmutableArray();
        var value = Bind(multiDimIndexAssign.Value, context);
        var (resolved, _) = TryBindMultiDimIndex(target.StaticType.ClrType, indices.Length, context);
        return new BoundMultiDimIndexAssignExpr(target, indices, value,
            resolved?.TargetType, resolved?.IsArray ?? false, resolved?.Indexer, value.StaticType);
    }

    private static ((Type TargetType, bool IsArray, PropertyInfo? Indexer)? Resolved, Type ElementType) TryBindMultiDimIndex(
        Type targetType, int arity, BindingContext context)
    {
        if (targetType.IsArray)
        {
            var elementType = targetType.GetElementType() ?? typeof(object);
            return ((targetType, true, null), elementType);
        }

        if (targetType == typeof(object))
            return (null, typeof(object));

        var indexer = context.RuntimeContext.TypeMetadata
            .GetProperties(targetType, BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetIndexParameters().Length == arity);

        if (indexer != null)
            return ((targetType, false, indexer), indexer.PropertyType);

        return (null, typeof(object));
    }

    private BoundDeconstructionExpr BindDeconstruction(DeconstructionExpr deconstruction, BindingContext context)
    {
        var valueExpression = Bind(deconstruction.ValueExpression, context);
        var variableNames = deconstruction.VariableNames.ToImmutableArray();
        return new BoundDeconstructionExpr(variableNames, valueExpression, valueExpression.StaticType);
    }

    private static Type CreateTupleStaticType(Type[] elementTypes)
    {
        if (elementTypes.Length == 0)
            return typeof(ValueTuple);

        if (elementTypes.Length <= 7)
            return RuntimeGenericFactory.CloseGenericType(ConstructionRuntime.GetOpenValueTupleType(elementTypes.Length), elementTypes);

        var headTypes = new Type[8];
        Array.Copy(elementTypes, 0, headTypes, 0, 7);
        var restTypes = new Type[elementTypes.Length - 7];
        Array.Copy(elementTypes, 7, restTypes, 0, restTypes.Length);
        headTypes[7] = CreateTupleStaticType(restTypes);

        return RuntimeGenericFactory.CloseGenericType(ConstructionRuntime.GetOpenValueTupleType(8), headTypes);
    }

    private BoundInterpolatedStringExpr BindInterpolatedString(InterpolatedStringExpr interpolatedString, BindingContext context)
    {
        var parts = interpolatedString.Parts
            .Select(part => part switch
            {
                TextPart text => (BoundInterpolatedPart)new BoundInterpolatedTextPart(text.Text),
                ExpressionPart expressionPart => new BoundInterpolatedExpressionPart(
                    Bind(expressionPart.Expression, context),
                    expressionPart.AlignmentSpecifier,
                    expressionPart.FormatSpecifier),
                _ => throw new BindingNotSupportedException(
                    $"Interpolated part '{part.GetType().Name}' is not supported")
            })
            .ToImmutableArray();

        return new BoundInterpolatedStringExpr(parts, new BoundType(typeof(string)));
    }

    private BoundIndexFromEndExpr BindIndexFromEnd(IndexFromEndExpr expr, BindingContext context)
    {
        var operand = Bind(expr.Operand, context);
        return new BoundIndexFromEndExpr(operand, new BoundType(typeof(Index)));
    }

    private BoundRangeExpr BindRange(RangeExpr rangeExpr, BindingContext context)
    {
        var start = rangeExpr.Start != null ? Bind(rangeExpr.Start, context) : null;
        var end = rangeExpr.End != null ? Bind(rangeExpr.End, context) : null;
        // Open-ended ranges produce System.Range, not IEnumerable<int>
        var resultType = start == null || end == null ? typeof(Range) : typeof(IEnumerable<int>);
        return new BoundRangeExpr(start, end, rangeExpr.ExclusiveEnd, new BoundType(resultType));
    }

    /// <summary>
    /// ECMA-334 §12.6.3.15: Finding the best common type of a set of expressions.
    /// Collects all element static types as lower bounds, then finds the unique candidate
    /// to which all others implicitly convert (§12.6.3.12 fixing).
    /// Returns null if inference fails (no unique best type).
    /// </summary>
    internal static Type? FindBestCommonType(ImmutableArray<BoundExpr> elements)
    {
        if (elements.Length == 0)
            return null;

        var types = new List<Type>(elements.Length);
        var hasNullLiteral = false;
        foreach (var element in elements)
        {
            if (element is BoundLiteralExpr { Value: null })
            {
                hasNullLiteral = true;
                continue;
            }

            var clrType = element.StaticType.ClrType;
            if (!types.Contains(clrType))
                types.Add(clrType);
        }

        if (types.Count == 0)
            return hasNullLiteral ? typeof(object) : null;

        if (types.Count == 1)
            return LiftIfNeeded(types[0], hasNullLiteral);

        // §12.6.3.12: For each lower bound U, eliminate candidates where no implicit conversion
        // from U exists. The result must be a unique type to which all others implicitly convert.
        Type? bestType = null;
        foreach (var candidate in types)
        {
            var allConvert = true;
            foreach (var other in types)
            {
                if (other == candidate)
                    continue;
                if (!TypeHelpers.CanImplicitlyConvert(other, candidate))
                {
                    allConvert = false;
                    break;
                }
            }

            if (!allConvert)
                continue;

            if (bestType != null)
                return null; // ambiguous — two candidates both accept all

            bestType = candidate;
        }

        return bestType != null ? LiftIfNeeded(bestType, hasNullLiteral) : null;

        static Type LiftIfNeeded(Type type, bool hasNull) =>
            hasNull && type.IsValueType && Nullable.GetUnderlyingType(type) == null
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;
    }

    private static Type InferSliceType(Type targetType)
    {
        if (targetType == typeof(string))
            return typeof(string);

        if (targetType.IsArray)
            return targetType;

        return typeof(object);
    }
}
