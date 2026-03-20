namespace CsEval.Parsing;

/// <summary>
/// Walks an AST and captures identifier token occurrences with source locations.
/// Uses the same declaration tracking model as <see cref="VariableCollector"/>
/// to filter out locally declared names.
/// </summary>
internal sealed class IdentifierOccurrenceCollector : AstWalker<byte>
{
    private readonly List<Token> _identifiers = [];
    private readonly HashSet<string> _declared = [];

    protected override byte DefaultValue => 0;

    public void Collect(Expr root)
    {
        _identifiers.Clear();
        _declared.Clear();
        Visit(root);
    }

    public IReadOnlyList<Token> GetUnboundTokens(StringComparer comparer)
    {
        var declared = new HashSet<string>(_declared, comparer);
        var result = new List<Token>(_identifiers.Count);
        foreach (var token in _identifiers)
        {
            if (!declared.Contains(token.Lexeme))
                result.Add(token);
        }

        return result;
    }

    public override byte VisitIdentifier(IdentifierExpr expr)
    {
        _identifiers.Add(expr.Name);
        return 0;
    }

    public override byte VisitVariableDecl(VariableDeclExpr expr)
    {
        _declared.Add(expr.Name.Lexeme);
        Visit(expr.Initializer);
        return 0;
    }

    public override byte VisitLambda(LambdaExpr expr)
    {
        foreach (var param in expr.Parameters)
            _declared.Add(param.Name.Lexeme);
        Visit(expr.Body);
        return 0;
    }

    public override byte VisitForEach(ForEachStatementExpr expr)
    {
        _declared.Add(expr.VariableName.Lexeme);
        Visit(expr.Collection);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        return 0;
    }

    public override byte VisitTryCatchFinally(TryCatchFinallyExpr expr)
    {
        foreach (var stmt in expr.TryBody)
            Visit(stmt);
        foreach (var catchClause in expr.CatchClauses)
        {
            if (catchClause.VariableName.HasValue)
                _declared.Add(catchClause.VariableName.Value.Lexeme);
            if (catchClause.WhenGuard != null)
                Visit(catchClause.WhenGuard);
            foreach (var stmt in catchClause.Body)
                Visit(stmt);
        }

        if (expr.FinallyBody != null)
        {
            foreach (var stmt in expr.FinallyBody)
                Visit(stmt);
        }

        return 0;
    }

    public override byte VisitDeconstruction(DeconstructionExpr expr)
    {
        foreach (var name in expr.VariableNames)
            _declared.Add(name);
        Visit(expr.ValueExpression);
        return 0;
    }

    public override byte VisitOutArg(OutArgExpr expr)
    {
        if (!expr.IsDiscard)
            _declared.Add(expr.VariableName);
        return 0;
    }

    public override byte VisitIsPattern(IsPatternExpr expr)
    {
        VariableCollector.CollectPatternDeclarations(expr.Pattern, _declared);
        return base.VisitIsPattern(expr);
    }

    public override byte VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        foreach (var arm in expr.Arms)
            VariableCollector.CollectPatternDeclarations(arm.Pattern, _declared);
        return base.VisitSwitchExpression(expr);
    }

    public override byte VisitSwitch(SwitchStatementExpr expr)
    {
        foreach (var caseExpr in expr.Cases)
            if (caseExpr.CasePattern != null)
                VariableCollector.CollectPatternDeclarations(caseExpr.CasePattern, _declared);
        return base.VisitSwitch(expr);
    }
}
