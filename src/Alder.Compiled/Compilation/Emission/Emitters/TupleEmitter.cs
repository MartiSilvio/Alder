using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class TupleEmitter : INodeEmitter<BoundTupleExpr>
{
    public LinqExpression Emit(BoundTupleExpr node, EmissionContext ctx)
    {
        var hasNames = node.ElementNames.Any(static n => n != null);
        if (!hasNames && TypeHelpers.IsValueTupleType(node.StaticType.ClrType) && node.Elements.Length <= 7)
        {
            var pure = TryEmitPure(node, ctx);
            if (pure != null) return pure;
        }

        var elements = LinqExpression.NewArrayInit(
            typeof(object),
            node.Elements.Select(ctx.EmitBoxed));

        if (hasNames)
        {
            var names = LinqExpression.NewArrayInit(
                typeof(string),
                node.ElementNames.Select(static n =>
                    n != null
                        ? LinqExpression.Constant(n, typeof(string))
                        : LinqExpression.Constant(null, typeof(string))));
            return LinqExpression.Call(CreateNamedTupleMethod, elements, names);
        }

        return LinqExpression.Call(CreateTupleMethod, elements);
    }

    private static NewExpression? TryEmitPure(BoundTupleExpr node, EmissionContext ctx)
    {
        var tupleType = node.StaticType.ClrType;
        var elementTypes = tupleType.GetGenericArguments();
        var ctor = tupleType.GetConstructor(elementTypes);
        if (ctor == null)
            return null;

        var args = new LinqExpression[node.Elements.Length];
        for (var i = 0; i < args.Length; i++)
            args[i] = EmitHelpers.EnsureTypedExpression(ctx.Emit(node.Elements[i]), elementTypes[i]);

        return LinqExpression.New(ctor, args);
    }
}
