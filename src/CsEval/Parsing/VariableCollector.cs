namespace CsEval.Parsing;

/// <summary>
/// Walks an AST and collects distinct names of unbound identifiers.
/// Tracks locally declared names (var declarations, lambda parameters,
/// foreach variables, catch clause variables, deconstruction targets)
/// so they are excluded from the result.
/// </summary>
internal sealed class VariableCollector : AstWalker<byte>
{
    private readonly HashSet<string> _identifiers = [];
    private readonly HashSet<string> _declared = [];

    protected override byte DefaultValue => 0;

    /// <summary>
    /// The collected distinct unbound identifier names.
    /// </summary>
    public IReadOnlyList<string> Variables => _identifiers.Except(_declared).ToList();

    /// <summary>
    /// Walks the AST and collects identifier names.
    /// </summary>
    public void Collect(Expr root) => Visit(root);

    public override byte VisitIdentifier(IdentifierExpr expr)
    {
        _identifiers.Add(expr.Name.Lexeme);
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
        CollectPatternDeclarations(expr.Pattern, _declared);
        return base.VisitIsPattern(expr);
    }

    public override byte VisitSwitchExpression(SwitchExpressionExpr expr)
    {
        foreach (var arm in expr.Arms)
            CollectPatternDeclarations(arm.Pattern, _declared);
        return base.VisitSwitchExpression(expr);
    }

    public override byte VisitSwitch(SwitchStatementExpr expr)
    {
        foreach (var caseExpr in expr.Cases)
            if (caseExpr.CasePattern != null)
                CollectPatternDeclarations(caseExpr.CasePattern, _declared);
        return base.VisitSwitch(expr);
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
