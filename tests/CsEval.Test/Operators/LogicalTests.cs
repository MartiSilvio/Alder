namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalTests(CompilationMode mode) 
{
    [Test]
    public void Eval_Logical_And()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("true && true"), Is.True);
        Assert.That(engine.Evaluate("true && false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Or()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("true || false"), Is.True);
        Assert.That(engine.Evaluate("false || false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Not()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("!true"), Is.False);
        Assert.That(engine.Evaluate("!false"), Is.True);
    }
}