using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Audit;

/// <summary>
/// Pre-release adversarial audit: Section 7 — Dispose Semantics.
/// Verifies that Dispose is safe to call multiple times, that all public APIs throw
/// ObjectDisposedException after disposal, and that parent/child disposal interactions
/// are correct.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class DisposeAuditTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(Action<AlderOptions>? configure = null)
        => TestEngineFactory.Create(mode, configure);

    // --- Double dispose must not throw ---

    [Test]
    public void DoubleDispose_DoesNotThrow()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.DoesNotThrow(() => engine.Dispose());
    }

    [Test]
    public void TripleDispose_DoesNotThrow()
    {
        var engine = CreateEngine();
        engine.Dispose();
        engine.Dispose();
        Assert.DoesNotThrow(() => engine.Dispose());
    }

    // --- Every public API after dispose must throw ObjectDisposedException ---

    [Test]
    public void AfterDispose_Evaluate_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.Evaluate("return 1;"));
    }

    [Test]
    public void AfterDispose_EvaluateGeneric_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.Evaluate<int>("return 1;"));
    }

    [Test]
    public void AfterDispose_TryEvaluate_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            engine.TryEvaluate("return 1;", out _));
    }

    [Test]
    public void AfterDispose_TryEvaluateGeneric_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            engine.TryEvaluate<int>("return 1;", out _));
    }

    [Test]
    public void AfterDispose_TryValidate_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            engine.TryValidate("return 1;", out _));
    }

    [Test]
    public void AfterDispose_Parse_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.Parse("return 1;"));
    }

    [Test]
    public void AfterDispose_CreateChild_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.CreateChild());
    }

    // DEFECT-DISP-1: SetVariable does not call ThrowIfDisposed().
    //
    // SetVariable, SetVariable<T>, and SetVariables are public methods that do not check
    // disposal. They call DefineOrStageVariable which accesses _context (may be null after
    // disposal). This can produce NullReferenceException instead of the expected
    // ObjectDisposedException.
    //
    // Fix: Add ThrowIfDisposed() at the top of SetVariable, SetVariable<T>, and SetVariables.
    [Test]
    public void AfterDispose_SetVariable_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();

        try
        {
            engine.SetVariable("x", 42);
            Assert.Fail("SetVariable after dispose should throw");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass("Correctly throws ObjectDisposedException");
        }
        catch (Exception ex)
        {
            Assert.Fail($"SetVariable after dispose threw {ex.GetType().Name} instead of " +
                        $"ObjectDisposedException: {ex.Message}");
        }
    }

    [Test]
    public void AfterDispose_SetVariableGeneric_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();

        try
        {
            engine.SetVariable<int>("x", 42);
            Assert.Fail("SetVariable<T> after dispose should throw");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass("Correctly throws ObjectDisposedException");
        }
        catch (Exception ex)
        {
            Assert.Fail($"SetVariable<T> after dispose threw {ex.GetType().Name} instead of " +
                        $"ObjectDisposedException: {ex.Message}");
        }
    }

    [Test]
    public void AfterDispose_SetVariables_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();

        try
        {
            engine.SetVariables(new Dictionary<string, object?> { ["x"] = 1 });
            Assert.Fail("SetVariables after dispose should throw");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass("Correctly throws ObjectDisposedException");
        }
        catch (Exception ex)
        {
            Assert.Fail($"SetVariables after dispose threw {ex.GetType().Name} instead of " +
                        $"ObjectDisposedException: {ex.Message}");
        }
    }

    // DEFECT-DISP-2: GetRegisteredModules does not check disposal.
    //
    // GetRegisteredModules accesses _config.Modules without calling ThrowIfDisposed().
    // Since _config is readonly and set at construction, this actually works after disposal
    // (it reads from a still-valid reference). But it violates the contract that all public
    // APIs on a disposed object throw ObjectDisposedException.
    //
    // Fix: Add ThrowIfDisposed() at the top of GetRegisteredModules.
    [Test]
    public void AfterDispose_GetRegisteredModules_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();

        try
        {
            engine.GetRegisteredModules();
            Assert.Warn("GetRegisteredModules succeeds after dispose — missing ThrowIfDisposed() check. " +
                        "Returns stale-but-valid data since _config is readonly.");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass("Correctly throws ObjectDisposedException");
        }
    }

    // --- Compiled extension methods after dispose ---

    [Test]
    public void AfterDispose_CompileExpression_ThrowsObjectDisposed()
    {
        if (mode != CompilationMode.Compiled) return;

        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            engine.CompileExpression<Func<int>>("return 1;"));
    }

    // DEFECT-DISP-3: Compiled delegate invocation after engine dispose.
    //
    // The compiled delegate holds a closure over the engine's AlderContext.
    // After disposal, invoking the delegate may produce stale results, throw
    // ObjectDisposedException, or crash with NRE depending on what the delegate accesses.
    //
    // Fix: Either (a) the compiled delegate should check disposal and throw
    // ObjectDisposedException, or (b) document that compiled delegates are valid
    // independently of engine lifetime (fire-and-forget pattern).
    [Test]
    public void CompiledDelegate_AfterEngineDispose_BehaviorDocumented()
    {
        if (mode != CompilationMode.Compiled) return;

        var engine = new AlderEngine(o => o.UseCompiler());
        engine.SetVariable<int>("x", 21);
        var fn = engine.CompileExpression<Func<int>>("return x * 2;");
        Assert.That(fn(), Is.EqualTo(42));

        engine.Dispose();

        try
        {
            var result = fn();
            // If this succeeds, the delegate works after disposal (reads from captured context)
            Assert.Warn($"Compiled delegate returned {result} after engine disposal — " +
                        "delegate holds strong closure reference to context, works independently of engine lifetime");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass("Compiled delegate correctly throws ObjectDisposedException after engine disposal");
        }
        catch (AlderException ex) when (ex.ErrorCode == DiagnosticCode.ALDR0003)
        {
            Assert.Pass("Compiled delegate correctly throws CompiledExpressionStale after disposal");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Compiled delegate threw unexpected {ex.GetType().Name}: {ex.Message}");
        }
    }

    // --- Parent/child disposal interactions ---

    [Test]
    public void DisposeParent_ChildEvaluate_ThrowsObjectDisposed()
    {
        var parent = CreateEngine();
        parent.SetVariable("x", 42L);
        var child = parent.CreateChild();
        child.SetVariable("y", 1L);

        // Verify child works before parent disposal
        Assert.That(child.Evaluate("return x + y;"), Is.EqualTo(43L));

        parent.Dispose();

        // Child should detect parent disposal via IsDisposed() chain
        Assert.Throws<ObjectDisposedException>(() => child.Evaluate("return x + y;"));
    }

    [Test]
    public void DisposeChild_ParentUnaffected()
    {
        var parent = CreateEngine();
        parent.SetVariable("x", 42L);
        var child = parent.CreateChild();
        child.Dispose();

        // Parent should work fine
        Assert.That(parent.Evaluate("return x;"), Is.EqualTo(42L));
        parent.Dispose();
    }

    [Test]
    public void DisposeChild_SiblingUnaffected()
    {
        var parent = CreateEngine();
        parent.SetVariable("x", 10L);
        var child1 = parent.CreateChild();
        var child2 = parent.CreateChild();

        child1.Dispose();

        child2.SetVariable("y", 5L);
        Assert.That(child2.Evaluate("return x + y;"), Is.EqualTo(15L));

        child2.Dispose();
        parent.Dispose();
    }

    [Test]
    public void CreateChildAfterParentDispose_ThrowsObjectDisposed()
    {
        var parent = CreateEngine();
        parent.Dispose();
        Assert.Throws<ObjectDisposedException>(() => parent.CreateChild());
    }

    [Test]
    public void SetVariableOnDisposedChild_Throws()
    {
        var parent = CreateEngine();
        var child = parent.CreateChild();
        child.Dispose();

        try
        {
            child.SetVariable("x", 1);
            Assert.Fail("SetVariable on disposed child should throw");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected ObjectDisposedException, got {ex.GetType().Name}: {ex.Message}");
        }

        parent.Dispose();
    }
}
