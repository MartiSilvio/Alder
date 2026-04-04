using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundGotoExpr(GotoExpr Source, BoundType StaticType) : BoundExpr(StaticType)
{
    public string Label => Source.Label;

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

internal sealed record BoundLabelExpr(LabelExpr Source, BoundType StaticType) : BoundExpr(StaticType)
{
    public string Name => Source.Name;

    internal override BoundNodeKind Kind => BoundNodeKind.Label;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
