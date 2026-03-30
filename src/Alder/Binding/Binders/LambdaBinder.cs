using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(LambdaExpr))]
internal static class LambdaBinder
{
    public static BoundExpr Bind(LambdaExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLambdaExpr(
            [..expr.Parameters.Select(static parameter => parameter.Name.Lexeme)],
            expr.Body,
            new BoundType(typeof(LambdaValue)));
    }
}
