using Alder.Diagnostics;

namespace Alder.Parsing.Extensions;

/// <summary>
/// Parser for anonymous object literal syntax: new { Name = "John", Age = 30 }.
/// Creates ExpandoObject (not C# anonymous types). Handles spread operator (..) within object literals.
/// Called explicitly from PrimaryParser.
/// </summary>
internal static class ObjectLiteralParser
{
    internal static Expr ParseAnonymousObject(ParserBase parser, Func<Expr> parseExpression)
    {
        var mark = parser.Mark();
        var properties = new List<(Token, Expr)>();

        if (!parser.Check(TokenType.RightBrace))
        {
            do
            {
                if (parser.Match(TokenType.DotDot))
                {
                    if (parser.LanguageMode == LanguageMode.Standard)
                        throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired,TokenLexemes.GetCanonical(TokenType.DotDot));
                    // Spread property: ..expr
                    var spreadMark = parser.Mark();
                    var spreadExpr = parseExpression();
                    // Use a special marker token for spread entries
                    var spreadMarker = TokenLexemes.CreateSynthetic(TokenType.DotDot, parser.Previous());
                    properties.Add((spreadMarker, new SpreadExpr(spreadExpr) { Span = parser.SpanFrom(spreadMark) }));
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
        return new ObjectLiteralExpr(properties) { Span = parser.SpanFrom(mark) };
    }
}
