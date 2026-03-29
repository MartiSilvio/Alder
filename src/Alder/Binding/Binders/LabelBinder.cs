using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class LabelBinder : INodeBinder<LabelExpr>
{
    public BoundExpr Bind(LabelExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLabelExpr(expr.Name, BoundType.Void);
    }
}
