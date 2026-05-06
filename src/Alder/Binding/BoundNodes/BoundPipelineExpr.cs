namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.PipelineExpression, "Pipeline")]
internal sealed partial record BoundPipelineExpr(
    BoundExpr Left,
    BoundExpr Right,
    BoundType StaticType) : BoundExpr(StaticType);
