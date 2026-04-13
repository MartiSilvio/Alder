using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(AssignExpr))]
internal static class AssignBinder
{
    public static BoundExpr Bind(AssignExpr expr, BindingContext context, BinderContext binder)
    {
        var value = binder.Bind(expr.Value, context);

        // ECMA-334 §9.2.9.1: _ is a discard when not defined as a variable
        if (expr.Name.Lexeme == TokenLexemes.DiscardIdentifier &&
            !context.TryGetLocal(TokenLexemes.DiscardIdentifier, out _, out _) &&
            !context.RuntimeContext.TryGet(TokenLexemes.DiscardIdentifier, out _))
            return value;

        EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var staticType = context.TryGetVariableType(expr.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isAssignLocal = context.TryGetLocal(expr.Name.Lexeme, out _, out var assignLocalId);
        return new BoundAssignExpr(expr.Name.Lexeme, value, staticType, isAssignLocal ? assignLocalId : null);
    }

    internal static void EnsureVariableIsAssignable(string variableName, BindingContext context)
    {
        var reason = context.GetReadOnlyReason(variableName);
        if (reason == ReadOnlyReason.None)
            return;

        // §13.9.5: foreach's iteration variable is readonly; Roslyn reports CS1656 with the
        // specific "kind" text rather than the generic CS0131.
        if (reason == ReadOnlyReason.IterationVariable)
            throw new AlderException(
                DiagnosticDescriptors.ReadOnlyAssignmentToReservedKind,
                variableName, "foreach iteration variable");

        throw new AlderException(DiagnosticDescriptors.AssignmentRequiresVariable);
    }
}
