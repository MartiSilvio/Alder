using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Binding.Binders;

[BindsNode(typeof(BinaryExpr))]
internal static class BinaryBinder
{
    public static BoundExpr Bind(BinaryExpr expr, BindingContext context, BinderContext binder)
    {
        if (expr.Left is not BinaryExpr)
            return BindSingle(expr, binder.Bind(expr.Left, context), binder.Bind(expr.Right, context));

        var chain = new List<BinaryExpr>();
        Expr leftmost = expr;
        while (leftmost is BinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var result = binder.Bind(leftmost, context);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var link = chain[i];
            result = BindSingle(link, result, binder.Bind(link.Right, context));
        }

        return result;
    }

    private static BoundExpr BindSingle(BinaryExpr link, BoundExpr left, BoundExpr right)
    {
        if (left.HasErrors || right.HasErrors)
            return new BoundBinaryExpr(link.Op.Type, left, right, BoundType.Unknown) { Span = link.Span, HasErrors = true };

        ValidateOperator(link.Op.Type, left.StaticType, right.StaticType);

        var resultClrType = InferBinaryResultType(link.Op.Type, left.StaticType, right.StaticType);
        var resultType = resultClrType == typeof(object) ? BoundType.Unknown : new BoundType(resultClrType);
        return new BoundBinaryExpr(link.Op.Type, left, right, resultType)
        {
            Span = link.Span,
            PromotedType = ComputeBinaryPromotedType(link.Op.Type, left, right)
        };
    }

    // §12.10-§12.12: reject obviously-invalid predefined operator applications at bind time.
    // Only fires when BOTH operands are well-understood primitive scalars (arith, bool, string,
    // char, enum, delegate). Non-scalar operand types (tuples, user classes, dictionaries,
    // dynamic objects) dispatch at runtime — the binder cannot know which extension or user
    // operator will be chosen, so it leaves those paths alone.
    private static void ValidateOperator(TokenType op, BoundType leftBound, BoundType rightBound)
    {
        if (leftBound is BoundUnknownType || rightBound is BoundUnknownType)
            return;

        var leftType = leftBound.ClrType;
        var rightType = rightBound.ClrType;
        if (leftType == typeof(object) || rightType == typeof(object))
            return;

        var leftEff = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var rightEff = Nullable.GetUnderlyingType(rightType) ?? rightType;

        if (!IsBindTimeValidatable(leftEff) || !IsBindTimeValidatable(rightEff))
            return;

        if (IsPredefinedBinary(op, leftEff, rightEff))
            return;
        if (HasUserDefinedBinaryOperator(op, leftEff, rightEff))
            return;

        throw new AlderException(
            DiagnosticDescriptors.BadBinaryOps,
            TokenLexemes.GetCanonical(op),
            leftType.Name,
            rightType.Name);
    }

    private static bool IsBindTimeValidatable(Type type) =>
        TypeHelpers.IsArithmetic(type) ||
        type == typeof(bool) ||
        type == typeof(string) ||
        type == typeof(char) ||
        type.IsEnum ||
        typeof(Delegate).IsAssignableFrom(type);

    private static bool IsPredefinedBinary(TokenType op, Type l, Type r)
    {
        // Both sides must be known/concrete by this point.
        var lArith = TypeHelpers.IsArithmetic(l);
        var rArith = TypeHelpers.IsArithmetic(r);
        var lString = l == typeof(string);
        var rString = r == typeof(string);
        var lBool = l == typeof(bool);
        var rBool = r == typeof(bool);
        var lEnum = l.IsEnum;
        var rEnum = r.IsEnum;
        var lIntegral = UnaryBinder.IsIntegralOrChar(l);
        var rIntegral = UnaryBinder.IsIntegralOrChar(r);

        switch (op)
        {
            // §12.10 arithmetic
            case TokenType.Plus:
                // string concat: string + any or any + string; otherwise numeric; enum ± integral
                if (lString || rString) return true;
                if (lArith && rArith) return true;
                if (lEnum && rIntegral) return true;
                if (lIntegral && rEnum) return true;
                if (typeof(Delegate).IsAssignableFrom(l) && typeof(Delegate).IsAssignableFrom(r)) return true;
                return false;

            case TokenType.Minus:
                if (lArith && rArith) return true;
                if (lEnum && rEnum && l == r) return true;
                if (lEnum && rIntegral) return true;
                if (typeof(Delegate).IsAssignableFrom(l) && typeof(Delegate).IsAssignableFrom(r)) return true;
                return false;

            case TokenType.Star:
            case TokenType.Slash:
            case TokenType.Percent:
            case TokenType.StarStar:
                return lArith && rArith;

            // §12.11 shift: left integral, right must implicitly convert to int (sbyte/byte/short/ushort/int/char).
            // Wider integrals (uint/long/ulong) on the right fail overload resolution.
            case TokenType.LessLess:
            case TokenType.GreaterGreater:
            case TokenType.GreaterGreaterGreater:
                return lIntegral && rIntegral && IsShiftCountType(r);

            // §12.12 relational — numeric, same-type enums, or pointers. NOT strings.
            case TokenType.Less:
            case TokenType.LessEqual:
            case TokenType.Greater:
            case TokenType.GreaterEqual:
                if (lArith && rArith) return true;
                if (lEnum && rEnum && l == r) return true;
                return false;

            // Alder strict equality: always valid — returns false on any type mismatch.
            case TokenType.EqualEqualEqual:
            case TokenType.BangEqualEqual:
                return true;

            // §12.12 equality — allow broad set: any ref types, numerics, enums, bools, strings, delegates
            case TokenType.EqualEqual:
            case TokenType.BangEqual:
                if (!l.IsValueType || !r.IsValueType) return true;
                if (lArith && rArith) return true;
                if (lBool && rBool) return true;
                if (lEnum && rEnum && l == r) return true;
                if (l == r) return true;
                return false;

            // §12.13 logical
            case TokenType.Amp:
            case TokenType.Pipe:
            case TokenType.Caret:
                if (lBool && rBool) return true;
                if (lIntegral && rIntegral) return true;
                if (lEnum && rEnum && l == r) return true;
                return false;

            case TokenType.AmpAmp:
            case TokenType.PipePipe:
                return lBool && rBool;

            // Extended-mode and Alder-specific operators: leave to runtime
            case TokenType.In:
            case TokenType.Like:
            case TokenType.EqualTilde:
            case TokenType.BangTilde:
            case TokenType.LessEqualGreater:
                return true;

            default:
                return true;
        }
    }

    // §12.11: shift-count types that implicitly convert to int
    private static bool IsShiftCountType(Type type) =>
        type == typeof(int) || type == typeof(sbyte) || type == typeof(byte)
        || type == typeof(short) || type == typeof(ushort) || type == typeof(char);

    private static bool HasUserDefinedBinaryOperator(TokenType op, Type left, Type right)
    {
        var methodName = op switch
        {
            TokenType.Plus => "op_Addition",
            TokenType.Minus => "op_Subtraction",
            TokenType.Star => "op_Multiply",
            TokenType.Slash => "op_Division",
            TokenType.Percent => "op_Modulus",
            TokenType.Amp => "op_BitwiseAnd",
            TokenType.Pipe => "op_BitwiseOr",
            TokenType.Caret => "op_ExclusiveOr",
            TokenType.LessLess => "op_LeftShift",
            TokenType.GreaterGreater => "op_RightShift",
            TokenType.GreaterGreaterGreater => "op_UnsignedRightShift",
            TokenType.EqualEqual => "op_Equality",
            TokenType.BangEqual => "op_Inequality",
            TokenType.Less => "op_LessThan",
            TokenType.LessEqual => "op_LessThanOrEqual",
            TokenType.Greater => "op_GreaterThan",
            TokenType.GreaterEqual => "op_GreaterThanOrEqual",
            _ => null
        };
        if (methodName == null)
            return false;

        return HasOperator(left, methodName) || HasOperator(right, methodName);

        static bool HasOperator(Type type, string name)
        {
            foreach (var m in RuntimeTypeIntrospection.GetMethods(type, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (m.Name == name && m.GetParameters().Length == 2)
                    return true;
            }
            return false;
        }
    }

    internal static Type InferBinaryResultType(TokenType op, BoundType left, BoundType right)
    {
        var leftType = left.ClrType;
        var rightType = right.ClrType;

        if (op is TokenType.EqualEqual or TokenType.BangEqual or TokenType.EqualEqualEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual or
            TokenType.In or TokenType.Like or TokenType.EqualTilde or TokenType.BangTilde)
        {
            return typeof(bool);
        }

        if (op == TokenType.LessEqualGreater)
            return typeof(int);

        if (op == TokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
            return typeof(string);

        if (op == TokenType.StarStar)
            return typeof(double);

        // §12.9.5 enum arithmetic / §12.13.3 enum bitwise — must come before the general arithmetic
        // branch because Type.GetTypeCode on an enum returns its underlying TypeCode, so
        // TypeHelpers.IsArithmetic(Type) is true for enum types and would otherwise wrongly
        // select the integer-result path.
        if (leftType.IsEnum || rightType.IsEnum)
        {
            var enumResult = InferEnumBinaryResultType(op, leftType, rightType);
            if (enumResult != null)
                return enumResult;
        }

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(leftType, rightType, op);

        // §12.4.8 lifted operators: if either operand is Nullable<T> with an arithmetic T,
        // the operator lifts and the result type is Nullable<R> where R = unlifted result.
        var leftUnderlying = Nullable.GetUnderlyingType(leftType);
        var rightUnderlying = Nullable.GetUnderlyingType(rightType);
        if ((leftUnderlying != null || rightUnderlying != null))
        {
            var lEff = leftUnderlying ?? leftType;
            var rEff = rightUnderlying ?? rightType;
            if (TypeHelpers.IsArithmetic(lEff) && TypeHelpers.IsArithmetic(rEff))
            {
                var unlifted = InferArithmeticResultType(lEff, rEff, op);
                if (unlifted != typeof(object) && unlifted.IsValueType)
                    return RuntimeGenericClosure.CloseType(typeof(Nullable<>), [unlifted]);
            }
        }

        if (left is not BoundUnknownType && leftType == typeof(object) && TypeHelpers.IsArithmetic(rightType))
            return InferArithmeticResultType(rightType, rightType, op);
        if (right is not BoundUnknownType && rightType == typeof(object) && TypeHelpers.IsArithmetic(leftType))
            return InferArithmeticResultType(leftType, leftType, op);

        return typeof(object);
    }

    private static Type? InferEnumBinaryResultType(TokenType op, Type leftType, Type rightType)
    {
        var lEnum = leftType.IsEnum;
        var rEnum = rightType.IsEnum;
        var lIntegral = !lEnum && UnaryBinder.IsIntegralOrChar(leftType);
        var rIntegral = !rEnum && UnaryBinder.IsIntegralOrChar(rightType);

        switch (op)
        {
            // §12.10.5: E + U → E, U + E → E (but not E + E)
            case TokenType.Plus:
                if (lEnum && rIntegral) return leftType;
                if (lIntegral && rEnum) return rightType;
                return null;

            // §12.10.6: E - U → E, E - E → underlying
            case TokenType.Minus:
                if (lEnum && rIntegral) return leftType;
                if (lEnum && rEnum && leftType == rightType) return Enum.GetUnderlyingType(leftType);
                return null;

            // §12.13.3: E & E → E, E | E → E, E ^ E → E
            case TokenType.Amp:
            case TokenType.Pipe:
            case TokenType.Caret:
                if (lEnum && rEnum && leftType == rightType) return leftType;
                return null;

            default:
                return null;
        }
    }

    internal static Type InferArithmeticResultType(Type leftType, Type rightType, TokenType op)
    {
        var normalizedLeft = NormalizeArithmeticType(leftType);
        var normalizedRight = NormalizeArithmeticType(rightType);

        if (op is TokenType.LessLess or TokenType.GreaterGreater or TokenType.GreaterGreaterGreater)
            return normalizedLeft;

        if (normalizedLeft == normalizedRight)
            return normalizedLeft;

        if (normalizedLeft == typeof(decimal) || normalizedRight == typeof(decimal))
            return typeof(decimal);

        if (normalizedLeft == typeof(double) || normalizedRight == typeof(double))
            return typeof(double);

        if (normalizedLeft == typeof(float) || normalizedRight == typeof(float))
            return typeof(float);

        if (normalizedLeft == typeof(ulong) || normalizedRight == typeof(ulong))
        {
            var other = normalizedLeft == typeof(ulong) ? normalizedRight : normalizedLeft;
            if (IsSignedIntegralType(other))
                return typeof(object);
            return typeof(ulong);
        }

        if (normalizedLeft == typeof(long) || normalizedRight == typeof(long))
            return typeof(long);

        if (normalizedLeft == typeof(uint) || normalizedRight == typeof(uint))
        {
            var other = normalizedLeft == typeof(uint) ? normalizedRight : normalizedLeft;
            if (IsSignedIntegralType(other))
                return typeof(long);
            return typeof(uint);
        }

        return typeof(int);
    }

    /// <summary>
    /// ECMA-334 §12.4.7.2: sbyte/byte/short/ushort/char → int, unary - on uint → long.
    /// </summary>
    internal static Type NormalizeArithmeticType(Type type)
    {
        if (type == typeof(char) || type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort))
            return typeof(int);
        return type;
    }

    internal static bool IsSignedIntegralType(Type type)
    {
        return type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(int) ||
               type == typeof(long);
    }

    internal static Type GetCommonType(Type leftType, Type rightType)
    {
        if (leftType == rightType)
            return leftType;

        if (TypeHelpers.IsArithmetic(leftType) && TypeHelpers.IsArithmetic(rightType))
            return NumericDispatch.GetResultType(leftType, rightType);

        if (leftType.IsAssignableFrom(rightType))
            return leftType;
        if (rightType.IsAssignableFrom(leftType))
            return rightType;

        if (leftType == typeof(string) || rightType == typeof(string))
            return typeof(string);

        return typeof(object);
    }

    private static Type? ComputeBinaryPromotedType(TokenType op, BoundExpr left, BoundExpr right)
    {
        if (!IsFastPathOperator(op))
            return null;

        var leftType = left.StaticType.ClrType;
        var rightType = right.StaticType.ClrType;
        if (!TypeHelpers.IsArithmetic(leftType) || !TypeHelpers.IsArithmetic(rightType))
            return null;
        if (leftType.IsEnum || rightType.IsEnum)
            return null;

        if (op is TokenType.StarStar)
            return typeof(double);

        if (op is TokenType.LessLess or TokenType.GreaterGreater)
        {
            if (!UnaryBinder.IsIntegralOrChar(leftType) || !UnaryBinder.IsIntegralOrChar(rightType))
                return null;
            return NormalizeArithmeticType(leftType);
        }

        if (op is TokenType.Amp or TokenType.Pipe or TokenType.Caret)
        {
            if (!UnaryBinder.IsIntegralOrChar(leftType) || !UnaryBinder.IsIntegralOrChar(rightType))
                return null;
        }

        if ((leftType == typeof(decimal) && (rightType == typeof(float) || rightType == typeof(double))) ||
            (rightType == typeof(decimal) && (leftType == typeof(float) || leftType == typeof(double))))
            return null;

        if (TryConstantPromotion(leftType, rightType, left as BoundLiteralExpr, right as BoundLiteralExpr, out var promoted))
            return promoted;

        var result = NumericDispatch.GetResultType(leftType, rightType);
        return TypeHelpers.IsArithmetic(result) ? result : null;
    }

    private static bool TryConstantPromotion(
        Type leftType, Type rightType,
        BoundLiteralExpr? leftLiteral, BoundLiteralExpr? rightLiteral,
        out Type promoted)
    {
        promoted = null!;
        if (leftType == typeof(uint) && IsNonNegativeIntConstant(rightLiteral) ||
            rightType == typeof(uint) && IsNonNegativeIntConstant(leftLiteral))
        {
            promoted = typeof(uint);
            return true;
        }

        if (leftType == typeof(ulong) && IsNonNegativeIntOrLongConstant(rightLiteral) ||
            rightType == typeof(ulong) && IsNonNegativeIntOrLongConstant(leftLiteral))
        {
            promoted = typeof(ulong);
            return true;
        }

        return false;
    }

    private static bool IsNonNegativeIntConstant(BoundLiteralExpr? literal) =>
        literal?.Value is int and >= 0;

    private static bool IsNonNegativeIntOrLongConstant(BoundLiteralExpr? literal) =>
        literal?.Value is int and >= 0 or long and >= 0;

    private static bool IsFastPathOperator(TokenType op) =>
        op is TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or TokenType.Percent
            or TokenType.EqualEqual or TokenType.BangEqual
            or TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual
            or TokenType.Amp or TokenType.Pipe or TokenType.Caret
            or TokenType.LessLess or TokenType.GreaterGreater
            or TokenType.StarStar;
}
