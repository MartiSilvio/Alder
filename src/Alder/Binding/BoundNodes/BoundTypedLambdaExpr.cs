using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal readonly record struct BoundTypedLambdaParameter(string Name, Type Type);

internal sealed record BoundTypedLambdaExpr(
    ImmutableArray<BoundTypedLambdaParameter> Parameters,
    BoundExpr Body,
    Type DelegateType,
    BoundType StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.TypedLambda;
    internal override void EnumerateChildren(Action<BoundExpr> visit) => visit(Body);
}
