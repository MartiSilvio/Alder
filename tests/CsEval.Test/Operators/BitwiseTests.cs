namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.13 — Logical operators (integer bitwise AND, OR, XOR, NOT),
/// §12.11 — Shift operators, §12.13.5 — Boolean logical operators (non-short-circuit).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class BitwiseTests(CompilationMode mode)
{
    #region ECMA-334 §12.13 — Integer Bitwise Operators

    // ECMA-334 §12.13.1: "The predefined integer logical operators are: int operator &(int x, int y); ..."
    [TestCase("5 & 3", 1, TestName = "And_5And3")]
    [TestCase("12 & 10", 8, TestName = "And_12And10")]
    [TestCase("255 & 0", 0, TestName = "And_WithZero")]
    [TestCase("15 & 15", 15, TestName = "And_SameValue")]
    [TestCase("5 | 3", 7, TestName = "Or_5Or3")]
    [TestCase("12 | 10", 14, TestName = "Or_12Or10")]
    [TestCase("255 | 0", 255, TestName = "Or_WithZero")]
    [TestCase("1 | 2 | 4", 7, TestName = "Or_Chained")]
    [TestCase("5 ^ 3", 6, TestName = "Xor_5Xor3")]
    [TestCase("12 ^ 10", 6, TestName = "Xor_12Xor10")]
    [TestCase("42 ^ 42", 0, TestName = "Xor_SameValueIsZero")]
    [TestCase("15 ^ 255", 240, TestName = "Xor_15Xor255")]
    // ECMA-334 §12.9.5: Bitwise complement operator
    [TestCase("~0", -1, TestName = "Not_Zero")]
    [TestCase("~1", -2, TestName = "Not_One")]
    [TestCase("~(-1)", 0, TestName = "Not_NegativeOne")]
    [TestCase("~~42", 42, TestName = "Not_DoubleNot")]
    public async Task Eval_IntegerBitwise(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.11 — Shift Operators

    // ECMA-334 §12.11: "The << and >> operators are used to perform bit-shifting operations."
    [TestCase("1 << 1", 2, TestName = "LeftShift_By1")]
    [TestCase("1 << 2", 4, TestName = "LeftShift_By2")]
    [TestCase("1 << 3", 8, TestName = "LeftShift_By3")]
    [TestCase("5 << 2", 20, TestName = "LeftShift_5By2")]
    [TestCase("3 << 4", 48, TestName = "LeftShift_3By4")]
    [TestCase("42 << 0", 42, TestName = "LeftShift_ByZero")]
    [TestCase("0 << 10", 0, TestName = "LeftShift_ZeroValue")]
    [TestCase("8 >> 1", 4, TestName = "RightShift_By1")]
    [TestCase("8 >> 2", 2, TestName = "RightShift_By2")]
    [TestCase("8 >> 3", 1, TestName = "RightShift_By3")]
    [TestCase("20 >> 2", 5, TestName = "RightShift_20By2")]
    [TestCase("48 >> 4", 3, TestName = "RightShift_48By4")]
    [TestCase("42 >> 0", 42, TestName = "RightShift_ByZero")]
    public async Task Eval_ShiftOperators(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.2 — Bitwise Operator Precedence

    // ECMA-334 §12.4.2: & binds tighter than ^, ^ tighter than |
    [TestCase("1 | 2 & 3", 3, TestName = "Precedence_AndBeforeOr")]
    [TestCase("1 << 2 < 5", true, TestName = "Precedence_ShiftBeforeComparison")]
    [TestCase("7 ^ 3 & 5", 6, TestName = "Precedence_AndBeforeXor")]
    [TestCase("1 | 2 ^ 3", 1, TestName = "Precedence_XorBeforeOr")]
    [TestCase("(2 + 3) & 7", 5, TestName = "Combined_ArithmeticAndBitwise")]
    [TestCase("10 | (3 * 2)", 14, TestName = "Combined_BitwiseAndMultiply")]
    [TestCase("(5 & 1) == 1 && true", true, TestName = "Combined_BitwiseAndLogical")]
    [TestCase("true ? 5 & 3 : 0", 1, TestName = "InTernary")]
    public async Task Eval_BitwisePrecedence(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.13.5 — Boolean Logical Operators (Non-Short-Circuit)

    // ECMA-334 §12.13.5: "bool operator &(bool x, bool y); bool operator |(bool x, bool y); bool operator ^(bool x, bool y);"
    [TestCase("true & true", true, TestName = "BoolAnd_TrueTrue")]
    [TestCase("true & false", false, TestName = "BoolAnd_TrueFalse")]
    [TestCase("false & false", false, TestName = "BoolAnd_FalseFalse")]
    [TestCase("true | false", true, TestName = "BoolOr_TrueFalse")]
    [TestCase("false | true", true, TestName = "BoolOr_FalseTrue")]
    [TestCase("false | false", false, TestName = "BoolOr_FalseFalse")]
    [TestCase("true ^ true", false, TestName = "BoolXor_TrueTrue")]
    [TestCase("true ^ false", true, TestName = "BoolXor_TrueFalse")]
    [TestCase("false ^ false", false, TestName = "BoolXor_FalseFalse")]
    public async Task Eval_BooleanBitwise(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.11 — Shift Count Masking

    // ECMA-334 §12.11: "When the type of x is int or uint, the shift count is given by
    // the low-order five bits of count (count & 0x1F). When the type of x is long or ulong,
    // the shift count is given by the low-order six bits of count (count & 0x3F)."

    // Long shift masking (& 0x3F = & 63)
    [TestCase("1L << 65", TestName = "S12_11_LongLeftShift_Mask0x3F_65")]     // 65 & 63 = 1, so 1L << 1 = 2
    [TestCase("1L << 64", TestName = "S12_11_LongLeftShift_Mask0x3F_64")]     // 64 & 63 = 0, so 1L << 0 = 1
    [TestCase("1L << 127", TestName = "S12_11_LongLeftShift_Mask0x3F_127")]   // 127 & 63 = 63
    [TestCase("256L >> 72", TestName = "S12_11_LongRightShift_Mask0x3F_72")]  // 72 & 63 = 8, so 256L >> 8 = 1

    // ULong shift masking (also & 0x3F)
    [TestCase("1UL << 65", TestName = "S12_11_ULongLeftShift_Mask0x3F_65")]
    [TestCase("1UL << 64", TestName = "S12_11_ULongLeftShift_Mask0x3F_64")]
    [TestCase("256UL >> 72", TestName = "S12_11_ULongRightShift_Mask0x3F_72")]

    // Int shift masking confirmation (& 0x1F = & 31, existing behavior but adding explicit large-count test)
    [TestCase("1 << 33", TestName = "S12_11_IntLeftShift_Mask0x1F_33")]       // 33 & 31 = 1, so 1 << 1 = 2
    [TestCase("1 << 32", TestName = "S12_11_IntLeftShift_Mask0x1F_32")]       // 32 & 31 = 0, so 1 << 0 = 1
    [TestCase("1u << 33", TestName = "S12_11_UIntLeftShift_Mask0x1F_33")]
    public async Task ShiftMasking(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.11, §12.4.2 — Shift Precedence and Edge Cases

    // ECMA-334 §12.4.2: Addition/subtraction has higher precedence than shift
    [TestCase("1 << 2 + 3", 32, TestName = "Precedence_ShiftVsAdd_AddFirst")]
    [TestCase("16 >> 1 + 1", 4, TestName = "Precedence_ShiftVsAdd_RightShift")]
    [TestCase("1 << 1 << 1", 4, TestName = "Precedence_ShiftLeftAssociativity")]
    [TestCase("1 << 2 * 2", 16, TestName = "Precedence_ShiftVsMul_MulFirst")]
    [TestCase("8 >> 4 / 2", 2, TestName = "Precedence_ShiftVsDiv_DivFirst")]
    // ECMA-334 §12.13: & -> ^ -> | precedence chain
    [TestCase("7 & 3 ^ 1", 2, TestName = "Precedence_AndBeforeXor")]
    [TestCase("7 ^ 3 | 1", 5, TestName = "Precedence_XorBeforeOr")]
    [TestCase("7 & 3 ^ 1 | 8", 10, TestName = "Precedence_FullChain_AndXorOr")]
    // ECMA-334 §12.11: Shift counts are masked to type width (5 bits for int = & 0x1F)
    [TestCase("1 << 32", 1, TestName = "LeftShift_By32_Wraps")]
    [TestCase("1 << 33", 2, TestName = "LeftShift_By33_Wraps")]
    [TestCase("16 >> -1", 0, TestName = "RightShift_NegativeCount")]
    public async Task Precedence_ShiftAndArithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.13 — Type Errors

    // ECMA-334 §12.13: Bitwise operators require integer or bool operands
    [TestCase("3.14 & 1", TestName = "And_DoubleWithInt")]
    [TestCase("3.14 | 1", TestName = "Or_DoubleWithInt")]
    [TestCase("3.14 ^ 1", TestName = "Xor_DoubleWithInt")]
    [TestCase("3.14 << 1", TestName = "LeftShift_Double")]
    [TestCase("1 << 3.14", TestName = "LeftShift_DoubleCount")]
    [TestCase("8 >> 1.5", TestName = "RightShift_DoubleCount")]
    [TestCase("~3.14", TestName = "Not_Double")]
    // ECMA-334 §12.4.2: == has higher precedence than &, so this parses as 1 & (1 == 1) = 1 & true
    [TestCase("1 & 1 == 1", TestName = "Precedence_CompareBeforeBitwiseAnd_TypeMismatch")]
    public async Task Eval_Bitwise_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #endregion
}
