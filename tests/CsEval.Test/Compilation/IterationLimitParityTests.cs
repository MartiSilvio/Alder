namespace CsEval.Test.Compilation;

/// <summary>
/// Tests to verify iteration limit behavior parity between IL-compiled and tree-walking modes.
/// The iteration counter should be GLOBAL across all loops in a single evaluation,
/// not per-loop.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IterationLimitParityTests(CompilationMode mode) 
{
    #region Global Iteration Counter (Nested Loops)

    [Test]
    public void NestedLoops_GlobalIterationCounter_HitsLimitAcrossAllLoops()
    {
        // With MaxIterations = 50, nested loops should hit limit based on TOTAL iterations
        // not per-loop iterations. 10 outer * 10 inner = 100 total, should hit 50 limit.
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 50 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 10; i++) {
                    for (var j = 0; j < 10; j++) {
                        count = count + 1;
                    }
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("50"));
    }

    [Test]
    public void NestedLoops_GlobalIterationCounter_CountsAllIterations()
    {
        // 3 outer * 4 inner = 12 total iterations
        // With limit of 15, should succeed
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 15 });

        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 3; i++) {
                for (var j = 0; j < 4; j++) {
                    count = count + 1;
                }
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void NestedLoops_GlobalIterationCounter_FailsWhenTotalExceedsLimit()
    {
        // 3 outer * 4 inner = 12 total iterations
        // With limit of 10, should fail
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 3; i++) {
                    for (var j = 0; j < 4; j++) {
                        count = count + 1;
                    }
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void SequentialLoops_GlobalIterationCounter_AccumulatesAcrossLoops()
    {
        // First loop: 6 iterations, Second loop: 6 iterations = 12 total
        // With limit of 10, should fail
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 6; i++) {
                    count = count + 1;
                }
                for (var j = 0; j < 6; j++) {
                    count = count + 1;
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    [Test]
    public void DeeplyNestedLoops_GlobalIterationCounter()
    {
        // 2 * 2 * 2 = 8 total iterations with limit 5 should fail
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 5 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 2; i++) {
                    for (var j = 0; j < 2; j++) {
                        for (var k = 0; k < 2; k++) {
                            count = count + 1;
                        }
                    }
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("5"));
    }

    #endregion

    #region Exact Iteration Count

    [Test]
    public void WhileLoop_ExactIterationCount_ThrowsAtCorrectIteration()
    {
        // With MaxIterations = 5, loop should execute exactly 5 times before throwing
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 5 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                while (true) {
                    count = count + 1;
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("5"));
    }

    [Test]
    public void ForLoop_ExactIterationCount_ExecutesExactlyMaxIterations()
    {
        // With MaxIterations = 10, a loop of exactly 10 iterations should succeed
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var result = engine.Evaluate(@"
        {
            var count = 0;
            for (var i = 0; i < 10; i++) {
                count = count + 1;
            }
            return count;
        }");

        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void ForLoop_ExactIterationCount_FailsAt11()
    {
        // With MaxIterations = 10, a loop of 11 iterations should fail
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 11; i++) {
                    count = count + 1;
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    #endregion

    #region Mixed Loop Types

    [Test]
    public void MixedLoopTypes_GlobalIterationCounter()
    {
        // for (3) + while (3) + do-while (3) = 9 total, limit 7 should fail
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 7 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 3; i++) {
                    count = count + 1;
                }
                var j = 0;
                while (j < 3) {
                    count = count + 1;
                    j = j + 1;
                }
                var k = 0;
                do {
                    count = count + 1;
                    k = k + 1;
                } while (k < 3);
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("7"));
    }

    [Test]
    public void ForWithWhileNested_GlobalIterationCounter()
    {
        // outer for (3) * inner while (4) = 12 total, limit 10 should fail
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 10 });

        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var count = 0;
                for (var i = 0; i < 3; i++) {
                    var j = 0;
                    while (j < 4) {
                        count = count + 1;
                        j = j + 1;
                    }
                }
                return count;
            }"));

        Assert.That(ex!.Message, Does.Contain("10"));
    }

    #endregion
}
