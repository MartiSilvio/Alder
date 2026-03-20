using CsEval.Test._Infrastructure;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class CatchFilterTests(CompilationMode mode)
{
    [Test]
    public void CatchVariable_WithWhenFalse_IsOutOfScopeAfterTryCatch()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
        {
            try { throw new Exception("boom"); }
            catch (Exception ex) when (false) { }
            catch (Exception) { }
            return ex;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void CatchFilter_WhenGuardThrows_IsTreatedAsFalse()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("""
        {
            var r = "";
            try { throw new Exception("boom"); }
            catch (Exception ex) when (1 / 0 == 0) { r = "bad"; }
            catch (Exception) { r = "ok"; }
            return r;
        }
        """);
        Assert.That(result, Is.EqualTo("ok"));
    }

    [Test]
    public void CatchFilter_WhenGuardThrows_CatchVariableRemainsOutOfScope()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
        {
            try { throw new Exception("boom"); }
            catch (Exception ex) when (1 / 0 == 0) { }
            catch (Exception) { }
            return ex;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    [Test]
    public void LogicalError_DoesNotEvaluateRightOperandForTypeDiagnostics()
    {
        var counter = new CounterProbe();
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("counter", counter);

        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("1 && counter.Bump()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0019));
        Assert.That(counter.Count, Is.EqualTo(0));
    }

    [Test]
    public void SwitchExpression_WhenGuardThrows_DoesNotLeakPatternVariable()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
        {
            try
            {
                var z = 1 switch { int n when (1/0 == 0) => n, _ => 0 };
            }
            catch { }
            return n;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0103));
    }

    private sealed class CounterProbe
    {
        public int Count { get; private set; }
        public bool Bump()
        {
            Count++;
            return true;
        }
    }
}
