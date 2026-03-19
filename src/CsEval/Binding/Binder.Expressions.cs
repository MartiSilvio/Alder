using CsEval.Binding.BoundNodes;
using CsEval.Parsing;
using CsEval.Runtime;
using System.Collections.Immutable;

namespace CsEval.Binding;

internal sealed partial class Binder
{
    private static BoundLiteralExpr BindTypeReference(TypeReferenceExpr typeReference, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeReference.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, typeof(Type));
    }

    private static BoundLiteralExpr BindTypeof(TypeofExpr typeofExpr, BindingContext context)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(typeofExpr.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, typeof(Type));
    }

    private static BoundLiteralExpr BindDefault(DefaultExpr defaultExpr, BindingContext context)
    {
        if (defaultExpr.TypeToken == null)
            return new BoundLiteralExpr(null, typeof(object));

        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(defaultExpr.TypeToken.Value.Lexeme);
        var value = TypeHelpers.GetDefaultValue(resolvedType);
        return new BoundLiteralExpr(value, resolvedType);
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
            return new BoundIdentifierExpr(name, typeof(object));
        }

        context.TryGetVariableType(name, out var staticType);
        if (staticType != typeof(object))
        {
            var isLocal = context.TryGetLocal(name, out _, out var localId);
            return new BoundIdentifierExpr(name, staticType, isLocal ? localId : null);
        }

        var resolvedType = context.RuntimeContext.TypeResolver.TryResolveType(name);
        if (resolvedType != null)
            return new BoundLiteralExpr(resolvedType, typeof(Type));
        return new BoundIdentifierExpr(name, staticType);
    }

    private BoundArrayLiteralExpr BindArrayLiteral(ArrayLiteralExpr arrayLiteral, BindingContext context)
    {
        var elements = arrayLiteral.Elements
            .Select(element => Bind(element, context))
            .ToImmutableArray();
        return new BoundArrayLiteralExpr(elements, typeof(object));
    }

    private BoundSpreadExpr BindSpread(SpreadExpr spread, BindingContext context)
    {
        var expression = Bind(spread.Expression, context);
        return new BoundSpreadExpr(expression, typeof(object));
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

        return new BoundObjectLiteralExpr(properties, typeof(object));
    }

    private BoundSliceExpr BindSlice(SliceExpr slice, BindingContext context)
    {
        var target = Bind(slice.Target, context);
        var start = slice.Start != null ? Bind(slice.Start, context) : null;
        var end = slice.End != null ? Bind(slice.End, context) : null;
        var step = slice.Step != null ? Bind(slice.Step, context) : null;
        return new BoundSliceExpr(target, start, end, step, typeof(object));
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
        return new BoundObjectCreationExpr(objectCreation.TypeName, arguments, initializerEntries, resolvedType);
    }

    private BoundTypedArrayCreationExpr BindTypedArrayCreation(TypedArrayCreationExpr typedArrayCreation, BindingContext context)
    {
        var size = Bind(typedArrayCreation.Size, context);
        var elementType = context.RuntimeContext.TypeResolver.TryResolveType(typedArrayCreation.ElementTypeName);
        var arrayType = elementType != null
            ? RuntimeArrayFactory.GetArrayType(elementType)
            : typeof(Array);
        return new BoundTypedArrayCreationExpr(typedArrayCreation.ElementTypeName, size, arrayType);
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
        return new BoundTypedArrayLiteralExpr(typedArrayLiteral.ElementTypeName, elements, arrayType);
    }

    private BoundTupleExpr BindTuple(TupleExpr tupleExpr, BindingContext context)
    {
        var elements = tupleExpr.Elements
            .Select(element => Bind(element.Expression, context))
            .ToImmutableArray();
        var names = tupleExpr.Elements
            .Select(static element => element.Name)
            .ToImmutableArray();
        var tupleType = CreateTupleStaticType(elements.Select(static element => element.StaticType).ToArray());
        return new BoundTupleExpr(elements, names, tupleType);
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
        return new BoundMultiDimTypedArrayCreationExpr(multiDimTypedArrayCreation.ElementTypeName, sizes, arrayType);
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
            arrayType);
    }

    private BoundMultiDimIndexAccessExpr BindMultiDimIndexAccess(
        MultiDimIndexAccessExpr multiDimIndexAccess,
        BindingContext context)
    {
        var target = Bind(multiDimIndexAccess.Object, context);
        var indices = multiDimIndexAccess.Indices
            .Select(index => Bind(index, context))
            .ToImmutableArray();
        return new BoundMultiDimIndexAccessExpr(target, indices, multiDimIndexAccess.NullSafe, typeof(object));
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
        {
            var openType = elementTypes.Length switch
            {
                1 => typeof(ValueTuple<>),
                2 => typeof(ValueTuple<,>),
                3 => typeof(ValueTuple<,,>),
                4 => typeof(ValueTuple<,,,>),
                5 => typeof(ValueTuple<,,,,>),
                6 => typeof(ValueTuple<,,,,,>),
                7 => typeof(ValueTuple<,,,,,,>),
                _ => throw new CsEvalException(Diagnostics.DiagnosticDescriptors.UnsupportedTupleArity, elementTypes.Length)
            };
            return RuntimeGenericFactory.CloseGenericType(openType, elementTypes);
        }

        var headTypes = new Type[8];
        Array.Copy(elementTypes, 0, headTypes, 0, 7);
        var restTypes = new Type[elementTypes.Length - 7];
        Array.Copy(elementTypes, 7, restTypes, 0, restTypes.Length);
        headTypes[7] = CreateTupleStaticType(restTypes);

        return RuntimeGenericFactory.CloseGenericType(typeof(ValueTuple<,,,,,,,>), headTypes);
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

        return new BoundInterpolatedStringExpr(parts, typeof(string));
    }

    private BoundIndexFromEndExpr BindIndexFromEnd(IndexFromEndExpr expr, BindingContext context)
    {
        var operand = Bind(expr.Operand, context);
        return new BoundIndexFromEndExpr(operand, typeof(Index));
    }

    private BoundRangeExpr BindRange(RangeExpr rangeExpr, BindingContext context)
    {
        var start = Bind(rangeExpr.Start, context);
        var end = Bind(rangeExpr.End, context);
        return new BoundRangeExpr(start, end, rangeExpr.ExclusiveEnd, typeof(IEnumerable<int>));
    }
}
