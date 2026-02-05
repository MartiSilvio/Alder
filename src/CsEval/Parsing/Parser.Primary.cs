namespace CsEval.Parsing;

public sealed partial class Parser
{
    #region Primary Expressions

    private Expr ParsePrimary()
    {
        // Literals
        if (Match(TokenType.Number, TokenType.String, TokenType.Character))
            return new LiteralExpr(Previous().Literal);

        if (Match(TokenType.True))
            return new LiteralExpr(true);

        if (Match(TokenType.False))
            return new LiteralExpr(false);

        if (Match(TokenType.Null))
            return new LiteralExpr(null);

        // JavaScript undefined (maps to null in C# semantics)
        if (Match(TokenType.Undefined))
            return new LiteralExpr(null);

        // Interpolated string
        if (Match(TokenType.InterpolatedString))
            return ParseInterpolatedString(Previous());

        if (Match(TokenType.New))
        {
            // new[] { ... } - implicitly typed array
            if (Match(TokenType.LeftBracket))
            {
                Consume(TokenType.RightBracket, "Expected ']' after 'new['");
                Consume(TokenType.LeftBrace, "Expected '{' after 'new[]'");
                return ParseArrayLiteralBody();
            }

            // new { ... } - anonymous object
            Consume(TokenType.LeftBrace, "Expected '{' or '[' after 'new'");
            return new NewExpr(ParseAnonymousObject());
        }

        // Grouping, lambda, or tuple
        if (Match(TokenType.LeftParen))
            return ParseParenthesized();

        // Array literal: new[] { } or [ ] - support both
        if (Match(TokenType.LeftBracket))
            return ParseArrayLiteral();

        // Block expression: { statements; return expr; }
        if (Match(TokenType.LeftBrace))
            return ParseBlock();

        // Type keyword followed by . for static member access (double.NaN, int.MaxValue)
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type == TokenType.Dot)
        {
            var typeToken = Advance();
            return new TypeReferenceExpr(typeToken);
        }

        // unchecked(expr) - pass through (CsEval operates in unchecked mode by default)
        if (Match(TokenType.Unchecked))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'unchecked'");
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after unchecked expression");
            return expr;
        }

        // checked(expr) - pass through (overflow checking not enforced at runtime)
        if (Match(TokenType.Checked))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'checked'");
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after checked expression");
            return expr;
        }

        // default(T) or default literal (ECMA-334 §12.8.20)
        if (Match(TokenType.Default))
        {
            if (Match(TokenType.LeftParen))
            {
                // default(Type) or default(Type?) - typed default
                var typeToken = Consume(IsTypeKeyword(Peek().Type) ? Peek().Type : TokenType.Identifier,
                    "Expected type after 'default('");
                // Handle nullable type suffix (e.g., int? -> int?)
                if (Match(TokenType.Question))
                {
                    typeToken = new Token(typeToken.Type, typeToken.Lexeme + "?", null, typeToken.Line, typeToken.Column);
                }
                Consume(TokenType.RightParen, "Expected ')' after default type");
                return new DefaultExpr(typeToken);
            }
            // bare default literal (C# 7.1+)
            return new DefaultExpr(null);
        }

        // nameof(expression) - returns the name of the final identifier (ECMA-334 §12.8.22)
        if (Match(TokenType.Nameof))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'nameof'");
            // Parse the name chain (x, x.y, x.y.z, etc.)
            var name = Consume(TokenType.Identifier, "Expected identifier after 'nameof('").Lexeme;
            while (Match(TokenType.Dot))
            {
                name = Consume(TokenType.Identifier, "Expected identifier after '.'").Lexeme;
            }
            Consume(TokenType.RightParen, "Expected ')' after nameof expression");
            return new NameofExpr(name);
        }

        // Identifier or single-parameter lambda (x => ...)
        if (Match(TokenType.Identifier))
        {
            var identifier = Previous();

            // Check for single-parameter lambda: x => expr
            if (Match(TokenType.Arrow))
            {
                var body = ParseExpression();
                return new LambdaExpr([identifier], body);
            }

            return new IdentifierExpr(identifier);
        }

        throw new CsEvalParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
    }

    #endregion

    #region Parenthesized and Lambda

    private Expr ParseParenthesized()
    {
        // Could be: grouping (expr), lambda (x) => ..., or parameter list (a, b) => ...

        // Empty parens - parameterless lambda
        if (Match(TokenType.RightParen))
        {
            Consume(TokenType.Arrow, "Expected '=>' after '()'");
            var body = ParseExpression();
            return new LambdaExpr([], body);
        }

        // Check if this looks like a lambda parameter list
        var savedPosition = _current;
        var parameters = new List<Token>();
        var isLambda = false;

        if (Check(TokenType.Identifier))
        {
            // Try to parse as parameter list
            parameters.Add(Advance());
            while (Match(TokenType.Comma))
            {
                if (!Check(TokenType.Identifier))
                    break;
                parameters.Add(Advance());
            }

            if (Match(TokenType.RightParen) && Match(TokenType.Arrow))
            {
                isLambda = true;
            }
        }

        if (isLambda)
        {
            var body = ParseExpression();
            return new LambdaExpr(parameters, body);
        }

        // Not a lambda - backtrack and parse as grouping
        _current = savedPosition;
        var expr = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after expression");
        return new GroupingExpr(expr);
    }

    #endregion

    #region Collection Literals

    private Expr ParseArrayLiteral()
    {
        var elements = new List<Expr>();

        if (!Check(TokenType.RightBracket))
        {
            do
            {
                if (Match(TokenType.DotDotDot))
                {
                    // Spread element: ...expr
                    var spreadExpr = ParseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(ParseExpression());
                }
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBracket, "Expected ']' after array elements");
        return new ArrayLiteralExpr(elements);
    }

    private Expr ParseArrayLiteralBody()
    {
        var elements = new List<Expr>();

        if (!Check(TokenType.RightBrace))
        {
            do
            {
                if (Match(TokenType.DotDotDot))
                {
                    var spreadExpr = ParseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(ParseExpression());
                }
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBrace, "Expected '}' after array elements");
        return new ArrayLiteralExpr(elements);
    }

    private Expr ParseAnonymousObject()
    {
        var properties = new List<(Token, Expr)>();

        if (!Check(TokenType.RightBrace))
        {
            do
            {
                if (Match(TokenType.DotDotDot))
                {
                    // Spread property: ...expr
                    var spreadExpr = ParseExpression();
                    // Use a special marker token for spread entries
                    var spreadMarker = new Token(TokenType.DotDotDot, "...", null, 0, 0);
                    properties.Add((spreadMarker, new SpreadExpr(spreadExpr)));
                }
                else
                {
                    var key = Consume(TokenType.Identifier, "Expected property name");
                    Consume(TokenType.Equal, "Expected '=' after property name");
                    var value = ParseExpression();
                    properties.Add((key, value));
                }
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBrace, "Expected '}' after anonymous object");
        return new ObjectLiteralExpr(properties);
    }

    #endregion

    #region Interpolated Strings

    private Expr ParseInterpolatedString(Token token)
    {
        var content = (string)token.Literal!;
        var parts = new List<InterpolatedPart>();
        var sb = new StringBuilder();
        var i = 0;

        while (i < content.Length)
        {
            if (content[i] == '{')
            {
                // Check for escaped brace {{
                if (i + 1 < content.Length && content[i + 1] == '{')
                {
                    sb.Append('{');
                    i += 2;
                    continue;
                }

                if (sb.Length > 0)
                {
                    parts.Add(new TextPart(sb.ToString()));
                    sb.Clear();
                }

                i++; // skip {
                var exprStart = i;
                var braceDepth = 1;

                while (i < content.Length && braceDepth > 0)
                {
                    if (content[i] == '{') braceDepth++;
                    else if (content[i] == '}') braceDepth--;
                    if (braceDepth > 0) i++;
                }

                var exprText = content[exprStart..i];
                i++; // skip }

                var lexer = new Lexer(exprText);
                var parserTokens = lexer.Tokenize();
                var parser = new Parser(parserTokens);
                var expr = parser.Parse();
                parts.Add(new ExpressionPart(expr));
            }
            else if (content[i] == '}')
            {
                // Check for escaped brace }}
                if (i + 1 < content.Length && content[i + 1] == '}')
                {
                    sb.Append('}');
                    i += 2;
                    continue;
                }

                // Single } outside expression - just append it
                sb.Append(content[i]);
                i++;
            }
            else
            {
                sb.Append(content[i]);
                i++;
            }
        }

        if (sb.Length > 0)
            parts.Add(new TextPart(sb.ToString()));

        return new InterpolatedStringExpr(parts);
    }

    #endregion
}
