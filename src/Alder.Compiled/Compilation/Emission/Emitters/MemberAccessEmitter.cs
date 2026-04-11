using System.Reflection;
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
            return EmitNullablePropertyAccess(node, ctx.Emit(node.Target));
        }

        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

        return EmitResolved(
            node.Property,
            node.MemberName, node.IsStatic, node.NullSafe,
            ctx.Emit(node.Target),
            node.Property.PropertyType, ctx);
    }

    public LinqExpression Emit(BoundFieldAccessExpr node, EmissionContext ctx)
    {
        if (ctx.TryEmitPostfixChain?.Invoke(node) is { } chainResult) return chainResult;

        var declaringType = node.Field.DeclaringType;
        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, ctx.Emit(node.Target), ctx);

        return EmitResolved(
            node.Field,
            node.MemberName, node.IsStatic, node.NullSafe,
            ctx.Emit(node.Target),
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

        if (declaringType != null && declaringType.IsGenericType &&
            declaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
            return EmitNullablePropertyAccess(node, emittedTarget);

        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, emittedTarget, ctx);

        return EmitResolved(
            node.Property,
            node.MemberName, node.IsStatic, node.NullSafe, emittedTarget,
            node.Property.PropertyType, ctx);
    }

    private static LinqExpression EmitResolvedOrDynamic(BoundFieldAccessExpr node, LinqExpression emittedTarget, EmissionContext ctx)
    {
        var declaringType = node.Field.DeclaringType;
        if (declaringType != null && TypeHelpers.IsValueTupleType(declaringType))
            return EmitDynamic(node.MemberName, node.NullSafe, emittedTarget, ctx);

        return EmitResolved(
            node.Field,
            node.MemberName, node.IsStatic, node.NullSafe, emittedTarget,
            node.Field.FieldType, ctx);
    }

    private static LinqExpression EmitResolved(
        MemberInfo member, string memberName, bool isStatic, bool nullSafe,
        LinqExpression emittedTarget,
        Type memberType, EmissionContext ctx)
    {
        if (ctx.PreferResolvedRuntimeDispatch)
        {
            var runtimeCall = LinqExpression.Call(
                GetResolvedMemberMethod,
                LinqExpression.Constant(member, typeof(MemberInfo)),
                isStatic
                    ? LinqExpression.Constant(null, typeof(object))
                    : EmitHelpers.AsObject(emittedTarget),
                LinqExpression.Constant(memberName),
                LinqExpression.Constant(nullSafe),
                ctx.ContextParam);

            return nullSafe
                ? runtimeCall
                : EmitHelpers.EnsureTypedExpression(runtimeCall, memberType);
        }

        return EmitDirectResolved(member, memberName, isStatic, nullSafe, emittedTarget, memberType);
    }

    private static LinqExpression EmitDirectResolved(
        MemberInfo member,
        string memberName,
        bool isStatic,
        bool nullSafe,
        LinqExpression emittedTarget,
        Type memberType)
    {
        var guardContext = EmitHelpers.CreateMemberGuardContext(memberName);

        if (isStatic)
        {
            var access = member switch
            {
                PropertyInfo property => LinqExpression.Property(null, property),
                FieldInfo field => LinqExpression.Field(null, field),
                _ => throw new NotSupportedException()
            };

            return EmitHelpers.WrapGuardedValue(access, memberType, guardContext);
        }

        var targetType = member.DeclaringType ?? member.ReflectedType ?? throw new InvalidOperationException("Resolved member has no target type.");
        var targetObjVar = LinqExpression.Variable(typeof(object), "memberTarget");
        var checkedTarget = LinqExpression.Call(
            EnsureMemberTargetNotNullMethod,
            targetObjVar,
            LinqExpression.Constant(memberName));
        var typedTarget = EmitHelpers.EnsureTypedExpression(checkedTarget, targetType);
        var accessExpr = member switch
        {
            PropertyInfo property => LinqExpression.Property(typedTarget, property),
            FieldInfo field => LinqExpression.Field(typedTarget, field),
            _ => throw new NotSupportedException()
        };
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

    private static LinqExpression EmitNullablePropertyAccess(BoundPropertyAccessExpr node, LinqExpression emittedTarget)
    {
        // For boxed targets (object), unbox.any handles null → default(Nullable<T>)
        // per CLR spec. Then HasValue returns false, Value throws InvalidOperationException.
        if (emittedTarget.Type == typeof(object))
            emittedTarget = LinqExpression.Unbox(emittedTarget, node.Property.DeclaringType!);

        return LinqExpression.Property(emittedTarget, node.Property);
    }

    internal static LinqExpression EmitDynamic(string memberName, bool nullSafe, LinqExpression emittedTarget, EmissionContext ctx)
    {
        return LinqExpression.Call(
            GetMemberMethod,
            EmitHelpers.AsObject(emittedTarget),
            LinqExpression.Constant(memberName),
            LinqExpression.Constant(nullSafe),
            ctx.ContextParam);
    }
}
