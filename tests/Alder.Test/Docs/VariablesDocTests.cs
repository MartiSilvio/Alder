using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class VariablesDocTests(CompilationMode mode)
{
    [Test]
    public void SetVariableTyped()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

        var result = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");

        Assert.That(result, Is.EqualTo(87.75));
    }

    [Test]
    public void FluentChaining()
    {
        var engine = TestEngineFactory.Create(mode);
        engine
            .SetVariable<double>("rate", 0.05)
            .SetVariable<int>("years", 10)
            .SetVariable<double>("principal", 1000.0);

        var result = engine.Evaluate<double>("principal * Math.Pow(1 + rate, years)");

        Assert.That(result, Is.EqualTo(1000.0 * Math.Pow(1.05, 10)).Within(0.001));
    }

    [Test]
    public void AnonymousObject()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<bool>(
            "age >= 18 && country != null",
            new { age = 25, country = "US" });

        Assert.That(result, Is.True);
    }

    [Test]
    public void Dictionary()
    {
        var engine = TestEngineFactory.Create(mode);

        var vars = new Dictionary<string, object?>
        {
            ["threshold"] = 100,
            ["multiplier"] = 1.5
        };
        var result = engine.Evaluate<double>("threshold * multiplier", vars);

        Assert.That(result, Is.EqualTo(150.0));
    }

    [Test]
    public void ChildEngines()
    {
        var parent = TestEngineFactory.Create(mode);
        parent.SetVariable<double>("baseFee", 50.0);

        var tenantA = parent.CreateChild();
        tenantA.SetVariable<double>("discount", 0.1);

        var tenantB = parent.CreateChild();
        tenantB.SetVariable<double>("discount", 0.25);

        Assert.That(tenantA.Evaluate<double>("baseFee * (1 - discount)"), Is.EqualTo(45.0));
        Assert.That(tenantB.Evaluate<double>("baseFee * (1 - discount)"), Is.EqualTo(37.5));
        Assert.That(parent.Evaluate<double>("baseFee"), Is.EqualTo(50.0));
    }

    [Test]
    public void ConcurrentChildEngines()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<double>("taxRate", 0.08);

        var amounts = Enumerable.Range(1, 50).Select(i => (double)i * 100).ToList();
        var results = new System.Collections.Concurrent.ConcurrentBag<double>();

        Parallel.ForEach(amounts, amount =>
        {
            var child = engine.CreateChild();
            child.SetVariable<double>("amount", amount);
            var tax = child.Evaluate<double>("amount * taxRate");
            results.Add(tax);
        });

        var expected = amounts.Select(a => a * 0.08).OrderBy(x => x).ToList();
        var actual = results.OrderBy(x => x).ToList();
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetVariables()
    {
        var engine = TestEngineFactory.Create(mode);

        var expr = engine.Parse("orders.Where(o => o.Total > minAmount).Count()");
        var vars = expr.GetVariables();

        Assert.That(vars, Does.Contain("orders"));
        Assert.That(vars, Does.Contain("minAmount"));
        Assert.That(vars, Does.Not.Contain("o"));
    }

    [Test]
    public void CaseInsensitive()
    {
        var engine = TestEngineFactory.Create(mode, o => o.IsCaseSensitive = false);
        engine.SetVariable<int>("count", 42);

        var result = engine.Evaluate<int>("COUNT");

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void PerCallVariables_DoNotPersist()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("x", 10);

        engine.Evaluate("x + y", new { y = 20 });

        // y should not exist in the engine after the call
        Assert.Throws<AlderException>(() => engine.Evaluate("y"));
    }

    [Test]
    public void TypeVersioning_RebindsOnTypeChange()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("x", 5);

        var expr = engine.Parse("x + 1");
        var result1 = engine.Evaluate<int>(expr);
        Assert.That(result1, Is.EqualTo(6));

        // Change value but not type — should not re-bind
        engine.SetVariable<int>("x", 10);
        var result2 = engine.Evaluate<int>(expr);
        Assert.That(result2, Is.EqualTo(11));
    }
}
