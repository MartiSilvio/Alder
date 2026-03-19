using System.Collections.Immutable;

namespace CsEval.Binding.BoundNodes;

internal sealed record BoundForEachExpr(
    string VariableName,
    BoundExpr Collection,
    ImmutableArray<BoundExpr> Body,
    Type ElementType,
    Type StaticType,
    int? LocalId = null) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.ForEachStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        visit(Collection);
        foreach (var s in Body) visit(s);
    }
}
