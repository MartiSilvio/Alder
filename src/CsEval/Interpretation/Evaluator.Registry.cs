using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Interpretation;

public sealed partial class Evaluator
{
    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?, object?>> CoreBinaryOperators = new()
    {
        { TokenType.Plus, (e, l, r) => Operators.Add(l, r, e._options, e._context) },
        { TokenType.Minus, (e, l, r) => Operators.Subtract(l, r, e._options) },
        { TokenType.Star, (e, l, r) => Operators.Multiply(l, r, e._options) },
        { TokenType.Slash, (e, l, r) => Operators.Divide(l, r, e._options) },
        { TokenType.Percent, (e, l, r) => Operators.Modulo(l, r, e._options) },
        { TokenType.EqualEqual, (e, l, r) => Operators.Equals(l, r, e._options) },
        { TokenType.BangEqual, (e, l, r) => !(bool)Operators.Equals(l, r, e._options) },
        { TokenType.Less, (e, l, r) => Operators.LessThan(l, r, e._options) },
        { TokenType.LessEqual, (e, l, r) => Operators.LessThanOrEqual(l, r, e._options) },
        { TokenType.Greater, (e, l, r) => Operators.GreaterThan(l, r, e._options) },
        { TokenType.GreaterEqual, (e, l, r) => Operators.GreaterThanOrEqual(l, r, e._options) },
        { TokenType.Amp, (e, l, r) => Operators.BitwiseAnd(l, r, e._options) },
        { TokenType.Pipe, (e, l, r) => Operators.BitwiseOr(l, r, e._options) },
        { TokenType.Caret, (e, l, r) => Operators.BitwiseXor(l, r, e._options) },
        { TokenType.LessLess, (e, l, r) => Operators.LeftShift(l, r) },
        { TokenType.GreaterGreater, (e, l, r) => Operators.RightShift(l, r) },
    };

    private static readonly Dictionary<TokenType, Func<Evaluator, object?, object?>> UnaryOperators = new()
    {
        { TokenType.Minus, (_, v) => Operators.Negate(v) },
        { TokenType.Bang, (_, v) => !TypeHelpers.RequireBoolean(v) },
        { TokenType.Tilde, (_, v) => Operators.BitwiseNot(v) },
    };

    private static readonly Dictionary<TokenType, TokenType> CompoundToBaseOperator = new()
    {
        { TokenType.PlusEqual, TokenType.Plus },
        { TokenType.MinusEqual, TokenType.Minus },
        { TokenType.StarEqual, TokenType.Star },
        { TokenType.SlashEqual, TokenType.Slash },
        { TokenType.PercentEqual, TokenType.Percent },
        { TokenType.AmpEqual, TokenType.Amp },
        { TokenType.PipeEqual, TokenType.Pipe },
        { TokenType.CaretEqual, TokenType.Caret },
        { TokenType.LessLessEqual, TokenType.LessLess },
        { TokenType.GreaterGreaterEqual, TokenType.GreaterGreater },
    };

    private bool TryGetBinaryOperator(TokenType op, out Func<Evaluator, object?, object?, object?> handler)
    {
        if (CoreBinaryOperators.TryGetValue(op, out handler!))
            return true;

        foreach (var ext in _options.Extensions)
        {
            if (ext.BinaryOperators.TryGetValue(op, out var extHandler))
            {
                handler = (e, l, r) => extHandler(l, r, e._options);
                return true;
            }
        }

        handler = null!;
        return false;
    }
}
