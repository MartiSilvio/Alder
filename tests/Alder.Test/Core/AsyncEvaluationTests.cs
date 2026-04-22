using Alder.Diagnostics;

namespace Alder.Test.Core;

[TestFixture]
public class AsyncEvaluationTests
{
    private AlderEngine _engine = null!;

    [SetUp]
    public void Setup() => _engine = new AlderEngine();

    [TearDown]
    public void TearDown() => _engine.Dispose();

    [Test]
    public async Task Await_TaskFromResult_ReturnsValue()
    {
        var result = await _engine.EvaluateAsync("await Task.FromResult(42)");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task Await_TaskFromResult_String()
    {
        var result = await _engine.EvaluateAsync("""await Task.FromResult("hello")""");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public async Task Await_TaskFromResult_Null()
    {
        var result = await _engine.EvaluateAsync("await Task.FromResult<string?>(null)");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Await_TaskDelay_ReturnsNull()
    {
        var result = await _engine.EvaluateAsync("await Task.Delay(1)");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Await_InBlock_WithVariable()
    {
        var result = await _engine.EvaluateAsync(
            "var x = await Task.FromResult(5); x * 2");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public async Task Await_InConditional()
    {
        var result = await _engine.EvaluateAsync(
            "true ? await Task.FromResult(1) : await Task.FromResult(2)");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task Await_InIfStatement()
    {
        var result = await _engine.EvaluateAsync("""
            var x = await Task.FromResult(true);
            if (x) { return await Task.FromResult(42); }
            return 0;
        """);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task Await_InWhileLoop()
    {
        var result = await _engine.EvaluateAsync("""
            var sum = 0;
            var i = 0;
            while (i < 3)
            {
                sum += await Task.FromResult(1);
                i++;
            }
            sum
        """);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task Await_InForEachLoop()
    {
        var result = await _engine.EvaluateAsync("""
            var items = new[] { 1, 2, 3 };
            var sum = 0;
            foreach (var item in items)
            {
                sum += await Task.FromResult(item);
            }
            sum
        """);
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public async Task Await_NullCoalescing()
    {
        var result = await _engine.EvaluateAsync("""
            var left = await Task.FromResult<string?>(null);
            left ?? "fallback"
        """);
        Assert.That(result, Is.EqualTo("fallback"));
    }

    [Test]
    public async Task Await_Generic_Typed()
    {
        var result = await _engine.EvaluateAsync<int>("await Task.FromResult(42)");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task Await_WithVariables()
    {
        var task = Task.FromResult(99);
        var result = await _engine.EvaluateAsync(
            "await t",
            new Dictionary<string, object?> { ["t"] = task });
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public async Task EvaluateAsync_NonAsync_Expression_Works()
    {
        var result = await _engine.EvaluateAsync("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task EvaluateAsync_NonAsync_StringExpression_Works()
    {
        var result = await _engine.EvaluateAsync("\"hello\".ToUpper()");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Evaluate_Sync_WithAwait_Throws()
    {
        Assert.Throws<AlderException>(() =>
            _engine.Evaluate("await Task.FromResult(1)"));
    }

    [Test]
    public void Await_NotAwaitable_Throws()
    {
        var ex = Assert.ThrowsAsync<AlderException>(() =>
            _engine.EvaluateAsync("await 42").AsTask());
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS4001));
    }

    [Test]
    public async Task Await_ValueTask_FromResult()
    {
        var vt = ValueTask.FromResult(77);
        var result = await _engine.EvaluateAsync(
            "await vt",
            new Dictionary<string, object?> { ["vt"] = vt });
        Assert.That(result, Is.EqualTo(77));
    }

    [Test]
    public async Task AlderEval_EvaluateAsync()
    {
        AlderEval.Reset();
        var result = await AlderEval.GetEngine().EvaluateAsync("await Task.FromResult(100)");
        Assert.That(result, Is.EqualTo(100));
        AlderEval.Reset();
    }

    [Test]
    public async Task AlderEval_EvaluateAsync_PreParsed_WithPositionalVariables()
    {
        AlderEval.Reset();
        var engine = new AlderEngine();
        var expression = engine.Parse("await Task.FromResult(@0 + @1)");

        var result = await AlderEval.GetEngine().EvaluateAsync<int>(expression, 30, 12);

        Assert.That(result, Is.EqualTo(42));
        AlderEval.Reset();
    }

    [Test]
    public async Task EvaluateAsync_WithAnonymousObjectVariables_DoesNotMutateParentEngine()
    {
        _engine.SetVariable("baseValue", 10);

        var result = await _engine.EvaluateAsync<int>("baseValue + local", new { local = 5 });

        Assert.That(result, Is.EqualTo(15));
        Assert.That(_engine.Evaluate<int>("baseValue"), Is.EqualTo(10));
        Assert.Throws<AlderException>(() => _engine.Evaluate("local"));
    }

    [Test]
    public void Await_InLockBody_ThrowsCS1996()
    {
        var ex = Assert.Throws<AlderException>(() =>
            _engine.Evaluate("lock (new object()) { await Task.FromResult(1); }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1996));
    }

    [Test]
    public void Break_OutsideLoop_ThrowsCS0139()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ break; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0139));
    }

    [Test]
    public void Continue_OutsideLoop_ThrowsCS0139()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("{ continue; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0139));
    }

    [Test]
    public void Break_InsideSwitch_IsValid()
    {
        Assert.DoesNotThrow(() => _engine.Evaluate("""
            switch (1) { case 1: break; }
        """));
    }

    [Test]
    public void Continue_InsideSwitch_OutsideLoop_ThrowsCS0139()
    {
        var ex = Assert.Throws<AlderException>(() => _engine.Evaluate("""
            switch (1) { case 1: continue; }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0139));
    }

    [Test]
    public void Break_InsideLoop_IsValid()
    {
        Assert.DoesNotThrow(() => _engine.Evaluate("for (var i = 0; i < 5; i++) { break; }"));
    }

    [Test]
    public void Continue_InsideLoop_IsValid()
    {
        Assert.DoesNotThrow(() => _engine.Evaluate("for (var i = 0; ; i++) { break; }"));
    }

    [Test]
    public async Task Await_InLockExpression_IsValid()
    {
        var result = await _engine.EvaluateAsync("""
            var obj = await Task.FromResult(new object());
            lock (obj) { return 1; }
        """);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task AsyncLambda_PassedToMethod()
    {
        _engine.SetVariable("items", new[] { 1, 2, 3, 4, 5 });
        var result = await _engine.EvaluateAsync("""
            Func<int, Task<bool>> isEvenAsync = async n => await Task.FromResult(n % 2 == 0);
            var count = 0;
            foreach (var item in items)
            {
                if (await isEvenAsync(item)) count++;
            }
            return count;
            """);
        Assert.That(result, Is.EqualTo(2));
    }
}
