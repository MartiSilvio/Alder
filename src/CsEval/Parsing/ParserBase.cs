namespace CsEval.Parsing;

/// <summary>
/// Shared mutable token stream state. All sub-parsers share a single instance
/// so that advancing the position in one parser is visible to all others.
/// </summary>
internal sealed class ParserState
{
    public readonly List<Token> Tokens;
    public int Current;

    public ParserState(List<Token> tokens)
    {
        Tokens = tokens;
        Current = 0;
    }
}

/// <summary>
/// Abstract base for all parser classes. Provides shared token stream utilities
/// (Match, Check, Advance, Peek, Consume, etc.) backed by a shared ParserState.
/// </summary>
public abstract class ParserBase
{
    private protected readonly ParserState State;

    private protected ParserBase(ParserState state)
    {
        State = state;
    }

    #region Token Utilities

    internal bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    internal bool MatchCompoundAssignment(out Token op)
    {
        if (Match(TokenType.PlusEqual, TokenType.MinusEqual, TokenType.StarEqual,
                  TokenType.SlashEqual, TokenType.PercentEqual, TokenType.AmpEqual,
                  TokenType.PipeEqual, TokenType.CaretEqual, TokenType.LessLessEqual,
                  TokenType.GreaterGreaterEqual))
        {
            op = Previous();
            return true;
        }
        op = default;
        return false;
    }

    internal bool MatchTypeKeyword(out Token typeToken)
    {
        if (IsTypeKeyword(Peek().Type))
        {
            typeToken = Advance();

            if (Check(TokenType.Question))
            {
                Advance();
                typeToken = typeToken with { Lexeme = typeToken.Lexeme + "?" };
            }

            return true;
        }
        typeToken = default;
        return false;
    }

    internal static bool IsTypeKeyword(TokenType type) =>
        type is TokenType.Int or TokenType.Long or TokenType.Double or
                TokenType.Float or TokenType.Decimal or TokenType.StringType or
                TokenType.Bool or TokenType.Object or TokenType.Sbyte or
                TokenType.Byte or TokenType.Short or TokenType.Ushort or
                TokenType.Uint or TokenType.Ulong or TokenType.Char;

    internal bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    internal Token Advance()
    {
        if (!IsAtEnd()) State.Current++;
        return Previous();
    }

    internal bool IsAtEnd() => Peek().Type == TokenType.Eof;

    internal Token Peek() => State.Tokens[State.Current];

    internal Token PeekNext() => State.Current + 1 < State.Tokens.Count ? State.Tokens[State.Current + 1] : State.Tokens[^1];

    internal Token Previous() => State.Tokens[State.Current - 1];

    internal Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new CsEvalParserException($"{message} at {Peek().Line}:{Peek().Column}");
    }

    #endregion
}

public class CsEvalParserException(string message) : CsEvalException(message);
