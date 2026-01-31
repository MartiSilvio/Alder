namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode) 
{
    [Test]
    public void Eval_StringConcatenation()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"Hello\" + \" \" + \"World\""), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Eval_InterpolatedString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "World");

        Assert.That(engine.Evaluate("$\"Hello {name}!\""), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_StringMethod_ToLower()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "HELLO");

        Assert.That(engine.Evaluate("s.ToLower()"), Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_StringMethod_ToUpper()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "hello");

        Assert.That(engine.Evaluate("s.ToUpper()"), Is.EqualTo("HELLO"));
    }

    [Test]
    public void Eval_StringProperty_Length()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "hello");

        Assert.That(engine.Evaluate("s.Length"), Is.EqualTo(5));
    }
}
