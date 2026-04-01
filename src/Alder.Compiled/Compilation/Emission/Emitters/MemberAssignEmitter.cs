using Alder.Binding.BoundNodes;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class MemberAssignEmitter : INodeEmitter<BoundMemberAssignExpr>
{
    public LinqExpression Emit(BoundMemberAssignExpr node, EmissionContext ctx)
    {
        if (node.ResolvedMember is PropertyInfo { CanWrite: true } property
            && !node.Target.StaticType.ClrType.IsValueType)
        {
            return EmitDirectProperty(node, property, ctx);
        }

        if (node.ResolvedMember is FieldInfo { IsInitOnly: false } field
            && !node.Target.StaticType.ClrType.IsValueType)
        {
            return EmitDirectField(node, field, ctx);
        }

        return LinqExpression.Call(
            ApplyMemberAssignMethod,
            ctx.EmitBoxed(node.Target),
            LinqExpression.Constant(node.MemberName),
            ctx.EmitBoxed(node.Value),
            ctx.ConfigParam,
            ctx.ContextParam);
    }

    private static LinqExpression EmitDirectProperty(BoundMemberAssignExpr node, PropertyInfo property, EmissionContext ctx)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "maTarget");
        var valueVar = LinqExpression.Variable(property.PropertyType, "maValue");
        var targetType = property.DeclaringType ?? node.DeclaringType!;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(node.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);

        return LinqExpression.Block(
            property.PropertyType,
            [targetObjVar, valueVar],
            LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
            LinqExpression.Assign(valueVar, ctx.EmitAs(node.Value, property.PropertyType)),
            LinqExpression.Assign(LinqExpression.Property(typedTarget, property), valueVar),
            valueVar);
    }

    private static LinqExpression EmitDirectField(BoundMemberAssignExpr node, FieldInfo field, EmissionContext ctx)
    {
        var targetObjVar = LinqExpression.Variable(typeof(object), "maTarget");
        var valueVar = LinqExpression.Variable(field.FieldType, "maValue");
        var targetType = field.DeclaringType ?? node.DeclaringType!;
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod, targetObjVar, LinqExpression.Constant(node.MemberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);

        return LinqExpression.Block(
            field.FieldType,
            [targetObjVar, valueVar],
            LinqExpression.Assign(targetObjVar, ctx.EmitBoxed(node.Target)),
            LinqExpression.Assign(valueVar, ctx.EmitAs(node.Value, field.FieldType)),
            LinqExpression.Assign(LinqExpression.Field(typedTarget, field), valueVar),
            valueVar);
    }
}
