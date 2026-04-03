using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.SwitchStatement)]
internal static class SwitchStatementEvaluator
{
    public static object? Evaluate(BoundSwitchStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var switchValue = ctx.Evaluate(node.Expression, ct);
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
                    if (!TypeHelpers.RequireBoolean(ctx.MatchPattern(switchValue, switchCase.CasePattern, ct)))
                        continue;

                    if (switchCase.WhenGuard != null)
                    {
                        var guardResult = ctx.Evaluate(switchCase.WhenGuard, ct);
                        if (!TypeHelpers.RequireBoolean(guardResult))
                            continue;
                    }

                    matched = true;
                    var signal = ExecuteSwitchCaseWithGoto(node, i, ctx, ct);
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
                var signal = ExecuteSwitchCaseWithGoto(node, defaultCaseIndex, ctx, ct);
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
        BoundSwitchStatementExpr switchStatement, int startIndex, EvaluationContext ctx, CancellationToken ct)
    {
        var signal = ExecuteSwitchCaseStatements(switchStatement.Cases, startIndex, ctx, ct);
        while (signal is { SignalKind: ControlFlowSignal.Kind.GotoCase or ControlFlowSignal.Kind.GotoDefault })
        {
            int targetIndex;
            if (signal.SignalKind == ControlFlowSignal.Kind.GotoDefault)
            {
                targetIndex = FindDefaultCaseIndex(switchStatement.Cases);
            }
            else
            {
                targetIndex = FindCaseIndex(switchStatement, signal.Value, ctx, ct);
            }
            if (targetIndex < 0)
                throw new AlderException(DiagnosticDescriptors.LabelNotFound, signal.Value?.ToString() ?? "default");
            signal = ExecuteSwitchCaseStatements(switchStatement.Cases, targetIndex, ctx, ct);
        }
        return signal;
    }

    private static ControlFlowSignal? ExecuteSwitchCaseStatements(
        IReadOnlyList<BoundSwitchCase> cases, int startIndex, EvaluationContext ctx, CancellationToken ct)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            foreach (var statement in switchCase.Statements)
            {
                ct.ThrowIfCancellationRequested();
                var result = ctx.Evaluate(statement, ct);
                if (result is ControlFlowSignal signal)
                    return signal;
            }

            throw new AlderException(DiagnosticDescriptors.CaseFallThrough);
        }

        return null;
    }

    public static async ValueTask<object?> EvaluateAsync(BoundSwitchStatementExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var switchValue = await ctx.EvaluateAsync(node.Expression, ct);
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
                    if (!TypeHelpers.RequireBoolean(ctx.MatchPattern(switchValue, switchCase.CasePattern, ct)))
                        continue;

                    if (switchCase.WhenGuard != null)
                    {
                        var guardResult = await ctx.EvaluateAsync(switchCase.WhenGuard, ct);
                        if (!TypeHelpers.RequireBoolean(guardResult))
                            continue;
                    }

                    matched = true;
                    var signal = await ExecuteSwitchCaseWithGotoAsync(node, i, ctx, ct);
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
                var signal = await ExecuteSwitchCaseWithGotoAsync(node, defaultCaseIndex, ctx, ct);
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

    private static int FindDefaultCaseIndex(IReadOnlyList<BoundSwitchCase> cases)
    {
        for (var i = 0; i < cases.Count; i++)
            if (cases[i].CasePattern == null)
                return i;
        return -1;
    }

    private static async ValueTask<ControlFlowSignal?> ExecuteSwitchCaseWithGotoAsync(
        BoundSwitchStatementExpr switchStatement, int startIndex, EvaluationContext ctx, CancellationToken ct)
    {
        var signal = await ExecuteSwitchCaseStatementsAsync(switchStatement.Cases, startIndex, ctx, ct);
        while (signal is { SignalKind: ControlFlowSignal.Kind.GotoCase or ControlFlowSignal.Kind.GotoDefault })
        {
            int targetIndex;
            if (signal.SignalKind == ControlFlowSignal.Kind.GotoDefault)
            {
                targetIndex = FindDefaultCaseIndex(switchStatement.Cases);
            }
            else
            {
                targetIndex = FindCaseIndex(switchStatement, signal.Value, ctx, ct);
            }
            if (targetIndex < 0)
                throw new AlderException(DiagnosticDescriptors.LabelNotFound, signal.Value?.ToString() ?? "default");
            signal = await ExecuteSwitchCaseStatementsAsync(switchStatement.Cases, targetIndex, ctx, ct);
        }
        return signal;
    }

    private static async ValueTask<ControlFlowSignal?> ExecuteSwitchCaseStatementsAsync(
        IReadOnlyList<BoundSwitchCase> cases, int startIndex, EvaluationContext ctx, CancellationToken ct)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            foreach (var statement in switchCase.Statements)
            {
                ct.ThrowIfCancellationRequested();
                var result = await ctx.EvaluateAsync(statement, ct);
                if (result is ControlFlowSignal signal)
                    return signal;
            }

            throw new AlderException(DiagnosticDescriptors.CaseFallThrough);
        }

        return null;
    }

    private static int FindCaseIndex(BoundSwitchStatementExpr switchStatement, object? targetValue, EvaluationContext ctx, CancellationToken ct)
    {
        for (var i = 0; i < switchStatement.Cases.Length; i++)
        {
            var casePattern = switchStatement.Cases[i].CasePattern;
            if (casePattern is ConstantPattern cp)
            {
                var caseValue = ctx.MatchPattern(targetValue, cp, ct);
                if (TypeHelpers.RequireBoolean(caseValue))
                    return i;
            }
        }
        return -1;
    }
}
