namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §12.17 — Null coalescing operator (??), §12.21 — Null coalescing assignment (??=),
/// §12.8.8 — Null-conditional member access (?.), §12.8.12 — Null-conditional element access (?[]),
/// §12.4.8 — Lifted operators for nullable value types.
/// Engine-only tests: error assertions, SetVariable with null/non-serializable types.
/// Parity tests migrated to TestData/Runtime/NullHandling/*.csx
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class NullHandlingTests(CompilationMode mode)
{
    #region Engine-only: error tests (CsEvalException assertions)

    // Engine-only: CsEvalException assertion for ??= on non-nullable type
    [Test]
    public void Eval_NullCoalesceAssign_ThrowsOnNonNullableType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() => engine.Evaluate("""
                                                                      {
                                                                          var x = 10;
                                                                          x ??= 42;
                                                                          return x;
                                                                      }
                                                                      """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CS0019));
        Assert.That(ex!.Message, Does.Contain("??=").And.Contain("Int32"));
    }

    #endregion

    #region Engine-only: SetVariable with null (engine API specific)

    // Engine-only: SetVariable with null + ?? operator
    [Test]
    public void Eval_NullCoalesce()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", null);
        engine.SetVariable("y", "default");

        Assert.That(engine.Evaluate("x ?? y"), Is.EqualTo("default"));
        Assert.That(engine.Evaluate("y ?? \"other\""), Is.EqualTo("default"));
    }

    // Engine-only: SetVariable with null + dynamic property access
    [Test]
    public void Eval_NullSafeAccess()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);

        Assert.That(engine.Evaluate("obj?.Name"), Is.Null);
    }

    #endregion

    #region Engine-only: SetVariable with non-serializable types (?[] element access)

    // Engine-only: SetVariable with null for ?[] element access
    [Test]
    public void NullConditionalIndex_NullArray_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);
        Assert.That(engine.Evaluate("arr?[0]"), Is.Null);
    }

    // Engine-only: SetVariable with int[] (non-serializable array type)
    [Test]
    public void NullConditionalIndex_NonNullArray_ReturnsElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", new[] { 1, 2, 3 });
        Assert.That(engine.Evaluate("arr?[1]"), Is.EqualTo(2));
    }

    // Engine-only: SetVariable with null + ?[] with ?? fallback
    [Test]
    public void NullConditionalIndex_WithNullCoalescing()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("arr", null);
        Assert.That(engine.Evaluate("arr?[0] ?? 42"), Is.EqualTo(42));
    }

    // Engine-only: SetVariable with Dictionary<string, int> (non-serializable)
    [Test]
    public void NullConditionalIndex_Dictionary()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("dict", new Dictionary<string, int> { ["key"] = 100 });
        Assert.That(engine.Evaluate("dict?[\"key\"]"), Is.EqualTo(100));
        engine.SetVariable("dict", null);
        Assert.That(engine.Evaluate("dict?[\"key\"]"), Is.Null);
    }

    // Engine-only: SetVariable with string + null reassignment for ?. method call
    [Test]
    public void NullConditional_MethodCall()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("s", "hello");
        Assert.That(engine.Evaluate("s?.ToUpper()"), Is.EqualTo("HELLO"));
        engine.SetVariable("s", null);
        Assert.That(engine.Evaluate("s?.ToUpper()"), Is.Null);
    }

    #endregion
}
