using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Parsing;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.CompoundAssignmentOperator)]
internal static class CompoundAssignEmitter
{
    public static LinqExpression Emit(BoundCompoundAssignExpr node, EmissionContext ctx)
    {
        if (ctx.TryGetPromoted(node.LocalId, out var promoted))
        {
            if (TryEmitPure(node, promoted, ctx, out var pureResult))
                return pureResult;

            var resultVar = LinqExpression.Variable(typeof(object), "compoundResult");
            return LinqExpression.Block(
                promoted.Variable.Type,
                [resultVar],
                LinqExpression.Assign(
                    resultVar,
                    LinqExpression.Call(
                        ApplyCompoundAssignLocalMethod,
                        LinqExpression.Constant(node.Name),
                        EmitHelpers.AsObject(promoted.Variable),
                        LinqExpression.Constant(node.Operator),
                        ctx.EmitBoxed(node.Value),
                        LinqExpression.Constant(promoted.VariableType, typeof(Type)),
                        ctx.ContextParam,
                        LinqExpression.Constant(ctx.IsChecked))),
                LinqExpression.Assign(promoted.Variable,
                    EmitHelpers.EnsureTypedExpression(resultVar, promoted.Variable.Type)),
                promoted.Variable);
        }

        return LinqExpression.Call(
            ApplyCompoundAssignMethod,
            LinqExpression.Constant(node.Name),
            LinqExpression.Constant(node.Operator),
            ctx.EmitBoxed(node.Value),
            ctx.ContextParam,
            LinqExpression.Constant(ctx.IsChecked));
    }

    private static bool TryEmitPure(
        BoundCompoundAssignExpr node,
        PromotedLocal promoted,
        EmissionContext ctx,
        out LinqExpression result)
    {
        result = null!;
        if (ctx.IsChecked || promoted.VariableType == typeof(object) || promoted.VariableType.IsEnum)
            return false;

        var rhsType = node.Value.StaticType.ClrType;
        if (rhsType == typeof(object))
            return false;

        var binaryFactory = GetCompoundBinaryFactory(node.Operator, promoted.VariableType, rhsType);
        if (binaryFactory == null)
            return false;

        var typedRhs = LinqExpression.Variable(rhsType, "cmpRhs");

        LinqExpression rhsOperand = typedRhs;
        if (rhsType != promoted.VariableType)
        {
            if (!IsConvertSafe(rhsType, promoted.VariableType))
                return false;
            rhsOperand = LinqExpression.Convert(typedRhs, promoted.VariableType);
        }

        var binaryResult = binaryFactory(promoted.Variable, rhsOperand);
        if (binaryResult.Type != promoted.VariableType)
        {
            if (!IsConvertSafe(binaryResult.Type, promoted.VariableType))
                return false;
            binaryResult = LinqExpression.Convert(binaryResult, promoted.VariableType);
        }

        result = LinqExpression.Block(
            promoted.Variable.Type,
            [typedRhs],
            LinqExpression.Assign(typedRhs, ctx.EmitAs(node.Value, rhsType)),
            LinqExpression.Assign(promoted.Variable, binaryResult),
            promoted.Variable);
        return true;
    }

    private static Func<LinqExpression, LinqExpression, LinqExpression>? GetCompoundBinaryFactory(
        TokenType compoundOp, Type leftType, Type rightType)
    {
        return compoundOp switch
        {
            TokenType.PlusEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Add,
            TokenType.MinusEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Subtract,
            TokenType.StarEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Multiply,
            TokenType.SlashEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Divide,
            TokenType.PercentEqual when IsAddSubtractSafeType(leftType) && IsAddSubtractSafeType(rightType)
                => LinqExpression.Modulo,
            TokenType.AmpEqual when IsIntegralSafeType(leftType) && IsIntegralSafeType(rightType)
                => LinqExpression.And,
            TokenType.PipeEqual when IsIntegralSafeType(leftType) && IsIntegralSafeType(rightType)
                => LinqExpression.Or,
            TokenType.CaretEqual when IsIntegralSafeType(leftType) && IsIntegralSafeType(rightType)
                => LinqExpression.ExclusiveOr,
            TokenType.LessLessEqual when IsIntegralSafeType(leftType) && rightType == typeof(int)
                => LinqExpression.LeftShift,
            TokenType.GreaterGreaterEqual when IsIntegralSafeType(leftType) && rightType == typeof(int)
                => LinqExpression.RightShift,
            _ => null
        };
    }

    internal static bool IsConvertSafe(Type sourceType, Type targetType) =>
        sourceType == targetType
        || targetType.IsAssignableFrom(sourceType)
        || (IsArithmeticFastPathType(sourceType) && IsArithmeticFastPathType(targetType));

    private static bool IsIntegralSafeType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(uint) || t == typeof(ulong);

    private static bool IsArithmeticFastPathType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float)
        || t == typeof(decimal) || t == typeof(uint) || t == typeof(ulong)
        || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);

    internal static bool IsAddSubtractSafeType(Type t) =>
        t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float)
        || t == typeof(decimal) || t == typeof(uint) || t == typeof(ulong);
}
