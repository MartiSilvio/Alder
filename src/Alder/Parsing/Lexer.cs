using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Parsing;

internal sealed class Lexer
{
    private readonly string _source;
    private readonly List<Token> _tokens = [];
    private int _start;
    private int _current;
    private int _line = 1;
    private int _column = 1;

    // All C# keywords - reserved to match C# spec
    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
    private static readonly FixedDictionary<string, TokenType> Keywords = FixedDictionary<string, TokenType>.Create(new Dictionary<string, TokenType>
    {
        // Literals
        ["true"] = TokenType.True,
        ["false"] = TokenType.False,
        ["null"] = TokenType.Null,

        // Keywords - Implemented
        ["new"] = TokenType.New,
        ["if"] = TokenType.If,
        ["else"] = TokenType.Else,
        ["return"] = TokenType.Return,
        ["var"] = TokenType.Var,


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
        ["super"] = TokenType.Super,  // JavaScript super
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

        // Extended mode operators (contextual keywords)
        ["like"] = TokenType.Like,
        ["between"] = TokenType.Between,
        ["unless"] = TokenType.Unless,
        ["until"] = TokenType.Until,

        // Contextual keywords (reserved for forward compatibility)
        ["add"] = TokenType.Add,
        ["alias"] = TokenType.Alias,
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
        ["notnull"] = TokenType.Notnull,
        ["on"] = TokenType.On,
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
    });

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

        _tokens.Add(new Token(TokenType.Eof, "", null, _line, _column, _current));
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
            case '*':
                if (Match('*'))
                {
                    if (Match('='))
                        AddToken(TokenType.StarStarEqual);
                    else
                        AddToken(TokenType.StarStar);
                }
                else if (Match('='))
                    AddToken(TokenType.StarEqual);
                else
                    AddToken(TokenType.Star);
                break;
            case '%': AddToken(Match('=') ? TokenType.PercentEqual : TokenType.Percent); break;

            case '.':
                if (Match('.'))
                {
                    if (Match('<')) AddToken(TokenType.DotDotLess);
                    else if (Match('=')) AddToken(TokenType.DotDotEquals);
                    else AddToken(TokenType.DotDot); // spread operator (..)
                }
                else if (char.IsDigit(Peek()))
                    ScanLeadingDecimalNumber(); // ECMA-334 §6.4.5.4: .5 is valid real literal
                else
                    AddToken(TokenType.Dot);
                break;

            case '!':
                if (Match('='))
                {
                    AddToken(Match('=') ? TokenType.BangEqualEqual : TokenType.BangEqual);
                }
                else if (Match('~'))
                {
                    AddToken(TokenType.BangTilde);
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
                else if (Match('~'))
                {
                    AddToken(TokenType.EqualTilde);
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
                else if (Match('='))
                {
                    if (Match('>')) AddToken(TokenType.LessEqualGreater);
                    else AddToken(TokenType.LessEqual);
                }
                else AddToken(TokenType.Less);
                break;

            case '>':
                if (Match('>'))
                {
                    if (Match('>'))
                    {
                        // >>> or >>>=
                        AddToken(Match('=') ? TokenType.GreaterGreaterGreaterEqual : TokenType.GreaterGreaterGreater);
                    }
                    else
                    {
                        // >> or >>=
                        AddToken(Match('=') ? TokenType.GreaterGreaterEqual : TokenType.GreaterGreater);
                    }
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
                else if (Match('>')) AddToken(TokenType.PipeGreater);
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
                else if (Match('[')) AddToken(TokenType.QuestionLeftBracket);
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
                // Check for raw string literal (C# 11)
                if (Peek() == '"' && PeekNext() == '"')
                {
                    ScanRawStringLiteral();
                }
                else
                {
                    ScanString('"');
                }
                break;

            case '\'':
                ScanCharacter();
                break;

            case '$':
                if (Match('@'))
                {
                    if (Match('"'))
                        ScanVerbatimInterpolatedString();
                    else
                        throw LexError($"Unexpected character sequence '$@' at {_line}:{_column}. Did you mean '$@\"...'?");
                }
                else if (Match('"'))
                {
                    if (Peek() == '"' && PeekNext() == '"')
                        ScanRawInterpolatedString();
                    else
                        ScanInterpolatedString();
                }
                else
                    throw LexError($"Unexpected character '$' at {_line}:{_column}. Did you mean '$\"...'?");
                break;

            case '@':
                if (Match('$'))
                {
                    if (Match('"'))
                        ScanVerbatimInterpolatedString();
                    else
                        throw LexError($"Unexpected character sequence '@$' at {_line}:{_column}. Did you mean '@$\"...'?");
                }
                else if (Match('"'))
                    ScanVerbatimString();
                else if (char.IsLetter(Peek()) || Peek() == '_')
                    ScanIdentifier(); // §6.4.3: verbatim identifier @keyword
                else
                    throw LexError($"Unexpected character '@' at {_line}:{_column}. Did you mean '@\"...'?");
                break;

            default:
                if (char.IsDigit(c))
                    ScanNumber();
                else if (char.IsLetter(c) || c == '_')
                    ScanIdentifier();
                else
                    throw LexError($"Unexpected character '{c}' at {_line}:{_column}");
                break;
        }
    }

    private void ScanString(char quote)
    {
        var sb = new StringBuilder();
        while (Peek() != quote && !IsAtEnd())
        {
            if (Peek() == '\n')
                throw LexError($"Newline in constant at {_line}:{_column}");
            if (Peek() == '\\')
            {
                Advance();
                sb.Append(ParseEscapeSequence());
            }
            else
            {
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw LexError($"Unterminated string at {_line}:{_column}");

        Advance(); // closing quote
        AddToken(TokenType.String, sb.ToString());
    }

    private void ScanCharacter()
    {
        char value;

        if (IsAtEnd())
            throw LexError($"Unterminated character literal at {_line}:{_column}");

        if (Peek() == '\\')
        {
            Advance(); // consume backslash
            if (IsAtEnd())
                throw LexError($"Unterminated character literal at {_line}:{_column}");

            var escaped = ParseEscapeSequence(forCharacterLiteral: true);
            if (escaped.Length != 1)
                throw LexError($"Character literal must contain exactly one character at {_line}:{_column}");
            value = escaped[0];
        }
        else if (Peek() == '\'')
        {
            throw LexError($"Empty character literal at {_line}:{_column}");
        }
        else
        {
            value = Advance();
        }

        if (Peek() != '\'')
            throw LexError($"Character literal must contain exactly one character at {_line}:{_column}");

        Advance(); // closing quote
        AddToken(TokenType.Character, value);
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
            else if (Peek() == '\\' && braceDepth == 0)
            {
                Advance();
                sb.Append(ParseEscapeSequence());
            }
            else
            {
                if (Peek() == '\n') { _line++; _column = 0; }
                sb.Append(Advance());
            }
        }

        if (IsAtEnd())
            throw LexError($"Unterminated interpolated string at {_line}:{_column}");

        Advance(); // closing "
        AddToken(TokenType.InterpolatedString, sb.ToString());
    }

    private void ScanRawStringLiteral()
    {
        // First " already consumed. Count opening quotes (at least 3).
        int openQuotes = 1;
        while (Peek() == '"')
        {
            Advance();
            openQuotes++;
        }

        var isMultiLine = Peek() == '\n';
        if (isMultiLine)
        {
            _line++;
            _column = 0;
            Advance();
        }

        var sb = new StringBuilder();
        var closed = false;
        while (!IsAtEnd())
        {
            if (Peek() == '"')
            {
                int closeQuotes = 0;
                while (Peek() == '"')
                {
                    closeQuotes++;
                    Advance();
                }
                if (closeQuotes >= openQuotes)
                {
                    closed = true;
                    break;
                }
                else
                {
                    sb.Append(new string('"', closeQuotes));
                }
            }
            else
            {
                if (Peek() == '\n')
                {
                    if (!isMultiLine)
                        throw LexError(DiagnosticDescriptors.UnterminatedRawStringLiteral,
                            $"Unterminated raw string literal at {_line}:{_column}");
                    _line++;
                    _column = 0;
                }
                sb.Append(Peek());
                Advance();
            }
        }

        if (!closed)
            throw LexError(DiagnosticDescriptors.UnterminatedRawStringLiteral, $"Unterminated raw string literal at {_line}:{_column}");

        if (isMultiLine)
            AddToken(TokenType.String, StripRawStringIndentation(sb.ToString(), openQuotes));
        else
            AddToken(TokenType.String, sb.ToString());
    }

    private void ScanRawInterpolatedString()
    {
        // $" already consumed. Count remaining opening quotes (at least 2 more for """).
        int openQuotes = 1;
        while (Peek() == '"')
        {
            Advance();
            openQuotes++;
        }

        var isMultiLine = Peek() == '\n';
        if (isMultiLine)
        {
            _line++;
            _column = 0;
            Advance();
        }

        var sb = new StringBuilder();
        var closed = false;
        var braceDepth = 0;

        while (!IsAtEnd())
        {
            if (Peek() == '"' && braceDepth == 0)
            {
                int closeQuotes = 0;
                while (Peek() == '"')
                {
                    closeQuotes++;
                    Advance();
                }
                if (closeQuotes >= openQuotes)
                {
                    closed = true;
                    break;
                }
                else
                {
                    sb.Append(new string('"', closeQuotes));
                }
            }
            else if (Peek() == '{')
            {
                if (braceDepth == 0 && PeekNext() == '{')
                {
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
                if (Peek() == '\n')
                {
                    if (!isMultiLine)
                        throw LexError(DiagnosticDescriptors.UnterminatedRawStringLiteral,
                            $"Unterminated raw interpolated string at {_line}:{_column}");
                    _line++;
                    _column = 0;
                }
                sb.Append(Peek());
                Advance();
            }
        }

        if (!closed)
            throw LexError(DiagnosticDescriptors.UnterminatedRawStringLiteral, $"Unterminated raw interpolated string at {_line}:{_column}");

        var content = isMultiLine ? StripRawStringIndentation(sb.ToString(), openQuotes) : sb.ToString();
        AddToken(TokenType.InterpolatedString, content);
    }

    private static string StripRawStringIndentation(string content, int quoteCount)
    {
        // C# 11 multi-line raw string rules:
        // - The content between opening newline and closing """ has common indentation stripped
        // - The indentation is determined by the whitespace before the closing """
        // - The trailing newline before closing """ is removed

        // Content includes everything after the opening newline up to (but not including) closing quotes.
        // The last line of content is the line before the closing quotes — its trailing newline was included.
        // Remove that trailing newline.
        if (content.EndsWith("\r\n"))
            content = content[..^2];
        else if (content.EndsWith("\n"))
            content = content[..^1];

        if (content.Length == 0)
            return string.Empty;

        // Find the indentation of the last line (which was the line before closing quotes).
        // In our case, the closing quotes consumed the last line's content, so we need to
        // look at the whitespace prefix of the last line in the remaining content.
        var lastNewline = content.LastIndexOf('\n');
        var lastLine = lastNewline >= 0 ? content[(lastNewline + 1)..] : content;

        var indent = 0;
        while (indent < lastLine.Length && lastLine[indent] is ' ' or '\t')
            indent++;

        // If the last line is only whitespace, it defines the indentation to strip and is removed
        if (indent == lastLine.Length && lastNewline >= 0)
        {
            content = content[..lastNewline];
            if (indent == 0)
                return content;
        }
        else
        {
            indent = 0;
        }

        if (indent == 0)
            return content;

        // Strip common indentation from each line
        var indentPrefix = lastLine[..indent];
        var lines = content.Split('\n');
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.EndsWith("\r"))
                line = line[..^1];

            if (line.StartsWith(indentPrefix))
                sb.Append(line[indent..]);
            else
                sb.Append(line);

            if (i < lines.Length - 1)
                sb.Append('\n');
        }

        return sb.ToString();
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
            throw LexError($"Unterminated verbatim string at {_line}:{_column}");

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
            throw LexError($"Unterminated verbatim interpolated string at {_line}:{_column}");

        Advance(); // closing "
        AddToken(TokenType.InterpolatedString, sb.ToString());
    }

    private void ScanNumber()
    {
        // Check for hex (0x/0X) or binary (0b/0B) prefix
        if (_source[_start] == '0' && _current == _start + 1)
        {
            var nextChar = char.ToLowerInvariant(Peek());
            if (nextChar == 'x')
            {
                Advance(); // consume 'x'
                ScanHexNumber();
                return;
            }
            if (nextChar == 'b')
            {
                Advance(); // consume 'b'
                ScanBinaryNumber();
                return;
            }
        }

        // Consume digits and digit separators
        ScanDigitsWithSeparators(char.IsDigit);

        // Look for decimal part
        var hasDecimalPoint = false;
        if (Peek() == '.' && char.IsDigit(PeekNext()))
        {
            hasDecimalPoint = true;
            Advance(); // consume .
            ScanDigitsWithSeparators(char.IsDigit);
        }

        // Look for exponent part (e.g., 1e10, 1.5E-3)
        var hasExponent = false;
        if (char.ToLowerInvariant(Peek()) == 'e')
        {
            var next = PeekNext();
            if (char.IsDigit(next) || next == '+' || next == '-')
            {
                hasExponent = true;
                Advance(); // consume 'e' or 'E'
                if (Peek() == '+' || Peek() == '-')
                    Advance(); // consume sign
                if (!char.IsDigit(Peek()))
                    throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid exponent: expected digit after sign at {_line}:{_column}");
                ScanDigitsWithSeparators(char.IsDigit);
            }
        }

        var numberText = StripDigitSeparators(_source[_start.._current]);

        // Check for type suffix - C# supports: L, U, UL, F, D, M (case-insensitive)
        var suffix = ParseNumericSuffix();

        // Exponent notation defaults to double, but can use F/M suffix
        var isFloatingPoint = hasDecimalPoint || hasExponent;

        // Parse based on suffix and decimal point presence
        object value;
        try
        {
            value = suffix switch
            {
                NumericSuffix.Long => ParseLongWithPromotion(numberText),
                NumericSuffix.ULong => ulong.Parse(numberText),
                NumericSuffix.UInt => ParseUIntWithPromotion(numberText),
                NumericSuffix.Float => float.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
                NumericSuffix.Double => double.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
                NumericSuffix.Decimal => decimal.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
                NumericSuffix.None => isFloatingPoint
                    ? double.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture)
                    : ParseIntegerWithPromotion(numberText),
                _ => throw LexError(DiagnosticDescriptors.InvalidNumber, $"Unknown numeric suffix at {_line}:{_column}")
            };
        }
        catch (OverflowException)
        {
            throw new AlderException(DiagnosticDescriptors.IntegralConstantTooLarge, default, _line, _column);
        }

        AddToken(TokenType.Number, value);
    }

    /// <summary>
    /// Scans a leading decimal number like .5, .123, .5e10 per ECMA-334 §6.4.5.4.
    /// Called when '.' followed by digit is detected. The '.' is already at _start.
    /// </summary>
    private void ScanLeadingDecimalNumber()
    {
        // Consume the decimal digits after the dot
        ScanDigitsWithSeparators(char.IsDigit);

        // Look for exponent part (e.g., .5e10, .123E-3)
        if (char.ToLowerInvariant(Peek()) == 'e')
        {
            var next = PeekNext();
            if (char.IsDigit(next) || next == '+' || next == '-')
            {
                Advance(); // consume 'e' or 'E'
                if (Peek() == '+' || Peek() == '-')
                    Advance(); // consume sign
                if (!char.IsDigit(Peek()))
                    throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid exponent: expected digit after sign at {_line}:{_column}");
                ScanDigitsWithSeparators(char.IsDigit);
            }
        }

        var numberText = StripDigitSeparators(_source[_start.._current]);
        var suffix = ParseNumericSuffix();

        object value = suffix switch
        {
            NumericSuffix.Float => float.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            NumericSuffix.Double => double.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            NumericSuffix.Decimal => decimal.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            NumericSuffix.None => double.Parse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            _ => throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid suffix for decimal literal at {_line}:{_column}")
        };

        AddToken(TokenType.Number, value);
    }

    private void ScanDigitsWithSeparators(Func<char, bool> isValidDigit)
    {
        while (isValidDigit(Peek()) || Peek() == '_')
        {
            if (Peek() == '_')
            {
                Advance();
                if (!isValidDigit(Peek()) && Peek() != '_')
                    throw LexError(DiagnosticDescriptors.InvalidNumber, $"Digit separator '_' must be followed by a digit at {_line}:{_column}");
            }
            else
            {
                Advance();
            }
        }

        if (_current > 0 && _source[_current - 1] == '_')
            throw LexError(DiagnosticDescriptors.InvalidNumber, $"Digit separator '_' cannot appear at end of number at {_line}:{_column}");
    }

    private static string StripDigitSeparators(string text) => text.Replace("_", "");

    private void ScanHexNumber()
    {
        var hexStart = _current;
        ScanDigitsWithSeparators(IsHexDigit);

        if (_current == hexStart)
            throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid hex literal at {_line}:{_column}");

        var hexText = StripDigitSeparators(_source[hexStart.._current]);
        var suffix = ParseNumericSuffix();

        object value;
        try
        {
            value = suffix switch
            {
                NumericSuffix.Long => ParseHexLongWithPromotion(hexText),
                NumericSuffix.ULong => Convert.ToUInt64(hexText, 16),
                NumericSuffix.UInt => ParseHexUIntWithPromotion(hexText),
                NumericSuffix.None => ParseHexWithPromotion(hexText),
                _ => throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid suffix for hex literal at {_line}:{_column}")
            };
        }
        catch (OverflowException)
        {
            throw new AlderException(DiagnosticDescriptors.IntegralConstantTooLarge, default, _line, _column);
        }

        AddToken(TokenType.Number, value);
    }

    private void ScanBinaryNumber()
    {
        var binStart = _current;
        ScanDigitsWithSeparators(c => c is '0' or '1');

        if (_current == binStart)
            throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid binary literal at {_line}:{_column}");

        var binText = StripDigitSeparators(_source[binStart.._current]);
        var suffix = ParseNumericSuffix();

        object value;
        try
        {
            value = suffix switch
            {
                NumericSuffix.Long => ParseBinaryLongWithPromotion(binText),
                NumericSuffix.ULong => Convert.ToUInt64(binText, 2),
                NumericSuffix.UInt => ParseBinaryUIntWithPromotion(binText),
                NumericSuffix.None => ParseBinaryWithPromotion(binText),
                _ => throw LexError(DiagnosticDescriptors.InvalidNumber, $"Invalid suffix for binary literal at {_line}:{_column}")
            };
        }
        catch (OverflowException)
        {
            throw new AlderException(DiagnosticDescriptors.IntegralConstantTooLarge, default, _line, _column);
        }

        AddToken(TokenType.Number, value);
    }

    private static bool IsHexDigit(char c) =>
        char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';

    private string ParseEscapeSequence(bool forCharacterLiteral = false)
    {
        var escaped = Peek();
        return escaped switch
        {
            'n' => Consume('\n').ToString(),
            'r' => Consume('\r').ToString(),
            't' => Consume('\t').ToString(),
            '0' => Consume('\0').ToString(),
            'a' => Consume('\a').ToString(),
            'b' => Consume('\b').ToString(),
            'f' => Consume('\f').ToString(),
            'v' => Consume('\v').ToString(),
            '\\' => Consume('\\').ToString(),
            '"' => Consume('"').ToString(),
            '\'' => Consume('\'').ToString(),
            'u' => ParseUnicodeEscape(4, forCharacterLiteral),
            'U' => ParseUnicodeEscape(8, forCharacterLiteral),
            'x' => ParseHexEscape().ToString(),
            _ => throw LexError($"Unknown escape sequence '\\{escaped}' at {_line}:{_column}")
        };

        char Consume(char c) { Advance(); return c; }
    }

    private string ParseUnicodeEscape(int digitCount, bool forCharacterLiteral)
    {
        Advance(); // consume 'u' or 'U'
        var startCol = _column;
        var hexDigits = new StringBuilder(digitCount);

        for (var i = 0; i < digitCount; i++)
        {
            if (IsAtEnd() || !IsHexDigit(Peek()))
                throw LexError($"Invalid unicode escape sequence at {_line}:{startCol}. Expected {digitCount} hex digits.");
            hexDigits.Append(Advance());
        }

        var codePoint = Convert.ToInt32(hexDigits.ToString(), 16);

        if (digitCount == 8)
        {
            if (codePoint > 0x10FFFF)
                throw LexError($"Invalid unicode code point U+{codePoint:X8} at {_line}:{startCol}. Maximum is U+10FFFF.");
            if (forCharacterLiteral && codePoint >= 0x10000)
                throw LexError($"Surrogate pairs from \\U escapes are not supported in char literals at {_line}:{startCol}.");
        }

        if (codePoint >= 0x10000)
            return char.ConvertFromUtf32(codePoint);

        return ((char)codePoint).ToString();
    }

    private char ParseHexEscape()
    {
        Advance(); // consume 'x'
        var startCol = _column;
        var hexDigits = new StringBuilder(4);

        // \x requires at least 1 hex digit, up to 4 (ECMA-334 §6.4.5.5)
        while (hexDigits.Length < 4 && !IsAtEnd() && IsHexDigit(Peek()))
        {
            hexDigits.Append(Advance());
        }

        if (hexDigits.Length == 0)
            throw LexError($"Invalid hex escape sequence at {_line}:{startCol}. Expected at least 1 hex digit after \\x.");

        var value = Convert.ToInt32(hexDigits.ToString(), 16);
        return (char)value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, hex L-suffixed literals promote: long → ulong.
    /// </summary>
    private static object ParseHexLongWithPromotion(string hexText)
    {
        var value = Convert.ToUInt64(hexText, 16);
        if (value <= long.MaxValue) return (long)value;
        return value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, hex U-suffixed literals promote: uint → ulong.
    /// </summary>
    private static object ParseHexUIntWithPromotion(string hexText)
    {
        var value = Convert.ToUInt64(hexText, 16);
        if (value <= uint.MaxValue) return (uint)value;
        return value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, hex literals without suffix use: int → uint → long → ulong
    /// </summary>
    private static object ParseHexWithPromotion(string hexText)
    {
        var value = Convert.ToUInt64(hexText, 16);
        if (value <= int.MaxValue) return (int)value;
        if (value <= uint.MaxValue) return (uint)value;
        if (value <= long.MaxValue) return (long)value;
        return value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, binary L-suffixed literals promote: long → ulong.
    /// </summary>
    private static object ParseBinaryLongWithPromotion(string binText)
    {
        var value = Convert.ToUInt64(binText, 2);
        if (value <= long.MaxValue) return (long)value;
        return value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, binary U-suffixed literals promote: uint → ulong.
    /// </summary>
    private static object ParseBinaryUIntWithPromotion(string binText)
    {
        var value = Convert.ToUInt64(binText, 2);
        if (value <= uint.MaxValue) return (uint)value;
        return value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, binary literals without suffix use: int → uint → long → ulong
    /// </summary>
    private static object ParseBinaryWithPromotion(string binText)
    {
        var value = Convert.ToUInt64(binText, 2);
        if (value <= int.MaxValue) return (int)value;
        if (value <= uint.MaxValue) return (uint)value;
        if (value <= long.MaxValue) return (long)value;
        return value;
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, L-suffixed literals promote: long → ulong.
    /// Special case: 9223372036854775808L (|long.MinValue|) stored as ulong for negation.
    /// </summary>
    private object ParseLongWithPromotion(string text)
    {
        if (long.TryParse(text, out var longValue))
            return longValue;
        if (ulong.TryParse(text, out var ulongValue))
            return ulongValue;
        throw new AlderException(DiagnosticDescriptors.IntegralConstantTooLarge, default, _line, _column);
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, U-suffixed literals promote: uint → ulong.
    /// </summary>
    private object ParseUIntWithPromotion(string text)
    {
        if (uint.TryParse(text, out var uintValue))
            return uintValue;
        if (ulong.TryParse(text, out var ulongValue))
            return ulongValue;
        throw new AlderException(DiagnosticDescriptors.IntegralConstantTooLarge, default, _line, _column);
    }

    /// <summary>
    /// Per ECMA-334 §6.4.5.3, unsuffixed decimal integer literals promote: int → uint → long → ulong.
    /// </summary>
    private object ParseIntegerWithPromotion(string text)
    {
        if (int.TryParse(text, out var intValue))
            return intValue;
        if (uint.TryParse(text, out var uintValue))
            return uintValue;
        if (long.TryParse(text, out var longValue))
            return longValue;
        if (ulong.TryParse(text, out var ulongValue))
            return ulongValue;
        throw new AlderException(DiagnosticDescriptors.IntegralConstantTooLarge, default, _line, _column);
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
        _tokens.Add(new Token(type, text, literal, _line, _column - text.Length, _start));
    }

    private AlderException LexError(string message, int? line = null, int? column = null)
        => LexError(DiagnosticDescriptors.InvalidExpressionTerm, message, line, column);

    private AlderException LexError(DiagnosticDescriptor descriptor, string message, int? line = null, int? column = null)
        => new(descriptor, default, line ?? _line, column ?? _column, message);
}
