using Alder.Parsing;

namespace Alder.Runtime;

/// <summary>
/// ECMA-334 §12.10.5-§12.10.6, §12.13.3: Predefined enum operators.
/// E + int → E, int + E → E, E - int → E, E - E → underlying,
/// E & E → E, E | E → E, E ^ E → E, ~E → E.
/// </summary>
internal static class EnumArithmetic
{
    public static object Add(object left, object right)
    {
        var leftIsEnum = left.GetType().IsEnum;
        var rightIsEnum = right.GetType().IsEnum;

        if (leftIsEnum && rightIsEnum)
            throw new AlderException(Diagnostics.DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Plus), left.GetType().Name, right.GetType().Name);

        if (leftIsEnum)
        {
            var enumType = left.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var l = Convert.ChangeType(left, underlyingType);
            var r = Convert.ChangeType(right, underlyingType);
            var result = NumericDispatch.Add(l, r)!;
            return Enum.ToObject(enumType, result);
        }
        else
        {
            var enumType = right.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var l = Convert.ChangeType(left, underlyingType);
            var r = Convert.ChangeType(right, underlyingType);
            var result = NumericDispatch.Add(l, r)!;
            return Enum.ToObject(enumType, result);
        }
    }

    public static object Subtract(object left, object right)
    {
        var leftIsEnum = left.GetType().IsEnum;
        var rightIsEnum = right.GetType().IsEnum;

        if (leftIsEnum && rightIsEnum)
        {
            // E - E → U (underlying type of E), per ECMA-334 §12.10.6
            var underlyingType = Enum.GetUnderlyingType(left.GetType());
            var l = Convert.ChangeType(left, underlyingType);
            var r = Convert.ChangeType(right, underlyingType);
            return NumericDispatch.Subtract(l, r)!;
        }

        if (leftIsEnum)
        {
            // E - int → E
            var enumType = left.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var l = Convert.ChangeType(left, underlyingType);
            var r = Convert.ChangeType(right, underlyingType);
            var result = NumericDispatch.Subtract(l, r)!;
            return Enum.ToObject(enumType, result);
        }

        // int - E is not a predefined operator
        throw new AlderException(Diagnostics.DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(TokenType.Minus), left.GetType().Name, right.GetType().Name);
    }

    public static object BitwiseOp(object left, object right, Func<object, object, object?> op)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        if (leftType != rightType)
            throw new AlderException(Diagnostics.DiagnosticDescriptors.BadBinaryOps,
                TokenLexemes.GetCanonical(TokenType.Pipe), leftType.Name, rightType.Name);

        var underlyingType = Enum.GetUnderlyingType(leftType);
        var l = Convert.ChangeType(left, underlyingType);
        var r = Convert.ChangeType(right, underlyingType);
        var result = op(l, r)!;
        return Enum.ToObject(leftType, result);
    }

    public static object BitwiseNot(object value)
    {
        var enumType = value.GetType();
        var underlyingType = Enum.GetUnderlyingType(enumType);
        var underlying = Convert.ChangeType(value, underlyingType);
        var result = NumericDispatch.BitwiseNot(underlying)!;
        return Enum.ToObject(enumType, result);
    }
}
