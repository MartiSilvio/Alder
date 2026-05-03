using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class EvaluationChaosTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void InfiniteLoop_ShouldTerminate_WithMaxStatements()
    {
        var engine = TestEngineFactory.Create(Mode, o => o.Constraints = new ExecutionConstraints { MaxStatements = 1000 });

        var expr = "{ var i = 0; while(true) { i++; } }";

        Assert.Throws<AlderExecutionLimitException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void NestedLoops_ExponentialComplexity_ShouldRespectMaxStatements()
    {
        var engine = TestEngineFactory.Create(Mode, o => o.Constraints = new ExecutionConstraints { MaxStatements = 5000 });

        const string expr = @"{
            var count = 0;
            for(var i=0; i<100; i++) {
                for(var j=0; j<100; j++) {
                    for(var k=0; k<100; k++) {
                        count++;
                    }
                }
            }
            return count;
        }";

        Assert.Throws<AlderExecutionLimitException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void CharPlusInt_PromotesToInt()
    {
        var result = Engine.Evaluate("'a' + 1");
        Assert.That(result, Is.EqualTo(98));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void AdditiveOperators_WithNullLiteral_UseLiftedOperator()
    {
        Assert.That(Engine.Evaluate("null + 5"), Is.Null);
        Assert.That(Engine.Evaluate("5 + null"), Is.Null);
    }

    [TestCase("1 + true")]
    [TestCase("true + false")]
    public void AdditiveOperators_WithInvalidOperands_ReportCSharpDiagnostic(string expr)
    {
        var ex = Assert.Throws<AlderException>(() => Engine.Evaluate(expr));
        Assert.That(ex!.Message, Does.Contain("CS0019"));
    }

    [Test]
    public void ArithmeticOverflow_DefaultContext_WrapsToNegative()
    {
        var result = Engine.Evaluate($"{int.MaxValue} + 1");
        Assert.That(result, Is.EqualTo(int.MinValue));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void ArithmeticOverflow_CheckedContext_ThrowsOverflow()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(int.MaxValue + 1)"));
    }

    [Test]
    public void ArithmeticOverflow_UncheckedContext_WrapsToNegative()
    {
        var result = Engine.Evaluate("unchecked(int.MaxValue + 1)");
        Assert.That(result, Is.EqualTo(int.MinValue));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void CheckedContext_UintOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(uint.MaxValue + 1u)"));
    }

    [Test]
    public void CheckedContext_UlongOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(ulong.MaxValue + 1UL)"));
    }

    [Test]
    public void CheckedContext_MultipleOps_OnlyLastOverflows()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(1 + 2 + int.MaxValue)"));
    }

    [Test]
    public void CheckedContext_NarrowingCast_Throws()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked((byte)256)"));
    }

    [Test]
    public void UncheckedContext_NarrowingCast_Truncates()
    {
        var result = Engine.Evaluate("unchecked((byte)256)");
        Assert.That(result, Is.EqualTo((byte)0));
        Assert.That(result, Is.TypeOf<byte>());
    }

    [Test]
    public void DivisionByZero_ShouldThrow()
    {
        var expr = "1 / 0";
        Assert.Throws<DivideByZeroException>(() => Engine.Evaluate(expr));
    }

    [Test]
    public void SandboxBypass_Reflection_ShouldBeBlockedInSafeMode()
    {
        var safeEngine = TestEngineFactory.Create(Mode, o => o.Sandbox = SandboxOptions.Safe());
        var expr = "\"hello\".GetType()";

        var ex = Assert.Throws<AlderException>(() => safeEngine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0100));
    }
}
