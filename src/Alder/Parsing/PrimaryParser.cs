using Alder.Diagnostics;
using Alder.Parsing.Extensions;

namespace Alder.Parsing;

/// <summary>
/// Parses primary expressions: literals, identifiers, new expressions, casts, groupings,
/// lambdas, tuples, array/object literals, typeof, nameof, default, interpolated strings.
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

    #region Primary Dispatch

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
            if (State.LanguageMode == LanguageMode.Standard)
            {
                if (IsComprehensionAhead())
                    throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired,"comprehension");
                throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired,TokenLexemes.CollectionExpressionLiteral);
            }
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

        if (Match(TokenType.Identifier))
            return ParseIdentifier(mark);

        // Query expression: from x in source where ... select ...
        // ECMA-334 §12.20 - Must check before contextual keyword fallback
        if (Check(TokenType.From) && _queryParser.IsQueryExpressionStart())
            return _queryParser.ParseQueryExpression();

        // Contextual keywords can be used as identifiers in expression contexts
        // ECMA-334 §6.4.4 - e.g., var from = 5; return from + 1;
        if (IsContextualKeyword(Peek().Type))
        {
            Advance();
            return ParseIdentifier(mark);
        }

        throw SyntaxError(DiagnosticDescriptors.InvalidExpressionTerm, Peek().Lexeme);
    }

    #endregion

    #region Array Literals

    private Expr ParseArrayLiteral(int mark)
    {
        if (Check(TokenType.RightBracket))
        {
            Consume(TokenType.RightBracket, "Expected ']' after array elements");
            return new ArrayLiteralExpr([]) { Span = SpanFrom(mark) };
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
        return new ArrayLiteralExpr(elements) { Span = SpanFrom(mark) };
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

    private Expr ParseArrayLiteralBody(int mark)
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
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightBrace, "Expected '}' after array elements");
        return new ArrayLiteralExpr(elements) { Span = SpanFrom(mark) };
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

    #endregion

    #region New Expression

    private Expr ParseNewExpression(int mark)
    {
        // new[] { ... } - implicitly typed array
        if (Match(TokenType.LeftBracket))
        {
            Consume(TokenType.RightBracket, "Expected ']' after 'new['");
            Consume(TokenType.LeftBrace, "Expected '{' after 'new[]'");
            return ParseArrayLiteralBody(mark);
        }

        // new { ... } - anonymous object
        if (Check(TokenType.LeftBrace))
        {
            Advance(); // consume '{'
            return new NewExpr(ObjectLiteralParser.ParseAnonymousObject(this, () => _expression.ParseExpression())) { Span = SpanFrom(mark) };
        }

        // new ClassName(args) - constructor invocation (ECMA-334 §12.8.16.2)
        if (Check(TokenType.Identifier) || IsTypeKeyword(Peek().Type))
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
        // Parse type name (could be simple like Exception or dotted like System.ArgumentException)
        string typeName;
        if (IsTypeKeyword(Peek().Type))
        {
            typeName = Advance().Lexeme;
        }
        else
        {
            typeName = Consume(TokenType.Identifier, "Expected type name after 'new'").Lexeme;
            // Support dotted names: System.Exception, System.Collections.Generic.List
            while (Match(TokenType.Dot))
            {
                var next = Consume(TokenType.Identifier, "Expected identifier after '.'");
                typeName += "." + next.Lexeme;
            }
        }

        // Support generic type arguments: List<int>, Dictionary<string, int>
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

        // Handle nullable type suffix: new int?[] or new int?(42)
        // After 'new Type', ? is always nullable suffix (ternary makes no sense after 'new')
        if (Check(TokenType.Question))
        {
            Advance(); // consume ?
            typeName += "?";
        }

        // Handle nullable array creation: new int?[] or new int?[size]
        // The lexer tokenizes ?[ as a single QuestionLeftBracket token,
        // so we handle it here by adding ? to typeName and jumping into
        // the array creation logic (the [ has already been consumed).
        if (Match(TokenType.QuestionLeftBracket))
        {
            typeName += "?";
            return ParseArrayCreationBody(typeName, mark);
        }

        // Check for array creation syntax: new TypeName[size] or new TypeName[] { ... }
        // ECMA-334 §12.8.16.4 - must check before constructor path
        if (Check(TokenType.LeftBracket))
        {
            Advance(); // consume '['
            return ParseArrayCreationBody(typeName, mark);
        }

        // Parse optional argument list - parentheses may be omitted with initializer: new X { Prop = val }
        var arguments = new List<Expr>();
        if (Check(TokenType.LeftParen))
        {
            Advance(); // consume '('
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(_expression.ParseExpression());
                } while (Match(TokenType.Comma));
            }
            Consume(TokenType.RightParen, "Expected ')' after constructor arguments");
        }

        // Check for object/collection initializer: new X() { ... } or new X { ... }
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
        // Unsized multidimensional array: new int[,] { ... }, new int[,,] { ... }
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

        // Check for array initializer: new int[] { ... }
        if (Check(TokenType.RightBracket))
        {
            Advance(); // consume ']'

            // Jagged array initializer: new int[][] { ... }, new double[][][] { ... }
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
                var arrayLiteral = (ArrayLiteralExpr)ParseArrayLiteralBody(mark);
                return new TypedArrayLiteralExpr(elementTypeName, arrayLiteral) { Span = SpanFrom(mark) };
            }

            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, $"'{{' after 'new {elementTypeName}[]'");
        }

        // Array with size: new int[10] or multi-dim: new int[3, 3]
        var firstSize = _expression.ParseExpression();
        if (Check(TokenType.Comma))
        {
            // Multi-dimensional: new int[3, 3] or new int[2, 3] { ... }
            var sizes = new List<Expr> { firstSize };
            while (Match(TokenType.Comma))
                sizes.Add(_expression.ParseExpression());
            Consume(TokenType.RightBracket, "Expected ']' after array sizes");

            // Check for optional initializer
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

        // Jagged array: new int[3][] or new int[3][][]
        var jaggedTypeName = typeName;
        while (Check(TokenType.LeftBracket) && CheckNext(TokenType.RightBracket))
        {
            Advance(); // consume '['
            Advance(); // consume ']'
            jaggedTypeName += "[]";
        }

        // §12.8.16.5: sized jagged array with initializer — new int[3][] { ... }
        if (Check(TokenType.LeftBrace))
        {
            Advance(); // consume '{'
            var arrayLiteral = (ArrayLiteralExpr)ParseArrayLiteralBody(mark);
            return new TypedArrayLiteralExpr(jaggedTypeName, arrayLiteral) { Span = SpanFrom(mark) };
        }

        return new TypedArrayCreationExpr(jaggedTypeName, firstSize) { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses a multidimensional array initializer: new int[,] { {1,2}, {3,4} }
    /// '{' has already been consumed. Flattens nested braces into a flat value list.
    /// </summary>
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
    /// Parses an object or collection initializer: { Name = value, ... } or { elem1, elem2, ... }
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
                // Property initializer: Name = value
                var propName = Advance().Lexeme;
                Advance(); // consume =
                var value = _expression.ParseExpression();
                entries.Add(new InitializerEntry(propName, value));
            }
            else if (Check(TokenType.LeftBracket))
            {
                // Indexer initializer: [key] = value (§12.8.16.3)
                Advance(); // consume [
                var key = _expression.ParseExpression();
                Consume(TokenType.RightBracket, "Expected ']' after indexer key in initializer");
                Consume(TokenType.Equal, "Expected '=' after indexer key in initializer");
                var value = _expression.ParseExpression();
                entries.Add(new InitializerEntry(null, value, key));
            }
            else
            {
                // Collection initializer element
                var value = _expression.ParseExpression();
                entries.Add(new InitializerEntry(null, value));
            }

            if (!Match(TokenType.Comma))
                break; // no trailing comma required
        }

        Consume(TokenType.RightBrace, "Expected '}' after object initializer");
        return new ObjectInitializer(entries);
    }

    #endregion

    #region Checked / Unchecked

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

    #endregion

    #region Typeof Expression

    private Expr ParseTypeofExpression(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'typeof'");
        // Accept type keywords, void, or identifiers (for non-built-in types)
        // Use TryParseTypeName to handle generics: typeof(List<int>), typeof(Dictionary<string, int>)
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

    #endregion

    #region Default Expression

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
        // bare default literal (C# 7.1+)
        return new DefaultExpr(null) { Span = SpanFrom(mark) };
    }

    #endregion

    #region Nameof Expression

    private Expr ParseNameofExpression(int mark)
    {
        Consume(TokenType.LeftParen, "Expected '(' after 'nameof'");
        // Parse the name chain (x, x.y, x.y.z, etc.)
        // Strip verbatim @ prefix — C# spec: @ is not part of the identifier name.
        var name = StripVerbatimPrefix(Consume(TokenType.Identifier, "Expected identifier after 'nameof('").Lexeme);
        while (Match(TokenType.Dot))
        {
            name = StripVerbatimPrefix(Consume(TokenType.Identifier, "Expected identifier after '.'").Lexeme);
        }
        Consume(TokenType.RightParen, "Expected ')' after nameof expression");
        return new NameofExpr(name) { Span = SpanFrom(mark) };
    }

    private static string StripVerbatimPrefix(string lexeme) =>
        lexeme.Length > 1 && lexeme[0] == '@' ? lexeme[1..] : lexeme;

    #endregion

    #region Sizeof Expression

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

    #endregion

    #region Identifier and Lambda

    private Expr ParseIdentifier(int mark)
    {
        var identifier = Previous();

        // Check for single-parameter lambda: x => expr
        if (Match(TokenType.Arrow))
        {
            var body = _expression.ParseExpression();
            return new LambdaExpr([new LambdaParameter(null, identifier)], body) { Span = SpanFrom(mark) };
        }

        return new IdentifierExpr(identifier) { Span = SpanFrom(mark) };
    }

    #endregion

    #region Parenthesized, Lambda, and Tuple

    private Expr ParseParenthesized(int mark)
    {
        // Could be: grouping (expr), lambda (x) => ..., typed lambda (int x) => ...,
        // parameter list (a, b) => ..., or tuple (expr1, expr2, ...)

        // Empty parens - parameterless lambda
        if (Match(TokenType.RightParen))
        {
            Consume(TokenType.Arrow, "Expected '=>' after '()'");
            var body = _expression.ParseExpression();
            return new LambdaExpr([], body) { Span = SpanFrom(mark) };
        }

        // Try typed lambda first: (type name, type name, ...) => body
        var typedLambdaResult = TryParseTypedLambda(mark);
        if (typedLambdaResult != null)
            return typedLambdaResult;

        // Try untyped lambda using backtracking: identifiers followed by ) =>
        var savedPosition = State.Current;
        var parameters = new List<LambdaParameter>();
        var isLambda = false;

        if (Check(TokenType.Identifier))
        {
            // Try to parse as parameter list (all identifiers separated by commas)
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

        // Not a lambda - backtrack and parse as expression (grouping or tuple)
        State.Current = savedPosition;

        // Parse the first element (could be named: "name: expr")
        var firstElement = ParseTupleElement();

        // If next is comma, this is a tuple
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

        // No comma - this is grouping: (expr) returns inner expression directly
        Consume(TokenType.RightParen, "Expected ')' after expression");
        return firstElement.Expression;
    }

    /// <summary>
    /// Parses a single tuple element, which may be named (name: expr) or unnamed (expr).
    /// </summary>
    private TupleElement ParseTupleElement()
    {
        // Check for named element: identifier followed by colon
        if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
        {
            var nameToken = Advance(); // consume identifier
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
    /// ECMA-334 §12.19 - Anonymous function expressions with explicitly typed parameters.
    /// </summary>
    private Expr? TryParseTypedLambda(int mark)
    {
        var saved = State.Current;

        try
        {
            var parameters = new List<LambdaParameter>();

            // First parameter: must be type followed by name
            var firstType = TryParseTypeName();
            if (firstType == null || !Check(TokenType.Identifier))
            {
                State.Current = saved;
                return null;
            }

            parameters.Add(new LambdaParameter(firstType, Advance()));

            // Additional parameters: comma-separated type-name pairs
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

            // Must end with ) =>
            if (!Match(TokenType.RightParen) || !Match(TokenType.Arrow))
            {
                State.Current = saved;
                return null;
            }

            // Block body: (params) => { ... } -- parse as block statement
            // Expression body: (params) => expr
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
            return new LambdaExpr(parameters, body) { Span = SpanFrom(mark) };
        }
        catch (AlderException)
        {
            State.Current = saved;
            return null;
        }
    }

    #endregion

    #region Interpolated Strings

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
                // Check for escaped brace {{
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
                                // Alignment specifier: everything between , and : or }
                                exprEnd = i;
                                i++; // skip ,
                                var alignStart = i;
                                // Scan alignment value (may include - for left-align)
                                while (i < content.Length && content[i] != ':' && content[i] != '}')
                                    i++;
                                alignmentSpec = content[alignStart..i].Trim();
                                if (i < content.Length && content[i] == ':')
                                {
                                    i++; // skip :
                                    var fmtStart = i;
                                    // Format specifier is everything until closing }
                                    while (i < content.Length && content[i] != '}')
                                        i++;
                                    formatSpec = content[fmtStart..i];
                                }
                                // Now i points at } -- let loop decrement braceDepth
                                continue;
                            }
                            case ':' when braceDepth == 1 && parenDepth == 0 && bracketDepth == 0 && alignmentSpec == null && formatSpec == null:
                            {
                                // Format specifier only (no alignment)
                                exprEnd = i;
                                i++; // skip :
                                var fmtStart = i;
                                // Format specifier is everything until closing }
                                while (i < content.Length && content[i] != '}')
                                    i++;
                                formatSpec = content[fmtStart..i];
                                // Now i points at } -- let loop decrement braceDepth
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
                // Check for escaped brace }}
                case '}' when i + 1 < content.Length && content[i + 1] == '}':
                    sb.Append('}');
                    i += 2;
                    continue;
                // Single } outside expression - just append it
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

    #endregion
}
