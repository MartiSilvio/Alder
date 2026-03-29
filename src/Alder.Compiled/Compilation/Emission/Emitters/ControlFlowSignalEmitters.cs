using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class BreakEmitter : INodeEmitter<BoundBreakExpr>
{
    public Expression Emit(BoundBreakExpr node, EmissionContext ctx)
    {
        if (ctx.LoopDepth > 0 || ctx.SwitchDepth > 0)
            return LinqExpression.Convert(LinqExpression.Field(null, ControlFlowBreakField), typeof(object));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(object));
    }
}

internal sealed class ContinueEmitter : INodeEmitter<BoundContinueExpr>
{
    public Expression Emit(BoundContinueExpr node, EmissionContext ctx)
    {
        if (ctx.LoopDepth > 0)
            return LinqExpression.Convert(LinqExpression.Field(null, ControlFlowContinueField), typeof(object));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(object));
    }
}

internal sealed class ReturnEmitter : INodeEmitter<BoundReturnExpr>
{
    public Expression Emit(BoundReturnExpr node, EmissionContext ctx)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(
                ControlFlowReturnMethod,
                node.Value == null
                    ? LinqExpression.Constant(null, typeof(object))
                    : EmitHelpers.AsObject(ctx.Emit(node.Value))),
            typeof(object));
    }
}

internal sealed class GotoEmitter : INodeEmitter<BoundGotoExpr>
{
    public Expression Emit(BoundGotoExpr node, EmissionContext ctx)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(ControlFlowGotoMethod, LinqExpression.Constant(node.Label)),
            typeof(object));
    }
}

internal sealed class GotoCaseEmitter : INodeEmitter<BoundGotoCaseExpr>
{
    public Expression Emit(BoundGotoCaseExpr node, EmissionContext ctx)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(ControlFlowGotoCaseMethod, EmitHelpers.AsObject(ctx.Emit(node.Value))),
            typeof(object));
    }
}
