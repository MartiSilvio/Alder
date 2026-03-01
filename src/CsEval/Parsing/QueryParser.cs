namespace CsEval.Parsing;

/// <summary>
/// Parses query expressions (ECMA-334 §12.20) and desugars them at parse time
/// into equivalent LINQ method call AST nodes (CallExpr, LambdaExpr, MemberAccessExpr).
/// No new AST nodes are introduced -- the desugared result is indistinguishable from
/// hand-written .Where(x => ...).Select(x => ...) method chains.
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
    /// Determines whether the current token sequence starting with 'from' is a query expression.
    /// Uses lookahead to disambiguate from 'from' used as an identifier.
    /// ECMA-334 §12.20: from identifier in expression ...
    /// </summary>
    internal bool IsQueryExpressionStart()
    {
        if (!Check(TokenType.From))
            return false;

        var saved = State.Current;
        try
        {
            Advance(); // skip 'from'

            // from TYPE IDENT in -> typed query (e.g., from int x in list)
            if (IsTypeKeyword(Peek().Type))
            {
                Advance(); // skip type keyword
                if (IsIdentifierOrContextualKeyword(Peek().Type))
                {
                    Advance(); // skip identifier
                    return Check(TokenType.In);
                }
                return false;
            }

            // from IDENT in -> untyped query (e.g., from x in list)
            if (IsIdentifierOrContextualKeyword(Peek().Type))
            {
                Advance(); // skip identifier
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
    /// Parses a complete query expression starting from the 'from' keyword.
    /// Returns the desugared LINQ method call chain as existing AST nodes.
    /// </summary>
    internal Expr ParseQueryExpression()
    {
        // Parse initial: from rangeVar in sourceExpr
        Consume(TokenType.From, "Expected 'from' at start of query expression");

        // Skip explicit type annotation (CsEval is dynamic, no Cast<T>() needed)
        if (IsTypeKeyword(Peek().Type) && !CheckInAfterNext())
        {
            Advance(); // skip type keyword (type is ignored in dynamic evaluation)
        }

        var rangeVarToken = ConsumeIdentifierOrContextualKeyword("Expected range variable name after 'from'");
        var rangeVarName = rangeVarToken.Lexeme;

        Consume(TokenType.In, "Expected 'in' after range variable in from clause");

        var source = ParseQuerySourceExpression();

        // Initialize scope: single range variable mapping to itself
        var scope = new QueryScope(rangeVarName);

        // Parse body clauses and terminal clause
        return ParseQueryBody(source, scope);
    }

    /// <summary>
    /// Parses the body of a query expression: zero or more body clauses followed by
    /// a terminal clause (select or group...by).
    /// </summary>
    private Expr ParseQueryBody(Expr source, QueryScope scope)
    {
        // Process body clauses in a loop until terminal clause
        while (true)
        {
            if (Check(TokenType.Where))
            {
                source = ParseWhereClause(source, scope);
            }
            else if (Check(TokenType.From))
            {
                // Second from clause -> SelectMany
                // ParseSecondFromClause handles continuation: returns the complete
                // expression for the optimized case (select follows directly), or
                // calls ParseQueryBody recursively for the general case.
                return ParseSecondFromClause(source, scope);
            }
            else if (Check(TokenType.Select))
            {
                // Terminal: select clause
                return ParseSelectClause(source, scope);
            }
            else if (Check(TokenType.Group))
            {
                // Terminal: group...by clause (deferred to Plan 2)
                throw new CsEvalParserException(
                    $"'group...by' clause is not yet supported at {Peek().Line}:{Peek().Column}");
            }
            else
            {
                throw new CsEvalParserException(
                    "Query expression must end with a select clause or group clause" +
                    $" at {Peek().Line}:{Peek().Column}");
            }
        }
    }

    #region Body Clauses

    /// <summary>
    /// Parses: where predicate
    /// Desugars to: source.Where(rangeVar => predicate)
    /// </summary>
    private Expr ParseWhereClause(Expr source, QueryScope scope)
    {
        Advance(); // consume 'where'

        var predicate = ParseQueryBodyExpression();

        // Rewrite identifier references through transparent identifiers
        predicate = RewriteIdentifiers(predicate, scope);

        var lambdaParam = scope.CurrentParameterName;
        var lambda = MakeLambda(lambdaParam, predicate);

        return MakeMethodCall(source, "Where", lambda);
    }

    /// <summary>
    /// Parses: from rangeVar2 in source2
    /// Desugars to: source.SelectMany(rangeVar => source2, (rangeVar, rangeVar2) => new { rangeVar, rangeVar2 })
    /// or optimized form when followed directly by select.
    /// </summary>
    private Expr ParseSecondFromClause(Expr source, QueryScope scope)
    {
        Advance(); // consume 'from'

        // Skip explicit type annotation
        if (IsTypeKeyword(Peek().Type) && !CheckInAfterNext())
        {
            Advance();
        }

        var rangeVar2Token = ConsumeIdentifierOrContextualKeyword("Expected range variable name after 'from'");
        var rangeVar2Name = rangeVar2Token.Lexeme;

        Consume(TokenType.In, "Expected 'in' after range variable in from clause");

        var source2Expr = ParseQuerySourceExpression();

        // Rewrite source2 expression through current scope
        source2Expr = RewriteIdentifiers(source2Expr, scope);

        var outerParam = scope.CurrentParameterName;

        // Check if this is followed directly by 'select' (optimization: no transparent identifier needed)
        if (Check(TokenType.Select))
        {
            Advance(); // consume 'select'
            var projection = ParseQueryBodyExpression();

            // In the optimized form, both outer and inner variables are directly available
            // Create a temporary scope for rewriting the projection
            var tempScope = scope.Clone();
            tempScope.AddDirectVariable(rangeVar2Name, rangeVar2Name);

            projection = RewriteIdentifiers(projection, tempScope);

            // source.SelectMany(outerParam => source2, (outerParam, rangeVar2) => projection)
            var collectionLambda = MakeLambda(outerParam, source2Expr);
            var resultLambda = MakeLambda2(outerParam, rangeVar2Name, projection);

            return MakeMethodCall(source, "SelectMany", collectionLambda, resultLambda);
        }

        // General case: create transparent identifier
        var transparentId = GenerateTransparentId();

        // source.SelectMany(outerParam => source2, (outerParam, rangeVar2) => new { outerParam, rangeVar2 })
        var collectionSelector = MakeLambda(outerParam, source2Expr);

        // Build: new { <outerVars...>, rangeVar2 }
        // The result selector projects outer scope vars and new var into anonymous object
        var resultSelectorBody = MakeTransparentObject(scope, rangeVar2Name, outerParam, rangeVar2Name);
        var resultSelector = MakeLambda2(outerParam, rangeVar2Name, resultSelectorBody);

        var result = MakeMethodCall(source, "SelectMany", collectionSelector, resultSelector);

        // Update scope: all previous variables now accessed through transparentId.memberName
        scope.AbsorbIntoTransparentIdentifier(transparentId, rangeVar2Name);

        // Continue parsing body
        return ParseQueryBody(result, scope);
    }

    #endregion

    #region Terminal Clauses

    /// <summary>
    /// Parses: select projection
    /// Desugars to: source.Select(rangeVar => projection)
    /// </summary>
    private Expr ParseSelectClause(Expr source, QueryScope scope)
    {
        Advance(); // consume 'select'

        var projection = ParseQueryBodyExpression();

        // Rewrite identifier references through transparent identifiers
        projection = RewriteIdentifiers(projection, scope);

        var lambdaParam = scope.CurrentParameterName;
        var lambda = MakeLambda(lambdaParam, projection);

        return MakeMethodCall(source, "Select", lambda);
    }

    #endregion

    #region Expression Parsing

    /// <summary>
    /// Parses an expression within a query body clause.
    /// Query clause boundaries are defined by query keywords (where, select, from, etc.)
    /// at the current nesting level.
    /// </summary>
    private Expr ParseQueryBodyExpression()
    {
        // Parse a full expression using the expression parser.
        // The expression parser's precedence chain will naturally stop at query keywords
        // because they are contextual keywords that don't participate in binary operations.
        return _expression.ParseExpression();
    }

    /// <summary>
    /// Parses a source expression in a from clause.
    /// Stops at query keywords that indicate the start of the next clause.
    /// </summary>
    private Expr ParseQuerySourceExpression()
    {
        return _expression.ParseExpression();
    }

    #endregion

    #region AST Construction Helpers

    /// <summary>
    /// Creates a single-parameter lambda expression: paramName => body
    /// </summary>
    private static LambdaExpr MakeLambda(string paramName, Expr body)
    {
        var paramToken = SyntheticToken(paramName);
        return new LambdaExpr(
            [new LambdaParameter(null, paramToken)],
            body);
    }

    /// <summary>
    /// Creates a two-parameter lambda expression: (param1, param2) => body
    /// </summary>
    private static LambdaExpr MakeLambda2(string param1, string param2, Expr body)
    {
        return new LambdaExpr(
            [
                new LambdaParameter(null, SyntheticToken(param1)),
                new LambdaParameter(null, SyntheticToken(param2))
            ],
            body);
    }

    /// <summary>
    /// Creates a method call: source.methodName(args...)
    /// </summary>
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

    /// <summary>
    /// Creates the transparent identifier anonymous object for SelectMany result selector.
    /// Builds: new { outerVarProps..., innerVar = innerVarExpr }
    /// </summary>
    private static Expr MakeTransparentObject(QueryScope scope, string innerVarName,
        string outerParamName, string innerParamName)
    {
        var props = new List<(string name, Expr value)>();

        // Add all current scope variables as properties
        foreach (var varName in scope.AllVariableNames)
        {
            // Each existing variable needs to be accessed from the outer parameter
            var accessExpr = scope.GetAccessExpression(varName, outerParamName);
            props.Add((varName, accessExpr));
        }

        // Add the new inner variable
        props.Add((innerVarName, new IdentifierExpr(SyntheticToken(innerParamName))));

        return MakeAnonymousObject([..props]);
    }

    /// <summary>
    /// Creates a synthetic token for use in AST node construction.
    /// </summary>
    private static Token SyntheticToken(string name)
    {
        return new Token(TokenType.Identifier, name, null, 0, 0);
    }

    private string GenerateTransparentId()
    {
        return $"_t{_transparentIdCounter++}";
    }

    #endregion

    #region Identifier Rewriting

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

            // Recursively rewrite composite expressions
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

            ArrayLiteralExpr arr =>
                new ArrayLiteralExpr(
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

            // Leaf nodes that don't need rewriting
            LiteralExpr or TypeReferenceExpr or DefaultExpr or NameofExpr
                or TypeofExpr or SizeofExpr =>
                expr,

            // IdentifierExpr that didn't match a range variable -- leave as-is
            IdentifierExpr => expr,

            // Anything else -- return as-is (blocks, statements, etc. shouldn't appear in query clauses)
            _ => expr
        };
    }

    /// <summary>
    /// Rewrites a lambda expression, excluding the lambda's own parameters from rewriting.
    /// </summary>
    private static Expr RewriteLambda(LambdaExpr lambda, QueryScope scope)
    {
        // Lambda parameters shadow range variables -- create a scope that excludes them
        var shadowedScope = scope.WithShadowedVariables(
            lambda.Parameters.Select(p => p.Name.Lexeme).ToHashSet());

        return new LambdaExpr(
            lambda.Parameters,
            RewriteIdentifiers(lambda.Body, shadowedScope));
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Checks if the token after next is 'in' (for disambiguating typed from clauses).
    /// </summary>
    private bool CheckInAfterNext()
    {
        if (State.Current + 1 >= State.Tokens.Count)
            return false;
        return State.Tokens[State.Current + 1].Type == TokenType.In;
    }

    /// <summary>
    /// Returns true if the token type is an identifier or a contextual keyword
    /// that can be used as a range variable name.
    /// </summary>
    private static bool IsIdentifierOrContextualKeyword(TokenType type)
    {
        return type == TokenType.Identifier || IsContextualKeyword(type);
    }

    #endregion

    #region Query Scope

    /// <summary>
    /// Tracks the mapping from range variable names to their access expressions
    /// through transparent identifier chains.
    /// </summary>
    private sealed class QueryScope
    {
        /// <summary>
        /// Maps variable names to their access path descriptions.
        /// When no transparent identifier: variable maps to itself.
        /// After transparent identifier: variable maps to chain of member accesses.
        /// </summary>
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

        /// <summary>
        /// Adds a direct variable mapping (used for optimized SelectMany case).
        /// </summary>
        public void AddDirectVariable(string name, string paramName)
        {
            _variables[name] = new VariableAccess.Direct(paramName);
        }

        /// <summary>
        /// After a SelectMany with transparent identifier, all existing variables
        /// become accessible through the transparent identifier's properties,
        /// and the new variable is also added.
        /// </summary>
        public void AbsorbIntoTransparentIdentifier(string transparentId, string newVarName)
        {
            // Existing variables: each gets wrapped with one level of member access
            var oldVars = _variables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _variables.Clear();

            foreach (var (varName, _) in oldVars)
            {
                _variables[varName] = new VariableAccess.ThroughMember(transparentId, varName);
            }

            // New variable: accessed through transparent identifier
            _variables[newVarName] = new VariableAccess.ThroughMember(transparentId, newVarName);

            // The lambda parameter is now the transparent identifier
            CurrentParameterName = transparentId;
        }

        /// <summary>
        /// Gets the AST expression to access a given variable.
        /// </summary>
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
                    // Direct reference: use the variable's own parameter name
                    new IdentifierExpr(SyntheticToken(d.ParamName)),

                VariableAccess.ThroughMember m =>
                    // Access through transparent identifier: _t0.varName
                    new MemberAccessExpr(
                        new IdentifierExpr(SyntheticToken(CurrentParameterName)),
                        SyntheticToken(m.MemberName),
                        false),

                _ => throw new InvalidOperationException($"Unknown access type for variable '{variableName}'")
            };

            return true;
        }

        /// <summary>
        /// Gets the access expression for a variable in the context of a specific parameter name.
        /// Used when building result selector lambdas.
        /// </summary>
        public Expr GetAccessExpression(string variableName, string paramName)
        {
            var access = _variables[variableName];
            return access switch
            {
                VariableAccess.Direct =>
                    new IdentifierExpr(SyntheticToken(paramName)),

                VariableAccess.ThroughMember m =>
                    new MemberAccessExpr(
                        new IdentifierExpr(SyntheticToken(paramName)),
                        SyntheticToken(m.MemberName),
                        false),

                _ => throw new InvalidOperationException($"Unknown access type for variable '{variableName}'")
            };
        }

        /// <summary>
        /// Creates a copy of this scope with certain variables excluded from rewriting
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

            /// <summary>Access through transparent identifier member: _t0.memberName</summary>
            public sealed record ThroughMember(string TransparentId, string MemberName) : VariableAccess;
        }
    }

    #endregion
}
