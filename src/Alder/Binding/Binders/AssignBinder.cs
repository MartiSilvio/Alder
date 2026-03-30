using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(AssignExpr))]
internal static class AssignBinder
{
    public static BoundExpr Bind(AssignExpr expr, BindingContext context, BinderContext binder)
    {
        EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var value = binder.Bind(expr.Value, context);
        var staticType = context.TryGetVariableType(expr.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isAssignLocal = context.TryGetLocal(expr.Name.Lexeme, out _, out var assignLocalId);
        return new BoundAssignExpr(expr.Name.Lexeme, value, staticType, isAssignLocal ? assignLocalId : null);
    }

    internal static void EnsureVariableIsAssignable(string variableName, BindingContext context)
    {
        if (context.IsReadOnlyLocal(variableName))
            throw new AlderException(DiagnosticDescriptors.AssignmentRequiresVariable);
    }
}
