namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalTests(CompilationMode mode)
{
    [Test]
    public async Task Eval_Logical_And()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "true && true";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.True);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "true && false";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.False);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Logical_Or()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "true || false";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.True);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "false || false";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.False);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Logical_Not()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "!true";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.False);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "!false";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.True);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }
}
