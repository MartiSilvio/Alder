namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiteralTests(CompilationMode mode) 
{
    [Test]
    public void Eval_Number_ReturnsNumber()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("42"), Is.EqualTo(42));
    }

    [Test]
    public void Eval_String_ReturnsString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("\"hello\""), Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_Boolean_ReturnsBoolean()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("true"), Is.EqualTo(true));
        Assert.That(engine.Evaluate("false"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_Null_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("null"), Is.Null);
    }
}
