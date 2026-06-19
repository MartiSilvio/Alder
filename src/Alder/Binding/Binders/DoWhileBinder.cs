using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(DoWhileStatementExpr))]
internal static class DoWhileBinder
{
    public static BoundExpr Bind(DoWhileStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var loopFlags = BinderFlags.InLoop;
        if (binder.Includes(BinderFlags.InFinally))
            loopFlags |= BinderFlags.InFinallyLoop;
        var loopBinder = binder.WithAdditionalFlags(loopFlags);
        var bodyScope = context.CreateChildScope();
        var body = expr.Body
            .Select(statement => loopBinder.Bind(statement, bodyScope))
            .ToImmutableArray();
        var condition = loopBinder.Bind(expr.Condition, context);
        return new BoundDoWhileExpr(body, condition, BoundType.Void);
    }
}
