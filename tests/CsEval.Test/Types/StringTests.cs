namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode)
{
    [Test]
    public async Task Eval_StringConcatenation()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "\"Hello\" + \" \" + \"World\"";
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo("Hello World"));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public void Eval_InterpolatedString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "World");

        Assert.That(engine.Evaluate("$\"Hello {name}!\""), Is.EqualTo("Hello World!"));
    }

    [Test]
    public async Task Eval_StringMethod_ToLower()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "HELLO");

        const string expr = "\"HELLO\".ToLower()";
        var result = engine.Evaluate("s.ToLower()");
        Assert.That(result, Is.EqualTo("hello"));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_StringMethod_ToUpper()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "hello");

        const string expr = "\"hello\".ToUpper()";
        var result = engine.Evaluate("s.ToUpper()");
        Assert.That(result, Is.EqualTo("HELLO"));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }

    [Test]
    public async Task Eval_StringProperty_Length()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "hello");

        const string expr = "\"hello\".Length";
        var result = engine.Evaluate("s.Length");
        Assert.That(result, Is.EqualTo(5));
        Assert.That(result, Is.EqualTo(await TestHelpers.EvaluateCSharpAsync(expr)));
    }
}
