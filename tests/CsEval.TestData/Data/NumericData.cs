using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §6.4.5.2 -- Integer literals, §6.4.5.3 -- Real literals,
/// §12.9 -- Arithmetic operators, §12.4.7.3 -- Binary numeric promotion,
/// §12.12 -- Relational operators, §12.13 -- Bitwise operators.
/// Comprehensive numeric test data covering literal parsing, arithmetic with type promotion,
/// comparisons, precision semantics, LINQ aggregation, and block expression evaluation.
/// Shared across compiler backends.
/// </summary>
public static class NumericData
{
    /// <summary>
    /// ECMA-334 §6.4.5, §12.9, §12.12, §12.13 -- Numeric expression parity cases.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ParityCases() =>
    [
        // Literals
        new("42") { TestName = "Literal_Int" },
        new("0") { TestName = "Literal_Zero" },
        new("-42") { TestName = "Literal_NegativeInt" },
        new("9223372036854775807") { TestName = "Literal_LongMax" },
        new("42L") { TestName = "Literal_LongSuffix" },
        new("3.14f") { TestName = "Literal_Float" },
        new("3.14m") { TestName = "Literal_Decimal" },
        new("42m") { TestName = "Literal_IntAsDecimal" },
        new("3.14") { TestName = "Literal_Double" },
        new("0.5") { TestName = "Literal_DoubleLeadingZero" },
        new("-3.14") { TestName = "Literal_NegativeDouble" },
        new("0.00001") { TestName = "Literal_SmallDouble" },
        // Arithmetic - same types
        new("5 + 3") { TestName = "Arithmetic_IntPlusInt" },
        new("10 - 4") { TestName = "Arithmetic_IntMinusInt" },
        new("6 * 7") { TestName = "Arithmetic_IntTimesInt" },
        new("10 / 4") { TestName = "Arithmetic_IntDivInt" },
        new("5L + 3L") { TestName = "Arithmetic_LongPlusLong" },
        new("5 + 3L") { TestName = "Arithmetic_IntPlusLong" },
        new("1.5 + 2.5") { TestName = "Arithmetic_DoublePlusDouble" },
        new("2.5 * 4.0") { TestName = "Arithmetic_DoubleTimesDouble" },
        // Arithmetic - mixed types
        new("5 + 2.5") { TestName = "Arithmetic_IntPlusDouble" },
        new("2.5 + 5") { TestName = "Arithmetic_DoublePlusInt" },
        // Comparisons
        new("3.14 == 3.14") { TestName = "Compare_DoubleEquals" },
        new("5 < 5.5") { TestName = "Compare_IntLessThanDouble" },
        new("5.5 > 5") { TestName = "Compare_DoubleGreaterThanInt" },
        new("10 >= 10") { TestName = "Compare_GreaterOrEqual" },
        new("10 <= 10") { TestName = "Compare_LessOrEqual" },
        new("5 != 6") { TestName = "Compare_NotEqual" },
        // Modulo
        new("10 % 3") { TestName = "Modulo_IntModInt" },
        new("10.5 % 3.0") { TestName = "Modulo_DoubleModDouble" },
        // Division precision
        new("7 / 3") { TestName = "Division_IntTruncates" },
        new("7.0 / 3.0") { TestName = "Division_DoublePrecision" },
        // Floating-point precision
        new("0.1 + 0.2") { TestName = "Precision_PointOnePointTwo" },
        // Negation
        new("-42") { TestName = "Negate_Int" },
        new("-3.14") { TestName = "Negate_Double" },
        new("-3.14m") { TestName = "Negate_Decimal" },
        // Bitwise
        new("15 & 9") { TestName = "Bitwise_And" },
        new("5 | 3") { TestName = "Bitwise_Or" },
        new("12 ^ 5") { TestName = "Bitwise_Xor" },
        new("1 << 4") { TestName = "Bitwise_LeftShift" },
        new("32 >> 2") { TestName = "Bitwise_RightShift" },
        // LINQ
        new("new[] { 1, 2, 3 }.Select(x => x * 2).ToList()") { TestName = "Linq_Select" },
        new("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 2).ToList()") { TestName = "Linq_Where" },
        new("new[] { 1, 2, 3, 4, 5 }.Sum()") { TestName = "Linq_Sum_IntArray" },
        new("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 2).Select(x => x * 10).Sum()") { TestName = "Linq_WhereSelectSum" },
        new("new[] { 1, 2, 3, 4, 5 }.Average()") { TestName = "Linq_Average_IntArray" },
        new("new[] { 1.5m, 2.5m, 3.5m }.Average()") { TestName = "Linq_Average_DecimalArray" },
        new("new[] { 1L, 2L, 3L, 4L, 5L }.Average()") { TestName = "Linq_Average_LongArray" },
        new("Enumerable.Range(1, 5).ToList()") { TestName = "Linq_Range_ToList" },
        new("Enumerable.Range(1, 5).ToArray()") { TestName = "Linq_Range_ToArray" },
        new("Enumerable.Range(1, 5).Where(x => x > 2).ToList()") { TestName = "Linq_Range_Where" },
        new("Enumerable.Range(1, 5).Select(x => x * 2).ToList()") { TestName = "Linq_Range_Select" },
        new("Enumerable.Range(1, 5).Take(3).ToList()") { TestName = "Linq_Range_Take" },
        new("Enumerable.Range(1, 5).Skip(2).ToList()") { TestName = "Linq_Range_Skip" },
        new("Enumerable.Range(1, 5).OrderByDescending(x => x).ToList()") { TestName = "Linq_Range_OrderByDescending" },
        new("Enumerable.Range(1, 5).Reverse().ToList()") { TestName = "Linq_Range_Reverse" },
        new("Enumerable.Range(1, 5).Distinct().ToList()") { TestName = "Linq_Range_Distinct" },
        new("Enumerable.Repeat(42, 3).ToList()") { TestName = "Linq_Repeat_Int" },
        new("Enumerable.Repeat(\"x\", 3).ToList()") { TestName = "Linq_Repeat_String" },
        new("new[] { 1, 2, 3 }.Where(x => x > 1).ToList()") { TestName = "Linq_Array_Where" },
        new("new[] { 1, 2, 3 }.Select(x => x.ToString()).ToList()") { TestName = "Linq_Array_SelectToString" },
        new("new[] { 1, 2, 3 }.Sum()") { TestName = "Linq_Array_Sum" },
        new("new[] { 1.5, 2.5, 3.5 }.Sum()") { TestName = "Linq_Sum_DoubleArray" },
        new("new[] { 1m, 2m, 3m }.Sum()") { TestName = "Linq_Sum_DecimalArray" },
        new("new[] { 1L, 2L, 3L }.Sum()") { TestName = "Linq_Sum_LongArray" },
        new("Enumerable.Range(1, 5).Sum()") { TestName = "Linq_Range_Sum" },
        new("Enumerable.Range(1, 5).Count()") { TestName = "Linq_Range_Count" },
        new("Enumerable.Range(1, 5).Min()") { TestName = "Linq_Range_Min" },
        new("Enumerable.Range(1, 5).Max()") { TestName = "Linq_Range_Max" },
        new("Enumerable.Range(1, 5).Average()") { TestName = "Linq_Range_Average" },
        new("Enumerable.Range(1, 5).First()") { TestName = "Linq_Range_First" },
        new("Enumerable.Range(1, 5).Last()") { TestName = "Linq_Range_Last" },
        new("Enumerable.Range(1, 5).Any()") { TestName = "Linq_Range_Any" },
        new("Enumerable.Range(1, 5).Any(x => x > 3)") { TestName = "Linq_Range_AnyPredicate" },
        new("Enumerable.Range(1, 5).All(x => x > 0)") { TestName = "Linq_Range_All" },
        new("Enumerable.Range(1, 5).Contains(3)") { TestName = "Linq_Range_Contains" },
        new("Enumerable.Repeat(5, 4).Sum()") { TestName = "Linq_Repeat_Sum" },
        new("Enumerable.Range(1, 3).Select(x => x * 1.5).Sum()") { TestName = "Linq_Range_SelectDoubleSum" },
    ];

    /// <summary>
    /// ECMA-334 -- Numeric boundary values for signed types.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BoundarySignedCases() =>
    [
        new("(sbyte)-128", (sbyte)-128) { TestName = "Boundary_SByte_MinValue" },
        new("(sbyte)127", (sbyte)127) { TestName = "Boundary_SByte_MaxValue" },
        new("(short)-32768", (short)-32768) { TestName = "Boundary_Short_MinValue" },
        new("(short)32767", (short)32767) { TestName = "Boundary_Short_MaxValue" },
        new("-2147483648", -2147483648) { TestName = "Boundary_Int_MinValue" },
        new("2147483647", 2147483647) { TestName = "Boundary_Int_MaxValue" },
        new("-9223372036854775808L", -9223372036854775808L) { TestName = "Boundary_Long_MinValue" },
        new("9223372036854775807L", 9223372036854775807L) { TestName = "Boundary_Long_MaxValue" },
    ];

    /// <summary>
    /// ECMA-334 -- Numeric boundary values for unsigned types.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BoundaryUnsignedCases() =>
    [
        new("(byte)0", (byte)0) { TestName = "Boundary_Byte_MinValue" },
        new("(byte)255", (byte)255) { TestName = "Boundary_Byte_MaxValue" },
        new("(ushort)0", (ushort)0) { TestName = "Boundary_UShort_MinValue" },
        new("(ushort)65535", (ushort)65535) { TestName = "Boundary_UShort_MaxValue" },
        new("0u", 0u) { TestName = "Boundary_UInt_MinValue" },
        new("4294967295u", 4294967295u) { TestName = "Boundary_UInt_MaxValue" },
        new("0UL", 0UL) { TestName = "Boundary_ULong_MinValue" },
        new("18446744073709551615UL", 18446744073709551615UL) { TestName = "Boundary_ULong_MaxValue" },
    ];

    /// <summary>
    /// Large hex literals.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> HexLiteralBoundaryCases() =>
    [
        new("0xFFFFFFFF", 0xFFFFFFFF) { TestName = "HexLiteral_MaxUInt" },
        new("0xFFFF_FFFF_FFFF_FFFF", 0xFFFF_FFFF_FFFF_FFFF) { TestName = "HexLiteral_MaxULong" },
        new("0x7FFFFFFF", 0x7FFFFFFF) { TestName = "HexLiteral_MaxInt" },
        new("0x7FFF_FFFF_FFFF_FFFF", 0x7FFF_FFFF_FFFF_FFFF) { TestName = "HexLiteral_MaxLong" },
    ];

    /// <summary>
    /// Binary literals at boundaries.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BinaryLiteralBoundaryCases() =>
    [
        new("0b11111111", 255) { TestName = "BinaryLiteral_Byte_Max" },
        new("0b1111_1111_1111_1111", 65535) { TestName = "BinaryLiteral_UShort_Max" },
    ];

    /// <summary>
    /// Char to int boundary conversions.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CharToIntBoundaryCases() =>
    [
        new("(int)'\\0'", 0) { TestName = "CharToInt_NullChar" },
        new("(int)'\\x00'", 0) { TestName = "CharToInt_HexNull" },
        new("(int)'\\uFFFF'", 65535) { TestName = "CharToInt_MaxUnicode" },
    ];
}
