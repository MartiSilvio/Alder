using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class CatchFilterTests(CompilationMode mode)
{
    [Test]
    public void CatchVariable_WithWhenFalse_IsOutOfScopeAfterTryCatch()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
        {
            try { throw new Exception("boom"); }
            catch (Exception ex) when (false) { }
            catch (Exception) { }
            return ex;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
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
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
        {
            try { throw new Exception("boom"); }
            catch (Exception ex) when (1 / 0 == 0) { }
            catch (Exception) { }
            return ex;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void LogicalError_DoesNotEvaluateRightOperandForTypeDiagnostics()
    {
        var counter = new CounterProbe();
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("counter", counter);

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("1 && counter.Bump()"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
        Assert.That(counter.Count, Is.EqualTo(0));
    }

    [Test]
    public void SwitchExpression_WhenGuardThrows_DoesNotLeakPatternVariable()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
        {
            try
            {
                var z = 1 switch { int n when (1/0 == 0) => n, _ => 0 };
            }
            catch { }
            return n;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

    [Test]
    public void EngineFault_IsNotSwallowedByTypedCatch()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("probe", new EngineFaultProbe());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
        {
            var r = "uncaught";
            try { probe.Erupt(); }
            catch (Exception) { r = "caught"; }
            return r;
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0319));
    }

    [Test]
    public void EngineFault_IsNotSwallowedByBareCatch()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("probe", new EngineFaultProbe());

        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""
        {
            try { probe.Erupt(); }
            catch { return "caught"; }
            return "uncaught";
        }
        """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0319));
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

    // Throws an engine/host AOT limitation fault (ALDR0319). A rule's own try/catch must never
    // swallow it — it has to propagate to the host — and that must hold identically in the
    // interpreter and the compiled backend.
    private sealed class EngineFaultProbe
    {
        public object Erupt() =>
            throw new AlderException(DiagnosticDescriptors.GeneratedClosureRequired, "Test.Closure");
    }
}
