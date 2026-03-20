using CsEval.Test._Infrastructure;

namespace CsEval.Test.Linq;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class QuantifierTests(CompilationMode mode)
{
    [Test]
    [TestCaseSource(nameof(AnyAllTestCases))]
    public async Task AnyAll(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(ContainsReverseTestCases))]
    public async Task ContainsReverse(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    #region Any / All Test Cases

    private static IEnumerable<TestCaseData> AnyAllTestCases()
    {
        yield return new TestCaseData(
            "numbers.Any()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            true
        ).SetName("Any_NonEmpty_ReturnsTrue");

        yield return new TestCaseData(
            "numbers.Any()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int>()
            },
            false
        ).SetName("Any_Empty_ReturnsFalse");

        yield return new TestCaseData(
            "numbers.Any(x => x > 2)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            true
        ).SetName("Any_WithPredicate_MatchExists_ReturnsTrue");

        yield return new TestCaseData(
            "numbers.Any(x => x > 10)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            false
        ).SetName("Any_WithPredicate_NoMatch_ReturnsFalse");

        yield return new TestCaseData(
            "numbers.All(x => x > 0)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 2, 4, 6 }
            },
            true
        ).SetName("All_AllMatch_ReturnsTrue");

        yield return new TestCaseData(
            "numbers.All(x => x > 1)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            false
        ).SetName("All_SomeDontMatch_ReturnsFalse");
    }

    #endregion

    #region Contains Test Cases

    private static IEnumerable<TestCaseData> ContainsReverseTestCases()
    {
        yield return new TestCaseData(
            "numbers.Contains(2)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            true
        ).SetName("Contains_ElementExists_ReturnsTrue");

        yield return new TestCaseData(
            "numbers.Contains(5)",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 3 }
            },
            false
        ).SetName("Contains_ElementNotExists_ReturnsFalse");

        yield return new TestCaseData(
            "names.Contains(\"Bob\")",
            new Dictionary<string, object?>
            {
                ["names"] = new List<string> { "Alice", "Bob", "Charlie" }
            },
            true
        ).SetName("Contains_StringElement_Works");

        // Note: Reverse test is in the non-parity section because
        // CsEval's Reverse() uses Enumerable.Reverse while C# List<T>.Reverse() is in-place
    }

    #endregion

    #region SequenceEqual

    [Test]
    public void SequenceEqual_SameElements_ReturnsTrue()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("a", new List<int> { 1, 2, 3 });
        engine.SetVariable("b", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("a.SequenceEqual(b)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void SequenceEqual_DifferentElements_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("a", new List<int> { 1, 2, 3 });
        engine.SetVariable("b", new List<int> { 1, 2, 4 });

        var result = engine.Evaluate("a.SequenceEqual(b)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void SequenceEqual_DifferentLength_ReturnsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("a", new List<int> { 1, 2, 3 });
        engine.SetVariable("b", new List<int> { 1, 2 });

        var result = engine.Evaluate("a.SequenceEqual(b)");
        Assert.That(result, Is.False);
    }

    #endregion
}
