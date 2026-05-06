using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class ProjectionTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(SelectTestCases))]
    public async Task Select(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(ChainedTestCases))]
    public async Task Chained(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    #region Select Test Cases

    private static IEnumerable<TestCaseData> SelectTestCases()
    {
        yield return new TestCaseData(
            "numbers.Select((x) => x * 2).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            new[] { 2, 4, 6 }
        ).SetName("Select_WithSelector_ProjectsElements");
    }

    #endregion

    #region Chained Operations Test Cases

    private static IEnumerable<TestCaseData> ChainedTestCases()
    {
        yield return new TestCaseData(
            "numbers.Select(x => x * 2).Where(x => x > 4).Take(2).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            new[] { 6, 8 }
        ).SetName("Chained_SelectWhereTake");
    }

    #endregion

    #region Non-Serializable Type Tests

    [Test]
    public void Select_WithMemberAccess_ProjectsProperty()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<object> {
            new { Name = "Alice" },
            new { Name = "Bob" }
        });

        var result = engine.Evaluate("items.Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Bob" }));
    }

    [Test]
    public void SelectMany_WithProjection_FlattensAndProjects()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<List<string>> {
            new() { "a", "b" },
            new() { "c" }
        });

        var result = engine.Evaluate("items.SelectMany(x => x).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void Zip_WithSelector_CombinesElements()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("nums1", new List<int> { 1, 2, 3 });
        engine.SetVariable("nums2", new List<int> { 10, 20, 30 });

        var result = engine.Evaluate("nums1.Zip(nums2, (a, b) => a + b).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 11, 22, 33 }));
    }

    [Test]
    public void Zip_WithoutSelector_ReturnsTuples()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("names", new List<string> { "Alice", "Bob" });
        engine.SetVariable("ages", new List<int> { 30, 25 });

        var result = engine.Evaluate("names.Zip(ages).ToList()") as IList;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));

        var first = ((string, int))result![0]!;
        Assert.That(first.Item1, Is.EqualTo("Alice"));
        Assert.That(first.Item2, Is.EqualTo(30));
    }

    [Test]
    public void Zip_DifferentLengths_StopsAtShorter()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("shortList", new List<int> { 1, 2 });
        engine.SetVariable("longList", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("shortList.Zip(longList, (a, b) => a + b).ToList()");
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Chained_WhereSelectOrderBy()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<object>
        {
            TestHelpers.CreateItem("Apple", 1.5),
            TestHelpers.CreateItem("Banana", 0.75),
            TestHelpers.CreateItem("Orange", 2.0),
            TestHelpers.CreateItem("Mango", 3.0)
        });

        var result = engine.Evaluate("items.Where(x => x.Price > 1).OrderBy(x => x.Name).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Apple", "Mango", "Orange" }));
    }

    #endregion
}
