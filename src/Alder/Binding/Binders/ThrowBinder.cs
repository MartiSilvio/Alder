using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ThrowExpr))]
internal static class ThrowBinder
{
    public static BoundExpr Bind(ThrowExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        // §13.10.6: the throw operand must be null or an expression of a type derived from System.Exception.
        // Roslyn reports CS0029 when the static type is known and not implicitly convertible to System.Exception.
        if (expression.StaticType is not BoundUnknownType and not BoundVoidType)
        {
            var clrType = expression.StaticType.ClrType;
            if (clrType != typeof(object) && !typeof(Exception).IsAssignableFrom(clrType))
                throw new AlderException(DiagnosticDescriptors.NoImplicitConversion, clrType.Name, nameof(Exception));
        }
        return new BoundThrowExpr(expression, BoundType.Void);
    }
}
