namespace CsEval.Parsing;

/// <summary>
/// Parses statements: if, while, for, do-while, foreach, switch, try/catch/finally,
/// variable declarations, return, break, continue, and block expressions.
/// </summary>
public sealed class StatementParser : ParserBase
{
    private ExpressionParser _expression = null!;

    internal StatementParser(ParserState state) : base(state)
    {
    }

    internal void SetExpressionParser(ExpressionParser expression) => _expression = expression;

    #region Block and Statement List

    internal Expr ParseBlock()
    {
        if (Check(TokenType.RightBrace))
        {
            Advance();
            return new BlockExpr([], null);
        }

        var statements = ParseStatementList();

        Consume(TokenType.RightBrace, "Expected '}' after block");
        return new BlockExpr(statements, null);
    }

    internal List<Expr> ParseStatementList()
    {
        var statements = new List<Expr>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
                statements.Add(stmt);
        }

        return statements;
    }

    internal Expr? ParseStatement()
    {
        if (Match(TokenType.Return))
        {
            Expr? value = null;
            if (!Check(TokenType.Semicolon))
                value = _expression.ParseExpression();
            Match(TokenType.Semicolon);
            return new ReturnExpr(value);
        }

        if (Match(TokenType.Break))
        {
            Match(TokenType.Semicolon);
            return new BreakExpr();
        }

        if (Match(TokenType.Continue))
        {
            Match(TokenType.Semicolon);
            return new ContinueExpr();
        }

        if (Match(TokenType.If))
            return ParseIfStatement();

        if (Match(TokenType.While))
            return ParseWhileStatement();

        if (Match(TokenType.For))
            return ParseForStatement();

        if (Match(TokenType.Do))
            return ParseDoWhileStatement();

        if (Match(TokenType.Foreach))
            return ParseForEachStatement();

        if (Match(TokenType.Switch))
            return ParseSwitchStatement();

        if (Match(TokenType.Try))
            return ParseTryCatchFinally();

        // Parameterless throw; (rethrow) -- must check before expression fallback
        if (Check(TokenType.Throw) && PeekNext().Type == TokenType.Semicolon)
        {
            Advance(); // consume 'throw'
            Advance(); // consume ';'
            return new ThrowStatementExpr();
        }

        if (Match(TokenType.Var))
        {
            // Check for deconstruction pattern: var (x, y, ...) = expr
            if (Check(TokenType.LeftParen))
            {
                Advance(); // consume '('
                var variableNames = new List<string>();
                variableNames.Add(Consume(TokenType.Identifier, "Expected variable name in deconstruction").Lexeme);
                while (Match(TokenType.Comma))
                {
                    variableNames.Add(Consume(TokenType.Identifier, "Expected variable name in deconstruction").Lexeme);
                }
                Consume(TokenType.RightParen, "Expected ')' after deconstruction variable list");
                Consume(TokenType.Equal, "Expected '=' after deconstruction");
                var valueExpr = _expression.ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after deconstruction");
                return new DeconstructionExpr(variableNames, valueExpr);
            }

            var name = Consume(TokenType.Identifier, "Expected variable name");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = _expression.ParseExpression();
            if (initializer is LiteralExpr { Value: null })
                throw new CsEvalParserException($"Cannot assign null to an implicitly-typed variable '{name.Lexeme}' at {name.Line}:{name.Column}");
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(null, name, initializer);
        }

        // Type keyword followed by identifier is a variable declaration (e.g., "int x = 5")
        // Type keyword followed by dot is a static member access (e.g., "double.NaN") - let expression parsing handle it
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot && MatchTypeKeyword(out var typeToken))
        {
            var name = Consume(TokenType.Identifier, "Expected variable name");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = _expression.ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(typeToken, name, initializer);
        }

        // Standalone block statement { ... }
        if (Match(TokenType.LeftBrace))
        {
            var statements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after block");
            return new BlockExpr(statements, null);
        }

        var expr = _expression.ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after statement");
        return expr;
    }

    #endregion

    #region Conditional Statements

    private Expr ParseIfStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after if condition");

        var thenStatements = new List<Expr>();

        // Either a block { ... } or a single statement
        if (Match(TokenType.LeftBrace))
        {
            thenStatements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after if body");
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null)
                thenStatements.Add(stmt);
        }

        List<Expr>? elseStatements = null;
        if (Match(TokenType.Else))
        {
            elseStatements = [];
            if (Match(TokenType.LeftBrace))
            {
                elseStatements = ParseStatementList();
                Consume(TokenType.RightBrace, "Expected '}' after else body");
            }
            else
            {
                var stmt = ParseStatement();
                if (stmt != null)
                    elseStatements.Add(stmt);
            }
        }

        return new IfStatementExpr(condition, thenStatements, elseStatements);
    }

    private Expr ParseSwitchStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'switch'");
        var expression = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after switch expression");
        Consume(TokenType.LeftBrace, "Expected '{' before switch cases");

        var cases = new List<SwitchCaseExpr>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Case))
            {
                // Parse case pattern
                var pattern = _expression.ParseExpression();
                Consume(TokenType.Colon, "Expected ':' after case pattern");

                // Parse statements until next case, default, or closing brace
                var statements = ParseCaseStatements();
                cases.Add(new SwitchCaseExpr(pattern, statements));
            }
            else if (Match(TokenType.Default))
            {
                Consume(TokenType.Colon, "Expected ':' after 'default'");

                // Parse statements until next case or closing brace
                var statements = ParseCaseStatements();
                cases.Add(new SwitchCaseExpr(null, statements));
            }
            else
            {
                throw new CsEvalParserException($"Expected 'case' or 'default' in switch at {Peek().Line}:{Peek().Column}");
            }
        }

        Consume(TokenType.RightBrace, "Expected '}' after switch cases");
        return new SwitchStatementExpr(expression, cases);
    }

    private List<Expr> ParseCaseStatements()
    {
        var statements = new List<Expr>();

        // Parse statements until we hit case, default, or closing brace
        while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
                statements.Add(stmt);
        }

        return statements;
    }

    #endregion

    #region Loop Statements

    private Expr ParseWhileStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'while'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after while condition");

        var body = new List<Expr>();

        // Either a block { ... } or a single statement
        if (Match(TokenType.LeftBrace))
        {
            body = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after while body");
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null)
                body.Add(stmt);
        }

        return new WhileStatementExpr(condition, body);
    }

    private Expr ParseForStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'for'");

        // Parse initializer (can be var declaration, expression, or empty)
        Expr? initializer = null;
        if (!Check(TokenType.Semicolon))
        {
            if (Match(TokenType.Var))
            {
                var name = Consume(TokenType.Identifier, "Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = _expression.ParseExpression();
                if (init is LiteralExpr { Value: null })
                    throw new CsEvalParserException($"Cannot assign null to an implicitly-typed variable '{name.Lexeme}' at {name.Line}:{name.Column}");
                initializer = new VariableDeclExpr(null, name, init);
            }
            else if (MatchTypeKeyword(out var typeToken))
            {
                var name = Consume(TokenType.Identifier, "Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = _expression.ParseExpression();
                initializer = new VariableDeclExpr(typeToken, name, init);
            }
            else
            {
                initializer = _expression.ParseExpression();
            }
        }
        Consume(TokenType.Semicolon, "Expected ';' after for initializer");

        // Parse condition (or empty for infinite loop)
        Expr? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = _expression.ParseExpression();
        }
        Consume(TokenType.Semicolon, "Expected ';' after for condition");

        // Parse increment (or empty)
        Expr? increment = null;
        if (!Check(TokenType.RightParen))
        {
            increment = _expression.ParseExpression();
        }
        Consume(TokenType.RightParen, "Expected ')' after for clauses");

        // Parse body
        var body = new List<Expr>();
        if (Match(TokenType.LeftBrace))
        {
            body = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after for body");
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null)
                body.Add(stmt);
        }

        return new ForStatementExpr(initializer, condition, increment, body);
    }

    private Expr ParseDoWhileStatement()
    {
        // Parse body
        var body = new List<Expr>();
        if (Match(TokenType.LeftBrace))
        {
            body = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after do body");
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null)
                body.Add(stmt);
        }

        Consume(TokenType.While, "Expected 'while' after do body");
        Consume(TokenType.LeftParen, "Expected '(' after 'while'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after while condition");
        Match(TokenType.Semicolon); // Optional semicolon

        return new DoWhileStatementExpr(body, condition);
    }

    private Expr ParseForEachStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'foreach'");

        // Parse variable declaration (var varName or type varName)
        if (!Match(TokenType.Var) && !MatchTypeKeyword(out _))
        {
            throw new CsEvalParserException($"Expected 'var' or type keyword in foreach at {Peek().Line}:{Peek().Column}");
        }

        var variableName = Consume(TokenType.Identifier, "Expected variable name in foreach");

        // Consume 'in' keyword - it's reserved as a contextual keyword
        if (!Match(TokenType.In))
        {
            throw new CsEvalParserException($"Expected 'in' after variable name in foreach at {Peek().Line}:{Peek().Column}");
        }

        var collection = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after foreach collection");

        // Parse body
        var body = new List<Expr>();
        if (Match(TokenType.LeftBrace))
        {
            body = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after foreach body");
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null)
                body.Add(stmt);
        }

        return new ForEachStatementExpr(variableName, collection, body);
    }

    #endregion

    #region Exception Handling

    private Expr ParseTryCatchFinally()
    {
        // Parse try body
        Consume(TokenType.LeftBrace, "Expected '{' after 'try'");
        var tryBody = ParseStatementList();
        Consume(TokenType.RightBrace, "Expected '}' after try body");

        var catchClauses = new List<CatchClause>();
        List<Expr>? finallyBody = null;

        // Parse catch clauses
        while (Check(TokenType.Catch))
        {
            Advance(); // consume 'catch'

            string? exceptionTypeName = null;
            Token? variableName = null;

            if (Check(TokenType.LeftParen))
            {
                Advance(); // consume '('

                // Parse type name (may be dot-separated, e.g., System.IO.IOException)
                var typeParts = new List<string>();
                typeParts.Add(Consume(TokenType.Identifier, "Expected exception type name").Lexeme);
                while (Check(TokenType.Dot))
                {
                    Advance(); // consume '.'
                    typeParts.Add(Consume(TokenType.Identifier, "Expected type name part").Lexeme);
                }
                exceptionTypeName = string.Join(".", typeParts);

                // Check for variable name (next token is Identifier and not ')')
                if (Check(TokenType.Identifier))
                {
                    variableName = Advance();
                }

                Consume(TokenType.RightParen, "Expected ')' after catch clause");
            }

            // Parse optional when guard
            Expr? whenGuard = null;
            if (Check(TokenType.When))
            {
                Advance(); // consume 'when'
                Consume(TokenType.LeftParen, "Expected '(' after 'when'");
                whenGuard = _expression.ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after when guard");
            }

            // Parse catch body
            Consume(TokenType.LeftBrace, "Expected '{' after catch clause");
            var catchBody = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after catch body");

            catchClauses.Add(new CatchClause(exceptionTypeName, variableName, whenGuard, catchBody));
        }

        // Parse optional finally block
        if (Match(TokenType.Finally))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'finally'");
            finallyBody = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after finally body");
        }

        // Validate: must have at least one catch or a finally
        if (catchClauses.Count == 0 && finallyBody == null)
            throw new CsEvalParserException($"Expected 'catch' or 'finally' after try block at {Peek().Line}:{Peek().Column}");

        // Validate: bare catch (no type) must be last
        for (var i = 0; i < catchClauses.Count - 1; i++)
        {
            if (catchClauses[i].ExceptionTypeName == null)
                throw new CsEvalParserException($"CS1017: A general catch clause must be the last catch clause at {Peek().Line}:{Peek().Column}");
        }

        return new TryCatchFinallyExpr(tryBody, catchClauses, finallyBody);
    }

    #endregion
}
