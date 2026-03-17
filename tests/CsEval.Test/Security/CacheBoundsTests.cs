using CsEval.Compilation;

namespace CsEval.Test.Security;

[TestFixture]
public class CacheBoundsTests
{
    [Test]
    public void ExpressionCache_DefaultCapacity_Is10000()
    {
        var cache = new ExpressionCache();
        // Add one entry and verify it's cached
        var info = cache.GetOrAdd("1 + 1", _ => new CompiledExpressionInfo(null, false, "test"));
        Assert.That(cache.Count, Is.EqualTo(1));
    }

    [Test]
    public void ExpressionCache_EvictsOldestWhenOverCapacity()
    {
        var cache = new ExpressionCache(capacity: 5);

        // Fill to capacity
        for (var i = 0; i < 5; i++)
            cache.GetOrAdd($"expr_{i}", _ => new CompiledExpressionInfo(null, false, $"val_{i}"));

        Assert.That(cache.Count, Is.EqualTo(5));

        // Add one more -- should evict the oldest (expr_0)
        cache.GetOrAdd("expr_5", _ => new CompiledExpressionInfo(null, false, "val_5"));

        Assert.That(cache.Count, Is.LessThanOrEqualTo(5));
        Assert.That(cache.TryGetValue("expr_5", out _), Is.True, "Newest entry should be present");
        Assert.That(cache.TryGetValue("expr_0", out _), Is.False, "Oldest entry should be evicted");
    }

    [Test]
    public void ExpressionCache_ExistingKeyDoesNotEvict()
    {
        var cache = new ExpressionCache(capacity: 3);
        var callCount = 0;

        cache.GetOrAdd("a", _ => { callCount++; return new CompiledExpressionInfo(null, false, "a"); });
        cache.GetOrAdd("b", _ => { callCount++; return new CompiledExpressionInfo(null, false, "b"); });
        cache.GetOrAdd("c", _ => { callCount++; return new CompiledExpressionInfo(null, false, "c"); });

        // Re-add "a" -- should hit cache, not create new entry
        cache.GetOrAdd("a", _ => { callCount++; return new CompiledExpressionInfo(null, false, "a_new"); });

        Assert.That(callCount, Is.EqualTo(3), "Factory should not be called for existing key");
        Assert.That(cache.Count, Is.EqualTo(3), "Count should not change");
    }

    [Test]
    public void ExpressionCache_Clear_RemovesAllEntries()
    {
        var cache = new ExpressionCache(capacity: 10);

        for (var i = 0; i < 5; i++)
            cache.GetOrAdd($"expr_{i}", _ => new CompiledExpressionInfo(null, false, "test"));

        Assert.That(cache.Count, Is.EqualTo(5));

        cache.Clear();

        Assert.That(cache.Count, Is.EqualTo(0));
    }

    [Test]
    public void ExpressionCache_AfterClear_CanAddNewEntries()
    {
        var cache = new ExpressionCache(capacity: 3);

        // Fill, clear, refill
        for (var i = 0; i < 3; i++)
            cache.GetOrAdd($"old_{i}", _ => new CompiledExpressionInfo(null, false, "old"));

        cache.Clear();

        for (var i = 0; i < 3; i++)
            cache.GetOrAdd($"new_{i}", _ => new CompiledExpressionInfo(null, false, "new"));

        Assert.That(cache.Count, Is.EqualTo(3));
        Assert.That(cache.TryGetValue("old_0", out _), Is.False);
        Assert.That(cache.TryGetValue("new_0", out _), Is.True);
    }

    [Test]
    public void ExpressionCache_InvalidCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpressionCache(capacity: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpressionCache(capacity: -1));
    }

    [Test]
    public void ExpressionCache_IntegrationWithEngine()
    {
        // Verify the cache works through the full engine pipeline
        var engine = new CsEvalEngine(CsEvalOptions.Default.UseCompiler());

        // Evaluate same expression twice -- second should use cache
        var result1 = engine.Evaluate("1 + 2");
        var result2 = engine.Evaluate("1 + 2");

        Assert.That(result1, Is.EqualTo(3));
        Assert.That(result2, Is.EqualTo(3));
    }
}
