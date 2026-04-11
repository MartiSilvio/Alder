using System.Collections.Immutable;
using Alder.Parsing;

namespace Alder.Binding.BoundNodes;

[BoundNode(BoundNodeKind.Lambda, "Lambda")]
internal sealed partial record BoundLambdaExpr(
    LambdaExpr Source,
    BoundType StaticType) : BoundExpr(StaticType)
{
    public ImmutableArray<string> Parameters { get; } = [..Source.Parameters.Select(static p => p.Name.Lexeme)];
    public Expr Body => Source.Body;
    public bool IsAsync => Source.IsAsync;
    public string? ReturnTypeName => Source.ReturnTypeName;
}
