using Alder.Security;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class GettingStartedDocTests(CompilationMode mode)
{
    [Test]
    public void LinqChain()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate("""
            new[] { "Alice", "Bob", "Charlie" }
                .Where(name => name.Length > 3)
                .Select(name => name.ToUpper())
                .ToList()
            """);

        Assert.That(result, Is.EqualTo(new List<string> { "ALICE", "CHARLIE" }));
    }

    [Test]
    public void SetVariable_TypedWithLinq()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

        var result = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");

        Assert.That(result, Is.EqualTo(87.75));
    }

    [Test]
    public void AnonymousObjectVariables()
    {
        var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<bool>(
            "age >= 18 && country != null",
            new { age = 25, country = "US" });

        Assert.That(result, Is.True);
    }

    [Test]
    public void ParseReuse()
    {
        var engine = TestEngineFactory.Create(mode);

        var expr = engine.Parse("price * (1 - discount)");

        engine.SetVariable<double>("price", 100.0);
        engine.SetVariable<double>("discount", 0.1);
        var result1 = engine.Evaluate<double>(expr);

        engine.SetVariable<double>("price", 250.0);
        var result2 = engine.Evaluate<double>(expr);

        Assert.That(result1, Is.EqualTo(90.0));
        Assert.That(result2, Is.EqualTo(225.0));
    }

    [Test]
    public void TryEvaluate_SyntaxError()
    {
        var engine = TestEngineFactory.Create(mode);

        Assert.That(engine.TryEvaluate("items.Where(", out _), Is.False);
    }

    [Test]
    public void Compiled()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);

        var result = engine.Evaluate<string>("""
            string.Join(", ", new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x))
            """);

        Assert.That(result, Is.EqualTo("1, 3, 4, 5"));
    }

    [Test]
    public void SecurityAndLimits()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.Sandbox = SandboxOptions.Safe();
            o.Constraints = new ExecutionConstraints
            {
                MaxStatements = 10_000,
                MaxLoopIterations = 1_000,
                MaxTimeout = TimeSpan.FromSeconds(5)
            };
        });

        // Safe sandbox allows property reads and arithmetic
        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }
}
