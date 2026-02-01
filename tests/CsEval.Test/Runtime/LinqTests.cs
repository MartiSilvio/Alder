namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LinqTests(CompilationMode mode)
{
    private async Task RunParityTestAsync(string expr, Dictionary<string, object?> variables, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        foreach (var (name, value) in variables)
            engine.SetVariable(name, value);

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr, variables);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity");
    }

    [Test]
    [TestCaseSource(nameof(WhereTestCases))]
    public async Task Where(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(SelectTestCases))]
    public async Task Select(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(AggregateTestCases))]
    public async Task Aggregate(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(FirstLastTestCases))]
    public async Task FirstLast(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(SingleTestCases))]
    public async Task Single(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(AnyAllTestCases))]
    public async Task AnyAll(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(CountTestCases))]
    public async Task Count(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(SumAverageTestCases))]
    public async Task SumAverage(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(MinMaxTestCases))]
    public async Task MinMax(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(OrderByTestCases))]
    public async Task OrderBy(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(DistinctTakeSkipTestCases))]
    public async Task DistinctTakeSkip(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(ContainsReverseTestCases))]
    public async Task ContainsReverse(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(SetOperationsTestCases))]
    public async Task SetOperations(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(ConversionTestCases))]
    public async Task Conversion(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);

    [Test]
    [TestCaseSource(nameof(ChainedTestCases))]
    public async Task Chained(string expr, Dictionary<string, object?> variables, object expected)
        => await RunParityTestAsync(expr, variables, expected);
    
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
    public async Task Reverse_CsEval_UsesEnumerableReverse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var numbers = new List<int> { 1, 2, 3 };
        engine.SetVariable("numbers", numbers);

        // CsEval's Reverse() uses Enumerable.Reverse (returns new sequence)
        // C# List<T>.Reverse() is in-place and returns void
        // Use AsEnumerable() in C# to get the same behavior
        var result = engine.Evaluate("numbers.Reverse().ToList()");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(
            "numbers.AsEnumerable().Reverse().ToList()",
            new Dictionary<string, object?>
            {
                ["numbers"] = numbers
            }
        );

        Assert.That(result, Is.EqualTo(new[] { 3, 2, 1 }));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity");
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
    public void FirstOrDefault_EmptyCollection_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.FirstOrDefault()");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FirstOrDefault_WithPredicate_NoMatch_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.FirstOrDefault(x => x > 10)");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LastOrDefault_EmptyCollection_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.LastOrDefault()");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Single_MultipleElements_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.Single()"));
    }

    [Test]
    public void SingleOrDefault_EmptyCollection_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.SingleOrDefault()");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Sum_WithStrings_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("strings", new List<string> { "a", "b", "c" });

        var ex = Assert.Throws<InvalidOperationException>(() => engine.Evaluate("strings.Sum()"));
        Assert.That(ex!.Message, Does.Contain("Sum()").And.Contain("numeric"));
    }

    [Test]
    public void Sum_WithMixedNonNumeric_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<object> { "hello", "world" });

        var ex = Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.Sum()"));
        Assert.That(ex!.Message, Does.Contain("Sum()").And.Contain("numeric"));
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

        var groupA = list.Cast<Dictionary<string, object?>>().First(g => (string)g["Key"]! == "A");
        var groupAItems = groupA["Items"];
        Assert.That(groupAItems, Is.InstanceOf<IList>());
        Assert.That(groupAItems, Has.Count.EqualTo(2));
    }

    [Test]
    public void GroupBy_ResultHasKeyAndItems()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6 });

        var result = engine.Evaluate("numbers.GroupBy(x => x > 3).ToList()");
        Assert.That(result, Is.InstanceOf<IList>());
        var list = (IList)result!;
        Assert.That(list, Has.Count.EqualTo(2));

        foreach (var group in list.Cast<Dictionary<string, object?>>())
        {
            Assert.That(group.ContainsKey("Key"), Is.True);
            Assert.That(group.ContainsKey("Items"), Is.True);
        }
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

        var first = result![0] as Dictionary<string, object?>;
        Assert.That(first!["First"], Is.EqualTo("Alice"));
        Assert.That(first["Second"], Is.EqualTo(30));
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

    #region Alias Tests (CsEval-specific syntax, cannot be parity tested)

    [Test]
    public void Filter_Alias_WorksAsWhere()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3, 4].filter(x => x > 2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
    }

    [Test]
    public void Map_Alias_WorksAsSelect()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3].map(x => x * 2).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    [Test]
    public void FlatMap_Alias_WorksAsSelectMany()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nested", new List<List<int>> {
            new() { 1, 2 },
            new() { 3, 4 }
        });

        var result = engine.Evaluate("nested.flatMap(x => x).ToList()");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Reduce_Alias_WithoutSeed()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3].reduce((a, b) => a + b)");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Reduce_Alias_WithSeed_JsStyle()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // JS style: reduce(fn, seed) - function first, seed second
        var result = engine.Evaluate("[1, 2, 3].reduce((acc, x) => acc + x, 10)");
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void Find_Alias_WorksAsFirstOrDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("[1, 2, 3].find(x => x > 1)"), Is.EqualTo(2));
        Assert.That(engine.Evaluate("[1, 2, 3].find(x => x > 5)"), Is.Null);
    }

    [Test]
    public void Some_Alias_WorksAsAny()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("[1, 2, 3].some(x => x > 2)"), Is.True);
        Assert.That(engine.Evaluate("[1, 2, 3].some(x => x > 5)"), Is.False);
    }

    [Test]
    public void Every_Alias_WorksAsAll()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("[2, 4, 6].every(x => x > 0)"), Is.True);
        Assert.That(engine.Evaluate("[1, 2, 3].every(x => x > 1)"), Is.False);
    }

    [Test]
    public void Includes_Alias_WorksAsContains()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("[1, 2, 3].includes(2)"), Is.True);
        Assert.That(engine.Evaluate("[1, 2, 3].includes(5)"), Is.False);
    }

    #endregion
}
