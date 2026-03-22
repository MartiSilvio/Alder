using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Security;

[TestFixture(CompilationMode.Interpreted)]
public class LoopIterationLimitTests(CompilationMode mode)
{
    private AlderEngine CreateEngine(long maxLoopIterations)
    {
        return TestEngineFactory.Create(mode, o =>
            o.Constraints = new ExecutionConstraints { MaxLoopIterations = maxLoopIterations });
    }
    [Test]
    public void For_ExceedsLimit_Throws()
    {
        var engine = CreateEngine(5);
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var c = 0; for (var i = 0; i < 10; i++) { c = c + 1; } return c; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
        Assert.That(ex.LimitValue, Is.EqualTo(5));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0204));
    }

    [Test]
    public void For_ExactlyAtLimit_Succeeds()
    {
        var engine = CreateEngine(5);
        var result = engine.Evaluate("{ var c = 0; for (var i = 0; i < 5; i++) { c = c + 1; } return c; }");
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void For_UnderLimit_Succeeds()
    {
        var engine = CreateEngine(100);
        var result = engine.Evaluate("{ var c = 0; for (var i = 0; i < 10; i++) { c = c + 1; } return c; }");
        Assert.That(result, Is.EqualTo(10));
    }
    [Test]
    public void While_ExceedsLimit_Throws()
    {
        var engine = CreateEngine(3);
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var i = 0; while (i < 10) { i = i + 1; } return i; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void While_ExactlyAtLimit_Succeeds()
    {
        var engine = CreateEngine(3);
        var result = engine.Evaluate("{ var i = 0; while (i < 3) { i = i + 1; } return i; }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void While_InfiniteLoop_Stopped()
    {
        var engine = CreateEngine(10);
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ while (true) { } }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
        Assert.That(ex.LimitValue, Is.EqualTo(10));
    }
    [Test]
    public void DoWhile_ExceedsLimit_Throws()
    {
        var engine = CreateEngine(3);
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var i = 0; do { i = i + 1; } while (i < 10); return i; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void DoWhile_ExactlyAtLimit_Succeeds()
    {
        var engine = CreateEngine(4);
        var result = engine.Evaluate("{ var i = 0; do { i = i + 1; } while (i < 4); return i; }");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void DoWhile_SingleIteration_UnderLimit()
    {
        var engine = CreateEngine(5);
        var result = engine.Evaluate("{ var i = 99; do { i = i + 1; } while (false); return i; }");
        Assert.That(result, Is.EqualTo(100));
    }
    [Test]
    public void ForEach_ExceedsLimit_Throws()
    {
        var engine = CreateEngine(3);
        engine.SetVariable("items", new[] { 1, 2, 3, 4, 5, 6, 7 });
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var sum = 0; foreach (var x in items) { sum = sum + x; } return sum; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void ForEach_ExactlyAtLimit_Succeeds()
    {
        var engine = CreateEngine(5);
        engine.SetVariable("items", new[] { 10, 20, 30, 40, 50 });
        var result = engine.Evaluate("{ var sum = 0; foreach (var x in items) { sum = sum + x; } return sum; }");
        Assert.That(result, Is.EqualTo(150));
    }
    [Test]
    public void NestedFor_IterationsAccumulateAcrossLoops()
    {
        var engine = CreateEngine(15);
        // 3 outer * 4 inner = 12 total iterations — under 15
        var result = engine.Evaluate("""
            {
                var c = 0;
                for (var i = 0; i < 3; i++) {
                    for (var j = 0; j < 4; j++) {
                        c = c + 1;
                    }
                }
                return c;
            }
            """);
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public void NestedFor_ExceedsGlobalLimit_Throws()
    {
        var engine = CreateEngine(10);
        // 3 outer * 4 inner = 12 total iterations — exceeds 10
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("""
                {
                    var c = 0;
                    for (var i = 0; i < 3; i++) {
                        for (var j = 0; j < 4; j++) {
                            c = c + 1;
                        }
                    }
                    return c;
                }
                """));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void SequentialLoops_IterationsAccumulate()
    {
        var engine = CreateEngine(10);
        // first loop: 5 iterations, second loop: 5 iterations = 10 total — exactly at limit
        var result = engine.Evaluate("""
            {
                var c = 0;
                for (var i = 0; i < 5; i++) { c = c + 1; }
                for (var j = 0; j < 5; j++) { c = c + 1; }
                return c;
            }
            """);
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void SequentialLoops_ExceedsAccumulatedLimit()
    {
        var engine = CreateEngine(8);
        // first loop: 5 iterations, second loop starts at 5, exceeds 8 on iter 4
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("""
                {
                    var c = 0;
                    for (var i = 0; i < 5; i++) { c = c + 1; }
                    for (var j = 0; j < 5; j++) { c = c + 1; }
                    return c;
                }
                """));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void MixedLoopTypes_IterationsAccumulate()
    {
        var engine = CreateEngine(9);
        // for: 3, while: 3, do-while: 3 = 9 total iterations — exactly at limit
        var result = engine.Evaluate("""
            {
                var c = 0;
                for (var i = 0; i < 3; i++) { c = c + 1; }
                var j = 0;
                while (j < 3) { c = c + 1; j = j + 1; }
                var k = 0;
                do { c = c + 1; k = k + 1; } while (k < 3);
                return c;
            }
            """);
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void DeeplyNested_3Levels_Throws()
    {
        var engine = CreateEngine(20);
        // 3 * 3 * 3 = 27 total — exceeds 20
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("""
                {
                    var c = 0;
                    for (var i = 0; i < 3; i++) {
                        for (var j = 0; j < 3; j++) {
                            for (var k = 0; k < 3; k++) {
                                c = c + 1;
                            }
                        }
                    }
                    return c;
                }
                """));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }
    [Test]
    public void BreakExitsEarly_IterationsStillCounted()
    {
        var engine = CreateEngine(5);
        // loop breaks after 3 iterations — under limit of 5
        var result = engine.Evaluate("""
            {
                var c = 0;
                for (var i = 0; i < 100; i++) {
                    if (i >= 3) break;
                    c = c + 1;
                }
                return c;
            }
            """);
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void ContinueDoesNotSkipCounting()
    {
        var engine = CreateEngine(10);
        // 10 iterations total, continue skips body but iteration still counted
        var result = engine.Evaluate("""
            {
                var c = 0;
                for (var i = 0; i < 10; i++) {
                    if (i % 2 == 0) continue;
                    c = c + 1;
                }
                return c;
            }
            """);
        Assert.That(result, Is.EqualTo(5));
    }
    [Test]
    public void Until_ExceedsLimit_Throws()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.LanguageMode = LanguageMode.Extended;
            o.Constraints = new ExecutionConstraints { MaxLoopIterations = 3 };
        });

        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var i = 0; until (i >= 10) { i = i + 1; } return i; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void Until_WithinLimit_Succeeds()
    {
        var engine = TestEngineFactory.Create(mode, o =>
        {
            o.LanguageMode = LanguageMode.Extended;
            o.Constraints = new ExecutionConstraints { MaxLoopIterations = 10 };
        });

        var result = engine.Evaluate("{ var i = 0; until (i >= 5) { i = i + 1; } return i; }");
        Assert.That(result, Is.EqualTo(5));
    }
    [Test]
    public void NoConstraints_LoopsRunFreely()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("{ var c = 0; for (var i = 0; i < 10000; i++) { c = c + 1; } return c; }");
        Assert.That(result, Is.EqualTo(10000));
    }

    [Test]
    public void ZeroLimit_DisablesCheck()
    {
        var engine = TestEngineFactory.Create(mode, o =>
            o.Constraints = new ExecutionConstraints { MaxLoopIterations = 0 });
        var result = engine.Evaluate("{ var c = 0; for (var i = 0; i < 100; i++) { c = c + 1; } return c; }");
        Assert.That(result, Is.EqualTo(100));
    }
    [Test]
    public void Exception_HasCorrectProperties()
    {
        var engine = CreateEngine(5);
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var i = 0; while (true) { i = i + 1; } }"));

        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
        Assert.That(ex.LimitValue, Is.EqualTo(5));
        Assert.That(ex.ActualValue, Is.EqualTo(6));
        Assert.That(ex.StatementsExecuted, Is.GreaterThan(0));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0204));
    }
    [Test]
    public void DefaultLimit_IsApplied()
    {
        Assert.That(ExecutionConstraints.DefaultMaxLoopIterations, Is.EqualTo(1_000_000));

        var constraints = new ExecutionConstraints();
        Assert.That(constraints.MaxLoopIterations, Is.EqualTo(1_000_000));
    }
    [Test]
    public void LoopLimit_IndependentOfStatementLimit()
    {
        // High statement limit, low loop limit — loop limit should trigger
        var engine = TestEngineFactory.Create(mode, o =>
            o.Constraints = new ExecutionConstraints
            {
                MaxStatements = 1000,
                MaxLoopIterations = 5
            });

        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var i = 0; for (var j = 0; j < 20; j++) { i = i + 1; } return i; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }

    [Test]
    public void StatementLimit_CanFireBeforeLoopLimit()
    {
        // Low statement limit, high loop limit — statement limit should trigger
        var engine = TestEngineFactory.Create(mode, o =>
            o.Constraints = new ExecutionConstraints
            {
                MaxStatements = 3,
                MaxLoopIterations = 1000
            });

        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var i = 0; for (var j = 0; j < 20; j++) { i = i + 1; } return i; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.Statements));
    }
    [Test]
    public void ForEach_LinqRange_ExceedsLimit()
    {
        var engine = CreateEngine(5);
        engine.SetVariable("items", Enumerable.Range(1, 20).ToList());
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var sum = 0; foreach (var x in items) { sum = sum + x; } return sum; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
    }
    [Test]
    public void ForNoCondition_InfiniteLoop_Stopped()
    {
        var engine = CreateEngine(7);
        var ex = Assert.Throws<AlderExecutionLimitException>(() =>
            engine.Evaluate("{ var c = 0; for (;;) { c = c + 1; } return c; }"));
        Assert.That(ex!.LimitType, Is.EqualTo(ExecutionLimitType.LoopIterations));
        Assert.That(ex.LimitValue, Is.EqualTo(7));
    }
}
