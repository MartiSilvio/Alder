using Alder.Diagnostics;

namespace Alder.Parsing;

/// <summary>
/// Parses expressions using precedence climbing (Roslyn-style).
/// A single <see cref="ParseSubExpression"/> method with a while-loop replaces the
/// traditional one-method-per-precedence-level chain, keeping recursion depth
/// proportional to expression nesting rather than the number of precedence levels.
/// </summary>
internal sealed partial class ExpressionParser : ParserBase
{
    private readonly PrimaryParser _primary;
    private readonly PatternParser _pattern;
    private readonly StatementParser _statement;
    private int _recursionDepth;

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
    /// Creates a fully wired parser graph for a token list. Used by AlderEngine and for
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
            throw SyntaxError(DiagnosticDescriptors.InvalidExpressionTerm, Peek().Lexeme);

        State.Current = 0;
        return ParseProgram();
    }

    private Expr ParseProgram()
    {
        var mark = Mark();
        var statements = new List<Expr>();

        while (!IsAtEnd())
        {
            if (IsStatementKeyword())
            {
                _statement.ParseStatementInto(statements);
            }
            else if (Check(TokenType.LeftBrace))
            {
                _statement.ParseStatementInto(statements);
            }
            else if (Check(TokenType.Identifier) && PeekNext().Type == TokenType.Colon)
            {
                var labelMark = Mark();
                var label = Advance();
                Advance(); // consume ':'
                statements.Add(new LabelExpr(label.Lexeme) { Span = SpanFrom(labelMark) });
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
                    return new BlockExpr(statements, expr) { Span = SpanFrom(mark) };
                }
                else
                {
                    throw SyntaxError(DiagnosticDescriptors.InvalidExpressionTerm, Peek().Lexeme);
                }
            }
        }

        if (statements.Count > 0)
            return new BlockExpr(statements, null) { Span = SpanFrom(mark) };

        throw SyntaxError(DiagnosticDescriptors.ExpressionExpected);
    }

    private bool IsStatementKeyword()
    {
        if (Check(TokenType.If))
        {
            if (IsIfExpressionStart())
                return false;
            return true;
        }

        if (Check(TokenType.Return) || Check(TokenType.Break) || Check(TokenType.Continue) ||
            Check(TokenType.Goto) ||
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

        if (State.LanguageMode == LanguageMode.Extended &&
            (Check(TokenType.Unless) || Check(TokenType.Until)))
            return true;

        if ((Check(TokenType.Checked) || Check(TokenType.Unchecked)) && PeekNext().Type == TokenType.LeftBrace)
            return true;

        // ECMA-334 §13.6.2 — Typed declarations and local functions.
        // Covers type keywords (int x), identifiers (Action f), generics (List<int> x),
        // dotted names (System.DayOfWeek d), tuples ((int, string) t).
        // Type keywords followed by dot are static member access (double.NaN) — skip.
        if ((IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot)
            || Check(TokenType.Identifier)
            || Check(TokenType.LeftParen))
        {
            var saved = State.Current;
            var parsedType = TryParseTypeName();
            if (parsedType != null && (Check(TokenType.Identifier) || IsContextualKeyword(Peek().Type)))
            {
                // Verify the token after the variable name is consistent with a declaration
                // (= for variable, ( for local function, ; for uninitialized).
                // Without this, "text like pattern" would be misidentified as "text like = ..."
                var namePos = State.Current + 1;
                if (namePos < State.Tokens.Count)
                {
                    var afterName = State.Tokens[namePos].Type;
                    if (afterName is TokenType.Equal or TokenType.LeftParen or TokenType.Semicolon)
                    {
                        State.Current = saved;
                        return true;
                    }
                }
            }
            State.Current = saved;
        }

        return false;
    }

    #endregion

    #region Precedence

    private enum Precedence : byte
    {
        Expression = 0,
        Assignment,
        Pipeline,
        Conditional,
        NullCoalescing,
        LogicalOr,
        LogicalAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseAnd,
        Equality,
        Relational,
        Shift,
        Additive,
        Multiplicative,
        Range,
        Unary,
        Power,
    }

    private static (Precedence prec, bool rightAssoc)? GetBinaryOperatorInfo(TokenType type) => type switch
    {
        TokenType.PipeGreater => (Precedence.Pipeline, false),
        TokenType.QuestionQuestion => (Precedence.NullCoalescing, true),
        TokenType.PipePipe => (Precedence.LogicalOr, false),
        TokenType.AmpAmp => (Precedence.LogicalAnd, false),
        TokenType.Pipe => (Precedence.BitwiseOr, false),
        TokenType.Caret => (Precedence.BitwiseXor, false),
        TokenType.Amp => (Precedence.BitwiseAnd, false),
        TokenType.EqualEqual or TokenType.BangEqual
            or TokenType.EqualEqualEqual or TokenType.BangEqualEqual
            => (Precedence.Equality, false),
        TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual
            => (Precedence.Relational, false),
        TokenType.In or TokenType.NotIn or TokenType.Like or TokenType.NotLike
            or TokenType.EqualTilde or TokenType.BangTilde or TokenType.LessEqualGreater
            => (Precedence.Relational, false),
        TokenType.LessLess or TokenType.GreaterGreater or TokenType.GreaterGreaterGreater
            => (Precedence.Shift, false),
        TokenType.Plus or TokenType.Minus => (Precedence.Additive, false),
        TokenType.Star or TokenType.Slash or TokenType.Percent => (Precedence.Multiplicative, false),
        TokenType.DotDot or TokenType.DotDotEquals or TokenType.DotDotLess => (Precedence.Range, false),
        TokenType.StarStar => (Precedence.Power, true),
        _ => null
    };

    private static bool RequiresExtendedMode(TokenType type) => type is
        TokenType.PipeGreater or TokenType.StarStar or
        TokenType.EqualEqualEqual or TokenType.BangEqualEqual or
        TokenType.In or TokenType.NotIn or TokenType.Like or TokenType.NotLike or
        TokenType.EqualTilde or TokenType.BangTilde or TokenType.LessEqualGreater or
        TokenType.DotDotEquals or TokenType.DotDotLess;

    #endregion

    #region Expression API

    internal Expr ParseExpression() => ParseSubExpression(Precedence.Expression);

    internal Expr ParseUnary() => ParseSubExpression(Precedence.Unary);

    internal Expr ParseBitwiseOr() => ParseSubExpression(Precedence.BitwiseOr);

    #endregion

    #region Precedence Climbing Core

    private Expr ParseSubExpression(Precedence minPrecedence)
    {
        if (++_recursionDepth > MaxUncheckedRecursionDepth)
            System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
        try
        {
            return ParseSubExpressionCore(minPrecedence);
        }
        finally
        {
            _recursionDepth--;
        }
    }

    private Expr ParseSubExpressionCore(Precedence minPrecedence)
    {
        var mark = Mark();
        var extended = State.LanguageMode == LanguageMode.Extended;


        if (minPrecedence <= Precedence.Assignment)
        {
            if (Check(TokenType.Let))
            {
                if (!extended)
                    throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired, "let-in");
                if (IsLetInExpressionStart())
                {
                    Advance();
                    return ParseLetInExpression(Previous());
                }
            }

            if (Match(TokenType.Throw))
                return new ThrowExpr(ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
        }

        if (Check(TokenType.If))
        {
            if (!extended)
                throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired, "if-expression");
            if (IsIfExpressionStart())
                return ParseIfExpression();
        }


        Expr left;

        if (minPrecedence <= Precedence.Range && Check(TokenType.DotDot))
        {
            Advance();
            Expr? end = IsRangeEndFollowing() ? ParseSubExpression(Precedence.Unary) : null;
            left = new RangeExpr(null, end, ExclusiveEnd: true) { Span = SpanFrom(mark) };
        }
        else if (Check(TokenType.LeftParen) && IsCastExpression())
        {
            Advance();
            var startToken = Peek();
            var typeName = TryParseTypeName()
                ?? throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type in cast");
            var typeToken = new Token(TokenType.Identifier, typeName, null, startToken.Line, startToken.Column);
            Consume(TokenType.RightParen, "Expected ')' after cast type");
            var operand = ParseSubExpression(Precedence.Range);
            left = new CastExpr(typeToken, operand) { Span = SpanFrom(mark) };
        }
        else if (Match(TokenType.Bang, TokenType.Minus, TokenType.Plus, TokenType.Tilde))
        {
            var op = Previous();
            left = new UnaryExpr(op, ParseSubExpression(Precedence.Unary)) { Span = SpanFrom(mark) };
        }
        else if (MatchExtendedWordOperator(TokenLexemes.GetCanonical(TokenType.Not), TokenType.Bang) is { } notOp)
        {
            left = new UnaryExpr(notOp, ParseSubExpression(Precedence.Unary)) { Span = SpanFrom(mark) };
        }
        else if (Match(TokenType.Caret))
        {
            left = new IndexFromEndExpr(ParseSubExpression(Precedence.Unary)) { Span = SpanFrom(mark) };
        }
        else if (Match(TokenType.PlusPlus, TokenType.MinusMinus))
        {
            var op = Previous();
            var isIncrement = op.Type == TokenType.PlusPlus;
            var operand = ParsePostfix(_primary.ParsePrimary(), mark);
            left = operand switch
            {
                MemberAccessExpr m => new MemberIncrementExpr(m.Object, m.Name.Lexeme, true, isIncrement) { Span = SpanFrom(mark) },
                IndexAccessExpr idx => new IndexIncrementExpr(idx.Object, idx.Index, true, isIncrement) { Span = SpanFrom(mark) },
                IdentifierExpr id => new IncrementDecrementExpr(id.Name, op, true) { Span = SpanFrom(mark) },
                _ => throw SyntaxError(DiagnosticDescriptors.InvalidExpressionTerm, op.Lexeme)
            };
        }
        else
        {
            left = ParsePostfix(_primary.ParsePrimary(), mark);
        }


        while (true)
        {
            var tokenType = Peek().Type;

            // Table-driven binary operators
            if (GetBinaryOperatorInfo(tokenType) is (var opPrec, var rightAssoc))
            {
                if (opPrec < minPrecedence || (opPrec == minPrecedence && !rightAssoc))
                    break;

                if (RequiresExtendedMode(tokenType) && !extended)
                    throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired, TokenLexemes.GetCanonical(tokenType));

                var op = Advance();

                // ?? throw — special right-operand handling (ECMA-334 §12.16)
                if (tokenType == TokenType.QuestionQuestion && Check(TokenType.Throw))
                {
                    Advance();
                    var throwOperand = ParseSubExpression(Precedence.Assignment);
                    left = new NullCoalesceExpr(left, new ThrowExpr(throwOperand) { Span = SpanFrom(mark) }) { Span = SpanFrom(mark) };
                    continue;
                }

                // not in / not like — desugar to !(left op right)
                if (tokenType is TokenType.NotIn or TokenType.NotLike)
                {
                    var right = ParseSubExpression(opPrec);
                    var innerOpType = tokenType == TokenType.NotIn ? TokenType.In : TokenType.Like;
                    left = new UnaryExpr(
                        TokenLexemes.CreateSynthetic(TokenType.Bang, op),
                        new BinaryExpr(left, TokenLexemes.CreateSynthetic(innerOpType, op), right)
                            { Span = SpanFrom(mark) }) { Span = SpanFrom(mark) };
                    continue;
                }

                // Range operators — parse end, then break (no chaining)
                if (opPrec == Precedence.Range)
                {
                    if (tokenType == TokenType.DotDot)
                    {
                        Expr? end = IsRangeEndFollowing() ? ParseSubExpression(Precedence.Unary) : null;
                        left = new RangeExpr(left, end, ExclusiveEnd: true) { Span = SpanFrom(mark) };
                    }
                    else
                    {
                        var end = ParseSubExpression(Precedence.Unary);
                        left = new RangeExpr(left, end, ExclusiveEnd: tokenType != TokenType.DotDotEquals) { Span = SpanFrom(mark) };
                    }
                    break;
                }

                // Chained comparisons in Extended mode: a < b < c → ChainedComparisonExpr
                if ((opPrec == Precedence.Relational || opPrec == Precedence.Equality) && extended)
                {
                    var right = ParseSubExpression(opPrec);
                    if (IsChainableComparisonOperator(op.Type) && IsChainableComparisonOperator(Peek().Type))
                    {
                        var operands = new List<Expr> { left, right };
                        var operators = new List<Token> { op };
                        while (IsChainableComparisonOperator(Peek().Type))
                        {
                            operators.Add(Advance());
                            operands.Add(ParseSubExpression(opPrec));
                        }
                        left = new ChainedComparisonExpr(operands, operators) { Span = SpanFrom(mark) };
                        continue;
                    }
                    left = MakeInfixNode(left, op, right, mark);
                    continue;
                }

                var rightOperand = ParseSubExpression(opPrec);
                left = MakeInfixNode(left, op, rightOperand, mark);
                continue;
            }

            // Ternary conditional: ? then : else
            if (tokenType == TokenType.Question
                && (Precedence.Conditional > minPrecedence
                    || (Precedence.Conditional == minPrecedence))) // right-associative
            {
                Advance();
                var thenBranch = ParseSubExpression(Precedence.Expression);
                Consume(TokenType.Colon, "Expected ':' in ternary expression");
                var elseBranch = ParseSubExpression(Precedence.Expression);
                left = new ConditionalExpr(left, thenBranch, elseBranch) { Span = SpanFrom(mark) };
                continue;
            }

            // Assignment operators: =, +=, -=, *=, /=, %=, &=, |=, ^=, <<=, >>=, >>>=, ??=
            if (Precedence.Assignment > minPrecedence
                || (Precedence.Assignment == minPrecedence)) // right-associative
            {
                if (TryParseAssignment(ref left, mark))
                    continue;
            }

            // Type operators at Relational precedence: is, as, switch
            if (Precedence.Relational > minPrecedence
                || (Precedence.Relational == minPrecedence))
            {
                if (Match(TokenType.Is))
                {
                    left = ParseIsExpression(left, mark);
                    continue;
                }
                if (Match(TokenType.As))
                {
                    left = ParseAsExpression(left, mark);
                    continue;
                }
                if (Match(TokenType.Switch))
                {
                    left = ParseSwitchExpression(left, mark);
                    continue;
                }

                // between...and (Extended mode)
                if (Check(TokenType.Between))
                {
                    if (!extended)
                        throw new AlderException(DiagnosticDescriptors.ExtendedModeRequired, TokenLexemes.GetCanonical(TokenType.Between));
                    Advance();
                    var betweenToken = Previous();
                    var low = ParseSubExpression(Precedence.Relational);
                    if (!Match(TokenType.AmpAmp) && MatchExtendedWordOperator(TokenLexemes.GetCanonical(TokenType.And), TokenType.AmpAmp) == null)
                        throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'and' after 'between' lower bound");
                    var high = ParseSubExpression(Precedence.Relational);
                    var geExpr = new BinaryExpr(left, TokenLexemes.CreateSynthetic(TokenType.GreaterEqual, betweenToken), low) { Span = SpanFrom(mark) };
                    var leExpr = new BinaryExpr(left, TokenLexemes.CreateSynthetic(TokenType.LessEqual, betweenToken), high) { Span = SpanFrom(mark) };
                    left = new LogicalExpr(geExpr, TokenLexemes.CreateSynthetic(TokenType.AmpAmp, betweenToken), leExpr) { Span = SpanFrom(mark) };
                    continue;
                }
            }

            // Extended word operators: or, and
            if (extended && !IsAtEnd() && Peek().Type == TokenType.Identifier)
            {
                var lexeme = Peek().Lexeme;

                if (string.Equals(lexeme, TokenLexemes.GetCanonical(TokenType.Or), StringComparison.Ordinal)
                    && Precedence.LogicalOr > minPrecedence)
                {
                    var op = MatchExtendedWordOperator(TokenLexemes.GetCanonical(TokenType.Or), TokenType.PipePipe)!.Value;
                    left = new LogicalExpr(left, op, ParseSubExpression(Precedence.LogicalOr)) { Span = SpanFrom(mark) };
                    continue;
                }

                if (string.Equals(lexeme, TokenLexemes.GetCanonical(TokenType.And), StringComparison.Ordinal)
                    && Precedence.LogicalAnd > minPrecedence)
                {
                    var op = MatchExtendedWordOperator(TokenLexemes.GetCanonical(TokenType.And), TokenType.AmpAmp)!.Value;
                    left = new LogicalExpr(left, op, ParseSubExpression(Precedence.LogicalAnd)) { Span = SpanFrom(mark) };
                    continue;
                }
            }

            break;
        }

        return left;
    }

    private Expr MakeInfixNode(Expr left, Token op, Expr right, int mark) => op.Type switch
    {
        TokenType.PipePipe or TokenType.AmpAmp => new LogicalExpr(left, op, right) { Span = SpanFrom(mark) },
        TokenType.QuestionQuestion => new NullCoalesceExpr(left, right) { Span = SpanFrom(mark) },
        TokenType.PipeGreater => new PipelineExpr(left, right) { Span = SpanFrom(mark) },
        _ => new BinaryExpr(left, op, right) { Span = SpanFrom(mark) }
    };

    private bool TryParseAssignment(ref Expr left, int mark)
    {
        if (left is IdentifierExpr identifier)
        {
            if (Match(TokenType.QuestionQuestionEqual))
            {
                left = new NullCoalesceAssignExpr(identifier.Name, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
            if (Match(TokenType.Equal))
            {
                left = new AssignExpr(identifier.Name, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
            if (MatchCompoundAssignment(out var op))
            {
                left = new CompoundAssignExpr(identifier.Name, op, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
        }
        else if (left is MemberAccessExpr memberAccess)
        {
            if (Match(TokenType.QuestionQuestionEqual))
            {
                left = new MemberNullCoalesceAssignExpr(memberAccess.Object, memberAccess.Name.Lexeme, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
            if (Match(TokenType.Equal))
            {
                left = new MemberAssignExpr(memberAccess.Object, memberAccess.Name, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
            if (MatchCompoundAssignment(out var memberOp))
            {
                left = new MemberCompoundAssignExpr(memberAccess.Object, memberAccess.Name.Lexeme, memberOp.Type,
                    ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
        }
        else if (left is IndexAccessExpr indexAccess)
        {
            if (Match(TokenType.QuestionQuestionEqual))
            {
                left = new IndexNullCoalesceAssignExpr(indexAccess.Object, indexAccess.Index, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
            if (Match(TokenType.Equal))
            {
                left = new IndexAssignExpr(indexAccess.Object, indexAccess.Index, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
            if (MatchCompoundAssignment(out var indexOp))
            {
                left = new IndexCompoundAssignExpr(indexAccess.Object, indexAccess.Index, indexOp.Type, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
        }
        else if (left is MultiDimIndexAccessExpr multiIndex)
        {
            if (Match(TokenType.Equal))
            {
                left = new MultiDimIndexAssignExpr(multiIndex.Object, multiIndex.Indices, ParseSubExpression(Precedence.Assignment)) { Span = SpanFrom(mark) };
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Postfix

    private Expr ParsePostfix(Expr expr, int mark)
    {
        while (true)
        {
            // C# null-forgiving operator: expr! — no-op at runtime, just consume
            if (Check(TokenType.Bang) && State.Current + 1 < State.Tokens.Count &&
                State.Tokens[State.Current + 1].Type is TokenType.Dot or TokenType.LeftBracket or TokenType.QuestionDot)
            {
                Advance(); // consume '!'
                continue;
            }

            if (Match(TokenType.Dot))
            {
                var name = ConsumeIdentifierOrContextualKeyword("Expected property name after '.'");
                expr = new MemberAccessExpr(expr, name, false) { Span = SpanFrom(mark) };
            }
            else if (Match(TokenType.QuestionDot))
            {
                var name = ConsumeIdentifierOrContextualKeyword("Expected property name after '?.'");
                expr = new MemberAccessExpr(expr, name, true) { Span = SpanFrom(mark) };
            }
            else if (Match(TokenType.LeftBracket))
            {
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
                    expr = new SliceExpr(expr, null, end, step) { Span = SpanFrom(mark) };
                }
                else
                {
                    var firstIndex = ParseExpression();

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
                        expr = new SliceExpr(expr, firstIndex, end, step) { Span = SpanFrom(mark) };
                    }
                    else if (Check(TokenType.Comma))
                    {
                        var indices = new List<Expr> { firstIndex };
                        while (Match(TokenType.Comma))
                            indices.Add(ParseExpression());
                        Consume(TokenType.RightBracket, "Expected ']' after indices");
                        expr = new MultiDimIndexAccessExpr(expr, indices, false) { Span = SpanFrom(mark) };
                    }
                    else
                    {
                        Consume(TokenType.RightBracket, "Expected ']' after index");
                        expr = new IndexAccessExpr(expr, firstIndex, false) { Span = SpanFrom(mark) };
                    }
                }
            }
            else if (Match(TokenType.QuestionLeftBracket))
            {
                var firstIndex = ParseExpression();
                if (Check(TokenType.Comma))
                {
                    var indices = new List<Expr> { firstIndex };
                    while (Match(TokenType.Comma))
                        indices.Add(ParseExpression());
                    Consume(TokenType.RightBracket, "Expected ']' after null-conditional indices");
                    expr = new MultiDimIndexAccessExpr(expr, indices, true) { Span = SpanFrom(mark) };
                }
                else
                {
                    Consume(TokenType.RightBracket, "Expected ']' after null-conditional index");
                    expr = new IndexAccessExpr(expr, firstIndex, true) { Span = SpanFrom(mark) };
                }
            }
            else if (Check(TokenType.Less) && TryParseTypeArguments(out var typeArgs))
            {
                if (Check(TokenType.Dot) && expr is IdentifierExpr typeId)
                {
                    // Generic type static member access: Comparer<int>.Default, Nullable<int>.Value
                    var fullTypeName = typeId.Name.Lexeme + "<" + string.Join(", ", typeArgs) + ">";
                    var syntheticToken = new Token(TokenType.Identifier, fullTypeName, null, typeId.Name.Line, typeId.Name.Column);
                    expr = new TypeReferenceExpr(syntheticToken) { Span = SpanFrom(mark) };
                }
                else
                {
                    Consume(TokenType.LeftParen, "Expected '(' after generic type arguments");
                    expr = FinishCall(expr, typeArgs, mark);
                }
            }
            else if (Match(TokenType.LeftParen))
            {
                expr = FinishCall(expr, null, mark);
            }
            else if (Check(TokenType.With) && PeekNext().Type == TokenType.LeftBrace)
            {
                Advance(); // consume 'with'
                expr = ParseWithInitializer(expr, mark);
            }
            else if (Check(TokenType.PlusPlus) || Check(TokenType.MinusMinus))
            {
                if (expr is IdentifierExpr identifier)
                {
                    Advance();
                    var op = Previous();
                    expr = new IncrementDecrementExpr(identifier.Name, op, false) { Span = SpanFrom(mark) };
                }
                else if (expr is MemberAccessExpr memberExpr)
                {
                    Advance();
                    var isIncrement = Previous().Type == TokenType.PlusPlus;
                    expr = new MemberIncrementExpr(memberExpr.Object, memberExpr.Name.Lexeme, false, isIncrement) { Span = SpanFrom(mark) };
                }
                else if (expr is IndexAccessExpr indexExpr)
                {
                    Advance();
                    var isIncrement = Previous().Type == TokenType.PlusPlus;
                    expr = new IndexIncrementExpr(indexExpr.Object, indexExpr.Index, false, isIncrement) { Span = SpanFrom(mark) };
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

    private WithExpr ParseWithInitializer(Expr obj, int mark)
    {
        Consume(TokenType.LeftBrace, "Expected '{' after 'with'");
        var initializers = new List<(Token Key, Expr Value)>();
        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var key = ConsumeIdentifierOrContextualKeyword("Expected property name");
            Consume(TokenType.Equal, "Expected '=' after property name in 'with' initializer");
            var value = ParseExpression();
            initializers.Add((key, value));
            if (!Check(TokenType.RightBrace))
                Consume(TokenType.Comma, "Expected ',' or '}' in 'with' initializer");
        }
        Consume(TokenType.RightBrace, "Expected '}' after 'with' initializer");
        return new WithExpr(obj, initializers) { Span = SpanFrom(mark) };
    }

    #endregion

    #region Helpers

    private Token? MatchExtendedWordOperator(string keyword, TokenType operatorType)
    {
        if (State.LanguageMode != LanguageMode.Extended)
            return null;
        if (IsAtEnd() || Peek().Type != TokenType.Identifier)
            return null;
        if (!string.Equals(Peek().Lexeme, keyword, StringComparison.Ordinal))
            return null;
        var token = Peek();
        Advance();
        return new Token(operatorType, token.Lexeme, token.Literal, token.Line, token.Column, token.Start);
    }

    private static bool IsChainableComparisonOperator(TokenType type) =>
        type is TokenType.Less or TokenType.LessEqual or TokenType.Greater or TokenType.GreaterEqual
            or TokenType.EqualEqual or TokenType.BangEqual;

    private bool IsRangeEndFollowing()
    {
        if (IsAtEnd()) return false;
        var t = Peek().Type;
        return t != TokenType.RightBracket && t != TokenType.RightParen
            && t != TokenType.Comma && t != TokenType.Semicolon
            && t != TokenType.Colon && t != TokenType.Eof;
    }

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
            or TokenType.Sizeof or TokenType.Checked or TokenType.Unchecked
            or TokenType.Throw or TokenType.DotDot or TokenType.Caret;
    }

    #endregion

    #region Let-In Expression

    private Expr ParseLetInExpression(Token letToken)
    {
        var mark = Mark();
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

            var tempName = $"<let>__{letToken.Line}_{letToken.Column}_{State.Current}";
            var tempToken = new Token(TokenType.Identifier, tempName, null, letToken.Line, letToken.Column);
            statements.Add(new VariableDeclExpr(null, tempToken, initializer) { Span = SpanFrom(mark) });

            foreach (var name in names)
            {
                var memberToken = new Token(TokenType.Identifier, name.Lexeme, null, name.Line, name.Column);
                var memberAccess = new MemberAccessExpr(new IdentifierExpr(tempToken) { Span = SpanFrom(mark) }, memberToken, false) { Span = SpanFrom(mark) };
                statements.Add(new VariableDeclExpr(null, name, memberAccess) { Span = SpanFrom(mark) });
            }
        }
        else
        {
            var name = ConsumeIdentifierOrContextualKeyword("Expected variable name after 'let'");
            Consume(TokenType.Equal, "Expected '=' after variable name");
            var initializer = ParseLetInInitializer();
            statements.Add(new VariableDeclExpr(null, name, initializer) { Span = SpanFrom(mark) });
        }

        var body = ParseExpression();
        return new BlockExpr(statements, body) { Span = SpanFrom(mark) };
    }

    private Expr ParseLetInInitializer()
    {
        if (!TryFindTopLevelDelimiter(TokenType.In, State.Current, out var delimiterIndex))
            throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "'in' in let-in expression");

        var initializer = ParseExpressionSlice(State.Current, delimiterIndex);
        State.Current = delimiterIndex + 1;
        return initializer;
    }

    private Expr ParseExpressionSlice(int startInclusive, int endExclusive)
    {
        if (endExclusive <= startInclusive)
            throw SyntaxError(DiagnosticDescriptors.ExpressionExpected);

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

                if (tokenType is TokenType.Semicolon or TokenType.Eof)
                    break;
            }
        }

        delimiterIndex = -1;
        return false;
    }

    #endregion

    #region If Expression

    private Expr ParseIfExpression()
    {
        var mark = Mark();
        Consume(TokenType.If, "Expected 'if' at start of if-expression");
        Consume(TokenType.LeftParen, "Expected '(' after 'if'");
        var condition = ParseExpression();
        Consume(TokenType.RightParen, "Expected ')' after if condition");

        if (Check(TokenType.LeftBrace))
            throw SyntaxError(DiagnosticDescriptors.ExpressionExpected);

        var thenBranch = ParseExpression();
        Consume(TokenType.Else, "Expected 'else' in if-expression");
        var elseBranch = ParseExpression();
        return new ConditionalExpr(condition, thenBranch, elseBranch) { Span = SpanFrom(mark) };
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

                if (tokenType is TokenType.Semicolon or TokenType.Eof)
                    return false;
            }
        }

        return false;
    }

    #endregion

    #region Type Operators and Switch Expression

    private IsPatternExpr ParseIsExpression(Expr left, int mark)
    {
        var pattern = _pattern.ParsePattern();
        return new IsPatternExpr(left, pattern) { Span = SpanFrom(mark) };
    }

    private Expr ParseAsExpression(Expr left, int mark)
    {
        var typeName = TryParseTypeName()
            ?? throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "type after 'as'");
        var typeToken = new Token(TokenType.Identifier, typeName, null, Previous().Line, Previous().Column);
        return new AsExpr(left, typeToken) { Span = SpanFrom(mark) };
    }

    private Token ParseDottedTypeName()
    {
        var token = Consume(TokenType.Identifier, "Expected type name");
        var name = token.Lexeme;

        while (Check(TokenType.Dot) && State.Current + 1 < State.Tokens.Count
                                    && State.Tokens[State.Current + 1].Type == TokenType.Identifier)
        {
            Advance();
            var next = Advance();
            name += "." + next.Lexeme;
        }

        if (Check(TokenType.Question))
        {
            Advance();
            name += "?";
        }

        return token with { Lexeme = name };
    }

    private Expr ParseSwitchExpression(Expr subject, int mark)
    {
        Consume(TokenType.LeftBrace, "Expected '{' after 'switch'");
        var arms = new List<SwitchArm>();

        while (!Check(TokenType.RightBrace) && !IsAtEnd())
        {
            var pattern = _pattern.ParsePattern();

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
        return new SwitchExpressionExpr(subject, arms) { Span = SpanFrom(mark) };
    }

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

    #endregion

    #region Cast Disambiguation

    internal bool IsCastExpression()
    {
        if (State.Current + 1 >= State.Tokens.Count)
            return false;

        var nextToken = State.Tokens[State.Current + 1];

        if (IsTypeKeyword(nextToken.Type))
        {
            var scanIndex = State.Current + 2;
            if (scanIndex >= State.Tokens.Count)
                return false;

            scanIndex = SkipArrayRankSpecifiers(scanIndex);

            var afterType = State.Tokens[scanIndex];
            if (afterType.Type == TokenType.RightParen)
                return true;

            if (afterType.Type == TokenType.Question)
            {
                var afterNullable = scanIndex + 1;
                if (afterNullable >= State.Tokens.Count)
                    return false;
                afterNullable = SkipArrayRankSpecifiers(afterNullable);
                return State.Tokens[afterNullable].Type == TokenType.RightParen;
            }

            if (afterType.Type == TokenType.QuestionLeftBracket)
            {
                var afterQBracket = scanIndex + 1;
                while (afterQBracket < State.Tokens.Count && State.Tokens[afterQBracket].Type == TokenType.Comma)
                    afterQBracket++;
                if (afterQBracket >= State.Tokens.Count || State.Tokens[afterQBracket].Type != TokenType.RightBracket)
                    return false;
                afterQBracket++;
                afterQBracket = SkipArrayRankSpecifiers(afterQBracket);
                return afterQBracket < State.Tokens.Count && State.Tokens[afterQBracket].Type == TokenType.RightParen;
            }

            return false;
        }

        if (nextToken.Type == TokenType.Identifier)
        {
            var scanIndex = State.Current + 2;

            while (scanIndex + 1 < State.Tokens.Count
                   && State.Tokens[scanIndex].Type == TokenType.Dot
                   && State.Tokens[scanIndex + 1].Type == TokenType.Identifier)
            {
                scanIndex += 2;
            }

            if (scanIndex >= State.Tokens.Count)
                return false;

            if (State.Tokens[scanIndex].Type == TokenType.Less)
            {
                scanIndex = SkipBalancedAngleBrackets(scanIndex);
                if (scanIndex < 0)
                    return false;
            }

            scanIndex = SkipArrayRankSpecifiers(scanIndex);

            if (scanIndex < State.Tokens.Count
                && State.Tokens[scanIndex].Type == TokenType.Question
                && scanIndex + 1 < State.Tokens.Count)
            {
                scanIndex++;
            }

            if (scanIndex >= State.Tokens.Count)
                return false;

            if (State.Tokens[scanIndex].Type != TokenType.RightParen)
                return false;

            var afterParen = scanIndex + 1;
            if (afterParen >= State.Tokens.Count)
                return false;

            return IsValueStartToken(State.Tokens[afterParen].Type);
        }

        return false;
    }

    private int SkipBalancedAngleBrackets(int index)
    {
        if (index >= State.Tokens.Count || State.Tokens[index].Type != TokenType.Less)
            return -1;

        var depth = 1;
        index++;
        while (index < State.Tokens.Count && depth > 0)
        {
            var tt = State.Tokens[index].Type;
            if (tt == TokenType.Less) depth++;
            else if (tt == TokenType.Greater) depth--;
            else if (tt == TokenType.RightParen) return -1;
            index++;
        }

        return depth == 0 ? index : -1;
    }

    private int SkipArrayRankSpecifiers(int index)
    {
        while (index < State.Tokens.Count && State.Tokens[index].Type == TokenType.LeftBracket)
        {
            var bracketStart = index;
            index++;
            while (index < State.Tokens.Count && State.Tokens[index].Type == TokenType.Comma)
                index++;
            if (index >= State.Tokens.Count || State.Tokens[index].Type != TokenType.RightBracket)
                return bracketStart;
            index++;
        }
        return index;
    }

    #endregion
}
