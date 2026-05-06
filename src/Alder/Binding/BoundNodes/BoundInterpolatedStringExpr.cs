using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal abstract record BoundInterpolatedPart;

internal sealed record BoundInterpolatedTextPart(string Text) : BoundInterpolatedPart;

[BoundContainer]
internal sealed partial record BoundInterpolatedExpressionPart(
    BoundExpr Expression,
    string? AlignmentSpecifier,
    string? FormatSpecifier) : BoundInterpolatedPart;

[BoundNode(BoundNodeKind.InterpolatedString, "InterpolatedString")]
internal sealed partial record BoundInterpolatedStringExpr(
    ImmutableArray<BoundInterpolatedPart> Parts,
    BoundType StaticType) : BoundExpr(StaticType);
