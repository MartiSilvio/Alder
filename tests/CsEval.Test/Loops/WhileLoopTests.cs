namespace CsEval.Test.Loops;

// Engine-only: All tests use CsEval-specific configuration (MaxIterations, CancellationToken)
// or test parsing API (TryParse) - not expression evaluation

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class WhileLoopTests(CompilationMode mode)
{
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
                """, cancellationToken: cts.Token);
        });

        Thread.Sleep(100);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
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
