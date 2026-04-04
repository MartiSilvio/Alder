using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundOutArgExpr(
    OutArgExpr Source,
    BoundType StaticType) : BoundExpr(StaticType)
{
    public string VariableName => Source.VariableName;
    public string? TypeName => Source.TypeName;
    public bool IsDiscard => Source.IsDiscard;

    internal override BoundNodeKind Kind => BoundNodeKind.OutArgument;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
