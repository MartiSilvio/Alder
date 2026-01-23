namespace CsEval.Parsing;

public sealed class Lexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = [];
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["null"] = TokenType.Null,
        ["new"] = TokenType.New,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["switch"] = TokenType.Switch,
        ["case"] = TokenType.Case,
        ["default"] = TokenType.Default,
        ["return"] = TokenType.Return,
        ["var"] = TokenType.Var,
        // Type keywords (reserved, like C#)
        ["int"] = TokenType.Int,
        ["long"] = TokenType.Long,
        ["double"] = TokenType.Double,
        ["float"] = TokenType.Float,
        ["decimal"] = TokenType.Decimal,
        ["string"] = TokenType.StringType,
        ["bool"] = TokenType.Bool,
        ["object"] = TokenType.Object,
    };

    public Lexer(string source)
    {
        _source = source;
    }

    public List<Token> Tokenize()
    {
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }

        _tokens.Add(new Token(TokenType.Eof, "", null, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        var c = Advance();
        switch (c)
        {
            case '(': AddToken(TokenType.LeftParen); break;
            case ')': AddToken(TokenType.RightParen); break;
            case '[': AddToken(TokenType.LeftBracket); break;
            case ']': AddToken(TokenType.RightBracket); break;
            case '{': AddToken(TokenType.LeftBrace); break;
            case '}': AddToken(TokenType.RightBrace); break;
            case ',': AddToken(TokenType.Comma); break;
            case ':': AddToken(TokenType.Colon); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case '+': AddToken(TokenType.Plus); break;
            case '-': AddToken(TokenType.Minus); break;
            case '*': AddToken(TokenType.Star); break;
            case '%': AddToken(TokenType.Percent); break;

            case '.':
                if (Match('.') && Match('.'))
                    AddToken(TokenType.DotDotDot);
                else
                    AddToken(TokenType.Dot);
                break;

            case '!':
                AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                break;

            case '=':
                AddToken(Match('=') ? TokenType.EqualEqual : Match('>') ? TokenType.Arrow : TokenType.Equal);
                break;

            case '<':
                if (Match('<')) AddToken(TokenType.LessLess);
                else if (Match('=')) AddToken(TokenType.LessEqual);
                else AddToken(TokenType.Less);
                break;

            case '>':
                if (Match('>')) AddToken(TokenType.GreaterGreater);
                else if (Match('=')) AddToken(TokenType.GreaterEqual);
                else AddToken(TokenType.Greater);
                break;

            case '&':
                AddToken(Match('&') ? TokenType.AmpAmp : TokenType.Amp);
                break;

            case '|':
                AddToken(Match('|') ? TokenType.PipePipe : TokenType.Pipe);
                break;

            case '^':
                AddToken(TokenType.Caret);
                break;

            case '~':
                AddToken(TokenType.Tilde);
                break;

            case '?':
                if (Match('?'))
                {
                    if (Match('=')) AddToken(TokenType.QuestionQuestionEqual);
                    else AddToken(TokenType.QuestionQuestion);
                }
                else if (Match('.')) AddToken(TokenType.QuestionDot);
                else AddToken(TokenType.Question);
                break;

            case '/':
                if (Match('/'))
                {
                    // Single-line comment
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else if (Match('*'))
                {
                    // Multi-line comment
                    while (!IsAtEnd() && !(Peek() == '*' && PeekNext() == '/'))
                    {
                        if (Peek() == '\n') { _line++; _column = 0; }
                        Advance();
                    }
                    if (!IsAtEnd()) { Advance(); Advance(); } // consume */
                }
                else
                {
                    AddToken(TokenType.Slash);
                }
                break;

            case ' ':
            case '\r':
            case '\t':
                break;

            case '\n':
                _line++;
                _column = 1;
                break;

            case '"':
                ScanString('"');
                break;

            case '\'':
                ScanString('\'');
                break;

            case '$':
                if (Match('"'))
                    ScanInterpolatedString();
                else
                    throw new LexerException($"Unexpected character '$' at {_line}:{_column}. Did you mean '$\"...'?");
                break;

            default:
                if (char.IsDigit(c))
                    ScanNumber();
                else if (char.IsLetter(c) || c == '_')
                    ScanIdentifier();
                else
                    throw new LexerException($"Unexpected character '{c}' at {_line}:{_column}");
                break;
        }
    }

    private void ScanString(char quote)
    {
        var sb = new StringBuilder();
        while (Peek() != quote && !IsAtEnd())
        {
            if (Peek() == '\n') { _line++; _column = 0; }
            if (Peek() == '\\')
            {
                Advance();
                sb.Append(Peek() switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => throw new LexerException($"Unknown escape sequence '\\{Peek()}' at {_line}:{_column}")
                });
                Advance();
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw new LexerException($"Unterminated string at {_line}:{_column}");

        Advance(); // closing quote
        AddToken(TokenType.String, sb.ToString());
    }

    private void ScanInterpolatedString()
    {
        // Store the raw interpolated string content including {} expressions
        var sb = new StringBuilder();
        var braceDepth = 0;

        while (!IsAtEnd())
        {
            if (Peek() == '"' && braceDepth == 0)
                break;

            if (Peek() == '{')
            {
                braceDepth++;
                sb.Append(Advance());
            }
            else if (Peek() == '}')
            {
                braceDepth--;
                sb.Append(Advance());
            }
            else if (Peek() == '\\')
            {
                Advance();
                sb.Append(Peek() switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '{' => '{',
                    '}' => '}',
                    _ => throw new LexerException($"Unknown escape sequence '\\{Peek()}' at {_line}:{_column}")
                });
                Advance();
            }
            else
            {
                if (Peek() == '\n') { _line++; _column = 0; }
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw new LexerException($"Unterminated interpolated string at {_line}:{_column}");

        Advance(); // closing "
        AddToken(TokenType.InterpolatedString, sb.ToString());
    }

    private void ScanNumber()
    {
        while (char.IsDigit(Peek())) Advance();

        // Look for decimal part
        var hasDecimalPoint = false;
        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            hasDecimalPoint = true;
            Advance(); // consume .
            while (char.IsDigit(Peek())) Advance();
        }

        var numberText = _source[_start.._current];

        // Check for type suffix - C# supports: L, U, UL, F, D, M (case-insensitive)
        var suffix = ParseNumericSuffix();

        // Parse based on suffix and decimal point presence
        // Matches C# behavior: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
        object value = suffix switch
        {
            NumericSuffix.Long => long.Parse(numberText),
            NumericSuffix.ULong => ulong.Parse(numberText),
            NumericSuffix.UInt => uint.Parse(numberText),
            NumericSuffix.Float => float.Parse(numberText),
            NumericSuffix.Double => double.Parse(numberText),
            NumericSuffix.Decimal => decimal.Parse(numberText),
            NumericSuffix.None => hasDecimalPoint
                ? double.Parse(numberText)
                : ParseIntegerWithPromotion(numberText),
            _ => throw new LexerException($"Unknown numeric suffix at {_line}:{_column}")
        };

        AddToken(TokenType.Number, value);
    }

    /// <summary>
    /// Parse integer literal with automatic type promotion (C# behavior):
    /// - If fits in int → int
    /// - If fits in long → long
    /// - Otherwise → error
    /// </summary>
    private static object ParseIntegerWithPromotion(string text)
    {
        if (int.TryParse(text, out var intValue))
            return intValue;
        if (long.TryParse(text, out var longValue))
            return longValue;
        // For very large numbers, could support ulong, but keeping it simple
        throw new OverflowException($"Integer literal '{text}' is too large");
    }

    private enum NumericSuffix { None, Long, ULong, UInt, Float, Double, Decimal }

    /// <summary>
    /// Parse C# numeric suffix: L, U, UL, LU, F, D, M (case-insensitive)
    /// </summary>
    private NumericSuffix ParseNumericSuffix()
    {
        var c1 = char.ToLowerInvariant(Peek());

        if (c1 == 'f') { Advance(); return NumericSuffix.Float; }
        if (c1 == 'd') { Advance(); return NumericSuffix.Double; }
        if (c1 == 'm') { Advance(); return NumericSuffix.Decimal; }

        if (c1 == 'l')
        {
            Advance();
            var c2 = char.ToLowerInvariant(Peek());
            if (c2 == 'u') { Advance(); return NumericSuffix.ULong; }
            return NumericSuffix.Long;
        }

        if (c1 == 'u')
        {
            Advance();
            var c2 = char.ToLowerInvariant(Peek());
            if (c2 == 'l') { Advance(); return NumericSuffix.ULong; }
            return NumericSuffix.UInt;
        }

        return NumericSuffix.None;
    }

    private void ScanIdentifier()
    {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();

        var text = _source[_start.._current];
        var type = Keywords.GetValueOrDefault(text, TokenType.Identifier);
        AddToken(type);
    }

    private bool IsAtEnd() => _current >= _source.Length;

    private char Advance()
    {
        _column++;
        return _source[_current++];
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_current];

    private char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source[_current + 1];

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_current] != expected) return false;
        _current++;
        _column++;
        return true;
    }

    private void AddToken(TokenType type, object? literal = null)
    {
        var text = _source[_start.._current];
        _tokens.Add(new Token(type, text, literal, _line, _column - text.Length));
    }
}

public class LexerException(string message) : Exception(message);