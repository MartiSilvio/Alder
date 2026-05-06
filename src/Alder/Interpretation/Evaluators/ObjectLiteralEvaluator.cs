using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;

namespace Alder.Interpretation.Evaluators;

[EvaluatesNode(BoundNodeKind.ObjectLiteral)]
internal static class ObjectLiteralEvaluator
{
    public static object? Evaluate(BoundObjectLiteralExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var values = EvaluateValues(
            node,
            static (context, property, token) => context.Evaluate(property.Value, token),
            ctx,
            ct);
        return CreateObject(node, values);
    }

    public static async ValueTask<object?> EvaluateAsync(BoundObjectLiteralExpr node, EvaluationContext ctx, CancellationToken ct)
    {
        var values = new object?[node.Properties.Length];
        for (var i = 0; i < node.Properties.Length; i++)
            values[i] = await ctx.EvaluateAsync(node.Properties[i].Value, ct);

        return CreateObject(node, values);
    }

    private static object?[] EvaluateValues(
        BoundObjectLiteralExpr node,
        Func<EvaluationContext, BoundObjectLiteralProperty, CancellationToken, object?> evaluate,
        EvaluationContext ctx,
        CancellationToken ct)
    {
        var values = new object?[node.Properties.Length];
        for (var i = 0; i < node.Properties.Length; i++)
            values[i] = evaluate(ctx, node.Properties[i], ct);
        return values;
    }

    private static StructuralObjectValue CreateObject(BoundObjectLiteralExpr node, object?[] values)
    {
        if (((BoundStructuralType)node.StaticType).StructuralInfo is not { } structuralInfo)
            throw new InvalidOperationException("Structural object literal missing runtime type metadata.");

        return StructuralObjectTypeFactory.Create(structuralInfo, values);
    }
}
