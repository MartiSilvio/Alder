using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class NamedArgumentBinder : INodeBinder<NamedArgumentExpr>
{
    public BoundExpr Bind(NamedArgumentExpr expr, BindingContext context, BinderContext binder)
    {
        var value = binder.Bind(expr.Value, context);
        return new BoundNamedArgumentExpr(expr.Name.Lexeme, value, BoundType.Unknown);
    }
}
