using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

internal sealed class NullCoalesceAssignBinder : INodeBinder<NullCoalesceAssignExpr>
{
    public BoundExpr Bind(NullCoalesceAssignExpr expr, BindingContext context, BinderContext binder)
    {
        AssignBinder.EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var value = binder.Bind(expr.Value, context);
        var staticType = context.TryGetVariableType(expr.Name.Lexeme, out var variableType)
            ? variableType
            : value.StaticType;
        var isNcaLocal = context.TryGetLocal(expr.Name.Lexeme, out _, out var ncaLocalId);
        return new BoundNullCoalesceAssignExpr(expr.Name.Lexeme, value, staticType, isNcaLocal ? ncaLocalId : null);
    }
}
