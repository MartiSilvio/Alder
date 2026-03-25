namespace Alder.Binding.BoundNodes;

internal sealed record BoundGotoExpr(string Label, BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}

internal sealed record BoundGotoCaseExpr(BoundExpr Value, BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.GotoCaseStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}

internal sealed record BoundGotoDefaultExpr(BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.GotoDefaultStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}

internal sealed record BoundLabelExpr(string Name, BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Label;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
