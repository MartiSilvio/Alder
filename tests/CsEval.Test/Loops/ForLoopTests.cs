using CsEval.TestData.Data;

namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ForLoopTests(CompilationMode mode)
{
    [TestCaseSource(typeof(ForLoopData), nameof(ForLoopData.ValueCases))]
    public async Task Eval_ForLoop(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region Tests with External Variables

    [Test]
    public void ForLoop_WithExternalVariable_ModifiesCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("limit", 10);

        var result = engine.Evaluate("""
            var sum = 0;
            for (var i = 1; i <= limit; i++) {
                sum += i;
            }
            return sum;
            """);

        Assert.That(result, Is.EqualTo(55));
    }

    [Test]
    public void ForLoop_WithTernaryInCondition_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("useShort", true);

        var result = engine.Evaluate("""
            var count = 0;
            for (var i = 0; i < (useShort ? 3 : 10); i++) {
                count++;
            }
            return count;
            """);

        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ForLoop_WithConditionalReturn_ReturnsCorrectValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("target", 7);

        var result = engine.Evaluate("""
            for (var i = 0; i < 20; i++) {
                if (i == target) {
                    return $"Found at {i}";
                }
            }
            return "Not found";
            """);

        Assert.That(result, Is.EqualTo("Found at 7"));
    }

    #endregion

    #region Tests with Collections (CsEval-specific syntax)

    [Test]
    public void ForLoop_WithArrayIndexing_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            var arr = [1, 2, 3, 4, 5];
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                sum += arr[i];
            }
            return sum;
            """);

        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForLoop_WithListCount_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("items", new List<int> { 10, 20, 30, 40 });

        var result = engine.Evaluate("""
            var sum = 0;
            for (var i = 0; i < items.Count(); i++) {
                sum += items[i];
            }
            return sum;
            """);

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void ForLoop_BuildingList_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            var result = [];
            for (var i = 0; i < 5; i++) {
                result = [..result, i * 2];
            }
            return result;
            """);

        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That(result, Is.EqualTo(new int[] { 0, 2, 4, 6, 8 }));
    }

    #endregion

    #region Tests with LINQ

    [Test]
    public void ForLoop_WithLinqSum_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            var numbers = [];
            for (var i = 1; i <= 5; i++) {
                numbers = [..numbers, i];
            }
            return numbers.Sum();
            """);

        Assert.That(Convert.ToDouble(result), Is.EqualTo(15.0));
    }

    [Test]
    public void ForLoop_WithLinqWhere_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            var numbers = [];
            for (var i = 1; i <= 10; i++) {
                numbers = [..numbers, i];
            }
            return numbers.Where(x => x % 2 == 0).Count();
            """);

        Assert.That(Convert.ToInt32(result), Is.EqualTo(5));
    }

    #endregion

    #region Tests with Math Module

    [Test]
    public async Task ForLoop_WithMathModule_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = """
            var sum = 0.0;
            for (var i = 1; i <= 5; i++) {
                sum += Math.Sqrt(i);
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
    public void ForLoop_BuildingObjects_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("""
            object lastObj = null;
            for (var i = 0; i < 3; i++) {
                lastObj = new { Index = i, Squared = i * i };
            }
            return lastObj;
            """) as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Index"], Is.EqualTo(2));
        Assert.That(result["Squared"], Is.EqualTo(4));
    }

    #endregion

    #region Safety Tests

    [Test]
    public void ForLoop_ExceedsMaxIterations_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("""
                for (var i = 0; ; i++) { }
                return 0;
                """));

        Assert.That(ex!.Message, Does.Contain("maximum iterations"));
    }

    [Test]
    public void ForLoop_WithCustomMaxIterations_UsesConfiguredLimit()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("""
                for (var i = 0; ; i++) { }
                return 0;
                """));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void ForLoop_WithDisabledLimit_AllowsManyIterations()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });

        var result = engine.Evaluate("""
            var count = 0;
            for (var i = 0; i < 200000; i++) {
                count++;
            }
            return count;
            """);

        Assert.That(result, Is.EqualTo(200000));
    }

    [Test]
    public void ForLoop_WithCancellationToken_CanBeCancelled()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate("""
                for (var i = 0; i < 1000000000; i++) { }
                return 0;
                """, null, cts.Token);
        });

        Thread.Sleep(100);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    #endregion

    #region ShouldThrow Tests

    [TestCaseSource(typeof(ForLoopData), nameof(ForLoopData.ErrorCases))]
    public async Task Eval_ForLoop_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #endregion

    #region Parsing Tests

    [Test]
    public void ForLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("for (var i = 0; i < 5; i++) { } return 0;", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    [Test]
    public void ForLoop_TryParse_MissingParenthesis_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("for var i = 0; i < 5; i++ { }", out var expr, out var error);

        Assert.That(success, Is.False);
        Assert.That(expr, Is.Null);
        Assert.That(error, Is.Not.Null);
    }

    [Test]
    public void ForLoop_PreParsed_CanBeEvaluatedMultipleTimes()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse("""
            var sum = 0;
            for (var i = 0; i < limit; i++) {
                sum += i;
            }
            return sum;
            """);

        engine.SetVariable("limit", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(10));

        engine.SetVariable("limit", 10);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(45));
    }

    #endregion
}
