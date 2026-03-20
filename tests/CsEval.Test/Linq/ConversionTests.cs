using CsEval.Test._Infrastructure;

namespace CsEval.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConversionTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(ConversionTestCases))]
    public async Task Conversion(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    #region Conversion Test Cases

    private static IEnumerable<TestCaseData> ConversionTestCases()
    {
        yield return new TestCaseData(
            "numbers.ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            new[] { 1, 2, 3 }
        ).SetName("ToList_ReturnsList");

        yield return new TestCaseData(
            "numbers.ToArray()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            new[] { 1, 2, 3 }
        ).SetName("ToArray_ReturnsArray");

        yield return new TestCaseData(
            "first.Concat(second).ToList()",
            new Dictionary<string, object?>
            {
                ["first"] = new List<int> { 1, 2 },
                ["second"] = new List<int> { 3, 4 }
            },
            new[] { 1, 2, 3, 4 }
        ).SetName("Concat_CombinesSequences");
    }

    #endregion

    #region ToDictionary

    [Test]
    public void ToDictionary_WithKeySelector()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        });

        var result = engine.Evaluate("items.ToDictionary(x => x.Id)") as IDictionary;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result.Contains(1), Is.True);
        Assert.That(result.Contains(2), Is.True);
    }

    [Test]
    public void ToDictionary_WithKeyAndValueSelector()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        });

        var result = engine.Evaluate("items.ToDictionary(x => x.Id, x => x.Name)") as IDictionary;
        Assert.That(result, Is.Not.Null);
        Assert.That(result![1], Is.EqualTo("Alice"));
        Assert.That(result[2], Is.EqualTo("Bob"));
    }

    #endregion

    #region ToHashSet

    [Test]
    public void ToHashSet_RemovesDuplicates()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 2, 3, 3, 3 });

        var result = engine.Evaluate("numbers.ToHashSet()");
        Assert.That(result, Is.InstanceOf<HashSet<int>>());
        var set = (HashSet<int>)result!;
        Assert.That(set.Count, Is.EqualTo(3));
        Assert.That(set.Contains(1), Is.True);
        Assert.That(set.Contains(2), Is.True);
        Assert.That(set.Contains(3), Is.True);
    }

    #endregion
}
