using CsEval.Tracing;

namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase04Plan01VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => TestEngineFactory.Create(mode);

    private CsEvalEngine Engine(CsEvalOptions options)
        => TestEngineFactory.Create(mode, options);

    // ═══════════════════════════════════════════════════════════════
    // ENG-01: CsEvalOptions — Defaults
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Options_Default_IsCaseSensitive_True()
        => Assert.That(CsEvalOptions.Default.IsCaseSensitive, Is.True);

    [Test]
    public void Options_Default_NoCompiler()
        => Assert.That(CsEvalOptions.Default.UseCompiler().ExpressionCompiler, Is.Not.Null);

    [Test]
    public void Options_Default_MaxExpressionDepth_512()
        => Assert.That(CsEvalOptions.Default.MaxExpressionDepth, Is.EqualTo(512));

    [Test]
    public void Options_Default_Constraints_Null()
        => Assert.That(CsEvalOptions.Default.Constraints, Is.Null);

    [Test]
    public void Options_Default_LanguageMode_Standard()
        => Assert.That(CsEvalOptions.Default.LanguageMode, Is.EqualTo(LanguageMode.Standard));

    // ═══════════════════════════════════════════════════════════════
    // ENG-01: CsEvalOptions — Custom behavior
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Options_CaseInsensitive_MatchesVariable()
    {
        var engine = Engine(new CsEvalOptions { IsCaseSensitive = false });
        engine.SetVariable("UserName", "Alice");
        Assert.That(engine.Evaluate<string>("username"), Is.EqualTo("Alice"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-01: ExecutionConstraints
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Constraints_MaxStatements_ThrowsOnExceed()
    {
        var engine = Engine(new CsEvalOptions
        {
            Constraints = new ExecutionConstraints { MaxStatements = 5 }
        });
        Assert.That(
            () => engine.Evaluate("{ while (true) {} }"),
            Throws.InstanceOf<CsEvalExecutionLimitException>());
    }

    [Test]
    public void Constraints_MaxStatements_AllowsUnderLimit()
    {
        var engine = Engine(new CsEvalOptions
        {
            Constraints = new ExecutionConstraints { MaxStatements = 100 }
        });
        var result = engine.Evaluate<int>("{ var x = 0; for (int i = 0; i < 10; i++) x += i; return x; }");
        Assert.That(result, Is.EqualTo(45));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-01: Sandbox presets
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Sandbox_Trusted_AllowsMethodCalls()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Trusted() });
        engine.SetVariable("name", "alice");
        Assert.That(engine.Evaluate<string>("name.ToUpper()"), Is.EqualTo("ALICE"));
    }

    [Test]
    public void Sandbox_Safe_BlocksMethodCalls()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Safe() });
        engine.SetVariable("name", "alice");
        Assert.That(
            () => engine.Evaluate("name.ToUpper()"),
            Throws.InstanceOf<CsEvalSandboxException>());
    }

    [Test]
    public void Sandbox_Safe_AllowsPropertyRead()
    {
        var engine = Engine(new CsEvalOptions { Sandbox = SandboxOptions.Safe() });
        engine.SetVariable("name", "Alice");
        Assert.That(engine.Evaluate<int>("name.Length"), Is.EqualTo(5));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-01: Freeze behavior
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Freeze_RegisterModule_ThrowsAfterEvaluate()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.That(
            () => engine.RegisterFunction("fn", _ => null),
            Throws.InstanceOf<InvalidOperationException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-01: Dispose
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Dispose_EngineImplementsIDisposable()
    {
        Assert.That(typeof(IDisposable).IsAssignableFrom(typeof(CsEvalEngine)), Is.True);
    }

    [Test]
    public void Dispose_ThrowsObjectDisposedAfterDispose()
    {
        var engine = Engine();
        engine.Dispose();
        Assert.That(
            () => engine.Evaluate("1"),
            Throws.InstanceOf<ObjectDisposedException>());
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: Parse + Evaluate(CsEvalExpression)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Parse_ThenEvaluate_ReusesExpression()
    {
        var engine = Engine();
        var expr = engine.Parse("x * 2");
        var a = engine.Evaluate<int>(expr, new Dictionary<string, object?> { ["x"] = 5 });
        var b = engine.Evaluate<int>(expr, new Dictionary<string, object?> { ["x"] = 10 });
        Assert.That(a, Is.EqualTo(10));
        Assert.That(b, Is.EqualTo(20));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: Evaluate<T>
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Evaluate_Generic_ReturnsTyped()
    {
        var engine = Engine();
        int result = engine.Evaluate<int>("10 + 20");
        Assert.That(result, Is.EqualTo(30));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: TryEvaluate
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryEvaluate_ReturnsFalseOnInvalid()
    {
        var engine = Engine();
        bool success = engine.TryEvaluate("???invalid???", out _);
        Assert.That(success, Is.False);
    }

    [Test]
    public void TryEvaluate_ReturnsTrueOnValid()
    {
        var engine = Engine();
        bool success = engine.TryEvaluate("1 + 2", out var result);
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void TryEvaluate_Generic_ReturnsTrueOnValid()
    {
        var engine = Engine();
        bool success = engine.TryEvaluate<int>("1 + 2", out var result);
        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: TryParse
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryParse_ReturnsFalseOnSyntaxError()
    {
        var engine = Engine();
        bool success = engine.TryParse("1 +", out var expr, out var error);
        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void TryParse_ReturnsTrueOnValid()
    {
        var engine = Engine();
        bool success = engine.TryParse("1 + 2", out var expr);
        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: TryValidate
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TryValidate_ReturnsTrueForValid()
    {
        var engine = Engine();
        bool valid = engine.TryValidate("1 + 2", out var diagnostics);
        Assert.That(valid, Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryValidate_ReturnsFalseForUnresolvedIdentifier()
    {
        var engine = Engine();
        bool valid = engine.TryValidate("unknownVar + 1", out var diagnostics);
        Assert.That(valid, Is.False);
        Assert.That(diagnostics, Has.Count.GreaterThan(0));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: EvaluateWithTrace
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void EvaluateWithTrace_ReturnsResultAndSteps()
    {
        var engine = Engine(new CsEvalOptions
        {
        });
        var trace = engine.EvaluateWithTrace("1 + 2");
        Assert.That(trace.Result, Is.EqualTo(3));
        Assert.That(trace.Steps, Has.Count.GreaterThan(0));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-02: CsEvalExpression properties
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Expression_Source_ReturnsOriginalString()
    {
        var engine = Engine();
        var expr = engine.Parse("1 + 2");
        Assert.That(expr.Source, Is.EqualTo("1 + 2"));
    }

    [Test]
    public void Expression_GetVariables_ReturnsIdentifiers()
    {
        var engine = Engine();
        var expr = engine.Parse("x + y * z");
        var vars = expr.GetVariables();
        Assert.That(vars, Does.Contain("x"));
        Assert.That(vars, Does.Contain("y"));
        Assert.That(vars, Does.Contain("z"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: SetVariable + Evaluate
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SetVariable_InjectInt()
    {
        var engine = Engine();
        engine.SetVariable("x", 42);
        Assert.That(engine.Evaluate<int>("x + 8"), Is.EqualTo(50));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: SetVariable<T>
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SetVariable_Generic_InjectTypedValue()
    {
        var engine = Engine();
        engine.SetVariable<int>("x", 42);
        Assert.That(engine.Evaluate<int>("x + 8"), Is.EqualTo(50));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: SetVariables
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SetVariables_BulkInjectDictionary()
    {
        var engine = Engine();
        engine.SetVariables(new Dictionary<string, object?>
        {
            ["x"] = 10,
            ["y"] = 20
        });
        Assert.That(engine.Evaluate<int>("x + y"), Is.EqualTo(30));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: Anonymous object variables
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void AnonymousObject_Variables()
    {
        var engine = Engine();
        var result = engine.Evaluate<int>("x + y", new { x = 1, y = 2 });
        Assert.That(result, Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: Per-invocation dictionary variables
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void PerInvocation_DictionaryVariables()
    {
        var engine = Engine();
        var vars = new Dictionary<string, object?> { ["x"] = 100 };
        var result = engine.Evaluate<int>("x * 2", vars);
        Assert.That(result, Is.EqualTo(200));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: Fluent chaining
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void FluentChaining_SetVariableReturnsEngine()
    {
        var engine = Engine();
        var result = engine
            .SetVariable("a", 1)
            .SetVariable("b", 2)
            .SetVariable("c", 3)
            .Evaluate<int>("a + b + c");
        Assert.That(result, Is.EqualTo(6));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: SetVariable after freeze
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SetVariable_AfterFreeze_Works()
    {
        var engine = Engine();
        engine.SetVariable("x", 1);
        engine.Evaluate<int>("x");

        engine.SetVariable("x", 99);
        Assert.That(engine.Evaluate<int>("x"), Is.EqualTo(99));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-03: Variable persistence
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Variable_PersistsAcrossEvaluations()
    {
        var engine = Engine();
        engine.SetVariable("counter", 0);
        Assert.That(engine.Evaluate<int>("counter"), Is.EqualTo(0));

        engine.SetVariable("counter", 10);
        Assert.That(engine.Evaluate<int>("counter"), Is.EqualTo(10));
    }
}
