using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ThrowStatementExpr))]
internal static class ThrowStatementBinder
{
    public static BoundExpr Bind(ThrowStatementExpr expr, BindingContext context, BinderContext binder)
    {
        // §13.10.6: a rethrow (`throw;`) is only valid inside a catch clause.
        if (!binder.Includes(BinderFlags.InCatch))
            throw new AlderException(DiagnosticDescriptors.ThrowOutsideCatch);
        return new BoundThrowExpr(null, BoundType.Void);
    }
}
