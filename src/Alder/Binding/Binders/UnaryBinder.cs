using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

internal sealed class UnaryBinder : INodeBinder<UnaryExpr>
{
    public BoundExpr Bind(UnaryExpr expr, BindingContext context, BinderContext binder)
    {
        var operand = binder.Bind(expr.Right, context);
        if (operand.HasErrors)
            return new BoundUnaryExpr(expr.Op.Type, operand, BoundType.Unknown) { HasErrors = true };

        if (expr.Op.Type == TokenType.Minus && operand is BoundLiteralExpr literal)
        {
            if (literal.Value is uint u && u == (uint)int.MaxValue + 1)
                return BoundLiteralExpr.FromValue(int.MinValue);
            if (literal.Value is ulong ul && ul == (ulong)long.MaxValue + 1)
                return BoundLiteralExpr.FromValue(long.MinValue);
        }

        var resultType = expr.Op.Type == TokenType.Bang
            ? typeof(bool)
            : InferUnaryResultType(operand.StaticType.ClrType, expr.Op.Type);
        return new BoundUnaryExpr(expr.Op.Type, operand, new BoundType(resultType))
        {
            PromotedType = ComputeUnaryPromotedType(expr.Op.Type, operand.StaticType.ClrType)
        };
    }

    private static Type? ComputeUnaryPromotedType(TokenType op, Type operandType)
    {
        if (operandType == typeof(object) || operandType.IsEnum)
            return null;

        return op switch
        {
            TokenType.Bang when operandType == typeof(bool) => typeof(bool),
            TokenType.Tilde when IsIntegralOrChar(operandType) => BinaryBinder.NormalizeArithmeticType(operandType),
            TokenType.Minus when TypeHelpers.IsArithmetic(operandType) =>
                operandType == typeof(uint) ? typeof(long) : BinaryBinder.NormalizeArithmeticType(operandType),
            TokenType.Plus when TypeHelpers.IsArithmetic(operandType) => BinaryBinder.NormalizeArithmeticType(operandType),
            _ => null
        };
    }

    internal static bool IsIntegralOrChar(Type type) =>
        Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.UInt64 or TypeCode.Char;

    /// <summary>
    /// ECMA-334 §12.4.7.2: sbyte/byte/short/ushort/char → int, unary - on uint → long.
    /// </summary>
    private static Type InferUnaryResultType(Type operandType, TokenType op)
    {
        var promoted = BinaryBinder.NormalizeArithmeticType(operandType);
        if (op == TokenType.Minus && promoted == typeof(uint))
            return typeof(long);
        return promoted;
    }
}
