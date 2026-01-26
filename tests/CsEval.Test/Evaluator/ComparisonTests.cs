namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ComparisonTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Eval_Comparison_Equals()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("1 == 1"), Is.True);
        Assert.That(engine.Evaluate("1 == 2"), Is.False);
        Assert.That(engine.Evaluate("\"a\" == \"a\""), Is.True);
    }

    [Test]
    public void Eval_Comparison_NotEquals()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("1 != 2"), Is.True);
        Assert.That(engine.Evaluate("1 != 1"), Is.False);
    }

    [Test]
    public void Eval_Comparison_LessThan()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("1 < 2"), Is.True);
        Assert.That(engine.Evaluate("2 < 1"), Is.False);
    }

    [Test]
    public void Eval_Comparison_GreaterThan()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("2 > 1"), Is.True);
        Assert.That(engine.Evaluate("1 > 2"), Is.False);
    }
}
