using CsEval.Attributes;
using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class AsyncTests
{
    [Test]
    public async Task EvaluateAsync_SimpleExpression()
    {
        var engine = new CsEvalEngine();
        var result = await engine.EvaluateAsync("1 + 2");
        Assert.That(result, Is.EqualTo(3L));
    }

    [Test]
    public async Task EvaluateAsync_Generic()
    {
        var engine = new CsEvalEngine();
        var result = await engine.EvaluateAsync<long>("1 + 2");
        Assert.That(result, Is.EqualTo(3L));
    }

    [Test]
    public async Task EvaluateAsync_WithVariables()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("x", 10L);
        engine.SetVariable("y", 5L);

        var result = await engine.EvaluateAsync("x * y");
        Assert.That(result, Is.EqualTo(50L));
    }

    [Test]
    public void EvaluateAsync_Cancellation_Throws()
    {
        var engine = new CsEvalEngine();
        engine.SetVariable("items", Enumerable.Range(1, 1000000).Select(x => (object?)(long)x).ToList());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = Assert.CatchAsync<Exception>(async () =>
        {
            await engine.EvaluateAsync("items.Where((x) => x > 0).Select((x) => x * 2)",
                cancellationToken: cts.Token);
        });

        Assert.That(ex, Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Evaluate_WithCancellationToken_ThrowsOnCancellation()
    {
        var engine = new CsEvalEngine();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => { engine.Evaluate("1 + 2", null, cts.Token); });
    }

    [Test]
    public async Task EvaluateAsync_AsyncMethodOnProxy_AwaitsResult()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Async", new AsyncProxy());

        var result = await engine.EvaluateAsync("Async.GetValueAsync()");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public async Task EvaluateAsync_AsyncMethodWithArg_AwaitsResult()
    {
        var engine = new CsEvalEngine();
        engine.RegisterModule("Async", new AsyncProxy());

        var result = await engine.EvaluateAsync("Async.DelayedAdd(10, 5)");
        Assert.That(result, Is.EqualTo(15L));
    }

    [Test]
    public async Task EvaluateAsync_Module_WithAsyncMethod()
    {
        var engine = new CsEvalEngine();
        engine.RegisterFromType<AsyncModule>();

        var result = await engine.EvaluateAsync("AsyncOps.FetchValue()");
        Assert.That(result, Is.EqualTo(100L));
    }

    [Test]
    public void AsyncMethod_ReceivesCancellationToken()
    {
        var engine = new CsEvalEngine();
        var proxy = new CancellableProxy();
        engine.RegisterModule("Cancellable", proxy);

        var cts = new CancellationTokenSource();

        engine.Evaluate("Cancellable.DoWork()", null, cts.Token);

        Assert.That(proxy.ReceivedToken, Is.EqualTo(cts.Token));
    }

    private class AsyncProxy
    {
        public async Task<long> GetValueAsync()
        {
            await Task.Delay(1);
            return 42L;
        }

        public async Task<long> DelayedAdd(long a, long b)
        {
            await Task.Delay(1);
            return a + b;
        }
    }

    private class CancellableProxy
    {
        public CancellationToken ReceivedToken { get; private set; }

        public void DoWork(CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
        }
    }
}

[CsEvalModule("AsyncOps")]
public class AsyncModule
{
    public async Task<long> FetchValue()
    {
        await Task.Delay(1);
        return 100L;
    }
}