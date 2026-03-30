using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(PipelineExpr))]
internal static class PipelineBinder
{
    public static BoundExpr Bind(PipelineExpr expr, BindingContext context, BinderContext binder)
    {
        if (expr.Right is IdentifierExpr rightIdentifier)
        {
            var call = new CallExpr(rightIdentifier, [expr.Left]);
            return binder.Bind(call, context);
        }

        if (expr.Right is CallExpr rightCall)
        {
            var args = new List<Expr>(rightCall.Arguments.Count + 1) { expr.Left };
            args.AddRange(rightCall.Arguments);
            var call = new CallExpr(rightCall.Callee, args, rightCall.TypeArguments);
            return binder.Bind(call, context);
        }

        return new BoundPipelineExpr(
            binder.Bind(expr.Left, context),
            binder.Bind(expr.Right, context),
            BoundType.Unknown);
    }
}
