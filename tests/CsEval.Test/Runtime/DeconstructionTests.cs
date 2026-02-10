namespace CsEval.Test.Runtime;

/// <summary>
/// Tests for tuple deconstruction (ECMA-334 §12.7 - Deconstruction).
/// Engine-only tests: error assertions, SetVariable with non-serializable types.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DeconstructionTests(CompilationMode mode)
{
    #region Engine-only: error tests

    // Engine-only: error test, SetVariable with non-tuple value
    [Test]
    public void Deconstruct_NonTuple_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("val", 42);
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y) = val; return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot deconstruct non-tuple value"));
    }

    // Engine-only: error test (arity mismatch assertion)
    [Test]
    public void Deconstruct_ArityMismatch_TooMany_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y, z) = (1, 2); return x; }"));
        Assert.That(ex!.Message, Does.Contain("Deconstruction requires 3 values but tuple has 2"));
    }

    // Engine-only: error test (arity mismatch assertion)
    [Test]
    public void Deconstruct_ArityMismatch_TooFew_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y) = (1, 2, 3); return x; }"));
        Assert.That(ex!.Message, Does.Contain("Deconstruction requires 2 values but tuple has 3"));
    }

    #endregion
}
