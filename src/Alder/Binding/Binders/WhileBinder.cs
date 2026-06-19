using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(WhileStatementExpr))]
internal static class WhileBinder
{
    public static BoundExpr Bind(WhileStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var loopFlags = BinderFlags.InLoop;
        if (binder.Includes(BinderFlags.InFinally))
            loopFlags |= BinderFlags.InFinallyLoop;
        var loopBinder = binder.WithAdditionalFlags(loopFlags);
        var condition = loopBinder.Bind(expr.Condition, context);
        var bodyScope = context.CreateChildScope();
        var body = expr.Body
            .Select(statement => loopBinder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundWhileExpr(condition, body, BoundType.Void);
    }
}
