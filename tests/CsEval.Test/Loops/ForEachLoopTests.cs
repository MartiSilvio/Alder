namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class ForEachLoopTests(CompilationMode mode) : TestBase
{
    #region Basic ForEach Loop

    [Test]
    public void ForEachLoop_BasicIteration_IteratesAllElements()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var item in [1, 2, 3, 4, 5]) {
                sum = sum + item;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForEachLoop_EmptyCollection_NeverExecutes()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var executed = false;
            foreach (var item in []) {
                executed = true;
            }
            return executed;
        }");

        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void ForEachLoop_SingleElement_ExecutesOnce()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var count = 0;
            foreach (var item in [42]) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ForEachLoop_ItemAccessible_InBody()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var lastItem = null;
            foreach (var item in [10, 20, 30]) {
                lastItem = item;
            }
            return lastItem;
        }");

        Assert.That(result, Is.EqualTo(30));
    }

    #endregion

    #region ForEach with External Collections

    [Test]
    public void ForEachLoop_WithExternalList_IteratesCorrectly()
    {
        var engine = CreateEngine(mode);
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
        var engine = CreateEngine(mode);
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
        var engine = CreateEngine(mode);
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
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in numbers.Where(x => x % 2 == 0)) {
                sum = sum + n;
            }
            return sum;
        }");

        // 2 + 4 + 6 + 8 + 10 = 30
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ForEachLoop_WithLinqSelect_IteratesTransformedItems()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<int> { 1, 2, 3 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in numbers.Select(x => x * 10)) {
                sum = sum + n;
            }
            return sum;
        }");

        // 10 + 20 + 30 = 60
        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void ForEachLoop_WithLinqTake_IteratesLimitedItems()
    {
        var engine = CreateEngine(mode);
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

    #region ForEach with Return

    [Test]
    public void ForEachLoop_WithEarlyReturn_ExitsImmediately()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            foreach (var item in [1, 2, 3, 4, 5]) {
                if (item == 3) {
                    return item;
                }
            }
            return -1;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ForEachLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = CreateEngine(mode);
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

    #region ForEach with Nested Structures

    [Test]
    public void ForEachLoop_WithNestedIf_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var evenSum = 0;
            foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) {
                if (item % 2 == 0) {
                    evenSum = evenSum + item;
                }
            }
            return evenSum;
        }");

        // 2 + 4 + 6 + 8 + 10 = 30
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void ForEachLoop_Nested_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            foreach (var i in [1, 2, 3]) {
                foreach (var j in [10, 20, 30]) {
                    total = total + i * j;
                }
            }
            return total;
        }");

        // (1*10 + 1*20 + 1*30) + (2*10 + 2*20 + 2*30) + (3*10 + 3*20 + 3*30)
        // = 60 + 120 + 180 = 360
        Assert.That(result, Is.EqualTo(360));
    }

    [Test]
    public void ForEachLoop_NestedWithFor_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            foreach (var item in [1, 2, 3]) {
                for (var i = 0; i < 3; i = i + 1) {
                    total = total + item;
                }
            }
            return total;
        }");

        // (1+1+1) + (2+2+2) + (3+3+3) = 3 + 6 + 9 = 18
        Assert.That(result, Is.EqualTo(18));
    }

    [Test]
    public void ForEachLoop_NestedWithWhile_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            foreach (var item in [1, 2, 3]) {
                var count = 0;
                while (count < 3) {
                    total = total + item;
                    count = count + 1;
                }
            }
            return total;
        }");

        // (1+1+1) + (2+2+2) + (3+3+3) = 18
        Assert.That(result, Is.EqualTo(18));
    }

    #endregion

    #region ForEach with Various Types

    [Test]
    public void ForEachLoop_WithMixedTypes_HandlesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var types = """";
            foreach (var item in [1, ""hello"", true, null]) {
                if (item == null) {
                    types = types + ""null,"";
                } else {
                    types = types + ""val,"";
                }
            }
            return types;
        }");

        Assert.That(result, Is.EqualTo("val,val,val,null,"));
    }

    [Test]
    public void ForEachLoop_WithObjectList_AccessesProperties()
    {
        var engine = CreateEngine(mode);
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

    #region ForEach with Strings

    [Test]
    public void ForEachLoop_StringConcatenation_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var str = """";
            foreach (var item in [1, 2, 3, 4, 5]) {
                str = str + item;
            }
            return str;
        }");

        Assert.That(result, Is.EqualTo("12345"));
    }

    [Test]
    public void ForEachLoop_InterpolatedString_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var lines = """";
            foreach (var i in [1, 2, 3]) {
                lines = $""{lines}Line {i}\n"";
            }
            return lines;
        }");

        Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3\n"));
    }

    #endregion

    #region ForEach with Anonymous Objects

    [Test]
    public void ForEachLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var lastObj = null;
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
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var collected = [];
            foreach (var i in [1, 2, 3]) {
                collected = [...collected, new { Value = i, Double = i * 2 }];
            }
            return collected;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(3));
    }

    #endregion

    #region ForEach Single Statement Body

    [Test]
    public void ForEachLoop_SingleStatementBody_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var item in [1, 2, 3, 4, 5])
                sum = sum + item;
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region ForEach Loop Safety

    [Test]
    public void ForEachLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = CreateEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 5 });
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
        var engine = CreateEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
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
        var engine = CreateEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
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
        var engine = CreateEngine(mode);
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
        var engine = CreateEngine(mode);
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
        var engine = CreateEngine(mode);
        var success = engine.TryParse("{ foreach (var item in [1,2,3]) { } return 0; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void ForEachLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = CreateEngine(mode);
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

    #region ForEach Loop with Break

    [Test]
    public void ForEachLoop_Break_ExitsLoop()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var lastItem = 0;
            foreach (var item in [1, 2, 3, 4, 5]) {
                lastItem = item;
                if (item == 3) {
                    break;
                }
            }
            return lastItem;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ForEachLoop_Break_AtStart_ExitsAfterFirstIteration()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var count = 0;
            foreach (var item in [1, 2, 3, 4, 5]) {
                count = count + 1;
                break;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ForEachLoop_Break_PreservesVariableState()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var item in [1, 2, 3, 4, 5]) {
                sum = sum + item;
                if (sum > 6) {
                    break;
                }
            }
            return sum;
        }");

        // 1 + 2 + 3 + 4 = 10 > 6, breaks
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ForEachLoop_Break_OnlyExitsInnerLoop()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var outerCount = 0;
            var totalInner = 0;
            foreach (var i in [1, 2, 3]) {
                foreach (var j in [10, 20, 30, 40, 50]) {
                    if (j == 30) {
                        break;
                    }
                    totalInner = totalInner + 1;
                }
                outerCount = outerCount + 1;
            }
            return outerCount * 100 + totalInner;
        }");

        // outerCount = 3, totalInner = 2 * 3 = 6
        Assert.That(result, Is.EqualTo(306));
    }

    #endregion

    #region ForEach Loop with Continue

    [Test]
    public void ForEachLoop_Continue_SkipsRemainingBody()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) {
                if (item % 2 == 0) {
                    continue;
                }
                sum = sum + item;
            }
            return sum;
        }");

        // Sum of odd numbers 1-10: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void ForEachLoop_Continue_SkipsToNextItem()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var skipped = 0;
            var processed = 0;
            foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) {
                if (item <= 5) {
                    skipped = skipped + 1;
                    continue;
                }
                processed = processed + 1;
            }
            return skipped * 100 + processed;
        }");

        // skipped = 5, processed = 5
        Assert.That(result, Is.EqualTo(505));
    }

    [Test]
    public void ForEachLoop_Continue_InNestedLoop_OnlyAffectsInner()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            foreach (var i in [1, 2, 3]) {
                foreach (var j in [1, 2, 3, 4, 5]) {
                    if (j == 3) {
                        continue;
                    }
                    total = total + 1;
                }
            }
            return total;
        }");

        // Each inner loop: 4 iterations counted (1,2,4,5 - skip 3), outer: 3 iterations = 12
        Assert.That(result, Is.EqualTo(12));
    }

    #endregion

    #region ForEach Loop with Break and Continue Combined

    [Test]
    public void ForEachLoop_BreakAndContinue_Combined()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]) {
                if (item % 2 == 0) {
                    continue;
                }
                if (item > 10) {
                    break;
                }
                sum = sum + item;
            }
            return sum;
        }");

        // Odd numbers 1-10: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void ForEachLoop_BreakAndContinue_InNestedLoops()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            foreach (var i in [1, 2, 3, 4, 5]) {
                if (i == 3) {
                    continue;
                }
                foreach (var j in [1, 2, 3, 4, 5]) {
                    if (j == 2) {
                        break;
                    }
                    total = total + 1;
                }
            }
            return total;
        }");

        // Outer: 1,2,4,5 (skip 3) = 4 iterations
        // Inner: only j=1 counted before break = 1 per outer
        // Total = 4
        Assert.That(result, Is.EqualTo(4));
    }

    #endregion
}
