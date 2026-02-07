using CsEval.TestData.Data;

namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §12.4.7 numeric promotion compliance tests.
/// Validates that CsEval matches Roslyn output for both value AND type
/// across all three compilation modes.
///
/// References:
///   - ECMA-334 §12.4.7.2: Unary numeric promotion
///   - ECMA-334 §12.4.7.3: Binary numeric promotion
///   - ECMA-334 §12.11: Shift operators (left operand promotion)
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NumericPromotionTests(CompilationMode mode)
{
    #region ECMA-334 §12.4.7.2 -- Unary numeric promotion for bitwise NOT (~)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BitwiseNotParityCases))]
    public async Task UnaryPromotion_BitwiseNot_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.2 -- Unary numeric promotion for unary minus (-)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.NegateParityCases))]
    public async Task UnaryPromotion_Negate_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.2 -- Unary numeric promotion for unary plus (+)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.UnaryPlusParityCases))]
    public async Task UnaryPromotion_UnaryPlus_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 8 (both promoted to int)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule8ParityCases))]
    public async Task BinaryPromotion_Rule8_SmallTypesToInt(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 5 (long)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule5ParityCases))]
    public async Task BinaryPromotion_Rule5_Long(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 6 (uint + signed -> long)

    // ECMA-334 §12.4.7.3 Rule 6:
    // "Otherwise, if either operand is of type uint and the other operand is of type
    //  sbyte, short, or int, both operands are converted to type long."
    //
    // NOTE: Per ECMA-334 §10.2.11 (implicit constant expression conversions), a
    // constant expression of type int can be implicitly converted to uint if the value
    // is in range. So `5U + 3` has `3` converted to uint (Rule 7), not long (Rule 6).
    // To test Rule 6, we use explicitly-typed operands (short, sbyte) or negative int
    // constants that cannot be converted to uint.

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule6ParityCases))]
    public async Task BinaryPromotion_Rule6_UIntPlusSigned_ToLong(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // NOTE: ECMA-334 §10.2.11 allows implicit constant expression conversion where
    // an int literal like `3` can be implicitly converted to uint if it fits. Roslyn applies
    // this at compile time, making `5U + 3` -> uint (Rule 7).
    // CsEval now implements constant-aware promotion via
    // LiteralExpr.IsConstant + NumericDispatch.TryConstantPromotion. The `5U + 3` case
    // is now correctly handled and tested in the section 10.2.11 test region below.
    // Rule 6 tests above use explicitly-typed signed operands (short, sbyte) which are
    // NOT constant expressions and correctly apply Rule 6.

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 7 (uint)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule7ParityCases))]
    public async Task BinaryPromotion_Rule7_UInt(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 4 (ulong)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule4ParityCases))]
    public async Task BinaryPromotion_Rule4_ULong(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 3 (float)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule3ParityCases))]
    public async Task BinaryPromotion_Rule3_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 2 (double)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule2ParityCases))]
    public async Task BinaryPromotion_Rule2_Double(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 1 (decimal)

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BinaryRule1ParityCases))]
    public async Task BinaryPromotion_Rule1_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.11 -- Shift operators: left operand promotion

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.ShiftLeftParityCases))]
    public async Task ShiftPromotion_LeftShift_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.ShiftRightParityCases))]
    public async Task ShiftPromotion_RightShift_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary promotion across multiple operators

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.OtherOperatorsParityCases))]
    public async Task BinaryPromotion_OtherOperators_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.13 -- Bitwise binary operators with promotion

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.BitwiseBinaryParityCases))]
    public async Task BitwiseBinaryPromotion_SmallTypes_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.12 -- Relational operators with promotion

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.RelationalParityCases))]
    public async Task RelationalPromotion_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §10.2.11 -- Implicit constant expression conversions

    [TestCaseSource(typeof(NumericPromotionData), nameof(NumericPromotionData.ConstantExprConversionCases))]
    public async Task ConstantExprConversion_MatchesRoslyn(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region Specific value and type verification for key edge cases

    // These tests verify BOTH value AND type explicitly for the most critical edge cases.

    [Test]
    public async Task BitwiseNot_Byte_ValueAndType()
    {
        // ECMA-334 §12.4.7.2: ~(byte)5 promotes byte to int, then applies ~
        // Result: ~(int)5 = -6 as int
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("~(byte)5");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("~(byte)5");

        Assert.That(result, Is.EqualTo(-6), "Value should be -6 (not 250)");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int (not byte)");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task BitwiseNot_Char_ValueAndType()
    {
        // ECMA-334 §12.4.7.2: ~(char)'A' promotes char to int, then applies ~
        // 'A' = 65, ~65 = -66 as int
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("~(char)'A'");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("~(char)'A'");

        Assert.That(result, Is.EqualTo(-66), "Value should be -66");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task CharPlusChar_ValueAndType()
    {
        // ECMA-334 §12.4.7.3 Rule 8: char + char -> int
        // 'A' = 65, 'B' = 66, result = 131 as int
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(char)'A' + (char)'B'");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(char)'A' + (char)'B'");

        Assert.That(result, Is.EqualTo(131), "Value should be 131");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int (not char)");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task UIntPlusShort_ValueAndType()
    {
        // ECMA-334 §12.4.7.3 Rule 6: uint + short -> both promoted to long
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("5U + (short)3");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("5U + (short)3");

        Assert.That(result, Is.EqualTo(8L), "Value should be 8L");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(long)), "Type should be long");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    [Test]
    public async Task ByteLeftShift_ValueAndType()
    {
        // ECMA-334 §12.11: byte is promoted to int for shift operators
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(byte)5 << 2");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(byte)5 << 2");

        Assert.That(result, Is.EqualTo(20), "Value should be 20");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(int)), "Type should be int");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    #endregion
}
