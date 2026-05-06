using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Runtime;

/// <summary>
/// ECMA-334 §8.5 — Variable declarations, §8.5.1 — Local variable declarations,
/// §10.2 — Implicit conversions, §10.6.1 — Nullable conversions.
/// Engine-only tests: error assertions, SetVariable with non-serializable types.
/// </summary>
[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class VariableDeclarationTests(CompilationMode mode)
{
    #region Engine-only: error tests

    // Engine-only: error test (parser exception assertion)
    [Test]
    public void Var_NullAssignment_ThrowsAlderException()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ var x = null; return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot assign null to an implicitly-typed variable"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0815));
    }

    // Engine-only: error test (parser exception assertion)
    [Test]
    public void Var_NullAssignment_InForLoop_ThrowsAlderException()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ for (var x = null; x != null; x = null) { } return 0; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot assign null to an implicitly-typed variable"));
        Assert.That(ex.ErrorCode, Is.EqualTo(DiagnosticCode.CS0815));
    }

    #endregion

    #region Engine-only: SetVariable with non-serializable types

    // Engine-only: SetVariable with byte (cannot serialize for Roslyn parity)
    [Test]
    public void TypedDeclaration_Int_CoercesFromSmaller()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("b", (byte)100);
        var result = engine.Evaluate("{ int x = b; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(100));
    }

    #endregion

    #region Const declarations

    [Test]
    public void ConstDeclaration_TypedLocal_Works()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("{ const int x = 42; return x; }");
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ConstDeclaration_Assignment_ThrowsCs0131()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ const int x = 1; x = 2; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0131));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0131"));
    }

    [Test]
    public void ConstDeclaration_CompoundAssignment_ThrowsCs0131()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ const int x = 1; x += 2; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0131));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS0131"));
    }

    [Test]
    public void ConstDeclaration_Increment_ThrowsCs1059()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ const int x = 1; x++; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1059));
        Assert.That(ex.FormattedCode, Is.EqualTo("CS1059"));
    }

    #endregion

    #region Engine-only: type validation error tests

    // Engine-only: error test (AlderException assertion)
    [Test]
    public void TypedDeclaration_Int_ThrowsOnStringAssignment()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""{ int x = "hello"; return x; } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0029));
    }

    // Engine-only: error test (AlderException assertion)
    [Test]
    public void TypedDeclaration_Int_ThrowsOnNullAssignment()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ int x = null; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0037));
    }

    // Engine-only: error test (AlderException assertion)
    [Test]
    public void TypedDeclaration_String_ThrowsOnIntAssignment()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ string x = 42; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0029));
    }

    // Engine-only: error test (AlderException assertion)
    [Test]
    public void TypedDeclaration_Bool_ThrowsOnIntAssignment()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ bool x = 1; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0029));
    }

    [Test]
    public void TypedDeclaration_Byte_ThrowsOnIntVariableInitializer()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ int x = 1; byte b = x; return b; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0266));
    }

    // Engine-only: error test (AlderException assertion)
    [Test]
    public void NullableInt_ThrowsOnStringAssignment()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("""{ int? x = "hello"; return x; } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0029));
    }

    #endregion
}
