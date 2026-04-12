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
        var elements = expr.Elements
            .Select(element => binder.Bind(element.Expression, context))
            .ToImmutableArray();
        var names = expr.Elements
            .Select(static element => element.Name)
            .ToImmutableArray();
        var elementClrTypes = elements.Select(static element => element.StaticType.ClrType).ToArray();
        var tupleType = CreateTupleStaticType(elementClrTypes);
        var staticType = CreateBoundType(tupleType, names, elementClrTypes);
        return new BoundTupleExpr(elements, names, staticType);
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
