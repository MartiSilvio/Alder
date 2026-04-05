using Alder.Parsing;

namespace Alder.Test.Compliance;

[TestFixture]
public class EcmaSectionParitySmokeTests
{
    [Test]
    public void S6_4_1_Tokens_General_MixedTokenStream()
    {
        var lexer = new Lexer("x + 42 == 42 ? true : false");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
        {
            TokenType.Identifier,
            TokenType.Plus,
            TokenType.Number,
            TokenType.EqualEqual,
            TokenType.Number,
            TokenType.Question,
            TokenType.True,
            TokenType.Colon,
            TokenType.False,
            TokenType.Eof
        }));
    }
}
