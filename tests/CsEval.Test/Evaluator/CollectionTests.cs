using NUnit.Framework;

namespace CsEval.Test.Evaluator;

[TestFixture]
public class CollectionTests : EvaluatorTestBase
{
    [Test]
    public void Eval_ArrayLiteral()
    {
        var result = Eval("[1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1L));
    }

    [Test]
    public void Eval_ArrayLiteral_Multiline()
    {
        var result = Eval(@"[
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
        var result = Eval("[\r\n    \"one\"\r\n]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result![0], Is.EqualTo("one"));
    }

    [Test]
    public void Eval_AnonymousObject()
    {
        var result = Eval("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30L));
    }
}
