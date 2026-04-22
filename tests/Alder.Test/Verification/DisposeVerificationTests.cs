using Alder.Test._Infrastructure;

namespace Alder.Test.Verification;

/// <summary>
/// Verification: Section 7 — Dispose Semantics.
/// Verifies that Dispose is safe to call multiple times, that all public APIs throw
/// ObjectDisposedException after disposal, and that parent/child disposal interactions
/// are correct.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class DisposeVerificationTests(CompilationMode mode)
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

    // SetVariable APIs must throw after disposal.
    [Test]
    public void AfterDispose_SetVariable_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.SetVariable("x", 42));
    }

    [Test]
    public void AfterDispose_SetVariableGeneric_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.SetVariable<int>("x", 42));
    }

    [Test]
    public void AfterDispose_SetVariables_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            engine.SetVariables(new Dictionary<string, object?> { ["x"] = 1 }));
    }

    // GetRegisteredModules must throw after disposal.
    [Test]
    public void AfterDispose_GetRegisteredModules_ThrowsObjectDisposed()
    {
        var engine = CreateEngine();
        engine.Dispose();
        Assert.Throws<ObjectDisposedException>(() => engine.GetRegisteredModules());
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
        Assert.Throws<ObjectDisposedException>(() => child.SetVariable("x", 1));

        parent.Dispose();
    }

}
