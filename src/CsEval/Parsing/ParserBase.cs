using CsEval.Diagnostics;

namespace CsEval.Parsing;

/// <summary>
/// Shared mutable token stream state. All sub-parsers share a single instance
/// so that advancing the position in one parser is visible to all others.
/// </summary>
internal sealed class ParserState
{
    public readonly List<Token> Tokens;
    public int Current;
    public readonly LanguageMode LanguageMode;

    public ParserState(List<Token> tokens, LanguageMode languageMode = LanguageMode.Standard)
    {
        Tokens = tokens;
        Current = 0;
        LanguageMode = languageMode;
    }
}

/// <summary>
/// Abstract base for all parser classes. Provides shared token stream utilities
/// (Match, Check, Advance, Peek, Consume, etc.) backed by a shared ParserState.
/// </summary>
public abstract class ParserBase
{
    private protected readonly ParserState State;

    internal LanguageMode LanguageMode => State.LanguageMode;

    private protected ParserBase(ParserState state)
    {
        State = state;
    }

    #region Token Utilities

    /// <summary>
    /// Checks if the current token is 'var' or (in Extended mode) 'let'.
    /// </summary>
    internal bool CheckVar() =>
        Check(TokenType.Var) || (State.LanguageMode == LanguageMode.Extended && Check(TokenType.Let));

    /// <summary>
    /// Matches 'var' or (in Extended mode) 'let', advancing past the token.
    /// </summary>
    internal bool MatchVar() =>
        Match(TokenType.Var) || (State.LanguageMode == LanguageMode.Extended && Match(TokenType.Let));

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
                  TokenType.GreaterGreaterEqual, TokenType.GreaterGreaterGreaterEqual))
        {
            op = Previous();
            return true;
        }

        // **= is an Extended-only compound assignment (matches ** gating in ExpressionParser)
        if (Check(TokenType.StarStarEqual))
        {
            if (State.LanguageMode == LanguageMode.Standard)
                throw new CsEvalLanguageModeException("**=",
                    "Compound power assignment '**=' is not available in Standard mode. " +
                    "Use LanguageMode.Extended to enable non-standard syntax extensions.");
            Advance();
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
                TokenType.Uint or TokenType.Ulong or TokenType.Char or
            TokenType.Void;

    internal bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    /// <summary>
    /// Checks if the token one position ahead of current has the given type.
    /// Used for two-token lookahead (e.g., "not in", "not like").
    /// </summary>
    internal bool CheckNext(TokenType type)
    {
        if (State.Current + 1 >= State.Tokens.Count) return false;
        return State.Tokens[State.Current + 1].Type == type;
    }

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

    /// <summary>
    /// Consumes an identifier or contextual keyword token for use as a variable name.
    /// ECMA-334 §6.4.4 - Contextual keywords are not reserved and can appear as identifiers
    /// in variable declaration contexts (e.g., var from = ..., int where = ...).
    /// </summary>
    internal Token ConsumeIdentifierOrContextualKeyword(string message)
    {
        if (Check(TokenType.Identifier) || IsContextualKeyword(Peek().Type))
            return Advance();
        throw new CsEvalParserException($"{message} at {Peek().Line}:{Peek().Column}");
    }

    /// <summary>
    /// Checks if a token type is a contextual keyword that can be used as an identifier.
    /// ECMA-334 §6.4.4 - Contextual keywords are not reserved and can appear as identifiers.
    /// </summary>
    internal static bool IsContextualKeyword(TokenType type) =>
        type is TokenType.Value or TokenType.From or TokenType.Where or TokenType.Select
            or TokenType.Group or TokenType.Into or TokenType.Orderby or TokenType.Join or TokenType.On
            or TokenType.Equals or TokenType.By or TokenType.Ascending or TokenType.Descending or TokenType.Let
            or TokenType.Get or TokenType.Set or TokenType.Add or TokenType.Remove or TokenType.Init or TokenType.When
            or TokenType.With or TokenType.And or TokenType.Or or TokenType.Not or TokenType.File or TokenType.Required
            or TokenType.Scoped or TokenType.Args
            or TokenType.Like or TokenType.Between;

    #endregion

    #region Type Name Parsing

    /// <summary>
    /// Matches a closing '>' for generic type arguments, handling the classic C# ambiguity
    /// where the lexer greedily produces '>>' or '>>>' tokens.
    /// ECMA-334 §8.2.6 - Grammar ambiguities: >> in nested generics.
    /// Splits multi-character tokens by replacing them in the token list.
    /// </summary>
    internal bool MatchClosingAngleBracket()
    {
        if (Match(TokenType.Greater))
            return true;

        if (Check(TokenType.GreaterGreater))
        {
            // Split >> into > (consumed now) + > (left for parent generic)
            var token = Peek();
            State.Tokens[State.Current] = token with { Type = TokenType.Greater, Lexeme = ">" };
            State.Tokens.Insert(State.Current + 1, token with { Type = TokenType.Greater, Lexeme = ">" });
            Advance();
            return true;
        }

        if (Check(TokenType.GreaterGreaterGreater))
        {
            // Split >>> into > (consumed now) + >> (left for parent)
            var token = Peek();
            State.Tokens[State.Current] = token with { Type = TokenType.Greater, Lexeme = ">" };
            State.Tokens.Insert(State.Current + 1, token with { Type = TokenType.GreaterGreater, Lexeme = ">>" });
            Advance();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to parse a type name at the current position, including generic arguments
    /// (e.g., int, string, Func&lt;int, int&gt;, int?, int[]).
    /// Returns null if no type name can be parsed. Used in backtracking contexts.
    /// ECMA-334 §8.1 - Type syntax.
    /// </summary>
    internal string? TryParseTypeName()
    {
        string name;
        if (IsTypeKeyword(Peek().Type))
        {
            name = Advance().Lexeme;
        }
        else if (Check(TokenType.Identifier))
        {
            name = Advance().Lexeme;
            // Support dotted names: System.Collections.Generic.List
            while (Check(TokenType.Dot))
            {
                Advance(); // consume dot
                if (!Check(TokenType.Identifier))
                    return null;
                name += "." + Advance().Lexeme;
            }
        }
        else
        {
            return null;
        }

        // Handle generic args: Func<int, int>
        if (Check(TokenType.Less))
        {
            Advance(); // consume <
            name += "<";

            var firstArg = TryParseTypeName();
            if (firstArg == null) return null;
            name += firstArg;

            while (Match(TokenType.Comma))
            {
                name += ", ";
                var nextArg = TryParseTypeName();
                if (nextArg == null) return null;
                name += nextArg;
            }

            if (!MatchClosingAngleBracket()) return null;
            name += ">";
        }

        // Handle nullable suffix
        if (Check(TokenType.Question))
        {
            Advance();
            name += "?";
        }

        // Handle array suffix
        if (Check(TokenType.LeftBracket) && PeekNext().Type == TokenType.RightBracket)
        {
            Advance(); Advance();
            name += "[]";
        }

        return name;
    }

    #endregion
}

public class CsEvalParserException : CsEvalException
{
    public CsEvalParserException(string message) : base(message) { }
    public CsEvalParserException(DiagnosticDescriptor descriptor, params object?[] args) : base(descriptor, args) { }
}
