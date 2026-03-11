using SysRuntimeHelpers = System.Runtime.CompilerServices.RuntimeHelpers;

namespace CsEval.Parsing;

/// <summary>
/// Parses expressions using recursive descent with Pratt-style precedence climbing.
/// Serves as the main parser entry point via Parse().
/// </summary>
internal sealed class ExpressionParser : ParserBase
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
    public static ExpressionParser CreateForSubExpression(List<Token> tokens, LanguageMode languageMode = LanguageMode.Standard)
    {
        var state = new ParserState(tokens, languageMode);
        var primary = new PrimaryParser(state);
        var pattern = new PatternParser(state);
        var statement = new StatementParser(state);
        var queryParser = new QueryParser(state);
        var expression = new ExpressionParser(state, primary, pattern, statement);

        // Wire cross-references
        primary.SetExpressionParser(expression);
        primary.SetStatementParser(statement);
        primary.SetQueryParser(queryParser);
        queryParser.SetExpressionParser(expression);
        pattern.SetExpressionParser(expression);
        statement.SetExpressionParser(expression);
        statement.SetPatternParser(pattern);

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

        throw new CsEvalParserException("Empty expression", Peek().Line, Peek().Column);
    }

    private bool IsStatementKeyword()
    {
        if (Check(TokenType.If))
        {
            if (IsIfExpressionStart())
                return false;
            return true;
        }

        // Control flow keywords are always statement keywords
        if (Check(TokenType.Return) || Check(TokenType.Break) || Check(TokenType.Continue) ||
            Check(TokenType.While) || Check(TokenType.For) ||
            Check(TokenType.Do) || Check(TokenType.Foreach) || Check(TokenType.Switch) ||
            Check(TokenType.Try) || Check(TokenType.Const) ||
            Check(TokenType.Using) || Check(TokenType.Lock))
            return true;

        if (CheckVar())
        {
            if (State.LanguageMode == LanguageMode.Extended &&
                Check(TokenType.Let) &&
                IsLetInExpressionStart())
            {
                return false;
            }

            return true;
        }

        // unless/until are statement keywords in Extended mode (Ruby/Perl)
        if (State.LanguageMode == LanguageMode.Extended &&
            (Check(TokenType.Unless) || Check(TokenType.Until)))
            return true;

        // Type keywords are statement keywords ONLY if NOT followed by '.' (for static member access like double.NaN)
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot)
            return true;

        // Generic declaration statements: Func<int, int> f = ..., List<T> items = ...
        // Use tentative parse to avoid misclassifying expression starts like "a < b".
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Less)
        {
            var saved = State.Current;
            var parsedType = TryParseTypeName();
            var isDeclaration = parsedType != null && Check(TokenType.Identifier);
            State.Current = saved;
            if (isDeclaration)
                return true;
        }

        return false;
    }

    #endregion

    #region Expression Precedence

    internal Expr ParseExpression()
    {
        SysRuntimeHelpers.EnsureSufficientExecutionStack();
        return ParseAssignment();
    }

    private Expr ParseAssignment()
    {
        if (Check(TokenType.Let))
        {
            if (State.LanguageMode == LanguageMode.Standard)
                throw new CsEvalLanguageModeException("let-in");

            Advance();
            return ParseLetInExpression(Previous());
        }

        // Throw expression: throw expr (ECMA-334 §12.16)
        if (Match(TokenType.Throw))
        {
            var throwExpr = ParseAssignment();
            return new ThrowExpr(throwExpr);
        }

        var expr = ParsePipeline();

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
        else if (expr is MultiDimIndexAccessExpr multiIndex)
        {
            // Handle arr[i, j] = value
            if (Match(TokenType.Equal))
            {
                var value = ParseAssignment();
                return new MultiDimIndexAssignExpr(multiIndex.Object, multiIndex.Indices, value);
            }
        }

        return expr;
    }

    private Expr ParseLetInExpression(Token letToken)
    {
        var statements = new List<Expr>();

        if (Match(TokenType.LeftBrace))
        {
            var names = new List<Token>
            {
                ConsumeIdentifierOrContextualKeyword("Expected property name in let destructuring")
            };

            while (Match(TokenType.Comma))
                names.Add(ConsumeIdentifierOrContextualKeyword("Expected property name in let destructuring"));

            Consume(TokenType.RightBrace, "Expected '}' after let destructuring");
            Consume(TokenType.Equal, "Expected '=' after let destructuring");
            var initializer = ParseLetInInitializer();

            var tempName = $"__let_tmp_{letToken.Line}_{letToken.Column}_{State.Current}";
            var tempToken = new Token(TokenType.Identifier, tempName, null, letToken.Line, letToken.Column);
            statements.Add(new VariableDeclExpr(null, tempToken, initializer));

            foreach (var name in names)
            {
                var memberToken = new Token(TokenType.Identifier, name.Lexeme, null, name.Line, name.Column);
                var memberAccess = new MemberAccessExpr(new IdentifierExpr(tempToken), memberToken, false);
                statements.Add(new VariableDeclExpr(null, name, memberAccess));
            }
        }
        else
        {
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name after 'let'");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = ParseLetInInitializer();
            statements.Add(new VariableDeclExpr(null, name, initializer));
        }

        var body = ParseExpression();
        return new BlockExpr(statements, body);
    }

    private Expr ParseLetInInitializer()
    {
        if (!TryFindTopLevelDelimiter(TokenType.In, State.Current, out var delimiterIndex))
            throw new CsEvalParserException($"Expected 'in' in let-in expression at {Peek().Line}:{Peek().Column}");

        var initializer = ParseExpressionSlice(State.Current, delimiterIndex);
        State.Current = delimiterIndex + 1; // consume 'in'
        return initializer;
    }

    private Expr ParseExpressionSlice(int startInclusive, int endExclusive)
    {
        if (endExclusive <= startInclusive)
            throw new CsEvalParserException("Expected expression in let-in initializer", Peek().Line, Peek().Column);

        var tokens = new List<Token>(endExclusive - startInclusive + 1);
        for (var i = startInclusive; i < endExclusive; i++)
            tokens.Add(State.Tokens[i]);

        var anchor = State.Tokens[Math.Min(endExclusive, State.Tokens.Count - 1)];
        tokens.Add(new Token(TokenType.Eof, string.Empty, null, anchor.Line, anchor.Column));

        var parser = CreateForSubExpression(tokens, State.LanguageMode);
        return parser.ParseExpression();
    }

    private bool IsLetInExpressionStart()
    {
        if (!Check(TokenType.Let))
            return false;

        var index = State.Current + 1;
        if (index >= State.Tokens.Count)
            return false;

        if (State.Tokens[index].Type == TokenType.LeftBrace)
        {
            index++;
            if (index >= State.Tokens.Count || State.Tokens[index].Type != TokenType.Identifier)
                return false;

            while (index < State.Tokens.Count && State.Tokens[index].Type != TokenType.RightBrace)
                index++;

            if (index >= State.Tokens.Count || State.Tokens[index].Type != TokenType.RightBrace)
                return false;

            index++;
            if (index >= State.Tokens.Count || State.Tokens[index].Type != TokenType.Equal)
                return false;

            index++;
            return TryFindTopLevelDelimiter(TokenType.In, index, out _);
        }

        if (State.Tokens[index].Type != TokenType.Identifier)
            return false;

        index++;
        if (index >= State.Tokens.Count || State.Tokens[index].Type != TokenType.Equal)
            return false;

        index++;
        return TryFindTopLevelDelimiter(TokenType.In, index, out _);
    }

    private bool TryFindTopLevelDelimiter(TokenType delimiter, int startIndex, out int delimiterIndex)
    {
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;

        for (var i = startIndex; i < State.Tokens.Count; i++)
        {
            var tokenType = State.Tokens[i].Type;

            switch (tokenType)
            {
                case TokenType.LeftParen:
                    parenDepth++;
                    break;
                case TokenType.RightParen:
                    parenDepth = Math.Max(0, parenDepth - 1);
                    break;
                case TokenType.LeftBracket:
                    bracketDepth++;
                    break;
                case TokenType.RightBracket:
                    bracketDepth = Math.Max(0, bracketDepth - 1);
                    break;
                case TokenType.LeftBrace:
                    braceDepth++;
                    break;
                case TokenType.RightBrace:
                    braceDepth = Math.Max(0, braceDepth - 1);
                    break;
            }

            if (parenDepth == 0 && bracketDepth == 0 && braceDepth == 0)
            {
                if (tokenType == delimiter)
                {
                    delimiterIndex = i;
                    return true;
                }

                if (tokenType == TokenType.Semicolon || tokenType == TokenType.Eof)
                    break;
            }
        }

        delimiterIndex = -1;
        return false;
    }

    /// <summary>
    /// Parses pipeline expressions: x |> f passes x as argument to f.
    /// Left-associative: x |> f |> g evaluates as (x |> f) |> g.
    /// Precedence: below assignment, above ternary conditional.
    /// Extended mode only.
    /// </summary>
    private Expr ParsePipeline()
    {
        var expr = ParseConditional();

        if (State.LanguageMode == LanguageMode.Extended)
        {
            while (Match(TokenType.PipeGreater))
            {
                var right = ParseConditional();
                expr = new PipelineExpr(expr, right);
            }
        }
        else if (Check(TokenType.PipeGreater))
        {
            throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.PipeGreater));
        }

        return expr;
    }

    private Expr ParseConditional()
    {
        if (Check(TokenType.If))
        {
            if (State.LanguageMode == LanguageMode.Standard)
                throw new CsEvalLanguageModeException("if-expression");

            if (IsIfExpressionStart())
                return ParseIfExpression();
        }

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

    private Expr ParseIfExpression()
    {
        Consume(TokenType.If, "Expected 'if' at start of if-expression");
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after if condition");

        if (Check(TokenType.LeftBrace))
            throw new CsEvalParserException($"If-expression branches must be expressions at {Peek().Line}:{Peek().Column}");

        var thenBranch = ParseExpression();
        Consume(TokenType.Else, "Expected 'else' in if-expression");
        var elseBranch = ParseExpression();
        return new ConditionalExpr(condition, thenBranch, elseBranch);
    }

    private bool IsIfExpressionStart()
    {
        if (!Check(TokenType.If) || !CheckNext(TokenType.LeftParen))
            return false;

        var index = State.Current + 2;
        var parenDepth = 1;
        while (index < State.Tokens.Count && parenDepth > 0)
        {
            switch (State.Tokens[index].Type)
            {
                case TokenType.LeftParen:
                    parenDepth++;
                    break;
                case TokenType.RightParen:
                    parenDepth--;
                    break;
            }

            index++;
        }

        if (parenDepth != 0 || index >= State.Tokens.Count)
            return false;

        if (State.Tokens[index].Type == TokenType.LeftBrace)
            return false;

        var branchParenDepth = 0;
        var branchBracketDepth = 0;
        var branchBraceDepth = 0;

        for (var i = index; i < State.Tokens.Count; i++)
        {
            var tokenType = State.Tokens[i].Type;
            switch (tokenType)
            {
                case TokenType.LeftParen:
                    branchParenDepth++;
                    break;
                case TokenType.RightParen:
                    branchParenDepth = Math.Max(0, branchParenDepth - 1);
                    break;
                case TokenType.LeftBracket:
                    branchBracketDepth++;
                    break;
                case TokenType.RightBracket:
                    branchBracketDepth = Math.Max(0, branchBracketDepth - 1);
                    break;
                case TokenType.LeftBrace:
                    branchBraceDepth++;
                    break;
                case TokenType.RightBrace:
                    branchBraceDepth = Math.Max(0, branchBraceDepth - 1);
                    break;
            }

            if (branchParenDepth == 0 && branchBracketDepth == 0 && branchBraceDepth == 0)
            {
                if (tokenType == TokenType.Else)
                    return true;

                if (tokenType == TokenType.Semicolon || tokenType == TokenType.Eof)
                    return false;
            }
        }

        return false;
    }

    private Expr ParseNullCoalesce()
    {
        var expr = ParseRange();

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

    /// <summary>
    /// Parses range literals: start..end (inclusive) or start..&lt;end (exclusive).
    /// Precedence: below null-coalesce, above logical OR.
    /// Range .. is INFIX (between two expressions). Spread .. is PREFIX (inside collection literals).
    /// Since we reach here only after parsing a left-hand expression, this is always range context.
    /// </summary>
    private Expr ParseRange()
    {
        var expr = ParseOr();

        if (State.LanguageMode == LanguageMode.Extended)
        {
            if (Match(TokenType.DotDot))
            {
                var end = ParseOr();
                return new RangeExpr(expr, end, ExclusiveEnd: false);
            }

            if (Match(TokenType.DotDotLess))
            {
                var end = ParseOr();
                return new RangeExpr(expr, end, ExclusiveEnd: true);
            }
        }
        else if (Check(TokenType.DotDot) || Check(TokenType.DotDotLess))
        {
            // Active rejection: if we see .. or ..< in infix position in Standard mode,
            // and the next token isn't a comma/bracket (which would be spread context handled
            // elsewhere), reject with a clear message.
            // But we must be careful: DotDot in Standard mode at this level could be spread
            // context leaking through. Only reject if it's clearly infix (we already parsed
            // a left-hand expression and this isn't inside array/object literal brackets).
            // Since spread is always handled in PrimaryParser before reaching here, any DotDot
            // at this level in Standard mode is either a genuine range attempt or a stray token.
            // We don't reject here to avoid breaking spread; the stray token will fail naturally.
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
            if (op.Type is TokenType.EqualEqualEqual or TokenType.BangEqualEqual && State.LanguageMode == LanguageMode.Standard)
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(op.Type));
            }
            var right = ParseComparison();

            // Chained comparisons (Python/Julia): a == b == c chains as a == b && b == c
            if (State.LanguageMode == LanguageMode.Extended && IsChainableComparisonOperator(Peek().Type))
            {
                var operands = new List<Expr> { expr, right };
                var operators = new List<Token> { op };

                while (IsChainableComparisonOperator(Peek().Type))
                {
                    var nextOp = Advance();
                    var nextOperand = ParseShift();
                    operators.Add(nextOp);
                    operands.Add(nextOperand);
                }

                return new ChainedComparisonExpr(operands, operators);
            }

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

                // Chained comparisons (Python/Julia): 0 < x < 10 chains as 0 < x && x < 10
                if (State.LanguageMode == LanguageMode.Extended && IsChainableComparisonOperator(Peek().Type))
                {
                    var operands = new List<Expr> { expr, right };
                    var operators = new List<Token> { op };

                    while (IsChainableComparisonOperator(Peek().Type))
                    {
                        var nextOp = Advance();
                        var nextOperand = ParseShift();
                        operators.Add(nextOp);
                        operands.Add(nextOperand);
                    }

                    return new ChainedComparisonExpr(operands, operators);
                }

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
            else if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.In))
            {
                var op = Previous();
                var right = ParseShift();
                expr = new BinaryExpr(expr, op, right);
            }
            else if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.NotIn))
            {
                var aliasToken = Previous();
                var unaryNot = TokenLexemes.CreateSynthetic(TokenType.Bang, aliasToken);
                var op = TokenLexemes.CreateSynthetic(TokenType.In, aliasToken);
                var right = ParseShift();
                expr = new UnaryExpr(unaryNot, new BinaryExpr(expr, op, right));
            }
            else if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.Like))
            {
                var op = Previous();
                var right = ParseShift();
                expr = new BinaryExpr(expr, op, right);
            }
            else if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.NotLike))
            {
                var aliasToken = Previous();
                var unaryNot = TokenLexemes.CreateSynthetic(TokenType.Bang, aliasToken);
                var op = TokenLexemes.CreateSynthetic(TokenType.Like, aliasToken);
                var right = ParseShift();
                expr = new UnaryExpr(unaryNot, new BinaryExpr(expr, op, right));
            }
            else if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.Between))
            {
                // between...and is a ternary-style operator: expr between low and high
                // Desugar to (expr >= low && expr <= high) at parse time
                var betweenToken = Previous();
                var low = ParseShift();
                Consume(TokenType.AmpAmp, "Expected 'and' after 'between' lower bound");
                var high = ParseShift();
                var geExpr = new BinaryExpr(expr, TokenLexemes.CreateSynthetic(TokenType.GreaterEqual, betweenToken), low);
                var leExpr = new BinaryExpr(expr, TokenLexemes.CreateSynthetic(TokenType.LessEqual, betweenToken), high);
                expr = new LogicalExpr(geExpr, TokenLexemes.CreateSynthetic(TokenType.AmpAmp, betweenToken), leExpr);
            }
            else if (State.LanguageMode == LanguageMode.Extended
                     && Match(TokenType.EqualTilde, TokenType.BangTilde, TokenType.LessEqualGreater))
            {
                var op = Previous();
                var right = ParseShift();
                expr = new BinaryExpr(expr, op, right);
            }
            // Active rejection in Standard mode for Extended-only operators at operator position.
            // These tokens are contextual keywords that can be used as identifiers in primary
            // expressions; here they appear after an expression (infix position) so they must
            // be operator usage.
            else if (State.LanguageMode == LanguageMode.Standard && Check(TokenType.In))
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.In));
            }
            else if (State.LanguageMode == LanguageMode.Standard && Check(TokenType.Like))
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.Like));
            }
            else if (State.LanguageMode == LanguageMode.Standard && Check(TokenType.Between))
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.Between));
            }
            else if (State.LanguageMode == LanguageMode.Standard
                     && Check(TokenType.EqualTilde))
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.EqualTilde));
            }
            else if (State.LanguageMode == LanguageMode.Standard
                     && Check(TokenType.BangTilde))
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.BangTilde));
            }
            else if (State.LanguageMode == LanguageMode.Standard
                     && Check(TokenType.LessEqualGreater))
            {
                throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.LessEqualGreater));
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private static bool IsChainableComparisonOperator(TokenType type) =>
        type is TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual
            or TokenType.EqualEqual or TokenType.BangEqual;

    private IsPatternExpr ParseIsExpression(Expr left)
    {
        var pattern = _pattern.ParsePattern();
        return new IsPatternExpr(left, pattern);
    }

    private Expr ParseAsExpression(Expr left)
    {
        // Use TryParseTypeName to handle all type forms including generics:
        // x as string, x as List<int>, x as Dictionary<string, int>?, x as System.Exception
        var typeName = TryParseTypeName();
        if (typeName == null)
            throw new CsEvalParserException($"Expected type after 'as' at {Peek().Line}:{Peek().Column}");

        var typeToken = new Token(TokenType.Identifier, typeName, null, Previous().Line, Previous().Column);
        return new AsExpr(left, typeToken);
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

        while (Match(TokenType.LessLess, TokenType.GreaterGreater, TokenType.GreaterGreaterGreater))
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

        // Implicit multiplication (Extended mode): number followed by identifier or '('
        // desugars to multiplication at the same precedence as explicit *.
        // 2x -> 2 * x, 3.5y -> 3.5 * y, 2(x+1) -> 2 * (x+1)
        // Only fires when left side is a numeric literal. Identifiers/parens on the
        // left are NOT implicit multiply (they stay as invocation or separate expressions).
        if (State.LanguageMode == LanguageMode.Extended
            && expr is LiteralExpr { Value: var numValue } && IsNumericValue(numValue)
            && (Check(TokenType.Identifier) || Check(TokenType.LeftParen)))
        {
            var syntheticStar = TokenLexemes.CreateSynthetic(TokenType.Star, Peek());
            var right = ParseUnary();
            expr = new BinaryExpr(expr, syntheticStar, right);
        }

        while (Match(TokenType.Star, TokenType.Slash, TokenType.Percent))
        {
            var op = Previous();
            var right = ParseUnary();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
    }

    /// <summary>
    /// Returns true if the value is a numeric type (int, long, double, float, decimal, etc.).
    /// Used for implicit multiplication detection.
    /// </summary>
    private static bool IsNumericValue(object? value) =>
        value is int or long or double or float or decimal
            or uint or ulong or short or ushort or byte or sbyte;

    #endregion

    #region Unary, Power, and Postfix

    /// <summary>
    /// Parses unary operators: -, +, !, ~, casts, prefix ++/--.
    /// Falls through to ParsePower() for non-unary expressions.
    /// Precedence: unary minus binds LOOSER than ** (like Python).
    /// So -2 ** 2 = -(2 ** 2) = -4, not (-2) ** 2 = 4.
    /// </summary>
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

        return ParsePower();
    }

    /// <summary>
    /// Parses the ** power operator with right-associativity.
    /// Precedence: higher than unary -; lower than postfix and primary.
    /// Right-associative: 2 ** 3 ** 2 = 2 ** (3 ** 2) = 512.
    /// Left operand via ParsePostfix (no unary), right operand via ParseUnary
    /// (allows unary and recursion for right-associativity).
    /// Like Python: -2 ** 2 = -(2 ** 2), but 2 ** -2 = 2 ** (-2).
    /// </summary>
    private Expr ParsePower()
    {
        var expr = ParsePostfix();

        if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.StarStar))
        {
            var op = Previous();
            var right = ParseUnary(); // Right via ParseUnary for right-associativity + unary support
            expr = new BinaryExpr(expr, op, right);
        }
        else if (State.LanguageMode == LanguageMode.Standard && Check(TokenType.StarStar))
        {
            throw new CsEvalLanguageModeException(TokenLexemes.GetCanonical(TokenType.StarStar));
        }

        return expr;
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
                var name = ConsumeIdentifierOrContextualKeyword("Expected property name after '.'");
                expr = new MemberAccessExpr(expr, name, false);
            }
            else if (Match(TokenType.QuestionDot))
            {
                var name = ConsumeIdentifierOrContextualKeyword("Expected property name after '?.'");
                expr = new MemberAccessExpr(expr, name, true);
            }
            else if (Match(TokenType.LeftBracket))
            {
                // Check for slice with omitted start: [:end] or [:] or [::step] or [:end:step]
                if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.Colon))
                {
                    Expr? end = null;
                    Expr? step = null;
                    if (!Check(TokenType.RightBracket) && !Check(TokenType.Colon))
                        end = ParseExpression();
                    if (Match(TokenType.Colon))
                    {
                        if (!Check(TokenType.RightBracket))
                            step = ParseExpression();
                    }
                    Consume(TokenType.RightBracket, "Expected ']' after slice");
                    expr = new SliceExpr(expr, null, end, step);
                }
                else
                {
                    var firstIndex = ParseExpression();

                    if (State.LanguageMode == LanguageMode.Extended && Match(TokenType.Colon))
                    {
                        // [start:end] or [start:] or [start:end:step] or [start::step]
                        Expr? end = null;
                        Expr? step = null;
                        if (!Check(TokenType.RightBracket) && !Check(TokenType.Colon))
                            end = ParseExpression();
                        if (Match(TokenType.Colon))
                        {
                            if (!Check(TokenType.RightBracket))
                                step = ParseExpression();
                        }
                        Consume(TokenType.RightBracket, "Expected ']' after slice");
                        expr = new SliceExpr(expr, firstIndex, end, step);
                    }
                    else if (Check(TokenType.Comma))
                    {
                        // Multi-dimensional access: arr[i, j, k]
                        var indices = new List<Expr> { firstIndex };
                        while (Match(TokenType.Comma))
                            indices.Add(ParseExpression());
                        Consume(TokenType.RightBracket, "Expected ']' after indices");
                        expr = new MultiDimIndexAccessExpr(expr, indices, false);
                    }
                    else
                    {
                        Consume(TokenType.RightBracket, "Expected ']' after index");
                        expr = new IndexAccessExpr(expr, firstIndex, false);
                    }
                }
            }
            else if (Match(TokenType.QuestionLeftBracket))
            {
                var firstIndex = ParseExpression();
                if (Check(TokenType.Comma))
                {
                    // Multi-dimensional null-safe access: arr?[i, j]
                    var indices = new List<Expr> { firstIndex };
                    while (Match(TokenType.Comma))
                        indices.Add(ParseExpression());
                    Consume(TokenType.RightBracket, "Expected ']' after null-conditional indices");
                    expr = new MultiDimIndexAccessExpr(expr, indices, true);
                }
                else
                {
                    Consume(TokenType.RightBracket, "Expected ']' after null-conditional index");
                    expr = new IndexAccessExpr(expr, firstIndex, true);
                }
            }
            else if (Check(TokenType.Less) && TryParseTypeArguments(out var typeArgs))
            {
                // Generic method call: Method<T>() or Method<T1, T2>()
                Consume(TokenType.LeftParen, "Expected '(' after generic type arguments");
                expr = FinishCall(expr, typeArgs);
            }
            else if (Check(TokenType.LeftParen)
                     && State.LanguageMode == LanguageMode.Extended
                     && expr is LiteralExpr { Value: var lpVal } && IsNumericValue(lpVal))
            {
                // In Extended mode, numeric literal followed by '(' is implicit multiplication
                // (e.g., 2(x+1) -> 2 * (x+1)), NOT a function call. Break out of postfix loop
                // so ParseFactor can handle it.
                break;
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
    /// Uses TryParseTypeName for each argument to support nested generics, dotted names,
    /// and nullable types: Method&lt;Dictionary&lt;string, int&gt;&gt;(), Method&lt;int?&gt;()
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

        // Parse type arguments using TryParseTypeName for full type name support
        while (true)
        {
            var typeName = TryParseTypeName();
            if (typeName == null)
            {
                // Not a valid type - backtrack
                State.Current = startPos;
                typeArgs.Clear();
                return false;
            }

            // Normalize keyword type names to CLR names for runtime resolution
            typeArgs.Add(NormalizeTypeNameString(typeName));

            // After type: expect ',' for more types, or '>' to end
            if (Match(TokenType.Comma))
            {
                continue;
            }
            else if (MatchClosingAngleBracket())
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
    /// Normalizes a type name string from C# keyword form to CLR form.
    /// Handles both simple names (int -> Int32) and compound names (preserving generics/arrays).
    /// </summary>
    private static string NormalizeTypeNameString(string typeName)
    {
        // Only normalize simple keyword type names, not compound/generic ones
        return typeName switch
        {
            "int" => "Int32",
            "long" => "Int64",
            "short" => "Int16",
            "byte" => "Byte",
            "sbyte" => "SByte",
            "ushort" => "UInt16",
            "uint" => "UInt32",
            "ulong" => "UInt64",
            "float" => "Single",
            "double" => "Double",
            "decimal" => "Decimal",
            "bool" => "Boolean",
            "char" => "Char",
            "string" => "String",
            "object" => "Object",
            "dynamic" => "Object",
            "nint" => "IntPtr",
            "nuint" => "UIntPtr",
            _ => typeName
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
        // Check for out argument: out var x, out int x, out _
        // ECMA-334 §12.6.2 - Output parameters
        if (Match(TokenType.Out))
        {
            return ParseOutArgument();
        }

        // Check for named argument: identifier (or contextual keyword) followed by colon
        if (IsParameterName(Peek().Type) && PeekNext().Type == TokenType.Colon)
        {
            var name = Advance(); // consume the identifier/keyword
            Advance(); // consume the colon
            var value = ParseExpression();
            return new NamedArgumentExpr(name, value);
        }

        var argument = ParseExpression();
        if (TryLowerImplicitPlaceholderLambda(argument, out var lowered))
            return lowered;

        return argument;
    }

    private bool TryLowerImplicitPlaceholderLambda(Expr argument, out Expr lowered)
    {
        lowered = argument;

        if (State.LanguageMode != LanguageMode.Extended || argument is LambdaExpr)
            return false;

        var collector = new IdentifierOccurrenceCollector();
        collector.Collect(argument);

        var foundPlaceholder = false;
        Token placeholderToken = default;
        foreach (var token in collector.GetUnboundTokens(StringComparer.Ordinal))
        {
            if (token.Type != TokenType.Identifier)
                continue;

            if (string.Equals(token.Lexeme, "it", StringComparison.Ordinal) ||
                string.Equals(token.Lexeme, TokenLexemes.DiscardIdentifier, StringComparison.Ordinal))
            {
                if (!foundPlaceholder)
                {
                    foundPlaceholder = true;
                    placeholderToken = token;
                }
            }
        }

        if (!foundPlaceholder)
            return false;

        lowered = new LambdaExpr([new LambdaParameter(null, placeholderToken)], argument);
        return true;
    }

    /// <summary>
    /// Parses an out argument after the 'out' keyword has been consumed.
    /// Handles: out _, out var x, out int x, out SomeType x
    /// </summary>
    private Expr ParseOutArgument()
    {
        // out _ (discard)
        if (Check(TokenType.Identifier) && Peek().Lexeme == "_")
        {
            Advance(); // consume _
            return new OutArgExpr("_", null, true);
        }

        // out var x
        if (MatchVar())
        {
            var varName = ConsumeIdentifierOrContextualKeyword("Expected variable name after 'out var'");
            return new OutArgExpr(varName.Lexeme, null, false);
        }

        // out <type> x -- type keyword (int, string, double, etc.) or identifier type
        string? typeName = TryParseTypeName();
        if (typeName != null && Check(TokenType.Identifier))
        {
            var varName = Advance();
            return new OutArgExpr(varName.Lexeme, typeName, false);
        }

        // Fallback: out <existing_variable> -- variable already declared
        // If TryParseTypeName consumed something but there's no identifier after, it's an existing var.
        // We need to handle this case. But TryParseTypeName backtracks on failure.
        // If typeName is not null, it means it parsed what looked like a type -- but
        // there's no identifier following. This could be `out existingVar`.
        // Since we already consumed the type name, treat it as a variable name.
        if (typeName != null)
        {
            return new OutArgExpr(typeName, null, false);
        }

        throw new CsEvalParserException($"Expected variable declaration or discard after 'out' at {Peek().Line}:{Peek().Column}");
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
            or TokenType.Scoped or TokenType.Args
            or TokenType.Like or TokenType.Between;
    }

    #endregion
}
