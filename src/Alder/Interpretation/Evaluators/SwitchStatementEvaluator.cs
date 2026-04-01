using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.SwitchStatement)]
internal static class SwitchStatementEvaluator
{
    public static object? Evaluate(BoundSwitchStatementExpr node, EvaluationContext ctx)
    {
        var switchValue = ctx.Evaluate(node.Expression);
        var matched = false;
        var defaultCaseIndex = -1;

        ctx.BreakContextDepth++;
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

                if (matched)
                    continue;

                var previousContext = ctx.Context;
                ctx.Context = ctx.Context.CreateChild();
                try
                {
                    if (!TypeHelpers.RequireBoolean(ctx.MatchPattern(switchValue, switchCase.CasePattern)))
                        continue;

                    if (switchCase.WhenGuard != null)
                    {
                        var guardResult = ctx.Evaluate(switchCase.WhenGuard);
                        if (!TypeHelpers.RequireBoolean(guardResult))
                            continue;
                    }

                    matched = true;
                    var signal = ExecuteSwitchCaseWithGoto(node, i, ctx);
                    if (signal != null)
                        return signal.SignalKind == ControlFlowSignal.Kind.Break ? null : signal;
                }
                finally
                {
                    ctx.Context = previousContext;
                }
            }

            if (!matched && defaultCaseIndex >= 0)
            {
                var signal = ExecuteSwitchCaseWithGoto(node, defaultCaseIndex, ctx);
                if (signal != null && signal.SignalKind != ControlFlowSignal.Kind.Break)
                    return signal;
            }

            return null;
        }
        finally
        {
            ctx.BreakContextDepth--;
        }
    }

    private static ControlFlowSignal? ExecuteSwitchCaseWithGoto(
        BoundSwitchStatementExpr switchStatement, int startIndex, EvaluationContext ctx)
    {
        var signal = ExecuteSwitchCaseStatements(switchStatement.Cases, startIndex, ctx);
        while (signal is { SignalKind: ControlFlowSignal.Kind.GotoCase or ControlFlowSignal.Kind.GotoDefault })
        {
            int targetIndex;
            if (signal.SignalKind == ControlFlowSignal.Kind.GotoDefault)
            {
                targetIndex = FindDefaultCaseIndex(switchStatement.Cases);
            }
            else
            {
                targetIndex = FindCaseIndex(switchStatement, signal.Value, ctx);
            }
            if (targetIndex < 0)
                throw new AlderException(DiagnosticDescriptors.LabelNotFound, signal.Value?.ToString() ?? "default");
            signal = ExecuteSwitchCaseStatements(switchStatement.Cases, targetIndex, ctx);
        }
        return signal;
    }

    private static ControlFlowSignal? ExecuteSwitchCaseStatements(
        IReadOnlyList<BoundSwitchCase> cases, int startIndex, EvaluationContext ctx)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            foreach (var statement in switchCase.Statements)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                var result = ctx.Evaluate(statement);
                if (result is ControlFlowSignal signal)
                    return signal;
            }

            throw new AlderException(DiagnosticDescriptors.CaseFallThrough);
        }

        return null;
    }

    private static int FindDefaultCaseIndex(IReadOnlyList<BoundSwitchCase> cases)
    {
        for (var i = 0; i < cases.Count; i++)
            if (cases[i].CasePattern == null)
                return i;
        return -1;
    }

    private static int FindCaseIndex(BoundSwitchStatementExpr switchStatement, object? targetValue, EvaluationContext ctx)
    {
        for (var i = 0; i < switchStatement.Cases.Length; i++)
        {
            var casePattern = switchStatement.Cases[i].CasePattern;
            if (casePattern is ConstantPattern cp)
            {
                var caseValue = ctx.MatchPattern(targetValue, cp);
                if (TypeHelpers.RequireBoolean(caseValue))
                    return i;
            }
        }
        return -1;
    }
}
