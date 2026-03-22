namespace Alder.Test.Core;

[TestFixture]
public class AlderEvalTests
{
    [TearDown]
    public void TearDown()
    {
        AlderEval.Reset();
    }
    [Test]
    public void Evaluate_SimpleArithmetic()
    {
        Assert.That(AlderEval.Evaluate("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void Evaluate_Generic_ReturnsTypedResult()
    {
        Assert.That(AlderEval.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void Evaluate_StringExpression()
    {
        Assert.That(AlderEval.Evaluate<string>("""  "hello" + " " + "world"  """), Is.EqualTo("hello world"));
    }

    [Test]
    public void Evaluate_BooleanExpression()
    {
        Assert.That(AlderEval.Evaluate<bool>("3 > 2"), Is.True);
    }
    [Test]
    public void Evaluate_WithDictionaryVariables()
    {
        var vars = new Dictionary<string, object?> { ["x"] = 10, ["y"] = 20 };
        Assert.That(AlderEval.Evaluate<int>("x + y", vars), Is.EqualTo(30));
    }
    [Test]
    public void Evaluate_WithAnonymousObjectVariables()
    {
        Assert.That(AlderEval.Evaluate<int>("x * y", new { x = 5, y = 6 }), Is.EqualTo(30));
    }

    [Test]
    public void Evaluate_NonGeneric_WithAnonymousObject()
    {
        Assert.That(AlderEval.Evaluate("x + y", new { x = 3, y = 4 }), Is.EqualTo(7));
    }
    [Test]
    public void TryEvaluate_ValidExpression_ReturnsTrue()
    {
        Assert.That(AlderEval.TryEvaluate("1 + 1", out var result), Is.True);
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void TryEvaluate_InvalidExpression_ReturnsFalse()
    {
        Assert.That(AlderEval.TryEvaluate("???", out var result), Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryEvaluate_Generic_ValidExpression()
    {
        Assert.That(AlderEval.TryEvaluate<int>("2 + 3", out var result), Is.True);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void TryEvaluate_Generic_InvalidExpression()
    {
        Assert.That(AlderEval.TryEvaluate<int>("???", out var result), Is.False);
        Assert.That(result, Is.EqualTo(default(int)));
    }
    [Test]
    public void TryValidate_ValidExpression()
    {
        Assert.That(AlderEval.TryValidate("1 + 2", out var diagnostics), Is.True);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void TryValidate_InvalidExpression()
    {
        Assert.That(AlderEval.TryValidate("???", out var diagnostics), Is.False);
        Assert.That(diagnostics, Is.Not.Empty);
    }
    [Test]
    public void Configure_BeforeFirstUse_Succeeds()
    {
        AlderEval.Configure(o => o.LanguageMode = LanguageMode.Extended);
        Assert.That(AlderEval.Evaluate<int>("2 ** 3"), Is.EqualTo(8));
    }

    [Test]
    public void Configure_AfterEvaluation_Throws()
    {
        AlderEval.Evaluate("1");

        Assert.Throws<InvalidOperationException>(() =>
            AlderEval.Configure(o => o.LanguageMode = LanguageMode.Extended));
    }

    [Test]
    public void Configure_CalledTwice_Throws()
    {
        AlderEval.Configure(o => { });

        Assert.Throws<InvalidOperationException>(() =>
            AlderEval.Configure(o => { }));
    }

    [Test]
    public void Configure_NullCallback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AlderEval.Configure(null!));
    }
    [Test]
    public void Reset_AllowsReconfigure()
    {
        AlderEval.Configure(o => o.LanguageMode = LanguageMode.Extended);
        AlderEval.Evaluate("2 ** 3");

        AlderEval.Reset();

        AlderEval.Configure(o => o.LanguageMode = LanguageMode.Standard);
        Assert.That(AlderEval.Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void Reset_WithoutConfigure_WorksWithDefaults()
    {
        AlderEval.Reset();
        Assert.That(AlderEval.Evaluate<int>("1 + 1"), Is.EqualTo(2));
    }
    [Test]
    public void ConcurrentEvaluations_AreThreadSafe()
    {
        const int threadCount = 10;
        const int evalPerThread = 50;
        var exceptions = new List<Exception>();
        var barrier = new Barrier(threadCount);

        var threads = Enumerable.Range(0, threadCount).Select(t => new Thread(() =>
        {
            try
            {
                barrier.SignalAndWait();
                for (var i = 0; i < evalPerThread; i++)
                {
                    var result = AlderEval.Evaluate<int>("1 + 1");
                    if (result != 2)
                        throw new Exception($"Expected 2, got {result}");
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        })).ToList();

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join();

        Assert.That(exceptions, Is.Empty, () => string.Join("\n", exceptions.Select(e => e.Message)));
    }

    [Test]
    public void ConcurrentEvaluations_WithVariables_AreIsolated()
    {
        const int threadCount = 10;
        var exceptions = new List<Exception>();
        var barrier = new Barrier(threadCount);

        var threads = Enumerable.Range(0, threadCount).Select(t => new Thread(() =>
        {
            try
            {
                barrier.SignalAndWait();
                for (var i = 0; i < 20; i++)
                {
                    var expected = t * 100 + i;
                    var result = AlderEval.Evaluate<int>("a + b", new { a = t * 100, b = i });
                    if (result != expected)
                        throw new Exception($"Thread {t}: expected {expected}, got {result}");
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        })).ToList();

        foreach (var t in threads) t.Start();
        foreach (var t in threads) t.Join();

        Assert.That(exceptions, Is.Empty, () => string.Join("\n", exceptions.Select(e => e.Message)));
    }
    [Test]
    public void GlobalEngine_IndependentOfInstanceEngines()
    {
        var custom = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);
        Assert.That(custom.Evaluate<int>("2 ** 3"), Is.EqualTo(8));

        // Global uses Standard by default — ** is not valid
        Assert.Throws<AlderException>(() => AlderEval.Evaluate("2 ** 3"));
        Assert.That(AlderEval.Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }
}
