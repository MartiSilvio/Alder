using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.GotoStatement, "Goto")]
internal sealed partial record BoundGotoExpr(GotoExpr Source, BoundType StaticType) : BoundExpr(StaticType)
{
    public string Label => Source.Label;
}

[BoundNode(BoundNodeKind.GotoCaseStatement, "GotoCase")]
internal sealed partial record BoundGotoCaseExpr(BoundExpr Value, BoundType StaticType) : BoundExpr(StaticType);

[BoundNode(BoundNodeKind.GotoDefaultStatement, "GotoDefault")]
internal sealed partial record BoundGotoDefaultExpr(BoundType StaticType) : BoundExpr(StaticType);

[BoundNode(BoundNodeKind.Label, "Label")]
internal sealed partial record BoundLabelExpr(LabelExpr Source, BoundType StaticType) : BoundExpr(StaticType)
{
    public string Name => Source.Name;
}
