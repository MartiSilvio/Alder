using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S10.3 -- Explicit conversions (cast expressions),
/// S10.3.7 -- Unboxing conversions, S10.3.2 -- Explicit numeric conversions.
/// Test data for explicit casts, nullable casts, unboxing rules, and conversion edge cases.
/// Shared across compiler backends.
/// </summary>
public static class CastData
{
    /// <summary>
    /// Value-producing cast expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S10.3.2 -- Explicit Numeric Conversions
        new("(int)3.7", 3) { TestName = "Cast_DoubleToInt" },
        new("(int)3.9", 3) { TestName = "Cast_DoubleToInt_Truncates" },
        new("(double)5", 5.0) { TestName = "Cast_IntToDouble" },
        new("(long)42", 42L) { TestName = "Cast_IntToLong" },
        new("(int)100L", 100) { TestName = "Cast_LongToInt" },
        new("(float)3.14", 3.14f) { TestName = "Cast_DoubleToFloat" },
        new("(double)3.14f", (double)3.14f) { TestName = "Cast_FloatToDouble" },
        new("(byte)255", (byte)255) { TestName = "Cast_IntToByte" },
        new("(sbyte)127", (sbyte)127) { TestName = "Cast_IntToSbyte" },
        new("(short)1000", (short)1000) { TestName = "Cast_IntToShort" },
        new("(ushort)50000", (ushort)50000) { TestName = "Cast_IntToUshort" },
        new("(uint)100", 100u) { TestName = "Cast_IntToUint" },
        new("(ulong)100", 100ul) { TestName = "Cast_IntToUlong" },
        new("(int)'A'", 65) { TestName = "Cast_CharToInt" },
        new("(char)65", 'A') { TestName = "Cast_IntToChar" },
        new("(char)90", 'Z') { TestName = "Cast_IntToChar_Z" },
        new("(int)'9'", 57) { TestName = "Cast_DigitCharToInt" },
        new("(int)(2.5 + 3.7)", 6) { TestName = "Cast_ExpressionResult" },
        new("(int)(-3.9)", -3) { TestName = "Cast_NegativeDouble" },
        new("(double)(10 / 3)", 3.0) { TestName = "Cast_IntDivisionResult" },
        new("(int)(double)42L", 42) { TestName = "Cast_ChainedCasts" },
        new("(int)3.7 + (int)4.2", 7) { TestName = "Cast_InArithmetic" },
        new("(int)3.7m", 3) { TestName = "Cast_DecimalToInt" },
        new("(int?)42", 42) { TestName = "Cast_IntToNullableInt" },
        new("(decimal)42", 42) { TestName = "Cast_IntToDecimal" },
        new("{ var x = 3.7; return (int)x; }", 3) { TestName = "Cast_Variable" },
        new("{ var x = 3.7; return (int)x > 3 ? \"yes\" : \"no\"; }", "no") { TestName = "Cast_InConditional" },
    ];

    /// <summary>
    /// Cast expressions producing null (nullable casts).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NullCastCases() =>
    [
        // ECMA-334 S10.6.1 -- Nullable Cast Conversions
        new("(int?)null", null) { TestName = "Cast_NullToNullableInt" },
        new("(long?)null", null) { TestName = "Cast_NullToNullableLong" },
        new("(double?)null", null) { TestName = "Cast_NullToNullableDouble" },
    ];

    /// <summary>
    /// Valid unboxing cast expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValidUnboxingCases() =>
    [
        // ECMA-334 S10.3.7 -- Unboxing Requires Exact Type Match
        new("{ object x = 42; return (int)x; }", 42) { TestName = "Unboxing_IntToInt" },
        new("{ object x = 42L; return (long)x; }", 42L) { TestName = "Unboxing_LongToLong" },
        new("{ object x = 3.14; return (double)x; }", 3.14) { TestName = "Unboxing_DoubleToDouble" },
        new("{ object x = true; return (bool)x; }", true) { TestName = "Unboxing_BoolToBool" },
        new("{ object x = 'A'; return (char)x; }", 'A') { TestName = "Unboxing_CharToChar" },
    ];

    /// <summary>
    /// Numeric conversion (non-unboxing) cast expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NumericConversionCases() =>
    [
        // When static type is known (not object), conversions use numeric conversion, not unboxing
        new("{ int x = 42; return (long)x; }", 42L) { TestName = "Conversion_IntToLong" },
        new("{ int x = 42; return (double)x; }", 42.0) { TestName = "Conversion_IntToDouble" },
        new("{ long x = 42L; return (int)x; }", 42) { TestName = "Conversion_LongToInt" },
        new("{ double x = 3.7; return (int)x; }", 3) { TestName = "Conversion_DoubleToInt" },
    ];

    /// <summary>
    /// Char explicit conversion expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CharConversionCases() =>
    [
        // ECMA-334 S10.2.3: char -> int, long, double are implicit conversions
        new("(int)'A'", 65) { TestName = "CharToInt_Conversion" },
        new("(long)'A'", 65L) { TestName = "CharToLong_Conversion" },
        new("(double)'A'", 65.0) { TestName = "CharToDouble_Conversion" },
    ];

    /// <summary>
    /// Float-to-integer truncation expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> FloatTruncationCases() =>
    [
        // ECMA-334 S10.3.2: Float-to-integer truncates toward zero
        new("(int)3.9", 3) { TestName = "DoubleToInt_Truncates" },
        new("(int)-3.9", -3) { TestName = "NegativeDoubleToInt_Truncates" },
        new("(long)3.9", 3L) { TestName = "DoubleToLong_Truncates" },
        new("(int)3.5f", 3) { TestName = "FloatToInt_Truncates" },
    ];

    /// <summary>
    /// Narrowing overflow expressions with expected results (unchecked context).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NarrowingOverflowCases() =>
    [
        // ECMA-334 S10.3.2: In unchecked context, narrowing wraps
        new("unchecked((byte)256)", (byte)0) { TestName = "IntToByte_Overflow" },
        new("unchecked((sbyte)128)", (sbyte)-128) { TestName = "IntToSbyte_Overflow" },
        new("unchecked((short)32768)", (short)-32768) { TestName = "IntToShort_Overflow" },
        new("unchecked((byte)-1)", (byte)255) { TestName = "NegativeIntToByte_Wraps" },
        new("unchecked((ushort)-1)", (ushort)65535) { TestName = "NegativeIntToUShort_Wraps" },
        new("unchecked((uint)-1)", 4294967295u) { TestName = "NegativeIntToUInt_Wraps" },
        new("(int)0.5", 0) { TestName = "FloatToInt_RoundsTowardZero_Positive" },
        new("(int)-0.5", 0) { TestName = "FloatToInt_RoundsTowardZero_Negative" },
        new("(int)0.999999", 0) { TestName = "FloatToInt_TruncatesHigh" },
    ];

    /// <summary>
    /// Cast expressions that should throw (invalid casts).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> InvalidCastErrorCases() =>
    [
        // ECMA-334 S10.3.7 -- Invalid Cast Errors
        new("{ object x = null; return (int)x; }") { TestName = "CastNullObjectToInt" },
        new("{ object x = \"hello\"; return (int)x; }") { TestName = "CastStringObjectToInt" },
    ];

    /// <summary>
    /// Cast expressions that should throw InvalidCastException (invalid unboxing).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> InvalidUnboxingErrorCases() =>
    [
        // ECMA-334 S10.3.7: "An unboxing conversion ... consists of first checking that the object
        // instance is a boxed value of the given value_type, and then copying the value out of the instance."
        // (long)(object)42 fails because 42 is boxed as int, not long.
        new("{ object x = 42; return (long)x; }") { TestName = "Unboxing_IntToLong" },
        new("{ object x = 42; return (double)x; }") { TestName = "Unboxing_IntToDouble" },
        new("{ object x = true; return (int)x; }") { TestName = "Unboxing_BoolToInt" },
        new("{ object x = 3.14; return (int)x; }") { TestName = "Unboxing_DoubleToInt" },
        new("{ object x = 42L; return (int)x; }") { TestName = "Unboxing_LongToInt" },
        new("{ object x = 3.14f; return (double)x; }") { TestName = "Unboxing_FloatToDouble" },
    ];
}
