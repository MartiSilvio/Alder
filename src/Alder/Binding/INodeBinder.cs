using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding;

internal interface INodeBinder<in TExpr> where TExpr : Expr
{
    BoundExpr Bind(TExpr expr, BindingContext context, BinderContext binder);
}
