using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SwitchStatementExpr))]
internal static class SwitchStatementBinder
{
    public static BoundExpr Bind(SwitchStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var switchBinder = binder.WithAdditionalFlags(BinderFlags.InSwitch);
        var cases = expr.Cases
            .Select(switchCase =>
            {
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
        return new BoundSwitchStatementExpr(expression, cases, BoundType.Void);
    }
}
