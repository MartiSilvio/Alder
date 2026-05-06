using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(BlockExpr))]
internal static class BlockBinder
{
    public static BoundExpr Bind(BlockExpr expr, BindingContext context, BinderContext binder)
    {
        var blockScope = context.CreateChildScope();
        var statements = expr.Statements
            .Select(statement => binder.Bind(statement, blockScope))
            .ToImmutableArray();
        var returnExpr = expr.ReturnExpr != null ? binder.Bind(expr.ReturnExpr, blockScope) : null;
        var staticType = returnExpr?.StaticType ?? BoundType.Unknown;
        return new BoundBlockExpr(statements, returnExpr, staticType);
    }
}
