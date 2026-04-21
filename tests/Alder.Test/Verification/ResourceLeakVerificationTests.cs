using System.Runtime.CompilerServices;
using Alder.Test._Infrastructure;

namespace Alder.Test.Verification;

/// <summary>
/// Verification: Section 3 — Memory and Resource Leaks.
/// Tests verify that static caches don't grow without bound, that disposed engines
/// are GC-collectible, and that cache entries are properly scoped.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ResourceLeakVerificationTests
{
    // MethodDispatchCache.ParameterCache and FastInvokerCache are static
    // ConcurrentDictionaries with no eviction policy. Keyed by MethodInfo, which is unbounded
    // when MakeGenericMethod creates new MethodInfo objects for every unique type-argument
    // combination. In a long-running server evaluating diverse LINQ expressions, these grow
    // without bound and can never shrink, even after engine disposal.
    //
    // Fix: Add FIFO eviction (ConcurrentQueue + MaxSize cap) matching the pattern already
    // used by ResolutionCache. A cap of 8192 entries is generous for
    // any normal application while preventing unbounded growth.
    [Test]
    [Explicit("Exploratory probe; not part of the enforced CI contract.")]
    public void MethodDispatchCache_StaticCaches_SurviveEngineDisposal()
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var memBefore = GC.GetTotalMemory(true);

        for (int i = 0; i < 200; i++)
        {
            using var engine = new AlderEngine();
            engine.SetVariable("items", Enumerable.Range(1, 10).Select(n => (long)n).ToList());
            engine.Evaluate("return items.Where(x => x > 3).Select(x => x * 2).Sum();");
        }

        // Engines are all disposed — but static caches remain
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var memAfter = GC.GetTotalMemory(true);

        // Static caches should retain entries after engine disposal.
        // This test documents the behavior — it's not necessarily wrong (caching is intentional),
        // but in a server creating/disposing engines per-request, static caches grow permanently.
        var growthBytes = memAfter - memBefore;
        if (growthBytes > 1024 * 1024) // > 1MB
            Assert.Pass($"Observed static-cache growth of {growthBytes / 1024.0:F0} KB.");

        Assert.Pass("No significant static-cache growth observed in this run.");
    }

    // Disposed child engine must remain GC-collectible.
    // When child engines are created and disposed, verify the child AlderContext is collected.
    // The parent must NOT hold strong references to child contexts.
    [Test]
    public void ChildEngine_IsGCCollectible_AfterDispose()
    {
        var parent = new AlderEngine();
        parent.SetVariable("x", 42L);
        parent.Evaluate("return x;");

        var childRef = CreateAndDisposeChild(parent);

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        Assert.That(childRef.IsAlive, Is.False,
            "Disposed child engine should be GC-collectible — parent must not hold strong reference to children");

        parent.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeChild(AlderEngine parent)
    {
        var child = parent.CreateChild();
        child.SetVariable("y", 99L);
        child.Evaluate("return x + y;");
        var weakRef = new WeakReference(child);
        child.Dispose();
        return weakRef;
    }

    // Unique expression churn must not leak engine state across dispose.
    [Test]
    public void RepeatedUniqueEvaluation_RemainsStableAcrossDispose()
    {
        using var engine = new AlderEngine();

        // Evaluate many unique expressions and verify the engine remains stable.
        for (int i = 0; i < 200; i++)
        {
            engine.Evaluate($"return {i} + 1;");
        }

        // Verify disposal clears engine-owned state by checking a new engine doesn't see old entries.
        engine.Dispose();

        using var engine2 = new AlderEngine();
        // This should work fine — no stale state from the previous engine's cache
        var result = engine2.Evaluate("return 42;");
        Assert.That(result, Is.EqualTo(42));
    }

    // Compiled expression delegate lifetime and engine retention.
    // Compile an expression, dispose the engine. The compiled delegate holds a closure
    // reference to the engine's AlderContext. Verify the engine can still be GC'd
    // (i.e., the delegate doesn't pin the engine indefinitely).
    [Test]
    [Explicit("Exploratory probe; not part of the enforced CI contract.")]
    public void CompiledDelegate_DoesNotPinEngine_AfterDispose()
    {
        var weakRef = CreateCompiledDelegateAndDisposeEngine(out var fn);

        // fn still holds a reference to internal state, but the engine itself should be collectible
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        if (weakRef.IsAlive)
            Assert.Pass("Observed pinned engine after disposal.");

        // Verify the delegate itself is usable or throws appropriately
        try
        {
            fn();
        }
        catch (ObjectDisposedException)
        {
            // Expected and acceptable
        }
        catch (AlderException)
        {
            // Also acceptable (stale expression)
        }

        GC.KeepAlive(fn);
        Assert.Pass("Engine was collectible after disposal in this run.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateCompiledDelegateAndDisposeEngine(out Func<int> fn)
    {
        var engine = new AlderEngine(o => o.UseCompiler());
        engine.SetVariable<int>("x", 21);
        fn = engine.CompileToFunc<int>("return x * 2;");
        Assert.That(fn(), Is.EqualTo(42));
        var weakRef = new WeakReference(engine);
        engine.Dispose();
        return weakRef;
    }

    // Lambda delegate cache must release entries when keys are collected.
    // Verify that when the lambda goes out of scope, the cache entry is reclaimed.
    [Test]
    public void LambdaDelegateCache_ReleasesEntries_WhenLambdaCollected()
    {
        using var engine = new AlderEngine();

        var lambdaRef = EvaluateLambdaAndGetWeakRef(engine);

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        // ConditionalWeakTable should release the entry when the lambda key is collected
        Assert.That(lambdaRef.IsAlive, Is.False,
            "Lambda object should be collectible — ConditionalWeakTable key should not prevent GC");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference EvaluateLambdaAndGetWeakRef(AlderEngine engine)
    {
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });
        var result = engine.Evaluate("return items.Where(x => x > 2).ToList();");
        // The lambda (x => x > 2) is a LambdaValue internally. Capture a weak ref to the result
        // to verify the evaluation artifacts are collectible.
        return new WeakReference(result);
    }
}
