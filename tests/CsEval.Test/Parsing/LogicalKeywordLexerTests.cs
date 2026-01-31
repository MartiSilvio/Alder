using CsEval.Parsing;

namespace CsEval.Test.Parsing;

[TestFixture]
public class LogicalKeywordLexerTests 
{
    [Test]
    public void Lexer_And_ProducesAmpAmpToken()
    {
        var lexer = new Lexer("and");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.AmpAmp));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("and"));
    }

    [Test]
    public void Lexer_Or_ProducesPipePipeToken()
    {
        var lexer = new Lexer("or");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.PipePipe));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("or"));
    }

    [Test]
    public void Lexer_Not_ProducesBangToken()
    {
        var lexer = new Lexer("not");
        var tokens = lexer.Tokenize();
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.Bang));
        Assert.That(tokens[0].Lexeme, Is.EqualTo("not"));
    }

    [Test]
    public void Lexer_TrueOrFalse_ProducesCorrectTokens()
    {
        var lexer = new Lexer("true or false");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Count, Is.EqualTo(4)); // true, or, false, EOF
        Assert.That(tokens[0].Type, Is.EqualTo(TokenType.True));
        Assert.That(tokens[1].Type, Is.EqualTo(TokenType.PipePipe));
        Assert.That(tokens[1].Lexeme, Is.EqualTo("or"));
        Assert.That(tokens[2].Type, Is.EqualTo(TokenType.False));
    }
}
