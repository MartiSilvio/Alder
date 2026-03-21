namespace Alder.Binding.BoundNodes;

internal sealed record BoundGotoExpr(string Label, Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}

internal sealed record BoundGotoCaseExpr(BoundExpr Value, Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.GotoCaseStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { visit(Value); }
}

internal sealed record BoundGotoDefaultExpr(Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.GotoDefaultStatement;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}

internal sealed record BoundLabelExpr(string Name, Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.Label;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
