using CsEval.Parsing;

namespace CsEval.Test.Parsing;

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
    public void Tokenize_SingleQuotedString_ReturnsStringToken()
    {
        var lexer = new Lexer("'hello world'");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo("hello world"));
    }

    [Test]
    public void Tokenize_SingleQuotedStringWithEscapedQuote_ReturnsStringToken()
    {
        var lexer = new Lexer("'it\\'s working'");
        var tokens = lexer.Tokenize();

        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.String));
        Assert.That(tokens[0].Literal, Is.EqualTo("it's working"));
    }
}