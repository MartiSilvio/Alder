using System.Collections.Frozen;
using CsEval.Parsing;

namespace CsEval.Runtime;

internal static class OperatorRegistry
{
    internal static readonly FrozenDictionary<TokenType, TokenType> CompoundToBaseOperator = new Dictionary<TokenType, TokenType>
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
    }.ToFrozenDictionary();
}
