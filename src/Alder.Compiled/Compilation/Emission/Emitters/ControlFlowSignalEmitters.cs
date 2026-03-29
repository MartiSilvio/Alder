using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Interpretation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class BreakEmitter : INodeEmitter<BoundBreakExpr>
{
    public LinqExpression Emit(BoundBreakExpr node, EmissionContext ctx)
    {
        if (ctx.LoopDepth > 0 || ctx.SwitchDepth > 0)
            return LinqExpression.Assign(ctx.SignalParam, LinqExpression.Field(null, ControlFlowBreakField));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(ControlFlowSignal));
    }
}

internal sealed class ContinueEmitter : INodeEmitter<BoundContinueExpr>
{
    public LinqExpression Emit(BoundContinueExpr node, EmissionContext ctx)
    {
        if (ctx.LoopDepth > 0)
            return LinqExpression.Assign(ctx.SignalParam, LinqExpression.Field(null, ControlFlowContinueField));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(ControlFlowSignal));
    }
}

internal sealed class ReturnEmitter : INodeEmitter<BoundReturnExpr>
{
    public LinqExpression Emit(BoundReturnExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(
            ctx.SignalParam,
            LinqExpression.Call(
                ControlFlowReturnMethod,
                node.Value == null
                    ? LinqExpression.Constant(null, typeof(object))
                    : ctx.EmitBoxed(node.Value)));
    }
}

internal sealed class GotoEmitter : INodeEmitter<BoundGotoExpr>
{
    public LinqExpression Emit(BoundGotoExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(
            ctx.SignalParam,
            LinqExpression.Call(ControlFlowGotoMethod, LinqExpression.Constant(node.Label)));
    }
}

internal sealed class GotoCaseEmitter : INodeEmitter<BoundGotoCaseExpr>
{
    public LinqExpression Emit(BoundGotoCaseExpr node, EmissionContext ctx)
    {
        return LinqExpression.Assign(
            ctx.SignalParam,
            LinqExpression.Call(ControlFlowGotoCaseMethod, ctx.EmitBoxed(node.Value)));
    }
}
