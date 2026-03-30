using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MemberIncrementExpr))]
internal static class MemberIncrementBinder
{
    public static BoundExpr Bind(MemberIncrementExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var (resolvedMember, _) = MemberAssignBinder.ResolveMemberForAssignment(target.StaticType.ClrType, expr.MemberName, context);
        var memberType = MemberAssignBinder.ExtractMemberType(resolvedMember);
        return new BoundMemberIncrementExpr(
            target,
            expr.MemberName,
            expr.IsPrefix,
            expr.IsIncrement,
            new BoundType(memberType));
    }
}
