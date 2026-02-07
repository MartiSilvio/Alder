namespace CsEval.Parsing.Extensions;

/// <summary>
/// Parser for CsEval array literal syntax: [1, 2, 3] and new[] { 1, 2, 3 }.
/// Handles spread operator (..) within array literals.
/// Called explicitly from PrimaryParser.
/// </summary>
internal static class ArrayLiteralParser
{
    internal static Expr ParseArrayLiteral(ParserBase parser, Func<Expr> parseExpression)
    {
        var elements = new List<Expr>();

        if (!parser.Check(TokenType.RightBracket))
        {
            do
            {
                if (parser.Match(TokenType.DotDot))
                {
                    // Spread element: ..expr
                    var spreadExpr = parseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(parseExpression());
                }
            } while (parser.Match(TokenType.Comma));
        }

        parser.Consume(TokenType.RightBracket, "Expected ']' after array elements");
        return new ArrayLiteralExpr(elements);
    }

    internal static Expr ParseArrayLiteralBody(ParserBase parser, Func<Expr> parseExpression)
    {
        var elements = new List<Expr>();

        if (!parser.Check(TokenType.RightBrace))
        {
            do
            {
                if (parser.Match(TokenType.DotDot))
                {
                    var spreadExpr = parseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(parseExpression());
                }
            } while (parser.Match(TokenType.Comma));
        }

        parser.Consume(TokenType.RightBrace, "Expected '}' after array elements");
        return new ArrayLiteralExpr(elements);
    }
}
