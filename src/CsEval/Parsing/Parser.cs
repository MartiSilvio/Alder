namespace CsEval.Parsing;

public sealed class Parser(List<Token> tokens)
{
    private int _current;

    public Expr Parse()
    {
        var expr = ParseExpression();
        if (!IsAtEnd())
            throw new ParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
        return expr;
    }

    private Expr ParseExpression() => ParseAssignment();

    private Expr ParseAssignment()
    {
        var expr = ParseNullCoalesce();

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

        return expr;
    }

    private bool MatchCompoundAssignment(out Token op)
    {
        if (Match(TokenType.PlusEqual, TokenType.MinusEqual, TokenType.StarEqual,
                  TokenType.SlashEqual, TokenType.PercentEqual, TokenType.AmpEqual,
                  TokenType.PipeEqual, TokenType.CaretEqual, TokenType.LessLessEqual,
                  TokenType.GreaterGreaterEqual))
        {
            op = Previous();
            return true;
        }
        op = default;
        return false;
    }

    private Expr ParseNullCoalesce()
    {
        var expr = ParseConditional();

        while (Match(TokenType.QuestionQuestion))
        {
            var right = ParseConditional();
            expr = new NullCoalesceExpr(expr, right);
        }

        return expr;
    }

    private Expr ParseConditional()
    {
        var expr = ParseOr();

        if (Match(TokenType.Question))
        {
            var thenBranch = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' in ternary expression");
            var elseBranch = ParseExpression();
            return new ConditionalExpr(expr, thenBranch, elseBranch);
        }

        return expr;
    }

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

    private Expr ParseEquality()
    {
        var expr = ParseComparison();

        while (Match(TokenType.EqualEqual, TokenType.BangEqual))
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

        while (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
        {
            var op = Previous();
            var right = ParseShift();
            expr = new BinaryExpr(expr, op, right);
        }

        return expr;
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

    private Expr ParseUnary()
    {
        if (Match(TokenType.Bang, TokenType.Minus, TokenType.Tilde))
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
                expr = new IndexAccessExpr(expr, index);
            }
            else if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr);
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

    private Expr FinishCall(Expr callee)
    {
        var arguments = new List<Expr>();

        if (!Check(TokenType.RightParen))
        {
            do
            {
                arguments.Add(ParseExpression());
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after arguments");
        return new CallExpr(callee, arguments);
    }

    private Expr ParsePrimary()
    {
        // Literals
        if (Match(TokenType.Number, TokenType.String))
            return new LiteralExpr(Previous().Literal);

        if (Match(TokenType.True))
            return new LiteralExpr(true);

        if (Match(TokenType.False))
            return new LiteralExpr(false);

        if (Match(TokenType.Null))
            return new LiteralExpr(null);

        // Interpolated string
        if (Match(TokenType.InterpolatedString))
            return ParseInterpolatedString(Previous());

        if (Match(TokenType.New))
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'new'");
            return new NewExpr(ParseAnonymousObject());
        }

        // Grouping, lambda, or tuple
        if (Match(TokenType.LeftParen))
            return ParseParenthesized();

        // Array literal: new[] { } or [ ] - support both
        if (Match(TokenType.LeftBracket))
            return ParseArrayLiteral();

        // Block expression: { statements; return expr; }
        if (Match(TokenType.LeftBrace))
            return ParseBlock();

        // Identifier or single-parameter lambda (x => ...)
        if (Match(TokenType.Identifier))
        {
            var identifier = Previous();

            // Check for single-parameter lambda: x => expr
            if (Match(TokenType.Arrow))
            {
                var body = ParseExpression();
                return new LambdaExpr([identifier], body);
            }

            return new IdentifierExpr(identifier);
        }

        throw new ParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
    }

    private Expr ParseParenthesized()
    {
        // Could be: grouping (expr), lambda (x) => ..., or parameter list (a, b) => ...

        // Empty parens - parameterless lambda
        if (Match(TokenType.RightParen))
        {
            Consume(TokenType.Arrow, "Expected '=>' after '()'");
            var body = ParseExpression();
            return new LambdaExpr([], body);
        }

        // Check if this looks like a lambda parameter list
        var savedPosition = _current;
        var parameters = new List<Token>();
        var isLambda = false;

        if (Check(TokenType.Identifier))
        {
            // Try to parse as parameter list
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
            var body = ParseExpression();
            return new LambdaExpr(parameters, body);
        }

        // Not a lambda - backtrack and parse as grouping
        _current = savedPosition;
        var expr = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after expression");
        return new GroupingExpr(expr);
    }

    private Expr ParseArrayLiteral()
    {
        var elements = new List<Expr>();

        if (!Check(TokenType.RightBracket))
        {
            do
            {
                if (Match(TokenType.DotDotDot))
                {
                    // Spread element: ...expr
                    var spreadExpr = ParseExpression();
                    elements.Add(new SpreadExpr(spreadExpr));
                }
                else
                {
                    elements.Add(ParseExpression());
                }
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBracket, "Expected ']' after array elements");
        return new ArrayLiteralExpr(elements);
    }

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

    private Expr ParseAnonymousObject()
    {
        var properties = new List<(Token, Expr)>();

        if (!Check(TokenType.RightBrace))
        {
            do
            {
                if (Match(TokenType.DotDotDot))
                {
                    // Spread property: ...expr
                    var spreadExpr = ParseExpression();
                    // Use a special marker token for spread entries
                    var spreadMarker = new Token(TokenType.DotDotDot, "...", null, 0, 0);
                    properties.Add((spreadMarker, new SpreadExpr(spreadExpr)));
                }
                else
                {
                    var key = Consume(TokenType.Identifier, "Expected property name");
                    Consume(TokenType.Equal, "Expected '=' after property name");
                    var value = ParseExpression();
                    properties.Add((key, value));
                }
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBrace, "Expected '}' after anonymous object");
        return new ObjectLiteralExpr(properties);
    }

    private Expr ParseInterpolatedString(Token token)
    {
        var content = (string)token.Literal!;
        var parts = new List<InterpolatedPart>();
        var sb = new StringBuilder();
        var i = 0;

        while (i < content.Length)
        {
            if (content[i] == '{')
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
                    if (content[i] == '{') braceDepth++;
                    else if (content[i] == '}') braceDepth--;
                    if (braceDepth > 0) i++;
                }

                var exprText = content[exprStart..i];
                i++; // skip }

                var lexer = new Lexer(exprText);
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens);
                var expr = parser.Parse();
                parts.Add(new ExpressionPart(expr));
            }
            else
            {
                sb.Append(content[i]);
                i++;
            }
        }

        if (sb.Length > 0)
            parts.Add(new TextPart(sb.ToString()));

        return new InterpolatedStringExpr(parts);
    }

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool MatchTypeKeyword(out Token typeToken)
    {
        // All C# primitive type keywords that can be used for variable declarations
        if (Check(TokenType.Int) || Check(TokenType.Long) || Check(TokenType.Double) ||
            Check(TokenType.Float) || Check(TokenType.Decimal) || Check(TokenType.StringType) ||
            Check(TokenType.Bool) || Check(TokenType.Object) ||
            Check(TokenType.Sbyte) || Check(TokenType.Byte) || Check(TokenType.Short) ||
            Check(TokenType.Ushort) || Check(TokenType.Uint) || Check(TokenType.Ulong) ||
            Check(TokenType.Char))
        {
            typeToken = Advance();
            return true;
        }
        typeToken = default;
        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.Eof;

    private Token Peek() => tokens[_current];

    private Token Previous() => tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new ParserException($"{message} at {Peek().Line}:{Peek().Column}");
    }
}

public class ParserException(string message) : Exception(message);