using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ForBinder : INodeBinder<ForStatementExpr>
{
    public BoundExpr Bind(ForStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var loopScope = context.CreateChildScope();
        var initializers = expr.Initializers
            .Select(initializer => binder.Bind(initializer, loopScope))
            .ToImmutableArray();
        var condition = expr.Condition != null
            ? binder.Bind(expr.Condition, loopScope)
            : null;
        var increments = expr.Increments
            .Select(increment => binder.Bind(increment, loopScope))
            .ToImmutableArray();

        var bodyScope = loopScope.CreateChildScope();
        var body = expr.Body
            .Select(statement => binder.Bind(statement, bodyScope))
            .ToImmutableArray();
        return new BoundForExpr(initializers, condition, increments, body, BoundType.Void);
    }
}
