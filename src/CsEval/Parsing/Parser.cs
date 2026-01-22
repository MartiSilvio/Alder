namespace CsEval.Parsing
{
    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _current;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
        }

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

            // Handle ??= as an expression (for use in return statements, etc.)
            if (expr is IdentifierExpr identifier && Match(TokenType.QuestionQuestionEqual))
            {
                var value = ParseAssignment();
                return new NullCoalesceAssignExpr(identifier.Name, value);
            }

            return expr;
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
            var expr = ParseEquality();

            while (Match(TokenType.AmpAmp))
            {
                var op = Previous();
                var right = ParseEquality();
                expr = new LogicalExpr(expr, op, right);
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
            var expr = ParseTerm();

            while (Match(TokenType.Less, TokenType.LessEqual, TokenType.Greater, TokenType.GreaterEqual))
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
            if (Match(TokenType.Bang, TokenType.Minus))
            {
                var op = Previous();
                var right = ParseUnary();
                return new UnaryExpr(op, right);
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
                    elements.Add(ParseExpression());
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

            if (Match(TokenType.If))
                return ParseIfStatement();

            if (Match(TokenType.Var))
            {
                var name = Consume(TokenType.Identifier, "Expected variable name");
                Consume(TokenType.Equal, "Expected '=' after variable name");
                var initializer = ParseExpression();
                Consume(TokenType.Semicolon, "Expected ';' after variable declaration");
                return new VariableDeclExpr(name, initializer);
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

        private Expr ParseAnonymousObject()
        {
            var properties = new List<(Token, Expr)>();

            if (!Check(TokenType.RightBrace))
            {
                do
                {
                    var key = Consume(TokenType.Identifier, "Expected property name");
                    Consume(TokenType.Equal, "Expected '=' after property name");
                    var value = ParseExpression();
                    properties.Add((key, value));
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

        private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool IsAtEnd() => Peek().Type == TokenType.Eof;

        private Token Peek() => _tokens[_current];

        private Token Previous() => _tokens[_current - 1];

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new ParserException($"{message} at {Peek().Line}:{Peek().Column}");
        }
    }

    public class ParserException(string message) : Exception(message);
}
