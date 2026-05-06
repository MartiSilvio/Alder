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
    public void GetEngine_DefaultGlobalEngine_Evaluates()
    {
        var engine = AlderEval.GetEngine();

        Assert.That(engine.Evaluate("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public void GetEngine_ReturnsSameGlobalInstance()
    {
        var first = AlderEval.GetEngine();
        var second = AlderEval.GetEngine();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void Configure_BeforeFirstUse_AffectsGlobalEngine()
    {
        AlderEval.Configure(o => o.LanguageMode = LanguageMode.Extended);

        var engine = AlderEval.GetEngine();

        Assert.That(engine.Evaluate<int>("2 ** 3"), Is.EqualTo(8));
    }

    [Test]
    public void Configure_AfterGlobalEngineCreation_Throws()
    {
        _ = AlderEval.GetEngine();

        Assert.Throws<InvalidOperationException>(() =>
            AlderEval.Configure(o => o.LanguageMode = LanguageMode.Extended));
    }

    [Test]
    public void Configure_CalledTwice_Throws()
    {
        AlderEval.Configure(_ => { });

        Assert.Throws<InvalidOperationException>(() =>
            AlderEval.Configure(_ => { }));
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
        Assert.That(AlderEval.GetEngine().Evaluate<int>("2 ** 3"), Is.EqualTo(8));

        AlderEval.Reset();

        AlderEval.Configure(o => o.LanguageMode = LanguageMode.Standard);
        Assert.That(AlderEval.GetEngine().Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void Reset_ReplacesGlobalEngineInstance()
    {
        var before = AlderEval.GetEngine();

        AlderEval.Reset();

        var after = AlderEval.GetEngine();

        Assert.That(after, Is.Not.SameAs(before));
    }

    [Test]
    public void GlobalEngine_UsesNormalEngineApis()
    {
        var engine = AlderEval.GetEngine();
        var parsed = engine.Parse("x + y");

        var result = engine.Evaluate<int>(parsed, new Dictionary<string, object?>
        {
            ["x"] = 7,
            ["y"] = 5,
        });

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void GlobalEngine_WithAnonymousObjectVariables_IsolatedPerCall()
    {
        var engine = AlderEval.GetEngine();

        var first = engine.Evaluate<int>("x + y", new { x = 3, y = 4 });
        var second = engine.Evaluate<int>("x + y", new { x = 10, y = 20 });

        Assert.That(first, Is.EqualTo(7));
        Assert.That(second, Is.EqualTo(30));
    }

    [Test]
    public void Concurrent_GlobalEngine_Evaluations_AreThreadSafe()
    {
        const int threadCount = 10;
        const int evalPerThread = 50;
        var exceptions = new List<Exception>();
        var barrier = new Barrier(threadCount);

        var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
        {
            try
            {
                barrier.SignalAndWait();
                var engine = AlderEval.GetEngine();
                for (var i = 0; i < evalPerThread; i++)
                {
                    var result = engine.Evaluate<int>("1 + 1");
                    if (result != 2)
                        throw new Exception($"Expected 2, got {result}");
                }
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        })).ToList();

        foreach (var thread in threads) thread.Start();
        foreach (var thread in threads) thread.Join();

        Assert.That(exceptions, Is.Empty, () => string.Join("\n", exceptions.Select(e => e.Message)));
    }
}
