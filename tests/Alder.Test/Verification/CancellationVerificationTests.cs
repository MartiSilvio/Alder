using Alder.Test._Infrastructure;
using System.Reflection;

namespace Alder.Test.Verification;

/// <summary>
/// Verification: Section 5 — Cancellation Correctness.
/// Tests verify that CancellationToken is responsive across all APIs and execution paths,
/// including loops, compiled paths, and async evaluation.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class CancellationVerificationTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(Action<AlderOptions>? configure = null)
        => TestEngineFactory.Create(mode, configure);

    // --- Already-cancelled token on every API ---

    [Test]
    public void AlreadyCancelled_Evaluate_Throws()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate("return 1 + 1;", cancellationToken: cts.Token));
    }

    [Test]
    public void AlreadyCancelled_EvaluateGeneric_Throws()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate<int>("return 1 + 1;", cancellationToken: cts.Token));
    }

    [Test]
    public void AlreadyCancelled_TryEvaluate_Throws()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            engine.TryEvaluate("return 1;", out _, cancellationToken: cts.Token));
    }

    [Test]
    public void AlreadyCancelled_TryEvaluateGeneric_Throws()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            engine.TryEvaluate<int>("return 1;", out _, cancellationToken: cts.Token));
    }

    // --- Infinite loop cancellation ---

    // The while(true) loop should be interruptible via cancellation token.
    // CheckExecutionConstraints is called at each iteration, which checks the token.
    [Test]
    public void WhileTrueLoop_CancelsWithinBound()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate("while (true) { }", cancellationToken: cts.Token));
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000),
            "Cancellation should interrupt while(true) within a reasonable bound");
    }

    // For(;;) infinite loop
    [Test]
    public void InfiniteForLoop_CancelsWithinBound()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate("for (;;) { }", cancellationToken: cts.Token));
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000));
    }

    // Do-while infinite loop
    [Test]
    public void InfiniteDoWhileLoop_CancelsWithinBound()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate("do { } while (true)", cancellationToken: cts.Token));
    }

    // Large foreach
    [Test]
    public void LargeForEach_CancelsWithinBound()
    {
        var engine = CreateEngine();
        engine.SetVariable("items", Enumerable.Range(0, int.MaxValue));
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate("foreach (var x in items) { }", cancellationToken: cts.Token));
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000));
    }

    // Compiled LINQ lambdas must check cancellation.
    //
    // On the compiled path, .Where(x => x >= 0) compiles to a direct .NET delegate
    // that .NET iterates without re-entering the Alder evaluator. The cancellation
    // token is never checked — the LINQ chain runs to completion (or OOMs) regardless
    // of cancellation. This is a real bug: a compiled expression with a long LINQ chain
    // is uncancellable.
    //
    // Fix: Inject ct.ThrowIfCancellationRequested() into compiled lambda delegate bodies,
    // or wrap the compiled delegate to check the token on each invocation.
    [Test]
    [CancelAfter(5000)]
    public void LinqWithLambda_CompiledPath_ShouldRespectCancellation()
    {
        var engine = CreateEngine();
        // int.MaxValue elements via lazy Enumerable.Range — no upfront allocation,
        // but iteration takes minutes. Cancellation should interrupt within 200ms.
        engine.SetVariable("items", Enumerable.Range(0, int.MaxValue));
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            engine.Evaluate(
                "return items.Where(x => x >= 0).Count();",
                cancellationToken: cts.Token);
            sw.Stop();
            Assert.Fail($"Should have been cancelled but completed in {sw.ElapsedMilliseconds}ms");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(3000),
                "Cancellation should be responsive");
        }
        catch (AlderException ex) when (ex.InnerException is OperationCanceledException)
        {
            sw.Stop();
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(3000));
        }
    }

    // Compiled while(true) must remain cancellation-responsive.
    [Test]
    public void CompiledWhileTrue_CancelsWithinBound()
    {
        if (mode != CompilationMode.Compiled) return;

        var engine = CreateEngine();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate("while (true) { }", cancellationToken: cts.Token));
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000),
            "Compiled while(true) should emit cancellation checks in loop body");
    }

    // --- Async evaluation cancellation ---

    [Test]
    public async Task AsyncEvaluation_CancelsViaToken()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await engine.EvaluateAsync("while (true) { }", cancellationToken: cts.Token);
            Assert.Fail("Should have been cancelled");
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(2000));
    }

    // --- Cancellation must NOT be swallowed as AlderException ---

    [Test]
    public void Cancellation_NotWrappedInAlderException()
    {
        var engine = CreateEngine();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            engine.Evaluate("return 1;", cancellationToken: cts.Token);
            Assert.Fail("Should have thrown");
        }
        catch (OperationCanceledException)
        {
            Assert.Pass("Correctly throws OperationCanceledException, not AlderException");
        }
        catch (AlderException ex)
        {
            Assert.Fail($"Cancellation was wrapped in AlderException: {ex.ErrorCode} — {ex.Message}");
        }
    }

    [Test]
    public void CancelledEvaluation_DoesNotPersistTokenOnRootContext()
    {
        var engine = CreateEngine();
        var parsed = engine.Parse("while (true) { }");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            engine.Evaluate(parsed, cancellationToken: cts.Token));

        var contextField = typeof(AlderEngine).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(contextField, Is.Not.Null);
        var context = contextField!.GetValue(engine);
        Assert.That(context, Is.Not.Null);

        var tokenField = context!.GetType().GetField("ActiveCancellationToken", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(tokenField, Is.Not.Null);

        var persisted = (CancellationToken)tokenField!.GetValue(context)!;
        Assert.That(persisted, Is.EqualTo(default(CancellationToken)),
            "Root context must not retain per-evaluation cancellation tokens.");
    }

    [Test]
    public void ConcurrentEvaluations_WithDifferentTokens_AreIsolated()
    {
        var engine = CreateEngine();
        var canceledExpr = engine.Parse("while (true) { }");
        var successExpr = engine.Parse("return 42;");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var barrier = new Barrier(2);

        var canceledTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            Assert.Throws<OperationCanceledException>(() =>
                engine.Evaluate(canceledExpr, cancellationToken: cts.Token));
        });

        var successTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return engine.Evaluate<int>(successExpr);
        });

        Task.WaitAll(canceledTask, successTask);
        Assert.That(successTask.Result, Is.EqualTo(42));
    }

    [Test]
    public async Task ConcurrentEvaluateAsync_WithDifferentTokens_AreIsolated()
    {
        var engine = CreateEngine();
        var canceledExpr = engine.Parse("while (true) { }");
        var successExpr = engine.Parse("return await Task.FromResult(42);");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var barrier = new Barrier(2);

        var canceledTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            try
            {
                _ = await engine.EvaluateAsync(canceledExpr, cancellationToken: cts.Token);
                Assert.Fail("Cancelled async evaluation should throw");
            }
            catch (OperationCanceledException)
            {
                // expected
            }
        });

        var successTask = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await engine.EvaluateAsync<int>(successExpr);
        });

        await Task.WhenAll(canceledTask, successTask);
        Assert.That(successTask.Result, Is.EqualTo(42));
    }

    [Test]
    public void ChildCancelledEvaluation_DoesNotLeakTokenToParentOrChild()
    {
        var parent = CreateEngine();
        parent.SetVariable("x", 7);
        var child = parent.CreateChild();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            child.Evaluate("while (true) { }", cancellationToken: cts.Token));

        Assert.That(parent.Evaluate<int>("return x;"), Is.EqualTo(7));
        Assert.That(child.Evaluate<int>("return x;"), Is.EqualTo(7));
    }
}
