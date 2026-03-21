using System.Collections.Immutable;

namespace Alder.Binding.BoundNodes;

internal abstract record BoundInterpolatedPart;

internal sealed record BoundInterpolatedTextPart(string Text) : BoundInterpolatedPart;

internal sealed record BoundInterpolatedExpressionPart(
    BoundExpr Expression,
    string? AlignmentSpecifier,
    string? FormatSpecifier) : BoundInterpolatedPart;

internal sealed record BoundInterpolatedStringExpr(
    ImmutableArray<BoundInterpolatedPart> Parts,
    Type StaticType) : BoundExpr(StaticType)
{
    internal override BoundNodeKind Kind => BoundNodeKind.InterpolatedString;
    internal override void EnumerateChildren(Action<BoundExpr> visit)
    {
        foreach (var p in Parts)
            if (p is BoundInterpolatedExpressionPart ep)
                visit(ep.Expression);
    }
}
