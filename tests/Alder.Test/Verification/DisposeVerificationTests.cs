using Alder.Diagnostics;
using Alder.Test._Infrastructure;
using System.Reflection;

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

    // Compiled delegate invocation contract after engine dispose.
    //
    // The compiled delegate holds a closure over the engine's AlderContext.
    // After disposal, invoking the delegate may produce stale results, throw
    // ObjectDisposedException, or crash with NRE depending on what the delegate accesses.
    //
    // Fix: Either (a) the compiled delegate should check disposal and throw
    // ObjectDisposedException, or (b) document that compiled delegates are valid
    // independently of engine lifetime (fire-and-forget pattern).
    [Test]
    [Explicit("Known contract gap: compiled delegate behavior after engine disposal is not yet strict.")]
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
            Assert.Pass($"Current behavior: compiled delegate returned {result} after engine disposal.");
        }
        catch (ObjectDisposedException)
        {
            Assert.Pass("Current behavior: compiled delegate throws ObjectDisposedException after disposal.");
        }
        catch (AlderException ex) when (ex.ErrorCode == DiagnosticCode.ALDR0003)
        {
            Assert.Pass("Current behavior: compiled delegate throws CompiledExpressionStale after disposal.");
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

    [Test]
    public void RootEngine_UsesSingleTypeMetadataProviderInstance()
    {
        var engine = CreateEngine();

        var configField = typeof(AlderEngine).GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic);
        var localMetadataField = typeof(AlderEngine).GetField("_typeMetadata", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(configField, Is.Not.Null);
        Assert.That(localMetadataField, Is.Not.Null);

        var config = configField!.GetValue(engine);
        Assert.That(config, Is.Not.Null);

        var configMetadataProperty = config!.GetType().GetProperty("TypeMetadata", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(configMetadataProperty, Is.Not.Null);

        var configMetadata = configMetadataProperty!.GetValue(config);
        var localMetadata = localMetadataField!.GetValue(engine);

        Assert.That(localMetadata, Is.SameAs(configMetadata),
            "Engine-local metadata cache must be the same instance used by runtime config.");
    }

    [Test]
    public void RootDispose_ClearsSharedTypeMetadataCaches()
    {
        var engine = CreateEngine();
        engine.SetVariable("s", "hello");
        Assert.That(engine.Evaluate<int>("return s.Length;"), Is.EqualTo(5));

        var metadata = GetEngineTypeMetadataProvider(engine);
        var beforeDisposeCount = GetMetadataCacheEntryCount(metadata);
        Assert.That(beforeDisposeCount, Is.GreaterThan(0),
            "Evaluation should populate shared metadata caches before disposal.");

        engine.Dispose();

        var afterDisposeCount = GetMetadataCacheEntryCount(metadata);
        Assert.That(afterDisposeCount, Is.EqualTo(0),
            "Root dispose must clear shared metadata caches.");
    }

    private static object GetEngineTypeMetadataProvider(AlderEngine engine)
    {
        var metadataField = typeof(AlderEngine).GetField("_typeMetadata", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(metadataField, Is.Not.Null);
        var metadata = metadataField!.GetValue(engine);
        Assert.That(metadata, Is.Not.Null);
        return metadata!;
    }

    private static int GetMetadataCacheEntryCount(object metadataProvider)
    {
        var total = 0;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (var field in metadataProvider.GetType().GetFields(flags))
        {
            if (!field.FieldType.IsGenericType ||
                field.FieldType.GetGenericTypeDefinition() != typeof(System.Collections.Concurrent.ConcurrentDictionary<,>))
            {
                continue;
            }

            var value = field.GetValue(metadataProvider);
            var countProperty = value?.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);
            if (countProperty?.GetValue(value) is int count)
                total += count;
        }

        return total;
    }
}
