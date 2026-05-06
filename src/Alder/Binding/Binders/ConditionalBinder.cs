using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(ConditionalExpr))]
internal static class ConditionalBinder
{
    public static BoundExpr Bind(ConditionalExpr expr, BindingContext context, BinderContext binder)
    {
        var condition = binder.Bind(expr.Condition, context);
        var thenBranch = binder.Bind(expr.ThenBranch, context);
        var elseBranch = binder.Bind(expr.ElseBranch, context);
        if (condition.HasErrors || thenBranch.HasErrors || elseBranch.HasErrors)
            return new BoundConditionalExpr(condition, thenBranch, elseBranch, BoundType.Unknown) { HasErrors = true };
        return new BoundConditionalExpr(
            condition,
            thenBranch,
            elseBranch,
            new BoundType(BinaryBinder.GetCommonType(thenBranch.StaticType.ClrType, elseBranch.StaticType.ClrType)));
    }
}
