namespace CsEval.Parsing
{
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
                    AddToken(TokenType.Dot);
                    break;

                case '!':
                    AddToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                    break;

                case '=':
                    AddToken(Match('=') ? TokenType.EqualEqual : Match('>') ? TokenType.Arrow : TokenType.Equal);
                    break;

                case '<':
                    AddToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                    break;

                case '>':
                    AddToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                    break;

                case '&':
                    if (Match('&')) AddToken(TokenType.AmpAmp);
                    else throw new LexerException($"Unexpected character '&' at {_line}:{_column}. Did you mean '&&'?");
                    break;

                case '|':
                    if (Match('|')) AddToken(TokenType.PipePipe);
                    else throw new LexerException($"Unexpected character '|' at {_line}:{_column}. Did you mean '||'?");
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
            if (Peek() == '.' && char.IsDigit(PeekNext()))
            {
                Advance(); // consume .
                while (char.IsDigit(Peek())) Advance();
            }

            var text = _source[_start.._current];
            var value = text.Contains('.') ? double.Parse(text) : long.Parse(text);
            AddToken(TokenType.Number, value);
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
}
