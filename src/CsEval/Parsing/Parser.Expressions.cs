namespace CsEval.Parsing;

public sealed partial class Parser
{
    #region Expression Precedence

    private Expr ParseExpression() => ParseAssignment();

    private Expr ParseAssignment()
    {
        var expr = ParseConditional();

        if (expr is IdentifierExpr identifier)
        {
            // Handle ??= as an expression (for use in return statements, etc.)
            if (Match(TokenType.QuestionQuestionEqual))
            {
                var value = ParseAssignment();
                return new NullCoalesceAssignExpr(identifier.Name, value);
            }

            // Handle = assignment
            if (Match(TokenType.Equal))
            {
                var value = ParseAssignment();
                return new AssignExpr(identifier.Name, value);
            }

            // Handle compound assignment operators: +=, -=, *=, /=, %=, &=, |=, ^=, <<=, >>=
            if (MatchCompoundAssignment(out var op))
            {
                var value = ParseAssignment();
                return new CompoundAssignExpr(identifier.Name, op, value);
            }
        }
        else if (expr is MemberAccessExpr memberAccess)
        {
            // Handle obj.Property = value
            if (Match(TokenType.Equal))
            {
                var value = ParseAssignment();
                return new MemberAssignExpr(memberAccess.Object, memberAccess.Name, value);
            }
        }
        else if (expr is IndexAccessExpr indexAccess)
        {
            // Handle arr[0] = value
            if (Match(TokenType.Equal))
            {
                var value = ParseAssignment();
                return new IndexAssignExpr(indexAccess.Object, indexAccess.Index, value);
            }
        }

        return expr;
    }

    private Expr ParseConditional()
    {
        var expr = ParseNullCoalesce();

        if (Match(TokenType.Question))
        {
            var thenBranch = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' in ternary expression");
            var elseBranch = ParseExpression();
            return new ConditionalExpr(expr, thenBranch, elseBranch);
        }

        return expr;
    }

    private Expr ParseNullCoalesce()
    {
        var expr = ParseOr();

        if (Match(TokenType.QuestionQuestion))
        {
            var right = ParseNullCoalesce();
            return new NullCoalesceExpr(expr, right);
        }

        return expr;
    }

    #endregion

    #region Logical Operators

    private Expr ParseOr()
    {
        var expr = ParseAnd();

        while (Match(TokenType.PipePipe))
        {
            var op = Previous();
            var right = ParseAnd();
            expr = new LogicalExpr(expr, op, right);
        }

        return expr;
    }

    private Expr ParseAnd()
    {
        var expr = ParseBitwiseOr();

        while (Match(TokenType.AmpAmp))
        {
            var op = Previous();
            var right = ParseBitwiseOr();
            expr = new LogicalExpr(expr, op, right);
        }

        return expr;
    }

    #endregion

    #region Bitwise Operators

    private Expr ParseBitwiseOr()
    {
        var expr = ParseBitwiseXor();

        while (Match(TokenType.Pipe))
        {
            var op = Previous();
            var right = ParseBitwiseXor();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    private Expr ParseBitwiseXor()
    {
        var expr = ParseBitwiseAnd();

        while (Match(TokenType.Caret))
        {
            var op = Previous();
            var right = ParseBitwiseAnd();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    private Expr ParseBitwiseAnd()
    {
        var expr = ParseEquality();

        while (Match(TokenType.Amp))
        {
            var op = Previous();
            var right = ParseEquality();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    #endregion

    #region Comparison Operators

    private Expr ParseEquality()
    {
        var expr = ParseComparison();

        // Support both == and === (JavaScript), != and !== (JavaScript)
        // === and !== are treated the same as == and != in C# semantics
        while (Match(TokenType.EqualEqual, TokenType.BangEqual,
                     TokenType.EqualEqualEqual, TokenType.BangEqualEqual))
        {
            var op = Previous();
            var right = ParseComparison();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    private Expr ParseComparison()
    {
        var expr = ParseShift();

        while (true)
        {
            if (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
            {
                var op = Previous();
                var right = ParseShift();
                expr = new BinaryExpr(expr, op, right);
            }
            else if (Match(TokenType.Is))
            {
                expr = ParseIsExpression(expr);
            }
            else if (Match(TokenType.As))
            {
                expr = ParseAsExpression(expr);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private Expr ParseIsExpression(Expr left)
    {
        // x is null
        if (Match(TokenType.Null))
            return new IsExpr(left, null, false);

        // x is not null OR x is not type (note: "not" is lexed as Bang)
        if (Check(TokenType.Bang) && Peek().Lexeme == "not")
        {
            Advance();
            // x is not null
            if (Match(TokenType.Null))
                return new IsExpr(left, null, true);
            // x is not type
            if (MatchTypeKeywordNoNullable(out var negatedType))
                return new IsExpr(left, negatedType, true);
            throw new CsEvalParserException($"Expected type or 'null' after 'is not' at {Peek().Line}:{Peek().Column}");
        }

        // x is var name (ECMA-334 §11.2.4 - var pattern always matches)
        if (Match(TokenType.Var))
        {
            var varToken = Previous();
            var varName = Consume(TokenType.Identifier, "Expected identifier after 'is var'");
            return new IsExpr(left, varToken, false, varName);
        }

        // x is type OR x is type name
        if (MatchTypeKeywordNoNullable(out var typeToken))
        {
            Token? varName = null;
            if (Check(TokenType.Identifier))
                varName = Advance();
            return new IsExpr(left, typeToken, false, varName);
        }

        throw new CsEvalParserException($"Expected type or 'null' after 'is' at {Peek().Line}:{Peek().Column}");
    }

    private bool MatchTypeKeywordNoNullable(out Token typeToken)
    {
        if (IsTypeKeyword(Peek().Type))
        {
            typeToken = Advance();
            return true;
        }
        typeToken = default;
        return false;
    }

    private Expr ParseAsExpression(Expr left)
    {
        // x as type
        if (MatchTypeKeyword(out var typeToken))
            return new AsExpr(left, typeToken);

        throw new CsEvalParserException($"Expected type after 'as' at {Peek().Line}:{Peek().Column}");
    }

    private Expr ParseShift()
    {
        var expr = ParseTerm();

        while (Match(TokenType.LessLess, TokenType.GreaterGreater))
        {
            var op = Previous();
            var right = ParseTerm();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    #endregion

    #region Arithmetic Operators

    private Expr ParseTerm()
    {
        var expr = ParseFactor();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var op = Previous();
            var right = ParseFactor();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    private Expr ParseFactor()
    {
        var expr = ParseUnary();

        while (Match(TokenType.Star, TokenType.Slash, TokenType.Percent))
        {
            var op = Previous();
            var right = ParseUnary();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    #endregion

    #region Unary and Postfix

    private Expr ParseUnary()
    {
        // Cast expression: (int)x, (double)y, (int?)z
        if (Check(TokenType.LeftParen) && IsCastExpression())
        {
            Advance(); // consume '('
            MatchTypeKeyword(out var typeToken);
            Consume(TokenType.RightParen, "Expected ')' after cast type");
            var operand = ParseUnary();
            return new CastExpr(typeToken, operand);
        }

        if (Match(TokenType.Bang, TokenType.Minus, TokenType.Plus, TokenType.Tilde))
        {
            var op = Previous();
            var right = ParseUnary();
            return new UnaryExpr(op, right);
        }

        // Prefix increment/decrement: ++x, --x
        if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
        {
            var op = Previous();
            var name = Consume(TokenType.Identifier, "Expected variable name after prefix operator");
            return new IncrementDecrementExpr(name, op, true);
        }

        return ParsePostfix();
    }

    private bool IsCastExpression()
    {
        // Look ahead: ( type ) or ( type? )
        // We're at '(' - check if next token is a type keyword
        if (_current + 1 >= _tokens.Count)
            return false;

        var nextToken = _tokens[_current + 1];
        if (!IsTypeKeyword(nextToken.Type))
            return false;

        // Check what follows the type: either ')' or '?' then ')'
        var afterTypeIndex = _current + 2;
        if (afterTypeIndex >= _tokens.Count)
            return false;

        var afterType = _tokens[afterTypeIndex];
        if (afterType.Type == TokenType.RightParen)
            return true;

        // Check for nullable: type?
        if (afterType.Type == TokenType.Question)
        {
            var afterNullable = afterTypeIndex + 1;
            if (afterNullable >= _tokens.Count)
                return false;
            return _tokens[afterNullable].Type == TokenType.RightParen;
        }

        return false;
    }

    private Expr ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(TokenType.Dot))
            {
                var name = Consume(TokenType.Identifier, "Expected property name after '.'");
                expr = new MemberAccessExpr(expr, name, false);
            }
            else if (Match(TokenType.QuestionDot))
            {
                var name = Consume(TokenType.Identifier, "Expected property name after '?.'");
                expr = new MemberAccessExpr(expr, name, true);
            }
            else if (Match(TokenType.LeftBracket))
            {
                var index = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after index");
                expr = new IndexAccessExpr(expr, index, false);
            }
            else if (Match(TokenType.QuestionLeftBracket))
            {
                var index = ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after null-conditional index");
                expr = new IndexAccessExpr(expr, index, true);
            }
            else if (Check(TokenType.Less) && TryParseTypeArguments(out var typeArgs))
            {
                // Generic method call: Method<T>() or Method<T1, T2>()
                // Type arguments must be followed by '(' for invocation
                Consume(TokenType.LeftParen, "Expected '(' after generic type arguments");
                expr = FinishCall(expr, typeArgs);
            }
            else if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr, null);
            }
            else if (expr is IdentifierExpr identifier && Match(TokenType.PlusPlus, TokenType.MinusMinus))
            {
                // Postfix increment/decrement: x++, x--
                var op = Previous();
                expr = new IncrementDecrementExpr(identifier.Name, op, false);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    /// <summary>
    /// Attempts to parse generic type arguments: &lt;T1, T2, ...&gt;
    /// Uses lookahead to disambiguate from less-than operator.
    /// Returns true if successfully parsed, with type arguments in 'typeArgs'.
    /// </summary>
    private bool TryParseTypeArguments(out List<string> typeArgs)
    {
        typeArgs = new List<string>();

        // Save position for potential backtrack
        var startPos = _current;

        // Must start with '<'
        if (!Check(TokenType.Less))
            return false;

        Advance(); // consume '<'

        // Parse type arguments
        while (true)
        {
            // Each type argument must be a type name (identifier or keyword type)
            if (!IsTypeName(Peek().Type))
            {
                // Not a valid type - backtrack
                _current = startPos;
                return false;
            }

            var typeName = NormalizeTypeName(Advance());
            typeArgs.Add(typeName);

            // After type: expect ',' for more types, or '>' to end
            if (Match(TokenType.Comma))
            {
                continue;
            }
            else if (Match(TokenType.Greater))
            {
                // Successfully parsed type arguments
                // Verify this looks like a generic call (followed by '(' or '.')
                if (Check(TokenType.LeftParen) || Check(TokenType.Dot) || Check(TokenType.QuestionDot))
                {
                    return true;
                }
                else
                {
                    // Doesn't look like a generic call - backtrack
                    _current = startPos;
                    typeArgs.Clear();
                    return false;
                }
            }
            else
            {
                // Unexpected token - backtrack
                _current = startPos;
                typeArgs.Clear();
                return false;
            }
        }
    }

    /// <summary>
    /// Checks if a token type can be used as a type name in generic arguments.
    /// </summary>
    private static bool IsTypeName(TokenType type)
    {
        return type == TokenType.Identifier ||
               // C# primitive type keywords
               type == TokenType.Int ||
               type == TokenType.Long ||
               type == TokenType.Short ||
               type == TokenType.Byte ||
               type == TokenType.Sbyte ||
               type == TokenType.Ushort ||
               type == TokenType.Uint ||
               type == TokenType.Ulong ||
               type == TokenType.Float ||
               type == TokenType.Double ||
               type == TokenType.Decimal ||
               type == TokenType.Bool ||
               type == TokenType.Char ||
               type == TokenType.StringType ||
               type == TokenType.Object ||
               type == TokenType.Dynamic ||
               type == TokenType.Nint ||
               type == TokenType.Nuint;
    }

    /// <summary>
    /// Converts a type token to its CLR type name.
    /// Keyword types are mapped to their full CLR names.
    /// </summary>
    private static string NormalizeTypeName(Token token)
    {
        return token.Type switch
        {
            TokenType.Int => "Int32",
            TokenType.Long => "Int64",
            TokenType.Short => "Int16",
            TokenType.Byte => "Byte",
            TokenType.Sbyte => "SByte",
            TokenType.Ushort => "UInt16",
            TokenType.Uint => "UInt32",
            TokenType.Ulong => "UInt64",
            TokenType.Float => "Single",
            TokenType.Double => "Double",
            TokenType.Decimal => "Decimal",
            TokenType.Bool => "Boolean",
            TokenType.Char => "Char",
            TokenType.StringType => "String",
            TokenType.Object => "Object",
            TokenType.Dynamic => "Object", // dynamic maps to object at runtime
            TokenType.Nint => "IntPtr",
            TokenType.Nuint => "UIntPtr",
            _ => token.Lexeme // For identifiers, use the lexeme directly
        };
    }

    private Expr FinishCall(Expr callee, List<string>? typeArgs)
    {
        var arguments = new List<Expr>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                arguments.Add(ParseArgument());
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after arguments");
        return new CallExpr(callee, arguments, typeArgs);
    }

    private Expr ParseArgument()
    {
        // Check for named argument: identifier (or contextual keyword) followed by colon
        if (IsParameterName(Peek().Type) && PeekNext().Type == TokenType.Colon)
        {
            var name = Advance(); // consume the identifier/keyword
            Advance(); // consume the colon
            var value = ParseExpression();
            return new NamedArgumentExpr(name, value);
        }

        return ParseExpression();
    }

    /// <summary>
    /// Returns true if the token type can be used as a parameter name in a named argument.
    /// This includes identifiers and contextual keywords that .NET methods might use as parameter names.
    /// </summary>
    private static bool IsParameterName(TokenType type)
    {
        return type == TokenType.Identifier ||
               // Common contextual keywords that appear as .NET parameter names
               type == TokenType.Value ||
               type == TokenType.From ||
               type == TokenType.Where ||
               type == TokenType.Select ||
               type == TokenType.Group ||
               type == TokenType.Into ||
               type == TokenType.Orderby ||
               type == TokenType.Join ||
               type == TokenType.On ||
               type == TokenType.Equals ||
               type == TokenType.By ||
               type == TokenType.Ascending ||
               type == TokenType.Descending ||
               type == TokenType.Let ||
               type == TokenType.Get ||
               type == TokenType.Set ||
               type == TokenType.Add ||
               type == TokenType.Remove ||
               type == TokenType.Init ||
               type == TokenType.When ||
               type == TokenType.With ||
               type == TokenType.And ||
               type == TokenType.Or ||
               type == TokenType.Not ||
               type == TokenType.File ||
               type == TokenType.Required ||
               type == TokenType.Scoped ||
               type == TokenType.Args;
    }

    #endregion
}
