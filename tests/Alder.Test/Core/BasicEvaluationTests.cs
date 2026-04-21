using Alder.Diagnostics;

using Alder.Test._Infrastructure;

namespace Alder.Test.Core;

[TestFixture]
public class BasicEvaluationTests
{
    [Test]
    public void SimpleExpression()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WithVariable()
    {
        var engine = new AlderEngine();
        engine.SetVariable("x", 10);

        var result = engine.Evaluate("x * 2");
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void WithMultipleVariables()
    {
        var engine = new AlderEngine();
        engine.SetVariables(new Dictionary<string, object?>
        {
            ["a"] = 5,
            ["b"] = 3
        });

        var result = engine.Evaluate("a + b");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void FluentApi()
    {
        var result = new AlderEngine()
            .SetVariable("x", 10)
            .SetVariable("y", 5)
            .Evaluate("x - y");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Generic_ReturnsTypedResult()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate<long>("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Generic_ConvertsType()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate<double>("10");
        Assert.That(result, Is.EqualTo(10.0));
    }

    [Test]
    public void ComplexExpression()
    {
        var engine = new AlderEngine();
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("items.Where((x) => x > 2).Select((x) => x * 2).ToList()");
        Assert.That(result, Is.EquivalentTo(new List<int> { 6, 8, 10 }));
    }

    [Test]
    public void InterpolatedString()
    {
        var engine = new AlderEngine();
        engine.SetVariable("name", "Alder");

        var result = engine.Evaluate("""$"Hello, {name}!" """);
        Assert.That(result, Is.EqualTo("Hello, Alder!"));
    }

    [Test]
    public void AnonymousObject()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate("""new { Name = "Test", Value = 42 } """);

        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Name"), Is.EqualTo("Test"));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Value"), Is.EqualTo(42));
    }

    [Test]
    public void AnonymousObject_InferredMember_UsesMemberName()
    {
        var engine = new AlderEngine();
        engine.SetVariable("person", new { Name = "Ada", Age = 32 });

        var result = engine.Evaluate("""new { person.Name, person.Age } """);

        Assert.That(result, Is.Not.Null);
        Assert.That(TestHelpers.ReadProjectedMember(result, "Name"), Is.EqualTo("Ada"));
        Assert.That(TestHelpers.ReadProjectedMember(result, "Age"), Is.EqualTo(32));
    }

    [Test]
    public void AnonymousObject_DuplicateMember_Throws()
    {
        var engine = new AlderEngine();

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""new { Name = "Ada", Name = "Grace" } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0833));
    }

    [Test]
    public void AnonymousObject_NonNameableExpression_Throws()
    {
        var engine = new AlderEngine();

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""new { 1 + 2 } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8081));
    }

    [Test]
    public void Block()
    {
        var engine = new AlderEngine();
        var result = engine.Evaluate("{ var x = 10; var y = 20; return x + y; }");
        Assert.That(result, Is.EqualTo(30));
    }
}
