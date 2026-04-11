using Alder.Test._Infrastructure;

namespace Alder.Test.Audit;

/// <summary>
/// Pre-release adversarial audit: Section 5 — Cancellation Correctness.
/// Tests verify that CancellationToken is responsive across all APIs and execution paths,
/// including loops, compiled paths, and async evaluation.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class CancellationAuditTests(CompilationMode mode)
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

    // DEFECT-CANCEL-1: Compiled LINQ lambdas don't check cancellation.
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
    [Timeout(5000)]
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

    // --- Compiled path loop cancellation ---

    // DEFECT-CANCEL-2: Compiled path loop cancellation.
    //
    // The compiled ForEmitter and DoWhileEmitter rely on BlockEmitter.EmitLoopIterationBody
    // to emit constraint checks. ForEachEmitter explicitly emits CheckExecutionConstraints.
    // Verify the compiled while(true) loop is actually cancellable — if the emitter doesn't
    // emit the check, the compiled loop runs forever.
    //
    // Fix: Ensure all compiled loop emitters emit CheckExecutionConstraints with the
    // CancellationTokenParam in the loop body.
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
}
