namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DoWhileLoopTests(CompilationMode mode) : TestBase
{
    #region Basic Do-While Loop

    [Test]
    public void DoWhileLoop_BasicCounter_CountsCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            do {
                count = count + 1;
                i = i + 1;
            } while (i < 5);
            return count;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void DoWhileLoop_FalseCondition_ExecutesOnce()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var executed = 0;
            do {
                executed = executed + 1;
            } while (false);
            return executed;
        }");

        // Key difference from while: executes at least once
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void DoWhileLoop_TrueCondition_LoopsUntilFalse()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var count = 0;
            var done = false;
            do {
                count = count + 1;
                if (count >= 3) {
                    done = true;
                }
            } while (done == false);
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void DoWhileLoop_CountDown_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 10;
            do {
                sum = sum + i;
                i = i - 1;
            } while (i > 0);
            return sum;
        }");

        // 10 + 9 + 8 + ... + 1 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    #endregion

    #region Do-While vs While Comparison

    [Test]
    public void DoWhileLoop_ExecutesAtLeastOnce_UnlikeWhile()
    {
        var engine = CreateEngine(mode);

        // While loop with false condition - never executes
        var whileResult = engine.Evaluate(@"
        {
            var count = 0;
            while (false) {
                count = count + 1;
            }
            return count;
        }");

        // Do-while loop with false condition - executes once
        var doWhileResult = engine.Evaluate(@"
        {
            var count = 0;
            do {
                count = count + 1;
            } while (false);
            return count;
        }");

        Assert.That(whileResult, Is.EqualTo(0));
        Assert.That(doWhileResult, Is.EqualTo(1));
    }

    [Test]
    public void DoWhileLoop_ConditionCheckedAfterBody()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var i = 10;
            var bodyExecuted = false;
            do {
                bodyExecuted = true;
            } while (i < 5);
            return bodyExecuted;
        }");

        // Even though i >= 5, body executes once before condition is checked
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region Do-While Loop with Variables

    [Test]
    public void DoWhileLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("limit", 10);

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            do {
                sum = sum + i;
                i = i + 1;
            } while (i <= limit);
            return sum;
        }");

        // Sum of 1 to 10 = 55
        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void DoWhileLoop_MultipleVariables_TracksAll()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var a = 0;
            var b = 1;
            var count = 0;
            do {
                var temp = a + b;
                a = b;
                b = temp;
                count = count + 1;
            } while (count < 10);
            return b;
        }");

        // Fibonacci: 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89
        Assert.That(result, Is.EqualTo(89));
    }

    #endregion

    #region Do-While Loop with Complex Conditions

    [Test]
    public void DoWhileLoop_WithAndCondition_EvaluatesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 0;
            var y = 0;
            do {
                x = x + 1;
                y = y + 1;
            } while (x < 5 && y < 3);
            return x;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void DoWhileLoop_WithOrCondition_EvaluatesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var x = 0;
            var y = 10;
            do {
                x = x + 1;
                y = y - 1;
            } while (x < 5 || y > 5);
            return x;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void DoWhileLoop_WithTernaryCondition_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("useShort", true);

        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            do {
                count = count + 1;
                i = i + 1;
            } while (i < (useShort ? 3 : 10));
            return count;
        }");

        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Do-While Loop with Return

    [Test]
    public void DoWhileLoop_WithEarlyReturn_ExitsImmediately()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var i = 0;
            do {
                if (i == 5) {
                    return i;
                }
                i = i + 1;
            } while (i < 100);
            return -1;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void DoWhileLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("target", 7L);

        var result = engine.Evaluate(@"
        {
            var i = 0;
            do {
                if (i == target) {
                    return $""Found at {i}"";
                }
                i = i + 1;
            } while (i < 20);
            return ""Not found"";
        }");

        Assert.That(result, Is.EqualTo("Found at 7"));
    }

    #endregion

    #region Do-While Loop with Nested Structures

    [Test]
    public void DoWhileLoop_WithNestedIf_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            do {
                if (i % 2 == 0) {
                    sum = sum + i;
                }
                i = i + 1;
            } while (i < 10);
            return sum;
        }");

        // Sum of even numbers 0-9: 0 + 2 + 4 + 6 + 8 = 20
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void DoWhileLoop_Nested_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            do {
                var j = 0;
                do {
                    total = total + 1;
                    j = j + 1;
                } while (j < 3);
                i = i + 1;
            } while (i < 3);
            return total;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void DoWhileLoop_NestedWithWhile_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            do {
                var j = 0;
                while (j < 3) {
                    total = total + 1;
                    j = j + 1;
                }
                i = i + 1;
            } while (i < 3);
            return total;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void DoWhileLoop_NestedWithFor_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            do {
                for (var j = 0; j < 3; j = j + 1) {
                    total = total + 1;
                }
                i = i + 1;
            } while (i < 3);
            return total;
        }");

        Assert.That(result, Is.EqualTo(9));
    }

    #endregion

    #region Do-While Loop with Collections

    [Test]
    public void DoWhileLoop_WithArrayIndexing_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var arr = [1, 2, 3, 4, 5];
            var sum = 0;
            var i = 0;
            do {
                sum = sum + arr[i];
                i = i + 1;
            } while (i < 5);
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void DoWhileLoop_WithListCount_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("items", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            do {
                sum = sum + items[i];
                i = i + 1;
            } while (i < items.Count());
            return sum;
        }");

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void DoWhileLoop_BuildingList_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var result = [];
            var i = 0;
            do {
                result = [...result, i * 2];
                i = i + 1;
            } while (i < 5);
            return result;
        }") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(new List<object?> { 0, 2, 4, 6, 8 }));
    }

    #endregion

    #region Do-While Loop with Strings

    [Test]
    public void DoWhileLoop_StringConcatenation_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var str = """";
            var i = 0;
            do {
                str = str + i;
                i = i + 1;
            } while (i < 5);
            return str;
        }");

        Assert.That(result, Is.EqualTo("01234"));
    }

    [Test]
    public void DoWhileLoop_InterpolatedString_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var lines = """";
            var i = 1;
            do {
                lines = $""{lines}Line {i}\n"";
                i = i + 1;
            } while (i <= 3);
            return lines;
        }");

        Assert.That(result, Is.EqualTo("Line 1\nLine 2\nLine 3\n"));
    }

    #endregion

    #region Do-While Loop with Anonymous Objects

    [Test]
    public void DoWhileLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var i = 0;
            var lastObj = null;
            do {
                lastObj = new { Index = i, Squared = i * i };
                i = i + 1;
            } while (i < 3);
            return lastObj;
        }") as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Index"], Is.EqualTo(2));
        Assert.That(result["Squared"], Is.EqualTo(4));
    }

    #endregion

    #region Do-While Loop Single Statement Body

    [Test]
    public void DoWhileLoop_SingleStatementBody_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var i = 0;
            do
                i = i + 1;
            while (i < 5);
            return i;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Do-While Loop Safety

    [Test]
    public void DoWhileLoop_ExceedsMaxIterations_ThrowsException()
    {
        var engine = CreateEngine(mode);

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var i = 0;
                do {
                    i = i + 1;
                } while (true);
                return i;
            }"));

        Assert.That(ex!.Message, Does.Contain("maximum iterations"));
    }

    [Test]
    public void DoWhileLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = CreateEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var i = 0;
                do {
                    i = i + 1;
                } while (true);
                return i;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void DoWhileLoop_WithDisabledLimit_AllowsManyIterations()
    {
        var engine = CreateEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });

        var result = engine.Evaluate(@"
        {
            var count = 0;
            var i = 0;
            do {
                count = count + 1;
                i = i + 1;
            } while (i < 200000);
            return count;
        }");

        Assert.That(result, Is.EqualTo(200000));
    }

    [Test]
    public void DoWhileLoop_WithCancellationToken_CanBeCancelled()
    {
        var engine = CreateEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate(@"
            {
                var i = 0;
                do {
                    i = i + 1;
                } while (i < 1000000000);
                return i;
            }", null, cts.Token);
        });

        Thread.Sleep(100);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    #endregion

    #region Do-While Loop with Arithmetic

    [Test]
    public void DoWhileLoop_Factorial_CalculatesCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var n = 5;
            var factorial = 1;
            do {
                factorial = factorial * n;
                n = n - 1;
            } while (n > 1);
            return factorial;
        }");

        Assert.That(result, Is.EqualTo(120));
    }

    [Test]
    public void DoWhileLoop_PowerCalculation_WorksCorrectly()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var power = 1;
            var i = 0;
            do {
                power = power * 2;
                i = i + 1;
            } while (i < 10);
            return power;
        }");

        Assert.That(result, Is.EqualTo(1024));
    }

    #endregion

    #region Do-While Loop Parsing Tests

    [Test]
    public void DoWhileLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = CreateEngine(mode);
        var success = engine.TryParse("{ var i = 0; do { i = i + 1; } while (i < 5); return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void DoWhileLoop_TryParse_WithoutSemicolon_StillWorks()
    {
        var engine = CreateEngine(mode);
        var success = engine.TryParse("{ var i = 0; do { i = i + 1; } while (i < 5) return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
    }

    [Test]
    public void DoWhileLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = CreateEngine(mode);
        var expr = engine.Parse(@"
        {
            var sum = 0;
            var i = 0;
            do {
                sum = sum + i;
                i = i + 1;
            } while (i < limit);
            return sum;
        }");

        engine.SetVariable("limit", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(10)); // 0+1+2+3+4

        engine.SetVariable("limit", 10);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(45)); // 0+1+2+...+9
    }

    #endregion

    #region Do-While Loop with Break

    [Test]
    public void DoWhileLoop_Break_ExitsLoop()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var i = 0;
            do {
                if (i == 5) {
                    break;
                }
                i = i + 1;
            } while (i < 100);
            return i;
        }");

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void DoWhileLoop_Break_AtStart_ExitsAfterFirstIteration()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var count = 0;
            do {
                count = count + 1;
                break;
            } while (true);
            return count;
        }");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void DoWhileLoop_Break_PreservesVariableState()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            do {
                sum = sum + i;
                if (sum > 10) {
                    break;
                }
                i = i + 1;
            } while (i <= 10);
            return sum;
        }");

        // 1 + 2 + 3 + 4 + 5 = 15 > 10, breaks
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void DoWhileLoop_Break_OnlyExitsInnerLoop()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var outerCount = 0;
            var totalInner = 0;
            var i = 0;
            do {
                var j = 0;
                do {
                    if (j == 2) {
                        break;
                    }
                    totalInner = totalInner + 1;
                    j = j + 1;
                } while (j < 10);
                outerCount = outerCount + 1;
                i = i + 1;
            } while (i < 3);
            return outerCount * 100 + totalInner;
        }");

        // outerCount = 3, totalInner = 2 * 3 = 6
        Assert.That(result, Is.EqualTo(306));
    }

    #endregion

    #region Do-While Loop with Continue

    [Test]
    public void DoWhileLoop_Continue_SkipsRemainingBody()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            do {
                i = i + 1;
                if (i % 2 == 0) {
                    continue;
                }
                sum = sum + i;
            } while (i < 10);
            return sum;
        }");

        // Sum of odd numbers 1-9: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void DoWhileLoop_Continue_JumpsToCondition()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var skipped = 0;
            var processed = 0;
            var i = 0;
            do {
                i = i + 1;
                if (i <= 5) {
                    skipped = skipped + 1;
                    continue;
                }
                processed = processed + 1;
            } while (i < 10);
            return skipped * 100 + processed;
        }");

        // skipped = 5, processed = 5
        Assert.That(result, Is.EqualTo(505));
    }

    [Test]
    public void DoWhileLoop_Continue_InNestedLoop_OnlyAffectsInner()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            do {
                var j = 0;
                do {
                    j = j + 1;
                    if (j == 3) {
                        continue;
                    }
                    total = total + 1;
                } while (j < 5);
                i = i + 1;
            } while (i < 3);
            return total;
        }");

        // Each inner loop: 4 iterations counted (1,2,4,5 - skip 3), outer: 3 iterations = 12
        Assert.That(result, Is.EqualTo(12));
    }

    #endregion

    #region Do-While Loop with Break and Continue Combined

    [Test]
    public void DoWhileLoop_BreakAndContinue_Combined()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 0;
            do {
                i = i + 1;
                if (i % 2 == 0) {
                    continue;
                }
                if (i > 10) {
                    break;
                }
                sum = sum + i;
            } while (true);
            return sum;
        }");

        // Odd numbers 1-10: 1 + 3 + 5 + 7 + 9 = 25
        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void DoWhileLoop_BreakAndContinue_InNestedLoops()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var total = 0;
            var i = 0;
            do {
                i = i + 1;
                if (i == 3) {
                    continue;
                }
                var j = 0;
                do {
                    j = j + 1;
                    if (j == 2) {
                        break;
                    }
                    total = total + 1;
                } while (j < 5);
            } while (i < 5);
            return total;
        }");

        // Outer: 1,2,4,5 (skip 3) = 4 iterations
        // Inner: only j=1 counted before break = 1 per outer
        // Total = 4
        Assert.That(result, Is.EqualTo(4));
    }

    #endregion
}
