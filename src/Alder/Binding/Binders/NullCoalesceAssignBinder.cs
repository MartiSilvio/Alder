using Alder.Binding.BoundNodes;
using Alder.Binding.Services;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(NullCoalesceAssignExpr))]
internal static class NullCoalesceAssignBinder
{
    public static BoundExpr Bind(NullCoalesceAssignExpr expr, BindingContext context, BinderContext binder)
    {
        AssignBinder.EnsureVariableIsAssignable(expr.Name.Lexeme, context);
        var value = binder.Bind(expr.Value, context);
        var target = NamedTargetBindingService.Resolve(expr.Name.Lexeme, context, value.StaticType);
        return new BoundNullCoalesceAssignExpr(expr.Name.Lexeme, value, target.StaticType, target.LocalId);
    }
}
