namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DoWhileLoopTests(CompilationMode mode)
{
    [TestCase("""
        var count = 0;
        var i = 0;
        do {
            count = count + 1;
            i = i + 1;
        } while (i < 5);
        return count;
        """,
        5,
        TestName = "Basic_CountsToFive")]
    [TestCase("""
        var executed = 0;
        do {
            executed = executed + 1;
        } while (false);
        return executed;
        """,
        1,
        TestName = "FalseCondition_ExecutesOnce")]
    [TestCase("""
        var count = 0;
        var done = false;
        do {
            count = count + 1;
            if (count >= 3) {
                done = true;
            }
        } while (done == false);
        return count;
        """,
        3,
        TestName = "TrueCondition_LoopsUntilFalse")]
    [TestCase("""
        var sum = 0;
        var i = 10;
        do {
            sum = sum + i;
            i = i - 1;
        } while (i > 0);
        return sum;
        """,
        55,
        TestName = "CountDown")]
    [TestCase("""
        var i = 10;
        var bodyExecuted = false;
        do {
            bodyExecuted = true;
        } while (i < 5);
        return bodyExecuted;
        """,
        true,
        TestName = "Condition_CheckedAfterBody")]
    [TestCase("""
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
        """,
        89,
        TestName = "Fibonacci")]
    [TestCase("""
        var x = 0;
        var y = 0;
        do {
            x = x + 1;
            y = y + 1;
        } while (x < 5 && y < 3);
        return x;
        """,
        3,
        TestName = "AndCondition")]
    [TestCase("""
        var x = 0;
        var y = 10;
        do {
            x = x + 1;
            y = y - 1;
        } while (x < 5 || y > 5);
        return x;
        """,
        5,
        TestName = "OrCondition")]
    [TestCase("""
        var i = 0;
        do {
            if (i == 5) {
                return i;
            }
            i = i + 1;
        } while (i < 100);
        return -1;
        """,
        5,
        TestName = "EarlyReturn")]
    [TestCase("""
        var sum = 0;
        var i = 0;
        do {
            if (i % 2 == 0) {
                sum = sum + i;
            }
            i = i + 1;
        } while (i < 10);
        return sum;
        """,
        20,
        TestName = "NestedIf_SumOfEvenNumbers")]
    [TestCase("""
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
        """,
        9,
        TestName = "NestedDoWhile")]
    [TestCase("""
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
        """,
        9,
        TestName = "NestedWithWhile")]
    [TestCase("""
        var total = 0;
        var i = 0;
        do {
            for (var j = 0; j < 3; j = j + 1) {
                total = total + 1;
            }
            i = i + 1;
        } while (i < 3);
        return total;
        """,
        9,
        TestName = "NestedWithFor")]
    [TestCase("""
        var str = "";
        var i = 0;
        do {
            str = str + i;
            i = i + 1;
        } while (i < 5);
        return str;
        """,
        "01234",
        TestName = "StringConcatenation")]
    [TestCase("""
        var lines = "";
        var i = 1;
        do {
            lines = $"{lines}Line {i}\n";
            i = i + 1;
        } while (i <= 3);
        return lines;
        """,
        "Line 1\nLine 2\nLine 3\n",
        TestName = "InterpolatedString")]
    [TestCase("""
        var i = 0;
        do
            i = i + 1;
        while (i < 5);
        return i;
        """,
        5,
        TestName = "SingleStatementBody")]
    [TestCase("""
        var n = 5;
        var factorial = 1;
        do {
            factorial = factorial * n;
            n = n - 1;
        } while (n > 1);
        return factorial;
        """,
        120,
        TestName = "Factorial")]
    [TestCase("""
        var power = 1;
        var i = 0;
        do {
            power = power * 2;
            i = i + 1;
        } while (i < 10);
        return power;
        """,
        1024,
        TestName = "PowerOfTwo")]
    [TestCase("""
        var i = 0;
        do {
            if (i == 5) {
                break;
            }
            i = i + 1;
        } while (i < 100);
        return i;
        """,
        5,
        TestName = "Break_ExitsLoop")]
    [TestCase("""
        var count = 0;
        do {
            count = count + 1;
            break;
        } while (true);
        return count;
        """,
        1,
        TestName = "Break_AtStartExitsAfterFirstIteration")]
    [TestCase("""
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
        """,
        15,
        TestName = "Break_PreservesVariableState")]
    [TestCase("""
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
        """,
        306,
        TestName = "Break_OnlyExitsInnerLoop")]
    [TestCase("""
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
        """,
        25,
        TestName = "Continue_SkipsRemainingBody")]
    [TestCase("""
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
        """,
        505,
        TestName = "Continue_JumpsToCondition")]
    [TestCase("""
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
        """,
        12,
        TestName = "Continue_InNestedLoopOnlyAffectsInner")]
    [TestCase("""
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
        """,
        25,
        TestName = "BreakAndContinue_Combined")]
    [TestCase("""
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
        """,
        4,
        TestName = "BreakAndContinue_InNestedLoops")]
    public async Task Eval_DoWhileLoop(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate($"{{ {expr} }}");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync($"{{ {expr} }}");

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    #region Do-While vs While Comparison

    [Test]
    public void DoWhileLoop_ExecutesAtLeastOnce_UnlikeWhile()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var whileResult = engine.Evaluate(@"
        {
            var count = 0;
            while (false) {
                count = count + 1;
            }
            return count;
        }");

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

    #endregion

    #region Do-While Loop with External Variables

    [Test]
    public void DoWhileLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void DoWhileLoop_WithTernaryCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    [Test]
    public void DoWhileLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    #region Do-While Loop with Collections

    [Test]
    public void DoWhileLoop_WithArrayIndexing_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var result = [];
            var i = 0;
            do {
                result = [...result, i * 2];
                i = i + 1;
            } while (i < 5);
            return result;
        }");

        Assert.That(result, Is.TypeOf<List<int>>());
        Assert.That(result, Is.EqualTo(new List<int> { 0, 2, 4, 6, 8 }));
    }

    #endregion

    #region Do-While Loop with Anonymous Objects

    [Test]
    public void DoWhileLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var i = 0;
            object lastObj = null;
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

    #region Do-While Loop Safety

    [Test]
    public void DoWhileLoop_ExceedsMaxIterations_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });

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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
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

    #region Do-While Loop Parsing Tests

    [Test]
    public void DoWhileLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; do { i = i + 1; } while (i < 5); return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void DoWhileLoop_TryParse_WithoutSemicolon_StillWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; do { i = i + 1; } while (i < 5) return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
    }

    [Test]
    public void DoWhileLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("limit", 10);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(45));
    }

    #endregion
}
