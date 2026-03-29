using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class IncrementDecrementBinder : INodeBinder<IncrementDecrementExpr>
{
    public BoundExpr Bind(IncrementDecrementExpr expr, BindingContext context, BinderContext binder)
    {
        AssignBinder.EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var staticType = context.TryGetVariableType(expr.Name.Lexeme, out var variableType)
            ? variableType
            : BoundType.Unknown;
        var isIdLocal = context.TryGetLocal(expr.Name.Lexeme, out _, out var idLocalId);
        return new BoundIncrementDecrementExpr(
            expr.Name.Lexeme,
            expr.Op.Type,
            expr.IsPrefix,
            staticType,
            isIdLocal ? idLocalId : null);
    }
}
