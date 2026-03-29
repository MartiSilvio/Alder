using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class AsBinder : INodeBinder<AsExpr>
{
    public BoundExpr Bind(AsExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(expr.TargetType.Lexeme);
        return new BoundAsExpr(expression, targetType, new BoundType(targetType));
    }
}
