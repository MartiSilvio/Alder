using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ForStatementExpr))]
internal static class ForBinder
{
    public static BoundExpr Bind(ForStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var loopScope = context.CreateChildScope();
        var loopFlags = BinderFlags.InLoop;
        if (binder.Includes(BinderFlags.InFinally))
            loopFlags |= BinderFlags.InFinallyLoop;
        var loopBinder = binder.WithAdditionalFlags(loopFlags);
        var initializers = expr.Initializers
            .Select(initializer => loopBinder.Bind(initializer, loopScope))
            .ToImmutableArray();
        var condition = expr.Condition != null
            ? loopBinder.Bind(expr.Condition, loopScope)
            : null;
        var increments = expr.Increments
            .Select(increment => loopBinder.Bind(increment, loopScope))
            .ToImmutableArray();

        var bodyScope = loopScope.CreateChildScope();
        var body = expr.Body
            .Select(statement => loopBinder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForExpr(initializers, condition, increments, body, BoundType.Void);
    }
}
