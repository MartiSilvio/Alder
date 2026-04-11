using Alder.Diagnostics;
using Alder.Parsing.Extensions;

namespace Alder.Parsing;

/// <summary>
/// Parses primary expressions.
/// This stage owns literals, identifiers, grouping, lambda entry points, object and collection literals,
/// and the syntax forms that begin with dedicated keywords such as <c>new</c>, <c>typeof</c>, and <c>nameof</c>.
/// </summary>
internal sealed class PrimaryParser : ParserBase
{
    private ExpressionParser _expression = null!;
    private StatementParser _statement = null!;
    private QueryParser _queryParser = null!;

    internal PrimaryParser(ParserState state) : base(state)
    {
    }

    internal void SetExpressionParser(ExpressionParser expression) => _expression = expression;
    internal void SetStatementParser(StatementParser statement) => _statement = statement;
    internal void SetQueryParser(QueryParser queryParser) => _queryParser = queryParser;

    internal Expr ParsePrimary()
    {
        var mark = Mark();

        if (Match(TokenType.Number, TokenType.String, TokenType.Character))
            return new LiteralExpr(Previous().Literal, IsConstant: true) { Span = SpanFrom(mark) };

        if (Match(TokenType.True))
            return new LiteralExpr(true, IsConstant: true) { Span = SpanFrom(mark) };

        if (Match(TokenType.False))
            return new LiteralExpr(false, IsConstant: true) { Span = SpanFrom(mark) };

        if (Match(TokenType.Null))
            return new LiteralExpr(null, IsConstant: true) { Span = SpanFrom(mark) };

        if (Match(TokenType.InterpolatedString))
            return ParseInterpolatedString(Previous(), mark);

        if (Match(TokenType.New))
            return ParseNewExpression(mark);

        if (Match(TokenType.LeftParen))
            return ParseParenthesized(mark);

        if (Match(TokenType.LeftBracket))
        {
            if (State.LanguageMode == LanguageMode.Standard && IsComprehensionAhead())
                throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired, "comprehension");
            return ParseArrayLiteral(mark);
        }

        if (Match(TokenType.LeftBrace))
            return _statement.ParseBlock();

        if (IsTypeKeyword(Peek().Type) && PeekNext().Type == TokenType.Dot)
        {
            var typeToken = Advance();
            return new TypeReferenceExpr(typeToken) { Span = SpanFrom(mark) };
        }

        if (Match(TokenType.Unchecked))
            return ParseCheckedUnchecked("unchecked", mark);

        if (Match(TokenType.Checked))
            return ParseCheckedUnchecked("checked", mark);

        if (Match(TokenType.Typeof))
            return ParseTypeofExpression(mark);

        if (Match(TokenType.Default))
            return ParseDefaultExpression(mark);

        if (Match(TokenType.Nameof))
            return ParseNameofExpression(mark);

        if (Match(TokenType.Sizeof))
            return ParseSizeofExpression(mark);

        // ECMA-334 §12.19: async anonymous functions must be recognized before plain identifier parsing.
        if (Check(TokenType.Async) && IsAsyncLambdaAhead())
        {
            Advance();
            if (Match(TokenType.LeftParen))
                return ParseAsyncParenthesizedLambda(mark);
            Advance();
            return ParseAsyncSingleParamLambda(mark);
        }

        // ECMA-334 §12.19: anonymous method expressions begin with delegate.
        if (Match(TokenType.Delegate))
            return ParseAnonymousDelegate(mark);

        if (Match(TokenType.Identifier))
            return ParseIdentifier(mark);

        // ECMA-334 §12.20: query syntax must win before contextual-keyword fallback turns "from" into an identifier.
        if (Check(TokenType.From) && _queryParser.IsQueryExpressionStart())
            return _queryParser.ParseQueryExpression();

        // ECMA-334 §6.4.4: contextual keywords remain legal identifiers outside their contextual positions.
        if (IsContextualKeyword(Peek().Type))
        {
            Advance();
            return ParseIdentifier(mark);
        }

        throw SyntaxError(DiagnosticDescriptors.InvalidExpressionTerm, Peek().Lexeme);
    }

    private Expr ParseArrayLiteral(int mark)
    {
        if (Check(TokenType.RightBracket))
        {
            Consume(TokenType.RightBracket, "Expected ']' after array elements");
            return new CollectionExpr([]) { Span = SpanFrom(mark) };
        }

        var elements = new List<Expr>();
        Expr firstElement;
        if (Match(TokenType.DotDot))
        {
            if (State.LanguageMode == LanguageMode.Standard)
                throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired,TokenLexemes.GetCanonical(TokenType.DotDot));
            var spreadMark = Mark();
            firstElement = new SpreadExpr(_expression.ParseExpression()) { Span = SpanFrom(spreadMark) };
        }
        else
        {
            firstElement = _expression.ParseExpression();
        }

        if (firstElement is not SpreadExpr && Match(TokenType.For))
            return ParseComprehension(firstElement, mark);

        elements.Add(firstElement);
        while (Match(TokenType.Comma))
        {
            if (Match(TokenType.DotDot))
            {
                if (State.LanguageMode == LanguageMode.Standard)
                    throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired,TokenLexemes.GetCanonical(TokenType.DotDot));
                var spreadMark = Mark();
                var spreadExpr = _expression.ParseExpression();
                elements.Add(new SpreadExpr(spreadExpr) { Span = SpanFrom(spreadMark) });
            }
            else
            {
                elements.Add(_expression.ParseExpression());
            }
        }

        Consume(TokenType.RightBracket, "Expected ']' after array elements");
        return new CollectionExpr(elements) { Span = SpanFrom(mark) };
    }

    private Expr ParseComprehension(Expr projection, int mark)
    {
        var rangeVariable = ConsumeIdentifierOrContextualKeyword("Expected identifier after 'for' in comprehension");
        Consume(TokenType.In, "Expected 'in' in comprehension");
        var source = _expression.ParseExpression();

        Expr? filter = null;
        if (Match(TokenType.If))
            filter = _expression.ParseExpression();

        Consume(TokenType.RightBracket, "Expected ']' after comprehension");

        var lambdaParameter = new LambdaParameter(null, rangeVariable);
        Expr query = source;

        if (filter != null)
        {
            var whereToken = new Token(TokenType.Identifier, "Where", null, rangeVariable.Line, rangeVariable.Column);
            var whereLambda = new LambdaExpr([lambdaParameter], filter) { Span = SpanFrom(mark) };
            query = new CallExpr(new MemberAccessExpr(query, whereToken, false) { Span = SpanFrom(mark) }, [whereLambda]) { Span = SpanFrom(mark) };
        }

        var selectToken = new Token(TokenType.Identifier, "Select", null, rangeVariable.Line, rangeVariable.Column);
        var selectLambda = new LambdaExpr([lambdaParameter], projection) { Span = SpanFrom(mark) };
        var selectCall = new CallExpr(new MemberAccessExpr(query, selectToken, false) { Span = SpanFrom(mark) }, [selectLambda]) { Span = SpanFrom(mark) };
        var toArrayToken = new Token(TokenType.Identifier, "ToArray", null, rangeVariable.Line, rangeVariable.Column);
        return new CallExpr(new MemberAccessExpr(selectCall, toArrayToken, false) { Span = SpanFrom(mark) }, []) { Span = SpanFrom(mark) };
    }

    private List<Expr> ParseBraceElementList()
    {
        var elements = new List<Expr>();

        if (!Check(TokenType.RightBrace))
        {
            do
            {
                if (Match(TokenType.DotDot))
                {
                    if (State.LanguageMode == LanguageMode.Standard)
                        throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired,TokenLexemes.GetCanonical(TokenType.DotDot));
                    var spreadMark = Mark();
                    var spreadExpr = _expression.ParseExpression();
                    elements.Add(new SpreadExpr(spreadExpr) { Span = SpanFrom(spreadMark) });
                }
                else
                {
                    elements.Add(_expression.ParseExpression());
                }
            } while (Match(TokenType.Comma) && !Check(TokenType.RightBrace));
        }

        Consume(TokenType.RightBrace, "Expected '}' after array elements");
        return elements;
    }

    private bool IsComprehensionAhead()
    {
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;

        for (var i = State.Current; i < State.Tokens.Count; i++)
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
                    if (bracketDepth == 0)
                        return false;
                    bracketDepth--;
                    break;
                case TokenType.LeftBrace:
                    braceDepth++;
                    break;
                case TokenType.RightBrace:
                    braceDepth = Math.Max(0, braceDepth - 1);
                    break;
                case TokenType.For:
                    if (parenDepth == 0 && bracketDepth == 0 && braceDepth == 0)
                        return true;
                    break;
                case TokenType.Eof:
                    return false;
            }
        }

        return false;
    }

    private Expr ParseNewExpression(int mark)
    {
        // new[] { ... } - implicitly typed array
        if (Match(TokenType.LeftBracket))
        {
            Consume(TokenType.RightBracket, "Expected ']' after 'new['");
            Consume(TokenType.LeftBrace, "Expected '{' after 'new[]'");
            return new ImplicitArrayCreationExpr(ParseBraceElementList()) { Span = SpanFrom(mark) };
        }

        // new { ... } - anonymous object
        if (Check(TokenType.LeftBrace))
        {
            Advance(); // consume '{'
            return new NewExpr(ObjectLiteralParser.ParseAnonymousObject(this, () => _expression.ParseExpression())) { Span = SpanFrom(mark) };
        }

        // new ClassName(args) or target-typed new() (ECMA-334 §12.8.16.2)
        if (Check(TokenType.Identifier) || IsTypeKeyword(Peek().Type) || Check(TokenType.LeftParen))
        {
            return ParseObjectCreation(mark);
        }

        throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'{', '[', or type name after 'new'");
    }

    /// <summary>
    /// Parses new ClassName(args) constructor invocation.
    /// Called after 'new' has been consumed and next token is an identifier or type keyword.
    /// </summary>
    private Expr ParseObjectCreation(int mark)
    {
        // ECMA-334 §12.8.16.2: Target-typed new (type inferred from declaration context)
        if (Check(TokenType.LeftParen))
        {
            Advance(); // consume '('
            var ttArgs = new List<Expr>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    ttArgs.Add(_expression.ParseArgument());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after constructor arguments");

            ObjectInitializer? ttInit = null;
            if (Check(TokenType.LeftBrace))
                ttInit = ParseObjectInitializer();

            return new ObjectCreationExpr("", ttArgs, ttInit) { Span = SpanFrom(mark) };
        }

        string typeName;
        if (IsTypeKeyword(Peek().Type))
        {
            typeName = Advance().Lexeme;
        }
        else
        {
            typeName = Consume(TokenType.Identifier, "Expected '{', '[', or type name after 'new'").Lexeme;
            while (Match(TokenType.Dot))
            {
                var next = Consume(TokenType.Identifier, "Expected identifier after '.'");
                typeName += "." + next.Lexeme;
            }
        }

        if (Check(TokenType.Less))
        {
            Advance(); // consume <
            typeName += "<";

            var firstArg = TryParseTypeName();
            if (firstArg == null)
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type argument after '<'");
            typeName += firstArg;

            while (Match(TokenType.Comma))
            {
                typeName += ", ";
                var nextArg = TryParseTypeName();
                if (nextArg == null)
                    throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type argument after ','");
                typeName += nextArg;
            }

            if (!MatchClosingAngleBracket())
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'>' after generic type arguments");
            typeName += ">";
        }

        // After 'new Type', ? is always nullable suffix (ternary makes no sense after 'new')
        if (Check(TokenType.Question))
        {
            Advance();
            typeName += "?";
        }

        // The lexer tokenizes ?[ as a single QuestionLeftBracket token,
        // so add ? to typeName and jump into the array creation logic.
        if (Match(TokenType.QuestionLeftBracket))
        {
            typeName += "?";
            return ParseArrayCreationBody(typeName, mark);
        }

        // ECMA-334 §12.8.16.4
        if (Check(TokenType.LeftBracket))
        {
            Advance(); // consume '['
            return ParseArrayCreationBody(typeName, mark);
        }

        // Parentheses may be omitted with initializer: new X { Prop = val }
        var arguments = new List<Expr>();
        if (Check(TokenType.LeftParen))
        {
            Advance(); // consume '('
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(_expression.ParseArgument());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after constructor arguments");
        }

        ObjectInitializer? initializer = null;
        if (Check(TokenType.LeftBrace))
        {
            initializer = ParseObjectInitializer();
        }

        return new ObjectCreationExpr(typeName, arguments, initializer) { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses the body of an array creation expression after '[' has been consumed.
    /// Handles: new int[] { ... }, new int[10], new int[3, 3]
    /// Called from ParseObjectCreation for both regular (LeftBracket) and nullable (QuestionLeftBracket) paths.
    /// </summary>
    private Expr ParseArrayCreationBody(string typeName, int mark)
    {
        if (Check(TokenType.Comma))
        {
            var rank = 1;
            while (Match(TokenType.Comma))
                rank++;
            Consume(TokenType.RightBracket, "Expected ']' after array rank specifier");
            if (!Check(TokenType.LeftBrace))
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, $"'{{' after 'new {typeName}[{new string(',', rank - 1)}]'");
            Advance(); // consume '{'
            var initExpr = ParseMultiDimArrayInitializer(typeName, rank, mark);
            return initExpr;
        }

        if (Check(TokenType.RightBracket))
        {
            Advance(); // consume ']'

            var elementTypeName = typeName;
            while (Check(TokenType.LeftBracket) && CheckNext(TokenType.RightBracket))
            {
                Advance(); // consume '['
                Advance(); // consume ']'
                elementTypeName += "[]";
            }

            if (Check(TokenType.LeftBrace))
            {
                Advance(); // consume '{'
                return new TypedArrayLiteralExpr(elementTypeName, ParseBraceElementList()) { Span = SpanFrom(mark) };
            }

            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, $"'{{' after 'new {elementTypeName}[]'");
        }

        var firstSize = _expression.ParseExpression();
        if (Check(TokenType.Comma))
        {
            var sizes = new List<Expr> { firstSize };
            while (Match(TokenType.Comma))
                sizes.Add(_expression.ParseExpression());
            Consume(TokenType.RightBracket, "Expected ']' after array sizes");

            if (Check(TokenType.LeftBrace))
            {
                Advance(); // consume '{'
                var rank = sizes.Count;
                var flatValues = new List<Expr>();
                var dimensions = new int[rank];
                ParseNestedArrayInitializer(flatValues, dimensions, 0, rank);
                return new MultiDimArrayInitExpr(typeName, rank, sizes, flatValues, dimensions) { Span = SpanFrom(mark) };
            }

            return new MultiDimTypedArrayCreationExpr(typeName, sizes) { Span = SpanFrom(mark) };
        }
        Consume(TokenType.RightBracket, "Expected ']' after array size");

        var jaggedTypeName = typeName;
        while (Check(TokenType.LeftBracket) && CheckNext(TokenType.RightBracket))
        {
            Advance(); // consume '['
            Advance(); // consume ']'
            jaggedTypeName += "[]";
        }

        // §12.8.16.5: sized jagged array with initializer
        if (Check(TokenType.LeftBrace))
        {
            Advance(); // consume '{'
            return new TypedArrayLiteralExpr(jaggedTypeName, ParseBraceElementList()) { Span = SpanFrom(mark) };
        }

        return new TypedArrayCreationExpr(jaggedTypeName, firstSize) { Span = SpanFrom(mark) };
    }

    private Expr ParseMultiDimArrayInitializer(string typeName, int rank, int mark)
    {
        var flatValues = new List<Expr>();
        var dimensions = new int[rank];
        ParseNestedArrayInitializer(flatValues, dimensions, 0, rank);
        return new MultiDimArrayInitExpr(typeName, rank, null, flatValues, dimensions) { Span = SpanFrom(mark) };
    }

    private void ParseNestedArrayInitializer(List<Expr> flatValues, int[] dimensions, int depth, int rank)
    {
        var count = 0;
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (count > 0) Consume(TokenType.Comma, "Expected ',' between array elements");

            if (depth < rank - 1)
            {
                Consume(TokenType.LeftBrace, "Expected '{' in nested array initializer");
                ParseNestedArrayInitializer(flatValues, dimensions, depth + 1, rank);
            }
            else
            {
                flatValues.Add(_expression.ParseExpression());
            }
            count++;
        }
        Consume(TokenType.RightBrace, "Expected '}' after array initializer");

        if (dimensions[depth] == 0)
            dimensions[depth] = count;
    }

    /// <summary>
    /// ECMA-334 §12.8.16.3 - Object initializers / §12.8.16.6 - Collection initializers
    /// </summary>
    private ObjectInitializer ParseObjectInitializer()
    {
        Consume(TokenType.LeftBrace, "Expected '{' for object initializer");
        var entries = new List<InitializerEntry>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Equal)
            {
                var propName = Advance().Lexeme;
                Advance(); // consume =
                var value = _expression.ParseExpression();
                entries.Add(new InitializerEntry(propName, value));
            }
            else if (Check(TokenType.LeftBracket))
            {
                // §12.8.16.3: Indexer initializer
                Advance(); // consume [
                var key = _expression.ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after indexer key in initializer");
                Consume(TokenType.Equal, "Expected '=' after indexer key in initializer");
                var value = _expression.ParseExpression();
                entries.Add(new InitializerEntry(null, value, key));
            }
            else if (Check(TokenType.LeftBrace))
            {
                // §12.8.16.6: Grouped element initializer, calls Add(expr, expr, ...)
                Advance(); // consume '{'
                var elements = new List<Expr>();
                do
                {
                    elements.Add(_expression.ParseExpression());
                } while (Match(TokenType.Comma));
                Consume(TokenType.RightBrace, "Expected '}' after element initializer");
                entries.Add(new InitializerEntry(null, Elements: elements));
            }
            else
            {
                var value = _expression.ParseExpression();
                entries.Add(new InitializerEntry(null, value));
            }

            if (!Match(TokenType.Comma))
                break;
        }

        Consume(TokenType.RightBrace, "Expected '}' after object initializer");
        return new ObjectInitializer(entries);
    }

    private Expr ParseCheckedUnchecked(string keyword, int mark)
    {
        if (Match(TokenType.LeftBrace))
        {
            var block = _statement.ParseBlock();
            return new CheckedExpr(block, keyword == "checked") { Span = SpanFrom(mark) };
        }

        Consume(TokenType.LeftParen, $"Expected '(' or '{{' after '{keyword}'");
        var expr = _expression.ParseExpression();
        Consume(TokenType.RightParen, $"Expected ')' after {keyword} expression");
        return new CheckedExpr(expr, keyword == "checked") { Span = SpanFrom(mark) };
    }

    private Expr ParseTypeofExpression(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'typeof'");
        Token typeToken;
        if (Match(TokenType.Void))
        {
            typeToken = Previous();
        }
        else
        {
            var typeName = TryParseTypeName();
            if (typeName == null)
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type name after 'typeof('");
            typeToken = new Token(TokenType.Identifier, typeName, null, Peek().Line, Peek().Column);
        }
        Consume(TokenType.RightParen, "Expected ')' after typeof type");
        return new TypeofExpr(typeToken) { Span = SpanFrom(mark) };
    }

    private Expr ParseDefaultExpression(int mark)
    {
        if (Match(TokenType.LeftParen))
        {
            var typeName = TryParseTypeName();
            if (typeName == null)
                throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type after 'default('");
            var typeToken = new Token(TokenType.Identifier, typeName, null, Previous().Line, Previous().Column);
            Consume(TokenType.RightParen, "Expected ')' after default type");
            return new DefaultExpr(typeToken) { Span = SpanFrom(mark) };
        }
        return new DefaultExpr(null) { Span = SpanFrom(mark) };
    }

    private Expr ParseNameofExpression(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'nameof'");
        var name = StripVerbatimPrefix(Consume(TokenType.Identifier, "Expected identifier after 'nameof('").Lexeme);

        if (Check(TokenType.Less))
        {
            Advance();
            var depth = 1;
            while (depth > 0 && !IsAtEnd())
            {
                if (Check(TokenType.Less)) depth++;
                else if (Check(TokenType.Greater)) depth--;
                Advance();
            }
        }

        while (Match(TokenType.Dot))
        {
            name = StripVerbatimPrefix(Consume(TokenType.Identifier, "Expected identifier after '.'").Lexeme);
            if (Check(TokenType.Less))
            {
                Advance();
                var depth = 1;
                while (depth > 0 && !IsAtEnd())
                {
                    if (Check(TokenType.Less)) depth++;
                    else if (Check(TokenType.Greater)) depth--;
                    Advance();
                }
            }
        }

        Consume(TokenType.RightParen, "Expected ')' after nameof expression");
        return new NameofExpr(name) { Span = SpanFrom(mark) };
    }

    private static string StripVerbatimPrefix(string lexeme) =>
        lexeme.Length > 1 && lexeme[0] == '@' ? lexeme[1..] : lexeme;

    private Expr ParseSizeofExpression(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'sizeof'");
        string typeName;
        if (IsTypeKeyword(Peek().Type))
        {
            MatchTypeKeyword(out var typeToken);
            typeName = typeToken.Lexeme;
        }
        else
        {
            typeName = Consume(TokenType.Identifier, "Expected type name in sizeof").Lexeme;
        }
        Consume(TokenType.RightParen, "Expected ')' after sizeof type");
        return new SizeofExpr(typeName) { Span = SpanFrom(mark) };
    }

    // §12.19: look ahead to distinguish `async x => ...` and `async (x) => ...` from `async` as identifier
    private bool IsAsyncLambdaAhead()
    {
        var next = PeekNext();
        if (next.Type == TokenType.LeftParen)
            return true;
        // async x => ...: identifier followed by =>
        if (next.Type == TokenType.Identifier && PeekAt(2).Type == TokenType.Arrow)
            return true;
        return false;
    }

    private Expr ParseAsyncSingleParamLambda(int mark)
    {
        var identifier = Previous();
        Consume(TokenType.Arrow, "Expected '=>' after async lambda parameter");
        var body = _expression.ParseExpression();
        return new LambdaExpr([new LambdaParameter(null, identifier)], body, IsAsync: true) { Span = SpanFrom(mark) };
    }

    private Expr ParseAsyncParenthesizedLambda(int mark)
    {
        if (Match(TokenType.RightParen))
        {
            Consume(TokenType.Arrow, "Expected '=>' after 'async ()'");
            var body = _expression.ParseExpression();
            return new LambdaExpr([], body, IsAsync: true) { Span = SpanFrom(mark) };
        }

        var typed = TryParseTypedLambda(mark, isAsync: true);
        if (typed != null) return typed;

        var parameters = new List<LambdaParameter>();
        do
        {
            var param = Consume(TokenType.Identifier, "Expected parameter name");
            parameters.Add(new LambdaParameter(null, param));
        } while (Match(TokenType.Comma));

        Consume(TokenType.RightParen, "Expected ')' after lambda parameters");
        Consume(TokenType.Arrow, "Expected '=>' after async lambda parameters");
        var lambdaBody = _expression.ParseExpression();
        return new LambdaExpr(parameters, lambdaBody, IsAsync: true) { Span = SpanFrom(mark) };
    }

    private Expr ParseIdentifier(int mark)
    {
        var identifier = Previous();

        if (Match(TokenType.Arrow))
        {
            var body = _expression.ParseExpression();
            return new LambdaExpr([new LambdaParameter(null, identifier)], body) { Span = SpanFrom(mark) };
        }

        return new IdentifierExpr(identifier) { Span = SpanFrom(mark) };
    }

    private Expr ParseParenthesized(int mark)
    {
        if (Match(TokenType.RightParen))
        {
            Consume(TokenType.Arrow, "Expected '=>' after '()'");
            var body = _expression.ParseExpression();
            return new LambdaExpr([], body) { Span = SpanFrom(mark) };
        }

        var typedLambdaResult = TryParseTypedLambda(mark);
        if (typedLambdaResult != null)
            return typedLambdaResult;

        var savedPosition = State.Current;
        var parameters = new List<LambdaParameter>();
        var isLambda = false;

        if (Check(TokenType.Identifier))
        {
            parameters.Add(new LambdaParameter(null, Advance()));
            while (Match(TokenType.Comma))
            {
                if (!Check(TokenType.Identifier))
                    break;
                parameters.Add(new LambdaParameter(null, Advance()));
            }

            if (Match(TokenType.RightParen) && Match(TokenType.Arrow))
            {
                isLambda = true;
            }
        }

        if (isLambda)
        {
            var body = _expression.ParseExpression();
            return new LambdaExpr(parameters, body) { Span = SpanFrom(mark) };
        }

        State.Current = savedPosition;

        var firstElement = ParseTupleElement();

        if (Check(TokenType.Comma))
        {
            var elements = new List<TupleElement> { firstElement };
            while (Match(TokenType.Comma))
            {
                elements.Add(ParseTupleElement());
            }
            Consume(TokenType.RightParen, "Expected ')' after tuple elements");
            return new TupleExpr(elements) { Span = SpanFrom(mark) };
        }

        Consume(TokenType.RightParen, "Expected ')' after expression");
        return firstElement.Expression;
    }

    private TupleElement ParseTupleElement()
    {
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
        {
            var nameToken = Advance();
            Advance(); // consume colon
            var expr = _expression.ParseExpression();
            return new TupleElement(nameToken.Lexeme, expr);
        }

        var expression = _expression.ParseExpression();
        return new TupleElement(null, expression);
    }

    /// <summary>
    /// Attempts to parse a typed lambda: (type name, type name, ...) => body.
    /// Returns null if the pattern doesn't match (restores position on failure).
    /// ECMA-334 §12.19
    /// </summary>
    private Expr? TryParseTypedLambda(int mark, bool isAsync = false)
    {
        var saved = State.Current;

        try
        {
            var parameters = new List<LambdaParameter>();

            var firstType = TryParseTypeName();
            if (firstType == null || !Check(TokenType.Identifier))
            {
                State.Current = saved;
                return null;
            }

            parameters.Add(new LambdaParameter(firstType, Advance()));

            while (Match(TokenType.Comma))
            {
                var paramType = TryParseTypeName();
                if (paramType == null || !Check(TokenType.Identifier))
                {
                    State.Current = saved;
                    return null;
                }
                parameters.Add(new LambdaParameter(paramType, Advance()));
            }

            if (!Match(TokenType.RightParen) || !Match(TokenType.Arrow))
            {
                State.Current = saved;
                return null;
            }

            Expr body;
            if (Check(TokenType.LeftBrace))
            {
                Advance(); // consume '{'
                body = _statement.ParseBlock();
            }
            else
            {
                body = _expression.ParseExpression();
            }
            return new LambdaExpr(parameters, body, isAsync) { Span = SpanFrom(mark) };
        }
        catch (AlderException)
        {
            State.Current = saved;
            return null;
        }
    }

    // §12.19
    private Expr ParseAnonymousDelegate(int mark)
    {
        var parameters = new List<LambdaParameter>();

        if (Match(TokenType.LeftParen))
        {
            if (!Check(TokenType.RightParen))
            {
                while (true)
                {
                    var paramType = TryParseTypeName();
                    if (paramType == null)
                        throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type in anonymous delegate parameter list");
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name");
                    parameters.Add(new LambdaParameter(paramType, paramName));
                    if (!Match(TokenType.Comma))
                        break;
                }
            }

            Consume(TokenType.RightParen, "Expected ')' after anonymous delegate parameter list");
        }

        Consume(TokenType.LeftBrace, "Expected '{' for anonymous delegate body");
        var body = _statement.ParseBlock();
        return new LambdaExpr(parameters, body) { Span = SpanFrom(mark) };
    }

    private InterpolatedStringExpr ParseInterpolatedString(Token token, int mark)
    {
        var content = (string)token.Literal!;
        var parts = new List<InterpolatedPart>();
        var sb = new StringBuilder();
        var i = 0;

        while (i < content.Length)
        {
            switch (content[i])
            {
                case '{' when i + 1 < content.Length && content[i + 1] == '{':
                    sb.Append('{');
                    i += 2;
                    continue;
                case '{':
                {
                    if (sb.Length > 0)
                    {
                        parts.Add(new TextPart(sb.ToString()));
                        sb.Clear();
                    }

                    i++; // skip {
                    var exprStart = i;
                    var braceDepth = 1;
                    var parenDepth = 0;
                    var bracketDepth = 0;
                    string? alignmentSpec = null;
                    string? formatSpec = null;
                    var exprEnd = -1;

                    while (i < content.Length && braceDepth > 0)
                    {
                        var ch = content[i];

                        switch (ch)
                        {
                            case '{':
                                braceDepth++;
                                break;
                            case '}':
                                braceDepth--;
                                break;
                            case '(':
                                if (braceDepth == 1) parenDepth++;
                                break;
                            case ')':
                                if (braceDepth == 1) parenDepth--;
                                break;
                            case '[':
                                if (braceDepth == 1) bracketDepth++;
                                break;
                            case ']':
                                if (braceDepth == 1) bracketDepth--;
                                break;
                            case ',' when braceDepth == 1 && parenDepth == 0 && bracketDepth == 0 && alignmentSpec == null && formatSpec == null:
                            {
                                exprEnd = i;
                                i++; // skip ,
                                var alignStart = i;
                                while (i < content.Length && content[i] != ':' && content[i] != '}')
                                    i++;
                                alignmentSpec = content[alignStart..i].Trim();
                                if (i < content.Length && content[i] == ':')
                                {
                                    i++; // skip :
                                    var fmtStart = i;
                                    while (i < content.Length && content[i] != '}')
                                        i++;
                                    formatSpec = content[fmtStart..i];
                                }
                                continue;
                            }
                            case ':' when braceDepth == 1 && parenDepth == 0 && bracketDepth == 0 && alignmentSpec == null && formatSpec == null:
                            {
                                exprEnd = i;
                                i++; // skip :
                                var fmtStart = i;
                                while (i < content.Length && content[i] != '}')
                                    i++;
                                formatSpec = content[fmtStart..i];
                                continue;
                            }
                        }

                        if (braceDepth > 0) i++;
                    }

                    var exprText = exprEnd >= 0 ? content[exprStart..exprEnd] : content[exprStart..i];
                    i++; // skip }

                    var lexer = new Lexer(exprText);
                    var parserTokens = lexer.Tokenize();
                    var subParser = ExpressionParser.CreateForSubExpression(parserTokens, State.LanguageMode);
                    var expr = subParser.ParseExpression();
                    parts.Add(new ExpressionPart(expr, alignmentSpec, formatSpec));
                    break;
                }
                case '}' when i + 1 < content.Length && content[i + 1] == '}':
                    sb.Append('}');
                    i += 2;
                    continue;
                case '}':
                    sb.Append(content[i]);
                    i++;
                    break;
                default:
                    sb.Append(content[i]);
                    i++;
                    break;
            }
        }

        if (sb.Length > 0)
            parts.Add(new TextPart(sb.ToString()));

        return new InterpolatedStringExpr(parts) { Span = SpanFrom(mark) };
    }
}
