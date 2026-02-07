using CsEval.TestData.Data;

namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ForEachLoopTests(CompilationMode mode)
{
    [TestCaseSource(typeof(ForEachLoopData), nameof(ForEachLoopData.ValueCases))]
    public void Eval_ForEachLoop(string expr, object expected)
    {
        // Note: No C# parity check because collection expression syntax [1, 2, 3] in foreach context
        // isn't supported by Roslyn scripting (CS9176: There is no target type for the collection expression)
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
    }

    #region ForEach with External Collections

    [Test]
    public void ForEachLoop_WithExternalList_IteratesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var item in items) {
                sum = sum + item;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForEachLoop_WithExternalArray_IteratesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new int[] { 10, 20, 30 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in numbers) {
                sum = sum + n;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void ForEachLoop_WithStringCollection_IteratesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("names", new List<string> { "Alice", "Bob", "Charlie" });

        var result = engine.Evaluate(@"
        {
            var concat = """";
            foreach (var name in names) {
                concat = concat + name + "","";
            }
            return concat;
        }");

        Assert.That(result, Is.EqualTo("Alice,Bob,Charlie,"));
    }

    #endregion

    #region ForEach with LINQ Results

    [Test]
    public void ForEachLoop_WithLinqWhere_IteratesFilteredItems()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in numbers.Where(x => x % 2 == 0)) {
                sum = sum + n;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ForEachLoop_WithLinqSelect_IteratesTransformedItems()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in numbers.Select(x => x * 10)) {
                sum = sum + n;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void ForEachLoop_WithLinqTake_IteratesLimitedItems()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5 });

        var result = engine.Evaluate(@"
        {
            var count = 0;
            foreach (var n in numbers.Take(3)) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region ForEach with Return and External Variables

    [Test]
    public void ForEachLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("target", 7);
        engine.SetVariable("items", new List<int> { 1, 3, 5, 7, 9 });

        var result = engine.Evaluate(@"
        {
            foreach (var item in items) {
                if (item == target) {
                    return $""Found: {item}"";
                }
            }
            return ""Not found"";
        }");

        Assert.That(result, Is.EqualTo("Found: 7"));
    }

    #endregion

    #region ForEach with Various Types

    [Test]
    public void ForEachLoop_WithObjectList_AccessesProperties()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var items = [
                new { Name = ""Alice"", Age = 25 },
                new { Name = ""Bob"", Age = 30 },
                new { Name = ""Charlie"", Age = 35 }
            ];
            var totalAge = 0;
            foreach (var person in items) {
                totalAge = totalAge + person[""Age""];
            }
            return totalAge;
        }");

        Assert.That(result, Is.EqualTo(90));
    }

    #endregion

    #region ForEach with Anonymous Objects

    [Test]
    public void ForEachLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            object lastObj = null;
            foreach (var i in [0, 1, 2]) {
                lastObj = new { Index = i, Squared = i * i };
            }
            return lastObj;
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Index"], Is.EqualTo(2));
        Assert.That(result["Squared"], Is.EqualTo(4));
    }

    [Test]
    public void ForEachLoop_CollectingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var collected = [];
            foreach (var i in [1, 2, 3]) {
                collected = [..collected, new { Value = i, Double = i * 2 }];
            }
            return collected;
        }");

        Assert.That(result, Is.TypeOf<System.Dynamic.ExpandoObject[]>());
        var list = (System.Collections.IList)result!;
        Assert.That(list.Count, Is.EqualTo(3));
    }

    #endregion

    #region ForEach Loop Safety

    [Test]
    public void ForEachLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 5 });
        engine.SetVariable("items", Enumerable.Range(1, 100).ToList());

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var sum = 0;
                foreach (var item in items) {
                    sum = sum + item;
                }
                return sum;
            }"));

        Assert.That(ex!.Message, Does.Contain("5"));
    }

    [Test]
    public void ForEachLoop_WithDisabledLimit_AllowsManyIterations()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
        engine.SetVariable("items", Enumerable.Range(1, 10000).ToList());

        var result = engine.Evaluate(@"
        {
            var count = 0;
            foreach (var item in items) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(10000));
    }

    [Test]
    public void ForEachLoop_WithCancellationToken_CanBeCancelled()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
        engine.SetVariable("items", Enumerable.Range(1, 100000000).ToList());
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate(@"
            {
                var sum = 0;
                foreach (var item in items) {
                    sum = sum + item;
                }
                return sum;
            }", null, cts.Token);
        });

        Thread.Sleep(100);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    #endregion

    #region ForEach Error Cases

    [Test]
    public void ForEachLoop_NonEnumerable_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("notIterable", 42);

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var item in notIterable) {
                }
                return 0;
            }"));

        Assert.That(ex!.Message, Does.Contain("Cannot iterate"));
    }

    [Test]
    public void ForEachLoop_NullCollection_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("nullCollection", null);

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                foreach (var item in nullCollection) {
                }
                return 0;
            }"));

        Assert.That(ex!.Message, Does.Contain("Cannot iterate"));
    }

    #endregion

    #region ForEach Parsing Tests

    [Test]
    public void ForEachLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ foreach (var item in [1,2,3]) { } return 0; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void ForEachLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var sum = 0;
            foreach (var item in items) {
                sum = sum + item;
            }
            return sum;
        }");

        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(6));

        engine.SetVariable("items", new List<int> { 10, 20, 30 });
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(60));
    }

    #endregion
}
