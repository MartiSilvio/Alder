using Alder.Diagnostics;
using Alder.Text;

namespace Alder.Parsing;

/// <summary>
/// Shared mutable token-stream state for the parser family.
/// All parser components observe the same cursor so sub-parser transitions remain cheap and explicit.
/// </summary>
internal sealed class ParserState
{
    public readonly List<Token> Tokens;
    public int Current;
    public readonly LanguageMode LanguageMode;

    public ParserState(List<Token> tokens, LanguageMode languageMode = LanguageMode.Standard)
    {
        Tokens = languageMode == LanguageMode.Extended
            ? NormalizeExtendedCompoundOperators(tokens)
            : tokens;
        Current = 0;
        LanguageMode = languageMode;
    }

    private static List<Token> NormalizeExtendedCompoundOperators(List<Token> tokens)
    {
        if (tokens.Count < 3)
            return tokens;

        var normalized = new List<Token>(tokens.Count);

        for (var i = 0; i < tokens.Count; i++)
        {
            var current = tokens[i];

            if (current.Type == TokenType.Identifier &&
                string.Equals(current.Lexeme, TokenLexemes.GetCanonical(TokenType.Not), StringComparison.Ordinal) &&
                i + 1 < tokens.Count)
            {
                var next = tokens[i + 1];
                if (next.Type == TokenType.In)
                {
                    normalized.Add(TokenLexemes.CreateSynthetic(TokenType.NotIn, current));
                    i++;
                    continue;
                }

                if (next.Type == TokenType.Like)
                {
                    normalized.Add(TokenLexemes.CreateSynthetic(TokenType.NotLike, current));
                    i++;
                    continue;
                }
            }

            normalized.Add(current);
        }

        return normalized;
    }
}

/// <summary>
/// Base class for parser components that operate over a shared <see cref="ParserState"/>.
/// </summary>
internal abstract class ParserBase
{
    private protected const int MaxUncheckedRecursionDepth = 20;

    private protected readonly ParserState State;

    internal LanguageMode LanguageMode => State.LanguageMode;

    private protected ParserBase(ParserState state)
    {
        State = state;
    }

    /// <summary>
    /// Records the current token start offset so the caller can later build a span with <see cref="SpanFrom"/>.
    /// </summary>
    internal int Mark() => Peek().Start;

    /// <summary>
    /// Computes a <see cref="TextSpan"/> from a recorded start offset to the end of the most recently consumed token.
    /// </summary>
    internal TextSpan SpanFrom(int start)
    {
        var prev = Previous();
        return new TextSpan(start, prev.Start + prev.Length - start);
    }

    /// <summary>
    /// Checks whether the current token can start an implicitly typed variable declaration.
    /// </summary>
    internal bool CheckVar() =>
        Check(TokenType.Var) || (State.LanguageMode == LanguageMode.Extended && Check(TokenType.Let));

    /// <summary>
    /// Matches an implicitly typed variable-declaration keyword and advances past it.
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

        // **= remains gated by language mode so the parser can share tokenization with the extended surface
        // without making the standard surface accept the operator.
        if (Check(TokenType.StarStarEqual))
        {
            if (State.LanguageMode == LanguageMode.Standard)
                throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired, TokenLexemes.GetCanonical(TokenType.StarStarEqual));
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
                TokenType.Bool or TokenType.Object or TokenType.Dynamic or
                TokenType.Sbyte or TokenType.Byte or TokenType.Short or
                TokenType.Ushort or TokenType.Uint or TokenType.Ulong or
                TokenType.Char or TokenType.Void;

    internal bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    /// <summary>
    /// Checks whether the next token has the given type.
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

    internal Token PeekNext() => PeekAt(1);

    internal Token PeekAt(int offset)
    {
        var index = State.Current + offset;
        return index < State.Tokens.Count ? State.Tokens[index] : State.Tokens[^1];
    }

    internal Token Previous() => State.Tokens[State.Current - 1];

    internal Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, message);
    }

    /// <summary>
    /// Consumes an identifier or contextual keyword token for identifier positions.
    /// ECMA-334 §6.4.4 permits contextual keywords to appear as identifiers outside their contextual use.
    /// </summary>
    internal Token ConsumeIdentifierOrContextualKeyword(string message)
    {
        if (Check(TokenType.Identifier) || IsContextualKeyword(Peek().Type))
            return Advance();
        throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, message);
    }

    internal AlderException SyntaxError(DiagnosticDescriptor descriptor, params object?[] args)
    {
        var token = Peek();
        return new AlderException(descriptor, token.Span, token.Line, token.Column, args);
    }

    /// <summary>
    /// Returns whether <paramref name="type"/> is a contextual keyword rather than a reserved keyword.
    /// </summary>
    internal static bool IsContextualKeyword(TokenType type) =>
        type is TokenType.Value or TokenType.From or TokenType.Where or TokenType.Select
            or TokenType.Group or TokenType.Into or TokenType.Orderby or TokenType.Join or TokenType.On
            or TokenType.Equals or TokenType.By or TokenType.Ascending or TokenType.Descending or TokenType.Let
            or TokenType.Get or TokenType.Set or TokenType.Add or TokenType.Remove or TokenType.Init or TokenType.When
            or TokenType.With or TokenType.And or TokenType.Or or TokenType.Not or TokenType.File or TokenType.Required
            or TokenType.Scoped or TokenType.Args
            or TokenType.Like or TokenType.Between
            or TokenType.Unless or TokenType.Until;

    /// <summary>
    /// Matches a closing generic-argument <c>&gt;</c> while handling the lexer ambiguity around <c>&gt;&gt;</c> and <c>&gt;&gt;&gt;</c>.
    /// ECMA-334 §8.2.4 and §8.2.6 require the parser to recover nested generic closers from shift-token lexing.
    /// </summary>
    internal bool MatchClosingAngleBracket()
    {
        if (Match(TokenType.Greater))
            return true;

        if (Check(TokenType.GreaterGreater))
        {
            // Rewrite the current token so the inner generic consumes one > and the outer generic sees the other.
            var token = Peek();
            var greaterLexeme = TokenLexemes.GetCanonical(TokenType.Greater);
            State.Tokens[State.Current] = token with { Type = TokenType.Greater, Lexeme = greaterLexeme };
            State.Tokens.Insert(State.Current + 1, token with { Type = TokenType.Greater, Lexeme = greaterLexeme });
            Advance();
            return true;
        }

        if (Check(TokenType.GreaterGreaterGreater))
        {
            // Split >>> into > (consumed now) + >> (left for parent)
            var token = Peek();
            State.Tokens[State.Current] = token with
            {
                Type = TokenType.Greater,
                Lexeme = TokenLexemes.GetCanonical(TokenType.Greater)
            };
            State.Tokens.Insert(State.Current + 1, token with
            {
                Type = TokenType.GreaterGreater,
                Lexeme = TokenLexemes.GetCanonical(TokenType.GreaterGreater)
            });
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
    internal string? TryParseTypeName() => TryParseTypeName(out _);

    internal string? TryParseTypeName(out IReadOnlyList<string?>? tupleElementNames)
    {
        tupleElementNames = null;
        System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();

        string name;
        if (IsTypeKeyword(Peek().Type))
        {
            name = Advance().Lexeme;
        }
        else if (Check(TokenType.Identifier))
        {
            name = Advance().Lexeme;
            while (Check(TokenType.Dot))
            {
                Advance();
                if (!Check(TokenType.Identifier))
                    return null;
                name += "." + Advance().Lexeme;
            }
        }
        else if (Check(TokenType.LeftParen))
        {
            // Tuple type: (int, string), (int x, string[] y)
            Advance(); // consume (

            var firstElementType = TryParseTypeName();
            if (firstElementType == null) return null;

            string? firstName = null;
            if (Check(TokenType.Identifier) && !IsTypeKeyword(Peek().Type))
                firstName = Advance().Lexeme;

            if (!Check(TokenType.Comma)) return null;

            var elements = new List<string> { firstElementType };
            var names = new List<string?> { firstName };
            while (Match(TokenType.Comma))
            {
                var elementType = TryParseTypeName();
                if (elementType == null) return null;

                string? elementName = null;
                if (Check(TokenType.Identifier) && !IsTypeKeyword(Peek().Type))
                    elementName = Advance().Lexeme;

                elements.Add(elementType);
                names.Add(elementName);
            }

            if (!Match(TokenType.RightParen)) return null;

            name = nameof(ValueTuple) + "<" + string.Join(", ", elements) + ">";
            if (names.Any(n => n != null))
                tupleElementNames = names;
        }
        else
        {
            return null;
        }

        // Generic args: Func<int, int> or open generics: List<>, Dictionary<,>
        if (Check(TokenType.Less))
        {
            Advance(); // consume <

            if (Check(TokenType.Greater) || Check(TokenType.Comma))
            {
                var arity = 1;
                while (Match(TokenType.Comma)) arity++;
                if (!MatchClosingAngleBracket()) return null;
                name += "`" + arity;
            }
            else
            {
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
        }

        if (Check(TokenType.Question))
        {
            Advance();
            name += "?";
        }

        // Nullable array: type?[]. The lexer tokenizes ?[ as QuestionLeftBracket.
        if (Check(TokenType.QuestionLeftBracket))
        {
            Advance(); // consume ?[
            name += "?";
            var rank = 1;
            while (Check(TokenType.Comma)) { Advance(); rank++; }
            if (!Match(TokenType.RightBracket))
                return null;
            name += "[" + new string(',', rank - 1) + "]";
        }

        while (Check(TokenType.LeftBracket))
        {
            Advance(); // consume '['

            var rank = 1;
            while (Check(TokenType.Comma))
            {
                Advance();
                rank++;
            }

            if (!Match(TokenType.RightBracket))
                return null;

            name += "[" + new string(',', rank - 1) + "]";
        }

        return name;
    }
}
