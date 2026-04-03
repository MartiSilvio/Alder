using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class WithEmitter : INodeEmitter<BoundWithExpr>
{
    public LinqExpression Emit(BoundWithExpr node, EmissionContext ctx)
    {
        var original = ctx.Emit(node.Object);

        var names = LinqExpression.NewArrayInit(typeof(string),
            node.Initializers.Select(i => LinqExpression.Constant(i.PropertyName)));

        var values = LinqExpression.NewArrayInit(typeof(object),
            node.Initializers.Select(i => ctx.EmitBoxed(i.Value)));

        return LinqExpression.Call(ApplyWithMethod,
            LinqExpression.Convert(original, typeof(object)),
            names,
            values,
            ctx.ContextParam);
    }
}
