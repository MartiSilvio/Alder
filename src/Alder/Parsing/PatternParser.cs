using Alder.Diagnostics;

namespace Alder.Parsing;

/// <summary>
/// Parses pattern grammar (ECMA-334 §11.2).
/// Handles type, relational, logical (and/or/not), property, var, discard, and constant patterns.
/// </summary>
internal sealed class PatternParser : ParserBase
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

        while (IsPatternKeyword(TokenType.Or))
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

        while (IsPatternKeyword(TokenType.And))
        {
            Advance(); // consume 'and' (lexed as AmpAmp)
            var right = ParseNotPattern();
            left = new AndPattern(left, right);
        }

        return left;
    }

    private Pattern ParseNotPattern()
    {
        if (IsPatternKeyword(TokenType.Not))
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
        // List pattern: [pattern, pattern, ..]
        if (Check(TokenType.LeftBracket))
        {
            return ParseListPattern();
        }

        // Discard pattern: _
        if (Check(TokenType.Identifier) &&
            string.Equals(Peek().Lexeme, TokenLexemes.DiscardIdentifier, StringComparison.Ordinal))
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

        // Parenthesized or positional (tuple) pattern: (pattern) or (pattern, pattern, ...)
        if (Check(TokenType.LeftParen) && !_expression.IsCastExpression())
        {
            Advance(); // consume '('
            var first = ParsePattern();
            if (Check(TokenType.Comma))
            {
                var subpatterns = new List<Pattern> { first };
                while (Match(TokenType.Comma))
                    subpatterns.Add(ParsePattern());
                Consume(TokenType.RightParen, "Expected ')' after positional pattern");
                return new PositionalPattern(subpatterns);
            }
            Consume(TokenType.RightParen, "Expected ')' after parenthesized pattern");
            return new ParenthesizedPattern(first);
        }

        // Null pattern
        if (Check(TokenType.Null))
        {
            var nullMark = Mark();
            Advance();
            return new ConstantPattern(new LiteralExpr(null, IsConstant: true) { Span = SpanFrom(nullMark) });
        }

        // Boolean constants
        if (Check(TokenType.True))
        {
            var trueMark = Mark();
            Advance();
            return new ConstantPattern(new LiteralExpr(true, IsConstant: true) { Span = SpanFrom(trueMark) });
        }
        if (Check(TokenType.False))
        {
            var falseMark = Mark();
            Advance();
            return new ConstantPattern(new LiteralExpr(false, IsConstant: true) { Span = SpanFrom(falseMark) });
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
                    || IsPatternKeywordToken(afterQuestion)
                    || afterQuestion.Type == TokenType.When
                    || (afterQuestion.Type == TokenType.Identifier &&
                        !string.Equals(afterQuestion.Lexeme, TokenLexemes.DiscardIdentifier, StringComparison.Ordinal)))
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
        // Use ParseBitwiseOr (not ParseExpression) to avoid consuming pattern combinators
        // (and/or/not) which live at the ParseAnd/ParseOr levels.
        var expr = _expression.ParseBitwiseOr();
        return new ConstantPattern(expr);
    }

    /// <summary>
    /// Determines if the current Identifier token starts a non-keyword type pattern.
    /// Uses lookahead: Identifier followed by another Identifier (binding variable),
    /// Identifier followed by '{' (property pattern), Identifier.Identifier (dotted type name),
    /// or Identifier&lt; (generic type like List&lt;int&gt;).
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

        // Generic type: List<int>, Dictionary<string, int>, etc.
        // In pattern context, Identifier< always starts a generic type (never a comparison)
        if (next.Type == TokenType.Less)
            return true;

        return false;
    }

    /// <summary>
    /// Parses a type name for pattern matching contexts, including dotted names
    /// and generic type arguments.
    /// Handles: Exception, System.IO.IOException, List&lt;int&gt;, Dictionary&lt;string, int&gt;
    /// Does NOT consume nullable suffix here -- nullable disambiguation is handled
    /// at the pattern level (ParsePrimaryPattern) to avoid conflicting with ternary operator.
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

        // Handle generic type arguments: List<int>, Dictionary<string, int>
        if (Check(TokenType.Less))
        {
            Advance(); // consume <
            name += "<";

            var firstArg = TryParseTypeName();
            if (firstArg == null)
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type argument after '<'");
            name += firstArg;

            while (Match(TokenType.Comma))
            {
                name += ", ";
                var nextArg = TryParseTypeName();
                if (nextArg == null)
                    throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type argument after ','");
                name += nextArg;
            }

            if (!MatchClosingAngleBracket())
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'>' after generic type arguments");
            name += ">";
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
    private bool IsPatternKeyword(TokenType keywordType)
    {
        if (IsAtEnd())
            return false;

        var token = Peek();
        if (token.Type != TokenType.Identifier)
            return false;

        return keywordType switch
        {
            TokenType.And => string.Equals(token.Lexeme, TokenLexemes.GetCanonical(TokenType.And), StringComparison.Ordinal),
            TokenType.Or => string.Equals(token.Lexeme, TokenLexemes.GetCanonical(TokenType.Or), StringComparison.Ordinal),
            TokenType.Not => string.Equals(token.Lexeme, TokenLexemes.GetCanonical(TokenType.Not), StringComparison.Ordinal),
            _ => false
        };
    }

    /// <summary>
    /// Returns true if token should NOT be consumed as a variable name in a type pattern.
    /// Pattern combinators (and, or, not), 'when' keyword, and discard '_' stop parsing.
    /// </summary>
    private static bool IsPatternCombinatorOrReserved(Token token)
    {
        if (IsPatternKeywordToken(token))
            return true;
        if (token.Type == TokenType.Identifier &&
            string.Equals(token.Lexeme, TokenLexemes.DiscardIdentifier, StringComparison.Ordinal))
            return true;
        if (token.Type == TokenType.When)
            return true;
        return false;
    }

    private static bool IsPatternKeywordToken(Token token) =>
        token.Type == TokenType.Identifier &&
        token.Lexeme is var lexeme &&
        (lexeme == TokenLexemes.GetCanonical(TokenType.And) ||
         lexeme == TokenLexemes.GetCanonical(TokenType.Or) ||
         lexeme == TokenLexemes.GetCanonical(TokenType.Not));

    private ListPattern ParseListPattern()
    {
        Consume(TokenType.LeftBracket, "Expected '[' for list pattern");
        var patterns = new List<Pattern>();

        if (!Check(TokenType.RightBracket))
        {
            patterns.Add(ParseListElement());
            while (Match(TokenType.Comma))
                patterns.Add(ParseListElement());
        }

        Consume(TokenType.RightBracket, "Expected ']' after list pattern");
        return new ListPattern(patterns);
    }

    private Pattern ParseListElement()
    {
        if (Check(TokenType.DotDot))
        {
            Advance();
            // Slice with optional subpattern: ..var rest, .._, ..pattern
            if (!Check(TokenType.Comma) && !Check(TokenType.RightBracket))
                return new SlicePattern(ParsePattern());
            return new SlicePattern(null);
        }

        return ParsePattern();
    }
}
