using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class GotoDefaultEmitter : INodeEmitter<BoundGotoDefaultExpr>
{
    public LinqExpression Emit(BoundGotoDefaultExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(ctx.SignalParam, LinqExpression.Field(null, ControlFlowGotoDefaultField));
    }
}
