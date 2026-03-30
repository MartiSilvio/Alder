using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(WhileStatementExpr))]
internal static class WhileBinder
{
    public static BoundExpr Bind(WhileStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var condition = binder.Bind(expr.Condition, context);
        var bodyScope = context.CreateChildScope();
        var body = expr.Body
            .Select(statement => binder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundWhileExpr(condition, body, BoundType.Void);
    }
}
