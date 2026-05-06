using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MemberNullCoalesceAssignExpr))]
internal static class MemberNullCoalesceAssignBinder
{
    public static BoundExpr Bind(MemberNullCoalesceAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var value = binder.Bind(expr.Value, context);
        var (resolvedMember, _) = MemberAssignBinder.ResolveMemberForAssignment(target.StaticType.ClrType, expr.MemberName, context);
        var memberType = MemberAssignBinder.ExtractMemberType(resolvedMember);
        return new BoundMemberNullCoalesceAssignExpr(target, expr.MemberName, value, new BoundType(memberType));
    }
}
