namespace CsEval.Parsing.Extensions;

/// <summary>
/// Parser for anonymous object literal syntax: new { Name = "John", Age = 30 }.
/// Creates ExpandoObject (not C# anonymous types). Handles spread operator (..) within object literals.
/// Called explicitly from PrimaryParser.
/// </summary>
internal static class ObjectLiteralParser
{
    internal static Expr ParseAnonymousObject(ParserBase parser, Func<Expr> parseExpression)
    {
        var properties = new List<(Token, Expr)>();

        if (!parser.Check(TokenType.RightBrace))
        {
            do
            {
                if (parser.Match(TokenType.DotDot))
                {
                    if (parser.LanguageMode == LanguageMode.Standard)
                        throw new CsEvalParserException(
                            "Spread operator '..' is not available in Standard mode. " +
                            "Use LanguageMode.Extended to enable non-standard syntax extensions.");
                    // Spread property: ..expr
                    var spreadExpr = parseExpression();
                    // Use a special marker token for spread entries
                    var spreadMarker = new Token(TokenType.DotDot, "..", null, 0, 0);
                    properties.Add((spreadMarker, new SpreadExpr(spreadExpr)));
                }
                else
                {
                    var key = parser.Consume(TokenType.Identifier, "Expected property name");
                    parser.Consume(TokenType.Equal, "Expected '=' after property name");
                    var value = parseExpression();
                    properties.Add((key, value));
                }
            } while (parser.Match(TokenType.Comma));
        }

        parser.Consume(TokenType.RightBrace, "Expected '}' after anonymous object");
        return new ObjectLiteralExpr(properties);
    }
}
