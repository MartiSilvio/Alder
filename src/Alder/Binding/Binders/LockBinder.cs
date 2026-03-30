using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(LockStatementExpr))]
internal static class LockBinder
{
    public static BoundExpr Bind(LockStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var lockObject = binder.Bind(expr.LockObject, context);
        var body = binder.Bind(expr.Body, context.CreateChildScope());
        return new BoundLockStatementExpr(lockObject, body, BoundType.Void);
    }
}
