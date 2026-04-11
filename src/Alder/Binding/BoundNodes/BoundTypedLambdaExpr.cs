using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal readonly record struct BoundTypedLambdaParameter(string Name, Type Type);

[BoundNode(BoundNodeKind.TypedLambda, "TypedLambda")]
internal sealed partial record BoundTypedLambdaExpr(
    ImmutableArray<BoundTypedLambdaParameter> Parameters,
    BoundExpr Body,
    Type DelegateType,
    BoundType StaticType) : BoundExpr(StaticType);
