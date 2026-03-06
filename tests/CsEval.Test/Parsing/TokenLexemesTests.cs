using CsEval.Parsing;

namespace CsEval.Test.Parsing;

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
