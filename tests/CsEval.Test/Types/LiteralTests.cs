namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiteralTests(CompilationMode mode)
{
    [Test]
    public async Task Eval_Number_ReturnsNumber()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "42";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(42));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_String_ReturnsString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "\"hello\"";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("hello"));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_Boolean_ReturnsBoolean()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr1 = "true";
        var result1 = engine.Evaluate(expr1);
        Assert.That(result1, Is.EqualTo(true));
        Assert.That(result1, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr1)));

        const string expr2 = "false";
        var result2 = engine.Evaluate(expr2);
        Assert.That(result2, Is.EqualTo(false));
        Assert.That(result2, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr2)));
    }

    [Test]
    public async Task Eval_Null_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "null";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.Null);
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }
}
