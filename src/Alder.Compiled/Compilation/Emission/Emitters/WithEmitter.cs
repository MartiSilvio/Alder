using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class WithEmitter : INodeEmitter<BoundWithExpr>
{
    private static readonly MethodInfo ApplyWithMethod =
        typeof(WithRuntime).GetMethod(nameof(WithRuntime.ApplyWith), BindingFlags.Public | BindingFlags.Static)!;

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
            ctx.ConfigParam,
            ctx.ContextParam);
    }
}
