using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.Optimization;
using Alder.Parsing;

namespace Alder.Test.Optimization;

[TestFixture]
public sealed class BoundTreeOptimizerTests
{
    [Test]
    public void Pipeline_FoldsThenEliminates()
    {
        // 2 > 1 ? "yes" : "no" → constant fold 2>1 to true → dead branch eliminates to "yes"
        var tree = new BoundConditionalExpr(
            new BoundBinaryExpr(
                TokenType.Greater,
                new BoundLiteralExpr(2,
            new BoundType(typeof(int))),
                new BoundLiteralExpr(1,
            new BoundType(typeof(int))),
                new BoundType(typeof(bool))),
            new BoundLiteralExpr("yes",
            new BoundType(typeof(string))),
            new BoundLiteralExpr("no",
            new BoundType(typeof(string))),
            new BoundType(typeof(string)));

        var result = BoundTreeOptimizer.Optimize(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo("yes"));
    }

    [Test]
    public void Pipeline_InsertsConversions_ForMixedTypes()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundIdentifierExpr("x",
            new BoundType(typeof(int))),
            new BoundIdentifierExpr("y",
            new BoundType(typeof(long))),
            new BoundType(typeof(long)));

        var result = BoundTreeOptimizer.Optimize(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundCastExpr>());
        Assert.That(((BoundCastExpr)binary.Left).TargetType, Is.EqualTo(typeof(long)));
    }

    [Test]
    public void Pipeline_DoesNotMutateOriginalTree()
    {
        var left = new BoundLiteralExpr(2,
            new BoundType(typeof(int)));
        var right = new BoundLiteralExpr(3,
            new BoundType(typeof(int)));
        var tree = new BoundBinaryExpr(TokenType.Plus, left, right,
            new BoundType(typeof(int)));

        var result = BoundTreeOptimizer.Optimize(tree);

        // Original tree is unchanged
        Assert.That(tree.Left, Is.SameAs(left));
        Assert.That(tree.Right, Is.SameAs(right));
        // Result is a folded literal
        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(5));
    }
}
