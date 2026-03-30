using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(DoWhileStatementExpr))]
internal static class DoWhileBinder
{
    public static BoundExpr Bind(DoWhileStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var bodyScope = context.CreateChildScope();
        var body = expr.Body
            .Select(statement => binder.Bind(statement, bodyScope))
            .ToImmutableArray();
        var condition = binder.Bind(expr.Condition, context);
        return new BoundDoWhileExpr(body, condition, BoundType.Void);
    }
}
