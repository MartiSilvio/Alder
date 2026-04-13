using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ReturnExpr))]
internal static class ReturnBinder
{
    public static BoundExpr Bind(ReturnExpr expr, BindingContext context, BinderContext binder)
    {
        // §13.10.5 / CS0157: a return statement cannot transfer control out of a finally block
        if (binder.Includes(BinderFlags.InFinally))
            throw new AlderException(DiagnosticDescriptors.ControlCannotLeaveFinally);

        var value = expr.Value != null ? binder.Bind(expr.Value, context) : null;
        return new BoundReturnExpr(value, value?.StaticType ?? BoundType.Void);
    }
}
