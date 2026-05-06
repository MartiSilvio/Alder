using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SwitchStatementExpr))]
internal static class SwitchStatementBinder
{
    public static BoundExpr Bind(SwitchStatementExpr expr, BindingContext context, BinderContext binder)
    {
        ValidateCaseLabels(expr);

        var expression = binder.Bind(expr.Expression, context);
        var switchBinder = binder.WithAdditionalFlags(BinderFlags.InSwitch);
        var cases = expr.Cases
            .Select(switchCase =>
            {
                if (switchCase.CasePattern != null)
                    PatternValidator.Validate(switchCase.CasePattern, expression.StaticType, context);
                var caseScope = context.CreateChildScope();
                var guard = switchCase.WhenGuard != null
                    ? switchBinder.Bind(switchCase.WhenGuard, caseScope)
                    : null;
                var statements = switchCase.Statements
                    .Select(statement => switchBinder.Bind(statement, caseScope))
                    .ToImmutableArray();
                return new BoundSwitchCase(switchCase.CasePattern, guard, statements);
            })
            .ToImmutableArray();

        // §13.8.3 (CS0163): every non-empty case body must terminate — control cannot fall through
        // from one case label to another. An empty case (`case 1:` with no body) is allowed and
        // falls through to the next case per C# semantics.
        foreach (var switchCase in cases)
        {
            if (switchCase.Statements.IsDefaultOrEmpty) continue;
            var last = switchCase.Statements[^1];
            if (TerminatesControlFlow(last)) continue;
            throw new AlderException(DiagnosticDescriptors.CaseFallThrough, last.Span, null, null);
        }

        return new BoundSwitchStatementExpr(expression, cases, BoundType.Void);
    }

    private static bool TerminatesControlFlow(BoundExpr expr) => expr.Kind switch
    {
        BoundNodeKind.BreakStatement => true,
        BoundNodeKind.ReturnStatement => true,
        BoundNodeKind.ContinueStatement => true,
        BoundNodeKind.GotoStatement => true,
        BoundNodeKind.GotoCaseStatement => true,
        BoundNodeKind.GotoDefaultStatement => true,
        BoundNodeKind.ThrowExpression => true,
        BoundNodeKind.YieldBreakStatement => true,
        BoundNodeKind.Block when ((BoundBlockExpr)expr).Statements.Length > 0 =>
            TerminatesControlFlow(((BoundBlockExpr)expr).Statements[^1]),
        _ => false
    };

    // §13.8.3: default label may appear only once; constant-pattern case labels must be distinct constants.
    private static void ValidateCaseLabels(SwitchStatementExpr expr)
    {
        var defaultSeen = false;
        var seenConstants = new HashSet<object?>();
        foreach (var switchCase in expr.Cases)
        {
            if (switchCase.CasePattern is null)
            {
                if (defaultSeen)
                    throw new AlderException(DiagnosticDescriptors.DuplicateCaseLabel, "default");
                defaultSeen = true;
                continue;
            }

            // Only bare `case X:` with a when-less constant pattern is subject to the duplicate-label check.
            if (switchCase.WhenGuard != null)
                continue;

            if (switchCase.CasePattern is ConstantPattern { Value: LiteralExpr { Value: var literalValue } })
            {
                if (!seenConstants.Add(literalValue))
                    throw new AlderException(
                        DiagnosticDescriptors.DuplicateCaseLabel, literalValue?.ToString() ?? "null");
            }
        }
    }
}
