using Alder.Diagnostics;

namespace Alder.Parsing;

/// <summary>
/// Parses statement forms.
/// This stage owns control flow, local declarations, block structure, and statement-only syntactic forms.
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

    internal Expr ParseBlock()
    {
        var mark = Mark();
        if (Check(TokenType.RightBrace))
        {
            Advance();
            return new BlockExpr([], null) { Span = SpanFrom(mark) };
        }

        var statements = ParseStatementList();

        Consume(TokenType.RightBrace, "Expected '}' after block");
        return new BlockExpr(statements, null) { Span = SpanFrom(mark) };
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
        // ECMA-334 local declaration parsing can expand one source statement into multiple bound declaration nodes.
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

        var mark = Mark();

        if (Match(TokenType.Return))
        {
            Expr? value = null;
            if (!Check(TokenType.Semicolon))
                value = _expression.ParseExpression();
            Match(TokenType.Semicolon);
            return new ReturnExpr(value) { Span = SpanFrom(mark) };
        }

        if (Match(TokenType.Break))
        {
            Match(TokenType.Semicolon);
            return new BreakExpr() { Span = SpanFrom(mark) };
        }

        if (Match(TokenType.Continue))
        {
            Match(TokenType.Semicolon);
            return new ContinueExpr() { Span = SpanFrom(mark) };
        }

        // ECMA-334 §13.10.4: goto statements cover labels, case labels, and default labels.
        if (Match(TokenType.Goto))
        {
            if (Match(TokenType.Case))
            {
                var value = _expression.ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after goto case");
                return new GotoCaseExpr(value) { Span = SpanFrom(mark) };
            }
            if (Match(TokenType.Default))
            {
                Consume(TokenType.Semicolon, "Expected ';' after goto default");
                return new GotoDefaultExpr() { Span = SpanFrom(mark) };
            }
            var label = Consume(TokenType.Identifier, "Expected label name after goto").Lexeme;
            Consume(TokenType.Semicolon, "Expected ';' after goto");
            return new GotoExpr(label) { Span = SpanFrom(mark) };
        }

        // ECMA-334 §13.3: a block is itself a statement form.
        if (Match(TokenType.LeftBrace))
            return ParseBlock();

        // Labels are recognized here before expression parsing can reinterpret the identifier.
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
        {
            var label = Advance();
            Advance(); // consume ':'
            return new LabelExpr(label.Lexeme) { Span = SpanFrom(mark) };
        }

        if (Match(TokenType.If))
            return ParseIfStatement(mark);

        // Extended syntax lowers unless to an inverted if-statement.
        if (LanguageMode == LanguageMode.Extended && Match(TokenType.Unless))
            return ParseUnlessStatement(mark);

        if (Match(TokenType.While))
            return ParseWhileStatement(mark);

        // Extended syntax lowers until to an inverted while-statement.
        if (LanguageMode == LanguageMode.Extended && Match(TokenType.Until))
            return ParseUntilStatement(mark);

        if (Match(TokenType.For))
            return ParseForStatement(mark);

        if (Match(TokenType.Do))
            return ParseDoWhileStatement(mark);

        if (Match(TokenType.Foreach))
            return ParseForEachStatement(mark);

        if (Match(TokenType.Switch))
            return ParseSwitchStatement(mark);

        if (Match(TokenType.Using))
            return ParseUsingStatement(mark);

        if (Match(TokenType.Lock))
            return ParseLockStatement(mark);

        if (Match(TokenType.Try))
            return ParseTryCatchFinally(mark);

        // A bare throw statement must be recognized before expression parsing can consume the token stream.
        if (Check(TokenType.Throw) && PeekNext().Type == TokenType.Semicolon)
        {
            Advance(); // consume 'throw'
            Advance(); // consume ';'
            return new ThrowStatementExpr() { Span = SpanFrom(mark) };
        }

        if (Match(TokenType.Const))
            return ParseConstDeclaration(mark);

        // ECMA-334 §13.15: yield return and yield break are statement forms.
        if (Match(TokenType.Yield))
        {
            if (Match(TokenType.Return))
            {
                var value = _expression.ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after yield return");
                return new YieldReturnExpr(value) { Span = SpanFrom(mark) };
            }
            if (Match(TokenType.Break))
            {
                Consume(TokenType.Semicolon, "Expected ';' after yield break");
                return new YieldBreakExpr() { Span = SpanFrom(mark) };
            }
            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'return' or 'break' after 'yield'");
        }

        if (MatchVar())
        {
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
                return new DeconstructionExpr(variableNames, valueExpr) { Span = SpanFrom(mark) };
            }

            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = _expression.ParseExpression();
            if (initializer is LiteralExpr { Value: null })
                throw new AlderException(DiagnosticDescriptors.NullToImplicitlyTyped, name.Span, name.Line, name.Column);
            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(null, name, initializer) { Span = SpanFrom(mark) };
        }

        // Typed declarations and local functions must be recognized before general expression parsing.
        // The probe covers keyword types, identifier types, generics, dotted names, tuples, arrays, and nullable forms.
        // Type keywords followed by dot remain ordinary member access, such as double.NaN.
        {
            var declResult = TryParseTypedDeclaration(mark);
            if (declResult != null)
                return declResult;
        }

        if (Match(TokenType.LeftBrace))
        {
            var statements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after block");
            return new BlockExpr(statements, null) { Span = SpanFrom(mark) };
        }

        // checked and unchecked support both expression and block forms.
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

    private Expr ParseLocalFunctionDeclaration(Token _returnType, Token functionName, int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after local function name");
        var parameters = new List<LambdaParameter>();

        if (!Check(TokenType.RightParen))
        {
            while (true)
            {
                string? parameterType = null;
                if (IsTypeKeyword(Peek().Type) || (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Less)
                    || (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Identifier)
                    || (Check(TokenType.LeftParen)))
                {
                    var saved = State.Current;
                    parameterType = TryParseTypeName();
                    if (parameterType == null || (!Check(TokenType.Identifier) && !IsContextualKeyword(Peek().Type)))
                    {
                        parameterType = null;
                        State.Current = saved;
                    }
                }

                var parameterName = ConsumeIdentifierOrContextualKeyword("Expected parameter name");
                parameters.Add(new LambdaParameter(parameterType, parameterName));

                if (!Match(TokenType.Comma))
                    break;
            }
        }

        Consume(TokenType.RightParen, "Expected ')' after parameter list");

        Expr body;
        if (Match(TokenType.Arrow))
        {
            body = _expression.ParseExpression();
            Match(TokenType.Semicolon);
        }
        else
        {
            Consume(TokenType.LeftBrace, "Expected '{' or '=>' for local function body");
            body = ParseBlock();
        }

        var lambda = new LambdaExpr(parameters, body, ReturnTypeName: _returnType.Lexeme) { Span = SpanFrom(mark) };
        return new VariableDeclExpr(null, functionName, lambda) { Span = SpanFrom(mark) };
    }

    private Expr ParseConstDeclaration(int mark)
    {
        var constToken = Previous();
        var typeName = TryParseTypeName();
        if (typeName == null)
        {
            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, $"type after '{TokenLexemes.GetCanonical(TokenType.Const)}'");
        }

        var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
        Consume(TokenType.Equal, "Expected '=' after variable name");
        var initializer = _expression.ParseExpression();
        Consume(TokenType.Semicolon, "Expected ';' after variable declaration");

        var declaredType = new Token(TokenType.Identifier, typeName, null, constToken.Line, constToken.Column);
        return new VariableDeclExpr(declaredType, name, initializer, IsConst: true) { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Unified typed declaration parser for all type shapes.
    /// Handles: type keywords (int x), identifiers (Action f), generics (List&lt;int&gt; x),
    /// dotted names (System.DayOfWeek d), tuples ((int, string) t), arrays (int[] a),
    /// and nullable (int? n). Also handles local function declarations (int F() =&gt; ...).
    /// Returns null and restores position if the pattern doesn't match.
    /// ECMA-334 §13.6.2 - Local variable declarations.
    /// </summary>
    private Expr? TryParseTypedDeclaration(int mark)
    {
        // Type keywords followed by dot are static member access (double.NaN), not declarations
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type == TokenType.Dot)
            return null;

        if (!IsTypeKeyword(Peek().Type) && !Check(TokenType.Identifier) && !Check(TokenType.LeftParen))
            return null;

        var saved = State.Current;

        try
        {
            var typeName = TryParseTypeName(out var tupleElementNames);
            if (typeName == null)
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

            // Local function: Type Name(...) => ... or Type Name(...) { ... }
            if (Check(TokenType.LeftParen))
            {
                var typeToken = new Token(TokenType.Identifier, typeName, null,
                    State.Tokens[saved].Line, State.Tokens[saved].Column, State.Tokens[saved].Start);
                return ParseLocalFunctionDeclaration(typeToken, name, mark);
            }

            if (!Match(TokenType.Equal))
            {
                State.Current = saved;
                return null;
            }

            // ECMA-334 §12.8.16.5: Bare array initializer (int[] nums = { 1, 2, 3 };)
            Expr initializer;
            if (typeName.EndsWith("[]", StringComparison.Ordinal) && Check(TokenType.LeftBrace))
            {
                var initMark = Mark();
                Advance(); // consume '{'
                var elements = new List<Expr>();
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    if (elements.Count > 0) Consume(TokenType.Comma, "Expected ',' between array elements");
                    elements.Add(_expression.ParseExpression());
                }
                Consume(TokenType.RightBrace, "Expected '}' after array initializer");
                var elementTypeName = typeName[..^2]; // strip "[]"
                initializer = new TypedArrayLiteralExpr(elementTypeName, elements) { Span = SpanFrom(initMark) };
            }
            else
            {
                initializer = _expression.ParseExpression();
            }

            var syntheticTypeToken = new Token(TokenType.Identifier, typeName, null,
                State.Tokens[saved].Line, State.Tokens[saved].Column, State.Tokens[saved].Start);

            // ECMA-334 §13.6.2: Multiple variable declarations (int x = 1, y = 2;)
            if (Check(TokenType.Comma))
            {
                while (Match(TokenType.Comma))
                {
                    var markPending = Mark();
                    var nextName = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                    Consume(TokenType.Equal, "Expected '=' after variable name");
                    var nextInit = _expression.ParseExpression();
                    _pendingDecls.Add(new VariableDeclExpr(syntheticTypeToken, nextName, nextInit) { Span = SpanFrom(markPending) });
                }
            }

            Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
            return new VariableDeclExpr(syntheticTypeToken, name, initializer, TupleElementNames: tupleElementNames) { Span = SpanFrom(mark) };
        }
        catch (AlderException)
        {
            State.Current = saved;
            return null;
        }
    }

    private Expr ParseIfStatement(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after if condition");

        var thenStatements = new List<Expr>();

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

        return new IfStatementExpr(condition, thenStatements, elseStatements) { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses unless (cond) { body } [else { body }] and desugars to if (!cond) { body } [else { body }].
    /// No new AST nodes needed -- unless is purely a parse-time transformation.
    /// </summary>
    private Expr ParseUnlessStatement(int mark)
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

        var negatedCondition = new UnaryExpr(
            TokenLexemes.CreateSynthetic(TokenType.Bang, unlessToken),
            condition) { Span = SpanFrom(mark) };
        return new IfStatementExpr(negatedCondition, thenStatements, elseStatements) { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses until (cond) { body } and desugars to while (!cond) { body }.
    /// No new AST nodes needed -- until is purely a parse-time transformation.
    /// </summary>
    private Expr ParseUntilStatement(int mark)
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

        var negatedCondition = new UnaryExpr(
            TokenLexemes.CreateSynthetic(TokenType.Bang, untilToken),
            condition) { Span = SpanFrom(mark) };
        return new WhileStatementExpr(negatedCondition, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseSwitchStatement(int mark)
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
                var pattern = _pattern.ParsePattern();

                Expr? whenGuard = null;
                if (Match(TokenType.When))
                    whenGuard = _expression.ParseExpression();

                Consume(TokenType.Colon, "Expected ':' after case pattern");

                var statements = ParseCaseStatements();
                cases.Add(new SwitchCaseExpr(pattern, whenGuard, statements));
            }
            else if (Match(TokenType.Default))
            {
                Consume(TokenType.Colon, "Expected ':' after 'default'");

                var statements = ParseCaseStatements();
                cases.Add(new SwitchCaseExpr(null, null, statements));
            }
            else
            {
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'case' or 'default' in switch");
            }
        }

        Consume(TokenType.RightBrace, "Expected '}' after switch cases");
        return new SwitchStatementExpr(expression, cases) { Span = SpanFrom(mark) };
    }

    private List<Expr> ParseCaseStatements()
    {
        var statements = new List<Expr>();

        while (!Check(TokenType.Case) && !Check(TokenType.Default) && !Check(TokenType.RightBrace) && !IsAtEnd())
        {
            ParseStatementInto(statements);
        }

        return statements;
    }

    private Expr ParseWhileStatement(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'while'");
        var condition = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after while condition");

        var body = new List<Expr>();

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

        return new WhileStatementExpr(condition, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseForStatement(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'for'");

        var initializers = new List<Expr>();
        if (!Check(TokenType.Semicolon))
        {
            if (MatchVar())
            {
                var markInit = Mark();
                var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = _expression.ParseExpression();
                if (init is LiteralExpr { Value: null })
                    throw new AlderException(DiagnosticDescriptors.NullToImplicitlyTyped, name.Span, name.Line, name.Column);
                initializers.Add(new VariableDeclExpr(null, name, init) { Span = SpanFrom(markInit) });
                while (Match(TokenType.Comma))
                {
                    var markInit2 = Mark();
                    var name2 = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                    Consume(TokenType.Equal, "Expected '=' after variable name");
                    var init2 = _expression.ParseExpression();
                    if (init2 is LiteralExpr { Value: null })
                        throw new AlderException(DiagnosticDescriptors.NullToImplicitlyTyped, name2.Span, name2.Line, name2.Column);
                    initializers.Add(new VariableDeclExpr(null, name2, init2) { Span = SpanFrom(markInit2) });
                }
            }
            else if (MatchTypeKeyword(out var typeToken))
            {
                var markInit = Mark();
                var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var init = _expression.ParseExpression();
                initializers.Add(new VariableDeclExpr(typeToken, name, init) { Span = SpanFrom(markInit) });
                while (Match(TokenType.Comma))
                {
                    var markInit2 = Mark();
                    var name2 = ConsumeIdentifierOrContextualKeyword("Expected variable name");
                    Consume(TokenType.Equal, "Expected '=' after variable name");
                    var init2 = _expression.ParseExpression();
                    initializers.Add(new VariableDeclExpr(typeToken, name2, init2) { Span = SpanFrom(markInit2) });
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

        Expr? condition = null;
        if (!Check(TokenType.Semicolon))
        {
            condition = _expression.ParseExpression();
        }

        Consume(TokenType.Semicolon, "Expected ';' after for condition");

        var increments = new List<Expr>();
        if (!Check(TokenType.RightParen))
        {
            increments.Add(_expression.ParseExpression());
            while (Match(TokenType.Comma))
                increments.Add(_expression.ParseExpression());
        }

        Consume(TokenType.RightParen, "Expected ')' after for clauses");

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

        return new ForStatementExpr(initializers, condition, increments, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseDoWhileStatement(int mark)
    {
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

        return new DoWhileStatementExpr(body, condition) { Span = SpanFrom(mark) };
    }

    private Expr ParseForEachStatement(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'foreach'");

        if (!MatchVar() && !MatchTypeKeyword(out _))
        {
            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'var' or type keyword in foreach");
        }

        var variableName = ConsumeIdentifierOrContextualKeyword("Expected variable name in foreach");

        if (!Match(TokenType.In))
        {
            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'in' after variable name in foreach");
        }

        var collection = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after foreach collection");

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

        return new ForEachStatementExpr(variableName, collection, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseUsingStatement(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'using'");

        Expr resource;
        if (MatchVar())
        {
            var markRes = Mark();
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            Consume(TokenType.Equal, "Expected '=' in using declaration");
            var init = _expression.ParseExpression();
            resource = new VariableDeclExpr(null, name, init) { Span = SpanFrom(markRes) };
        }
        else if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot && MatchTypeKeyword(out var typeToken))
        {
            var markRes = Mark();
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name");
            Consume(TokenType.Equal, "Expected '=' in using declaration");
            var init = _expression.ParseExpression();
            resource = new VariableDeclExpr(typeToken, name, init) { Span = SpanFrom(markRes) };
        }
        else
        {
            resource = _expression.ParseExpression();
        }

        Consume(TokenType.RightParen, "Expected ')' after using resource");

        Expr body;
        if (Match(TokenType.LeftBrace))
        {
            var markBody = Mark();
            var statements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after using body");
            body = new BlockExpr(statements, null) { Span = SpanFrom(markBody) };
        }
        else
        {
            body = ParseStatement()!;
        }

        return new UsingStatementExpr(resource, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseLockStatement(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'lock'");
        var lockObj = _expression.ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after lock expression");

        Expr body;
        if (Match(TokenType.LeftBrace))
        {
            var markBody = Mark();
            var statements = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after lock body");
            body = new BlockExpr(statements, null) { Span = SpanFrom(markBody) };
        }
        else
        {
            body = ParseStatement()!;
        }

        return new LockStatementExpr(lockObj, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseTryCatchFinally(int mark)
    {
        Consume(TokenType.LeftBrace, "Expected '{' after 'try'");
        var tryBody = ParseStatementList();
        Consume(TokenType.RightBrace, "Expected '}' after try body");

        var catchClauses = new List<CatchClause>();
        List<Expr>? finallyBody = null;

        while (Check(TokenType.Catch))
        {
            Advance(); // consume 'catch'

            string? exceptionTypeName = null;
            Token? variableName = null;

            if (Check(TokenType.LeftParen))
            {
                Advance(); // consume '('

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

                if (Check(TokenType.Identifier))
                {
                    variableName = Advance();
                }

                Consume(TokenType.RightParen, "Expected ')' after catch clause");
            }

            Expr? whenGuard = null;
            if (Check(TokenType.When))
            {
                Advance(); // consume 'when'
                Consume(TokenType.LeftParen, "Expected '(' after 'when'");
                whenGuard = _expression.ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after when guard");
            }

            Consume(TokenType.LeftBrace, "Expected '{' after catch clause");
            var catchBody = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after catch body");

            catchClauses.Add(new CatchClause(exceptionTypeName, variableName, whenGuard, catchBody));
        }

        if (Match(TokenType.Finally))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'finally'");
            finallyBody = ParseStatementList();
            Consume(TokenType.RightBrace, "Expected '}' after finally body");
        }

        if (catchClauses.Count == 0 && finallyBody == null)
            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'catch' or 'finally' after try block");

        // Bare catch (no type AND no when guard) must be last
        for (var i = 0; i < catchClauses.Count - 1; i++)
        {
            if (catchClauses[i].ExceptionTypeName == null && catchClauses[i].WhenGuard == null)
                throw new AlderException(DiagnosticDescriptors.GeneralCatchMustBeLast, Peek().Span, Peek().Line, Peek().Column);
        }

        return new TryCatchFinallyExpr(tryBody, catchClauses, finallyBody) { Span = SpanFrom(mark) };
    }
}
