using CsEval.Parsing;

namespace CsEval.Test.Runtime;

/// <summary>
/// ECMA-334 §8.5 — Variable declarations, §8.5.1 — Local variable declarations,
/// §10.2 — Implicit conversions, §10.6.1 — Nullable conversions.
/// Engine-only tests: error assertions, SetVariable with non-serializable types.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class VariableDeclarationTests(CompilationMode mode)
{
    #region Engine-only: error tests

    // Engine-only: error test (CsEvalParserException assertion)
    [Test]
    public void Var_NullAssignment_ThrowsCsEvalParserException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalParserException>(() => engine.Evaluate("{ var x = null; return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot assign null to an implicitly-typed variable"));
    }

    // Engine-only: error test (CsEvalParserException assertion)
    [Test]
    public void Var_NullAssignment_InForLoop_ThrowsCsEvalParserException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalParserException>(() => engine.Evaluate("{ for (var x = null; x != null; x = null) { } return 0; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot assign null to an implicitly-typed variable"));
    }

    #endregion

    #region Engine-only: SetVariable with non-serializable types

    // Engine-only: SetVariable with byte (cannot serialize for Roslyn parity)
    [Test]
    public void TypedDeclaration_Int_CoercesFromSmaller()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("b", (byte)100);
        var result = engine.Evaluate("{ int x = b; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(100));
    }

    #endregion

    #region Engine-only: type validation error tests

    // Engine-only: error test (CsEvalException assertion)
    [Test]
    public void TypedDeclaration_Int_ThrowsOnStringAssignment()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int x = \"hello\"; return x; }"));
    }

    // Engine-only: error test (CsEvalException assertion)
    [Test]
    public void TypedDeclaration_Int_ThrowsOnNullAssignment()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int x = null; return x; }"));
    }

    // Engine-only: error test (CsEvalException assertion)
    [Test]
    public void TypedDeclaration_String_ThrowsOnIntAssignment()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ string x = 42; return x; }"));
    }

    // Engine-only: error test (CsEvalException assertion)
    [Test]
    public void TypedDeclaration_Bool_ThrowsOnIntAssignment()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ bool x = 1; return x; }"));
    }

    // Engine-only: error test (CsEvalException assertion)
    [Test]
    public void NullableInt_ThrowsOnStringAssignment()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate("{ int? x = \"hello\"; return x; }"));
    }

    #endregion
}
