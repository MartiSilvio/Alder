using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ThrowStatementExpr))]
internal static class ThrowStatementBinder
{
    public static BoundExpr Bind(ThrowStatementExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundThrowExpr(null, BoundType.Void);
    }
}
