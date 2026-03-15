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
}
