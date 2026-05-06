namespace Alder.Parsing;

/// <summary>
/// Base walker that tracks lexical scopes (blocks, lambdas, catch clauses, etc.)
/// and determines which identifiers are locally declared vs. unbound.
/// Subclasses override <see cref="OnUnboundIdentifier"/> to collect results.
/// </summary>
internal abstract class ScopeTrackingWalker : AstWalker<byte>
{
    private readonly Stack<HashSet<string>> _scopes = [];

    protected override byte DefaultValue => 0;

    protected void CollectFrom(Expr root)
    {
        _scopes.Clear();
        PushScope();
        Visit(root);
        PopScope();
    }

    protected abstract void OnUnboundIdentifier(IdentifierExpr expr);

    public override byte VisitIdentifier(IdentifierExpr expr)
    {
        if (!IsDeclared(expr.Name.Lexeme))
            OnUnboundIdentifier(expr);
        return 0;
    }

    public override byte VisitVariableDecl(VariableDeclExpr expr)
    {
        Visit(expr.Initializer);
        CurrentScope.Add(expr.Name.Lexeme);
        return 0;
    }

    public override byte VisitLambda(LambdaExpr expr)
    {
        PushScope();
        foreach (var param in expr.Parameters)
            CurrentScope.Add(param.Name.Lexeme);
        Visit(expr.Body);
        PopScope();
        return 0;
    }

    public override byte VisitBlock(BlockExpr expr)
    {
        PushScope();
        foreach (var stmt in expr.Statements)
            Visit(stmt);
        if (expr.ReturnExpr != null)
            Visit(expr.ReturnExpr);
        PopScope();
        return 0;
    }

    public override byte VisitForEach(ForEachStatementExpr expr)
    {
        Visit(expr.Collection);
        PushScope();
        CurrentScope.Add(expr.VariableName.Lexeme);
        foreach (var stmt in expr.Body)
            Visit(stmt);
        PopScope();
        return 0;
    }

    public override byte VisitTryCatchFinally(TryCatchFinallyExpr expr)
    {
        foreach (var stmt in expr.TryBody)
            Visit(stmt);
        foreach (var catchClause in expr.CatchClauses)
        {
            PushScope();
            if (catchClause.VariableName.HasValue)
                CurrentScope.Add(catchClause.VariableName.Value.Lexeme);
            if (catchClause.WhenGuard != null)
                Visit(catchClause.WhenGuard);
            foreach (var stmt in catchClause.Body)
                Visit(stmt);
            PopScope();
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
            CurrentScope.Add(name);
        Visit(expr.ValueExpression);
        return 0;
    }

    public override byte VisitOutArg(OutArgExpr expr)
    {
        if (!expr.IsDiscard)
            CurrentScope.Add(expr.VariableName);
        return 0;
    }

    public override byte VisitIsPattern(IsPatternExpr expr)
    {
        VariableCollector.CollectPatternDeclarations(expr.Pattern, CurrentScope);
        return base.VisitIsPattern(expr);
    }

    public override byte VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        foreach (var arm in expr.Arms)
            VariableCollector.CollectPatternDeclarations(arm.Pattern, CurrentScope);
        return base.VisitSwitchExpression(expr);
    }

    public override byte VisitSwitch(SwitchStatementExpr expr)
    {
        foreach (var caseExpr in expr.Cases)
            if (caseExpr.CasePattern != null)
                VariableCollector.CollectPatternDeclarations(caseExpr.CasePattern, CurrentScope);
        return base.VisitSwitch(expr);
    }

    private HashSet<string> CurrentScope => _scopes.Peek();
    private void PushScope() => _scopes.Push([]);
    private void PopScope() => _scopes.Pop();

    private bool IsDeclared(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.Contains(name))
                return true;
        }
        return false;
    }
}
