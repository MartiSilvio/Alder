using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class FilteringTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(WhereTestCases))]
    public async Task Where(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(DistinctTestCases))]
    public async Task Distinct(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    #region Where Test Cases

    private static IEnumerable<TestCaseData> WhereTestCases()
    {
        yield return new TestCaseData(
            "numbers.Where((x) => x > 2).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            new[] { 3, 4, 5 }
        ).SetName("Where_WithPredicate_FiltersElements");

        yield return new TestCaseData(
            "numbers.Where(x => x > 2).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            new[] { 3, 4, 5 }
        ).SetName("Where_WithoutParens_FiltersElements");

        yield return new TestCaseData(
            "numbers.Where(x => x > 10).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            Array.Empty<int>()
        ).SetName("Where_EmptyResult_ReturnsEmptyList");
    }

    #endregion

    #region Distinct Test Cases

    private static IEnumerable<TestCaseData> DistinctTestCases()
    {
        yield return new TestCaseData(
            "numbers.Distinct().ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 2, 3, 3, 3 }
            },
            new[] { 1, 2, 3 }
        ).SetName("Distinct_RemovesDuplicates");
    }

    #endregion

    #region Cast / OfType

    [Test]
    public void Cast_WithGenericSyntax()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("objects", new object[] { 1, 2, 3 });

        var result = engine.Evaluate("objects.Cast<int>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Cast_WithKeywordType()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("objects", new object[] { 1L, 2L, 3L });

        var result = engine.Evaluate("objects.Cast<long>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1L, 2L, 3L }));
    }

    [Test]
    public void OfType_FiltersIntegers()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("mixed", new object[] { 1, "hello", 2, "world", 3 });

        var result = engine.Evaluate("mixed.OfType<int>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void OfType_FiltersStrings()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("mixed", new object[] { 1, "hello", 2, "world", 3 });

        var result = engine.Evaluate("mixed.OfType<string>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { "hello", "world" }));
    }

    #endregion

    #region Distinct / DistinctBy

    [Test]
    public void DistinctBy_RemovesDuplicatesByKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Category"] = "A", ["Name"] = "First" },
            new() { ["Category"] = "B", ["Name"] = "Second" },
            new() { ["Category"] = "A", ["Name"] = "Third" }
        });

        var result = engine.Evaluate("items.DistinctBy(x => x.Category).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "First", "Second" }));
    }

    #endregion
}
