using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class PartitioningTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(DistinctTakeSkipTestCases))]
    public async Task DistinctTakeSkip(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);


    private static IEnumerable<TestCaseData> DistinctTakeSkipTestCases()
    {
        yield return new TestCaseData(
            "numbers.Take(3).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            new[] { 1, 2, 3 }
        ).SetName("Take_ReturnsFirstN");

        yield return new TestCaseData(
            "numbers.Take(10).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2 }
            },
            new[] { 1, 2 }
        ).SetName("Take_MoreThanCount_ReturnsAll");

        yield return new TestCaseData(
            "numbers.Skip(2).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            new[] { 3, 4, 5 }
        ).SetName("Skip_SkipsFirstN");

        yield return new TestCaseData(
            "numbers.Skip(10).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2 }
            },
            Array.Empty<int>()
        ).SetName("Skip_MoreThanCount_ReturnsEmpty");
    }



    [Test]
    public void TakeWhile_TakesWhileConditionTrue()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 1, 2 });

        var result = engine.Evaluate("numbers.TakeWhile(x => x < 4).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void TakeWhile_AllMatch_ReturnsAll()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.TakeWhile(x => x < 10).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void TakeWhile_NoneMatch_ReturnsEmpty()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 5, 6, 7 });

        var result = engine.Evaluate("numbers.TakeWhile(x => x < 5).ToList()");
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void SkipWhile_SkipsWhileConditionTrue()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 1, 2 });

        var result = engine.Evaluate("numbers.SkipWhile(x => x < 4).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 4, 5, 1, 2 }));
    }

    [Test]
    public void SkipWhile_AllMatch_ReturnsEmpty()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.SkipWhile(x => x < 10).ToList()");
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void SkipWhile_NoneMatch_ReturnsAll()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 5, 6, 7 });

        var result = engine.Evaluate("numbers.SkipWhile(x => x < 5).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 5, 6, 7 }));
    }



    [Test]
    public void TakeLast_TakesLastN()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.TakeLast(3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 3, 4, 5 }));
    }

    [Test]
    public void TakeLast_MoreThanCount_ReturnsAll()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2 });

        var result = engine.Evaluate("numbers.TakeLast(10).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void SkipLast_SkipsLastN()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.SkipLast(2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void SkipLast_MoreThanCount_ReturnsEmpty()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2 });

        var result = engine.Evaluate("numbers.SkipLast(10).ToList()");
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }



    [Test]
    public void Chunk_SplitsIntoChunks()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6, 7 });

        var result = engine.Evaluate("numbers.Chunk(3).ToList()") as IList;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(result[1], Is.EqualTo(new[] { 4, 5, 6 }));
        Assert.That(result[2], Is.EqualTo(new[] { 7 }));
    }

}
