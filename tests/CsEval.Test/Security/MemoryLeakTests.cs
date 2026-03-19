using System.Runtime.CompilerServices;

namespace CsEval.Test.Security;

[TestFixture]
[Category("Memory")]
public class MemoryLeakTests
{
    [Test]
    [Retry(3)]
    public void DisposedEngine_ShouldBeCollectable()
    {
        var weakRef = CreateAndDisposeEngine();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(weakRef.IsAlive, Is.False, "Disposed engine should be garbage-collectable");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeEngine()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 42);
        engine.Evaluate("x + 1");
        engine.Dispose();
        return new WeakReference(engine);
    }

    [Test]
    [Retry(3)]
    public void RepeatedEvaluation_MemoryShouldPlateau()
    {
        using var engine = new CsEvalEngine();

        // Warm up
        engine.Evaluate("1 + 1");
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(true);

        for (var i = 0; i < 12_000; i++)
            engine.Evaluate($"{i} + 1");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(true);

        var growthMb = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);
        Assert.That(growthMb, Is.LessThan(50),
            $"Memory grew by {growthMb:F1}MB after 12K unique evals — expected plateau under 50MB");
    }

    [Test]
    [Retry(3)]
    public void LambdaDelegateConversion_ShouldNotRetainContext()
    {
        var weakRef = CreateEngineWithLambda();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.That(weakRef.IsAlive, Is.False,
            "Engine used for lambda delegate conversion should be collectable after disposal");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateEngineWithLambda()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new[] { 1, 2, 3 });
        engine.Evaluate("items.Select(x => x * 2).ToList()");
        engine.Dispose();
        return new WeakReference(engine);
    }

    [Test]
    [Retry(3)]
    public void MultipleEngineLifecycles_NoMemoryLeak()
    {
        // Warm up static caches
        using (var warmup = new CsEvalEngine())
            warmup.Evaluate("1 + 1");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(true);

        for (var i = 0; i < 100; i++)
        {
            using var engine = new CsEvalEngine();
            engine.SetVariable("val", i);
            engine.Evaluate("val + 1");
            engine.Evaluate($"{i} * 2");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(true);

        var growthMb = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);
        Assert.That(growthMb, Is.LessThan(20),
            $"Memory grew by {growthMb:F1}MB after 100 engine lifecycles — should be bounded");
    }

    [Test]
    [Retry(3)]
    public void ExtensionMethodCache_BoundedUnderDiverseTypes()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(true);

        var typeExpressions = new[]
        {
            ("new[] { 1, 2, 3 }", "int"),
            ("new[] { \"a\", \"b\", \"c\" }", "string"),
            ("new[] { 1.0, 2.0, 3.0 }", "double"),
            ("new[] { 1f, 2f, 3f }", "float"),
            ("new[] { 1L, 2L, 3L }", "long"),
            ("new[] { (short)1, (short)2, (short)3 }", "short"),
            ("new[] { (byte)1, (byte)2, (byte)3 }", "byte"),
            ("new[] { true, false, true }", "bool"),
            ("new[] { 'a', 'b', 'c' }", "char"),
            ("new[] { 1m, 2m, 3m }", "decimal"),
            ("new[] { (uint)1, (uint)2, (uint)3 }", "uint"),
            ("new[] { (ulong)1, (ulong)2, (ulong)3 }", "ulong"),
            ("new[] { (ushort)1, (ushort)2, (ushort)3 }", "ushort"),
            ("new[] { (sbyte)1, (sbyte)2, (sbyte)3 }", "sbyte"),
            ("new[] { 1, 2, 3 }.Select(x => x.ToString()).ToArray()", "projected-string"),
            ("new[] { 1.0, 2.0, 3.0 }.Select(x => (int)x).ToArray()", "projected-int"),
            ("new[] { \"hello\", \"world\" }.Select(x => x.Length).ToArray()", "projected-length"),
            ("new[] { 1, 2, 3 }.Select(x => (double)x).ToArray()", "projected-double"),
            ("new[] { 1, 2, 3 }.Where(x => x > 1).ToArray()", "filtered-int"),
            ("new[] { \"a\", \"bb\", \"ccc\" }.Where(x => x.Length > 1).ToArray()", "filtered-string"),
        };

        for (var i = 0; i < 5; i++)
        {
            foreach (var (expr, _) in typeExpressions)
            {
                using var engine = new CsEvalEngine();
                engine.SetVariable("data", new object());
                engine.Evaluate(expr);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(true);

        var growthMb = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);
        Assert.That(growthMb, Is.LessThan(30),
            $"Memory grew by {growthMb:F1}MB with 20 distinct type combinations — extension cache should be bounded");
    }
}
