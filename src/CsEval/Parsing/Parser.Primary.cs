namespace CsEval.Parsing;

public sealed partial class Parser
{
    #region Primary Expressions

    private Expr ParsePrimary()
    {
        // Literals -- IsConstant=true enables ECMA-334 §10.2.11 constant expression conversions
        if (Match(TokenType.Number, TokenType.String, TokenType.Character))
            return new LiteralExpr(Previous().Literal, IsConstant: true);

        if (Match(TokenType.True))
            return new LiteralExpr(true, IsConstant: true);

        if (Match(TokenType.False))
            return new LiteralExpr(false, IsConstant: true);

        if (Match(TokenType.Null))
            return new LiteralExpr(null, IsConstant: true);

        // JavaScript undefined (maps to null in C# semantics)
        if (Match(TokenType.Undefined))
            return new LiteralExpr(null, IsConstant: true);

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
            if (Check(TokenType.LeftBrace))
            {
                Advance(); // consume '{'
                return new NewExpr(ParseAnonymousObject());
            }

            // new ClassName(args) - constructor invocation (ECMA-334 §12.8.16.2)
            if (Check(TokenType.Identifier) || IsTypeKeyword(Peek().Type))
            {
                return ParseObjectCreation();
            }

            throw new CsEvalParserException($"Expected '{{', '[', or type name after 'new' at {Peek().Line}:{Peek().Column}");
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

        // typeof(T) expression (ECMA-334 §12.8.17)
        if (Match(TokenType.Typeof))
        {
            Consume(TokenType.LeftParen, "Expected '(' after 'typeof'");
            // Accept type keywords, void, or identifiers (for non-built-in types)
            Token typeToken;
            if (Match(TokenType.Void))
            {
                typeToken = Previous();
            }
            else if (IsTypeKeyword(Peek().Type))
            {
                typeToken = Advance();
            }
            else
            {
                typeToken = Consume(TokenType.Identifier, "Expected type name after 'typeof('");
                // Support dotted type names: System.Exception, System.Collections.Generic.List
                while (Match(TokenType.Dot))
                {
                    var next = Consume(TokenType.Identifier, "Expected identifier after '.'");
                    typeToken = new Token(TokenType.Identifier, typeToken.Lexeme + "." + next.Lexeme, null, typeToken.Line, typeToken.Column);
                }
            }
            Consume(TokenType.RightParen, "Expected ')' after typeof type");
            return new TypeofExpr(typeToken);
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

    /// <summary>
    /// Parses new ClassName(args) constructor invocation.
    /// Called after 'new' has been consumed and next token is an identifier or type keyword.
    /// </summary>
    private Expr ParseObjectCreation()
    {
        // Parse type name (could be simple like Exception or dotted like System.ArgumentException)
        string typeName;
        if (IsTypeKeyword(Peek().Type))
        {
            typeName = Advance().Lexeme;
        }
        else
        {
            typeName = Consume(TokenType.Identifier, "Expected type name after 'new'").Lexeme;
            // Support dotted names: System.Exception, System.Collections.Generic.List
            while (Match(TokenType.Dot))
            {
                var next = Consume(TokenType.Identifier, "Expected identifier after '.'");
                typeName += "." + next.Lexeme;
            }
        }

        // Parse argument list
        Consume(TokenType.LeftParen, $"Expected '(' after type name '{typeName}'");
        var arguments = new List<Expr>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                arguments.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, "Expected ')' after constructor arguments");

        return new ObjectCreationExpr(typeName, arguments);
    }

    #endregion

    #region Parenthesized, Lambda, and Tuple

    private Expr ParseParenthesized()
    {
        // Could be: grouping (expr), lambda (x) => ..., parameter list (a, b) => ..., or tuple (expr1, expr2, ...)

        // Empty parens - parameterless lambda
        if (Match(TokenType.RightParen))
        {
            Consume(TokenType.Arrow, "Expected '=>' after '()'");
            var body = ParseExpression();
            return new LambdaExpr([], body);
        }

        // Try lambda first using backtracking: identifiers followed by ) =>
        var savedPosition = _current;
        var parameters = new List<Token>();
        var isLambda = false;

        if (Check(TokenType.Identifier))
        {
            // Try to parse as parameter list (all identifiers separated by commas)
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

        // Not a lambda - backtrack and parse as expression (grouping or tuple)
        _current = savedPosition;

        // Parse the first element (could be named: "name: expr")
        var firstElement = ParseTupleElement();

        // If next is comma, this is a tuple
        if (Check(TokenType.Comma))
        {
            var elements = new List<TupleElement> { firstElement };
            while (Match(TokenType.Comma))
            {
                elements.Add(ParseTupleElement());
            }
            Consume(TokenType.RightParen, "Expected ')' after tuple elements");
            return new TupleExpr(elements);
        }

        // No comma - this is grouping: (expr)
        Consume(TokenType.RightParen, "Expected ')' after expression");
        return new GroupingExpr(firstElement.Expression);
    }

    /// <summary>
    /// Parses a single tuple element, which may be named (name: expr) or unnamed (expr).
    /// For named elements, checks for "identifier :" pattern at start of element.
    /// </summary>
    private TupleElement ParseTupleElement()
    {
        // Check for named element: identifier followed by colon
        // Be careful not to confuse with ternary ?: -- in tuple element start position,
        // identifier followed by colon is a name separator.
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
        {
            var nameToken = Advance(); // consume identifier
            Advance(); // consume colon
            var expr = ParseExpression();
            return new TupleElement(nameToken.Lexeme, expr);
        }

        var expression = ParseExpression();
        return new TupleElement(null, expression);
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
