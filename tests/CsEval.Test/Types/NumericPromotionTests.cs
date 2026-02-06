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

    // ECMA-334 §12.4.7.2:
    // "Operands of type sbyte, byte, short, ushort, or char are converted to type int."
    // The predefined ~ operator applies unary numeric promotion before the operation.
    // Therefore ~(byte)5 should return int, not byte.

    [TestCase("~(byte)5", TestName = "BitwiseNot_Byte_ReturnsInt")]
    [TestCase("~(sbyte)5", TestName = "BitwiseNot_SByte_ReturnsInt")]
    [TestCase("~(short)5", TestName = "BitwiseNot_Short_ReturnsInt")]
    [TestCase("~(ushort)5", TestName = "BitwiseNot_UShort_ReturnsInt")]
    [TestCase("~(char)'A'", TestName = "BitwiseNot_Char_ReturnsInt")]
    [TestCase("~5", TestName = "BitwiseNot_Int_StaysInt")]
    [TestCase("~5L", TestName = "BitwiseNot_Long_StaysLong")]
    [TestCase("~5U", TestName = "BitwiseNot_UInt_StaysUInt")]
    [TestCase("~5UL", TestName = "BitwiseNot_ULong_StaysULong")]
    public async Task UnaryPromotion_BitwiseNot_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.2 -- Unary numeric promotion for unary minus (-)

    // ECMA-334 §12.4.7.2:
    // "Unary numeric promotion ... is used by the predefined unary -, +, and ~ operators."
    // Operands of type sbyte, byte, short, ushort, or char are converted to int.

    [TestCase("-(byte)5", TestName = "Negate_Byte_ReturnsInt")]
    [TestCase("-(sbyte)5", TestName = "Negate_SByte_ReturnsInt")]
    [TestCase("-(short)5", TestName = "Negate_Short_ReturnsInt")]
    [TestCase("-(ushort)5", TestName = "Negate_UShort_ReturnsInt")]
    [TestCase("-(char)'A'", TestName = "Negate_Char_ReturnsInt")]
    [TestCase("-5", TestName = "Negate_Int_StaysInt")]
    [TestCase("-5L", TestName = "Negate_Long_StaysLong")]
    public async Task UnaryPromotion_Negate_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.2 -- Unary numeric promotion for unary plus (+)

    [TestCase("+(byte)5", TestName = "UnaryPlus_Byte_ReturnsInt")]
    [TestCase("+(sbyte)5", TestName = "UnaryPlus_SByte_ReturnsInt")]
    [TestCase("+(short)5", TestName = "UnaryPlus_Short_ReturnsInt")]
    [TestCase("+(ushort)5", TestName = "UnaryPlus_UShort_ReturnsInt")]
    [TestCase("+(char)'A'", TestName = "UnaryPlus_Char_ReturnsInt")]
    [TestCase("+5", TestName = "UnaryPlus_Int_StaysInt")]
    [TestCase("+5L", TestName = "UnaryPlus_Long_StaysLong")]
    public async Task UnaryPromotion_UnaryPlus_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 8 (both promoted to int)

    // ECMA-334 §12.4.7.3 Rule 8:
    // "Otherwise, both operands are converted to type int."
    // This applies to all small integer types: byte, sbyte, short, ushort, char.

    [TestCase("(byte)5 + (byte)3", TestName = "BinaryPromo_BytePlusByte_IsInt")]
    [TestCase("(sbyte)5 + (sbyte)3", TestName = "BinaryPromo_SBytePlusSByte_IsInt")]
    [TestCase("(short)5 + (short)3", TestName = "BinaryPromo_ShortPlusShort_IsInt")]
    [TestCase("(ushort)5 + (ushort)3", TestName = "BinaryPromo_UShortPlusUShort_IsInt")]
    [TestCase("(char)'A' + (char)'B'", TestName = "BinaryPromo_CharPlusChar_IsInt")]
    [TestCase("(byte)5 + (short)3", TestName = "BinaryPromo_BytePlusShort_IsInt")]
    [TestCase("(sbyte)5 + (ushort)3", TestName = "BinaryPromo_SBytePlusUShort_IsInt")]
    [TestCase("(byte)5 + (char)'A'", TestName = "BinaryPromo_BytePlusChar_IsInt")]
    public async Task BinaryPromotion_Rule8_SmallTypesToInt(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 5 (long)

    // ECMA-334 §12.4.7.3 Rule 5:
    // "Otherwise, if either operand is of type long, the other operand is converted to type long."

    [TestCase("5 + 3L", TestName = "BinaryPromo_IntPlusLong_IsLong")]
    [TestCase("5L + 3", TestName = "BinaryPromo_LongPlusInt_IsLong")]
    [TestCase("(byte)5 + 3L", TestName = "BinaryPromo_BytePlusLong_IsLong")]
    [TestCase("(short)5 + 3L", TestName = "BinaryPromo_ShortPlusLong_IsLong")]
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

    [TestCase("5U + (short)3", TestName = "BinaryPromo_UIntPlusShort_IsLong")]
    [TestCase("5U + (sbyte)3", TestName = "BinaryPromo_UIntPlusSByte_IsLong")]
    [TestCase("(short)3 + 5U", TestName = "BinaryPromo_ShortPlusUInt_IsLong")]
    [TestCase("(sbyte)3 + 5U", TestName = "BinaryPromo_SBytePlusUInt_IsLong")]
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

    // ECMA-334 §12.4.7.3 Rule 7:
    // "Otherwise, if either operand is of type uint, the other operand is converted to type uint."
    // This applies ONLY when the other operand is NOT a signed type (sbyte, short, int).
    // Unsigned small types (byte, ushort) can be converted to uint.

    [TestCase("5U + 3U", TestName = "BinaryPromo_UIntPlusUInt_IsUInt")]
    [TestCase("5U + (byte)3", TestName = "BinaryPromo_UIntPlusByte_IsUInt")]
    [TestCase("5U + (ushort)3", TestName = "BinaryPromo_UIntPlusUShort_IsUInt")]
    [TestCase("5U + (char)'A'", TestName = "BinaryPromo_UIntPlusChar_IsUInt")]
    public async Task BinaryPromotion_Rule7_UInt(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 4 (ulong)

    // ECMA-334 §12.4.7.3 Rule 4:
    // "Otherwise, if either operand is of type ulong, the other operand is converted to type ulong."
    // Error if other operand is signed (sbyte, short, int, long).

    [TestCase("5UL + 3UL", TestName = "BinaryPromo_ULongPlusULong_IsULong")]
    [TestCase("5UL + 3U", TestName = "BinaryPromo_ULongPlusUInt_IsULong")]
    [TestCase("5UL + (byte)3", TestName = "BinaryPromo_ULongPlusByte_IsULong")]
    [TestCase("5UL + (ushort)3", TestName = "BinaryPromo_ULongPlusUShort_IsULong")]
    [TestCase("5UL + (char)'A'", TestName = "BinaryPromo_ULongPlusChar_IsULong")]
    public async Task BinaryPromotion_Rule4_ULong(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 3 (float)

    // ECMA-334 §12.4.7.3 Rule 3:
    // "Otherwise, if either operand is of type float, the other operand is converted to type float."

    [TestCase("5 + 3.0f", TestName = "BinaryPromo_IntPlusFloat_IsFloat")]
    [TestCase("5L + 3.0f", TestName = "BinaryPromo_LongPlusFloat_IsFloat")]
    [TestCase("(byte)5 + 3.0f", TestName = "BinaryPromo_BytePlusFloat_IsFloat")]
    public async Task BinaryPromotion_Rule3_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 2 (double)

    // ECMA-334 §12.4.7.3 Rule 2:
    // "Otherwise, if either operand is of type double, the other operand is converted to type double."

    [TestCase("5 + 3.0", TestName = "BinaryPromo_IntPlusDouble_IsDouble")]
    [TestCase("5L + 3.0", TestName = "BinaryPromo_LongPlusDouble_IsDouble")]
    [TestCase("5.0f + 3.0", TestName = "BinaryPromo_FloatPlusDouble_IsDouble")]
    [TestCase("(byte)5 + 3.0", TestName = "BinaryPromo_BytePlusDouble_IsDouble")]
    public async Task BinaryPromotion_Rule2_Double(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary numeric promotion: Rule 1 (decimal)

    // ECMA-334 §12.4.7.3 Rule 1:
    // "If either operand is of type decimal, the other operand is converted to type decimal,
    //  or a binding-time error occurs if the other operand is of type float or double."

    [TestCase("5 + 3.0m", TestName = "BinaryPromo_IntPlusDecimal_IsDecimal")]
    [TestCase("5L + 3.0m", TestName = "BinaryPromo_LongPlusDecimal_IsDecimal")]
    [TestCase("(byte)5 + 3.0m", TestName = "BinaryPromo_BytePlusDecimal_IsDecimal")]
    public async Task BinaryPromotion_Rule1_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.11 -- Shift operators: left operand promotion

    // ECMA-334 §12.11:
    // The predefined shift operators are:
    //   int operator <<(int x, int count);
    //   uint operator <<(uint x, int count);
    //   long operator <<(long x, int count);
    //   ulong operator <<(ulong x, int count);
    // Unary numeric promotion is applied to the left operand.
    // So (byte)5 << 2 promotes byte to int, result is int.

    [TestCase("(byte)5 << 2", TestName = "Shift_ByteLeft_ReturnsInt")]
    [TestCase("(sbyte)5 << 2", TestName = "Shift_SByteLeft_ReturnsInt")]
    [TestCase("(short)5 << 2", TestName = "Shift_ShortLeft_ReturnsInt")]
    [TestCase("(ushort)5 << 2", TestName = "Shift_UShortLeft_ReturnsInt")]
    [TestCase("(char)'A' << 2", TestName = "Shift_CharLeft_ReturnsInt")]
    [TestCase("5 << 2", TestName = "Shift_IntLeft_StaysInt")]
    [TestCase("5L << 2", TestName = "Shift_LongLeft_StaysLong")]
    [TestCase("5U << 2", TestName = "Shift_UIntLeft_StaysUInt")]
    [TestCase("5UL << 2", TestName = "Shift_ULongLeft_StaysULong")]
    public async Task ShiftPromotion_LeftShift_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("(byte)32 >> 2", TestName = "RightShift_Byte_ReturnsInt")]
    [TestCase("(sbyte)32 >> 2", TestName = "RightShift_SByte_ReturnsInt")]
    [TestCase("(short)32 >> 2", TestName = "RightShift_Short_ReturnsInt")]
    [TestCase("(ushort)32 >> 2", TestName = "RightShift_UShort_ReturnsInt")]
    [TestCase("(char)'A' >> 2", TestName = "RightShift_Char_ReturnsInt")]
    public async Task ShiftPromotion_RightShift_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 -- Binary promotion across multiple operators

    // Verify promotion works consistently across different operators (not just +).

    [TestCase("(byte)10 - (byte)3", TestName = "Subtraction_ByteMinusByte_IsInt")]
    [TestCase("(byte)5 * (byte)3", TestName = "Multiply_ByteTimesByte_IsInt")]
    [TestCase("(byte)10 / (byte)3", TestName = "Divide_ByteDivByte_IsInt")]
    [TestCase("(byte)10 % (byte)3", TestName = "Modulo_ByteModByte_IsInt")]
    [TestCase("(short)10 - (short)3", TestName = "Subtraction_ShortMinusShort_IsInt")]
    [TestCase("(short)5 * (short)3", TestName = "Multiply_ShortTimesShort_IsInt")]
    public async Task BinaryPromotion_OtherOperators_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.13 -- Bitwise binary operators with promotion

    // Bitwise &, |, ^ on small types should also promote to int per section 12.4.7.3.

    [TestCase("(byte)5 & (byte)3", TestName = "BitwiseAnd_ByteAndByte_IsInt")]
    [TestCase("(short)5 | (short)3", TestName = "BitwiseOr_ShortOrShort_IsInt")]
    [TestCase("(byte)5 ^ (byte)3", TestName = "BitwiseXor_ByteXorByte_IsInt")]
    [TestCase("(char)'A' & (char)'B'", TestName = "BitwiseAnd_CharAndChar_IsInt")]
    public async Task BitwiseBinaryPromotion_SmallTypes_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.12 -- Relational operators with promotion

    // Relational operators also apply binary numeric promotion.

    [TestCase("(byte)5 < (byte)10", TestName = "LessThan_ByteVsByte_IsPromotedToInt")]
    [TestCase("(short)5 > (short)3", TestName = "GreaterThan_ShortVsShort_IsPromotedToInt")]
    [TestCase("5U == 5", TestName = "Equality_UIntVsInt_PromotesToLong")]
    [TestCase("5U != 3", TestName = "NotEqual_UIntVsInt_PromotesToLong")]
    [TestCase("5U < 10", TestName = "LessThan_UIntVsInt_PromotesToLong")]
    public async Task RelationalPromotion_MatchesRoslyn(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §10.2.11 -- Implicit constant expression conversions

    // ECMA-334 §10.2.11: A constant expression of type int can be converted to
    // sbyte, byte, short, ushort, uint, or ulong, provided the value is within the range
    // of the destination type. This means `5U + 3` promotes the constant `3` to uint
    // (since 3 fits in uint), making it uint + uint -> uint (Rule 7), NOT uint + int -> long (Rule 6).

    [TestCase("5U + 3", 8U, TestName = "ConstantExpr_UIntPlusIntLiteral_ReturnsUInt")]
    [TestCase("3 + 5U", 8U, TestName = "ConstantExpr_IntLiteralPlusUInt_ReturnsUInt")]
    [TestCase("5UL + 3", 8UL, TestName = "ConstantExpr_ULongPlusIntLiteral_ReturnsULong")]
    [TestCase("3 + 5UL", 8UL, TestName = "ConstantExpr_IntLiteralPlusULong_ReturnsULong")]
    [TestCase("5U + 0", 5U, TestName = "ConstantExpr_UIntPlusZero_ReturnsUInt")]
    [TestCase("5U + (-1)", -1L + 5U, TestName = "ConstantExpr_UIntPlusNegativeInt_FallsToLong")]
    [TestCase("5U * 2", 10U, TestName = "ConstantExpr_UIntTimesIntLiteral_ReturnsUInt")]
    [TestCase("10U - 3", 7U, TestName = "ConstantExpr_UIntMinusIntLiteral_ReturnsUInt")]
    [TestCase("10U / 2", 5U, TestName = "ConstantExpr_UIntDivIntLiteral_ReturnsUInt")]
    [TestCase("10U % 3", 1U, TestName = "ConstantExpr_UIntModIntLiteral_ReturnsUInt")]
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
