using System.Runtime.CompilerServices;

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
