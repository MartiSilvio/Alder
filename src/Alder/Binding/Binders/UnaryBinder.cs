using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(UnaryExpr))]
internal static class UnaryBinder
{
    public static BoundExpr Bind(UnaryExpr expr, BindingContext context, BinderContext binder)
    {
        var operand = binder.Bind(expr.Right, context);
        if (operand.HasErrors)
            return new BoundUnaryExpr(expr.Op.Type, operand, BoundType.Unknown) { HasErrors = true };

        if (expr.Op.Type == TokenType.Minus && operand is BoundLiteralExpr literal)
        {
            if (literal.Value is uint and (uint)int.MaxValue + 1)
                return BoundLiteralExpr.FromValue(int.MinValue);
            if (literal.Value is ulong and (ulong)long.MaxValue + 1)
                return BoundLiteralExpr.FromValue(long.MinValue);
        }

        ValidateOperator(expr.Op.Type, operand.StaticType);

        var resultType = expr.Op.Type == TokenType.Bang
            ? typeof(bool)
            : InferUnaryResultType(operand.StaticType.ClrType, expr.Op.Type);
        return new BoundUnaryExpr(expr.Op.Type, operand, new BoundType(resultType))
        {
            PromotedType = ComputeUnaryPromotedType(expr.Op.Type, operand.StaticType.ClrType)
        };
    }

    // §12.9.3-12.9.6: enforce predefined unary operators. Skip the check when the operand
    // type is dynamic/unknown so the runtime still owns late-bound dispatch.
    private static void ValidateOperator(TokenType op, BoundType operandType)
    {
        if (operandType is BoundUnknownType)
            return;
        var type = operandType.ClrType;
        if (type == typeof(object))
            return;

        var effective = Nullable.GetUnderlyingType(type) ?? type;
        if (IsPredefinedOperand(op, effective))
            return;
        if (HasUserDefinedOperator(effective, op))
            return;

        throw new AlderException(
            DiagnosticDescriptors.BadUnaryOp,
            TokenLexemes.GetCanonical(op),
            type.Name);
    }

    private static bool IsPredefinedOperand(TokenType op, Type type) => op switch
    {
        // §12.9.3 unary +: numeric types
        TokenType.Plus => TypeHelpers.IsArithmetic(type),
        // §12.9.4 unary -: signed numerics (and uint via promotion), excludes ulong
        TokenType.Minus => TypeHelpers.IsArithmetic(type) && type != typeof(ulong),
        // §12.9.5 logical negation: bool only
        TokenType.Bang => type == typeof(bool),
        // §12.9.6 bitwise complement: integral types and enums
        TokenType.Tilde => IsIntegralOrChar(type) || type.IsEnum,
        _ => true
    };

    private static bool HasUserDefinedOperator(Type type, TokenType op)
    {
        var methodName = op switch
        {
            TokenType.Plus => "op_UnaryPlus",
            TokenType.Minus => "op_UnaryNegation",
            TokenType.Bang => "op_LogicalNot",
            TokenType.Tilde => "op_OnesComplement",
            _ => null
        };
        if (methodName == null)
            return false;

        foreach (var m in RuntimeTypeIntrospection.GetMethods(type, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (m.Name == methodName && m.GetParameters().Length == 1)
                return true;
        }
        return false;
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
