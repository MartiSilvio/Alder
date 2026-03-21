using Alder.Test._Infrastructure;

namespace Alder.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ElementAccessTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(FirstLastTestCases))]
    public async Task FirstLast(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(SingleTestCases))]
    public async Task Single(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    #region First / Last Test Cases

    private static IEnumerable<TestCaseData> FirstLastTestCases()
    {
        yield return new TestCaseData(
            "numbers.First()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            1
        ).SetName("First_ReturnsFirstElement");

        yield return new TestCaseData(
            "numbers.First(x => x > 3)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            4
        ).SetName("First_WithPredicate_ReturnsFirstMatching");

        yield return new TestCaseData(
            "numbers.FirstOrDefault()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            1
        ).SetName("FirstOrDefault_ReturnsFirstElement");

        yield return new TestCaseData(
            "numbers.Last()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            3
        ).SetName("Last_ReturnsLastElement");

        yield return new TestCaseData(
            "numbers.Last(x => x < 4)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
            },
            3
        ).SetName("Last_WithPredicate_ReturnsLastMatching");
    }

    #endregion

    #region Single Test Cases

    private static IEnumerable<TestCaseData> SingleTestCases()
    {
        yield return new TestCaseData(
            "numbers.Single()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 42 }
            },
            42
        ).SetName("Single_SingleElement_ReturnsIt");

        yield return new TestCaseData(
            "numbers.Single(x => x == 2)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            2
        ).SetName("Single_WithPredicate_ReturnsMatching");
    }

    #endregion

    #region Exception Tests

    [Test]
    public void First_EmptyCollection_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.First()"));
    }

    [Test]
    public void FirstOrDefault_EmptyCollection_ReturnsDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.FirstOrDefault()");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void FirstOrDefault_WithPredicate_NoMatch_ReturnsDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.FirstOrDefault(x => x > 10)");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void LastOrDefault_EmptyCollection_ReturnsDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.LastOrDefault()");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Single_MultipleElements_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.Single()"));
    }

    [Test]
    public void SingleOrDefault_EmptyCollection_ReturnsDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.SingleOrDefault()");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region ElementAt / ElementAtOrDefault

    [Test]
    public void ElementAt_ReturnsElementAtIndex()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("numbers.ElementAt(2)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ElementAt_OutOfRange_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Evaluate("numbers.ElementAt(10)"));
    }

    [Test]
    public void ElementAtOrDefault_ReturnsElementAtIndex()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("numbers.ElementAtOrDefault(2)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ElementAtOrDefault_OutOfRange_ReturnsDefault()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.ElementAtOrDefault(10)");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion
}
