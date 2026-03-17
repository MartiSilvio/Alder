using CsEval.Diagnostics;

namespace CsEval.Parsing;

/// <summary>
/// Parses statements: if, while, for, do-while, foreach, switch, try/catch/finally,
/// variable declarations, return, break, continue, and block expressions.
/// </summary>
internal sealed class StatementParser : ParserBase
{
    private ExpressionParser _expression = null!;
    private PatternParser _pattern = null!;
    private readonly List<Expr> _pendingDecls = [];

    internal StatementParser(ParserState state) : base(state)
    {
    }

    internal void SetExpressionParser(ExpressionParser expression) => _expression = expression;
    internal void SetPatternParser(PatternParser pattern) => _pattern = pattern;

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
            ParseStatementInto(statements);
        }

        return statements;
    }

    internal void ParseStatementInto(List<Expr> statements)
    {
        var stmt = ParseStatement();
        if (stmt != null)
            statements.Add(stmt);
        // ECMA-334 §13.6.2: Multi-var declarations produce extra decls via _pendingDecls
        if (_pendingDecls.Count > 0)
        {
            statements.AddRange(_pendingDecls);
            _pendingDecls.Clear();
        }
    }

    internal Expr? ParseStatement()
    {
        if (Match(TokenType.Semicolon))
            return null;

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

        // ECMA-334 §13.10.4: goto label; / goto case expr; / goto default;
        if (Match(TokenType.Goto))
        {
            if (Match(TokenType.Case))
            {
                var value = _expression.ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after goto case");
                return new GotoCaseExpr(value);
            }
            if (Match(TokenType.Default))
            {
                Consume(TokenType.Semicolon, "Expected ';' after goto default");
                return new GotoDefaultExpr();
            }
            var label = Consume(TokenType.Identifier, "Expected label name after goto").Lexeme;
            Consume(TokenType.Semicolon, "Expected ';' after goto");
            return new GotoExpr(label);
        }

        // Label: identifier followed by ':' (not part of ternary or case)
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
        {
            // Disambiguate from ternary (x ? y : z) - labels only appear at statement level
            var label = Advance(); // consume identifier
            Advance(); // consume ':'
            return new LabelExpr(label.Lexeme);
        }

        if (Match(TokenType.If))
            return ParseIfStatement();

        // unless (cond) { body } desugars to if (!cond) { body } (Extended mode, Ruby/Perl)
        if (LanguageMode == LanguageMode.Extended && Match(TokenType.Unless))
            return ParseUnlessStatement();

        if (Match(TokenType.While))
            return ParseWhileStatement();

        // until (cond) { body } desugars to while (!cond) { body } (Extended mode, Ruby/Perl)
        if (LanguageMode == LanguageMode.Extended && Match(TokenType.Until))
            return ParseUntilStatement();

        if (Match(TokenType.For))
            return ParseForStatement();

        if (Match(TokenType.Do))
            return ParseDoWhileStatement();

        if (Match(TokenType.Foreach))
            return ParseForEachStatement();

        if (Match(TokenType.Switch))
            return ParseSwitchStatement();

        if (Match(TokenType.Using))
            return ParseUsingStatement();

        if (Match(TokenType.Lock))
            return ParseLockStatement();

        if (Match(TokenType.Try))
            return ParseTryCatchFinally();

        // Parameterless throw; (rethrow) -- must check before expression fallback
        if (Check(TokenType.Throw) && PeekNext().Type == TokenType.Semicolon)
        {
            Advance(); // consume 'throw'
            Advance(); // consume ';'
            return new ThrowStatementExpr();
        }

        if (Match(TokenType.Const))
            return ParseConstDeclaration();

        if (MatchVar())
        {
            // Check for deconstruction pattern: var (x, y, ...) = expr
            if (Check(TokenType.LeftParen))
            {
                Advance(); // consume '('
                var variableNames = new List<string>
                    { Consume(TokenType.Identifier, "Expected variable name in deconstruction").Lexeme };
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

            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = _expression.ParseExpression();
            if (initializer is LiteralExpr { Value: null })
                throw new CsEvalParserException(DiagnosticDescriptors.NullToImplicitlyTyped, name.Line, name.Column);
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(null, name, initializer);
        }

        // Generic type variable declaration: Func<int, int> f = ..., Action<string> a = ...
        // ECMA-334 §13.6.2 - Local variable declarations with constructed types
        // MUST come before type keyword check to handle generic types properly
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Less)
        {
            var genericResult = TryParseGenericTypeDeclaration();
            if (genericResult != null)
                return genericResult;
        }

        // Non-generic type name variable declaration: Action f = ..., Exception ex = ...
        // ECMA-334 §13.6.2 - Pattern: Identifier Identifier = expr;
        // Disambiguated from expression statements by the Identifier Identifier = pattern.
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Identifier)
        {
            var peekAt2 = State.Current + 2 < State.Tokens.Count ? State.Tokens[State.Current + 2] : State.Tokens[^1];
            if (peekAt2.Type is TokenType.Equal or TokenType.Semicolon)
            {
                var typeName = Advance(); // consume type name
                var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                if (Check(TokenType.LeftParen))
                    return ParseLocalFunctionDeclaration(typeName, name);
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var initializer = _expression.ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
                return new VariableDeclExpr(typeName, name, initializer);
            }
        }

        // Fully-qualified type name variable declaration: System.DayOfWeek d = 0;
        // Pattern: Identifier.Identifier[.Identifier...] Identifier = expr;
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Dot)
        {
            var fqnResult = TryParseFqnTypeDeclaration();
            if (fqnResult != null)
                return fqnResult;
        }

        // Type keyword followed by identifier is a variable declaration (e.g., "int x = 5")
        // Type keyword followed by dot is a static member access (e.g., "double.NaN") - let expression parsing handle it
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot && MatchTypeKeyword(out var typeToken))
        {
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            if (Check(TokenType.LeftParen))
                return ParseLocalFunctionDeclaration(typeToken, name);

            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = _expression.ParseExpression();

            // ECMA-334 §13.6.2: Multiple variable declarations — int x = 1, y = 2;
            // Handled by ParseMultiVarDecl which adds extra declarations to _pendingDecls
            if (Check(TokenType.Comma))
            {
                while (Match(TokenType.Comma))
                {
                    var nextName = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                    Consume(TokenType.Equal, "Expected '=' after variable name");
                    var nextInit = _expression.ParseExpression();
                    _pendingDecls.Add(new VariableDeclExpr(typeToken, nextName, nextInit));
                }
                Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
                return new VariableDeclExpr(typeToken, name, initializer);
            }

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

        // checked/unchecked block statements — no semicolon needed after block form
        if (Check(TokenType.Checked) || Check(TokenType.Unchecked))
        {
            var checkedExpr = _expression.ParseExpression();
            if (checkedExpr is CheckedExpr { Expression: BlockExpr })
                return checkedExpr;
            Consume(TokenType.Semicolon, "Expected ';' after statement");
            return checkedExpr;
        }

        var expr = _expression.ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after statement");
        return expr;
    }

    private Expr ParseLocalFunctionDeclaration(Token _returnType, Token functionName)
    {
        Consume(TokenType.LeftParen, "Expected '(' after local function name");
        var parameters = new List<LambdaParameter>();

        if (!Check(TokenType.RightParen))
        {
            while (true)
            {
                string? parameterType = null;
                if (IsTypeKeyword(Peek().Type))
                    parameterType = TryParseTypeName();

                var parameterName = ConsumeIdentifierOrContextualKeyword("Expected parameter name");
                parameters.Add(new LambdaParameter(parameterType, parameterName));

                if (!Match(TokenType.Comma))
                    break;
            }
        }

        Consume(TokenType.RightParen, "Expected ')' after parameter list");
        Consume(TokenType.LeftBrace, "Expected '{' before local function body");
        var body = ParseBlock();
        var lambda = new LambdaExpr(parameters, body);
        return new VariableDeclExpr(null, functionName, lambda);
    }

    private Expr ParseConstDeclaration()
    {
        var constToken = Previous();
        var typeName = TryParseTypeName();
        if (typeName == null)
        {
            throw new CsEvalParserException(
                $"Expected type after '{TokenLexemes.GetCanonical(TokenType.Const)}' at {constToken.Line}:{constToken.Column}",
                constToken.Line,
                constToken.Column);
        }

        var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
        Consume(TokenType.Equal, "Expected '=' after variable name");
        var initializer = _expression.ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after variable declaration");

        var declaredType = new Token(TokenType.Identifier, typeName, null, constToken.Line, constToken.Column);
        return new VariableDeclExpr(declaredType, name, initializer, IsConst: true);
    }

    /// <summary>
    /// Attempts to parse a generic type variable declaration: Identifier&lt;TypeArgs&gt; name = expr;
    /// Returns null and restores position if the pattern doesn't match.
    /// ECMA-334 §13.6.2 - Local variable declarations with constructed types.
    /// </summary>
    private Expr? TryParseGenericTypeDeclaration()
    {
        var saved = State.Current;

        try
        {
            var typeName = TryParseTypeName();
            if (typeName == null || !typeName.Contains('<'))
            {
                State.Current = saved;
                return null;
            }

            if (!Check(TokenType.Identifier) && !IsContextualKeyword(Peek().Type))
            {
                State.Current = saved;
                return null;
            }

            var name = Advance();

            if (!Match(TokenType.Equal))
            {
                State.Current = saved;
                return null;
            }

            var initializer = _expression.ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");

            // Create a synthetic type token with the full generic type name
            var syntheticTypeToken = new Token(TokenType.Identifier, typeName, null, name.Line, name.Column);
            return new VariableDeclExpr(syntheticTypeToken, name, initializer);
        }
        catch
        {
            // Speculative parse: backtrack on any failure
            State.Current = saved;
            return null;
        }
    }

    private Expr? TryParseFqnTypeDeclaration()
    {
        var saved = State.Current;

        try
        {
            var typeName = TryParseTypeName();
            if (typeName == null || !typeName.Contains('.'))
            {
                State.Current = saved;
                return null;
            }

            if (!Check(TokenType.Identifier) && !IsContextualKeyword(Peek().Type))
            {
                State.Current = saved;
                return null;
            }

            var name = Advance();

            if (!Match(TokenType.Equal))
            {
                State.Current = saved;
                return null;
            }

            var initializer = _expression.ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");

            var syntheticTypeToken = new Token(TokenType.Identifier, typeName, null, name.Line, name.Column);
            return new VariableDeclExpr(syntheticTypeToken, name, initializer);
        }
        catch
        {
            // Speculative parse: backtrack on any failure
            State.Current = saved;
            return null;
        }
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

    /// <summary>
    /// Parses unless (cond) { body } [else { body }] and desugars to if (!cond) { body } [else { body }].
    /// No new AST nodes needed -- unless is purely a parse-time transformation.
    /// </summary>
    private Expr ParseUnlessStatement()
    {
        var unlessToken = Previous();
        Consume(TokenType.LeftParen, "Expected '(' after 'unless'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after unless condition");

        var thenStatements = new List<Expr>();
        if (Match(TokenType.LeftBrace))
        {
            thenStatements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after unless body");
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

        // Desugar: unless (cond) -> if (!cond)
        var negatedCondition = new UnaryExpr(
            TokenLexemes.CreateSynthetic(TokenType.Bang, unlessToken),
            condition);
        return new IfStatementExpr(negatedCondition, thenStatements, elseStatements);
    }

    /// <summary>
    /// Parses until (cond) { body } and desugars to while (!cond) { body }.
    /// No new AST nodes needed -- until is purely a parse-time transformation.
    /// </summary>
    private Expr ParseUntilStatement()
    {
        var untilToken = Previous();
        Consume(TokenType.LeftParen, "Expected '(' after 'until'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after until condition");

        var body = new List<Expr>();
        if (Match(TokenType.LeftBrace))
        {
            body = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after until body");
        }
        else
        {
            var stmt = ParseStatement();
            if (stmt != null)
                body.Add(stmt);
        }

        // Desugar: until (cond) -> while (!cond)
        var negatedCondition = new UnaryExpr(
            TokenLexemes.CreateSynthetic(TokenType.Bang, untilToken),
            condition);
        return new WhileStatementExpr(negatedCondition, body);
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
                // Parse case pattern (type patterns, relational, constant, etc.)
                var pattern = _pattern.ParsePattern();

                // Parse optional when guard
                Expr? whenGuard = null;
                if (Match(TokenType.When))
                    whenGuard = _expression.ParseExpression();

                Consume(TokenType.Colon, "Expected ':' after case pattern");

                // Parse statements until next case, default, or closing brace
                var statements = ParseCaseStatements();
                cases.Add(new SwitchCaseExpr(pattern, whenGuard, statements));
            }
            else if (Match(TokenType.Default))
            {
                Consume(TokenType.Colon, "Expected ':' after 'default'");

                // Parse statements until next case or closing brace
                var statements = ParseCaseStatements();
                cases.Add(new SwitchCaseExpr(null, null, statements));
            }
            else
            {
                throw new CsEvalParserException(
                    $"Expected 'case' or 'default' in switch at {Peek().Line}:{Peek().Column}");
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

        // Parse initializers (can be var declarations, typed declarations, expressions, or empty)
        var initializers = new List<Expr>();
        if (!Check(TokenType.Semicolon))
        {
            if (MatchVar())
            {
                var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = _expression.ParseExpression();
                if (init is LiteralExpr { Value: null })
                    throw new CsEvalParserException(DiagnosticDescriptors.NullToImplicitlyTyped, name.Line,
                        name.Column);
                initializers.Add(new VariableDeclExpr(null, name, init));
                while (Match(TokenType.Comma))
                {
                    var name2 = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                    Consume(TokenType.Equal, "Expected '=' after variable name");
                    var init2 = _expression.ParseExpression();
                    if (init2 is LiteralExpr { Value: null })
                        throw new CsEvalParserException(DiagnosticDescriptors.NullToImplicitlyTyped, name2.Line,
                            name2.Column);
                    initializers.Add(new VariableDeclExpr(null, name2, init2));
                }
            }
            else if (MatchTypeKeyword(out var typeToken))
            {
                var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = _expression.ParseExpression();
                initializers.Add(new VariableDeclExpr(typeToken, name, init));
                while (Match(TokenType.Comma))
                {
                    var name2 = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                    Consume(TokenType.Equal, "Expected '=' after variable name");
                    var init2 = _expression.ParseExpression();
                    initializers.Add(new VariableDeclExpr(typeToken, name2, init2));
                }
            }
            else
            {
                initializers.Add(_expression.ParseExpression());
                while (Match(TokenType.Comma))
                    initializers.Add(_expression.ParseExpression());
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

        // Parse increments (comma-separated, or empty)
        var increments = new List<Expr>();
        if (!Check(TokenType.RightParen))
        {
            increments.Add(_expression.ParseExpression());
            while (Match(TokenType.Comma))
                increments.Add(_expression.ParseExpression());
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

        return new ForStatementExpr(initializers, condition, increments, body);
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
        if (!MatchVar() && !MatchTypeKeyword(out _))
        {
            throw new CsEvalParserException(
                $"Expected 'var' or type keyword in foreach at {Peek().Line}:{Peek().Column}");
        }

        var variableName = ConsumeIdentifierOrContextualKeyword("Expected variable name in foreach");

        // Consume 'in' keyword - it's reserved as a contextual keyword
        if (!Match(TokenType.In))
        {
            throw new CsEvalParserException(
                $"Expected 'in' after variable name in foreach at {Peek().Line}:{Peek().Column}");
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

    #region Resource Management & Synchronization

    private Expr ParseUsingStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'using'");

        // Parse resource declaration (var x = expr or TypeName x = expr or just expression)
        Expr resource;
        if (MatchVar())
        {
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            Consume(TokenType.Equal, "Expected '=' in using declaration");
            var init = _expression.ParseExpression();
            resource = new VariableDeclExpr(null, name, init);
        }
        else if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot && MatchTypeKeyword(out var typeToken))
        {
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            Consume(TokenType.Equal, "Expected '=' in using declaration");
            var init = _expression.ParseExpression();
            resource = new VariableDeclExpr(typeToken, name, init);
        }
        else
        {
            resource = _expression.ParseExpression();
        }

        Consume(TokenType.RightParen, "Expected ')' after using resource");

        // Parse body: block or single statement
        Expr body;
        if (Match(TokenType.LeftBrace))
        {
            var statements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after using body");
            body = new BlockExpr(statements, null);
        }
        else
        {
            body = ParseStatement()!;
        }

        return new UsingStatementExpr(resource, body);
    }

    private Expr ParseLockStatement()
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'lock'");
        var lockObj = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after lock expression");

        // Parse body: block or single statement
        Expr body;
        if (Match(TokenType.LeftBrace))
        {
            var statements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after lock body");
            body = new BlockExpr(statements, null);
        }
        else
        {
            body = ParseStatement()!;
        }

        return new LockStatementExpr(lockObj, body);
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
                var typeParts = new List<string>
                {
                    Consume(TokenType.Identifier, "Expected exception type name").Lexeme
                };
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
            throw new CsEvalParserException(
                $"Expected 'catch' or 'finally' after try block at {Peek().Line}:{Peek().Column}");

        // Validate: bare catch (no type) must be last
        for (var i = 0; i < catchClauses.Count - 1; i++)
        {
            if (catchClauses[i].ExceptionTypeName == null)
                throw new CsEvalParserException(
                    DiagnosticDescriptors.GeneralCatchMustBeLast,
                    Peek().Line,
                    Peek().Column);
        }

        return new TryCatchFinallyExpr(tryBody, catchClauses, finallyBody);
    }

    #endregion
}
