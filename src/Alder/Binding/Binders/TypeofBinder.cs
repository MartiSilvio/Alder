using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(TypeofExpr))]
internal static class TypeofBinder
{
    public static BoundExpr Bind(TypeofExpr expr, BindingContext context, BinderContext binder)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(expr.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
    }
}
