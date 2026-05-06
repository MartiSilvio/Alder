using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(AsExpr))]
internal static class AsBinder
{
    public static BoundExpr Bind(AsExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(expr.TargetType.Lexeme);
        return new BoundAsExpr(expression, targetType, new BoundType(targetType));
    }
}
