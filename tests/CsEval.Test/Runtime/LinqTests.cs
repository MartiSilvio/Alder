// All tests engine-only: RegisterExtensionMethods is CsEval-specific API,
// lambda isolation is internal behavior (per 09-08 audit).

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class LinqTests(CompilationMode mode)
{
    [Test]
    public void Where_ImplicitItPredicate_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Where(it > 2).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void Select_ImplicitDiscardPlaceholder_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Select(_ * 10).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 10, 20, 30, 40 }));
    }

    [Test]
    public void Where_ExplicitItLambda_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Where(it => it > 2).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void Select_ExplicitDiscardLambda_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Select(_ => _ * 10).ToArray()");

        Assert.That(result, Is.EqualTo(new[] { 10, 20, 30, 40 }));
    }

    [Test]
    public void AggregateBuiltins_SumCountAvgMinMax_Work()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Extended
        });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        Assert.That(engine.Evaluate("sum(numbers)"), Is.EqualTo(10));
        Assert.That(engine.Evaluate("count(numbers.Where(it > 2))"), Is.EqualTo(2));
        Assert.That(engine.Evaluate("avg(numbers)"), Is.EqualTo(2.5d));
        Assert.That(engine.Evaluate("min(numbers)"), Is.EqualTo(1));
        Assert.That(engine.Evaluate("max(numbers)"), Is.EqualTo(4));
    }

    [Test]
    [TestCaseSource(nameof(WhereTestCases))]
    public async Task Where(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(SelectTestCases))]
    public async Task Select(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(AggregateTestCases))]
    public async Task Aggregate(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(FirstLastTestCases))]
    public async Task FirstLast(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(SingleTestCases))]
    public async Task Single(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(AnyAllTestCases))]
    public async Task AnyAll(string expr, Dictionary<string, object?> variables, object expected)
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

    [Test]
    [TestCaseSource(nameof(OrderByTestCases))]
    public async Task OrderBy(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(DistinctTakeSkipTestCases))]
    public async Task DistinctTakeSkip(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(ContainsReverseTestCases))]
    public async Task ContainsReverse(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(SetOperationsTestCases))]
    public async Task SetOperations(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(ConversionTestCases))]
    public async Task Conversion(string expr, Dictionary<string, object?> variables, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, variables, expected, mode);

    [Test]
    [TestCaseSource(nameof(ChainedTestCases))]
    public async Task Chained(string expr, Dictionary<string, object?> variables, object expected)
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

    #region Aggregate Test Cases

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

    #endregion

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

    #region Count Test Cases

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

    #endregion

    #region Sum / Average Test Cases

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

    #endregion

    #region Min / Max Test Cases

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

    #endregion

    #region OrderBy Test Cases

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

    #endregion

    #region Distinct / Take / Skip Test Cases

    private static IEnumerable<TestCaseData> DistinctTakeSkipTestCases()
    {
        yield return new TestCaseData(
            "numbers.Distinct().ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = new List<int> { 1, 2, 2, 3, 3, 3 }
            },
            new[] { 1, 2, 3 }
        ).SetName("Distinct_RemovesDuplicates");

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

    #endregion

    #region Contains / Reverse Test Cases

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

    #region Reverse - CsEval Specific

    [Test]
    public void Reverse_OnArray_UsesEnumerableReverse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var numbers = new[] { 1, 2, 3 };
        engine.SetVariable("numbers", numbers);

        var result = engine.Evaluate("numbers.Reverse().ToList()");

        Assert.That(result, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    #endregion

    #region Exception Tests (Cannot be parity tested)

    [Test]
    public void First_EmptyCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.First()"));
    }

    [Test]
    public void FirstOrDefault_EmptyCollection_ReturnsDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.FirstOrDefault()");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void FirstOrDefault_WithPredicate_NoMatch_ReturnsDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.FirstOrDefault(x => x > 10)");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void LastOrDefault_EmptyCollection_ReturnsDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.LastOrDefault()");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Single_MultipleElements_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.Single()"));
    }

    [Test]
    public void SingleOrDefault_EmptyCollection_ReturnsDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.SingleOrDefault()");
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Sum_WithStrings_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("strings", new List<string> { "a", "b", "c" });

        Assert.Throws<CsEvalException>(() => engine.Evaluate("strings.Sum()"));
    }

    [Test]
    public void Sum_WithMixedNonNumeric_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<object> { "hello", "world" });

        Assert.Throws<CsEvalException>(() => engine.Evaluate("items.Sum()"));
    }

    [Test]
    public void MinBy_EmptyCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.MinBy(x => x)"));
    }

    [Test]
    public void MaxBy_EmptyCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.MaxBy(x => x)"));
    }

    #endregion

    #region Tests with Non-Serializable Types (Cannot be parity tested via TestCaseSource)

    [Test]
    public void Select_WithMemberAccess_ProjectsProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Tags"] = new List<string> { "a", "b" } },
            new() { ["Tags"] = new List<string> { "c" } }
        });

        var result = engine.Evaluate("items.SelectMany(x => x.Tags).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void Sum_WithSelector_ReturnsSumOfSelected()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Alice", ["Value"] = 1 },
            new() { ["Name"] = "Charlie", ["Value"] = 3 },
            new() { ["Name"] = "Bob", ["Value"] = 2 }
        });

        var result = engine.Evaluate("items.MaxBy(x => x.Name)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Charlie"));
    }

    [Test]
    public void OrderBy_WithPropertySelector_SortsByProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Charlie" },
            new() { ["Name"] = "Alice" },
            new() { ["Name"] = "Bob" }
        });

        var result = engine.Evaluate("items.OrderBy(x => x.Name).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Bob", "Charlie" }));
    }

    [Test]
    public void GroupBy_GroupsByKey()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Category"] = "A", ["Value"] = 1 },
            new() { ["Category"] = "B", ["Value"] = 2 },
            new() { ["Category"] = "A", ["Value"] = 3 }
        });

        var result = engine.Evaluate("items.GroupBy(x => x.Category).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(2));

        var groupA = list.Cast<IGrouping<object?, object?>>().First(g => (string)g.Key! == "A");
        Assert.That(groupA.Count(), Is.EqualTo(2));
    }

    [Test]
    public void GroupBy_ReturnsIGrouping()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6 });

        var result = engine.Evaluate("numbers.GroupBy(x => x > 3).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(2));

        foreach (var group in list)
        {
            Assert.That(group, Is.InstanceOf<IGrouping<bool, int>>());
            var g = (IGrouping<bool, int>)group!;
            Assert.That(g.Any(), Is.True);
        }
    }

    [Test]
    public void GroupBy_CanPassToFunctionAcceptingIGrouping()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums", new[] { 1, 2, 3, 4, 5 });

        // Register a function that accepts IGrouping<bool, int>
        engine.RegisterFunction("SumGroup", args =>
        {
            var group = (IGrouping<bool, int>)args[0]!;
            return group.Sum();
        });

        var result = engine.Evaluate("nums.GroupBy(x => x > 2).Select(g => SumGroup(g)).ToList()");
        var sums = ((IList)result!).Cast<object>().Select(x => (int)x).OrderBy(x => x).ToList();

        Assert.That(sums, Is.EqualTo(new[] { 3, 12 })); // 1+2=3, 3+4+5=12
    }

    [Test]
    public void Zip_WithSelector_CombinesElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums1", new List<int> { 1, 2, 3 });
        engine.SetVariable("nums2", new List<int> { 10, 20, 30 });

        var result = engine.Evaluate("nums1.Zip(nums2, (a, b) => a + b).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 11, 22, 33 }));
    }

    [Test]
    public void Zip_WithoutSelector_ReturnsTuples()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("shortList", new List<int> { 1, 2 });
        engine.SetVariable("longList", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("shortList.Zip(longList, (a, b) => a + b).ToList()");
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void Chained_WhereSelectOrderBy()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    #region ThenBy / ThenByDescending

    [Test]
    public void ThenBy_SecondarySort()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Category"] = "A", ["Name"] = "Apple" },
            new() { ["Category"] = "A", ["Name"] = "Zebra" },
            new() { ["Category"] = "B", ["Name"] = "Banana" }
        });

        var result = engine.Evaluate("items.OrderBy(x => x.Category).ThenByDescending(x => x.Name).Select(x => x.Name).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Zebra", "Apple", "Banana" }));
    }

    #endregion

    #region TakeWhile / SkipWhile

    [Test]
    public void TakeWhile_TakesWhileConditionTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 1, 2 });

        var result = engine.Evaluate("numbers.TakeWhile(x => x < 4).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void TakeWhile_AllMatch_ReturnsAll()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.TakeWhile(x => x < 10).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void TakeWhile_NoneMatch_ReturnsEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 5, 6, 7 });

        var result = engine.Evaluate("numbers.TakeWhile(x => x < 5).ToList()");
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void SkipWhile_SkipsWhileConditionTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 1, 2 });

        var result = engine.Evaluate("numbers.SkipWhile(x => x < 4).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 4, 5, 1, 2 }));
    }

    [Test]
    public void SkipWhile_AllMatch_ReturnsEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.SkipWhile(x => x < 10).ToList()");
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }

    [Test]
    public void SkipWhile_NoneMatch_ReturnsAll()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 5, 6, 7 });

        var result = engine.Evaluate("numbers.SkipWhile(x => x < 5).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 5, 6, 7 }));
    }

    #endregion

    #region ElementAt / ElementAtOrDefault

    [Test]
    public void ElementAt_ReturnsElementAtIndex()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("numbers.ElementAt(2)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ElementAt_OutOfRange_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Evaluate("numbers.ElementAt(10)"));
    }

    [Test]
    public void ElementAtOrDefault_ReturnsElementAtIndex()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("numbers.ElementAtOrDefault(2)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ElementAtOrDefault_OutOfRange_ReturnsDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.ElementAtOrDefault(10)");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region DefaultIfEmpty

    [Test]
    public void DefaultIfEmpty_NonEmpty_ReturnsOriginal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.DefaultIfEmpty().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void DefaultIfEmpty_Empty_ReturnsDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.DefaultIfEmpty().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void DefaultIfEmpty_WithValue_Empty_ReturnsSpecifiedDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.DefaultIfEmpty(42).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 42 }));
    }

    #endregion

    #region Append / Prepend

    [Test]
    public void Append_AddsToEnd()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Append(4).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Prepend_AddsToStart()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 2, 3, 4 });

        var result = engine.Evaluate("numbers.Prepend(1).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    #endregion

    #region SequenceEqual

    [Test]
    public void SequenceEqual_SameElements_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("a", new List<int> { 1, 2, 3 });
        engine.SetVariable("b", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("a.SequenceEqual(b)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void SequenceEqual_DifferentElements_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("a", new List<int> { 1, 2, 3 });
        engine.SetVariable("b", new List<int> { 1, 2, 4 });

        var result = engine.Evaluate("a.SequenceEqual(b)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void SequenceEqual_DifferentLength_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("a", new List<int> { 1, 2, 3 });
        engine.SetVariable("b", new List<int> { 1, 2 });

        var result = engine.Evaluate("a.SequenceEqual(b)");
        Assert.That(result, Is.False);
    }

    #endregion

    #region LongCount

    [Test]
    public void LongCount_ReturnsLongCount()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.LongCount()");
        Assert.That(result, Is.EqualTo(5L));
        Assert.That(result, Is.TypeOf<long>());
    }

    [Test]
    public void LongCount_WithPredicate_ReturnsMatchingCount()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.LongCount(x => x > 2)");
        Assert.That(result, Is.EqualTo(3L));
    }

    #endregion

    #region Cast / OfType

    [Test]
    public void Cast_WithGenericSyntax()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("objects", new object[] { 1, 2, 3 });

        var result = engine.Evaluate("objects.Cast<int>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Cast_WithKeywordType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("objects", new object[] { 1L, 2L, 3L });

        var result = engine.Evaluate("objects.Cast<long>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1L, 2L, 3L }));
    }

    [Test]
    public void OfType_FiltersIntegers()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("mixed", new object[] { 1, "hello", 2, "world", 3 });

        var result = engine.Evaluate("mixed.OfType<int>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void OfType_FiltersStrings()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("mixed", new object[] { 1, "hello", 2, "world", 3 });

        var result = engine.Evaluate("mixed.OfType<string>().ToList()");
        Assert.That(result, Is.EqualTo(new[] { "hello", "world" }));
    }

    #endregion

    #region ToDictionary

    [Test]
    public void ToDictionary_WithKeySelector()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    #region Join

    [Test]
    public void Join_InnerJoin()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("people", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice" },
            new() { ["Id"] = 2, ["Name"] = "Bob" }
        });
        engine.SetVariable("orders", new List<Dictionary<string, object?>>
        {
            new() { ["PersonId"] = 1, ["Product"] = "Apple" },
            new() { ["PersonId"] = 1, ["Product"] = "Banana" },
            new() { ["PersonId"] = 2, ["Product"] = "Orange" }
        });

        var result = engine.Evaluate(
            "people.Join(orders, p => p.Id, o => o.PersonId, (p, o) => p.Name + \": \" + o.Product).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Alice: Apple", "Alice: Banana", "Bob: Orange" }));
    }

    #endregion

    #region GroupJoin

    [Test]
    public void GroupJoin_GroupsMatchingElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("categories", new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Fruit" },
            new() { ["Id"] = 2, ["Name"] = "Vegetable" }
        });
        engine.SetVariable("products", new List<Dictionary<string, object?>>
        {
            new() { ["CategoryId"] = 1, ["Name"] = "Apple" },
            new() { ["CategoryId"] = 1, ["Name"] = "Banana" },
            new() { ["CategoryId"] = 2, ["Name"] = "Carrot" }
        });

        var result = engine.Evaluate(
            "categories.GroupJoin(products, c => c.Id, p => p.CategoryId, (c, ps) => c.Name + \": \" + ps.Count()).ToList()");
        Assert.That(result, Is.EqualTo(new[] { "Fruit: 2", "Vegetable: 1" }));
    }

    #endregion

    #region Chunk (.NET 6+)

    [Test]
    public void Chunk_SplitsIntoChunks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6, 7 });

        var result = engine.Evaluate("numbers.Chunk(3).ToList()") as IList;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
        Assert.That(result[0], Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(result[1], Is.EqualTo(new[] { 4, 5, 6 }));
        Assert.That(result[2], Is.EqualTo(new[] { 7 }));
    }

    #endregion

    #region DistinctBy (.NET 6+)

    [Test]
    public void DistinctBy_RemovesDuplicatesByKey()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    #region ExceptBy / IntersectBy / UnionBy (.NET 6+)

    [Test]
    public void ExceptBy_ExcludesByKey()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    #region MinBy / MaxBy with multiple items

    [Test]
    public void MinBy_MultipleWithSameKey_ReturnsFirst()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Alice", ["Age"] = 25 },
            new() { ["Name"] = "Bob", ["Age"] = 25 },
            new() { ["Name"] = "Charlie", ["Age"] = 30 }
        });

        var result = engine.Evaluate("items.MinBy(x => x.Age)") as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Alice"));
    }

    #endregion

    #region TakeLast / SkipLast

    [Test]
    public void TakeLast_TakesLastN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.TakeLast(3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 3, 4, 5 }));
    }

    [Test]
    public void TakeLast_MoreThanCount_ReturnsAll()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2 });

        var result = engine.Evaluate("numbers.TakeLast(10).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void SkipLast_SkipsLastN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.SkipLast(2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void SkipLast_MoreThanCount_ReturnsEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2 });

        var result = engine.Evaluate("numbers.SkipLast(10).ToList()");
        Assert.That(result, Is.EqualTo(Array.Empty<int>()));
    }

    #endregion

    #region Static LINQ Methods (Enumerable.Range, Repeat)

    [Test]
    public void Enumerable_Range()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterModule("Enumerable", typeof(Enumerable));

        var result = engine.Evaluate("Enumerable.Range(1, 5).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Enumerable_Repeat()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.RegisterModule("Enumerable", typeof(Enumerable));

        var result = engine.Evaluate("Enumerable.Repeat(42, 3).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 42, 42, 42 }));
    }

    // Note: Enumerable.Empty<T>() requires generic method invocation which is not yet supported

    #endregion

    #region Sum / Average with selector

    [Test]
    public void Sum_WithIntSelector()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Sum(x => x * 2)");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Average_WithIntSelector()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Average(x => x * 2)");
        Assert.That(result, Is.EqualTo(6.0));
    }

    #endregion

    #region Min / Max with selector returning non-numeric

    [Test]
    public void Min_WithStringSelector()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("names", new List<string> { "Charlie", "Alice", "Bob" });

        var result = engine.Evaluate("names.Min(x => x.Length)");
        Assert.That(result, Is.EqualTo(3)); // "Bob" has length 3
    }

    [Test]
    public void Max_WithStringSelector()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("names", new List<string> { "Charlie", "Alice", "Bob" });

        var result = engine.Evaluate("names.Max(x => x.Length)");
        Assert.That(result, Is.EqualTo(7)); // "Charlie" has length 7
    }

    #endregion
}
