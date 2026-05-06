using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(CompoundAssignExpr))]
internal static class CompoundAssignBinder
{
    public static BoundExpr Bind(CompoundAssignExpr expr, BindingContext context, BinderContext binder)
    {
        AssignBinder.EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var value = binder.Bind(expr.Value, context);
        var target = NamedTargetBindingService.Resolve(expr.Name.Lexeme, context, value.StaticType);
        return new BoundCompoundAssignExpr(expr.Name.Lexeme, expr.Op.Type, value, target.StaticType, target.LocalId);
    }
}
