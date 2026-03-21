namespace Alder.Parsing;

/// <summary>
/// Walks an AST and collects distinct names of unbound identifiers.
/// Tracks locally declared names (var declarations, lambda parameters,
/// foreach variables, catch clause variables, deconstruction targets)
/// so they are excluded from the result.
/// Uses scope stacking so inner declarations don't hide outer references.
/// </summary>
internal sealed class VariableCollector : AstWalker<byte>
{
    private readonly HashSet<string> _identifiers = [];
    private readonly Stack<HashSet<string>> _scopes = [];

    protected override byte DefaultValue => 0;

    public IReadOnlyList<string> Variables => _identifiers.ToList();

    public void Collect(Expr root)
    {
        _identifiers.Clear();
        _scopes.Clear();
        PushScope();
        Visit(root);
        PopScope();
    }

    public override byte VisitIdentifier(IdentifierExpr expr)
    {
        if (!IsDeclared(expr.Name.Lexeme))
            _identifiers.Add(expr.Name.Lexeme);
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
        CollectPatternDeclarations(expr.Pattern, CurrentScope);
        return base.VisitIsPattern(expr);
    }

    public override byte VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        foreach (var arm in expr.Arms)
            CollectPatternDeclarations(arm.Pattern, CurrentScope);
        return base.VisitSwitchExpression(expr);
    }

    public override byte VisitSwitch(SwitchStatementExpr expr)
    {
        foreach (var caseExpr in expr.Cases)
            if (caseExpr.CasePattern != null)
                CollectPatternDeclarations(caseExpr.CasePattern, CurrentScope);
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

    internal static void CollectPatternDeclarations(Pattern pattern, HashSet<string> declared)
    {
        switch (pattern)
        {
            case TypePattern { VariableName: not null } tp:
                declared.Add(tp.VariableName.Value.Lexeme);
                break;
            case VarPattern vp:
                declared.Add(vp.VariableName.Lexeme);
                break;
            case PropertyPattern { VariableName: not null } pp:
                declared.Add(pp.VariableName.Value.Lexeme);
                foreach (var (_, subPattern) in pp.Properties)
                    CollectPatternDeclarations(subPattern, declared);
                break;
            case PropertyPattern pp2:
                foreach (var (_, subPattern) in pp2.Properties)
                    CollectPatternDeclarations(subPattern, declared);
                break;
            case AndPattern ap:
                CollectPatternDeclarations(ap.Left, declared);
                CollectPatternDeclarations(ap.Right, declared);
                break;
            case OrPattern op:
                CollectPatternDeclarations(op.Left, declared);
                CollectPatternDeclarations(op.Right, declared);
                break;
            case NotPattern np:
                CollectPatternDeclarations(np.Operand, declared);
                break;
            case ParenthesizedPattern par:
                CollectPatternDeclarations(par.Inner, declared);
                break;
            case PositionalPattern pos:
                foreach (var subPattern in pos.Subpatterns)
                    CollectPatternDeclarations(subPattern, declared);
                break;
            case ListPattern lp:
                foreach (var subPattern in lp.Patterns)
                    CollectPatternDeclarations(subPattern, declared);
                break;
            case SlicePattern { Subpattern: not null } sp:
                CollectPatternDeclarations(sp.Subpattern, declared);
                break;
        }
    }
}
