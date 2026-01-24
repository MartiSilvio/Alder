namespace CsEval.Parsing;

public sealed class Lexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = [];
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;

    // All C# keywords - reserved to match C# spec
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        // Literals
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["null"] = TokenType.Null,
        ["undefined"] = TokenType.Undefined,  // JavaScript

        // Keywords - Implemented
        ["new"] = TokenType.New,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["return"] = TokenType.Return,
        ["var"] = TokenType.Var,
        ["function"] = TokenType.Function,  // JavaScript (reserved)

        // Keywords - Control flow (reserved)
        ["switch"] = TokenType.Switch,
        ["case"] = TokenType.Case,
        ["default"] = TokenType.Default,
        ["for"] = TokenType.For,
        ["foreach"] = TokenType.Foreach,
        ["while"] = TokenType.While,
        ["do"] = TokenType.Do,
        ["break"] = TokenType.Break,
        ["continue"] = TokenType.Continue,
        ["goto"] = TokenType.Goto,

        // Keywords - Exception handling (reserved)
        ["try"] = TokenType.Try,
        ["catch"] = TokenType.Catch,
        ["finally"] = TokenType.Finally,
        ["throw"] = TokenType.Throw,

        // Keywords - Type declarations (reserved)
        ["class"] = TokenType.Class,
        ["struct"] = TokenType.Struct,
        ["interface"] = TokenType.Interface,
        ["enum"] = TokenType.Enum,
        ["record"] = TokenType.Record,
        ["delegate"] = TokenType.Delegate,
        ["namespace"] = TokenType.Namespace,

        // Keywords - Access modifiers (reserved)
        ["public"] = TokenType.Public,
        ["private"] = TokenType.Private,
        ["protected"] = TokenType.Protected,
        ["internal"] = TokenType.Internal,

        // Keywords - Member modifiers (reserved)
        ["static"] = TokenType.Static,
        ["readonly"] = TokenType.Readonly,
        ["const"] = TokenType.Const,
        ["volatile"] = TokenType.Volatile,
        ["virtual"] = TokenType.Virtual,
        ["override"] = TokenType.Override,
        ["abstract"] = TokenType.Abstract,
        ["sealed"] = TokenType.Sealed,
        ["extern"] = TokenType.Extern,
        ["partial"] = TokenType.Partial,
        ["async"] = TokenType.Async,
        ["await"] = TokenType.Await,

        // Keywords - Parameter modifiers (reserved)
        ["ref"] = TokenType.Ref,
        ["out"] = TokenType.Out,
        ["in"] = TokenType.In,
        ["params"] = TokenType.Params,

        // Keywords - Type operations (reserved)
        ["is"] = TokenType.Is,
        ["as"] = TokenType.As,
        ["typeof"] = TokenType.Typeof,
        ["sizeof"] = TokenType.Sizeof,
        ["nameof"] = TokenType.Nameof,
        ["stackalloc"] = TokenType.Stackalloc,
        ["checked"] = TokenType.Checked,
        ["unchecked"] = TokenType.Unchecked,

        // Keywords - Other (reserved)
        ["this"] = TokenType.This,
        ["base"] = TokenType.Base,
        ["super"] = TokenType.Super,  // JavaScript (equivalent to base)
        ["using"] = TokenType.Using,
        ["lock"] = TokenType.Lock,
        ["fixed"] = TokenType.Fixed,
        ["unsafe"] = TokenType.Unsafe,
        ["implicit"] = TokenType.Implicit,
        ["explicit"] = TokenType.Explicit,
        ["operator"] = TokenType.Operator,
        ["event"] = TokenType.Event,

        // Type keywords (some implemented for variable declarations)
        ["int"] = TokenType.Int,
        ["long"] = TokenType.Long,
        ["double"] = TokenType.Double,
        ["float"] = TokenType.Float,
        ["decimal"] = TokenType.Decimal,
        ["string"] = TokenType.StringType,
        ["bool"] = TokenType.Bool,
        ["object"] = TokenType.Object,
        ["void"] = TokenType.Void,
        ["sbyte"] = TokenType.Sbyte,
        ["byte"] = TokenType.Byte,
        ["short"] = TokenType.Short,
        ["ushort"] = TokenType.Ushort,
        ["uint"] = TokenType.Uint,
        ["ulong"] = TokenType.Ulong,
        ["char"] = TokenType.Char,
        ["nint"] = TokenType.Nint,
        ["nuint"] = TokenType.Nuint,
        ["dynamic"] = TokenType.Dynamic,

        // Contextual keywords (reserved for forward compatibility)
        ["add"] = TokenType.Add,
        ["alias"] = TokenType.Alias,
        ["and"] = TokenType.And,
        ["args"] = TokenType.Args,
        ["ascending"] = TokenType.Ascending,
        ["by"] = TokenType.By,
        ["descending"] = TokenType.Descending,
        ["equals"] = TokenType.Equals,
        ["file"] = TokenType.File,
        ["from"] = TokenType.From,
        ["get"] = TokenType.Get,
        ["global"] = TokenType.Global,
        ["group"] = TokenType.Group,
        ["init"] = TokenType.Init,
        ["into"] = TokenType.Into,
        ["join"] = TokenType.Join,
        ["let"] = TokenType.Let,
        ["managed"] = TokenType.Managed,
        ["not"] = TokenType.Not,
        ["notnull"] = TokenType.Notnull,
        ["on"] = TokenType.On,
        ["or"] = TokenType.Or,
        ["orderby"] = TokenType.Orderby,
        ["remove"] = TokenType.Remove,
        ["required"] = TokenType.Required,
        ["scoped"] = TokenType.Scoped,
        ["select"] = TokenType.Select,
        ["set"] = TokenType.Set,
        ["unmanaged"] = TokenType.Unmanaged,
        ["value"] = TokenType.Value,
        ["when"] = TokenType.When,
        ["where"] = TokenType.Where,
        ["with"] = TokenType.With,
        ["yield"] = TokenType.Yield,
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
            case '+': AddToken(Match('+') ? TokenType.PlusPlus : Match('=') ? TokenType.PlusEqual : TokenType.Plus); break;
            case '-': AddToken(Match('-') ? TokenType.MinusMinus : Match('=') ? TokenType.MinusEqual : TokenType.Minus); break;
            case '*': AddToken(Match('=') ? TokenType.StarEqual : TokenType.Star); break;
            case '%': AddToken(Match('=') ? TokenType.PercentEqual : TokenType.Percent); break;

            case '.':
                if (Match('.') && Match('.'))
                    AddToken(TokenType.DotDotDot);
                else
                    AddToken(TokenType.Dot);
                break;

            case '!':
                if (Match('='))
                {
                    AddToken(Match('=') ? TokenType.BangEqualEqual : TokenType.BangEqual);
                }
                else
                {
                    AddToken(TokenType.Bang);
                }
                break;

            case '=':
                if (Match('='))
                {
                    AddToken(Match('=') ? TokenType.EqualEqualEqual : TokenType.EqualEqual);
                }
                else if (Match('>'))
                {
                    AddToken(TokenType.Arrow);
                }
                else
                {
                    AddToken(TokenType.Equal);
                }
                break;

            case '<':
                if (Match('<'))
                {
                    AddToken(Match('=') ? TokenType.LessLessEqual : TokenType.LessLess);
                }
                else if (Match('=')) AddToken(TokenType.LessEqual);
                else AddToken(TokenType.Less);
                break;

            case '>':
                if (Match('>'))
                {
                    AddToken(Match('=') ? TokenType.GreaterGreaterEqual : TokenType.GreaterGreater);
                }
                else if (Match('=')) AddToken(TokenType.GreaterEqual);
                else AddToken(TokenType.Greater);
                break;

            case '&':
                if (Match('&')) AddToken(TokenType.AmpAmp);
                else if (Match('=')) AddToken(TokenType.AmpEqual);
                else AddToken(TokenType.Amp);
                break;

            case '|':
                if (Match('|')) AddToken(TokenType.PipePipe);
                else if (Match('=')) AddToken(TokenType.PipeEqual);
                else AddToken(TokenType.Pipe);
                break;

            case '^':
                AddToken(Match('=') ? TokenType.CaretEqual : TokenType.Caret);
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
                else if (Match('='))
                {
                    AddToken(TokenType.SlashEqual);
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
                if (Match('@'))
                {
                    if (Match('"'))
                        ScanVerbatimInterpolatedString();
                    else
                        throw new LexerException($"Unexpected character sequence '$@' at {_line}:{_column}. Did you mean '$@\"...'?");
                }
                else if (Match('"'))
                    ScanInterpolatedString();
                else
                    throw new LexerException($"Unexpected character '$' at {_line}:{_column}. Did you mean '$\"...'?");
                break;

            case '@':
                if (Match('$'))
                {
                    if (Match('"'))
                        ScanVerbatimInterpolatedString();
                    else
                        throw new LexerException($"Unexpected character sequence '@$' at {_line}:{_column}. Did you mean '@$\"...'?");
                }
                else if (Match('"'))
                    ScanVerbatimString();
                else
                    throw new LexerException($"Unexpected character '@' at {_line}:{_column}. Did you mean '@\"...'?");
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
                if (braceDepth == 0 && PeekNext() == '{')
                {
                    // Escaped brace {{ - keep as-is for parser
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
                else
                {
                    braceDepth++;
                    sb.Append(Advance());
                }
            }
            else if (Peek() == '}')
            {
                if (braceDepth == 0 && PeekNext() == '}')
                {
                    // Escaped brace }} - keep as-is for parser
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
                else
                {
                    braceDepth--;
                    sb.Append(Advance());
                }
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

    private void ScanVerbatimString()
    {
        // Verbatim strings: @"..." - backslashes are literal, "" escapes "
        var sb = new StringBuilder();

        while (!IsAtEnd())
        {
            if (Peek() == '"')
            {
                if (PeekNext() == '"')
                {
                    // Escaped quote: "" becomes "
                    Advance();
                    Advance();
                    sb.Append('"');
                }
                else
                {
                    // End of string
                    break;
                }
            }
            else
            {
                if (Peek() == '\n') { _line++; _column = 0; }
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw new LexerException($"Unterminated verbatim string at {_line}:{_column}");

        Advance(); // closing "
        AddToken(TokenType.String, sb.ToString());
    }

    private void ScanVerbatimInterpolatedString()
    {
        // Verbatim interpolated strings: $@"..." or @$"..."
        // Backslashes are literal, "" escapes ", {{ and }} escape braces
        var sb = new StringBuilder();
        var braceDepth = 0;

        while (!IsAtEnd())
        {
            if (Peek() == '"' && braceDepth == 0)
            {
                if (PeekNext() == '"')
                {
                    // Escaped quote: "" becomes "
                    Advance();
                    Advance();
                    sb.Append('"');
                }
                else
                {
                    // End of string
                    break;
                }
            }
            else if (Peek() == '{')
            {
                if (braceDepth == 0 && PeekNext() == '{')
                {
                    // Escaped brace {{ - keep as-is for parser
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
                else
                {
                    braceDepth++;
                    sb.Append(Advance());
                }
            }
            else if (Peek() == '}')
            {
                if (braceDepth == 0 && PeekNext() == '}')
                {
                    // Escaped brace }} - keep as-is for parser
                    sb.Append(Advance());
                    sb.Append(Advance());
                }
                else
                {
                    braceDepth--;
                    sb.Append(Advance());
                }
            }
            else
            {
                if (Peek() == '\n') { _line++; _column = 0; }
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw new LexerException($"Unterminated verbatim interpolated string at {_line}:{_column}");

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

        // Treat 'let' as 'var' for JavaScript compatibility
        if (type == TokenType.Let)
        {
            type = TokenType.Var;
        }

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