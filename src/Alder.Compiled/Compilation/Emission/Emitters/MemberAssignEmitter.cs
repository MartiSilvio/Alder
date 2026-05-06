using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.MemberAssignment)]
internal static class MemberAssignEmitter
{
    public static LinqExpression Emit(BoundMemberAssignExpr node, EmissionContext ctx)
    {
        if (node.ResolvedMember is PropertyInfo { CanWrite: true } property
            && !node.Target.StaticType.ClrType.IsValueType)
        {
            return EmitResolved(node, property, property.PropertyType, ctx);
        }

        if (node.ResolvedMember is FieldInfo { IsInitOnly: false } field
            && !node.Target.StaticType.ClrType.IsValueType)
        {
            return EmitResolved(node, field, field.FieldType, ctx);
        }

        return LinqExpression.Call(
            ApplyMemberAssignMethod,
            ctx.EmitBoxed(node.Target),
            LinqExpression.Constant(node.MemberName),
            ctx.EmitBoxed(node.Value),
            ctx.ContextParam);
    }

    private static LinqExpression EmitResolved(BoundMemberAssignExpr node, MemberInfo member, Type valueType, EmissionContext ctx)
    {
        if (ctx.ResolvedDispatchMode == ResolvedDispatchMode.RuntimeDispatch)
        {
            var runtimeCall = LinqExpression.Call(
                SetResolvedMemberMethod,
                LinqExpression.Constant(member, typeof(MemberInfo)),
                ctx.EmitBoxed(node.Target),
                LinqExpression.Constant(node.MemberName),
                ctx.EmitBoxed(node.Value),
                ctx.ContextParam);

            return EmitHelpers.EnsureTypedExpression(runtimeCall, valueType);
        }

        return EmitDirect(member, node, valueType, ctx);
    }

    private static LinqExpression EmitDirect(MemberInfo member, BoundMemberAssignExpr node, Type valueType, EmissionContext ctx)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "maTarget");
        var valueVar = LinqExpression.Variable(valueType, "maValue");
        var targetType = member.DeclaringType ?? node.DeclaringType!;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(node.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var assignTarget = member switch
        {
            PropertyInfo property => (LinqExpression)LinqExpression.Assign(LinqExpression.Property(typedTarget, property), valueVar),
            FieldInfo field => LinqExpression.Assign(LinqExpression.Field(typedTarget, field), valueVar),
            _ => throw new NotSupportedException()
        };

        return LinqExpression.Block(
            valueType,
            [targetObjVar, valueVar],
            LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
            LinqExpression.Assign(valueVar, ctx.EmitAs(node.Value, valueType)),
            assignTarget,
            valueVar);
    }
}
