namespace CsEval.Test.Evaluator;

[TestFixture]
public class ComparisonTests : EvaluatorTestBase
{
    [Test]
    public void Eval_Comparison_Equals()
    {
        Assert.That(Eval("1 == 1"), Is.True);
        Assert.That(Eval("1 == 2"), Is.False);
        Assert.That(Eval("\"a\" == \"a\""), Is.True);
    }

    [Test]
    public void Eval_Comparison_NotEquals()
    {
        Assert.That(Eval("1 != 2"), Is.True);
        Assert.That(Eval("1 != 1"), Is.False);
    }

    [Test]
    public void Eval_Comparison_LessThan()
    {
        Assert.That(Eval("1 < 2"), Is.True);
        Assert.That(Eval("2 < 1"), Is.False);
    }

    [Test]
    public void Eval_Comparison_GreaterThan()
    {
        Assert.That(Eval("2 > 1"), Is.True);
        Assert.That(Eval("1 > 2"), Is.False);
    }
}
