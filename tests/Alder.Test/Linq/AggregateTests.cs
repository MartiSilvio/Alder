using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class AggregateTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(AggregateTestCases))]
    public async Task Aggregate(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(CountTestCases))]
    public async Task Count(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(SumAverageTestCases))]
    public async Task SumAverage(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(MinMaxTestCases))]
    public async Task MinMax(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);


    private static IEnumerable<TestCaseData> AggregateTestCases()
    {
        yield return new TestCaseData(
            "numbers.Aggregate(0, (acc, x) => acc + x)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4 }
            },
            10
        ).SetName("Aggregate_WithSeed_ReducesCollection");

        yield return new TestCaseData(
            "numbers.Aggregate((acc, x) => acc + x)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4 }
            },
            10
        ).SetName("Aggregate_WithoutSeed_ReducesCollection");

        yield return new TestCaseData(
            "words.Aggregate(\"\", (acc, x) => acc + x)",
            new Dictionary<string, object?>
            {
                ["words"] = new List<string> { "a", "b", "c" }
            },
            "abc"
        ).SetName("Aggregate_StringConcat_ConcatenatesStrings");
    }



    private static IEnumerable<TestCaseData> CountTestCases()
    {
        yield return new TestCaseData(
            "numbers.Count()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            5
        ).SetName("Count_ReturnsElementCount");

        yield return new TestCaseData(
            "numbers.Count(x => x > 2)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            3
        ).SetName("Count_WithPredicate_ReturnsMatchingCount");

        yield return new TestCaseData(
            "numbers.Count()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int>()
            },
            0
        ).SetName("Count_EmptyCollection_ReturnsZero");
    }



    private static IEnumerable<TestCaseData> SumAverageTestCases()
    {
        yield return new TestCaseData(
            "numbers.Sum()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            15
        ).SetName("Sum_ReturnsSum");

        yield return new TestCaseData(
            "numbers.Average()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 10, 20, 30 }
            },
            20.0
        ).SetName("Average_ReturnsAverage");
    }



    private static IEnumerable<TestCaseData> MinMaxTestCases()
    {
        yield return new TestCaseData(
            "numbers.Min()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 5, 2, 8, 1, 9 }
            },
            1
        ).SetName("Min_ReturnsMinimum");

        yield return new TestCaseData(
            "numbers.Max()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 5, 2, 8, 1, 9 }
            },
            9
        ).SetName("Max_ReturnsMaximum");
    }



    [Test]
    public void Sum_WithSelector_ReturnsSumOfSelected()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 },
            new() { ["Value"] = 30 }
        });

        var result = engine.Evaluate("items.Sum(x => x.Value)");
        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void Average_WithSelector_ReturnsAverageOfSelected()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 }
        });

        var result = engine.Evaluate("items.Average(x => x.Value)");
        Assert.That(result, Is.EqualTo(15.0));
    }

    [Test]
    public void Min_WithSelector_ReturnsMinimumOfSelected()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 30 },
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 }
        });

        var result = engine.Evaluate("items.Min(x => x.Value)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Max_WithSelector_ReturnsMaximumOfSelected()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 30 },
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 }
        });

        var result = engine.Evaluate("items.Max(x => x.Value)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void MinBy_ReturnsElementWithMinimumKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Bob", ["Age"] = 30 },
            new() { ["Name"] = "Alice", ["Age"] = 25 },
            new() { ["Name"] = "Charlie", ["Age"] = 35 }
        });

        var result = engine.Evaluate("items.MinBy(x => x.Age)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Alice"));
        Assert.That(result["Age"], Is.EqualTo(25));
    }

    [Test]
    public void MinBy_WithStrings_ReturnsElementWithMinimumKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Charlie", ["Value"] = 3 },
            new() { ["Name"] = "Alice", ["Value"] = 1 },
            new() { ["Name"] = "Bob", ["Value"] = 2 }
        });

        var result = engine.Evaluate("items.MinBy(x => x.Name)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Alice"));
    }

    [Test]
    public void MaxBy_ReturnsElementWithMaximumKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Bob", ["Age"] = 30 },
            new() { ["Name"] = "Alice", ["Age"] = 25 },
            new() { ["Name"] = "Charlie", ["Age"] = 35 }
        });

        var result = engine.Evaluate("items.MaxBy(x => x.Age)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Charlie"));
        Assert.That(result["Age"], Is.EqualTo(35));
    }

    [Test]
    public void MaxBy_WithStrings_ReturnsElementWithMaximumKey()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Alice", ["Value"] = 1 },
            new() { ["Name"] = "Charlie", ["Value"] = 3 },
            new() { ["Name"] = "Bob", ["Value"] = 2 }
        });

        var result = engine.Evaluate("items.MaxBy(x => x.Name)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Charlie"));
    }

    [Test]
    public void MinBy_MultipleWithSameKey_ReturnsFirst()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Alice", ["Age"] = 25 },
            new() { ["Name"] = "Bob", ["Age"] = 25 },
            new() { ["Name"] = "Charlie", ["Age"] = 30 }
        });

        var result = engine.Evaluate("items.MinBy(x => x.Age)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Alice"));
    }



    [Test]
    public void Sum_WithStrings_ThrowsException()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("strings", new List<string> { "a", "b", "c" });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("strings.Sum()"));
        // Overload resolution now reports CS1501 ("no overload takes 0 arguments") because
        // extension-receiver filtering excludes Sum() overloads that cannot bind to IEnumerable<string>.
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1501));
    }

    [Test]
    public void Sum_WithMixedNonNumeric_ThrowsException()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<object> { "hello", "world" });

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("items.Sum()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1501));
    }

    [Test]
    public void MinBy_EmptyCollection_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.MinBy(x => x)"));
    }

    [Test]
    public void MaxBy_EmptyCollection_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.MaxBy(x => x)"));
    }



    [Test]
    public void LongCount_ReturnsLongCount()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.LongCount()");
        Assert.That(result, Is.EqualTo(5L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void LongCount_WithPredicate_ReturnsMatchingCount()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.LongCount(x => x > 2)");
        Assert.That(result, Is.EqualTo(3L));
    }



    [Test]
    public void Sum_WithIntSelector()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Sum(x => x * 2)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Average_WithIntSelector()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Average(x => x * 2)");
        Assert.That(result, Is.EqualTo(6.0));
    }



    [Test]
    public void Min_WithStringSelector()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("names", new List<string> { "Charlie", "Alice", "Bob" });

        var result = engine.Evaluate("names.Min(x => x.Length)");
        Assert.That(result, Is.EqualTo(3)); // "Bob" has length 3
    }

    [Test]
    public void Max_WithStringSelector()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("names", new List<string> { "Charlie", "Alice", "Bob" });

        var result = engine.Evaluate("names.Max(x => x.Length)");
        Assert.That(result, Is.EqualTo(7)); // "Charlie" has length 7
    }

}
