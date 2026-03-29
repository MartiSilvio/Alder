using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class NameofBinder : INodeBinder<NameofExpr>
{
    public BoundExpr Bind(NameofExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLiteralExpr(expr.Name, new BoundType(typeof(string)));
    }
}
