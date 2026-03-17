namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §12.4.7 numeric promotion compliance tests.
/// Validates that CsEval matches Roslyn output for both value AND type
/// across all three compilation modes.
///
/// Engine-only tests retained here verify result TYPE explicitly via typeof() comparisons,
/// which cannot be expressed as .csx parity files (parity runner checks value + type automatically,
/// but these tests assert specific expected types for documentation clarity).
/// Standard numeric promotion value tests are in TestData/NumericPromotion/*.csx.
///
/// References:
///   - ECMA-334 §12.4.7.2: Unary numeric promotion
///   - ECMA-334 §12.4.7.3: Binary numeric promotion
///   - ECMA-334 §12.11: Shift operators (left operand promotion)
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class NumericPromotionTests(CompilationMode mode)
{
    #region Specific value and type verification for key edge cases

    // Engine-only: Type object comparison -- these tests verify BOTH value AND type explicitly

    [Test]
    public async Task BitwiseNot_Byte_ValueAndType()
    {
        // Engine-only: Type object comparison (verifies result type is typeof(int))
        // ECMA-334 §12.4.7.2: ~(byte)5 promotes byte to int, then applies ~
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("~(byte)5");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("~(byte)5");

        Assert.That(result, Is.EqualTo(-6), "Value should be -6 (not 250)");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int (not byte)");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task BitwiseNot_Char_ValueAndType()
    {
        // Engine-only: Type object comparison (verifies result type is typeof(int))
        // ECMA-334 §12.4.7.2: ~(char)'A' promotes char to int, then applies ~
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("~(char)'A'");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("~(char)'A'");

        Assert.That(result, Is.EqualTo(-66), "Value should be -66");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task CharPlusChar_ValueAndType()
    {
        // Engine-only: Type object comparison (verifies result type is typeof(int))
        // ECMA-334 §12.4.7.3 Rule 8: char + char -> int
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("(char)'A' + (char)'B'");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(char)'A' + (char)'B'");

        Assert.That(result, Is.EqualTo(131), "Value should be 131");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int (not char)");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task UIntPlusShort_ValueAndType()
    {
        // Engine-only: Type object comparison (verifies result type is typeof(long))
        // ECMA-334 §12.4.7.3 Rule 6: uint + short -> both promoted to long
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("5U + (short)3");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("5U + (short)3");

        Assert.That(result, Is.EqualTo(8L), "Value should be 8L");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(long)), "Type should be long");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task ByteLeftShift_ValueAndType()
    {
        // Engine-only: Type object comparison (verifies result type is typeof(int))
        // ECMA-334 §12.11: byte is promoted to int for shift operators
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("(byte)5 << 2");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(byte)5 << 2");

        Assert.That(result, Is.EqualTo(20), "Value should be 20");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    #endregion
}
