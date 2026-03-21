// All tests engine-only: Constraints config, CancellationToken, AlderExecutionLimitException assertions
// -- Alder-specific safety features with no Roslyn equivalent.

using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ExecutionLimitTests(CompilationMode mode)
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
        // 2 block stmts (var c, for) + 3 outer iters + 12 inner iters + 1 block stmt (return) = 18
        new("{ var c = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 4; j++) { c = c + 1; } } return c; }", 18, 12) { TestName = "Nested_3x4_Limit18" },
        // 1 block stmt (var c) + 1 block stmt (for) + 10 loop iters + 1 block stmt (return) = 13
        new("{ var c = 0; for (var i = 0; i < 10; i++) { c = c + 1; } return c; }", 13, 10) { TestName = "ForExact10_Limit13" },
    ];

    [TestCaseSource(nameof(LimitViolationCases))]
    public void StatementLimit_ThrowsAlderExecutionLimitException(string expr, int limit)
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with {
                        Constraints = new ExecutionConstraints { MaxStatements = limit }
        });
        var ex = Assert.Throws<AlderExecutionLimitException>(() => engine.Evaluate(expr));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
        Assert.That(ex.LimitValue, Is.EqualTo(limit));
    }

    [TestCaseSource(nameof(WithinLimitCases))]
    public void StatementLimit_WithinLimit_Succeeds(string expr, int limit, object expected)
    {
        var engine = TestEngineFactory.Create(mode, AlderOptions.Default with {
                        Constraints = new ExecutionConstraints { MaxStatements = limit }
        });
        var result = engine.Evaluate(expr);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CancellationToken_CanBeCancelled()
    {
        var engine = TestEngineFactory.Create(mode);
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
            }", cancellationToken: cts.Token);
        });

        // Give it a tiny bit of time to start
        Thread.Sleep(10);
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }
}
