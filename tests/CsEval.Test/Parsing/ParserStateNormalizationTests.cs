using CsEval.Parsing;

namespace CsEval.Test.Parsing;

[TestFixture]
public class ParserStateNormalizationTests
{
    [Test]
    public void ExtendedMode_Normalizes_NotIn_ToSingleToken()
    {
        var tokens = new Lexer("x not in y").Tokenize();
        var state = new ParserState(tokens, LanguageMode.Extended);

        Assert.That(state.Tokens.Select(t => t.Type),
            Is.EqualTo(new[] { TokenType.Identifier, TokenType.NotIn, TokenType.Identifier, TokenType.Eof }));
    }

    [Test]
    public void ExtendedMode_Normalizes_NotLike_ToSingleToken()
    {
        var tokens = new Lexer("x not like y").Tokenize();
        var state = new ParserState(tokens, LanguageMode.Extended);

        Assert.That(state.Tokens.Select(t => t.Type),
            Is.EqualTo(new[] { TokenType.Identifier, TokenType.NotLike, TokenType.Identifier, TokenType.Eof }));
    }

    [Test]
    public void StandardMode_DoesNotNormalize_NotIn_Alias()
    {
        var tokens = new Lexer("x not in y").Tokenize();
        var state = new ParserState(tokens, LanguageMode.Standard);

        Assert.That(state.Tokens.Select(t => t.Type),
            Is.EqualTo(new[] { TokenType.Identifier, TokenType.Bang, TokenType.In, TokenType.Identifier, TokenType.Eof }));
    }
}
