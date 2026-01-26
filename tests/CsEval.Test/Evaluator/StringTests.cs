namespace CsEval.Test.Evaluator;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode) : TestBase
{
    [Test]
    public void Eval_StringConcatenation()
    {
        var engine = CreateEngine(mode);
        Assert.That(engine.Evaluate("\"Hello\" + \" \" + \"World\""), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Eval_InterpolatedString()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("name", "World");

        Assert.That(engine.Evaluate("$\"Hello {name}!\""), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_StringMethod_ToLower()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("s", "HELLO");

        Assert.That(engine.Evaluate("s.ToLower()"), Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_StringMethod_ToUpper()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("s", "hello");

        Assert.That(engine.Evaluate("s.ToUpper()"), Is.EqualTo("HELLO"));
    }

    [Test]
    public void Eval_StringProperty_Length()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("s", "hello");

        Assert.That(engine.Evaluate("s.Length"), Is.EqualTo(5));
    }
}
