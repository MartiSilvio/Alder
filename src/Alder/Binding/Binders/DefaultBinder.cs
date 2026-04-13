using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(DefaultExpr))]
internal static class DefaultBinder
{
    public static BoundExpr Bind(DefaultExpr expr, BindingContext context, BinderContext binder)
    {
        // §12.8.20: a bare `default` literal (without a target type) is represented as a null
        // literal of unknown type. Surrounding binders (VariableDeclBinder, assignment, calls)
        // either provide a target type or reject the bare form with CS8716.
        if (expr.TypeToken == null)
            return new BoundLiteralExpr(null, BoundType.Unknown);

        var resolvedType = context.RuntimeContext.TypeResolver.ResolveType(expr.TypeToken.Value.Lexeme);
        var value = TypeHelpers.GetDefaultValue(resolvedType);
        return new BoundLiteralExpr(value, new BoundType(resolvedType));
    }
}
