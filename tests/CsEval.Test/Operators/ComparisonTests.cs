namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ComparisonTests(CompilationMode mode)
{
    [Test]
    public async Task Eval_Comparison_Equals()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "1 == 1";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.True);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "1 == 2";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.False);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));

        const string expr3 = "\"a\" == \"a\"";
        var result3 = engine.Evaluate(expr3);
        Assert.That(result3, Is.True);
        Assert.That(result3, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr3)));
    }

    [Test]
    public async Task Eval_Comparison_NotEquals()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "1 != 2";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.True);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "1 != 1";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.False);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Comparison_LessThan()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "1 < 2";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.True);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "2 < 1";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.False);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Comparison_GreaterThan()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "2 > 1";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.True);
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "1 > 2";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.False);
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }
}
