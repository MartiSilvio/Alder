using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class IfBinder : INodeBinder<IfStatementExpr>
{
    public BoundExpr Bind(IfStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var condition = binder.Bind(expr.Condition, context);
        var thenScope = context.CreateChildScope();
        var thenStatements = expr.ThenStatements
            .Select(statement => binder.Bind(statement, thenScope))
            .ToImmutableArray();

        ImmutableArray<BoundExpr> elseStatements = ImmutableArray<BoundExpr>.Empty;
        if (expr.ElseStatements is { Count: > 0 } elseSource)
        {
            var elseScope = context.CreateChildScope();
            elseStatements = [
                ..elseSource
                    .Select(statement => binder.Bind(statement, elseScope))
            ];
        }

        return new BoundIfStatementExpr(condition, thenStatements, elseStatements, BoundType.Void);
    }
}
