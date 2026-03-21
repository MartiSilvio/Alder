using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5.5 -- String literals and escape sequences,
/// §12.8.3 -- Interpolated string expressions, §6.4.5.5 -- Unicode escape sequences (\u, \U, \x).
/// Engine-only string tests for error cases and null SetVariable patterns.
/// Parity tests migrated to TestData/Types/String/*.csx and Parity/StringTests.cs.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class StringTests(CompilationMode mode)
{
    #region Engine-only: Unicode/hex escape error tests

    [Test]
    public void Eval_UnicodeEscape_InvalidHex_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"""\u00GG"""));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1525));
    }

    [Test]
    public void Eval_UnicodeEscape_TooFewDigits_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(""" "\u00" """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1525));
    }

    [Test]
    public void Eval_UnicodeEscape8_TooFewDigits_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(""" "\U0000" """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1525));
    }

    [Test]
    public void Eval_HexEscape_NoDigits_Throws()
    {
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(""" "\xG" """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1525));
    }

    #endregion

    #region Engine-only: Null SetVariable tests (null not serializable for Roslyn)

    [Test]
    public void StringEquality_WithNull()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("s", null);

        Assert.That(engine.Evaluate("s == null"), Is.True);
        Assert.That(engine.Evaluate("s != null"), Is.False);
        Assert.That(engine.Evaluate(""" "hello" == null """), Is.False);
    }

    #endregion
}
