using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(LabelExpr))]
internal static class LabelBinder
{
    public static BoundExpr Bind(LabelExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLabelExpr(expr, BoundType.Void);
    }
}
