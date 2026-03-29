using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

internal sealed class SizeofBinder : INodeBinder<SizeofExpr>
{
    public BoundExpr Bind(SizeofExpr expr, BindingContext context, BinderContext binder)
    {
        return new BoundLiteralExpr(TypeHelpers.GetSizeOf(expr.TypeName), new BoundType(typeof(int)));
    }
}
