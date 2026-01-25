namespace CsEval.Test.Evaluator;

[TestFixture]
public class LinqTests : EvaluatorTestBase
{
    #region Where

    [Test]
    public void Where_WithPredicate_FiltersElements()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where((x) => x > 2)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Is.EqualTo(new List<object?> { 3, 4, 5 }));
    }

    [Test]
    public void Where_WithoutParens_FiltersElements()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where(x => x > 2)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Where_EmptyResult_ReturnsEmptyList()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Where(x => x > 10)", context) as List<object?>;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Filter_Alias_WorksAsWhere()
    {
        var result = Eval("[1, 2, 3, 4].filter(x => x > 2)") as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 3, 4 }));
    }

    #endregion

    #region Select

    [Test]
    public void Select_WithSelector_ProjectsElements()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Select((x) => x * 2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 2, 4, 6 }));
    }

    [Test]
    public void Select_WithMemberAccess_ProjectsProperty()
    {
        var context = new EvalContext();
        context.Define("items", new List<object> {
            new { Name = "Alice" },
            new { Name = "Bob" }
        });

        var result = Eval("items.Select(x => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Alice", "Bob" }));
    }

    [Test]
    public void Map_Alias_WorksAsSelect()
    {
        var result = Eval("[1, 2, 3].map(x => x * 2)") as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 2, 4, 6 }));
    }

    #endregion

    #region SelectMany

    [Test]
    public void SelectMany_FlattensNestedCollections()
    {
        var context = new EvalContext();
        context.Define("nested", new List<List<int>> {
            new() { 1, 2 },
            new() { 3, 4 }
        });

        var result = Eval("nested.SelectMany(x => x)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3, 4 }));
    }

    [Test]
    public void SelectMany_WithProjection_FlattensAndProjects()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Tags"] = new List<string> { "a", "b" } },
            new() { ["Tags"] = new List<string> { "c" } }
        });

        var result = Eval("items.SelectMany(x => x.Tags)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "a", "b", "c" }));
    }

    [Test]
    public void FlatMap_Alias_WorksAsSelectMany()
    {
        var context = new EvalContext();
        context.Define("nested", new List<List<int>> {
            new() { 1, 2 },
            new() { 3, 4 }
        });

        var result = Eval("nested.flatMap(x => x)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3, 4 }));
    }

    #endregion

    #region Aggregate

    [Test]
    public void Aggregate_WithSeed_ReducesCollection()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4 });

        var result = Eval("numbers.Aggregate(0, (acc, x) => acc + x)", context);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Aggregate_WithoutSeed_ReducesCollection()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4 });

        var result = Eval("numbers.Aggregate((acc, x) => acc + x)", context);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Aggregate_StringConcat_ConcatenatesStrings()
    {
        var context = new EvalContext();
        context.Define("words", new List<string> { "a", "b", "c" });

        var result = Eval("words.Aggregate(\"\", (acc, x) => acc + x)", context);
        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test]
    public void Reduce_Alias_WithoutSeed()
    {
        var result = Eval("[1, 2, 3].reduce((a, b) => a + b)");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Reduce_Alias_WithSeed_JsStyle()
    {
        // JS style: reduce(fn, seed) - function first, seed second
        var result = Eval("[1, 2, 3].reduce((acc, x) => acc + x, 10)");
        Assert.That(result, Is.EqualTo(16));
    }

    #endregion

    #region First / FirstOrDefault

    [Test]
    public void First_ReturnsFirstElement()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.First()", context);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void First_WithPredicate_ReturnsFirstMatching()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.First(x => x > 3)", context);
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void First_EmptyCollection_Throws()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int>());

        Assert.Throws<InvalidOperationException>(() => Eval("numbers.First()", context));
    }

    [Test]
    public void FirstOrDefault_ReturnsFirstElement()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.FirstOrDefault()", context);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void FirstOrDefault_EmptyCollection_ReturnsNull()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int>());

        var result = Eval("numbers.FirstOrDefault()", context);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FirstOrDefault_WithPredicate_NoMatch_ReturnsNull()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.FirstOrDefault(x => x > 10)", context);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Find_Alias_WorksAsFirstOrDefault()
    {
        Assert.That(Eval("[1, 2, 3].find(x => x > 1)"), Is.EqualTo(2));
        Assert.That(Eval("[1, 2, 3].find(x => x > 5)"), Is.Null);
    }

    #endregion

    #region Last / LastOrDefault

    [Test]
    public void Last_ReturnsLastElement()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Last()", context);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Last_WithPredicate_ReturnsLastMatching()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Last(x => x < 4)", context);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void LastOrDefault_EmptyCollection_ReturnsNull()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int>());

        var result = Eval("numbers.LastOrDefault()", context);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Single / SingleOrDefault

    [Test]
    public void Single_SingleElement_ReturnsIt()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 42 });

        var result = Eval("numbers.Single()", context);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Single_MultipleElements_Throws()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        Assert.Throws<InvalidOperationException>(() => Eval("numbers.Single()", context));
    }

    [Test]
    public void Single_WithPredicate_ReturnsMatching()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Single(x => x == 2)", context);
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void SingleOrDefault_EmptyCollection_ReturnsNull()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int>());

        var result = Eval("numbers.SingleOrDefault()", context);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Any / All

    [Test]
    public void Any_NonEmpty_ReturnsTrue()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Any()", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Any_Empty_ReturnsFalse()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int>());

        var result = Eval("numbers.Any()", context);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Any_WithPredicate_MatchExists_ReturnsTrue()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Any(x => x > 2)", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Any_WithPredicate_NoMatch_ReturnsFalse()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Any(x => x > 10)", context);
        Assert.That(result, Is.False);
    }

    [Test]
    public void All_AllMatch_ReturnsTrue()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 2, 4, 6 });

        var result = Eval("numbers.All(x => x > 0)", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void All_SomeDontMatch_ReturnsFalse()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.All(x => x > 1)", context);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Some_Alias_WorksAsAny()
    {
        Assert.That(Eval("[1, 2, 3].some(x => x > 2)"), Is.True);
        Assert.That(Eval("[1, 2, 3].some(x => x > 5)"), Is.False);
    }

    [Test]
    public void Every_Alias_WorksAsAll()
    {
        Assert.That(Eval("[2, 4, 6].every(x => x > 0)"), Is.True);
        Assert.That(Eval("[1, 2, 3].every(x => x > 1)"), Is.False);
    }

    #endregion

    #region Count

    [Test]
    public void Count_ReturnsElementCount()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Count()", context);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Count_WithPredicate_ReturnsMatchingCount()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Count(x => x > 2)", context);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Count_EmptyCollection_ReturnsZero()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int>());

        var result = Eval("numbers.Count()", context);
        Assert.That(result, Is.EqualTo(0));
    }

    #endregion

    #region Sum / Average

    [Test]
    public void Sum_ReturnsSum()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Sum()", context);
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Sum_WithSelector_ReturnsSumOfSelected()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 },
            new() { ["Value"] = 30 }
        });

        var result = Eval("items.Sum(x => x.Value)", context);
        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void Sum_WithStrings_ThrowsException()
    {
        var context = new EvalContext();
        context.Define("strings", new List<string> { "a", "b", "c" });

        var ex = Assert.Throws<InvalidOperationException>(() => Eval("strings.Sum()", context));
        Assert.That(ex!.Message, Does.Contain("Sum()").And.Contain("numeric"));
    }

    [Test]
    public void Sum_WithMixedNonNumeric_ThrowsException()
    {
        var context = new EvalContext();
        context.Define("items", new List<object> { "hello", "world" });

        var ex = Assert.Throws<InvalidOperationException>(() => Eval("items.Sum()", context));
        Assert.That(ex!.Message, Does.Contain("Sum()").And.Contain("numeric"));
    }

    [Test]
    public void Average_ReturnsAverage()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 10, 20, 30 });

        var result = Eval("numbers.Average()", context);
        Assert.That(result, Is.EqualTo(20.0));
    }

    [Test]
    public void Average_WithSelector_ReturnsAverageOfSelected()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 }
        });

        var result = Eval("items.Average(x => x.Value)", context);
        Assert.That(result, Is.EqualTo(15.0));
    }

    #endregion

    #region Min / Max / MinBy / MaxBy

    [Test]
    public void Min_ReturnsMinimum()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 5, 2, 8, 1, 9 });

        var result = Eval("numbers.Min()", context);
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Min_WithSelector_ReturnsMinimumOfSelected()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 30 },
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 }
        });

        var result = Eval("items.Min(x => x.Value)", context);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Max_ReturnsMaximum()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 5, 2, 8, 1, 9 });

        var result = Eval("numbers.Max()", context);
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void Max_WithSelector_ReturnsMaximumOfSelected()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Value"] = 30 },
            new() { ["Value"] = 10 },
            new() { ["Value"] = 20 }
        });

        var result = Eval("items.Max(x => x.Value)", context);
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void MinBy_ReturnsElementWithMinimumKey()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Bob", ["Age"] = 30 },
            new() { ["Name"] = "Alice", ["Age"] = 25 },
            new() { ["Name"] = "Charlie", ["Age"] = 35 }
        });

        var result = Eval("items.MinBy(x => x.Age)", context) as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Alice"));
        Assert.That(result["Age"], Is.EqualTo(25));
    }

    [Test]
    public void MinBy_WithStrings_ReturnsElementWithMinimumKey()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Charlie", ["Value"] = 3 },
            new() { ["Name"] = "Alice", ["Value"] = 1 },
            new() { ["Name"] = "Bob", ["Value"] = 2 }
        });

        var result = Eval("items.MinBy(x => x.Name)", context) as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Alice"));
    }

    [Test]
    public void MinBy_EmptyCollection_Throws()
    {
        var context = new EvalContext();
        context.Define("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => Eval("items.MinBy(x => x)", context));
    }

    [Test]
    public void MaxBy_ReturnsElementWithMaximumKey()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Bob", ["Age"] = 30 },
            new() { ["Name"] = "Alice", ["Age"] = 25 },
            new() { ["Name"] = "Charlie", ["Age"] = 35 }
        });

        var result = Eval("items.MaxBy(x => x.Age)", context) as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Charlie"));
        Assert.That(result["Age"], Is.EqualTo(35));
    }

    [Test]
    public void MaxBy_WithStrings_ReturnsElementWithMaximumKey()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Alice", ["Value"] = 1 },
            new() { ["Name"] = "Charlie", ["Value"] = 3 },
            new() { ["Name"] = "Bob", ["Value"] = 2 }
        });

        var result = Eval("items.MaxBy(x => x.Name)", context) as Dictionary<string, object?>;
        Assert.That(result!["Name"], Is.EqualTo("Charlie"));
    }

    [Test]
    public void MaxBy_EmptyCollection_Throws()
    {
        var context = new EvalContext();
        context.Define("items", new List<int>());

        Assert.Throws<InvalidOperationException>(() => Eval("items.MaxBy(x => x)", context));
    }

    #endregion

    #region OrderBy / OrderByDescending

    [Test]
    public void OrderBy_SortsAscending()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 3, 1, 4, 1, 5 });

        var result = Eval("numbers.OrderBy(x => x)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 1, 3, 4, 5 }));
    }

    [Test]
    public void OrderBy_WithPropertySelector_SortsByProperty()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Name"] = "Charlie" },
            new() { ["Name"] = "Alice" },
            new() { ["Name"] = "Bob" }
        });

        var result = Eval("items.OrderBy(x => x.Name).Select(x => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Alice", "Bob", "Charlie" }));
    }

    [Test]
    public void OrderByDescending_SortsDescending()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 3, 1, 4, 1, 5 });

        var result = Eval("numbers.OrderByDescending(x => x)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 5, 4, 3, 1, 1 }));
    }

    #endregion

    #region GroupBy

    [Test]
    public void GroupBy_GroupsByKey()
    {
        var context = new EvalContext();
        context.Define("items", new List<Dictionary<string, object?>> {
            new() { ["Category"] = "A", ["Value"] = 1 },
            new() { ["Category"] = "B", ["Value"] = 2 },
            new() { ["Category"] = "A", ["Value"] = 3 }
        });

        var result = Eval("items.GroupBy(x => x.Category)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(2));

        var groupA = result!.Cast<Dictionary<string, object?>>().First(g => (string)g["Key"]! == "A");
        var groupAItems = groupA["Items"] as List<object?>;
        Assert.That(groupAItems, Has.Count.EqualTo(2));
    }

    [Test]
    public void GroupBy_ResultHasKeyAndItems()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5, 6 });

        var result = Eval("numbers.GroupBy(x => x > 3)", context) as List<object?>;
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
        var context = new EvalContext();
        context.Define("nums1", new List<int> { 1, 2, 3 });
        context.Define("nums2", new List<int> { 10, 20, 30 });

        var result = Eval("nums1.Zip(nums2, (a, b) => a + b)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 11, 22, 33 }));
    }

    [Test]
    public void Zip_WithoutSelector_ReturnsTuples()
    {
        var context = new EvalContext();
        context.Define("names", new List<string> { "Alice", "Bob" });
        context.Define("ages", new List<int> { 30, 25 });

        var result = Eval("names.Zip(ages)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(2));

        var first = result![0] as Dictionary<string, object?>;
        Assert.That(first!["First"], Is.EqualTo("Alice"));
        Assert.That(first["Second"], Is.EqualTo(30));
    }

    [Test]
    public void Zip_DifferentLengths_StopsAtShorter()
    {
        var context = new EvalContext();
        context.Define("shortList", new List<int> { 1, 2 });
        context.Define("longList", new List<int> { 10, 20, 30, 40 });

        var result = Eval("shortList.Zip(longList, (a, b) => a + b)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(2));
    }

    #endregion

    #region Distinct / Take / Skip

    [Test]
    public void Distinct_RemovesDuplicates()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 2, 3, 3, 3 });

        var result = Eval("numbers.Distinct()", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3 }));
    }

    [Test]
    public void Take_ReturnsFirstN()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Take(3)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3 }));
    }

    [Test]
    public void Take_MoreThanCount_ReturnsAll()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2 });

        var result = Eval("numbers.Take(10)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2 }));
    }

    [Test]
    public void Skip_SkipsFirstN()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Skip(2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 3, 4, 5 }));
    }

    [Test]
    public void Skip_MoreThanCount_ReturnsEmpty()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2 });

        var result = Eval("numbers.Skip(10)", context) as List<object?>;
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Contains / Reverse

    [Test]
    public void Contains_ElementExists_ReturnsTrue()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Contains(2)", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_ElementNotExists_ReturnsFalse()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Contains(5)", context);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_StringElement_Works()
    {
        var context = new EvalContext();
        context.Define("names", new List<string> { "Alice", "Bob", "Charlie" });

        var result = Eval("names.Contains(\"Bob\")", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Includes_Alias_WorksAsContains()
    {
        Assert.That(Eval("[1, 2, 3].includes(2)"), Is.True);
        Assert.That(Eval("[1, 2, 3].includes(5)"), Is.False);
    }

    [Test]
    public void Reverse_ReversesOrder()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.Reverse()", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 3, 2, 1 }));
    }

    #endregion

    #region Set Operations (Except / Intersect / Union)

    [Test]
    public void Except_ReturnsElementsNotInSecond()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3, 4, 5 });
        context.Define("second", new List<int> { 3, 4, 5, 6, 7 });

        var result = Eval("first.Except(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2 }));
    }

    [Test]
    public void Except_WithNoOverlap_ReturnsAll()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3 });
        context.Define("second", new List<int> { 4, 5, 6 });

        var result = Eval("first.Except(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3 }));
    }

    [Test]
    public void Except_WithFullOverlap_ReturnsEmpty()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3 });
        context.Define("second", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("first.Except(second)", context) as List<object?>;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Except_WithStrings_Works()
    {
        var context = new EvalContext();
        context.Define("first", new List<string> { "a", "b", "c" });
        context.Define("second", new List<string> { "b", "d" });

        var result = Eval("first.Except(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "a", "c" }));
    }

    [Test]
    public void Intersect_ReturnsCommonElements()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3, 4, 5 });
        context.Define("second", new List<int> { 3, 4, 5, 6, 7 });

        var result = Eval("first.Intersect(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 3, 4, 5 }));
    }

    [Test]
    public void Intersect_WithNoOverlap_ReturnsEmpty()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3 });
        context.Define("second", new List<int> { 4, 5, 6 });

        var result = Eval("first.Intersect(second)", context) as List<object?>;
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Intersect_WithStrings_Works()
    {
        var context = new EvalContext();
        context.Define("first", new List<string> { "a", "b", "c" });
        context.Define("second", new List<string> { "b", "c", "d" });

        var result = Eval("first.Intersect(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "b", "c" }));
    }

    [Test]
    public void Union_ReturnsCombinedWithoutDuplicates()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3 });
        context.Define("second", new List<int> { 3, 4, 5 });

        var result = Eval("first.Union(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void Union_WithNoOverlap_ReturnsCombined()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2 });
        context.Define("second", new List<int> { 3, 4 });

        var result = Eval("first.Union(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3, 4 }));
    }

    [Test]
    public void Union_WithFullOverlap_ReturnsDistinct()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2, 3 });
        context.Define("second", new List<int> { 1, 2, 3 });

        var result = Eval("first.Union(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3 }));
    }

    [Test]
    public void Union_WithStrings_Works()
    {
        var context = new EvalContext();
        context.Define("first", new List<string> { "a", "b" });
        context.Define("second", new List<string> { "b", "c" });

        var result = Eval("first.Union(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "a", "b", "c" }));
    }

    #endregion

    #region ToList / ToArray / Concat

    [Test]
    public void ToList_ReturnsList()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.ToList()", context);
        Assert.That(result, Is.TypeOf<List<object?>>());
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3 }));
    }

    [Test]
    public void ToArray_ReturnsArray()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3 });

        var result = Eval("numbers.ToArray()", context);
        Assert.That(result, Is.TypeOf<object?[]>());
        Assert.That(result, Is.EqualTo(new object?[] { 1, 2, 3 }));
    }

    [Test]
    public void Concat_CombinesSequences()
    {
        var context = new EvalContext();
        context.Define("first", new List<int> { 1, 2 });
        context.Define("second", new List<int> { 3, 4 });

        var result = Eval("first.Concat(second)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 1, 2, 3, 4 }));
    }

    #endregion

    #region Chained Operations

    [Test]
    public void Chained_WhereSelectOrderBy()
    {
        var context = new EvalContext();
        context.Define("items", new List<object>
        {
            CreateItem("Apple", 1.5),
            CreateItem("Banana", 0.75),
            CreateItem("Orange", 2.0),
            CreateItem("Mango", 3.0)
        });

        var result = Eval("items.Where(x => x.Price > 1).OrderBy(x => x.Name).Select(x => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Apple", "Mango", "Orange" }));
    }

    [Test]
    public void Chained_SelectWhereTake()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Select(x => x * 2).Where(x => x > 4).Take(2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 6, 8 }));
    }

    #endregion
}
