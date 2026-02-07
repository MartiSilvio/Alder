namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class WhileLoopTests(CompilationMode mode)
{
    [TestCase("""
        var count = 0; var i = 0;
        while (i < 5) { count++; i++; }
        return count;
        """,
        5,
        TestName = "Basic_CountsToFive")]
    [TestCase("""
        var executed = false;
        while (false) { executed = true; }
        return executed;
        """,
        false,
        TestName = "Basic_FalseConditionNeverExecutes")]
    [TestCase("""
        var count = 0; var done = false;
        while (done == false) { count++; done = true; }
        return count;
        """,
        1,
        TestName = "Basic_SingleIteration")]
    [TestCase("""
        var a = 0; var b = 1; var count = 0;
        while (count < 10) { var temp = a + b; a = b; b = temp; count++; }
        return b;
        """,
        89,
        TestName = "Fibonacci")]
    [TestCase("""
        var total = 0; var i = 0;
        while (i < 3) { var local = i * 2; total += local; i++; }
        return total;
        """,
        6,
        TestName = "Variables_ScopedToBlock")]
    [TestCase("""
        var x = 0; var y = 0;
        while (x < 5 && y < 3) { x++; y++; }
        return x;
        """,
        3,
        TestName = "AndCondition")]
    [TestCase("""
        var x = 0; var y = 10;
        while (x < 5 || y > 5) { x++; y--; }
        return x;
        """,
        5,
        TestName = "OrCondition")]
    [TestCase("""
        var text = "test"; var count = 0;
        while (text != null && count < 3) { count++; }
        return count;
        """,
        3,
        TestName = "NullCheck")]
    [TestCase("""
        var i = 0;
        while (i < 100) { if (i == 5) { return i; } i++; }
        return -1;
        """,
        5,
        TestName = "EarlyReturn")]
    [TestCase("""
        var sum = 0; var i = 0;
        while (i < 10) { if (i % 2 == 0) { sum += i; } i++; }
        return sum;
        """,
        20,
        TestName = "NestedIf")]
    [TestCase("""
        var total = 0; var i = 0;
        while (i < 3) {
            var j = 0;
            while (j < 3) { total++; j++; }
            i++;
        }
        return total;
        """,
        9,
        TestName = "NestedLoops")]
    [TestCase("""
        var product = 1; var i = 1;
        while (i <= 2) {
            var j = 1;
            while (j <= 2) {
                var k = 1;
                while (k <= 2) { product *= 2; k++; }
                j++;
            }
            i++;
        }
        return product;
        """,
        256,
        TestName = "DeeplyNested")]
    [TestCase("""
        var str = ""; var i = 0;
        while (i < 5) { str = str + i; i++; }
        return str;
        """,
        "01234",
        TestName = "StringConcatenation")]
    [TestCase("""
        var lines = ""; var i = 1;
        while (i <= 3) { lines = $"{lines}Line {i}\n"; i++; }
        return lines;
        """,
        "Line 1\nLine 2\nLine 3\n",
        TestName = "InterpolatedString")]
    [TestCase("""
        var count = 0; var i = 0;
        while (i < 3) { var now = DateTime.Now; if (now != null) { count++; } i++; }
        return count;
        """,
        3,
        TestName = "DateTimeModule")]
    [TestCase("""
        var n = 5; var factorial = 1;
        while (n > 1) { factorial *= n; n--; }
        return factorial;
        """,
        120,
        TestName = "Factorial")]
    [TestCase("""
        var a = 48; var b = 18;
        while (b != 0) { var temp = b; b = a % b; a = temp; }
        return a;
        """,
        6,
        TestName = "GCD")]
    [TestCase("""
        var baseNum = 2; var exp = 10; var power = 1;
        while (exp > 0) { power *= baseNum; exp--; }
        return power;
        """,
        1024,
        TestName = "PowerCalculation")]
    [TestCase("""
        var i = 0;
        while (i < 100) { if (i == 5) { break; } i++; }
        return i;
        """,
        5,
        TestName = "Break_ExitsLoop")]
    [TestCase("""
        var count = 0;
        while (true) { break; count++; }
        return count;
        """,
        0,
        TestName = "Break_AtStart")]
    [TestCase("""
        var sum = 0; var i = 1;
        while (i <= 10) { sum += i; if (sum > 10) { break; } i++; }
        return sum;
        """,
        15,
        TestName = "Break_PreservesState")]
    [TestCase("""
        var i = 0; var found = -1;
        while (i < 20) { if (i % 7 == 0 && i > 0) { found = i; break; } i++; }
        return found;
        """,
        7,
        TestName = "Break_InNestedIf")]
    [TestCase("""
        var outerCount = 0; var totalInner = 0; var i = 0;
        while (i < 3) {
            var j = 0;
            while (j < 10) { if (j == 2) { break; } totalInner++; j++; }
            outerCount++;
            i++;
        }
        return outerCount * 100 + totalInner;
        """,
        306,
        TestName = "Break_OnlyExitsInnerLoop")]
    [TestCase("""
        var iterations = 0; var i = 0;
        while (i < 1000) { iterations++; i++; if (i >= 50) { break; } }
        return iterations;
        """,
        50,
        TestName = "Break_AfterSomeIterations")]
    [TestCase("""
        var sum = 0; var i = 0;
        while (i < 10) { i++; if (i % 2 == 0) { continue; } sum += i; }
        return sum;
        """,
        25,
        TestName = "Continue_SkipsRemainingBody")]
    [TestCase("""
        var sum = 0; var i = 0;
        while (i < 20) {
            i++;
            if (i % 2 == 0) { continue; }
            if (i % 3 == 0) { continue; }
            sum += i;
        }
        return sum;
        """,
        73,
        TestName = "Continue_MultipleConditions")]
    [TestCase("""
        var total = 0; var i = 0;
        while (i < 3) {
            var j = 0;
            while (j < 5) { j++; if (j == 3) { continue; } total++; }
            i++;
        }
        return total;
        """,
        12,
        TestName = "Continue_InNestedLoop")]
    [TestCase("""
        var skipped = 0; var processed = 0; var i = 0;
        while (i < 10) { i++; if (i <= 5) { skipped++; continue; } processed++; }
        return skipped * 100 + processed;
        """,
        505,
        TestName = "Continue_SkipsToConditionCheck")]
    [TestCase("""
        var product = 1; var i = 0;
        while (i < 10) { i++; if (i % 2 == 0) { continue; } product *= i; }
        return product;
        """,
        945,
        TestName = "Continue_WithAccumulator")]
    [TestCase("""
        var count = 0; var iterations = 0; var i = 0;
        while (i < 5) { iterations++; i++; continue; count++; }
        return iterations * 100 + count;
        """,
        500,
        TestName = "Continue_DoesNotAffectCondition")]
    public async Task Eval_WhileLoop(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region Tests with External Variables

    [Test]
    public void WhileLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("limit", 10);

        var result = engine.Evaluate("""
            var sum = 0;
            var i = 1;
            while (i <= limit) {
                sum += i;
                i++;
            }
            return sum;
            """);

        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void WhileLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("target", 7);

        var result = engine.Evaluate("""
            var i = 0;
            while (i < 20) {
                if (i == target) {
                    return $"Found at {i}";
                }
                i++;
            }
            return "Not found";
            """);

        Assert.That(result, Is.EqualTo("Found at 7"));
    }

    #endregion

    #region Tests with Collections (CsEval-specific syntax)

    [Test]
    public void WhileLoop_WithArrayIndexing_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var arr = [1, 2, 3, 4, 5];
                var sum = 0;
                var i = 0;
                while (i < 5) {
                    sum = sum + arr[i];
                    i = i + 1;
                }
                return sum;
            }
            """);

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void WhileLoop_WithListCount_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("""
            {
                var sum = 0;
                var i = 0;
                while (i < items.Count()) {
                    sum = sum + items[i];
                    i = i + 1;
                }
                return sum;
            }
            """);

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void WhileLoop_BuildingList_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var result = [];
                var i = 0;
                while (i < 5) {
                    result = [..result, i * 2];
                    i = i + 1;
                }
                return result;
            }
            """);

        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That(result, Is.EqualTo(new int[] { 0, 2, 4, 6, 8 }));
    }

    [Test]
    public void WhileLoop_WithComparisonExpression_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            var numbers = [10, 5, 15, 3];
            var idx = 0;
            var found = -1;
            while (idx < 4 && found == -1) {
                if (numbers[idx] == 15) {
                    found = idx;
                }
                idx++;
            }
            return found;
            """);

        Assert.That(result, Is.EqualTo(2));
    }

    #endregion

    #region Tests with LINQ

    [Test]
    public void WhileLoop_WithLinqSum_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var numbers = [];
                var i = 1;
                while (i <= 5) {
                    numbers = [..numbers, i];
                    i = i + 1;
                }
                return numbers.Sum();
            }
            """);

        Assert.That(Convert.ToDouble(result), Is.EqualTo(15.0));
    }

    [Test]
    public void WhileLoop_WithLinqWhere_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var numbers = [];
                var i = 1;
                while (i <= 10) {
                    numbers = [..numbers, i];
                    i = i + 1;
                }
                return numbers.Where(x => x % 2 == 0).Count();
            }
            """);

        Assert.That(Convert.ToInt32(result), Is.EqualTo(5));
    }

    #endregion

    #region Tests with Math Module

    [Test]
    public async Task WhileLoop_WithMathModule_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = """
            var sum = 0.0;
            var i = 1;
            while (i <= 5) {
                sum += Math.Sqrt(i);
                i++;
            }
            return sum;
            """;

        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(Convert.ToDouble(result), Is.EqualTo(Math.Sqrt(1) + Math.Sqrt(2) + Math.Sqrt(3) + Math.Sqrt(4) + Math.Sqrt(5)).Within(0.001));
        Assert.That(Convert.ToDouble(result), Is.EqualTo(Convert.ToDouble(csharpResult)).Within(0.001));
    }

    #endregion

    #region Tests with Anonymous Objects

    [Test]
    public void WhileLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var i = 0;
                object lastObj = null;
                while (i < 3) {
                    lastObj = new { Index = i, Squared = i * i };
                    i = i + 1;
                }
                return lastObj;
            }
            """) as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Index"], Is.EqualTo(2));
        Assert.That(result["Squared"], Is.EqualTo(4));
    }

    #endregion

    #region Safety Tests

    [Test]
    public void WhileLoop_ExceedsMaxIterations_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("""
                {
                    var i = 0;
                    while (true) { i = i + 1; }
                    return i;
                }
                """));

        Assert.That(ex!.Message, Does.Contain("maximum iterations"));
    }

    [Test]
    public void WhileLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("""
                {
                    var i = 0;
                    while (true) { i = i + 1; }
                    return i;
                }
                """));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void WhileLoop_WithDisabledLimit_AllowsManyIterations()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });

        var result = engine.Evaluate("""
            {
                var count = 0;
                var i = 0;
                while (i < 200000) {
                    count = count + 1;
                    i = i + 1;
                }
                return count;
            }
            """);

        Assert.That(result, Is.EqualTo(200000));
    }

    [Test]
    public void WhileLoop_WithCancellationToken_CanBeCancelled()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate("""
                {
                    var i = 0;
                    while (i < 1000000000) { i = i + 1; }
                    return i;
                }
                """, null, cts.Token);
        });

        Thread.Sleep(100);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void WhileLoop_EmptyBody_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var i = 5;
                while (i < 3) { }
                return i;
            }
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_WithTernaryCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("useLimit", true);

        var result = engine.Evaluate("""
            {
                var count = 0;
                var i = 0;
                while (i < (useLimit ? 5 : 10)) {
                    count = count + 1;
                    i = i + 1;
                }
                return count;
            }
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_ConditionEvaluatedEachIteration()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var limit = 5;
                var i = 0;
                var sum = 0;
                while (i < limit) {
                    sum = sum + i;
                    i = i + 1;
                    if (i == 3) { limit = 3; }
                }
                return sum;
            }
            """);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_WithNullCoalesce_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("maybeNull", null);

        var result = engine.Evaluate("""
            {
                var count = 0;
                var num = maybeNull ?? 5;
                while (num > 0) {
                    count = count + 1;
                    num = num - 1;
                }
                return count;
            }
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_SingleStatementBody_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var count = 0;
                var i = 0;
                while (i < 5)
                    i = i + 1;
                return i;
            }
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    #endregion

    #region Break and Continue Combined

    [Test]
    public void WhileLoop_BreakAndContinue_Combined()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var sum = 0;
                var i = 0;
                while (true) {
                    i = i + 1;
                    if (i % 2 == 0) { continue; }
                    if (i > 10) { break; }
                    sum = sum + i;
                }
                return sum;
            }
            """);

        Assert.That(result, Is.EqualTo(25));
    }

    [Test]
    public void WhileLoop_BreakAndContinue_InNestedLoops()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var total = 0;
                var i = 0;
                while (i < 5) {
                    i = i + 1;
                    if (i == 3) { continue; }
                    var j = 0;
                    while (j < 5) {
                        j = j + 1;
                        if (j == 2) { break; }
                        total = total + 1;
                    }
                }
                return total;
            }
            """);

        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void WhileLoop_BreakAndContinue_FindFirstMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var data = [1, -2, 3, -4, 5, -6, 7];
                var i = 0;
                var firstPositiveAfterNegative = -1;
                var sawNegative = false;
                while (i < 7) {
                    var val = data[i];
                    i = i + 1;
                    if (val < 0) { sawNegative = true; continue; }
                    if (sawNegative) { firstPositiveAfterNegative = val; break; }
                }
                return firstPositiveAfterNegative;
            }
            """);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void WhileLoop_MultipleContinues_InSameIteration()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var count = 0;
                var i = 0;
                while (i < 10) {
                    i = i + 1;
                    if (i < 3) { continue; }
                    if (i > 7) { continue; }
                    count = count + 1;
                }
                return count;
            }
            """);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void WhileLoop_Break_WithCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("target", 42);

        var result = engine.Evaluate("""
            var arr = [10, 20, 42, 50, 60];
            var i = 0;
            var foundIndex = -1;
            while (i < 5) {
                if (arr[i] == target) { foundIndex = i; break; }
                i++;
            }
            return foundIndex;
            """);

        Assert.That(result, Is.EqualTo(2));
    }

    #endregion

    #region ShouldThrow Tests

    [TestCase("{ while (1) { break; } return 0; }", TestName = "NonBoolean_IntCondition")]
    [TestCase("{ while (\"true\") { break; } return 0; }", TestName = "NonBoolean_StringCondition")]
    [TestCase("{ while (3.14) { break; } return 0; }", TestName = "NonBoolean_DoubleCondition")]
    public async Task Eval_WhileLoop_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #endregion

    #region Parsing Tests

    [Test]
    public void WhileLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; while (i < 5) { i = i + 1; } return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void WhileLoop_TryParse_MissingParenthesis_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ while i < 5 { } }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void WhileLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse("""
            {
                var sum = 0;
                var i = 0;
                while (i < limit) {
                    sum = sum + i;
                    i = i + 1;
                }
                return sum;
            }
            """);

        engine.SetVariable("limit", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("limit", 10);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(45));
    }

    [Test]
    public void WhileLoop_Break_TryParse_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; while (true) { break; } return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void WhileLoop_Continue_TryParse_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; while (i < 5) { i = i + 1; continue; } return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void WhileLoop_BreakWithSemicolon_ParsesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            {
                var i = 0;
                while (i < 10) {
                    i = i + 1;
                    break;
                }
                return i;
            }
            """);

        Assert.That(result, Is.EqualTo(1));
    }

    #endregion
}
