using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;

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
            return new BoundLiteralExpr(null, new BoundType(typeof(object)));

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
            // Runtime resolution gives functions/modules precedence over variables.
            // Keep static type as object so compiled binding does not incorrectly
            // assume variable numeric types for shadowed identifiers.
            return new BoundIdentifierExpr(name, new BoundType(typeof(object)));
        }

        context.TryGetVariableType(name, out var staticType);
        if (staticType.ClrType != typeof(object))
        {
            var isLocal = context.TryGetLocal(name, out _, out var localId);
            return new BoundIdentifierExpr(name, staticType, isLocal ? localId : null);
        }

        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
        return new BoundIdentifierExpr(name, staticType);
    }

    private BoundArrayLiteralExpr BindArrayLiteral(ArrayLiteralExpr arrayLiteral, BindingContext context)
    {
        var elements = arrayLiteral.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();
        var arrayClrType = InferArrayLiteralType(elements);
        var elementMemberTypes = InferCommonElementMemberTypes(elements);
        var arrayBoundType = elementMemberTypes != null
            ? new BoundType(arrayClrType, elementMemberTypes)
            : new BoundType(arrayClrType);
        return new BoundArrayLiteralExpr(elements, arrayBoundType);
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
            : new BoundType(
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
        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(objectCreation.TypeName) ?? typeof(object);
        return new BoundObjectCreationExpr(objectCreation.TypeName, arguments, initializerEntries, new BoundType(resolvedType));
    }

    private BoundTypedArrayCreationExpr BindTypedArrayCreation(TypedArrayCreationExpr typedArrayCreation, BindingContext context)
    {
        var size = Bind(typedArrayCreation.Size, context);
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayCreation.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType)
            : typeof(Array);
        return new BoundTypedArrayCreationExpr(typedArrayCreation.ElementTypeName, size, new BoundType(arrayType));
    }

    private BoundTypedArrayLiteralExpr BindTypedArrayLiteral(TypedArrayLiteralExpr typedArrayLiteral, BindingContext context)
    {
        var elements = typedArrayLiteral.Elements.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayLiteral.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType)
            : typeof(Array);
        return new BoundTypedArrayLiteralExpr(typedArrayLiteral.ElementTypeName, elements, new BoundType(arrayType));
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

    private BoundMultiDimTypedArrayCreationExpr BindMultiDimTypedArrayCreation(
        MultiDimTypedArrayCreationExpr multiDimTypedArrayCreation,
        BindingContext context)
    {
        var sizes = multiDimTypedArrayCreation.Sizes
            .Select(size => Bind(size, context))
            .ToImmutableArray();
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(multiDimTypedArrayCreation.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType, multiDimTypedArrayCreation.Sizes.Count)
            : typeof(Array);
        return new BoundMultiDimTypedArrayCreationExpr(multiDimTypedArrayCreation.ElementTypeName, sizes, new BoundType(arrayType));
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
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(multiDimArrayInit.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType, multiDimArrayInit.Rank)
            : typeof(Array);
        return new BoundMultiDimArrayInitExpr(
            multiDimArrayInit.ElementTypeName,
            multiDimArrayInit.Rank,
            explicitSizes,
            flatValues,
            multiDimArrayInit.InferredDimensions,
            new BoundType(arrayType));
    }

    private BoundMultiDimIndexAccessExpr BindMultiDimIndexAccess(
        MultiDimIndexAccessExpr multiDimIndexAccess,
        BindingContext context)
    {
        var target = Bind(multiDimIndexAccess.Object, context);
        var indices = multiDimIndexAccess.Indices
            .Select(index => Bind(index, context))
            .ToImmutableArray();
        var elementType = target.StaticType.ClrType.IsArray
            ? target.StaticType.ClrType.GetElementType() ?? typeof(object)
            : typeof(object);
        return new BoundMultiDimIndexAccessExpr(target, indices, multiDimIndexAccess.NullSafe, new BoundType(elementType));
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
        return new BoundMultiDimIndexAssignExpr(target, indices, value, value.StaticType);
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

    private static Type InferArrayLiteralType(ImmutableArray<BoundExpr> elements)
    {
        if (elements.Length == 0)
            return typeof(object[]);

        var firstType = elements[0].StaticType.ClrType;
        if (firstType == typeof(object))
            return typeof(object[]);

        for (var i = 1; i < elements.Length; i++)
        {
            if (elements[i].StaticType.ClrType != firstType)
                return typeof(object[]);
        }

        return RuntimeArrayFactory.GetArrayType(firstType);
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
