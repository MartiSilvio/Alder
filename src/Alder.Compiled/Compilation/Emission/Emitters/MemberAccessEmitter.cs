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
    public LinqExpression Emit(BoundPropertyAccessExpr node, EmissionContext ctx)
    {
        if (ctx.TryEmitPostfixChain?.Invoke(node) is { } chainResult) return chainResult;

        var declaringType = node.Property.DeclaringType;

        if (declaringType != null && declaringType.IsGenericType &&
            declaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var emittedTarget = ctx.Emit(node.Target);
            if (node.MemberName == nameof(Nullable<int>.HasValue))
                return LinqExpression.Condition(
                    LinqExpression.Equal(emittedTarget, LinqExpression.Constant(null)),
                    LinqExpression.Constant((object)false, typeof(object)),
                    LinqExpression.Constant((object)true, typeof(object)));
            if (node.MemberName == nameof(Nullable<int>.Value))
                return LinqExpression.Condition(
                    LinqExpression.Equal(emittedTarget, LinqExpression.Constant(null)),
                    LinqExpression.Throw(
                        LinqExpression.New(typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                            LinqExpression.Constant("Nullable object must have a value.")),
                        typeof(object)),
                    emittedTarget);
        }

        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

        return EmitResolved(
            node.Property.DeclaringType ?? node.Property.ReflectedType!,
            node.MemberName, node.IsStatic, node.NullSafe,
            ctx.Emit(node.Target),
            target => LinqExpression.Property(target, node.Property),
            node.Property.PropertyType, ctx);
    }

    public LinqExpression Emit(BoundFieldAccessExpr node, EmissionContext ctx)
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

    public LinqExpression Emit(BoundMethodGroupExpr node, EmissionContext ctx) =>
        ctx.TryEmitPostfixChain?.Invoke(node) ?? EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

    public LinqExpression Emit(BoundDynamicMemberAccessExpr node, EmissionContext ctx) =>
        ctx.TryEmitPostfixChain?.Invoke(node) ?? EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

    internal static LinqExpression EmitWithTarget(BoundMemberAccessBase ma, LinqExpression emittedTarget, EmissionContext ctx)
    {
        return ma switch
        {
            BoundPropertyAccessExpr prop => EmitResolvedOrDynamic(prop, emittedTarget, ctx),
            BoundFieldAccessExpr field => EmitResolvedOrDynamic(field, emittedTarget, ctx),
            _ => EmitDynamic(ma.MemberName, ma.NullSafe, emittedTarget, ctx)
        };
    }

    private static LinqExpression EmitResolvedOrDynamic(BoundPropertyAccessExpr node, LinqExpression emittedTarget, EmissionContext ctx)
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

    private static LinqExpression EmitResolvedOrDynamic(BoundFieldAccessExpr node, LinqExpression emittedTarget, EmissionContext ctx)
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

    private static LinqExpression EmitResolved(
        Type targetType, string memberName, bool isStatic, bool nullSafe,
        LinqExpression emittedTarget,
        Func<LinqExpression, LinqExpression> accessFactory,
        Type memberType, EmissionContext ctx)
    {
        var guardContext = EmitHelpers.CreateMemberGuardContext(memberName);

        if (isStatic)
        {
            var access = accessFactory(null!);
            return EmitHelpers.WrapGuardedValue(access, memberType, guardContext);
        }

        var targetObjVar = LinqExpression.Variable(typeof(object), "memberTarget");
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = accessFactory(typedTarget);
        var guardedExpr = EmitHelpers.WrapGuardedValue(accessExpr, memberType, guardContext);

        if (nullSafe)
        {
            var boxedGuarded = EmitHelpers.AsObject(guardedExpr);
            return LinqExpression.Block(
                typeof(object),
                [targetObjVar],
                LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
                LinqExpression.Condition(
                    LinqExpression.Equal(targetObjVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Constant(null, typeof(object)),
                    boxedGuarded));
        }

        return LinqExpression.Block(
            memberType,
            [targetObjVar],
            LinqExpression.Assign(targetObjVar, EmitHelpers.AsObject(emittedTarget)),
            guardedExpr);
    }

    internal static LinqExpression EmitDynamic(string memberName, bool nullSafe, LinqExpression emittedTarget, EmissionContext ctx)
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
