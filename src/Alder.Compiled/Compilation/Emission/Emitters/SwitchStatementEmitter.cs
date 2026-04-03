using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class SwitchStatementEmitter : INodeEmitter<BoundSwitchStatementExpr>
{
    private static readonly object GotoDefaultSentinel = new();

    public LinqExpression Emit(BoundSwitchStatementExpr node, EmissionContext ctx)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "switchValue");
        var matchedVar = LinqExpression.Variable(typeof(bool), "switchMatched");
        var resultVar = LinqExpression.Variable(typeof(object), "switchResult");
        var doneLabel = LinqExpression.Label("switchDone");
        var loopBreak = LinqExpression.Label("switchLoopBreak");
        var loopContinue = LinqExpression.Label("switchLoopContinue");

        var outerStatements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, ctx.EmitBoxed(node.Expression)),
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        var loopBody = new List<LinqExpression>
        {
            LinqExpression.Assign(matchedVar, LinqExpression.Constant(false))
        };

        var defaultCaseIndex = -1;
        var previousSwitchDepth = ctx.SwitchDepth;
        ctx.SwitchDepth = previousSwitchDepth + 1;
        try
        {
            for (var i = 0; i < node.Cases.Length; i++)
            {
                var switchCase = node.Cases[i];
                if (switchCase.CasePattern == null)
                {
                    defaultCaseIndex = i;
                    continue;
                }

                var previousContextVar = LinqExpression.Variable(typeof(AlderContext), $"switchPrevCtx{i}");
                var matchCondition = BuildMatchCondition(valueVar, switchCase, ctx);
                var executeCase = EmitCaseExecution(node.Cases, i, resultVar, doneLabel, ctx);

                loopBody.Add(
                    LinqExpression.Block(
                        typeof(void),
                        [previousContextVar],
                        LinqExpression.Assign(previousContextVar, ctx.ContextParam),
                        LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
                        LinqExpression.TryFinally(
                            LinqExpression.IfThen(
                                LinqExpression.AndAlso(LinqExpression.Not(matchedVar), matchCondition),
                                LinqExpression.Block(
                                    LinqExpression.Assign(matchedVar, LinqExpression.Constant(true)),
                                    LinqExpression.Assign(resultVar, executeCase),
                                    LinqExpression.Goto(doneLabel))),
                            LinqExpression.Assign(ctx.ContextParam, previousContextVar))));
            }

            if (defaultCaseIndex >= 0)
            {
                var executeDefault = EmitCaseExecution(node.Cases, defaultCaseIndex, resultVar, doneLabel, ctx);
                loopBody.Add(
                    LinqExpression.IfThen(
                        LinqExpression.Not(matchedVar),
                        LinqExpression.Block(
                            LinqExpression.Assign(resultVar, executeDefault),
                            LinqExpression.Goto(doneLabel))));
            }

            loopBody.Add(LinqExpression.Label(doneLabel));

            var signalParam = ctx.SignalParam;
            var kindExpr = LinqExpression.Property(signalParam, ControlFlowSignalKindProperty);
            loopBody.Add(
                LinqExpression.IfThen(
                    LinqExpression.NotEqual(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                    LinqExpression.Block(
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.GotoCase)),
                            LinqExpression.Block(
                                LinqExpression.Assign(valueVar, LinqExpression.Property(signalParam, ControlFlowValueProperty)),
                                LinqExpression.Assign(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Continue(loopContinue))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.GotoDefault)),
                            LinqExpression.Block(
                                LinqExpression.Assign(valueVar, LinqExpression.Constant(GotoDefaultSentinel)),
                                LinqExpression.Assign(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Continue(loopContinue))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                            LinqExpression.Block(
                                LinqExpression.Assign(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))))))));

            loopBody.Add(LinqExpression.Break(loopBreak));

            outerStatements.Add(LinqExpression.Loop(
                LinqExpression.Block(typeof(void), loopBody),
                loopBreak,
                loopContinue));
            outerStatements.Add(resultVar);
            return LinqExpression.Block(typeof(object), [valueVar, matchedVar, resultVar], outerStatements);
        }
        finally
        {
            ctx.SwitchDepth = previousSwitchDepth;
        }
    }

    private static LinqExpression BuildMatchCondition(
        ParameterExpression valueVar,
        BoundSwitchCase switchCase,
        EmissionContext ctx)
    {
        var patternMatch = LinqExpression.Call(
            MatchPatternMethod,
            valueVar,
            LinqExpression.Constant(switchCase.CasePattern!, typeof(Pattern)),
            ctx.ContextParam,
            ctx.CancellationTokenParam);

        if (switchCase.WhenGuard == null)
            return patternMatch;

        return LinqExpression.AndAlso(
            patternMatch,
            LinqExpression.Call(RequireBooleanMethod, ctx.EmitBoxed(switchCase.WhenGuard)));
    }

    private static LinqExpression EmitCaseExecution(
        ImmutableArray<BoundSwitchCase> cases,
        int startIndex,
        ParameterExpression resultVar,
        LabelTarget doneLabel,
        EmissionContext ctx)
    {
        var statements = new List<LinqExpression>();

        for (var i = startIndex; i < cases.Length; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            if (!TerminatesControlFlow(switchCase.Statements[^1]))
                throw new AlderException(DiagnosticDescriptors.CaseFallThrough);

            var caseDone = LinqExpression.Label($"switchCaseDone{i}");
            BlockEmitter.EmitStatementListBody(ctx, statements, switchCase.Statements, resultVar, caseDone);
            statements.Add(LinqExpression.Label(caseDone));

            var signalParam = ctx.SignalParam;
            var kindExpr = LinqExpression.Property(signalParam, ControlFlowSignalKindProperty);
            statements.Add(
                LinqExpression.IfThen(
                    LinqExpression.NotEqual(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                    LinqExpression.Block(
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                            LinqExpression.Block(
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Goto(doneLabel))),
                        LinqExpression.Goto(doneLabel))));

            statements.Add(
                LinqExpression.Throw(
                    LinqExpression.Constant(new AlderException(DiagnosticDescriptors.CaseFallThrough)),
                    typeof(void)));
            break;
        }

        if (statements.Count == 0)
            return LinqExpression.Constant(null, typeof(object));

        statements.Add(resultVar);
        return LinqExpression.Block(typeof(object), statements);
    }

    private static bool TerminatesControlFlow(BoundExpr expr)
    {
        return expr.Kind switch
        {
            BoundNodeKind.BreakStatement => true,
            BoundNodeKind.ReturnStatement => true,
            BoundNodeKind.ContinueStatement => true,
            BoundNodeKind.GotoStatement => true,
            BoundNodeKind.GotoCaseStatement => true,
            BoundNodeKind.GotoDefaultStatement => true,
            BoundNodeKind.ThrowExpression => true,
            BoundNodeKind.Block when ((BoundBlockExpr)expr).Statements.Length > 0 =>
                TerminatesControlFlow(((BoundBlockExpr)expr).Statements[^1]),
            _ => false
        };
    }
}
