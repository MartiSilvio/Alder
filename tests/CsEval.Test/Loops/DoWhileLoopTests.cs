namespace CsEval.Test.Loops;

// Engine-only: Remaining tests use SetVariable, CsEval-specific configuration (MaxIterations, CancellationToken),
// CsEval collection expression syntax ([...], [..spread]), anonymous objects as IDictionary, or test parsing API (TryParse).
// Execution semantics tests migrated to TestData/Loops/DoWhileLoop/*.csx

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DoWhileLoopTests(CompilationMode mode)
{
    #region Do-While Loop with External Variables

    // Engine-only: SetVariable
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

    // Engine-only: SetVariable
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

    // Engine-only: SetVariable
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

    // Engine-only: CsEval collection expression syntax [1, 2, 3, 4, 5]
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

    // Engine-only: SetVariable with List<int>
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

    // Engine-only: CsEval collection expression syntax ([], [..spread])
    [Test]
    public void DoWhileLoop_BuildingList_WorksCorrectly()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var result = [];
            var i = 0;
            do {
                result = [..result, i * 2];
                i = i + 1;
            } while (i < 5);
            return result;
        }");

        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That(result, Is.EqualTo(new int[] { 0, 2, 4, 6, 8 }));
    }

    #endregion

    #region Do-While Loop with Anonymous Objects

    // Engine-only: anonymous objects as IDictionary<string, object?>
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

    // Engine-only: MaxIterations (CsEval-specific configuration)
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

    // Engine-only: MaxIterations (CsEval-specific configuration)
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

    // Engine-only: MaxIterations (CsEval-specific configuration)
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

    // Engine-only: CancellationToken (CsEval-specific configuration)
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

    // Engine-only: TryParse API (CsEval-specific)
    [Test]
    public void DoWhileLoop_TryParse_ValidExpression_Succeeds()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; do { i = i + 1; } while (i < 5); return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
        Assert.That(error, Is.Null);
    }

    // Engine-only: TryParse API (CsEval-specific)
    [Test]
    public void DoWhileLoop_TryParse_WithoutSemicolon_StillWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("{ var i = 0; do { i = i + 1; } while (i < 5) return i; }", out var expr, out var error);

        Assert.That(success, Is.True);
        Assert.That(expr, Is.Not.Null);
    }

    // Engine-only: Parse/SetVariable API (CsEval-specific), pre-parsed expression reuse
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
