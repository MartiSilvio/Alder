namespace Alder.Parsing;

/// <summary>
/// Walks an AST and collects distinct names of unbound identifiers.
/// </summary>
internal sealed class VariableCollector : ScopeTrackingWalker
{
    private readonly HashSet<string> _identifiers = [];

    public IReadOnlyList<string> Variables => _identifiers.ToList();

    public void Collect(Expr root)
    {
        _identifiers.Clear();
        CollectFrom(root);
    }

    protected override void OnUnboundIdentifier(IdentifierExpr expr) =>
        _identifiers.Add(expr.Name.Lexeme);

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
