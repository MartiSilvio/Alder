using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ContinueExpr))]
internal static class ContinueBinder
{
    public static BoundExpr Bind(ContinueExpr expr, BindingContext context, BinderContext binder)
    {
        // §13.10.3: if a continue statement is not enclosed by a while, do, for, or foreach statement,
        // a compile-time error occurs
        if (!binder.Includes(BinderFlags.InLoop))
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return new BoundContinueExpr(BoundType.Void);
    }
}
