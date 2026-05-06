using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.OutArgument, "OutArg")]
internal sealed partial record BoundOutArgExpr(
    OutArgExpr Source,
    BoundType StaticType) : BoundExpr(StaticType)
{
    public string VariableName => Source.VariableName;
    public string? TypeName => Source.TypeName;
    public bool IsDiscard => Source.IsDiscard;
}
