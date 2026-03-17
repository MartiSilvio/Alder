using CsEval.Tracing;

namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase05Plan03VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private CsEvalEngine Engine(CsEvalOptions options)
        => TestEngineFactory.Create(mode, options);

    // ═══════════════════════════════════════════════════════════════
    // Pipeline behavior
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Parse_ReturnsCsEvalExpression()
    {
        var engine = Engine();
        var expr = engine.Parse("1 + 2");
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr, Is.InstanceOf<CsEvalExpression>());
        Assert.That(expr.Source, Is.EqualTo("1 + 2"));
    }

    [Test]
    public void Evaluate_WithParsedExpression_ProducesResult()
    {
        var engine = Engine();
        var expr = engine.Parse("2 + 3");
        var result = engine.Evaluate<int>(expr);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void TryValidate_DetectsErrors_WithoutExecuting()
    {
        var engine = Engine();
        bool valid = engine.TryValidate("undeclaredVar + 1", out var diagnostics);
        Assert.That(valid, Is.False);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
    }

    [Test]
    public void TryValidate_AcceptsValid_Expression()
    {
        var engine = Engine();
        bool valid = engine.TryValidate("1 + 2", out var diagnostics);
        Assert.That(valid, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    // ═══════════════════════════════════════════════════════════════
    // Execution backends
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Interpreted_Engine_Evaluates_Correctly()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Interpreted);
        Assert.That(engine.Evaluate<int>("3 * 7"), Is.EqualTo(21));
    }

    [Test]
    public void Compiled_Engine_Evaluates_Correctly()
    {
        var engine = TestEngineFactory.Create(CompilationMode.Compiled);
        Assert.That(engine.Evaluate<int>("3 * 7"), Is.EqualTo(21));
    }

    [Test]
    public void EvaluateWithTrace_Returns_Steps()
    {
        var engine = Engine();
        var trace = engine.EvaluateWithTrace("1 + 2");
        Assert.That(trace, Is.InstanceOf<EvaluationTraceResult>());
        Assert.That(trace.Result, Is.EqualTo(3));
        Assert.That(trace.Steps, Has.Count.GreaterThan(0));
    }

    // ═══════════════════════════════════════════════════════════════
    // Engine lifecycle
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BeforeFirstEvaluate_RegisterFunction_Works()
    {
        var engine = Engine();
        Assert.DoesNotThrow(() =>
            engine.RegisterFunction("myFn", args => 42));
    }

    [Test]
    public void AfterFirstEvaluate_RegisterFunction_ThrowsInvalidOperation()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.That(
            () => engine.RegisterFunction("myFn", args => 42),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void AfterFirstEvaluate_SetVariable_StillWorks()
    {
        var engine = Engine();
        engine.SetVariable("x", 1);
        engine.Evaluate<int>("x");

        engine.SetVariable("x", 99);
        Assert.That(engine.Evaluate<int>("x"), Is.EqualTo(99));
    }

    [Test]
    public void AfterFirstEvaluate_SubsequentEvaluations_Work()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.That(engine.Evaluate<int>("2 + 3"), Is.EqualTo(5));
        Assert.That(engine.Evaluate<int>("10 * 2"), Is.EqualTo(20));
    }

    [Test]
    public void Dispose_ThrowsObjectDisposed()
    {
        var engine = Engine();
        engine.Dispose();
        Assert.That(
            () => engine.Evaluate("1"),
            Throws.InstanceOf<ObjectDisposedException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // Child engines
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CreateChild_ReturnsNewEngine()
    {
        var engine = Engine();
        engine.SetVariable("x", 1);
        engine.Evaluate("x");
        var child = engine.CreateChild();
        Assert.That(child, Is.Not.Null);
        Assert.That(child, Is.Not.SameAs(engine));
    }

    [Test]
    public void ChildEngine_CanEvaluate()
    {
        var engine = Engine();
        engine.Evaluate("1");
        var child = engine.CreateChild();
        Assert.That(child.Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void ChildEngine_SeesParentFunctions()
    {
        var engine = Engine();
        engine.RegisterFunction("twice", args => (int)args[0]! * 2);
        engine.Evaluate("1");
        var child = engine.CreateChild();
        Assert.That(child.Evaluate<int>("twice(5)"), Is.EqualTo(10));
    }

    [Test]
    public void ChildEngine_Variables_DontLeakToParent()
    {
        var engine = Engine();
        engine.SetVariable("x", 10);
        engine.Evaluate("x");

        var child = engine.CreateChild();
        child.SetVariable("childOnly", 999);
        child.Evaluate<int>("childOnly");

        bool parentHasChildVar = engine.TryEvaluate("childOnly", out _);
        Assert.That(parentHasChildVar, Is.False);
    }

    // ═══════════════════════════════════════════════════════════════
    // Expression caching
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Parse_SameExpression_ReturnsSameSource()
    {
        var engine = Engine();
        var expr1 = engine.Parse("1 + 2");
        var expr2 = engine.Parse("1 + 2");
        Assert.That(expr1.Source, Is.EqualTo(expr2.Source));
    }

    [Test]
    public void Evaluate_SameExpression_ProducesConsistentResults()
    {
        var engine = Engine();
        var r1 = engine.Evaluate<int>("5 + 5");
        var r2 = engine.Evaluate<int>("5 + 5");
        var r3 = engine.Evaluate<int>("5 + 5");
        Assert.That(r1, Is.EqualTo(10));
        Assert.That(r2, Is.EqualTo(10));
        Assert.That(r3, Is.EqualTo(10));
    }
}
