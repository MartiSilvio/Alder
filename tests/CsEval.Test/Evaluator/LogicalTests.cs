namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class LogicalTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Eval_Logical_And()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("true && true"), Is.True);
        Assert.That(engine.Evaluate("true && false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Or()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("true || false"), Is.True);
        Assert.That(engine.Evaluate("false || false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Not()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("!true"), Is.False);
        Assert.That(engine.Evaluate("!false"), Is.True);
    }
}