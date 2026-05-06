using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SizeofExpr))]
internal static class SizeofBinder
{
    public static BoundExpr Bind(SizeofExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLiteralExpr(TypeHelpers.GetSizeOf(expr.TypeName), new BoundType(typeof(int)));
    }
}
