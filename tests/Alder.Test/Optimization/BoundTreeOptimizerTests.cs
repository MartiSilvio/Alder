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
                new BoundLiteralExpr(2, typeof(int)),
                new BoundLiteralExpr(1, typeof(int)),
                typeof(bool)),
            new BoundLiteralExpr("yes", typeof(string)),
            new BoundLiteralExpr("no", typeof(string)),
            typeof(string));

        var result = BoundTreeOptimizer.Optimize(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo("yes"));
    }

    [Test]
    public void Pipeline_InsertsConversions_ForMixedTypes()
    {
        var tree = new BoundBinaryExpr(
            TokenType.Plus,
            new BoundIdentifierExpr("x", typeof(int)),
            new BoundIdentifierExpr("y", typeof(long)),
            typeof(long));

        var result = BoundTreeOptimizer.Optimize(tree);

        Assert.That(result, Is.TypeOf<BoundBinaryExpr>());
        var binary = (BoundBinaryExpr)result;
        Assert.That(binary.Left, Is.TypeOf<BoundCastExpr>());
        Assert.That(((BoundCastExpr)binary.Left).TargetType, Is.EqualTo(typeof(long)));
    }

    [Test]
    public void Pipeline_DoesNotMutateOriginalTree()
    {
        var left = new BoundLiteralExpr(2, typeof(int));
        var right = new BoundLiteralExpr(3, typeof(int));
        var tree = new BoundBinaryExpr(TokenType.Plus, left, right, typeof(int));

        var result = BoundTreeOptimizer.Optimize(tree);

        // Original tree is unchanged
        Assert.That(tree.Left, Is.SameAs(left));
        Assert.That(tree.Right, Is.SameAs(right));
        // Result is a folded literal
        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.EqualTo(5));
    }
}
