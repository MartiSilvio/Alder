namespace Alder.Parsing;

/// <summary>
/// Walks an AST and captures unbound identifier token occurrences with source locations.
/// </summary>
internal sealed class IdentifierOccurrenceCollector : ScopeTrackingWalker
{
    private readonly List<Token> _unboundIdentifiers = [];

    public void Collect(Expr root)
    {
        _unboundIdentifiers.Clear();
        CollectFrom(root);
    }

    public IReadOnlyList<Token> GetUnboundTokens(StringComparer _) => _unboundIdentifiers;

    protected override void OnUnboundIdentifier(IdentifierExpr expr) =>
        _unboundIdentifiers.Add(expr.Name);
}
