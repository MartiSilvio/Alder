using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(TypeReferenceExpr))]
internal static class TypeReferenceBinder
{
    public static BoundExpr Bind(TypeReferenceExpr expr, BindingContext context, BinderContext binder)
    {
        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(expr.TypeToken.Lexeme);
        return new BoundLiteralExpr(resolvedType, new BoundType(typeof(Type)));
    }
}
