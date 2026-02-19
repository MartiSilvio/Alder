using CsEval.Parsing;

namespace CsEval.Runtime;

/// <summary>
/// Data-driven registry mapping TokenType to Operators MethodInfo for binary and unary operators.
/// Consolidates the individually cached MethodInfo fields that were previously scattered across the compiler.
/// </summary>
internal static class OperatorRegistry
{
    /// <summary>
    /// Metadata for a binary operator method, including parameter signature information.
    /// </summary>
    internal readonly record struct BinaryOpInfo(MethodInfo Method, BinaryOpSignature Signature);

    /// <summary>
    /// Describes the parameter signature beyond the two operands (left, right).
    /// </summary>
    internal enum BinaryOpSignature
    {
        /// <summary>Two params: (object?, object?)</summary>
        TwoArgs,
        /// <summary>Three params: (object?, object?, CsEvalOptions)</summary>
        WithOptions,
        /// <summary>Four params: (object?, object?, CsEvalOptions, CsEvalContext?)</summary>
        WithOptionsAndContext,
    }

    private static readonly Dictionary<TokenType, BinaryOpInfo> BinaryOperators = new()
    {
        [TokenType.Plus] = new(typeof(Operators).GetMethod(nameof(Operators.Add), [typeof(object), typeof(object), typeof(CsEvalOptions), typeof(CsEvalContext)])!, BinaryOpSignature.WithOptionsAndContext),
        [TokenType.Minus] = new(typeof(Operators).GetMethod(nameof(Operators.Subtract), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.Star] = new(typeof(Operators).GetMethod(nameof(Operators.Multiply), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.Slash] = new(typeof(Operators).GetMethod(nameof(Operators.Divide), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.Percent] = new(typeof(Operators).GetMethod(nameof(Operators.Modulo), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.EqualEqual] = new(typeof(Operators).GetMethod("Equals", [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.EqualEqualEqual] = new(typeof(Operators).GetMethod("Equals", [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.BangEqual] = new(typeof(Operators).GetMethod(nameof(Operators.NotEquals), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.BangEqualEqual] = new(typeof(Operators).GetMethod(nameof(Operators.NotEquals), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.Less] = new(typeof(Operators).GetMethod(nameof(Operators.LessThan), [typeof(object), typeof(object), typeof(CsEvalOptions)])!, BinaryOpSignature.WithOptions),
        [TokenType.LessEqual] = new(typeof(Operators).GetMethod(nameof(Operators.LessThanOrEqual), [typeof(object), typeof(object), typeof(CsEvalOptions)])!, BinaryOpSignature.WithOptions),
        [TokenType.Greater] = new(typeof(Operators).GetMethod(nameof(Operators.GreaterThan), [typeof(object), typeof(object), typeof(CsEvalOptions)])!, BinaryOpSignature.WithOptions),
        [TokenType.GreaterEqual] = new(typeof(Operators).GetMethod(nameof(Operators.GreaterThanOrEqual), [typeof(object), typeof(object), typeof(CsEvalOptions)])!, BinaryOpSignature.WithOptions),
        [TokenType.Amp] = new(typeof(Operators).GetMethod(nameof(Operators.BitwiseAnd), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.Pipe] = new(typeof(Operators).GetMethod(nameof(Operators.BitwiseOr), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.Caret] = new(typeof(Operators).GetMethod(nameof(Operators.BitwiseXor), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.LessLess] = new(typeof(Operators).GetMethod(nameof(Operators.LeftShift), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.GreaterGreater] = new(typeof(Operators).GetMethod(nameof(Operators.RightShift), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.GreaterGreaterGreater] = new(typeof(Operators).GetMethod(nameof(Operators.UnsignedRightShift), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
        [TokenType.StarStar] = new(typeof(Operators).GetMethod(nameof(Operators.Power), [typeof(object), typeof(object)])!, BinaryOpSignature.TwoArgs),
    };

    private static readonly Dictionary<TokenType, MethodInfo> UnaryOperators = new()
    {
        [TokenType.Minus] = typeof(Operators).GetMethod(nameof(Operators.Negate), [typeof(object)])!,
        [TokenType.Plus] = typeof(Operators).GetMethod(nameof(Operators.UnaryPlus), [typeof(object)])!,
        [TokenType.Bang] = typeof(Operators).GetMethod(nameof(Operators.LogicalNot), [typeof(object)])!,
        [TokenType.Tilde] = typeof(Operators).GetMethod(nameof(Operators.BitwiseNot), [typeof(object)])!,
    };

    /// <summary>
    /// Maps compound assignment token types to their base binary operator token types.
    /// </summary>
    internal static readonly Dictionary<TokenType, TokenType> CompoundToBaseOperator = new()
    {
        [TokenType.PlusEqual] = TokenType.Plus,
        [TokenType.MinusEqual] = TokenType.Minus,
        [TokenType.StarEqual] = TokenType.Star,
        [TokenType.SlashEqual] = TokenType.Slash,
        [TokenType.PercentEqual] = TokenType.Percent,
        [TokenType.AmpEqual] = TokenType.Amp,
        [TokenType.PipeEqual] = TokenType.Pipe,
        [TokenType.CaretEqual] = TokenType.Caret,
        [TokenType.LessLessEqual] = TokenType.LessLess,
        [TokenType.GreaterGreaterEqual] = TokenType.GreaterGreater,
        [TokenType.GreaterGreaterGreaterEqual] = TokenType.GreaterGreaterGreater,
        [TokenType.StarStarEqual] = TokenType.StarStar,
    };

    /// <summary>
    /// Gets binary operator info for the given token type, or null if not found.
    /// </summary>
    public static BinaryOpInfo? GetBinaryOperator(TokenType op) =>
        BinaryOperators.TryGetValue(op, out var info) ? info : null;

    /// <summary>
    /// Gets unary operator MethodInfo for the given token type, or null if not found.
    /// </summary>
    public static MethodInfo? GetUnaryMethod(TokenType op) =>
        UnaryOperators.TryGetValue(op, out var method) ? method : null;
}
