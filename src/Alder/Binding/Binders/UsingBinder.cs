using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(UsingStatementExpr))]
internal static class UsingBinder
{
    public static BoundExpr Bind(UsingStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var resource = binder.Bind(expr.ResourceDeclaration, context);
        var body = binder.Bind(expr.Body, context.CreateChildScope());
        return new BoundUsingStatementExpr(resource, body, BoundType.Void);
    }
}
