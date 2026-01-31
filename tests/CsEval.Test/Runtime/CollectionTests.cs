
namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CollectionTests(CompilationMode mode) 
{
    [Test]
    public void Eval_ArrayLiteral()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1));
    }

    [Test]
    public void Eval_ArrayLiteral_Multiline()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"[
    ""one"",
    ""two"",
    ""three""
]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo("one"));
        Assert.That(result[1], Is.EqualTo("two"));
        Assert.That(result[2], Is.EqualTo("three"));
    }

    [Test]
    public void Eval_ArrayLiteral_CRLF()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[\r\n    \"one\"\r\n]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result![0], Is.EqualTo("one"));
    }

    [Test]
    public void Eval_AnonymousObject()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }
}
