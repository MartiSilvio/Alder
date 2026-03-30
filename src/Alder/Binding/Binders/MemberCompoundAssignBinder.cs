using Alder.Binding.BoundNodes;
using Alder.Parsing;

namespace Alder.Binding.Binders;

[BindsNode(typeof(MemberCompoundAssignExpr))]
internal static class MemberCompoundAssignBinder
{
    public static BoundExpr Bind(MemberCompoundAssignExpr expr, BindingContext context, BinderContext binder)
    {
        var target = binder.Bind(expr.Object, context);
        var value = binder.Bind(expr.Value, context);
        var (resolvedMember, _) = MemberAssignBinder.ResolveMemberForAssignment(target.StaticType.ClrType, expr.MemberName, context);
        var memberType = MemberAssignBinder.ExtractMemberType(resolvedMember);
        var staticType = memberType != typeof(object)
            ? BinaryBinder.InferBinaryResultType(expr.Operator, new BoundType(memberType), value.StaticType)
            : typeof(object);
        return new BoundMemberCompoundAssignExpr(
            target,
            expr.MemberName,
            expr.Operator,
            value,
            new BoundType(staticType));
    }
}
