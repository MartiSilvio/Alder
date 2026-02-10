namespace CsEval.Parsing;

/// <summary>
/// Parses expressions using recursive descent with Pratt-style precedence climbing.
/// Serves as the main parser entry point via Parse().
/// </summary>
public sealed class ExpressionParser : ParserBase
{
    private readonly PrimaryParser _primary;
    private readonly PatternParser _pattern;
    private readonly StatementParser _statement;

    internal ExpressionParser(
        ParserState state,
        PrimaryParser primary,
        PatternParser pattern,
        StatementParser statement)
        : base(state)
    {
        _primary = primary;
        _pattern = pattern;
        _statement = statement;
    }

    /// <summary>
    /// Creates a fully wired parser graph for a token list. Used by CsEvalEngine and for
    /// sub-expression parsing (interpolated strings).
    /// </summary>
    public static ExpressionParser CreateForSubExpression(List<Token> tokens)
    {
        var state = new ParserState(tokens);
        var primary = new PrimaryParser(state);
        var pattern = new PatternParser(state);
        var statement = new StatementParser(state);
        var expression = new ExpressionParser(state, primary, pattern, statement);

        // Wire cross-references
        primary.SetExpressionParser(expression);
        primary.SetStatementParser(statement);
        pattern.SetExpressionParser(expression);
        statement.SetExpressionParser(expression);

        return expression;
    }

    #region Entry Point

    public Expr Parse()
    {
        if (IsStatementKeyword())
            return ParseProgram();

        var expr = ParseExpression();

        if (IsAtEnd())
            return expr;

        if (!Check(TokenType.Semicolon))
            throw new CsEvalParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");

        State.Current = 0;
        return ParseProgram();
    }

    private Expr ParseProgram()
    {
        var statements = new List<Expr>();

        while (!IsAtEnd())
        {
            if (IsStatementKeyword())
            {
                var stmt = _statement.ParseStatement();
                if (stmt != null)
                    statements.Add(stmt);
            }
            else
            {
                var expr = ParseExpression();

                if (Check(TokenType.Semicolon))
                {
                    Advance();
                    statements.Add(expr);
                }
                else if (IsAtEnd())
                {
                    return new BlockExpr(statements, expr);
                }
                else
                {
                    throw new CsEvalParserException(
                        $"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
                }
            }
        }

        if (statements.Count > 0)
            return new BlockExpr(statements, null);

        throw new CsEvalParserException("Empty expression");
    }

    private bool IsStatementKeyword()
    {
        // Control flow keywords are always statement keywords
        if (Check(TokenType.Return) || Check(TokenType.Break) || Check(TokenType.Continue) ||
            Check(TokenType.If) || Check(TokenType.While) || Check(TokenType.For) ||
            Check(TokenType.Do) || Check(TokenType.Foreach) || Check(TokenType.Switch) ||
            Check(TokenType.Try) || Check(TokenType.Var))
            return true;

        // Type keywords are statement keywords ONLY if NOT followed by '.' (for static member access like double.NaN)
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot)
            return true;

        // Generic type variable declarations: Func<int, int> f = ..., List<T> items = ...
        // Identifier followed by '<' indicates a generic type declaration statement
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Less)
            return true;

        return false;
    }

    #endregion

    #region Expression Precedence

    internal Expr ParseExpression() => ParseAssignment();

    private Expr ParseAssignment()
    {
        // Throw expression: throw expr (ECMA-334 §12.16)
        if (Match(TokenType.Throw))
        {
            var throwExpr = ParseAssignment();
            return new ThrowExpr(throwExpr);
        }

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
            // Handle obj.Property ??= value
            if (Match(TokenType.QuestionQuestionEqual))
            {
                var value = ParseAssignment();
                return new MemberNullCoalesceAssignExpr(memberAccess.Object, memberAccess.Name.Lexeme, value);
            }

            // Handle obj.Property = value
            if (Match(TokenType.Equal))
            {
                var value = ParseAssignment();
                return new MemberAssignExpr(memberAccess.Object, memberAccess.Name, value);
            }

            // Handle obj.Property += value (compound assignment on member access)
            if (MatchCompoundAssignment(out var memberOp))
            {
                var value = ParseAssignment();
                return new MemberCompoundAssignExpr(memberAccess.Object, memberAccess.Name.Lexeme, memberOp.Type,
                    value);
            }
        }
        else if (expr is IndexAccessExpr indexAccess)
        {
            // Handle dict[key] ??= value
            if (Match(TokenType.QuestionQuestionEqual))
            {
                var value = ParseAssignment();
                return new IndexNullCoalesceAssignExpr(indexAccess.Object, indexAccess.Index, value);
            }

            // Handle arr[0] = value
            if (Match(TokenType.Equal))
            {
                var value = ParseAssignment();
                return new IndexAssignExpr(indexAccess.Object, indexAccess.Index, value);
            }

            // Handle arr[0] += value (compound assignment on index access)
            if (MatchCompoundAssignment(out var indexOp))
            {
                var value = ParseAssignment();
                return new IndexCompoundAssignExpr(indexAccess.Object, indexAccess.Index, indexOp.Type, value);
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
            // Right operand of ?? can be a throw expression (ECMA-334 §12.16)
            if (Check(TokenType.Throw))
            {
                Advance();
                var throwOperand = ParseAssignment();
                return new NullCoalesceExpr(expr, new ThrowExpr(throwOperand));
            }

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
            else if (Match(TokenType.Switch))
            {
                expr = ParseSwitchExpression(expr);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private IsPatternExpr ParseIsExpression(Expr left)
    {
        var pattern = _pattern.ParsePattern();
        return new IsPatternExpr(left, pattern);
    }

    private Expr ParseAsExpression(Expr left)
    {
        // x as type (keyword types: int, string, etc.)
        if (MatchTypeKeyword(out var typeToken))
            return new AsExpr(left, typeToken);

        // x as TypeName or x as Namespace.TypeName (non-keyword class types)
        if (Check(TokenType.Identifier))
        {
            var identToken = ParseDottedTypeName();
            return new AsExpr(left, identToken);
        }

        throw new CsEvalParserException($"Expected type after 'as' at {Peek().Line}:{Peek().Column}");
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

        // Check for nullable suffix: TypeName?
        if (Check(TokenType.Question))
        {
            Advance();
            name += "?";
        }

        return token with { Lexeme = name };
    }

    #region Switch Expression

    private Expr ParseSwitchExpression(Expr subject)
    {
        Consume(TokenType.LeftBrace, "Expected '{' after 'switch'");
        var arms = new List<SwitchArm>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var pattern = _pattern.ParsePattern();

            // Optional when guard
            Expr? whenGuard = null;
            if (Match(TokenType.When))
            {
                whenGuard = ParseExpression();
            }

            Consume(TokenType.Arrow, "Expected '=>' in switch arm");
            var value = ParseExpression();

            arms.Add(new SwitchArm(pattern, whenGuard, value));

            if (!Match(TokenType.Comma))
                break;
        }

        Consume(TokenType.RightBrace, "Expected '}' after switch expression arms");
        return new SwitchExpressionExpr(subject, arms);
    }

    #endregion

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

    internal Expr ParseUnary()
    {
        // Cast expression: (int)x, (double)y, (int?)z, (Exception)x
        if (Check(TokenType.LeftParen) && IsCastExpression())
        {
            Advance(); // consume '('
            Token typeToken;
            if (MatchTypeKeyword(out typeToken))
            {
                // keyword type cast
            }
            else if (Check(TokenType.Identifier))
            {
                typeToken = ParseDottedTypeName();
            }
            else
            {
                throw new CsEvalParserException($"Expected type in cast at {Peek().Line}:{Peek().Column}");
            }

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

        // Prefix increment/decrement: ++x, --x, ++obj.Prop, ++arr[i]
        if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
        {
            var op = Previous();
            var isIncrement = op.Type == TokenType.PlusPlus;
            // Parse the operand as a postfix expression to support member/index targets
            var operand = ParsePostfix();
            return operand switch
            {
                MemberAccessExpr m => new MemberIncrementExpr(m.Object, m.Name.Lexeme, true, isIncrement),
                IndexAccessExpr idx => new IndexIncrementExpr(idx.Object, idx.Index, true, isIncrement),
                IdentifierExpr id => new IncrementDecrementExpr(id.Name, op, true),
                _ => throw new CsEvalParserException($"Invalid prefix {op.Lexeme} target at {op.Line}:{op.Column}")
            };
        }

        return ParsePostfix();
    }

    internal bool IsCastExpression()
    {
        // Look ahead: ( type ) or ( type? )
        // We're at '(' - check if next token is a type keyword or identifier (class type)
        if (State.Current + 1 >= State.Tokens.Count)
            return false;

        var nextToken = State.Tokens[State.Current + 1];

        if (IsTypeKeyword(nextToken.Type))
        {
            // Check what follows the type keyword: either ')' or '?' then ')'
            var afterTypeIndex = State.Current + 2;
            if (afterTypeIndex >= State.Tokens.Count)
                return false;

            var afterType = State.Tokens[afterTypeIndex];
            if (afterType.Type == TokenType.RightParen)
                return true;

            // Check for nullable: type?
            if (afterType.Type == TokenType.Question)
            {
                var afterNullable = afterTypeIndex + 1;
                if (afterNullable >= State.Tokens.Count)
                    return false;
                return State.Tokens[afterNullable].Type == TokenType.RightParen;
            }

            return false;
        }

        // Identifier cast: (Exception)obj, (System.Exception)obj
        // Disambiguation: (identifier)expr is a cast if what follows ')' is a value-start token
        if (nextToken.Type == TokenType.Identifier)
        {
            // Walk past the identifier and optional dotted names
            var scanIndex = State.Current + 2;

            // Skip dotted names: Ident.Ident.Ident
            while (scanIndex + 1 < State.Tokens.Count
                   && State.Tokens[scanIndex].Type == TokenType.Dot
                   && State.Tokens[scanIndex + 1].Type == TokenType.Identifier)
            {
                scanIndex += 2;
            }

            if (scanIndex >= State.Tokens.Count)
                return false;

            // Optional nullable suffix
            if (State.Tokens[scanIndex].Type == TokenType.Question
                && scanIndex + 1 < State.Tokens.Count)
            {
                scanIndex++;
            }

            // Must have ')' next
            if (State.Tokens[scanIndex].Type != TokenType.RightParen)
                return false;

            // After ')' must be a value-start token (cast target)
            var afterParen = scanIndex + 1;
            if (afterParen >= State.Tokens.Count)
                return false;

            return IsValueStartToken(State.Tokens[afterParen].Type);
        }

        return false;
    }

    /// <summary>
    /// Returns true if a token type can start a value expression.
    /// Used for cast disambiguation: (Type)expr requires expr to start with a value token.
    /// </summary>
    private static bool IsValueStartToken(TokenType type)
    {
        return type is TokenType.Identifier
            or TokenType.Number or TokenType.String or TokenType.Character
            or TokenType.InterpolatedString
            or TokenType.True or TokenType.False or TokenType.Null
            or TokenType.New or TokenType.LeftParen
            or TokenType.Bang or TokenType.Tilde
            or TokenType.PlusPlus or TokenType.MinusMinus
            or TokenType.Minus or TokenType.Plus
            or TokenType.Typeof or TokenType.Nameof or TokenType.Default
            or TokenType.Throw;
    }

    private Expr ParsePostfix()
    {
        var expr = _primary.ParsePrimary();

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
                Consume(TokenType.LeftParen, "Expected '(' after generic type arguments");
                expr = FinishCall(expr, typeArgs);
            }
            else if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr, null);
            }
            else if (Check(TokenType.PlusPlus) || Check(TokenType.MinusMinus))
            {
                // Postfix increment/decrement: x++, x--, obj.Prop++, arr[i]++
                if (expr is IdentifierExpr identifier)
                {
                    Advance();
                    var op = Previous();
                    expr = new IncrementDecrementExpr(identifier.Name, op, false);
                }
                else if (expr is MemberAccessExpr memberExpr)
                {
                    Advance();
                    var isIncrement = Previous().Type == TokenType.PlusPlus;
                    expr = new MemberIncrementExpr(memberExpr.Object, memberExpr.Name.Lexeme, false, isIncrement);
                }
                else if (expr is IndexAccessExpr indexExpr)
                {
                    Advance();
                    var isIncrement = Previous().Type == TokenType.PlusPlus;
                    expr = new IndexIncrementExpr(indexExpr.Object, indexExpr.Index, false, isIncrement);
                }
                else
                {
                    break;
                }
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
    /// </summary>
    private bool TryParseTypeArguments(out List<string> typeArgs)
    {
        typeArgs = new List<string>();

        // Save position for potential backtrack
        var startPos = State.Current;

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
                State.Current = startPos;
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
                    State.Current = startPos;
                    typeArgs.Clear();
                    return false;
                }
            }
            else
            {
                // Unexpected token - backtrack
                State.Current = startPos;
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
        return type is TokenType.Identifier or TokenType.Int or TokenType.Long or TokenType.Short or TokenType.Byte
            or TokenType.Sbyte or TokenType.Ushort or TokenType.Uint or TokenType.Ulong or TokenType.Float
            or TokenType.Double or TokenType.Decimal or TokenType.Bool or TokenType.Char or TokenType.StringType
            or TokenType.Object or TokenType.Dynamic or TokenType.Nint or TokenType.Nuint;
    }

    /// <summary>
    /// Converts a type token to its CLR type name.
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
            TokenType.Dynamic => "Object",
            TokenType.Nint => "IntPtr",
            TokenType.Nuint => "UIntPtr",
            _ => token.Lexeme
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
    /// </summary>
    private static bool IsParameterName(TokenType type)
    {
        return type is TokenType.Identifier or TokenType.Value or TokenType.From or TokenType.Where or TokenType.Select
            or TokenType.Group or TokenType.Into or TokenType.Orderby or TokenType.Join or TokenType.On
            or TokenType.Equals or TokenType.By or TokenType.Ascending or TokenType.Descending or TokenType.Let
            or TokenType.Get or TokenType.Set or TokenType.Add or TokenType.Remove or TokenType.Init or TokenType.When
            or TokenType.With or TokenType.And or TokenType.Or or TokenType.Not or TokenType.File or TokenType.Required
            or TokenType.Scoped or TokenType.Args;
    }

    #endregion
}