using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.GotoDefaultStatement)]
internal static class GotoDefaultEmitter
{
    public static LinqExpression Emit(BoundGotoDefaultExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(ctx.SignalParam, LinqExpression.Field(null, ControlFlowGotoDefaultField));
    }
}
