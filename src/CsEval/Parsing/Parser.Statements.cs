namespace CsEval.Parsing;

public sealed partial class Parser
{
    #region Block and Statement List

    private Expr ParseBlock()
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

    private List<Expr> ParseStatementList()
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

    private Expr? ParseStatement()
    {
        if (Match(TokenType.Return))
        {
            Expr? value = null;
            if (!Check(TokenType.Semicolon))
                value = ParseExpression();
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

        if (Match(TokenType.Var))
        {
            var name = Consume(TokenType.Identifier, "Expected variable name");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(null, name, initializer);
        }

        if (MatchTypeKeyword(out var typeToken))
        {
            var name = Consume(TokenType.Identifier, "Expected variable name");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(typeToken, name, initializer);
        }

        var expr = ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after statement");
        return expr;
    }

    #endregion

    #region Conditional Statements

    private Expr ParseIfStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
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
        var expression = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after switch expression");
        Consume(TokenType.LeftBrace, "Expected '{' before switch cases");

        var cases = new List<SwitchCaseExpr>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Match(TokenType.Case))
            {
                // Parse case pattern
                var pattern = ParseExpression();
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
                throw new ParserException($"Expected 'case' or 'default' in switch at {Peek().Line}:{Peek().Column}");
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
        var condition = ParseExpression();
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
                var init = ParseExpression();
                initializer = new VariableDeclExpr(null, name, init);
            }
            else if (MatchTypeKeyword(out var typeToken))
            {
                var name = Consume(TokenType.Identifier, "Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = ParseExpression();
                initializer = new VariableDeclExpr(typeToken, name, init);
            }
            else
            {
                initializer = ParseExpression();
            }
        }
        Consume(TokenType.Semicolon, "Expected ';' after for initializer");

        // Parse condition (or empty for infinite loop)
        Expr? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = ParseExpression();
        }
        Consume(TokenType.Semicolon, "Expected ';' after for condition");

        // Parse increment (or empty)
        Expr? increment = null;
        if (!Check(TokenType.RightParen))
        {
            increment = ParseExpression();
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
        var condition = ParseExpression();
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
            throw new ParserException($"Expected 'var' or type keyword in foreach at {Peek().Line}:{Peek().Column}");
        }

        var variableName = Consume(TokenType.Identifier, "Expected variable name in foreach");

        // Consume 'in' keyword - it's reserved as a contextual keyword
        if (!Match(TokenType.In))
        {
            throw new ParserException($"Expected 'in' after variable name in foreach at {Peek().Line}:{Peek().Column}");
        }

        var collection = ParseExpression();
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
}
