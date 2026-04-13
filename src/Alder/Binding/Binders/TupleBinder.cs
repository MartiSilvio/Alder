using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;

namespace Alder.Binding.Binders;

[BindsNode(typeof(TupleExpr))]
internal static class TupleBinder
{
    public static BoundExpr Bind(TupleExpr expr, BindingContext context, BinderContext binder)
    {
        return BindWithTargetType(expr, context, binder, targetTupleType: null);
    }

    // §10.2.13: element-wise tuple conversion. When a tuple literal has a target type (e.g.
    // `(long, long) t = (1, 2)`), each element is bound against the corresponding target
    // element type so widening casts are inserted up front rather than being reconstructed
    // from a boxed `ValueTuple<int,int>` at runtime.
    internal static BoundExpr BindWithTargetType(
        TupleExpr expr,
        BindingContext context,
        BinderContext binder,
        Type? targetTupleType)
    {
        var targetArgs = targetTupleType != null && TypeHelpers.IsValueTupleType(targetTupleType)
                         && targetTupleType.GetGenericArguments().Length == expr.Elements.Count
            ? targetTupleType.GetGenericArguments()
            : null;

        var elements = ImmutableArray.CreateBuilder<BoundExpr>(expr.Elements.Count);
        for (var i = 0; i < expr.Elements.Count; i++)
        {
            var bound = binder.Bind(expr.Elements[i].Expression, context);
            if (targetArgs != null)
                bound = CoerceElement(bound, targetArgs[i]);
            elements.Add(bound);
        }

        var names = expr.Elements
            .Select(static element => element.Name)
            .ToImmutableArray();
        var elementClrTypes = elements.Select(static element => element.StaticType.ClrType).ToArray();
        var tupleType = CreateTupleStaticType(elementClrTypes);
        var staticType = CreateBoundType(tupleType, names, elementClrTypes);
        return new BoundTupleExpr(elements.ToImmutable(), names, staticType);
    }

    private static BoundExpr CoerceElement(BoundExpr element, Type targetElementType)
    {
        var sourceType = element.StaticType.ClrType;
        if (sourceType == targetElementType || sourceType == typeof(object))
            return element;
        if (!TypeHelpers.CanImplicitlyConvert(sourceType, targetElementType))
            return element;
        return new BoundCastExpr(element, targetElementType, sourceType, new BoundType(targetElementType))
        {
            Span = element.Span
        };
    }

    private static BoundType CreateBoundType(Type tupleType, ImmutableArray<string?> names, Type[] elementClrTypes)
    {
        if (names.All(static n => n == null))
            return new BoundType(tupleType);

        return BoundStructuralType.FromElementNames(tupleType, names, elementClrTypes);
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
}
