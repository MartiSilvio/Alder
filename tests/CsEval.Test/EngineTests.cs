using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class BasicEvaluationTests
{
    [Test]
    public void SimpleExpression()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WithVariable()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 10L);

        var result = engine.Evaluate("x * 2");
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void WithMultipleVariables()
    {
        var engine = new CsEvalEngine();
        engine.SetVariables(new Dictionary<string, object?>
        {
            ["a"] = 5L,
            ["b"] = 3L
        });

        var result = engine.Evaluate("a + b");
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void FluentApi()
    {
        var result = new CsEvalEngine()
            .SetVariable("x", 10L)
            .SetVariable("y", 5L)
            .Evaluate("x - y");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Generic_ReturnsTypedResult()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate<long>("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Generic_ConvertsType()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate<double>("10");
        Assert.That(result, Is.EqualTo(10.0));
    }

    [Test]
    public void ComplexExpression()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new List<object?> { 1L, 2L, 3L, 4L, 5L });

        var result = engine.Evaluate("items.Where((x) => x > 2).Select((x) => x * 2)") as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 6L, 8L, 10L }));
    }

    [Test]
    public void InterpolatedString()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("name", "CsEval");

        var result = engine.Evaluate("$\"Hello, {name}!\"");
        Assert.That(result, Is.EqualTo("Hello, CsEval!"));
    }

    [Test]
    public void AnonymousObject()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("new { Name = \"Test\", Value = 42 }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("Test"));
        Assert.That(result["Value"], Is.EqualTo(42));
    }

    [Test]
    public void Block()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 10; var y = 20; return x + y; }");
        Assert.That(result, Is.EqualTo(30));
    }
}

[TestFixture]
public class BuiltInProxyTests
{
    [Test]
    public void MathProxy()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void DateTimeProxy()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("DateTime.Now");
        Assert.That(result, Is.InstanceOf<DateTime>());
    }

    [Test]
    public void GuidProxy()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("Guid.NewGuid()");
        Assert.That(result, Is.InstanceOf<Guid>());
    }
}

[TestFixture]
public class CustomRegistrationTests
{
    [Test]
    public void CustomFunction()
    {
        var engine = new CsEvalEngine();
        engine.RegisterFunction("twice", args => Convert.ToInt64(args[0]) * 2);

        var result = engine.Evaluate("twice(5)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void CustomProxy()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Custom", new GreetingProxy());

        var result = engine.Evaluate("Custom.Greet(\"World\")");
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    private class GreetingProxy
    {
        public string Greet(string name) => $"Hello, {name}!";
    }
}

[TestFixture]
public class CaseSensitivityTests
{
    [Test]
    public void CaseSensitive_ThrowsOnWrongCase()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("MyVar", 42L);

        Assert.That(engine.Evaluate("MyVar"), Is.EqualTo(42));
        Assert.Throws<CsEval.Evaluation.EvalException>(() => engine.Evaluate("myvar"));
    }

    [Test]
    public void IgnoreCase_Variable()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });
        engine.SetVariable("MyVar", 42L);

        Assert.That(engine.Evaluate("MyVar"), Is.EqualTo(42));
        Assert.That(engine.Evaluate("myvar"), Is.EqualTo(42));
        Assert.That(engine.Evaluate("MYVAR"), Is.EqualTo(42));
    }

    [Test]
    public void IgnoreCase_MemberAccess()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });
        engine.SetVariable("obj", new TestObject { Name = "Test" });

        Assert.That(engine.Evaluate("obj.Name"), Is.EqualTo("Test"));
        Assert.That(engine.Evaluate("obj.name"), Is.EqualTo("Test"));
        Assert.That(engine.Evaluate("obj.NAME"), Is.EqualTo("Test"));
    }

    [Test]
    public void IgnoreCase_Proxy()
    {
        var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });

        Assert.That(engine.Evaluate("math.abs(-5)"), Is.EqualTo(5.0));
        Assert.That(engine.Evaluate("MATH.ABS(-5)"), Is.EqualTo(5.0));
    }

    private class TestObject
    {
        public string Name { get; set; } = "";
    }
}