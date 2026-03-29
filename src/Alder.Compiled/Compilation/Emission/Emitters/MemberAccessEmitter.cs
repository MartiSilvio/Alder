using System.Linq.Expressions;
using System.Reflection;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class MemberAccessEmitter :
    INodeEmitter<BoundPropertyAccessExpr>,
    INodeEmitter<BoundFieldAccessExpr>,
    INodeEmitter<BoundMethodGroupExpr>,
    INodeEmitter<BoundDynamicMemberAccessExpr>
{
    public Expression Emit(BoundPropertyAccessExpr node, EmissionContext ctx)
    {
        if (ctx.TryEmitPostfixChain?.Invoke(node) is { } chainResult) return chainResult;

        var declaringType = node.Property.DeclaringType;
        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

        return EmitResolved(
            node.Property.DeclaringType ?? node.Property.ReflectedType!,
            node.MemberName, node.IsStatic, node.NullSafe,
            ctx.Emit(node.Target),
            target => LinqExpression.Property(target, node.Property),
            node.Property.PropertyType, ctx);
    }

    public Expression Emit(BoundFieldAccessExpr node, EmissionContext ctx)
    {
        if (ctx.TryEmitPostfixChain?.Invoke(node) is { } chainResult) return chainResult;

        var declaringType = node.Field.DeclaringType;
        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

        return EmitResolved(
            node.Field.DeclaringType ?? node.Field.ReflectedType!,
            node.MemberName, node.IsStatic, node.NullSafe,
            ctx.Emit(node.Target),
            target => LinqExpression.Field(target, node.Field),
            node.Field.FieldType, ctx);
    }

    public Expression Emit(BoundMethodGroupExpr node, EmissionContext ctx) =>
        ctx.TryEmitPostfixChain?.Invoke(node) ?? EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

    public Expression Emit(BoundDynamicMemberAccessExpr node, EmissionContext ctx) =>
        ctx.TryEmitPostfixChain?.Invoke(node) ?? EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

    internal static Expression EmitWithTarget(BoundMemberAccessBase ma, Expression emittedTarget, EmissionContext ctx)
    {
        return ma switch
        {
            BoundPropertyAccessExpr prop => EmitResolvedOrDynamic(prop, emittedTarget, ctx),
            BoundFieldAccessExpr field => EmitResolvedOrDynamic(field, emittedTarget, ctx),
            _ => EmitDynamic(ma.MemberName, ma.NullSafe, emittedTarget, ctx)
        };
    }

    private static Expression EmitResolvedOrDynamic(BoundPropertyAccessExpr node, Expression emittedTarget, EmissionContext ctx)
    {
        var declaringType = node.Property.DeclaringType;
        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, emittedTarget, ctx);

        return EmitResolved(
            declaringType ?? node.Property.ReflectedType!,
            node.MemberName, node.IsStatic, node.NullSafe, emittedTarget,
            target => LinqExpression.Property(target, node.Property),
            node.Property.PropertyType, ctx);
    }

    private static Expression EmitResolvedOrDynamic(BoundFieldAccessExpr node, Expression emittedTarget, EmissionContext ctx)
    {
        var declaringType = node.Field.DeclaringType;
        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, emittedTarget, ctx);

        return EmitResolved(
            declaringType ?? node.Field.ReflectedType!,
            node.MemberName, node.IsStatic, node.NullSafe, emittedTarget,
            target => LinqExpression.Field(target, node.Field),
            node.Field.FieldType, ctx);
    }

    private static Expression EmitResolved(
        Type targetType, string memberName, bool isStatic, bool nullSafe,
        Expression emittedTarget,
        Func<Expression, Expression> accessFactory,
        Type memberType, EmissionContext ctx)
    {
        if (isStatic)
        {
            var access = accessFactory(null!);
            var guarded = EmitHelpers.WrapGuardedValue(access, memberType, EmitHelpers.CreateMemberGuardContext(memberName));
            return LinqExpression.Convert(guarded, typeof(object));
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "memberTarget");
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = accessFactory(typedTarget);
        var guardedExpr = LinqExpression.Convert(
            EmitHelpers.WrapGuardedValue(accessExpr, memberType, EmitHelpers.CreateMemberGuardContext(memberName)),
            typeof(object));

        if (nullSafe)
        {
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    guardedExpr));
        }

        return LinqExpression.Block(
            typeof(object),
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
            guardedExpr);
    }

    internal static Expression EmitDynamic(string memberName, bool nullSafe, Expression emittedTarget, EmissionContext ctx)
    {
        return LinqExpression.Call(
            GetMemberMethod,
            EmitHelpers.AsObject(emittedTarget),
            LinqExpression.Constant(memberName),
            ctx.ConfigParam,
            LinqExpression.Constant(nullSafe),
            ctx.ContextParam);
    }
}
