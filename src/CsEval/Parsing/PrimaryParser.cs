using System.Text;
using CsEval.Parsing.Extensions;

namespace CsEval.Parsing;

/// <summary>
/// Parses primary expressions: literals, identifiers, new expressions, casts, groupings,
/// lambdas, tuples, array/object literals, typeof, nameof, default, interpolated strings.
/// </summary>
public sealed class PrimaryParser : ParserBase
{
    private ExpressionParser _expression = null!;
    private StatementParser _statement = null!;

    internal PrimaryParser(ParserState state) : base(state)
    {
    }

    internal void SetExpressionParser(ExpressionParser expression) => _expression = expression;
    internal void SetStatementParser(StatementParser statement) => _statement = statement;

    #region Primary Dispatch

    internal Expr ParsePrimary()
    {
        if (Match(TokenType.Number, TokenType.String, TokenType.Character))
            return new LiteralExpr(Previous().Literal, IsConstant: true);

        if (Match(TokenType.True))
            return new LiteralExpr(true, IsConstant: true);

        if (Match(TokenType.False))
            return new LiteralExpr(false, IsConstant: true);

        if (Match(TokenType.Null))
            return new LiteralExpr(null, IsConstant: true);

        if (Match(TokenType.Undefined))
            return new LiteralExpr(null, IsConstant: true);

        if (Match(TokenType.InterpolatedString))
            return ParseInterpolatedString(Previous());

        if (Match(TokenType.New))
            return ParseNewExpression();

        if (Match(TokenType.LeftParen))
            return ParseParenthesized();

        if (Match(TokenType.LeftBracket))
            return ParseArrayLiteral();

        if (Match(TokenType.LeftBrace))
            return _statement.ParseBlock();

        if (IsTypeKeyword(Peek().Type) && PeekNext().Type == TokenType.Dot)
        {
            var typeToken = Advance();
            return new TypeReferenceExpr(typeToken);
        }

        if (Match(TokenType.Unchecked))
            return ParseCheckedUnchecked("unchecked");

        if (Match(TokenType.Checked))
            return ParseCheckedUnchecked("checked");

        if (Match(TokenType.Typeof))
            return ParseTypeofExpression();

        if (Match(TokenType.Default))
            return ParseDefaultExpression();

        if (Match(TokenType.Nameof))
            return ParseNameofExpression();

        if (Match(TokenType.Identifier))
            return ParseIdentifier();

        throw new CsEvalParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
    }

    #endregion

    #region New Expression

    private Expr ParseNewExpression()
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

        // new ClassName(args) - constructor invocation (ECMA-334 section 12.8.16.2)
        if (Check(TokenType.Identifier) || IsTypeKeyword(Peek().Type))
        {
            return ParseObjectCreation();
        }

        throw new CsEvalParserException($"Expected '{{', '[', or type name after 'new' at {Peek().Line}:{Peek().Column}");
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
                arguments.Add(_expression.ParseExpression());
            } while (Match(TokenType.Comma));
        }
        Consume(TokenType.RightParen, "Expected ')' after constructor arguments");

        return new ObjectCreationExpr(typeName, arguments);
    }

    #endregion

    #region Checked / Unchecked

    private Expr ParseCheckedUnchecked(string keyword)
    {
        Consume(TokenType.LeftParen, $"Expected '(' after '{keyword}'");
        var expr = _expression.ParseExpression();
        Consume(TokenType.RightParen, $"Expected ')' after {keyword} expression");
        return expr;
    }

    #endregion

    #region Typeof Expression

    private Expr ParseTypeofExpression()
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

    #endregion

    #region Default Expression

    private Expr ParseDefaultExpression()
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

    #endregion

    #region Nameof Expression

    private Expr ParseNameofExpression()
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

    #endregion

    #region Identifier and Lambda

    private Expr ParseIdentifier()
    {
        var identifier = Previous();

        // Check for single-parameter lambda: x => expr
        if (Match(TokenType.Arrow))
        {
            var body = _expression.ParseExpression();
            return new LambdaExpr([identifier], body);
        }

        return new IdentifierExpr(identifier);
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
            var body = _expression.ParseExpression();
            return new LambdaExpr([], body);
        }

        // Try lambda first using backtracking: identifiers followed by ) =>
        var savedPosition = State.Current;
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
            var body = _expression.ParseExpression();
            return new LambdaExpr(parameters, body);
        }

        // Not a lambda - backtrack and parse as expression (grouping or tuple)
        State.Current = savedPosition;

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
    /// </summary>
    private TupleElement ParseTupleElement()
    {
        // Check for named element: identifier followed by colon
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
        {
            var nameToken = Advance(); // consume identifier
            Advance(); // consume colon
            var expr = _expression.ParseExpression();
            return new TupleElement(nameToken.Lexeme, expr);
        }

        var expression = _expression.ParseExpression();
        return new TupleElement(null, expression);
    }

    #endregion

    #region Collection Literals

    // CsEval Extension: array literal [1, 2, 3]
    private Expr ParseArrayLiteral() =>
        ArrayLiteralParser.ParseArrayLiteral(this, () => _expression.ParseExpression());

    // CsEval Extension: array literal body (for new[] { ... } syntax)
    private Expr ParseArrayLiteralBody() =>
        ArrayLiteralParser.ParseArrayLiteralBody(this, () => _expression.ParseExpression());

    // CsEval Extension: anonymous object new { Name = "John" }
    private Expr ParseAnonymousObject() =>
        ObjectLiteralParser.ParseAnonymousObject(this, () => _expression.ParseExpression());

    #endregion

    #region Interpolated Strings

    private InterpolatedStringExpr ParseInterpolatedString(Token token)
    {
        var content = (string)token.Literal!;
        var parts = new List<InterpolatedPart>();
        var sb = new StringBuilder();
        var i = 0;

        while (i < content.Length)
        {
            switch (content[i])
            {
                // Check for escaped brace {{
                case '{' when i + 1 < content.Length && content[i + 1] == '{':
                    sb.Append('{');
                    i += 2;
                    continue;
                case '{':
                {
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
                        switch (content[i])
                        {
                            case '{':
                                braceDepth++;
                                break;
                            case '}':
                                braceDepth--;
                                break;
                        }

                        if (braceDepth > 0) i++;
                    }

                    var exprText = content[exprStart..i];
                    i++; // skip }

                    var lexer = new Lexer(exprText);
                    var parserTokens = lexer.Tokenize();
                    var subParser = ExpressionParser.CreateForSubExpression(parserTokens);
                    var expr = subParser.Parse();
                    parts.Add(new ExpressionPart(expr));
                    break;
                }
                // Check for escaped brace }}
                case '}' when i + 1 < content.Length && content[i + 1] == '}':
                    sb.Append('}');
                    i += 2;
                    continue;
                // Single } outside expression - just append it
                case '}':
                    sb.Append(content[i]);
                    i++;
                    break;
                default:
                    sb.Append(content[i]);
                    i++;
                    break;
            }
        }

        if (sb.Length > 0)
            parts.Add(new TextPart(sb.ToString()));

        return new InterpolatedStringExpr(parts);
    }

    #endregion
}
