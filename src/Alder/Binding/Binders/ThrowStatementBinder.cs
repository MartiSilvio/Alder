using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class ThrowStatementBinder : INodeBinder<ThrowStatementExpr>
{
    public BoundExpr Bind(ThrowStatementExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundThrowExpr(null, BoundType.Void);
    }
}
