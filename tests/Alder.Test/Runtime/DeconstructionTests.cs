using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// Tests for tuple deconstruction (ECMA-334 §12.7 - Deconstruction).
/// Engine-only tests: error assertions, SetVariable with non-serializable types.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class DeconstructionTests(CompilationMode mode)
{
    #region Engine-only: error tests

    // Engine-only: error test, SetVariable with non-tuple value
    [Test]
    public void Deconstruct_NonTuple_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("val", 42);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("{ var (x, y) = val; return x; }"));
        Assert.That(ex!.Message, Does.Contain("CS8129"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS8129));
    }

    // Engine-only: error test (arity mismatch assertion)
    [Test]
    public void Deconstruct_ArityMismatch_TooMany_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("{ var (x, y, z) = (1, 2); return x; }"));
        // Reported at bind time as CS8130 ("cannot infer type of implicitly-typed deconstruction
        // variable") to match Roslyn — the extra variable has no source to infer from.
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8130));
    }

    // Engine-only: error test (arity mismatch assertion)
    [Test]
    public void Deconstruct_ArityMismatch_TooFew_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() =>
            engine.Evaluate("{ var (x, y) = (1, 2, 3); return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot deconstruct"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS8132));
    }

    #endregion
}
