namespace CsEval.Parsing;

/// <summary>
/// Parses pattern grammar (ECMA-334 §11.2).
/// Handles type, relational, logical (and/or/not), property, var, discard, and constant patterns.
/// </summary>
public sealed class PatternParser : ParserBase
{
    private ExpressionParser _expression = null!;

    internal PatternParser(ParserState state) : base(state)
    {
    }

    internal void SetExpressionParser(ExpressionParser expression) => _expression = expression;

    // ECMA-334 §11.2 - Pattern grammar
    // Precedence: or < and < not < relational < primary
    internal Pattern ParsePattern() => ParseOrPattern();

    private Pattern ParseOrPattern()
    {
        var left = ParseAndPattern();

        while (IsPatternKeyword("or"))
        {
            Advance(); // consume 'or' (lexed as PipePipe)
            var right = ParseAndPattern();
            left = new OrPattern(left, right);
        }

        return left;
    }

    private Pattern ParseAndPattern()
    {
        var left = ParseNotPattern();

        while (IsPatternKeyword("and"))
        {
            Advance(); // consume 'and' (lexed as AmpAmp)
            var right = ParseNotPattern();
            left = new AndPattern(left, right);
        }

        return left;
    }

    private Pattern ParseNotPattern()
    {
        if (IsPatternKeyword("not"))
        {
            Advance(); // consume 'not' (lexed as Bang)
            var operand = ParseNotPattern(); // right-recursive
            return new NotPattern(operand);
        }

        return ParseRelationalPattern();
    }

    private Pattern ParseRelationalPattern()
    {
        // Check for relational operators: <, <=, >, >=
        if (Check(TokenType.Less) || Check(TokenType.LessEqual) ||
            Check(TokenType.Greater) || Check(TokenType.GreaterEqual))
        {
            var op = Advance();
            var operand = _expression.ParseUnary(); // limited expression parse for constant value
            return new RelationalPattern(op, operand);
        }

        return ParsePrimaryPattern();
    }

    private Pattern ParsePrimaryPattern()
    {
        // Discard pattern: _
        if (Check(TokenType.Identifier) && Peek().Lexeme == "_")
        {
            Advance();
            return new DiscardPattern();
        }

        // Var pattern: var name
        if (Match(TokenType.Var))
        {
            var varName = Consume(TokenType.Identifier, "Expected identifier after 'var'");
            return new VarPattern(varName);
        }

        // Property pattern without type: { Name: pattern, ... }
        if (Check(TokenType.LeftBrace))
        {
            return ParsePropertyPattern(null);
        }

        // Parenthesized pattern: (pattern)
        if (Check(TokenType.LeftParen) && !_expression.IsCastExpression())
        {
            Advance(); // consume '('
            var inner = ParsePattern();
            Consume(TokenType.RightParen, "Expected ')' after parenthesized pattern");
            return new ParenthesizedPattern(inner);
        }

        // Null pattern
        if (Match(TokenType.Null))
        {
            return new ConstantPattern(new LiteralExpr(null, IsConstant: true));
        }

        // Boolean constants
        if (Match(TokenType.True))
        {
            return new ConstantPattern(new LiteralExpr(true, IsConstant: true));
        }
        if (Match(TokenType.False))
        {
            return new ConstantPattern(new LiteralExpr(false, IsConstant: true));
        }

        // Type keyword: int, string, object, etc.
        if (IsTypeKeyword(Peek().Type))
        {
            var typeToken = Advance();

            // Nullable type: int?, string?, etc.
            // Only consume '?' if NOT followed by something that looks like a ternary branch.
            if (Check(TokenType.Question) && State.Current + 1 < State.Tokens.Count)
            {
                var afterQuestion = State.Tokens[State.Current + 1];
                // '?' is nullable suffix if followed by pattern-ending tokens or combinators
                if (afterQuestion.Type is TokenType.RightParen or TokenType.Comma
                    or TokenType.RightBrace or TokenType.Arrow
                    || afterQuestion.Lexeme is "and" or "or" or "not" or "when"
                    || (afterQuestion.Type == TokenType.Identifier && afterQuestion.Lexeme != "_"))
                {
                    Advance();
                    typeToken = typeToken with { Lexeme = typeToken.Lexeme + "?" };
                }
            }

            // Type + property pattern: int { ... }
            if (Check(TokenType.LeftBrace))
            {
                return ParsePropertyPattern(typeToken);
            }

            // Type + variable binding: string s (but not 'and'/'or'/'not'/'when'/'_')
            if (Check(TokenType.Identifier) && !IsPatternCombinatorOrReserved(Peek()))
            {
                var varName = Advance();
                return new TypePattern(typeToken, varName);
            }

            // Plain type pattern: string, int, etc.
            return new TypePattern(typeToken, null);
        }

        // Non-keyword type pattern: Exception, ArgumentException, System.IO.IOException, etc.
        // Identifier followed by Identifier (not a combinator) = type pattern with binding
        // Identifier followed by '{' = property pattern with type prefix
        // Identifier alone in pattern position = possible plain type pattern (resolved at runtime)
        if (Check(TokenType.Identifier) && IsNonKeywordTypePattern())
        {
            var typeToken = ParseDottedTypeName();

            // Type + property pattern: Exception { ... }
            if (Check(TokenType.LeftBrace))
            {
                return ParsePropertyPattern(typeToken);
            }

            // Type + variable binding: Exception ex (but not 'and'/'or'/'not'/'when'/'_')
            if (Check(TokenType.Identifier) && !IsPatternCombinatorOrReserved(Peek()))
            {
                var varName = Advance();
                return new TypePattern(typeToken, varName);
            }

            // Plain type pattern: Exception (no binding)
            return new TypePattern(typeToken, null);
        }

        // Number/string/character literals and constant expressions (e.g., -1, 5 * 2)
        var expr = _expression.ParseExpression();
        return new ConstantPattern(expr);
    }

    /// <summary>
    /// Determines if the current Identifier token starts a non-keyword type pattern.
    /// Uses lookahead: Identifier followed by another Identifier (binding variable),
    /// or Identifier followed by '{' (property pattern), or Identifier.Identifier (dotted type name).
    /// Single Identifier alone is NOT treated as a type pattern to avoid ambiguity with constants.
    /// </summary>
    private bool IsNonKeywordTypePattern()
    {
        if (!Check(TokenType.Identifier))
            return false;

        var scanIndex = State.Current + 1;
        if (scanIndex >= State.Tokens.Count)
            return false;

        var next = State.Tokens[scanIndex];

        // Identifier followed by Identifier (non-combinator): type pattern with binding
        // e.g., Exception ex, ArgumentException argEx
        if (next.Type == TokenType.Identifier && !IsPatternCombinatorOrReserved(next))
            return true;

        // Identifier followed by '{': property pattern with type prefix
        // e.g., Exception { Message: "test" }
        if (next.Type == TokenType.LeftBrace)
            return true;

        // Dotted name: System.Exception, etc.
        if (next.Type == TokenType.Dot)
            return true;

        return false;
    }

    /// <summary>
    /// Parses a dotted type name (e.g., System.Exception) and returns a single token
    /// with the full name as lexeme.
    /// </summary>
    private Token ParseDottedTypeName()
    {
        var token = Consume(TokenType.Identifier, "Expected type name");
        var name = token.Lexeme;

        while (Check(TokenType.Dot) && State.Current + 1 < State.Tokens.Count
               && State.Tokens[State.Current + 1].Type == TokenType.Identifier)
        {
            Advance(); // consume '.'
            var next = Advance(); // consume identifier
            name += "." + next.Lexeme;
        }

        return token with { Lexeme = name };
    }

    internal Pattern ParsePropertyPattern(Token? typeToken)
    {
        Consume(TokenType.LeftBrace, "Expected '{' for property pattern");
        var properties = new List<(Token Name, Pattern Pattern)>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var name = Consume(TokenType.Identifier, "Expected property name in property pattern");
            Consume(TokenType.Colon, "Expected ':' after property name in property pattern");
            var pattern = ParsePattern();
            properties.Add((name, pattern));

            if (!Match(TokenType.Comma))
                break;
        }

        Consume(TokenType.RightBrace, "Expected '}' after property pattern");

        // Optional variable binding after property pattern
        Token? variableName = null;
        if (Check(TokenType.Identifier) && !IsPatternCombinatorOrReserved(Peek()))
        {
            variableName = Advance();
        }

        return new PropertyPattern(typeToken, properties, variableName);
    }

    /// <summary>
    /// Checks if the current token is a contextual keyword used in pattern matching.
    /// These keywords (and, or, not) are lexed as AmpAmp, PipePipe, Bang respectively,
    /// but have the original lexeme preserved.
    /// </summary>
    private bool IsPatternKeyword(string keyword)
    {
        if (IsAtEnd()) return false;
        return Peek().Lexeme == keyword && keyword switch
        {
            "and" => Peek().Type == TokenType.AmpAmp,
            "or" => Peek().Type == TokenType.PipePipe,
            "not" => Peek().Type == TokenType.Bang,
            _ => false
        };
    }

    /// <summary>
    /// Returns true if token should NOT be consumed as a variable name in a type pattern.
    /// Pattern combinators (and, or, not), 'when' keyword, and discard '_' stop parsing.
    /// </summary>
    private static bool IsPatternCombinatorOrReserved(Token token)
    {
        if (token.Lexeme is "and" or "or" or "not" or "_")
            return true;
        if (token.Type == TokenType.When)
            return true;
        return false;
    }
}
