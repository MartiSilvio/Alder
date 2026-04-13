using Alder.Diagnostics;
using Alder.Parsing;

namespace Alder.Test.Parsing;

[TestFixture]
public class LexerTests
{
    [Test]
    public void Tokenize_Number_ReturnsNumberToken()
    {
        var lexer = new Lexer("42");
        var tokens = lexer.Tokenize();

        Assert.That(tokens, Has.Count.EqualTo(2));
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Literal, Is.EqualTo(42));
    }

    [Test]
    public void Tokenize_Decimal_ReturnsNumberToken()
    {
        var lexer = new Lexer("3.14");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Literal, Is.EqualTo(3.14));
    }

    [Test]
    public void Tokenize_String_ReturnsStringToken()
    {
        var lexer = new Lexer("\"hello world\"");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo("hello world"));
    }

    [Test]
    public void Tokenize_InterpolatedString_ReturnsInterpolatedStringToken()
    {
        var lexer = new Lexer("$\"Hello {name}!\"");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.InterpolatedString));
        Assert.That(tokens[0].Literal, Is.EqualTo("Hello {name}!"));
    }

    [Test]
    public void Tokenize_Operators_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("+ - * / % == != < <= > >= && || !");
        var tokens = lexer.Tokenize();

        var expected = new[]
        {
            TokenType.Plus, TokenType.Minus, TokenType.Star, TokenType.Slash,
            TokenType.Percent, TokenType.EqualEqual, TokenType.BangEqual,
            TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual,
            TokenType.AmpAmp, TokenType.PipePipe, TokenType.Bang, TokenType.Eof
        };

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(expected));
    }

    [Test]
    public void Tokenize_Arrow_ReturnsArrowToken()
    {
        var lexer = new Lexer("=>");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Arrow));
    }

    [Test]
    public void Tokenize_NullCoalesce_ReturnsQuestionQuestionToken()
    {
        var lexer = new Lexer("??");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.QuestionQuestion));
    }

    [Test]
    public void Tokenize_NullSafeAccess_ReturnsQuestionDotToken()
    {
        var lexer = new Lexer("?.");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.QuestionDot));
    }

    [Test]
    public void Tokenize_Keywords_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("true false null var return new if else");
        var tokens = lexer.Tokenize();

        var expected = new[]
        {
            TokenType.True, TokenType.False, TokenType.Null,
            TokenType.Var, TokenType.Return, TokenType.New,
            TokenType.If, TokenType.Else, TokenType.Eof
        };

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(expected));
    }

    [Test]
    public void Tokenize_Identifier_ReturnsIdentifierToken()
    {
        var lexer = new Lexer("value_123");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("value_123"));
    }

    [Test]
    public void Tokenize_Comment_IsIgnored()
    {
        var lexer = new Lexer("1 // this is a comment\n2");
        var tokens = lexer.Tokenize();

        Assert.That(tokens, Has.Count.EqualTo(3));
        Assert.That(tokens[0].Literal, Is.EqualTo(1));
        Assert.That(tokens[1].Literal, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_MultiLineComment_IsIgnored()
    {
        var lexer = new Lexer("1 /* comment */ 2");
        var tokens = lexer.Tokenize();

        Assert.That(tokens, Has.Count.EqualTo(3));
        Assert.That(tokens[0].Literal, Is.EqualTo(1));
        Assert.That(tokens[1].Literal, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_DelimitedComments_DoNotNest()
    {
        var lexer = new Lexer("1 /* outer /* inner */ + 2");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
        {
            TokenType.Number, TokenType.Plus, TokenType.Number, TokenType.Eof
        }));
        Assert.That(tokens[0].Literal, Is.EqualTo(1));
        Assert.That(tokens[2].Literal, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_LineTerminators_CRLF_SeparateTokens()
    {
        var lexer = new Lexer("1\r\n2");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
        {
            TokenType.Number, TokenType.Number, TokenType.Eof
        }));
        Assert.That(tokens[0].Literal, Is.EqualTo(1));
        Assert.That(tokens[1].Literal, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_Whitespace_TabAndCarriageReturn_AreIgnored()
    {
        var lexer = new Lexer("1\t+\r 2");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
        {
            TokenType.Number, TokenType.Plus, TokenType.Number, TokenType.Eof
        }));
        Assert.That(tokens[0].Literal, Is.EqualTo(1));
        Assert.That(tokens[2].Literal, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_DoubleQuotedString_ReturnsStringToken()
    {
        var lexer = new Lexer("\"hello world\"");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo("hello world"));
    }

    [Test]
    public void Tokenize_DoubleQuotedStringWithEscapedQuote_ReturnsStringToken()
    {
        var lexer = new Lexer("\"it's working\"");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo("it's working"));
    }

    #region Escape Sequences

    [TestCase(@"""\0""", "\0", TestName = "NullEscape")]
    [TestCase(@"""\a""", "\a", TestName = "AlertEscape")]
    [TestCase(@"""\b""", "\b", TestName = "BackspaceEscape")]
    [TestCase(@"""\f""", "\f", TestName = "FormFeedEscape")]
    [TestCase(@"""\v""", "\v", TestName = "VerticalTabEscape")]
    [TestCase(@"""\t""", "\t", TestName = "TabEscape")]
    [TestCase(@"""\n""", "\n", TestName = "NewlineEscape")]
    [TestCase(@"""\r""", "\r", TestName = "CarriageReturnEscape")]
    [TestCase(@"""\\""", "\\", TestName = "BackslashEscape")]
    public void Tokenize_EscapeSequence_ReturnsCorrectValue(string input, string expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    [Test]
    public void Tokenize_RegularStringContainingRawNewline_Throws()
    {
        var lexer = new Lexer("\"line1\nline2\"");
        var ex = Assert.Throws<AlderException>(() => lexer.Tokenize());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1010));
    }

    [TestCase(@"""\{""")]
    [TestCase(@"'\{'")]
    public void Tokenize_InvalidEscapeSequence_Throws(string input)
    {
        var lexer = new Lexer(input);
        var ex = Assert.Throws<AlderException>(() => lexer.Tokenize());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1009));
    }

    [Test]
    public void Tokenize_UnicodeSupplementaryEscape_ReturnsCorrectString()
    {
        var lexer = new Lexer(@"""\U0001F600""");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo(char.ConvertFromUtf32(0x1F600)));
    }

    [Test]
    public void Tokenize_UnterminatedRawString_Throws()
    {
        var lexer = new Lexer("\"\"\"abc");
        var ex = Assert.Throws<AlderException>(() => lexer.Tokenize());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8997));
    }

    [TestCase(@"$""Hello\tWorld""", "Hello\tWorld", TestName = "InterpolatedTabEscape")]
    [TestCase(@"$""Line1\nLine2""", "Line1\nLine2", TestName = "InterpolatedNewlineEscape")]
    [TestCase(@"$""Bell\a""", "Bell\a", TestName = "InterpolatedAlertEscape")]
    [TestCase(@"$""Null\0Char""", "Null\0Char", TestName = "InterpolatedNullEscape")]
    public void Tokenize_InterpolatedEscapeSequence_ReturnsCorrectValue(string input, string expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.InterpolatedString));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    #endregion

    #region Character Literals

    [TestCase("'a'", 'a', TestName = "CharLiteralA")]
    [TestCase("'Z'", 'Z', TestName = "CharLiteralZ")]
    [TestCase("'0'", '0', TestName = "CharLiteralDigit")]
    [TestCase("' '", ' ', TestName = "CharLiteralSpace")]
    public void Tokenize_CharLiteral_ReturnsCorrectValue(string input, char expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Character));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    [TestCase(@"'\n'", '\n', TestName = "CharEscapeNewline")]
    [TestCase(@"'\t'", '\t', TestName = "CharEscapeTab")]
    [TestCase(@"'\r'", '\r', TestName = "CharEscapeCarriageReturn")]
    [TestCase(@"'\0'", '\0', TestName = "CharEscapeNull")]
    [TestCase(@"'\\'", '\\', TestName = "CharEscapeBackslash")]
    [TestCase(@"'\''", '\'', TestName = "CharEscapeSingleQuote")]
    [TestCase(@"'\a'", '\a', TestName = "CharEscapeAlert")]
    [TestCase(@"'\b'", '\b', TestName = "CharEscapeBackspace")]
    [TestCase(@"'\f'", '\f', TestName = "CharEscapeFormFeed")]
    [TestCase(@"'\v'", '\v', TestName = "CharEscapeVerticalTab")]
    public void Tokenize_CharLiteralEscape_ReturnsCorrectValue(string input, char expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Character));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    [Test]
    public void Tokenize_EmptyCharLiteral_Throws()
    {
        var lexer = new Lexer("''");
        var ex = Assert.Throws<AlderException>(() => lexer.Tokenize());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1011));
    }

    [Test]
    public void Tokenize_MultiCharLiteral_Throws()
    {
        var lexer = new Lexer("'ab'");
        var ex = Assert.Throws<AlderException>(() => lexer.Tokenize());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1012));
    }

    #endregion

    #region Exponent Validation

    [TestCase("1e+", TestName = "ExponentPlusNoDigits")]
    [TestCase("1e-", TestName = "ExponentMinusNoDigits")]
    [TestCase(".5e+", TestName = "LeadingDecimalExponentPlusNoDigits")]
    [TestCase(".5e-", TestName = "LeadingDecimalExponentMinusNoDigits")]
    public void Tokenize_InvalidExponent_Throws(string input)
    {
        var lexer = new Lexer(input);
        var ex = Assert.Throws<AlderException>(() => lexer.Tokenize());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1013));
    }

    [TestCase("1e10", 1e10, TestName = "ExponentNoSign")]
    [TestCase("1e+10", 1e+10, TestName = "ExponentPlusDigits")]
    [TestCase("1.5E-3", 1.5E-3, TestName = "ExponentMinusDigits")]
    [TestCase(".5e2", .5e2, TestName = "LeadingDecimalExponent")]
    public void Tokenize_ValidExponent_ReturnsCorrectValue(string input, double expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    #endregion

    #region Hex and Binary Literals

    [TestCase("0xFF", 255, TestName = "HexFF")]
    [TestCase("0x1A", 26, TestName = "Hex1A")]
    [TestCase("0x0", 0, TestName = "Hex0")]
    [TestCase("0xABCD", 0xABCD, TestName = "HexABCD")]
    [TestCase("0xabcd", 0xabcd, TestName = "HexLowercase")]
    [TestCase("0X10", 16, TestName = "HexUppercaseX")]
    public void Tokenize_HexLiteral_ReturnsCorrectValue(string input, int expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    [TestCase("0xFFFFFFFFL", 0xFFFFFFFFL, TestName = "HexLong")]
    [TestCase("0xFFFFFFFFU", 0xFFFFFFFFU, TestName = "HexUInt")]
    [TestCase("0xFFFFFFFFFFFFFFFFUL", 0xFFFFFFFFFFFFFFFFUL, TestName = "HexULong")]
    public void Tokenize_HexLiteralWithSuffix_ReturnsCorrectValue(string input, object expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    [TestCase("0b1010", 10, TestName = "Binary1010")]
    [TestCase("0b0", 0, TestName = "Binary0")]
    [TestCase("0b1", 1, TestName = "Binary1")]
    [TestCase("0b11111111", 255, TestName = "Binary255")]
    [TestCase("0B1010", 10, TestName = "BinaryUppercaseB")]
    public void Tokenize_BinaryLiteral_ReturnsCorrectValue(string input, int expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Number));
        Assert.That(tokens[0].Literal, Is.EqualTo(expected));
    }

    #endregion
}

[TestFixture]
public class TokenLexemesTests
{
    [TestCase(nameof(TokenType.PipeGreater), "|>")]
    [TestCase(nameof(TokenType.StarStar), "**")]
    [TestCase(nameof(TokenType.StarStarEqual), "**=")]
    [TestCase(nameof(TokenType.In), "in")]
    [TestCase(nameof(TokenType.NotIn), "not in")]
    [TestCase(nameof(TokenType.Like), "like")]
    [TestCase(nameof(TokenType.NotLike), "not like")]
    [TestCase(nameof(TokenType.EqualTilde), "=~")]
    [TestCase(nameof(TokenType.BangTilde), "!~")]
    [TestCase(nameof(TokenType.LessEqualGreater), "<=>")]
    [TestCase(nameof(TokenType.DotDot), "..")]
    [TestCase(nameof(TokenType.DotDotLess), "..<")]
    [TestCase(nameof(TokenType.DotDotEquals), "..=")]
    [TestCase(nameof(TokenType.QuestionQuestionEqual), "??=")]
    public void GetCanonical_ReturnsExpectedLexeme(string tokenTypeName, string expected)
    {
        var type = Enum.Parse<TokenType>(tokenTypeName);
        Assert.That(TokenLexemes.GetCanonical(type), Is.EqualTo(expected));
    }

    [Test]
    public void CreateSynthetic_UsesCanonicalLexemeAndCoordinates()
    {
        var token = TokenLexemes.CreateSynthetic(TokenType.NotLike, line: 17, column: 29);

        Assert.That(token.Type, Is.EqualTo(TokenType.NotLike));
        Assert.That(token.Lexeme, Is.EqualTo("not like"));
        Assert.That(token.Line, Is.EqualTo(17));
        Assert.That(token.Column, Is.EqualTo(29));
    }
}

[TestFixture]
public class LogicalKeywordLexerTests
{
    [Test]
    public void Lexer_And_ProducesIdentifierToken()
    {
        var lexer = new Lexer("and");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("and"));
    }

    [Test]
    public void Lexer_Or_ProducesIdentifierToken()
    {
        var lexer = new Lexer("or");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("or"));
    }

    [Test]
    public void Lexer_Not_ProducesIdentifierToken()
    {
        var lexer = new Lexer("not");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("not"));
    }

    [Test]
    public void Lexer_TrueOrFalse_ProducesCorrectTokens()
    {
        var lexer = new Lexer("true or false");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Count, Is.EqualTo(4)); // true, or, false, EOF
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.True));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[1].Lexeme, Is.EqualTo("or"));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.False));
    }
}
