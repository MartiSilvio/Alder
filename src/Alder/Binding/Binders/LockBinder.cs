using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class LockBinder : INodeBinder<LockStatementExpr>
{
    public BoundExpr Bind(LockStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var lockObject = binder.Bind(expr.LockObject, context);
        var body = binder.Bind(expr.Body, context.CreateChildScope());
        return new BoundLockStatementExpr(lockObject, body, BoundType.Void);
    }
}
