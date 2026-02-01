namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LinqTests(CompilationMode mode) 
{
    #region Where

    [Test]
    public void Where_WithPredicate_FiltersElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Where((x) => x > 2)") as IList;
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Is.EqualTo(new[] { 3, 4, 5 }));
    }

    [Test]
    public void Where_WithoutParens_FiltersElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Where(x => x > 2)") as IList;
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Where_EmptyResult_ReturnsEmptyList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Where(x => x > 10)") as IList;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Filter_Alias_WorksAsWhere()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3, 4].filter(x => x > 2)") as IList;
        Assert.That(result, Is.EqualTo(new[] { 3, 4 }));
    }

    #endregion

    #region Select

    [Test]
    public void Select_WithSelector_ProjectsElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Select((x) => x * 2)") as IList;
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    [Test]
    public void Select_WithMemberAccess_ProjectsProperty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<object> {
            new { Name = "Alice" },
            new { Name = "Bob" }
        });

        var result = engine.Evaluate("items.Select(x => x.Name)") as IList;
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Bob" }));
    }

    [Test]
    public void Map_Alias_WorksAsSelect()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3].map(x => x * 2)") as IList;
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    #endregion

    #region SelectMany

    [Test]
    public void SelectMany_FlattensNestedCollections()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nested", new List<List<int>> {
            new() { 1, 2 },
            new() { 3, 4 }
        });

        var result = engine.Evaluate("nested.SelectMany(x => x)") as IList;
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void SelectMany_WithProjection_FlattensAndProjects()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Tags"] = new List<string> { "a", "b" } },
            new() { ["Tags"] = new List<string> { "c" } }
        });

        var result = engine.Evaluate("items.SelectMany(x => x.Tags)") as IList;
        Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void FlatMap_Alias_WorksAsSelectMany()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nested", new List<List<int>> {
            new() { 1, 2 },
            new() { 3, 4 }
        });

        var result = engine.Evaluate("nested.flatMap(x => x)") as IList;
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3, 4 }));
    }

    #endregion

    #region Aggregate

    [Test]
    public void Aggregate_WithSeed_ReducesCollection()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Aggregate(0, (acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Aggregate_WithoutSeed_ReducesCollection()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4 });

        var result = engine.Evaluate("numbers.Aggregate((acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Aggregate_StringConcat_ConcatenatesStrings()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("words", new List<string> { "a", "b", "c" });

        var result = engine.Evaluate("words.Aggregate(\"\", (acc, x) => acc + x)");
        Assert.That(result, Is.EqualTo("abc"));
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

    #endregion

    #region First / FirstOrDefault

    [Test]
    public void First_ReturnsFirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.First()");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void First_WithPredicate_ReturnsFirstMatching()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.First(x => x > 3)");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void First_EmptyCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.First()"));
    }

    [Test]
    public void FirstOrDefault_ReturnsFirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.FirstOrDefault()");
        Assert.That(result, Is.EqualTo(1));
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
    public void Find_Alias_WorksAsFirstOrDefault()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("[1, 2, 3].find(x => x > 1)"), Is.EqualTo(2));
        Assert.That(engine.Evaluate("[1, 2, 3].find(x => x > 5)"), Is.Null);
    }

    #endregion

    #region Last / LastOrDefault

    [Test]
    public void Last_ReturnsLastElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Last()");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Last_WithPredicate_ReturnsLastMatching()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Last(x => x < 4)");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void LastOrDefault_EmptyCollection_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.LastOrDefault()");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Single / SingleOrDefault

    [Test]
    public void Single_SingleElement_ReturnsIt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 42 });

        var result = engine.Evaluate("numbers.Single()");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Single_MultipleElements_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("numbers.Single()"));
    }

    [Test]
    public void Single_WithPredicate_ReturnsMatching()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Single(x => x == 2)");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void SingleOrDefault_EmptyCollection_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.SingleOrDefault()");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Any / All

    [Test]
    public void Any_NonEmpty_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Any()");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Any_Empty_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.Any()");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Any_WithPredicate_MatchExists_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Any(x => x > 2)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Any_WithPredicate_NoMatch_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Any(x => x > 10)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void All_AllMatch_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 2, 4, 6 });

        var result = engine.Evaluate("numbers.All(x => x > 0)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void All_SomeDontMatch_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.All(x => x > 1)");
        Assert.That(result, Is.False);
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

    #endregion

    #region Count

    [Test]
    public void Count_ReturnsElementCount()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Count()");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Count_WithPredicate_ReturnsMatchingCount()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Count(x => x > 2)");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Count_EmptyCollection_ReturnsZero()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int>());

        var result = engine.Evaluate("numbers.Count()");
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region Sum / Average

    [Test]
    public void Sum_ReturnsSum()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Sum()");
        Assert.That(result, Is.EqualTo(15));
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
    public void Average_ReturnsAverage()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 10, 20, 30 });

        var result = engine.Evaluate("numbers.Average()");
        Assert.That(result, Is.EqualTo(20.0));
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

    #endregion

    #region Min / Max / MinBy / MaxBy

    [Test]
    public void Min_ReturnsMinimum()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 5, 2, 8, 1, 9 });

        var result = engine.Evaluate("numbers.Min()");
        Assert.That(result, Is.EqualTo(1));
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
    public void Max_ReturnsMaximum()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 5, 2, 8, 1, 9 });

        var result = engine.Evaluate("numbers.Max()");
        Assert.That(result, Is.EqualTo(9));
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
    public void MinBy_EmptyCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.MinBy(x => x)"));
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
    public void MaxBy_EmptyCollection_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => engine.Evaluate("items.MaxBy(x => x)"));
    }

    #endregion

    #region OrderBy / OrderByDescending

    [Test]
    public void OrderBy_SortsAscending()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 3, 1, 4, 1, 5 });

        var result = engine.Evaluate("numbers.OrderBy(x => x)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 1, 3, 4, 5 }));
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

        var result = engine.Evaluate("items.OrderBy(x => x.Name).Select(x => x.Name)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { "Alice", "Bob", "Charlie" }));
    }

    [Test]
    public void OrderByDescending_SortsDescending()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 3, 1, 4, 1, 5 });

        var result = engine.Evaluate("numbers.OrderByDescending(x => x)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 5, 4, 3, 1, 1 }));
    }

    #endregion

    #region GroupBy

    [Test]
    public void GroupBy_GroupsByKey()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<Dictionary<string, object?>> {
            new() { ["Category"] = "A", ["Value"] = 1 },
            new() { ["Category"] = "B", ["Value"] = 2 },
            new() { ["Category"] = "A", ["Value"] = 3 }
        });

        var result = engine.Evaluate("items.GroupBy(x => x.Category)") as IList;
        Assert.That(result, Has.Count.EqualTo(2));

        var groupA = result!.Cast<Dictionary<string, object?>>().First(g => (string)g["Key"]! == "A");
        var groupAItems = groupA["Items"] as IList;
        Assert.That(groupAItems, Has.Count.EqualTo(2));
    }

    [Test]
    public void GroupBy_ResultHasKeyAndItems()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6 });

        var result = engine.Evaluate("numbers.GroupBy(x => x > 3)") as IList;
        Assert.That(result, Has.Count.EqualTo(2));

        foreach (var group in result!.Cast<Dictionary<string, object?>>())
        {
            Assert.That(group.ContainsKey("Key"), Is.True);
            Assert.That(group.ContainsKey("Items"), Is.True);
        }
    }

    #endregion

    #region Zip

    [Test]
    public void Zip_WithSelector_CombinesElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nums1", new List<int> { 1, 2, 3 });
        engine.SetVariable("nums2", new List<int> { 10, 20, 30 });

        var result = engine.Evaluate("nums1.Zip(nums2, (a, b) => a + b)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 11, 22, 33 }));
    }

    [Test]
    public void Zip_WithoutSelector_ReturnsTuples()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("names", new List<string> { "Alice", "Bob" });
        engine.SetVariable("ages", new List<int> { 30, 25 });

        var result = engine.Evaluate("names.Zip(ages)") as IList;
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

        var result = engine.Evaluate("shortList.Zip(longList, (a, b) => a + b)") as IList;
        Assert.That(result, Has.Count.EqualTo(2));
    }

    #endregion

    #region Distinct / Take / Skip

    [Test]
    public void Distinct_RemovesDuplicates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 2, 3, 3, 3 });

        var result = engine.Evaluate("numbers.Distinct()") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3 }));
    }

    [Test]
    public void Take_ReturnsFirstN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Take(3)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3 }));
    }

    [Test]
    public void Take_MoreThanCount_ReturnsAll()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2 });

        var result = engine.Evaluate("numbers.Take(10)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2 }));
    }

    [Test]
    public void Skip_SkipsFirstN()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Skip(2)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 3, 4, 5 }));
    }

    [Test]
    public void Skip_MoreThanCount_ReturnsEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2 });

        var result = engine.Evaluate("numbers.Skip(10)") as IList;
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Contains / Reverse

    [Test]
    public void Contains_ElementExists_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Contains(2)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_ElementNotExists_ReturnsFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Contains(5)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_StringElement_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("names", new List<string> { "Alice", "Bob", "Charlie" });

        var result = engine.Evaluate("names.Contains(\"Bob\")");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Includes_Alias_WorksAsContains()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.That(engine.Evaluate("[1, 2, 3].includes(2)"), Is.True);
        Assert.That(engine.Evaluate("[1, 2, 3].includes(5)"), Is.False);
    }

    [Test]
    public void Reverse_ReversesOrder()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.Reverse()") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 3, 2, 1 }));
    }

    #endregion

    #region Set Operations (Except / Intersect / Union)

    [Test]
    public void Except_ReturnsElementsNotInSecond()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3, 4, 5 });
        engine.SetVariable("second", new List<int> { 3, 4, 5, 6, 7 });

        var result = engine.Evaluate("first.Except(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2 }));
    }

    [Test]
    public void Except_WithNoOverlap_ReturnsAll()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3 });
        engine.SetVariable("second", new List<int> { 4, 5, 6 });

        var result = engine.Evaluate("first.Except(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3 }));
    }

    [Test]
    public void Except_WithFullOverlap_ReturnsEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3 });
        engine.SetVariable("second", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("first.Except(second)") as IList;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Except_WithStrings_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<string> { "a", "b", "c" });
        engine.SetVariable("second", new List<string> { "b", "d" });

        var result = engine.Evaluate("first.Except(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { "a", "c" }));
    }

    [Test]
    public void Intersect_ReturnsCommonElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3, 4, 5 });
        engine.SetVariable("second", new List<int> { 3, 4, 5, 6, 7 });

        var result = engine.Evaluate("first.Intersect(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 3, 4, 5 }));
    }

    [Test]
    public void Intersect_WithNoOverlap_ReturnsEmpty()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3 });
        engine.SetVariable("second", new List<int> { 4, 5, 6 });

        var result = engine.Evaluate("first.Intersect(second)") as IList;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Intersect_WithStrings_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<string> { "a", "b", "c" });
        engine.SetVariable("second", new List<string> { "b", "c", "d" });

        var result = engine.Evaluate("first.Intersect(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { "b", "c" }));
    }

    [Test]
    public void Union_ReturnsCombinedWithoutDuplicates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3 });
        engine.SetVariable("second", new List<int> { 3, 4, 5 });

        var result = engine.Evaluate("first.Union(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Union_WithNoOverlap_ReturnsCombined()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2 });
        engine.SetVariable("second", new List<int> { 3, 4 });

        var result = engine.Evaluate("first.Union(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void Union_WithFullOverlap_ReturnsDistinct()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2, 3 });
        engine.SetVariable("second", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("first.Union(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3 }));
    }

    [Test]
    public void Union_WithStrings_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<string> { "a", "b" });
        engine.SetVariable("second", new List<string> { "b", "c" });

        var result = engine.Evaluate("first.Union(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { "a", "b", "c" }));
    }

    #endregion

    #region ToList / ToArray / Concat

    [Test]
    public void ToList_ReturnsList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.ToList()");
        Assert.That(result, Is.TypeOf<List<int>>());
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ToArray_ReturnsArray()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate("numbers.ToArray()");
        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Concat_CombinesSequences()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", new List<int> { 1, 2 });
        engine.SetVariable("second", new List<int> { 3, 4 });

        var result = engine.Evaluate("first.Concat(second)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 1, 2, 3, 4 }));
    }

    #endregion

    #region Chained Operations

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

        var result = engine.Evaluate("items.Where(x => x.Price > 1).OrderBy(x => x.Name).Select(x => x.Name)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { "Apple", "Mango", "Orange" }));
    }

    [Test]
    public void Chained_SelectWhereTake()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate("numbers.Select(x => x * 2).Where(x => x > 4).Take(2)") as IList;
        Assert.That(result, Is.EqualTo(new object[] { 6, 8 }));
    }

    #endregion
}
