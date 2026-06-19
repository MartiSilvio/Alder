using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.ForEachStatement)]
internal static class ForEachEmitter
{
    public static LinqExpression Emit(BoundForEachExpr node, EmissionContext ctx)
    {
        var enumerableVar = LinqExpression.Variable(typeof(object), "foreachCollection");
        var enumeratorVar = LinqExpression.Variable(typeof(IEnumerator), "foreachEnumerator");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachResult");
        var currentVar = LinqExpression.Variable(typeof(object), "foreachCurrent");
        var loopBreakLabel = LinqExpression.Label(typeof(object), "foreachBreak");
        var loopContinueLabel = LinqExpression.Label("foreachContinue");

        List<LinqExpression> loopBody;
        var previousDepth = ctx.LoopDepth;
        ctx.LoopDepth = previousDepth + 1;
        try
        {
            var iterationBody = BlockEmitter.EmitForeachIteration(
                ctx,
                node.VariableName,
                currentVar,
                node.Body,
                node.ElementType,
                node.SourceElementType);
            loopBody = new List<LinqExpression>
            {
                LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    ctx.ConstraintStateParam,
                    LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints)),
                    ctx.CancellationTokenParam),
                LinqExpression.Call(
                    CheckLoopIterationConstraintMethod,
                    ctx.ConstraintStateParam,
                    LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints))),
                LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(enumeratorVar, MoveNextMethod)),
                    LinqExpression.Break(loopBreakLabel, resultVar)),
                LinqExpression.Assign(currentVar, LinqExpression.Convert(LinqExpression.Call(enumeratorVar, GetCurrentMethod), typeof(object))),
                LinqExpression.Assign(resultVar, iterationBody),
                BlockEmitter.BuildLoopSignalDispatch(ctx, resultVar, loopBreakLabel, loopContinueLabel),
                LinqExpression.Label(loopContinueLabel)
            };
        }
        finally
        {
            ctx.LoopDepth = previousDepth;
        }

        var disposableVar = LinqExpression.Variable(typeof(IDisposable), "foreachDisposable");
        var disposeBlock = LinqExpression.Block(
            LinqExpression.Assign(disposableVar, LinqExpression.TypeAs(enumeratorVar, typeof(IDisposable))),
            LinqExpression.IfThen(
                LinqExpression.NotEqual(disposableVar, LinqExpression.Constant(null, typeof(IDisposable))),
                LinqExpression.Call(disposableVar, DisposeMethod)));

        return LinqExpression.Block(
            typeof(object),
            [enumerableVar, enumeratorVar, resultVar, currentVar, disposableVar],
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(enumerableVar, LinqExpression.Call(EnsureEnumerableMethod, ctx.EmitBoxed(node.Collection))),
            LinqExpression.Assign(enumeratorVar, LinqExpression.Call(GetEnumeratorMethod, enumerableVar)),
            LinqExpression.TryFinally(
                LinqExpression.Loop(LinqExpression.Block(loopBody), loopBreakLabel),
                disposeBlock),
            resultVar);
    }
}
