using System.Collections.Immutable;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Binding.Optimization;

namespace Alder.Test.Optimization;

[TestFixture]
public sealed class DeadBranchEliminationTests
{
    private readonly DeadBranchEliminationPass _pass = new();

    [Test]
    public void Eliminates_IfTrueCondition()
    {
        var tree = new BoundIfStatementExpr(
            new BoundLiteralExpr(true, typeof(bool)),
            [new BoundLiteralExpr(42, typeof(int))],
            ImmutableArray<BoundExpr>.Empty,
            typeof(object));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBlockExpr>());
        var block = (BoundBlockExpr)result;
        Assert.That(block.Statements, Has.Length.EqualTo(1));
        Assert.That(((BoundLiteralExpr)block.Statements[0]).Value, Is.EqualTo(42));
    }

    [Test]
    public void Eliminates_IfFalseCondition_WithElse()
    {
        var tree = new BoundIfStatementExpr(
            new BoundLiteralExpr(false, typeof(bool)),
            [new BoundLiteralExpr(1, typeof(int))],
            [new BoundLiteralExpr(2, typeof(int))],
            typeof(object));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundBlockExpr>());
        var block = (BoundBlockExpr)result;
        Assert.That(block.Statements, Has.Length.EqualTo(1));
        Assert.That(((BoundLiteralExpr)block.Statements[0]).Value, Is.EqualTo(2));
    }

    [Test]
    public void Eliminates_IfFalseCondition_NoElse_ReturnsNoop()
    {
        var tree = new BoundIfStatementExpr(
            new BoundLiteralExpr(false, typeof(bool)),
            [new BoundLiteralExpr(1, typeof(int))],
            ImmutableArray<BoundExpr>.Empty,
            typeof(object));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundLiteralExpr>());
        Assert.That(((BoundLiteralExpr)result).Value, Is.Null);
    }

    [Test]
    public void DoesNotEliminate_NonLiteralCondition()
    {
        var tree = new BoundConditionalExpr(
            new BoundIdentifierExpr("flag", typeof(bool)),
            new BoundLiteralExpr("yes", typeof(string)),
            new BoundLiteralExpr("no", typeof(string)),
            typeof(string));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundConditionalExpr>());
    }

    [Test]
    public void DoesNotEliminate_IfStatement_NonLiteralCondition()
    {
        var tree = new BoundIfStatementExpr(
            new BoundIdentifierExpr("flag", typeof(bool)),
            [new BoundLiteralExpr(1, typeof(int))],
            ImmutableArray<BoundExpr>.Empty,
            typeof(object));

        var result = _pass.Rewrite(tree);

        Assert.That(result, Is.TypeOf<BoundIfStatementExpr>());
    }
}
