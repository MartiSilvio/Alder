using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class SwitchStatementBinder : INodeBinder<SwitchStatementExpr>
{
    public BoundExpr Bind(SwitchStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var cases = expr.Cases
            .Select(switchCase =>
            {
                var caseScope = context.CreateChildScope();
                var guard = switchCase.WhenGuard != null
                    ? binder.Bind(switchCase.WhenGuard, caseScope)
                    : null;
                var statements = switchCase.Statements
                    .Select(statement => binder.Bind(statement, caseScope))
                    .ToImmutableArray();
                return new BoundSwitchCase(switchCase.CasePattern, guard, statements);
            })
            .ToImmutableArray();
        return new BoundSwitchStatementExpr(expression, cases, BoundType.Void);
    }
}
