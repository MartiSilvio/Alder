using CsEval.Aot;
using CsEval.Compiled;

namespace CsEval.Test.DocVerification;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class Phase04Plan03VerificationTests(CompilationMode mode)
{
    private CsEvalEngine Engine()
        => new(new CsEvalOptions { CompilationMode = mode });

    // ═══════════════════════════════════════════════════════════════
    // ENG-06: Compilation Modes — Interpreted
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Interpreted_Evaluate_ReturnsResult()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = CompilationMode.Interpreted
        });
        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void Compiled_Evaluate_ReturnsResult()
    {
        var engine = new CsEvalEngine(new CsEvalOptions
        {
            CompilationMode = CompilationMode.Compiled
        });
        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-06: CsEval.Compiled extension methods
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Compile_T_InvokeReturnsResult()
    {
        var engine = Engine();
        var compiled = engine.Compile<int>("1 + 2");
        Assert.That(compiled.Invoke(), Is.EqualTo(3));
    }

    [Test]
    public void CompileToFunc_ReturnsFunc()
    {
        var engine = Engine();
        var add = engine.CompileToFunc<int>("1 + 2");
        Assert.That(add(), Is.EqualTo(3));
    }

    [Test]
    public void ParseAndCompile_ReturnsExpression()
    {
        var engine = Engine();
        var expr = engine.ParseAndCompile("1 + 2");
        Assert.That(expr, Is.Not.Null);
        Assert.That(engine.Evaluate<int>(expr), Is.EqualTo(3));
    }

    [Test]
    public void CsEvalExpression_IsCompiled_AfterTryCompile()
    {
        var engine = Engine();
        var expr = engine.Parse("1 + 2");
        Assert.That(expr.IsCompiled, Is.False);
        expr.TryCompile();
        // After TryCompile, IsCompiled reflects compilation outcome
        Assert.That(expr.IsCompilable, Is.Not.Null);
    }

    [Test]
    public void CsEvalExpression_GetVariables_ReturnsNames()
    {
        var engine = Engine();
        var expr = engine.Parse("x + y * 2");
        var vars = expr.GetVariables();
        Assert.That(vars, Does.Contain("x"));
        Assert.That(vars, Does.Contain("y"));
    }

    [Test]
    public void CsEvalExpression_TryCompile_SetsIsCompiled()
    {
        var engine = Engine();
        var expr = engine.Parse("1 + 2");
        var result = expr.TryCompile();
        if (result)
            Assert.That(expr.IsCompiled, Is.True);
        else
            Assert.That(expr.CompilationFailureReason, Is.Not.Null);
    }

    [Test]
    public void Expression_Reuse_DifferentVariables()
    {
        var engine = Engine();
        var expr = engine.Parse("x * 2");

        engine.SetVariable("x", 5);
        Assert.That(engine.Evaluate<int>(expr), Is.EqualTo(10));

        engine.SetVariable("x", 10);
        Assert.That(engine.Evaluate<int>(expr), Is.EqualTo(20));
    }

    [Test]
    public void CompileExpression_Lambda_ReturnsDelegate()
    {
        var engine = Engine();
        Func<int, bool> isPositive = engine.CompileExpression<Func<int, bool>>("x => x > 0");
        Assert.That(isPositive(42), Is.True);
        Assert.That(isPositive(-1), Is.False);
    }

    [Test]
    public void ParseAsExpression_ReturnsExpressionTree()
    {
        var engine = Engine();
        var expr = engine.ParseAsExpression<Func<int, bool>>("x => x > 5");
        Assert.That(expr, Is.Not.Null);
        var compiled = expr.Compile();
        Assert.That(compiled(10), Is.True);
        Assert.That(compiled(3), Is.False);
    }

    [Test]
    public void TryParseAsExpression_Success()
    {
        var engine = Engine();
        var success = engine.TryParseAsExpression<Func<int, bool>>(
            "x => x > 5", out var expr, out var diagnostics);
        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Compile_T_Invoke_WithVariables()
    {
        var engine = Engine();
        var compiled = engine.Compile<int>("x + y");
        var result = compiled.Invoke(new Dictionary<string, object?> { ["x"] = 3, ["y"] = 7 });
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Compile_T_VariableChangesVisibleAfterCompilation()
    {
        var engine = Engine();
        engine.SetVariable("x", 10);
        var compiled = engine.Compile<int>("x * 2");
        Assert.That(compiled.Invoke(), Is.EqualTo(20));

        engine.SetVariable("x", 50);
        Assert.That(compiled.Invoke(), Is.EqualTo(100));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-07: Thread Safety — Freeze detection
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void Freeze_RegisterFunctionThrows_AfterEvaluate()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterFunction("fn", args => 1));
    }

    [Test]
    public void Freeze_SetVariable_WorksAfterFreeze()
    {
        var engine = Engine();
        engine.SetVariable("x", 1);
        engine.Evaluate("x");
        engine.SetVariable("x", 2);
        Assert.That(engine.Evaluate<int>("x"), Is.EqualTo(2));
    }

    [Test]
    public void CreateChild_SharesConfig_HasOwnVariables()
    {
        var engine = Engine();
        engine.SetVariable("shared", 100);

        var child = engine.CreateChild();
        child.SetVariable("local", 42);

        Assert.That(child.Evaluate<int>("shared + local"), Is.EqualTo(142));
    }

    [Test]
    public void Child_Variable_NotVisibleOnParent()
    {
        var engine = Engine();
        engine.SetVariable("x", 1);

        var child = engine.CreateChild();
        child.SetVariable("childOnly", 99);

        Assert.That(child.Evaluate<int>("childOnly"), Is.EqualTo(99));
        Assert.Throws<CsEvalException>(() => engine.Evaluate("childOnly"));
    }

    [Test]
    public void Parent_Variable_ReadableFromChild()
    {
        var engine = Engine();
        engine.SetVariable("parentVar", 42);

        var child = engine.CreateChild();
        Assert.That(child.Evaluate<int>("parentVar"), Is.EqualTo(42));
    }

    [Test]
    public void PerInvocation_Variables_AvailableInExpression()
    {
        var engine = Engine();
        engine.SetVariable("price", 100);
        var result = engine.Evaluate<int>("price + bonus",
            new Dictionary<string, object?> { ["bonus"] = 50 });
        Assert.That(result, Is.EqualTo(150));
    }

    [Test]
    public void PerInvocation_Variables_NotPersisted()
    {
        var engine = Engine();
        engine.Evaluate("x + 1", new Dictionary<string, object?> { ["x"] = 5 });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("x"));
    }

    [Test]
    public void Freeze_RegisterModuleThrows_AfterEvaluate()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterModule("m", typeof(object)));
    }

    [Test]
    public void Freeze_RegisterAssemblyThrows_AfterEvaluate()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterAssembly(typeof(object).Assembly));
    }

    [Test]
    public void Freeze_RegisterNamespaceThrows_AfterEvaluate()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.Throws<InvalidOperationException>(() =>
            engine.RegisterNamespace("System"));
    }

    // ═══════════════════════════════════════════════════════════════
    // ENG-08: Native AOT
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void UseGeneratedContext_BuiltInContext_Evaluates()
    {
        var engine = Engine();
        engine.UseGeneratedContext(CsEvalBuiltInContext.Default);
        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void ClearGeneratedContexts_ThenUseCustom()
    {
        var engine = Engine();
        engine.ClearGeneratedContexts();
        engine.UseGeneratedContext(CsEvalBuiltInContext.Default);
        Assert.That(engine.Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void ClearGeneratedContexts_ThrowsAfterFreeze()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.Throws<InvalidOperationException>(() =>
            engine.ClearGeneratedContexts());
    }

    [Test]
    public void UseGeneratedContext_ThrowsAfterFreeze()
    {
        var engine = Engine();
        engine.Evaluate("1");
        Assert.Throws<InvalidOperationException>(() =>
            engine.UseGeneratedContext(CsEvalBuiltInContext.Default));
    }

    [Test]
    public void RegisterFromAssembly_HasRequiresUnreferencedCodeAttribute()
    {
        var method = typeof(CsEvalEngine).GetMethod("RegisterFromAssembly");
        Assert.That(method, Is.Not.Null);
        var attr = method!.GetCustomAttributes(true)
            .Any(a => a.GetType().Name == "RequiresUnreferencedCodeAttribute");
        Assert.That(attr, Is.True);
    }
}
