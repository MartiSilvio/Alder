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
    internal readonly record struct BinaryOpInfo(
        MethodInfo Method,
        BinaryOpSignature Signature,
        bool NegateBooleanResult = false);

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
        /// <summary>(object?, object?, bool) - TwoArgs + isChecked</summary>
        TwoArgsChecked,
        /// <summary>(object?, object?, CsEvalOptions, bool) - WithOptions + isChecked</summary>
        WithOptionsChecked,
        /// <summary>(object?, object?, CsEvalOptions, CsEvalContext?, bool) - WithOptionsAndContext + isChecked</summary>
        WithOptionsAndContextChecked,
    }

    private static readonly Dictionary<TokenType, BinaryOpInfo> BinaryOperators = BuildBinaryOperators();

    private static Dictionary<TokenType, BinaryOpInfo> BuildBinaryOperators()
    {
        var binaryOps = new Dictionary<TokenType, BinaryOpInfo>
        {
            [TokenType.Plus] = new(ResolveMethod(nameof(Operators.Add), typeof(object), typeof(object), typeof(CsEvalOptions), typeof(CsEvalContext), typeof(bool)), BinaryOpSignature.WithOptionsAndContextChecked),
            [TokenType.Minus] = new(ResolveMethod(nameof(Operators.Subtract), typeof(object), typeof(object), typeof(bool)), BinaryOpSignature.TwoArgsChecked),
            [TokenType.Star] = new(ResolveMethod(nameof(Operators.Multiply), typeof(object), typeof(object), typeof(CsEvalOptions), typeof(bool)), BinaryOpSignature.WithOptionsChecked),
            [TokenType.Slash] = new(ResolveMethod(nameof(Operators.Divide), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.Percent] = new(ResolveMethod(nameof(Operators.Modulo), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.EqualEqual] = new(ResolveMethod("Equals", typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.EqualEqualEqual] = new(ResolveMethod("Equals", typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.BangEqual] = new(ResolveMethod(nameof(Operators.NotEquals), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.BangEqualEqual] = new(ResolveMethod(nameof(Operators.NotEquals), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.Less] = new(ResolveMethod(nameof(Operators.LessThan), typeof(object), typeof(object), typeof(CsEvalOptions)), BinaryOpSignature.WithOptions),
            [TokenType.LessEqual] = new(ResolveMethod(nameof(Operators.LessThanOrEqual), typeof(object), typeof(object), typeof(CsEvalOptions)), BinaryOpSignature.WithOptions),
            [TokenType.Greater] = new(ResolveMethod(nameof(Operators.GreaterThan), typeof(object), typeof(object), typeof(CsEvalOptions)), BinaryOpSignature.WithOptions),
            [TokenType.GreaterEqual] = new(ResolveMethod(nameof(Operators.GreaterThanOrEqual), typeof(object), typeof(object), typeof(CsEvalOptions)), BinaryOpSignature.WithOptions),
            [TokenType.Amp] = new(ResolveMethod(nameof(Operators.BitwiseAnd), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.Pipe] = new(ResolveMethod(nameof(Operators.BitwiseOr), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.Caret] = new(ResolveMethod(nameof(Operators.BitwiseXor), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.LessLess] = new(ResolveMethod(nameof(Operators.LeftShift), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.GreaterGreater] = new(ResolveMethod(nameof(Operators.RightShift), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.GreaterGreaterGreater] = new(ResolveMethod(nameof(Operators.UnsignedRightShift), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.StarStar] = new(ResolveMethod(nameof(Operators.Power), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.In] = new(ResolveMethod(nameof(Operators.InOperator), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.Like] = new(ResolveMethod(nameof(Operators.Like), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.EqualTilde] = new(ResolveMethod(nameof(Operators.RegexMatch), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.BangTilde] = new(ResolveMethod(nameof(Operators.RegexNotMatch), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
            [TokenType.LessEqualGreater] = new(ResolveMethod(nameof(Operators.Spaceship), typeof(object), typeof(object)), BinaryOpSignature.TwoArgs),
        };

        // Alias operators share the same hot implementation and apply only a boolean negation.
        binaryOps[TokenType.NotIn] = binaryOps[TokenType.In] with { NegateBooleanResult = true };
        binaryOps[TokenType.NotLike] = binaryOps[TokenType.Like] with { NegateBooleanResult = true };

        return binaryOps;
    }

    private static MethodInfo ResolveMethod(string name, params Type[] parameters) =>
        typeof(Operators).GetMethod(name, parameters)!;

    internal readonly record struct UnaryOpInfo(MethodInfo Method, bool HasCheckedParam);

    private static readonly Dictionary<TokenType, UnaryOpInfo> UnaryOperators = new()
    {
        [TokenType.Minus] = new(typeof(Operators).GetMethod(nameof(Operators.Negate), [typeof(object), typeof(bool)])!, true),
        [TokenType.Plus] = new(typeof(Operators).GetMethod(nameof(Operators.UnaryPlus), [typeof(object)])!, false),
        [TokenType.Bang] = new(typeof(Operators).GetMethod(nameof(Operators.LogicalNot), [typeof(object)])!, false),
        [TokenType.Tilde] = new(typeof(Operators).GetMethod(nameof(Operators.BitwiseNot), [typeof(object)])!, false),
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
    /// Gets unary operator info for the given token type, or null if not found.
    /// </summary>
    public static UnaryOpInfo? GetUnaryOperator(TokenType op) =>
        UnaryOperators.TryGetValue(op, out var info) ? info : null;
}
