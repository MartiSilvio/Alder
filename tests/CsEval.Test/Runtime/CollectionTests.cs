using System.Collections;

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
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<int[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list[0], Is.EqualTo(1));
    }

    [Test]
    public void Eval_ArrayLiteral_TypeInference_Strings()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[\"a\", \"b\", \"c\"]");
        Assert.That(result, Is.TypeOf<string[]>());
    }

    [Test]
    public void Eval_ArrayLiteral_MixedTypes_FallbackToObjectList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, \"two\", 3.0]");
        Assert.That(result, Is.TypeOf<object?[]>());
    }

    [Test]
    public void Eval_ArrayLiteral_WithNulls_ValueType_CreatesNullableList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, null, 3]");
        Assert.That(result, Is.TypeOf<int?[]>());
    }

    [Test]
    public void Eval_ArrayLiteral_WithNulls_ReferenceType_TypedList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[\"a\", null, \"c\"]");
        Assert.That(result, Is.TypeOf<string[]>());
    }

    [Test]
    public void Eval_ArrayLiteral_Empty_ReturnsObjectList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[]");
        Assert.That(result, Is.TypeOf<object?[]>());
    }

    [Test]
    public void Eval_ArrayLiteral_Multiline()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"[
    ""one"",
    ""two"",
    ""three""
]");
        Assert.That(result, Is.TypeOf<string[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list[0], Is.EqualTo("one"));
        Assert.That(list[1], Is.EqualTo("two"));
        Assert.That(list[2], Is.EqualTo("three"));
    }

    [Test]
    public void Eval_ArrayLiteral_CRLF()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[\r\n    \"one\"\r\n]");
        Assert.That(result, Is.TypeOf<string[]>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0], Is.EqualTo("one"));
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
