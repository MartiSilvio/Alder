namespace CsEval.Test.Loops;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ForLoopTests(CompilationMode mode)
{


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
