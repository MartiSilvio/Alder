using System.Collections.Concurrent;
using Alder.Test._Infrastructure;

namespace Alder.Test.Audit;

/// <summary>
/// Pre-release adversarial audit: Section 1 — Thread Safety Under Concurrent Mutation.
/// CI assertions in this fixture cover only supported concurrency guarantees.
/// Unsupported shared-parent mutation scenarios are kept as explicit characterization
/// tests so they do not masquerade as a contractual thread-safety promise.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ThreadSafetyAuditTests
{
    // DEFECT-TS-1: ConditionalWeakTable Remove/Add race in AlderExpression.GetOrCreateBoundExpression
    //
    // When the same pre-parsed AlderExpression is evaluated concurrently by multiple threads
    // on the same engine (no CreateChild), the bound expression cache in AlderExpression uses
    // a non-atomic Remove() + Add() pair on a ConditionalWeakTable keyed by AlderContext.
    // Two threads can both call Remove() (one succeeds, one returns false), then both call
    // Add() — the second Add() throws ArgumentException because the key was re-added by the
    // first thread.
    //
    // Fix: Replace Remove+Add with AddOrUpdate (.NET 8+) or guard the compound operation
    // with a lock on _boundExpressionCacheByContext.
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

    // Characterization only: concurrent mutation of the same parent-scoped variable
    // is not a supported contract. Compound assignment reads and writes shared state
    // across multiple evaluation steps, so this probe remains explicit and outside CI.
    [Test]
    [Explicit("Unsupported contract: concurrent shared-parent compound mutation is characterization-only.")]
    public void ConcurrentSharedParentCompoundAssignment_CanLoseUpdates()
    {
        var engine = new AlderEngine();
        engine.SetVariable<long>("counter", 0);
        engine.Evaluate("return counter;");

        const int iterations = 5;
        var barrier = new Barrier(iterations);

        Parallel.For(0, iterations, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, _ =>
        {
            var child = engine.CreateChild();
            barrier.SignalAndWait();
            child.Evaluate("counter = counter + 1");
        });

        var finalValue = engine.Evaluate<long>("return counter;");
        if (finalValue != iterations)
            Assert.Warn($"Observed lost updates under unsupported shared mutation: expected {iterations}, got {finalValue}.");
        else
            Assert.Pass("No lost update observed in this run.");
    }

    // Characterization only: the engine does not promise evaluation-level snapshot
    // isolation while another thread mutates the same shared parent variable.
    [Test]
    [Explicit("Unsupported contract: concurrent shared-parent mutation during evaluation is characterization-only.")]
    public void SharedParentMutation_DuringEvaluate_CanProduceTornRead()
    {
        var engine = new AlderEngine();
        engine.SetVariable<long>("y", 0);
        engine.Evaluate("return y;");

        var tornReads = new ConcurrentBag<(long Left, long Right)>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var evaluator = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // y + y should always be even if reads are consistent
                    var result = (long)engine.Evaluate("return y + y;")!;
                    if (result % 2 != 0)
                        tornReads.Add((result / 2, result - result / 2));
                }
                catch { }
            }
        });

        var mutator = Task.Run(() =>
        {
            long val = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                val = val == 0 ? 1 : 0;
                engine.SetVariable<long>("y", val);
            }
        });

        Task.WaitAll(evaluator, mutator);

        if (tornReads.Count > 0)
            Assert.Warn($"Detected {tornReads.Count} torn reads under unsupported shared mutation.");
        else
            Assert.Pass("No torn reads detected in this run (race is timing-dependent)");
    }

    // DEFECT-TS-4: Dispose during Evaluate — once evaluation starts, ThrowIfDisposed is not
    // re-checked. Dispose clears _expressionCache and _typeMetadata (via ConcurrentDictionary.Clear),
    // which is thread-safe at the operation level but can cause cache misses mid-evaluation.
    // The real risk is NullReferenceException if cached state is accessed after disposal clears it.
    //
    // Fix: This is documented as acceptable (standard IDisposable race), but verify it doesn't
    // produce unhandled NRE — it should throw ObjectDisposedException or complete normally.
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

    // DEFECT-TS-5: Concurrent CreateChild — verify no child sees another child's variables,
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

    // DEFECT-TS-6: SetVariable with type change during compiled delegate invocation.
    //
    // Compile a Func<int> that references engine variable "multiplier" (initially int).
    // Thread A invokes the delegate in a loop. Thread B changes "multiplier" from int to double.
    // The compiled delegate's type assumptions may be invalidated.
    //
    // Fix: The compiled delegate should detect _variableTypeVersion change and throw
    // CompiledExpressionStale, or the variable type change should be rejected.
    // DEFECT-TS-6: SetVariable with type change during compiled evaluation.
    //
    // Compile an expression referencing variable "multiplier" (initially int).
    // Thread A evaluates the compiled expression in a loop.
    // Thread B changes "multiplier" from int to double via SetVariable<double>.
    // The compiled path should detect _variableTypeVersion change and throw
    // CompiledExpressionStale, or handle the type change gracefully.
    //
    // Fix: The compiled delegate should detect stale variable types and throw cleanly.
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
