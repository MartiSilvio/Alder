using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ContinueExpr))]
internal static class ContinueBinder
{
    public static BoundExpr Bind(ContinueExpr expr, BindingContext context, BinderContext binder)
    {
        // §13.10.3 / CS0157: a continue statement cannot transfer control out of a finally block.
        if (binder.Includes(BinderFlags.InFinally)
            && !binder.Includes(BinderFlags.InFinallyLoop))
            throw new AlderException(DiagnosticDescriptors.ControlCannotLeaveFinally);

        // §13.10.3: if a continue statement is not enclosed by a while, do, for, or foreach statement,
        // a compile-time error occurs
        if (!binder.Includes(BinderFlags.InLoop))
            throw new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return new BoundContinueExpr(BoundType.Void);
    }
}
