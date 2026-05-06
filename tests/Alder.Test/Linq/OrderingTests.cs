using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class OrderingTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(OrderByTestCases))]
    public async Task OrderBy(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);


    private static IEnumerable<TestCaseData> OrderByTestCases()
    {
        yield return new TestCaseData(
            "numbers.OrderBy(x => x).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 3, 1, 4, 1, 5 }
            },
            new[] { 1, 1, 3, 4, 5 }
        ).SetName("OrderBy_SortsAscending");

        yield return new TestCaseData(
            "numbers.OrderByDescending(x => x).ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 3, 1, 4, 1, 5 }
            },
            new[] { 5, 4, 3, 1, 1 }
        ).SetName("OrderByDescending_SortsDescending");
    }



    [Test]
    public void OrderBy_WithPropertySelector_SortsByProperty()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Charlie" },
            new() { ["Name"] = "Alice" },
            new() { ["Name"] = "Bob" }
        });

        var result = engine.Evaluate("items.OrderBy(x => x.Name).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Bob", "Charlie" }));
    }



    [Test]
    public void ThenBy_SecondarySort()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Category"] = "A", ["Name"] = "Zebra" },
            new() { ["Category"] = "B", ["Name"] = "Apple" },
            new() { ["Category"] = "A", ["Name"] = "Apple" },
            new() { ["Category"] = "B", ["Name"] = "Banana" }
        });

        var result = engine.Evaluate("items.OrderBy(x => x.Category).ThenBy(x => x.Name).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Apple", "Zebra", "Apple", "Banana" }));
    }

    [Test]
    public void ThenByDescending_SecondarySort()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Category"] = "A", ["Name"] = "Apple" },
            new() { ["Category"] = "A", ["Name"] = "Zebra" },
            new() { ["Category"] = "B", ["Name"] = "Banana" }
        });

        var result = engine.Evaluate("items.OrderBy(x => x.Category).ThenByDescending(x => x.Name).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Zebra", "Apple", "Banana" }));
    }



    [Test]
    public void Reverse_OnArray_UsesEnumerableReverse()
    {
        var engine = TestEngineFactory.Create(mode);
        var numbers = new[] { 1, 2, 3 };
        engine.SetVariable("numbers", numbers);

        var result = engine.Evaluate("numbers.Reverse().ToList()");

        Assert.That(result, Is.EqualTo(new[] { 3, 2, 1 }));
    }

}
