using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.13 -- Logical operators (integer bitwise AND, OR, XOR, NOT),
/// S12.11 -- Shift operators, S12.13.5 -- Boolean logical operators (non-short-circuit).
/// Test data for bitwise, shift, and boolean logical operations.
/// Shared across compiler backends.
/// </summary>
public static class BitwiseData
{
    /// <summary>
    /// Value-producing integer bitwise expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> IntegerBitwiseCases() =>
    [
        // ECMA-334 S12.13.1: "The predefined integer logical operators are: int operator &amp;(int x, int y); ..."
        new("5 & 3", 1) { TestName = "And_5And3" },
        new("12 & 10", 8) { TestName = "And_12And10" },
        new("255 & 0", 0) { TestName = "And_WithZero" },
        new("15 & 15", 15) { TestName = "And_SameValue" },
        new("5 | 3", 7) { TestName = "Or_5Or3" },
        new("12 | 10", 14) { TestName = "Or_12Or10" },
        new("255 | 0", 255) { TestName = "Or_WithZero" },
        new("1 | 2 | 4", 7) { TestName = "Or_Chained" },
        new("5 ^ 3", 6) { TestName = "Xor_5Xor3" },
        new("12 ^ 10", 6) { TestName = "Xor_12Xor10" },
        new("42 ^ 42", 0) { TestName = "Xor_SameValueIsZero" },
        new("15 ^ 255", 240) { TestName = "Xor_15Xor255" },
        // ECMA-334 S12.9.5: Bitwise complement operator
        new("~0", -1) { TestName = "Not_Zero" },
        new("~1", -2) { TestName = "Not_One" },
        new("~(-1)", 0) { TestName = "Not_NegativeOne" },
        new("~~42", 42) { TestName = "Not_DoubleNot" },
    ];

    /// <summary>
    /// Value-producing shift operator expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ShiftCases() =>
    [
        // ECMA-334 S12.11: "The &lt;&lt; and >> operators are used to perform bit-shifting operations."
        new("1 << 1", 2) { TestName = "LeftShift_By1" },
        new("1 << 2", 4) { TestName = "LeftShift_By2" },
        new("1 << 3", 8) { TestName = "LeftShift_By3" },
        new("5 << 2", 20) { TestName = "LeftShift_5By2" },
        new("3 << 4", 48) { TestName = "LeftShift_3By4" },
        new("42 << 0", 42) { TestName = "LeftShift_ByZero" },
        new("0 << 10", 0) { TestName = "LeftShift_ZeroValue" },
        new("8 >> 1", 4) { TestName = "RightShift_By1" },
        new("8 >> 2", 2) { TestName = "RightShift_By2" },
        new("8 >> 3", 1) { TestName = "RightShift_By3" },
        new("20 >> 2", 5) { TestName = "RightShift_20By2" },
        new("48 >> 4", 3) { TestName = "RightShift_48By4" },
        new("42 >> 0", 42) { TestName = "RightShift_ByZero" },
    ];

    /// <summary>
    /// Value-producing bitwise precedence expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> PrecedenceCases() =>
    [
        // ECMA-334 S12.4.2: &amp; binds tighter than ^, ^ tighter than |
        new("1 | 2 & 3", 3) { TestName = "Precedence_AndBeforeOr" },
        new("1 << 2 < 5", true) { TestName = "Precedence_ShiftBeforeComparison" },
        new("7 ^ 3 & 5", 6) { TestName = "Precedence_AndBeforeXor" },
        new("1 | 2 ^ 3", 1) { TestName = "Precedence_XorBeforeOr" },
        new("(2 + 3) & 7", 5) { TestName = "Combined_ArithmeticAndBitwise" },
        new("10 | (3 * 2)", 14) { TestName = "Combined_BitwiseAndMultiply" },
        new("(5 & 1) == 1 && true", true) { TestName = "Combined_BitwiseAndLogical" },
        new("true ? 5 & 3 : 0", 1) { TestName = "InTernary" },
    ];

    /// <summary>
    /// Value-producing boolean bitwise expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BooleanBitwiseCases() =>
    [
        // ECMA-334 S12.13.5: "bool operator &amp;(bool x, bool y); bool operator |(bool x, bool y); bool operator ^(bool x, bool y);"
        new("true & true", true) { TestName = "BoolAnd_TrueTrue" },
        new("true & false", false) { TestName = "BoolAnd_TrueFalse" },
        new("false & false", false) { TestName = "BoolAnd_FalseFalse" },
        new("true | false", true) { TestName = "BoolOr_TrueFalse" },
        new("false | true", true) { TestName = "BoolOr_FalseTrue" },
        new("false | false", false) { TestName = "BoolOr_FalseFalse" },
        new("true ^ true", false) { TestName = "BoolXor_TrueTrue" },
        new("true ^ false", true) { TestName = "BoolXor_TrueFalse" },
        new("false ^ false", false) { TestName = "BoolXor_FalseFalse" },
    ];

    /// <summary>
    /// Parity-only shift masking expressions (no expected value -- Roslyn determines correctness).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ShiftMaskingParityCases() =>
    [
        // ECMA-334 S12.11: "When the type of x is int or uint, the shift count is given by
        // the low-order five bits of count (count &amp; 0x1F). When the type of x is long or ulong,
        // the shift count is given by the low-order six bits of count (count &amp; 0x3F)."

        // Long shift masking (&amp; 0x3F = &amp; 63)
        new("1L << 65") { TestName = "S12_11_LongLeftShift_Mask0x3F_65" },     // 65 & 63 = 1, so 1L << 1 = 2
        new("1L << 64") { TestName = "S12_11_LongLeftShift_Mask0x3F_64" },     // 64 & 63 = 0, so 1L << 0 = 1
        new("1L << 127") { TestName = "S12_11_LongLeftShift_Mask0x3F_127" },   // 127 & 63 = 63
        new("256L >> 72") { TestName = "S12_11_LongRightShift_Mask0x3F_72" },  // 72 & 63 = 8, so 256L >> 8 = 1

        // ULong shift masking (also &amp; 0x3F)
        new("1UL << 65") { TestName = "S12_11_ULongLeftShift_Mask0x3F_65" },
        new("1UL << 64") { TestName = "S12_11_ULongLeftShift_Mask0x3F_64" },
        new("256UL >> 72") { TestName = "S12_11_ULongRightShift_Mask0x3F_72" },

        // Int shift masking confirmation (&amp; 0x1F = &amp; 31, existing behavior but adding explicit large-count test)
        new("1 << 33") { TestName = "S12_11_IntLeftShift_Mask0x1F_33" },       // 33 & 31 = 1, so 1 << 1 = 2
        new("1 << 32") { TestName = "S12_11_IntLeftShift_Mask0x1F_32" },       // 32 & 31 = 0, so 1 << 0 = 1
        new("1u << 33") { TestName = "S12_11_UIntLeftShift_Mask0x1F_33" },
    ];

    /// <summary>
    /// Value-producing shift precedence and edge case expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ShiftPrecedenceCases() =>
    [
        // ECMA-334 S12.4.2: Addition/subtraction has higher precedence than shift
        new("1 << 2 + 3", 32) { TestName = "Precedence_ShiftVsAdd_AddFirst" },
        new("16 >> 1 + 1", 4) { TestName = "Precedence_ShiftVsAdd_RightShift" },
        new("1 << 1 << 1", 4) { TestName = "Precedence_ShiftLeftAssociativity" },
        new("1 << 2 * 2", 16) { TestName = "Precedence_ShiftVsMul_MulFirst" },
        new("8 >> 4 / 2", 2) { TestName = "Precedence_ShiftVsDiv_DivFirst" },
        // ECMA-334 S12.13: &amp; -> ^ -> | precedence chain
        new("7 & 3 ^ 1", 2) { TestName = "Precedence_AndBeforeXor" },
        new("7 ^ 3 | 1", 5) { TestName = "Precedence_XorBeforeOr" },
        new("7 & 3 ^ 1 | 8", 10) { TestName = "Precedence_FullChain_AndXorOr" },
        // ECMA-334 S12.11: Shift counts are masked to type width (5 bits for int = &amp; 0x1F)
        new("1 << 32", 1) { TestName = "LeftShift_By32_Wraps" },
        new("1 << 33", 2) { TestName = "LeftShift_By33_Wraps" },
        new("16 >> -1", 0) { TestName = "RightShift_NegativeCount" },
    ];

    /// <summary>
    /// Bitwise expressions that should throw exceptions (type errors).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        // ECMA-334 S12.13: Bitwise operators require integer or bool operands
        new("3.14 & 1") { TestName = "And_DoubleWithInt" },
        new("3.14 | 1") { TestName = "Or_DoubleWithInt" },
        new("3.14 ^ 1") { TestName = "Xor_DoubleWithInt" },
        new("3.14 << 1") { TestName = "LeftShift_Double" },
        new("1 << 3.14") { TestName = "LeftShift_DoubleCount" },
        new("8 >> 1.5") { TestName = "RightShift_DoubleCount" },
        new("~3.14") { TestName = "Not_Double" },
        // ECMA-334 S12.4.2: == has higher precedence than &amp;, so this parses as 1 &amp; (1 == 1) = 1 &amp; true
        new("1 & 1 == 1") { TestName = "Precedence_CompareBeforeBitwiseAnd_TypeMismatch" },
    ];
}
