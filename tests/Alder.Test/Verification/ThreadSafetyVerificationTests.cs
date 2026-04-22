using System.Collections.Concurrent;

namespace Alder.Test.Verification;

/// <summary>
/// Verification: Section 1 — Thread Safety Under Concurrent Mutation.
/// CI assertions in this fixture cover only supported concurrency guarantees.
/// Exploratory shared-parent mutation probes are explicit and excluded from CI enforcement.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ThreadSafetyVerificationTests
{
    // Concurrent bound-cache updates on the same parsed expression must remain stable.
    [Test]
    public void ConcurrentEvaluate_SameParsedExpression_SameEngine_DoesNotThrow()
    {
        var engine = new AlderEngine();
        engine.SetVariable("x", 42L);
        engine.Evaluate("return x;");

        var parsed = engine.Parse("return x + 1;");
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, _ =>
        {
            try
            {
                var result = engine.Evaluate(parsed);
                Assert.That(result, Is.EqualTo(43L));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.That(exceptions, Is.Empty,
            () => $"Got {exceptions.Count} exceptions, first: {exceptions.First()}");
    }

    [Test]
    public void ConcurrentEvaluate_SameParsedExpression_DifferentChildContexts_IsStable()
    {
        var engine = new AlderEngine();
        var parsed = engine.Parse("return x + 1;");
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 500, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, i =>
        {
            try
            {
                var child = engine.CreateChild();
                child.SetVariable("x", i);
                var result = child.Evaluate<int>(parsed);
                Assert.That(result, Is.EqualTo(i + 1));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.That(exceptions, Is.Empty,
            () => $"Got {exceptions.Count} exceptions, first: {exceptions.First()}");
    }

    [Test]
    public void BoundCache_InvalidatesAndRebinds_WhenVariableTypeChanges()
    {
        var engine = new AlderEngine();
        var parsed = engine.Parse("return value.Length;");

        engine.SetVariable<string>("value", "abcd");
        Assert.That(engine.Evaluate<int>(parsed), Is.EqualTo(4));

        engine.SetVariable<int[]>("value", [1, 2, 3]);
        Assert.That(engine.Evaluate<int>(parsed), Is.EqualTo(3),
            "Type-version change must invalidate stale bound entries and rebind against new static types.");
    }

    // Dispose during Evaluate must not corrupt runtime state.
    [Test]
    public void Dispose_DuringEvaluate_DoesNotCorruptState()
    {
        var exceptions = new ConcurrentBag<Exception>();

        for (int trial = 0; trial < 100; trial++)
        {
            var engine = new AlderEngine();
            engine.SetVariable("items", Enumerable.Range(1, 10000).ToList());

            var evaluator = Task.Run(() =>
            {
                try
                {
                    engine.Evaluate("return items.Select(x => x * 2).Sum();");
                }
                catch (ObjectDisposedException) { }
                catch (AlderException) { }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            Thread.Sleep(1);
            engine.Dispose();

            evaluator.Wait();
        }

        Assert.That(exceptions, Is.Empty,
            () => $"Got unexpected exception types: {string.Join(", ", exceptions.Select(e => e.GetType().Name + ": " + e.Message))}");
    }

    // Concurrent CreateChild must preserve child isolation,
    // and the parent is unaffected.
    [Test]
    public void ConcurrentCreateChild_ChildIsolation()
    {
        var engine = new AlderEngine();
        engine.SetVariable("shared", 100L);

        var results = new ConcurrentBag<(int Id, long Result)>();
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 100, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, i =>
        {
            try
            {
                var child = engine.CreateChild();
                child.SetVariable("mine", (long)i);
                var result = child.Evaluate<long>("return shared + mine;");
                results.Add((i, result));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.That(exceptions, Is.Empty);

        foreach (var (id, result) in results)
        {
            Assert.That(result, Is.EqualTo(100L + id), $"Child {id} saw wrong result");
        }

        Assert.That(engine.Evaluate("return shared;"), Is.EqualTo(100L), "Parent should be unaffected");
    }

    // Variable type changes during compiled evaluation must fail safely.
    [Test]
    public void CompiledEvaluation_VariableTypeChange_DoesNotCrash()
    {
        var engine = new AlderEngine(o => o.UseCompiler());
        engine.SetVariable<int>("multiplier", 2);
        engine.SetVariable<int>("val", 5);

        var initial = engine.Evaluate<int>("return multiplier * val;");
        Assert.That(initial, Is.EqualTo(10));

        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new ManualResetEventSlim(false);

        var invoker = Task.Run(() =>
        {
            barrier.Wait();
            for (int i = 0; i < 500; i++)
            {
                try
                {
                    engine.Evaluate("return multiplier * val;");
                }
                catch (AlderException) { }
                catch (InvalidCastException) { }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        var mutator = Task.Run(() =>
        {
            barrier.Wait();
            engine.SetVariable<double>("multiplier", 2.5);
        });

        barrier.Set();
        Task.WaitAll(invoker, mutator);

        // After type change, compiled evaluation should either throw CompiledExpressionStale
        // or fall back to interpretation. It must NOT produce unhandled NRE or AccessViolation.
        Assert.That(exceptions, Is.Empty,
            () => $"Unexpected exception: {exceptions.FirstOrDefault()?.GetType().Name}: {exceptions.FirstOrDefault()?.Message}");
    }
}
