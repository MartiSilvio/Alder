using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class GettingStartedDocTests(CompilationMode mode)
{
    [Test]
    public void SimpleExpression()
    {
        var engine = TestEngineFactory.Create(mode);

        int result = engine.Evaluate<int>("(5 + 3) * 2");

        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void SetVariable()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

        double avg = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");

        Assert.That(avg, Is.EqualTo(87.75));
    }

    [Test]
    public void AnonymousObject()
    {
        var engine = TestEngineFactory.Create(mode);

        bool eligible = engine.Evaluate<bool>(
            "age >= 18 && country != null",
            new { age = 25, country = "US" });

        Assert.That(eligible, Is.True);
    }

    [Test]
    public void ControlFlow()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("score", 82);

        string grade = engine.Evaluate<string>("""
            var letter = score switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
            var passed = score >= 60;
            return $"{letter} ({score}) — {(passed ? "Pass" : "Fail")}";
            """);

        Assert.That(grade, Is.EqualTo("B (82) — Pass"));
    }

    [Test]
    public void ParseReuse()
    {
        var engine = TestEngineFactory.Create(mode);

        AlderExpression expr = engine.Parse("price * (1 - discount)");

        engine.SetVariable<double>("price", 100.0);
        engine.SetVariable<double>("discount", 0.1);
        double result1 = engine.Evaluate<double>(expr);

        engine.SetVariable<double>("price", 250.0);
        double result2 = engine.Evaluate<double>(expr);

        Assert.That(result1, Is.EqualTo(90.0));
        Assert.That(result2, Is.EqualTo(225.0));
    }

    [Test]
    public void TryEvaluate()
    {
        var engine = TestEngineFactory.Create(mode);

        Assert.That(engine.TryEvaluate("items.Where(", out _), Is.False);
    }

    [Test]
    public void TryValidate()
    {
        var engine = TestEngineFactory.Create(mode);

        bool valid = engine.TryValidate("undefinedVar + 1", out var diagnostics);

        Assert.That(valid, Is.False);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
        Assert.That(diagnostics[0].FormattedCode, Is.EqualTo("CS0103"));
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

        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void Tracing()
    {
        var engine = TestEngineFactory.Create(mode);

        var trace = engine.EvaluateWithTrace("""
            var items = new[] { 1, 2, 3 };
            var squared = items.Select(x => x * x).ToList();
            return squared.Sum();
            """);

        Assert.That(trace.Result, Is.EqualTo(14));
        Assert.That(trace.Error, Is.Null);
        Assert.That(trace.Tree, Is.Not.Null);
    }

    [Test]
    public void ExtendedMode()
    {
        var engine = TestEngineFactory.Create(mode, o => o.LanguageMode = LanguageMode.Extended);

        Assert.That(engine.Evaluate<double>("2 ** 10"), Is.EqualTo(1024.0));

        var comprehension = engine.Evaluate("[x * x for x in Enumerable.Range(1, 10) if x % 2 == 0]");
        Assert.That(comprehension, Is.EqualTo(new[] { 4, 16, 36, 64, 100 }));

        Assert.That(engine.Evaluate<int>("5 |> (x => x * 2)"), Is.EqualTo(10));
    }
}
