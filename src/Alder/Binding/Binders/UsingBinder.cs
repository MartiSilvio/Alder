using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(UsingStatementExpr))]
internal static class UsingBinder
{
    public static BoundExpr Bind(UsingStatementExpr expr, BindingContext context, BinderContext binder)
    {
        var usingScope = context.CreateChildScope();
        var resource = binder.Bind(expr.ResourceDeclaration, usingScope);
        var body = binder.Bind(expr.Body, usingScope);
        return new BoundUsingStatementExpr(resource, body, BoundType.Void);
    }
}
