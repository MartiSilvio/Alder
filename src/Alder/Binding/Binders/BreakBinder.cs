using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(BreakExpr))]
internal static class BreakBinder
{
    public static BoundExpr Bind(BreakExpr expr, BindingContext context, BinderContext binder)
    {
        // §13.10.2: if a break statement is not enclosed by a switch, while, do, for, or foreach statement,
        // a compile-time error occurs
        if (!binder.Includes(BinderFlags.InLoop) && !binder.Includes(BinderFlags.InSwitch))
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return new BoundBreakExpr(BoundType.Void);
    }
}
