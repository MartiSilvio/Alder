using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class CompoundAssignBinder : INodeBinder<CompoundAssignExpr>
{
    public BoundExpr Bind(CompoundAssignExpr expr, BindingContext context, BinderContext binder)
    {
        AssignBinder.EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var value = binder.Bind(expr.Value, context);
        var staticType = context.TryGetVariableType(expr.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isCaLocal = context.TryGetLocal(expr.Name.Lexeme, out _, out var caLocalId);
        return new BoundCompoundAssignExpr(expr.Name.Lexeme, expr.Op.Type, value, staticType, isCaLocal ? caLocalId : null);
    }
}
