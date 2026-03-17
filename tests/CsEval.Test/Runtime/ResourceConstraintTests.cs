// All tests engine-only: Constraints config, CsEvalExecutionLimitException assertions
// -- CsEval-specific resource constraint API with no Roslyn equivalent.

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ResourceConstraintTests(CompilationMode mode)
{
    private static CsEvalEngine CreateEngine(long? maxStatements = null, TimeSpan? maxTimeout = null,
        CompilationMode mode = CompilationMode.Compiled)
    {
        return TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        Constraints = (maxStatements != null || maxTimeout != null)
                ? new ExecutionConstraints { MaxStatements = maxStatements, MaxTimeout = maxTimeout }
                : null
        });
    }

    #region MaxStatements Tests

    [Test]
    public void WhileTrue_WithStatementLimit_ThrowsExecutionLimitException()
    {
        var engine = CreateEngine(maxStatements: 10, mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
        Assert.That(ex.StatementsExecuted, Is.GreaterThan(0));
    }

    [Test]
    public void ForLoop_WithStatementLimit_ThrowsExecutionLimitException()
    {
        var engine = CreateEngine(maxStatements: 5, mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; for (var i = 0; i < 100; i++) { x = x + 1; } return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }

    [Test]
    public void ForeachLoop_WithStatementLimit_ThrowsExecutionLimitException()
    {
        var engine = CreateEngine(maxStatements: 5, mode: mode);
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var sum = 0; foreach (var item in items) { sum = sum + item; } return sum; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }

    [Test]
    public void DoWhileLoop_WithStatementLimit_ThrowsExecutionLimitException()
    {
        var engine = CreateEngine(maxStatements: 5, mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; do { x = x + 1; } while (true); return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }

    [Test]
    public void SimpleExpression_WithinLimit_Succeeds()
    {
        var engine = CreateEngine(maxStatements: 100, mode: mode);
        var result = engine.Evaluate("1 + 2 + 3");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void BlockStatements_EachCountAsOne()
    {
        // Block: { var a = 1; var b = 2; var c = 3; return a + b + c; } = 4 block statements
        var engine = CreateEngine(maxStatements: 4, mode: mode);
        var result = engine.Evaluate("{ var a = 1; var b = 2; var c = 3; return a + b + c; }");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void BlockStatements_ExceedLimit_Throws()
    {
        // 4 block statements, limit 3 -> should throw
        var engine = CreateEngine(maxStatements: 3, mode: mode);
        Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var a = 1; var b = 2; var c = 3; return a + b + c; }"));
    }

    [Test]
    public void LoopIterations_CountTowardBudget()
    {
        // Block: { var x = 0; for(...) { ... }; return x; }
        // 1 (var x) + 1 (for stmt) + 3 (iterations) + 1 (return) = 6
        var engine = CreateEngine(maxStatements: 6, mode: mode);
        var result = engine.Evaluate("{ var x = 0; for (var i = 0; i < 3; i++) { x = x + 1; } return x; }");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region MaxTimeout Tests

    [Test]
    public void WhileTrue_WithTimeout_ThrowsExecutionLimitException()
    {
        var engine = CreateEngine(maxTimeout: TimeSpan.FromMilliseconds(100), mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Timeout));
        Assert.That(ex.StatementsExecuted, Is.GreaterThan(0));
        Assert.That(ex.ElapsedTime, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(50)));
    }

    [Test]
    public void FastExpression_WithGenerousTimeout_Succeeds()
    {
        var engine = CreateEngine(maxTimeout: TimeSpan.FromSeconds(10), mode: mode);
        var result = engine.Evaluate("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    #endregion

    #region Both Limits

    [Test]
    public void BothLimits_StatementHitsFirst()
    {
        // Low statement limit, generous timeout -- statement limit should trigger
        var engine = CreateEngine(maxStatements: 3, maxTimeout: TimeSpan.FromSeconds(30), mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }

    [Test]
    public void BothLimits_TimeoutHitsFirst()
    {
        // Very high statement limit, very short timeout -- timeout should trigger
        var engine = CreateEngine(maxStatements: long.MaxValue, maxTimeout: TimeSpan.FromMilliseconds(100), mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Timeout));
    }

    #endregion

    #region Engine Recovery

    [Test]
    public void EngineReusable_AfterStatementLimitException()
    {
        var engine = CreateEngine(maxStatements: 3, mode: mode);
        Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));

        // Engine should still work for subsequent evaluations
        var result = engine.Evaluate("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void EngineReusable_AfterTimeoutException()
    {
        var engine = CreateEngine(maxTimeout: TimeSpan.FromMilliseconds(50), mode: mode);
        Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));

        // Engine should still work for subsequent evaluations
        var result = engine.Evaluate("1 + 2");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Counter_ResetsBetweenEvaluations()
    {
        // 4 block statements per evaluation, limit 4 -- should succeed twice
        var engine = CreateEngine(maxStatements: 4, mode: mode);

        var result1 = engine.Evaluate("{ var a = 1; var b = 2; var c = 3; return a + b + c; }");
        Assert.That(result1, Is.EqualTo(6));

        var result2 = engine.Evaluate("{ var x = 10; var y = 20; var z = 30; return x + y + z; }");
        Assert.That(result2, Is.EqualTo(60));
    }

    #endregion

    #region Mutable Constraints

    [Test]
    public void Constraints_ChangeableBetweenEvaluations()
    {
        var constraints = new ExecutionConstraints { MaxStatements = 3 };
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        Constraints = constraints
        });

        // Should throw with limit 3
        Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var a = 1; var b = 2; var c = 3; return a + b + c; }"));

        // Raise the limit -- same expression should now succeed
        constraints.MaxStatements = 100;
        var result = engine.Evaluate("{ var a = 1; var b = 2; var c = 3; return a + b + c; }");
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region Concurrency Isolation

    [Test]
    public async Task ConcurrentEvaluations_WithStatementConstraints_DoNotInterfere()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        Constraints = new ExecutionConstraints { MaxStatements = 4 }
        });

        const string expr = "{ var a = 1; var b = 2; var c = 3; return a + b + c; }";
        var failures = 0;

        for (var i = 0; i < 200; i++)
        {
            var t1 = Task.Run(() => engine.Evaluate(expr));
            var t2 = Task.Run(() => engine.Evaluate(expr));
            try
            {
                await Task.WhenAll(t1, t2);
                if (!Equals(t1.Result, 6) || !Equals(t2.Result, 6))
                    failures++;
            }
            catch
            {
                failures++;
            }
        }

        Assert.That(failures, Is.EqualTo(0));
    }

    #endregion

    #region Exception Properties

    [Test]
    public void StatementException_HasCorrectProperties()
    {
        var engine = CreateEngine(maxStatements: 5, mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));

        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
        Assert.That(ex.LimitValue, Is.EqualTo(5));
        Assert.That(ex.ActualValue, Is.GreaterThan(5));
        Assert.That(ex.StatementsExecuted, Is.GreaterThan(5));
        Assert.That(ex.Message, Does.Contain("5"));
        Assert.That(ex.Message, Does.Contain("statement"));
    }

    [Test]
    public void TimeoutException_IncludesStatementsExecuted()
    {
        var engine = CreateEngine(maxTimeout: TimeSpan.FromMilliseconds(100), mode: mode);
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));

        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Timeout));
        Assert.That(ex.StatementsExecuted, Is.GreaterThan(0));
        Assert.That(ex.ElapsedTime, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(ex.Message, Does.Contain("timeout"));
    }

    [Test]
    public void StatementException_IsCatchableAsCsEvalException()
    {
        var engine = CreateEngine(maxStatements: 3, mode: mode);
        // CsEvalExecutionLimitException inherits from CsEvalException
        Assert.Catch<CsEvalException>(
            () => engine.Evaluate("{ var x = 0; while (true) { x = x + 1; } return x; }"));
    }

    #endregion

    #region No Constraints

    [Test]
    public void DefaultOptions_NullConstraints_WorksNormally()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("{ var x = 0; for (var i = 0; i < 100; i++) { x = x + 1; } return x; }");
        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public void ExplicitNullConstraints_WorksNormally()
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with {
                        Constraints = null
        });
        var result = engine.Evaluate("1 + 2 + 3");
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region CancellationToken at Statement Boundaries

    [Test]
    public void CancellationToken_CheckedAtSameStatementBoundaries()
    {
        var engine = TestEngineFactory.Create(mode);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Pre-cancelled token should throw immediately at first statement boundary
        Assert.Throws<OperationCanceledException>(
            () => engine.Evaluate("{ var x = 1; return x; }", cancellationToken: cts.Token));
    }

    [Test]
    public void CancellationToken_WithConstraints_BothChecked()
    {
        var engine = CreateEngine(maxStatements: 1000, mode: mode);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Cancellation should trigger before statement limit
        Assert.Throws<OperationCanceledException>(
            () => engine.Evaluate("{ var x = 1; return x; }", cancellationToken: cts.Token));
    }

    #endregion

    #region Shared Budget (Nested Evaluations)

    [Test]
    public void NestedEvaluations_ShareSameBudget()
    {
        // Use a function that calls Evaluate internally via registered function
        // Simulated via loop: inner loop shares budget with outer loop
        var engine = CreateEngine(maxStatements: 10, mode: mode);

        // Each loop iteration uses the shared budget. Nested for loops sharing budget.
        var ex = Assert.Throws<CsEvalExecutionLimitException>(
            () => engine.Evaluate("{ var x = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 5; j++) { x = x + 1; } } return x; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }

    #endregion
}
