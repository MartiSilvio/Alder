using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

internal sealed class DefaultBinder : INodeBinder<DefaultExpr>
{
    public BoundExpr Bind(DefaultExpr expr, BindingContext context, BinderContext binder)
    {
        if (expr.TypeToken == null)
            return new BoundLiteralExpr(null, BoundType.Unknown);

        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(expr.TypeToken.Value.Lexeme);
        var value = TypeHelpers.GetDefaultValue(resolvedType);
        return new BoundLiteralExpr(value, new BoundType(resolvedType));
    }
}
