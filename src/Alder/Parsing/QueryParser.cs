using Alder.Diagnostics;

namespace Alder.Parsing;

/// <summary>
/// Parses query expressions and lowers them immediately to the existing method-call AST.
/// ECMA-334 §12.20 defines the source syntax. Alder keeps the rest of the pipeline free of query-specific nodes
/// by translating queries to the same <c>Where</c>, <c>Select</c>, <c>SelectMany</c>, <c>Join</c>, and <c>GroupBy</c>
/// shapes that explicit method-call code already uses.
/// </summary>
internal sealed class QueryParser : ParserBase
{
    private ExpressionParser _expression = null!;
    private int _transparentIdCounter;

    internal QueryParser(ParserState state) : base(state)
    {
    }

    internal void SetExpressionParser(ExpressionParser expression) => _expression = expression;

    /// <summary>
    /// Determines whether the current token sequence starts a query expression.
    /// The lookahead exists to distinguish query syntax from ordinary identifier usage.
    /// </summary>
    internal bool IsQueryExpressionStart()
    {
        if (!Check(TokenType.From))
            return false;

        var saved = State.Current;
        try
        {
            Advance();

            if (IsTypeKeyword(Peek().Type))
            {
                Advance();
                if (IsIdentifierOrContextualKeyword(Peek().Type))
                {
                    Advance();
                    return Check(TokenType.In);
                }
                return false;
            }

            if (IsIdentifierOrContextualKeyword(Peek().Type))
            {
                Advance();
                return Check(TokenType.In);
            }

            return false;
        }
        finally
        {
            State.Current = saved;
        }
    }

    /// <summary>
    /// Parses a complete query expression rooted at <c>from</c>.
    /// </summary>
    internal Expr ParseQueryExpression()
    {
        var mark = Mark();

        Consume(TokenType.From, "Expected 'from' at start of query expression");

        // Query range-variable annotations influence parse validity, but they do not survive lowering.
        if (IsTypeKeyword(Peek().Type) && !CheckInAfterNext())
        {
            Advance();
        }

        var rangeVarToken = ConsumeIdentifierOrContextualKeyword("Expected range variable name after 'from'");
        var rangeVarName = rangeVarToken.Lexeme;

        Consume(TokenType.In, $"Expected 'in' after range variable '{rangeVarName}' in from clause");

        var source = ParseQuerySourceExpression();

        var scope = new QueryScope(rangeVarName);

        return ParseQueryBody(source, scope) with { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses query body clauses until a terminal <c>select</c> or <c>group ... by</c> clause is reached.
    /// </summary>
    private Expr ParseQueryBody(Expr source, QueryScope scope)
    {
        while (true)
        {
            if (Check(TokenType.Where))
            {
                source = ParseWhereClause(source, scope);
            }
            else if (Check(TokenType.From))
            {
                // A secondary from clause lowers to SelectMany. The helper either finishes the direct-select case
                // or returns a lowered intermediate query so clause parsing can continue.
                return ParseSecondFromClause(source, scope);
            }
            else if (Check(TokenType.Let))
            {
                source = ParseLetClause(source, scope);
            }
            else if (Check(TokenType.Orderby))
            {
                source = ParseOrderByClause(source, scope);
            }
            else if (Check(TokenType.Join))
            {
                source = ParseJoinClause(source, scope);
            }
            else if (Check(TokenType.Select))
            {
                return ParseTerminalWithContinuation(ParseSelectClause(source, scope), scope);
            }
            else if (Check(TokenType.Group))
            {
                return ParseTerminalWithContinuation(ParseGroupByClause(source, scope), scope);
            }
            else
            {
                throw SyntaxError(DiagnosticDescriptors.QueryBodyMustEndWithSelectOrGroup);
            }
        }
    }

    /// <summary>
    /// Lowers a <c>where</c> clause to a <c>Where</c> call.
    /// </summary>
    private Expr ParseWhereClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance();

        var predicate = ParseQueryBodyExpression();

        predicate = RewriteIdentifiers(predicate, scope);

        var lambdaParam = scope.CurrentParameterName;
        var lambda = MakeLambda(lambdaParam, predicate);

        return MakeMethodCall(source, "Where", lambda) with { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Lowers a secondary <c>from</c> clause to <c>SelectMany</c>.
    /// </summary>
    private Expr ParseSecondFromClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance();

        if (IsTypeKeyword(Peek().Type) && !CheckInAfterNext())
        {
            Advance();
        }

        var rangeVar2Token = ConsumeIdentifierOrContextualKeyword("Expected range variable name after 'from'");
        var rangeVar2Name = rangeVar2Token.Lexeme;

        Consume(TokenType.In, $"Expected 'in' after range variable '{rangeVar2Name}' in from clause");

        var source2Expr = ParseQuerySourceExpression();

        source2Expr = RewriteIdentifiers(source2Expr, scope);

        var outerParam = scope.CurrentParameterName;

        // The direct-select case can bind the final projection immediately and skip a transparent identifier.
        if (Check(TokenType.Select))
        {
            Advance();
            var projection = ParseQueryBodyExpression();

            var tempScope = scope.Clone();
            tempScope.AddDirectVariable(rangeVar2Name, rangeVar2Name);

            projection = RewriteIdentifiers(projection, tempScope);

            var collectionLambda = MakeLambda(outerParam, source2Expr);
            var resultLambda = MakeLambda2(outerParam, rangeVar2Name, projection);

            return MakeMethodCall(source, "SelectMany", collectionLambda, resultLambda) with { Span = SpanFrom(mark) };
        }

        // Subsequent clauses need a transparent identifier so both ranges remain visible.
        var transparentId = GenerateTransparentId();

        var collectionSelector = MakeLambda(outerParam, source2Expr);

        var resultSelectorBody = MakeTransparentObject(scope, rangeVar2Name, outerParam, rangeVar2Name);
        var resultSelector = MakeLambda2(outerParam, rangeVar2Name, resultSelectorBody);

        var result = MakeMethodCall(source, "SelectMany", collectionSelector, resultSelector);

        scope.AbsorbIntoTransparentIdentifier(transparentId, rangeVar2Name);

        return ParseQueryBody(result, scope);
    }

    /// <summary>
    /// Lowers a <c>let</c> clause to <c>Select</c> with a transparent-identifier payload.
    /// </summary>
    private Expr ParseLetClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance();

        var varNameToken = ConsumeIdentifierOrContextualKeyword("Expected variable name after 'let'");
        var varName = varNameToken.Lexeme;

        Consume(TokenType.Equal, $"Expected '=' after let variable '{varName}'");

        var expr = ParseQueryBodyExpression();

        expr = RewriteIdentifiers(expr, scope);

        var currentParam = scope.CurrentParameterName;

        var anonymousObj = MakeAnonymousObject(
            (currentParam, new IdentifierExpr(SyntheticToken(currentParam))),
            (varName, expr));

        var lambda = MakeLambda(currentParam, anonymousObj);

        var result = MakeMethodCall(source, "Select", lambda) with { Span = SpanFrom(mark) };

        var transparentId = GenerateTransparentId();
        scope.AbsorbIntoTransparentIdentifier(transparentId, varName);

        return result;
    }

    /// <summary>
    /// Lowers an <c>orderby</c> clause to <c>OrderBy</c> and chained <c>ThenBy</c> calls.
    /// </summary>
    private Expr ParseOrderByClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance();

        var lambdaParam = scope.CurrentParameterName;
        var isFirst = true;

        while (true)
        {
            var keyExpr = ParseQueryBodyExpression();
            keyExpr = RewriteIdentifiers(keyExpr, scope);

            var descending = false;
            if (Check(TokenType.Ascending))
            {
                Advance();
            }
            else if (Check(TokenType.Descending))
            {
                Advance();
                descending = true;
            }

            var keyLambda = MakeLambda(lambdaParam, keyExpr);

            string methodName;
            if (isFirst)
            {
                methodName = descending ? "OrderByDescending" : "OrderBy";
                isFirst = false;
            }
            else
            {
                methodName = descending ? "ThenByDescending" : "ThenBy";
            }

            source = MakeMethodCall(source, methodName, keyLambda);

            if (Check(TokenType.Comma))
            {
                Advance();
                continue;
            }

            break;
        }

        return source with { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses: join innerVar in innerSource on outerKey equals innerKey [into groupVar]
    /// Inner join desugars to: source.Join(innerSource, outerParam => outerKey, innerVar => innerKey, (outerParam, innerVar) => new { ... })
    /// Group join desugars to: source.GroupJoin(innerSource, outerParam => outerKey, innerVar => innerKey, (outerParam, groupVar) => new { ... })
    /// ECMA-334 §12.20.3.7 and §12.20.3.8
    /// </summary>
    private Expr ParseJoinClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance(); // consume 'join'

        var innerVarToken = ConsumeIdentifierOrContextualKeyword("Expected range variable name after 'join'");
        var innerVarName = innerVarToken.Lexeme;

        Consume(TokenType.In, "Expected 'in' after join range variable");
        var innerSource = ParseQuerySourceExpression();

        Consume(TokenType.On, "Expected 'on' in join clause");
        var outerKey = ParseQueryBodyExpression();
        outerKey = RewriteIdentifiers(outerKey, scope);

        if (!Match(TokenType.Equals))
            throw SyntaxError(DiagnosticDescriptors.ExpectedContextualKeyword, "equals");

        // Inner key references only the inner range variable, not the transparent identifier scope.
        var innerKey = ParseQueryBodyExpression();
        var innerScope = new QueryScope(innerVarName);
        innerKey = RewriteIdentifiers(innerKey, innerScope);

        var outerParam = scope.CurrentParameterName;
        var outerKeyLambda = MakeLambda(outerParam, outerKey);
        var innerKeyLambda = MakeLambda(innerVarName, innerKey);

        // Check for 'into' (group join)
        if (Check(TokenType.Into))
        {
            Advance(); // consume 'into'
            var groupVarToken = ConsumeIdentifierOrContextualKeyword("Expected group variable name after 'into'");
            var groupVarName = groupVarToken.Lexeme;

            var resultBody = MakeTransparentObject(scope, groupVarName, outerParam, groupVarName);
            var resultLambda = MakeLambda2(outerParam, groupVarName, resultBody);

            var result = MakeMethodCall(source, "GroupJoin", innerSource, outerKeyLambda, innerKeyLambda, resultLambda) with { Span = SpanFrom(mark) };

            var transparentId = GenerateTransparentId();
            scope.AbsorbIntoTransparentIdentifier(transparentId, groupVarName);

            return result;
        }

        // Inner join
        var joinResultBody = MakeTransparentObject(scope, innerVarName, outerParam, innerVarName);
        var joinResultLambda = MakeLambda2(outerParam, innerVarName, joinResultBody);

        var joinResult = MakeMethodCall(source, "Join", innerSource, outerKeyLambda, innerKeyLambda, joinResultLambda) with { Span = SpanFrom(mark) };

        var joinTransparentId = GenerateTransparentId();
        scope.AbsorbIntoTransparentIdentifier(joinTransparentId, innerVarName);

        return joinResult;
    }

    /// <summary>
    /// Parses: select projection
    /// Desugars to: source.Select(rangeVar => projection)
    /// </summary>
    private Expr ParseSelectClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance(); // consume 'select'

        var projection = ParseQueryBodyExpression();

        projection = RewriteIdentifiers(projection, scope);

        var lambdaParam = scope.CurrentParameterName;
        var lambda = MakeLambda(lambdaParam, projection);

        return MakeMethodCall(source, "Select", lambda) with { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Parses: group elementExpr by keyExpr
    /// Desugars to: source.GroupBy(param => keyExpr) for identity projection,
    /// or source.GroupBy(param => keyExpr, param => elementExpr) for custom projection.
    /// ECMA-334 §12.20.3.9
    /// </summary>
    private Expr ParseGroupByClause(Expr source, QueryScope scope)
    {
        var mark = Mark();
        Advance(); // consume 'group'

        var elementExpr = ParseQueryBodyExpression();
        elementExpr = RewriteIdentifiers(elementExpr, scope);

        Consume(TokenType.By, "Expected 'by' after group expression");

        var keyExpr = ParseQueryBodyExpression();
        keyExpr = RewriteIdentifiers(keyExpr, scope);

        var lambdaParam = scope.CurrentParameterName;
        var keyLambda = MakeLambda(lambdaParam, keyExpr);

        if (IsIdentityProjection(elementExpr, scope))
        {
            return MakeMethodCall(source, "GroupBy", keyLambda) with { Span = SpanFrom(mark) };
        }

        var elementLambda = MakeLambda(lambdaParam, elementExpr);
        return MakeMethodCall(source, "GroupBy", keyLambda, elementLambda) with { Span = SpanFrom(mark) };
    }

    /// <summary>
    /// Checks for 'into' continuation after a terminal clause (select or group...by).
    /// ECMA-334 §12.20.3.2: into z ... becomes a new query over the prior result.
    /// </summary>
    private Expr ParseTerminalWithContinuation(Expr terminalResult, QueryScope scope)
    {
        if (!Check(TokenType.Into))
            return terminalResult;

        Advance(); // consume 'into'

        var continuationVarToken = ConsumeIdentifierOrContextualKeyword("Expected variable name after 'into'");
        var continuationVarName = continuationVarToken.Lexeme;

        var newScope = new QueryScope(continuationVarName);

        return ParseQueryBody(terminalResult, newScope);
    }

    private static bool IsIdentityProjection(Expr expr, QueryScope scope)
    {
        return expr is IdentifierExpr id && id.Name.Lexeme == scope.CurrentParameterName;
    }

    private Expr ParseQueryBodyExpression()
    {
        return _expression.ParseExpression();
    }

    private Expr ParseQuerySourceExpression()
    {
        return _expression.ParseExpression();
    }

    private static LambdaExpr MakeLambda(string paramName, Expr body)
    {
        var paramToken = SyntheticToken(paramName);
        return new LambdaExpr(
            [new LambdaParameter(null, paramToken)],
            body);
    }

    private static LambdaExpr MakeLambda2(string param1, string param2, Expr body)
    {
        return new LambdaExpr(
            [
                new LambdaParameter(null, SyntheticToken(param1)),
                new LambdaParameter(null, SyntheticToken(param2))
            ],
            body);
    }

    private static CallExpr MakeMethodCall(Expr source, string methodName, params Expr[] args)
    {
        var memberAccess = new MemberAccessExpr(source, SyntheticToken(methodName), false);
        return new CallExpr(memberAccess, [..args]);
    }

    /// <summary>
    /// Creates an anonymous object expression: new { prop1 = val1, prop2 = val2, ... }
    /// Uses ObjectLiteralExpr which produces ExpandoObject at runtime.
    /// </summary>
    private static ObjectLiteralExpr MakeAnonymousObject(params (string name, Expr value)[] properties)
    {
        var props = new List<(Token Key, Expr Value)>();
        foreach (var (name, value) in properties)
        {
            props.Add((SyntheticToken(name), value));
        }
        return new ObjectLiteralExpr(props);
    }

    private static Expr MakeTransparentObject(QueryScope scope, string innerVarName,
        string outerParamName, string innerParamName)
    {
        return MakeAnonymousObject(
            (outerParamName, new IdentifierExpr(SyntheticToken(outerParamName))),
            (innerVarName, new IdentifierExpr(SyntheticToken(innerParamName))));
    }

    private static Token SyntheticToken(string name)
    {
        return new Token(TokenType.Identifier, name, null, 0, 0);
    }

    private string GenerateTransparentId()
    {
        return $"_t{_transparentIdCounter++}";
    }

    /// <summary>
    /// Deep-walks an expression AST and rewrites IdentifierExpr nodes whose names
    /// match range variables to their access path through the transparent identifier chain.
    /// </summary>
    private static Expr RewriteIdentifiers(Expr expr, QueryScope scope)
    {
        return expr switch
        {
            IdentifierExpr id when scope.TryGetAccessPath(id.Name.Lexeme, out var accessPath) =>
                accessPath,

            BinaryExpr binary =>
                new BinaryExpr(
                    RewriteIdentifiers(binary.Left, scope),
                    binary.Op,
                    RewriteIdentifiers(binary.Right, scope)),

            LogicalExpr logical =>
                new LogicalExpr(
                    RewriteIdentifiers(logical.Left, scope),
                    logical.Op,
                    RewriteIdentifiers(logical.Right, scope)),

            UnaryExpr unary =>
                new UnaryExpr(unary.Op, RewriteIdentifiers(unary.Right, scope)),

            CallExpr call =>
                new CallExpr(
                    RewriteIdentifiers(call.Callee, scope),
                    call.Arguments.Select(a => RewriteIdentifiers(a, scope)).ToList(),
                    call.TypeArguments),

            MemberAccessExpr member =>
                new MemberAccessExpr(
                    RewriteIdentifiers(member.Object, scope),
                    member.Name,
                    member.NullSafe),

            IndexAccessExpr index =>
                new IndexAccessExpr(
                    RewriteIdentifiers(index.Object, scope),
                    RewriteIdentifiers(index.Index, scope),
                    index.NullSafe),

            ConditionalExpr cond =>
                new ConditionalExpr(
                    RewriteIdentifiers(cond.Condition, scope),
                    RewriteIdentifiers(cond.ThenBranch, scope),
                    RewriteIdentifiers(cond.ElseBranch, scope)),

            CastExpr cast =>
                new CastExpr(cast.TargetType, RewriteIdentifiers(cast.Expression, scope)),

            IsPatternExpr isPattern =>
                new IsPatternExpr(RewriteIdentifiers(isPattern.Expression, scope), isPattern.Pattern),

            AsExpr asExpr =>
                new AsExpr(RewriteIdentifiers(asExpr.Expression, scope), asExpr.TargetType),

            NullCoalesceExpr nullCoalesce =>
                new NullCoalesceExpr(
                    RewriteIdentifiers(nullCoalesce.Left, scope),
                    RewriteIdentifiers(nullCoalesce.Right, scope)),

            LambdaExpr lambda =>
                RewriteLambda(lambda, scope),

            ObjectLiteralExpr objLit =>
                new ObjectLiteralExpr(
                    objLit.Properties.Select(p =>
                        (p.Key, RewriteIdentifiers(p.Value, scope))).ToList()),

            CollectionExpr arr =>
                new CollectionExpr(
                    arr.Elements.Select(e => RewriteIdentifiers(e, scope)).ToList()),

            InterpolatedStringExpr interp =>
                new InterpolatedStringExpr(
                    interp.Parts.Select(p => p switch
                    {
                        ExpressionPart ep => (InterpolatedPart)new ExpressionPart(
                            RewriteIdentifiers(ep.Expression, scope),
                            ep.AlignmentSpecifier,
                            ep.FormatSpecifier),
                        _ => p
                    }).ToList()),

            NewExpr newExpr =>
                new NewExpr(RewriteIdentifiers(newExpr.Initializer, scope)),

            LiteralExpr or TypeReferenceExpr or DefaultExpr or NameofExpr
                or TypeofExpr or SizeofExpr =>
                expr,

            IdentifierExpr => expr,

            _ => expr
        };
    }

    /// <summary>
    /// Rewrites a lambda expression, excluding the lambda's own parameters from rewriting.
    /// </summary>
    private static Expr RewriteLambda(LambdaExpr lambda, QueryScope scope)
    {
        // Lambda parameters shadow range variables
        var shadowedScope = scope.WithShadowedVariables(
            lambda.Parameters.Select(p => p.Name.Lexeme).ToHashSet());

        return new LambdaExpr(
            lambda.Parameters,
            RewriteIdentifiers(lambda.Body, shadowedScope));
    }

    private bool CheckInAfterNext()
    {
        if (State.Current + 1 >= State.Tokens.Count)
            return false;
        return State.Tokens[State.Current + 1].Type == TokenType.In;
    }

    private static bool IsIdentifierOrContextualKeyword(TokenType type)
    {
        return type == TokenType.Identifier || IsContextualKeyword(type);
    }

    /// <summary>
    /// Tracks the mapping from range variable names to their access expressions
    /// through transparent identifier chains.
    /// </summary>
    private sealed class QueryScope
    {
        private readonly Dictionary<string, VariableAccess> _variables = new();

        /// <summary>
        /// The parameter name used in the current lambda context.
        /// Initially the range variable name; becomes transparent identifier after SelectMany.
        /// </summary>
        public string CurrentParameterName { get; private set; }

        public IEnumerable<string> AllVariableNames => _variables.Keys;

        public QueryScope(string initialRangeVar)
        {
            CurrentParameterName = initialRangeVar;
            _variables[initialRangeVar] = new VariableAccess.Direct(initialRangeVar);
        }

        private QueryScope(Dictionary<string, VariableAccess> variables, string currentParam)
        {
            foreach (var kvp in variables)
                _variables[kvp.Key] = kvp.Value;
            CurrentParameterName = currentParam;
        }

        public void AddDirectVariable(string name, string paramName)
        {
            _variables[name] = new VariableAccess.Direct(paramName);
        }

        /// <summary>
        /// After a let/SelectMany/join with transparent identifier, all existing variables
        /// get their access paths prefixed with the old parameter name, and the new variable
        /// is added as a direct member of the transparent identifier.
        /// </summary>
        public void AbsorbIntoTransparentIdentifier(string transparentId, string newVarName)
        {
            var oldParam = CurrentParameterName;
            var oldVars = _variables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _variables.Clear();

            foreach (var (varName, access) in oldVars)
            {
                var newPath = access switch
                {
                    VariableAccess.Direct => [oldParam],
                    VariableAccess.ThroughChain tc => new List<string> { oldParam }.Concat(tc.MemberPath).ToList(),
                    _ => throw new InvalidOperationException()
                };
                _variables[varName] = new VariableAccess.ThroughChain(newPath);
            }

            _variables[newVarName] = new VariableAccess.ThroughChain([newVarName]);

            CurrentParameterName = transparentId;
        }

        public bool TryGetAccessPath(string variableName, out Expr accessExpr)
        {
            if (!_variables.TryGetValue(variableName, out var access))
            {
                accessExpr = null!;
                return false;
            }

            accessExpr = access switch
            {
                VariableAccess.Direct d =>
                    new IdentifierExpr(SyntheticToken(d.ParamName)),

                VariableAccess.ThroughChain tc =>
                    BuildChainedAccess(CurrentParameterName, tc.MemberPath),

                _ => throw new InvalidOperationException($"Unknown access type for variable '{variableName}'")
            };

            return true;
        }

        public Expr GetAccessExpression(string variableName, string paramName)
        {
            var access = _variables[variableName];
            return access switch
            {
                VariableAccess.Direct =>
                    new IdentifierExpr(SyntheticToken(paramName)),

                VariableAccess.ThroughChain tc =>
                    BuildChainedAccess(paramName, tc.MemberPath),

                _ => throw new InvalidOperationException($"Unknown access type for variable '{variableName}'")
            };
        }

        private static Expr BuildChainedAccess(string paramName, List<string> memberPath)
        {
            Expr current = new IdentifierExpr(SyntheticToken(paramName));
            foreach (var member in memberPath)
            {
                current = new MemberAccessExpr(current, SyntheticToken(member), false);
            }
            return current;
        }

        /// <summary>
        /// Creates a copy with certain variables excluded from rewriting
        /// (used when lambda parameters shadow range variables).
        /// </summary>
        public QueryScope WithShadowedVariables(HashSet<string> shadowedNames)
        {
            var filtered = _variables
                .Where(kvp => !shadowedNames.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return new QueryScope(filtered, CurrentParameterName);
        }

        public QueryScope Clone()
        {
            return new QueryScope(_variables, CurrentParameterName);
        }

        private abstract record VariableAccess
        {
            /// <summary>Direct access: the variable is the lambda parameter itself.</summary>
            public sealed record Direct(string ParamName) : VariableAccess;

            /// <summary>
            /// Access through transparent identifier member chain.
            /// MemberPath is the list of member names to walk from the current parameter.
            /// For single-level: ["varName"] means param.varName
            /// For nested: ["_t0", "varName"] means param._t0.varName
            /// </summary>
            public sealed record ThroughChain(List<string> MemberPath) : VariableAccess;
        }
    }
}
