using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class CastBinder : INodeBinder<CastExpr>
{
    public BoundExpr Bind(CastExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var targetType = context.RuntimeContext.TypeResolver.ResolveType(expr.TargetType.Lexeme);
        var sourceStaticType = expr.Expression is IdentifierExpr && expression.StaticType is not BoundUnknownType
            ? expression.StaticType.ClrType
            : (Type?)null;
        return new BoundCastExpr(expression, targetType, sourceStaticType, new BoundType(targetType));
    }
}
