using System.Linq.Expressions;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class ConditionalEmitter : INodeEmitter<BoundConditionalExpr>
{
    public LinqExpression Emit(BoundConditionalExpr node, EmissionContext ctx)
    {
        var conditionCandidate = ctx.Emit(node.Condition);
        var condition = conditionCandidate.Type == typeof(bool)
            ? conditionCandidate
            : LinqExpression.Call(RequireBooleanMethod, EmitHelpers.AsObject(conditionCandidate));

        var thenCandidate = ctx.Emit(node.ThenBranch);
        var elseCandidate = ctx.Emit(node.ElseBranch);

        if (TryEmitTyped(node, condition, thenCandidate, elseCandidate, out var typed))
            return typed;

        if (thenCandidate.Type == elseCandidate.Type && thenCandidate.Type != typeof(object))
            return EmitHelpers.AsObject(LinqExpression.Condition(condition, thenCandidate, elseCandidate));

        return LinqExpression.Condition(
            condition,
            EmitHelpers.AsObject(thenCandidate),
            EmitHelpers.AsObject(elseCandidate));
    }

    private static bool TryEmitTyped(
        BoundConditionalExpr conditional,
        LinqExpression condition,
        LinqExpression thenCandidate,
        LinqExpression elseCandidate,
        out LinqExpression typed)
    {
        typed = null!;
        var thenType = conditional.ThenBranch.StaticType.ClrType;
        var elseType = conditional.ElseBranch.StaticType.ClrType;
        var resultType = conditional.StaticType.ClrType;

        if (thenType == typeof(object) || elseType == typeof(object) || resultType == typeof(object))
            return false;

        if (!IsConvertSafe(thenType, resultType) || !IsConvertSafe(elseType, resultType))
            return false;

        var thenTyped = EmitHelpers.EnsureTypedExpression(thenCandidate, thenType);
        var elseTyped = EmitHelpers.EnsureTypedExpression(elseCandidate, elseType);

        if (thenTyped.Type != resultType)
            thenTyped = LinqExpression.Convert(thenTyped, resultType);
        if (elseTyped.Type != resultType)
            elseTyped = LinqExpression.Convert(elseTyped, resultType);

        typed = LinqExpression.Condition(condition, thenTyped, elseTyped);
        return true;
    }

    private static bool IsConvertSafe(Type sourceType, Type targetType) =>
        sourceType == targetType
        || targetType.IsAssignableFrom(sourceType)
        || (TypeHelpers.IsArithmetic(sourceType) && TypeHelpers.IsArithmetic(targetType));
}
