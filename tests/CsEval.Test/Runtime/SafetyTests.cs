namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SafetyTests(CompilationMode mode)
{
    private static IEnumerable<TestCaseData> LimitViolationCases() =>
    [
        new("{ var c = 0; for (var i = 0; i < 10; i++) { for (var j = 0; j < 10; j++) { c = c + 1; } } return c; }", 50) { TestName = "Nested_10x10_Limit50" },
        new("{ var c = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 4; j++) { c = c + 1; } } return c; }", 10) { TestName = "Nested_3x4_Limit10" },
        new("{ var c = 0; for (var i = 0; i < 6; i++) { c = c + 1; } for (var j = 0; j < 6; j++) { c = c + 1; } return c; }", 10) { TestName = "Sequential_6Plus6_Limit10" },
        new("{ var c = 0; for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { for (var k = 0; k < 2; k++) { c = c + 1; } } } return c; }", 5) { TestName = "DeeplyNested_2x2x2_Limit5" },
        new("{ var c = 0; while (true) { c = c + 1; } return c; }", 5) { TestName = "InfiniteWhile_Limit5" },
        new("{ var c = 0; for (var i = 0; i < 11; i++) { c = c + 1; } return c; }", 10) { TestName = "For11_Limit10" },
        new("{ var c = 0; for (var i = 0; i < 3; i++) { c = c + 1; } var j = 0; while (j < 3) { c = c + 1; j = j + 1; } var k = 0; do { c = c + 1; k = k + 1; } while (k < 3); return c; }", 7) { TestName = "MixedLoops_Limit7" },
        new("{ var c = 0; for (var i = 0; i < 3; i++) { var j = 0; while (j < 4) { c = c + 1; j = j + 1; } } return c; }", 10) { TestName = "ForWhileNested_Limit10" },
    ];

    private static IEnumerable<TestCaseData> WithinLimitCases() =>
    [
        new("{ var c = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 4; j++) { c = c + 1; } } return c; }", 15, 12) { TestName = "Nested_3x4_Limit15" },
        new("{ var c = 0; for (var i = 0; i < 10; i++) { c = c + 1; } return c; }", 10, 10) { TestName = "ForExact10_Limit10" },
    ];

    [TestCaseSource(nameof(LimitViolationCases))]
    public void IterationLimit_ThrowsCsEvalException(string expr, int limit)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = limit });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate(expr));
        Assert.That(ex!.Message, Does.Contain(limit.ToString()));
    }

    [TestCaseSource(nameof(WithinLimitCases))]
    public void IterationLimit_WithinLimit_Succeeds(string expr, int limit, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = limit });
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CancellationToken_CanBeCancelled()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode, MaxIterations = 0 });
        // Use a very large array to ensure it takes some time
        var items = Enumerable.Range(1, 10_000_000).ToArray();
        engine.SetVariable("items", items);
        using var cts = new CancellationTokenSource();

        var task = Task.Run(() =>
        {
            return engine.Evaluate(@"
            {
                var sum = 0L;
                foreach (var item in items) {
                    sum = sum + item;
                }
                return sum;
            }", null, cts.Token);
        });

        // Give it a tiny bit of time to start
        Thread.Sleep(10);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }
}
