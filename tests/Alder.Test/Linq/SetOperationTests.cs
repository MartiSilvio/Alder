using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class SetOperationTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(SetOperationsTestCases))]
    public async Task SetOperations(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    #region Set Operations Test Cases

    private static IEnumerable<TestCaseData> SetOperationsTestCases()
    {
        yield return new TestCaseData(
            "first.Except(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3, 4, 5 },
                ["second"] = new List<int> { 3, 4, 5, 6, 7 }
            },
            new[] { 1, 2 }
        ).SetName("Except_ReturnsElementsNotInSecond");

        yield return new TestCaseData(
            "first.Except(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3 },
                ["second"] = new List<int> { 4, 5, 6 }
            },
            new[] { 1, 2, 3 }
        ).SetName("Except_WithNoOverlap_ReturnsAll");

        yield return new TestCaseData(
            "first.Except(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3 },
                ["second"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            Array.Empty<int>()
        ).SetName("Except_WithFullOverlap_ReturnsEmpty");

        yield return new TestCaseData(
            "first.Except(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<string> { "a", "b", "c" },
                ["second"] = new List<string> { "b", "d" }
            },
            new[] { "a", "c" }
        ).SetName("Except_WithStrings_Works");

        yield return new TestCaseData(
            "first.Intersect(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3, 4, 5 },
                ["second"] = new List<int> { 3, 4, 5, 6, 7 }
            },
            new[] { 3, 4, 5 }
        ).SetName("Intersect_ReturnsCommonElements");

        yield return new TestCaseData(
            "first.Intersect(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3 },
                ["second"] = new List<int> { 4, 5, 6 }
            },
            Array.Empty<int>()
        ).SetName("Intersect_WithNoOverlap_ReturnsEmpty");

        yield return new TestCaseData(
            "first.Intersect(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<string> { "a", "b", "c" },
                ["second"] = new List<string> { "b", "c", "d" }
            },
            new[] { "b", "c" }
        ).SetName("Intersect_WithStrings_Works");

        yield return new TestCaseData(
            "first.Union(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3 },
                ["second"] = new List<int> { 3, 4, 5 }
            },
            new[] { 1, 2, 3, 4, 5 }
        ).SetName("Union_ReturnsCombinedWithoutDuplicates");

        yield return new TestCaseData(
            "first.Union(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2 },
                ["second"] = new List<int> { 3, 4 }
            },
            new[] { 1, 2, 3, 4 }
        ).SetName("Union_WithNoOverlap_ReturnsCombined");

        yield return new TestCaseData(
            "first.Union(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2, 3 },
                ["second"] = new List<int> { 1, 2, 3 }
            },
            new[] { 1, 2, 3 }
        ).SetName("Union_WithFullOverlap_ReturnsDistinct");

        yield return new TestCaseData(
            "first.Union(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<string> { "a", "b" },
                ["second"] = new List<string> { "b", "c" }
            },
            new[] { "a", "b", "c" }
        ).SetName("Union_WithStrings_Works");
    }

    #endregion

    #region ExceptBy / IntersectBy / UnionBy (.NET 6+)

    [Test]
    public void ExceptBy_ExcludesByKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" },
            new() { ["Id"] = 3, ["Name"] = "Charlie" }
        });
        engine.SetVariable("excludeIds", new List<int> { 2 });

        var result = engine.Evaluate("items.ExceptBy(excludeIds, x => x.Id).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Charlie" }));
    }

    [Test]
    public void IntersectBy_IntersectsByKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" },
            new() { ["Id"] = 3, ["Name"] = "Charlie" }
        });
        engine.SetVariable("keepIds", new List<int> { 1, 3 });

        var result = engine.Evaluate("items.IntersectBy(keepIds, x => x.Id).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Charlie" }));
    }

    [Test]
    public void UnionBy_UnionsByKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("first", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        });
        engine.SetVariable("second", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 2, ["Name"] = "Bob2" },
            new() { ["Id"] = 3, ["Name"] = "Charlie" }
        });

        var result = engine.Evaluate("first.UnionBy(second, x => x.Id).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Bob", "Charlie" }));
    }

    #endregion
}
