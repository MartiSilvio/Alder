using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(SwitchExpressionExpr))]
internal static class SwitchExpressionBinder
{
    public static BoundExpr Bind(SwitchExpressionExpr expr, BindingContext context, BinderContext binder)
    {
        var expression = binder.Bind(expr.Expression, context);
        var arms = expr.Arms
            .Select(arm =>
            {
                var armScope = context.CreateChildScope();
                var whenGuard = arm.WhenGuard != null ? binder.Bind(arm.WhenGuard, armScope) : null;
                var value = binder.Bind(arm.Value, armScope);
                return new BoundSwitchExpressionArm(arm.Pattern, whenGuard, value);
            })
            .ToImmutableArray();

        var staticType = typeof(object);
        if (arms.Length > 0)
        {
            staticType = arms[0].Value.StaticType.ClrType;
            for (var i = 1; i < arms.Length; i++)
                staticType = BinaryBinder.GetCommonType(staticType, arms[i].Value.StaticType.ClrType);
        }

        return new BoundSwitchExpressionExpr(expression, arms, new BoundType(staticType));
    }
}
