using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

internal sealed record BoundLambdaExpr(
    LambdaExpr Source,
    BoundType StaticType) : BoundExpr(StaticType)
{
    public ImmutableArray<string> Parameters { get; } = [..Source.Parameters.Select(static p => p.Name.Lexeme)];
    public Expr Body => Source.Body;
    public bool IsAsync => Source.IsAsync;
    public string? ReturnTypeName => Source.ReturnTypeName;

    internal override BoundNodeKind Kind => BoundNodeKind.Lambda;
    internal override void EnumerateChildren(Action<BoundExpr> visit) { }
}
