using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §10.2 and §10.3 -- Implicit and Explicit conversion test data.
/// Validates that CsEval's conversion behavior matches Roslyn and the ECMA-334 7th edition spec exactly.
/// Shared across compiler backends.
/// </summary>
public static class ConversionData
{
    /// <summary>
    /// ECMA-334 §10.2.3 -- Implicit numeric conversions.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ImplicitNumericCases() =>
    [
        // ECMA-334 §10.2.3: "From sbyte to short, int, long, float, double, or decimal."
        new("{ sbyte x = 5; short y = x; return y; }", (short)5) { TestName = "Implicit_SByte_To_Short" },
        new("{ sbyte x = 5; int y = x; return y; }", 5) { TestName = "Implicit_SByte_To_Int" },
        new("{ sbyte x = 5; long y = x; return y; }", 5L) { TestName = "Implicit_SByte_To_Long" },
        new("{ sbyte x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_SByte_To_Float" },
        new("{ sbyte x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_SByte_To_Double" },
        new("{ sbyte x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_SByte_To_Decimal" },
        // ECMA-334 §10.2.3: "From byte to short, ushort, int, uint, long, ulong, float, double, or decimal."
        new("{ byte x = 5; short y = x; return y; }", (short)5) { TestName = "Implicit_Byte_To_Short" },
        new("{ byte x = 5; ushort y = x; return y; }", (ushort)5) { TestName = "Implicit_Byte_To_UShort" },
        new("{ byte x = 5; int y = x; return y; }", 5) { TestName = "Implicit_Byte_To_Int" },
        new("{ byte x = 5; uint y = x; return y; }", 5u) { TestName = "Implicit_Byte_To_UInt" },
        new("{ byte x = 5; long y = x; return y; }", 5L) { TestName = "Implicit_Byte_To_Long" },
        new("{ byte x = 5; ulong y = x; return y; }", 5ul) { TestName = "Implicit_Byte_To_ULong" },
        new("{ byte x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_Byte_To_Float" },
        new("{ byte x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_Byte_To_Double" },
        new("{ byte x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_Byte_To_Decimal" },
        // ECMA-334 §10.2.3: "From short to int, long, float, double, or decimal."
        new("{ short x = 5; int y = x; return y; }", 5) { TestName = "Implicit_Short_To_Int" },
        new("{ short x = 5; long y = x; return y; }", 5L) { TestName = "Implicit_Short_To_Long" },
        new("{ short x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_Short_To_Float" },
        new("{ short x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_Short_To_Double" },
        new("{ short x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_Short_To_Decimal" },
        // ECMA-334 §10.2.3: "From ushort to int, uint, long, ulong, float, double, or decimal."
        new("{ ushort x = 5; int y = x; return y; }", 5) { TestName = "Implicit_UShort_To_Int" },
        new("{ ushort x = 5; uint y = x; return y; }", 5u) { TestName = "Implicit_UShort_To_UInt" },
        new("{ ushort x = 5; long y = x; return y; }", 5L) { TestName = "Implicit_UShort_To_Long" },
        new("{ ushort x = 5; ulong y = x; return y; }", 5ul) { TestName = "Implicit_UShort_To_ULong" },
        new("{ ushort x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_UShort_To_Float" },
        new("{ ushort x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_UShort_To_Double" },
        new("{ ushort x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_UShort_To_Decimal" },
        // ECMA-334 §10.2.3: "From int to long, float, double, or decimal."
        new("{ int x = 5; long y = x; return y; }", 5L) { TestName = "Implicit_Int_To_Long" },
        new("{ int x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_Int_To_Float" },
        new("{ int x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_Int_To_Double" },
        new("{ int x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_Int_To_Decimal" },
        // ECMA-334 §10.2.3: "From uint to long, ulong, float, double, or decimal."
        new("{ uint x = 5; long y = x; return y; }", 5L) { TestName = "Implicit_UInt_To_Long" },
        new("{ uint x = 5; ulong y = x; return y; }", 5ul) { TestName = "Implicit_UInt_To_ULong" },
        new("{ uint x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_UInt_To_Float" },
        new("{ uint x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_UInt_To_Double" },
        new("{ uint x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_UInt_To_Decimal" },
        // ECMA-334 §10.2.3: "From long to float, double, or decimal."
        new("{ long x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_Long_To_Float" },
        new("{ long x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_Long_To_Double" },
        new("{ long x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_Long_To_Decimal" },
        // ECMA-334 §10.2.3: "From ulong to float, double, or decimal."
        new("{ ulong x = 5; float y = x; return y; }", 5.0f) { TestName = "Implicit_ULong_To_Float" },
        new("{ ulong x = 5; double y = x; return y; }", 5.0d) { TestName = "Implicit_ULong_To_Double" },
        new("{ ulong x = 5; decimal y = x; return y; }", 5) { TestName = "Implicit_ULong_To_Decimal" },
        // ECMA-334 §10.2.3: "From char to ushort, int, uint, long, ulong, float, double, or decimal."
        new("{ char x = 'A'; ushort y = x; return y; }", (ushort)65) { TestName = "Implicit_Char_To_UShort" },
        new("{ char x = 'A'; int y = x; return y; }", 65) { TestName = "Implicit_Char_To_Int" },
        new("{ char x = 'A'; uint y = x; return y; }", 65u) { TestName = "Implicit_Char_To_UInt" },
        new("{ char x = 'A'; long y = x; return y; }", 65L) { TestName = "Implicit_Char_To_Long" },
        new("{ char x = 'A'; ulong y = x; return y; }", 65ul) { TestName = "Implicit_Char_To_ULong" },
        new("{ char x = 'A'; float y = x; return y; }", 65.0f) { TestName = "Implicit_Char_To_Float" },
        new("{ char x = 'A'; double y = x; return y; }", 65.0d) { TestName = "Implicit_Char_To_Double" },
        new("{ char x = 'A'; decimal y = x; return y; }", 65) { TestName = "Implicit_Char_To_Decimal" },
        // ECMA-334 §10.2.3: "From float to double."
        new("{ float x = 3.14f; double y = x; return y; }", (double)3.14f) { TestName = "Implicit_Float_To_Double" },
    ];

    /// <summary>
    /// ECMA-334 §10.2.3 -- Guard: No implicit conversion TO char.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> GuardNoImplicitToCharCases() =>
    [
        // ECMA-334 §10.2.3: "There are no predefined implicit conversions to the char type."
        new("{ byte x = 65; char c = x; return c; }") { TestName = "Guard_NoImplicit_Byte_To_Char" },
        new("{ ushort x = 65; char c = x; return c; }") { TestName = "Guard_NoImplicit_UShort_To_Char" },
        new("{ sbyte x = 65; char c = x; return c; }") { TestName = "Guard_NoImplicit_SByte_To_Char" },
        new("{ short x = 65; char c = x; return c; }") { TestName = "Guard_NoImplicit_Short_To_Char" },
        new("{ int x = 65; char c = x; return c; }") { TestName = "Guard_NoImplicit_Int_To_Char" },
        new("{ long x = 65; char c = x; return c; }") { TestName = "Guard_NoImplicit_Long_To_Char" },
    ];

    /// <summary>
    /// ECMA-334 §10.3.2 -- Explicit numeric conversions (cast).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExplicitNumericCases() =>
    [
        new("(byte)255", (byte)255) { TestName = "Explicit_Int_To_Byte" },
        new("(sbyte)127", (sbyte)127) { TestName = "Explicit_Int_To_SByte" },
        new("(short)1000", (short)1000) { TestName = "Explicit_Int_To_Short" },
        new("(ushort)50000", (ushort)50000) { TestName = "Explicit_Int_To_UShort" },
        new("(char)65", 'A') { TestName = "Explicit_Int_To_Char" },
        new("(char)90", 'Z') { TestName = "Explicit_Int_To_Char_Z" },
        new("(int)'A'", 65) { TestName = "Explicit_Char_To_Int" },
        new("(byte)'A'", (byte)65) { TestName = "Explicit_Char_To_Byte" },
        new("(int)3.7", 3) { TestName = "Explicit_Double_To_Int_Truncates" },
        new("(int)-3.9", -3) { TestName = "Explicit_NegativeDouble_To_Int_Truncates" },
        new("(long)3.9", 3L) { TestName = "Explicit_Double_To_Long_Truncates" },
        new("(int)3.5f", 3) { TestName = "Explicit_Float_To_Int_Truncates" },
        new("(float)3.14", 3.14f) { TestName = "Explicit_Double_To_Float" },
        new("(double)3.14f", (double)3.14f) { TestName = "Explicit_Float_To_Double" },
        new("(int)3.7m", 3) { TestName = "Explicit_Decimal_To_Int" },
        new("(double)42", 42.0) { TestName = "Explicit_Int_To_Double" },
        new("(long)42", 42L) { TestName = "Explicit_Int_To_Long" },
        new("(int)100L", 100) { TestName = "Explicit_Long_To_Int" },
        new("(uint)100", 100u) { TestName = "Explicit_Int_To_UInt" },
        new("(ulong)100", 100ul) { TestName = "Explicit_Int_To_ULong" },
        // Cross-type explicit casts
        new("(byte)(short)200", (byte)200) { TestName = "Explicit_Short_To_Byte" },
        new("(sbyte)(ushort)100", (sbyte)100) { TestName = "Explicit_UShort_To_SByte" },
        new("(char)(byte)65", 'A') { TestName = "Explicit_Byte_To_Char" },
        new("(char)(ushort)65", 'A') { TestName = "Explicit_UShort_To_Char" },
    ];

    /// <summary>
    /// ECMA-334 §10.2.6 / §10.6.1 -- Implicit nullable conversions.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ImplicitNullableCases() =>
    [
        // T -> T? (identity lift)
        new("{ int x = 42; int? y = x; return y; }", 42) { TestName = "Implicit_Int_To_NullableInt" },
        new("{ long x = 42; long? y = x; return y; }", 42L) { TestName = "Implicit_Long_To_NullableLong" },
        new("{ double x = 3.14; double? y = x; return y; }", 3.14) { TestName = "Implicit_Double_To_NullableDouble" },
        new("{ bool x = true; bool? y = x; return y; }", true) { TestName = "Implicit_Bool_To_NullableBool" },
        new("{ char x = 'A'; char? y = x; return y; }", 'A') { TestName = "Implicit_Char_To_NullableChar" },
        new("{ byte x = 5; byte? y = x; return y; }", (byte)5) { TestName = "Implicit_Byte_To_NullableByte" },
        // ECMA-334 §10.6.1: S -> T? where S -> T is an implicit numeric conversion
        new("{ int x = 42; long? y = x; return y; }", 42L) { TestName = "Implicit_Int_To_NullableLong" },
        new("{ int x = 42; double? y = x; return y; }", 42.0) { TestName = "Implicit_Int_To_NullableDouble" },
        new("{ byte x = 5; int? y = x; return y; }", 5) { TestName = "Implicit_Byte_To_NullableInt" },
        new("{ float x = 3.14f; double? y = x; return y; }", (double)3.14f) { TestName = "Implicit_Float_To_NullableDouble" },
        new("{ char x = 'A'; int? y = x; return y; }", 65) { TestName = "Implicit_Char_To_NullableInt" },
        // Null assignment to nullable
        new("{ int? x = null; return x; }", null) { TestName = "Implicit_Null_To_NullableInt" },
        new("{ double? x = null; return x; }", null) { TestName = "Implicit_Null_To_NullableDouble" },
        new("{ bool? x = null; return x; }", null) { TestName = "Implicit_Null_To_NullableBool" },
    ];

    /// <summary>
    /// ECMA-334 §10.2.11 -- Implicit constant expression conversions.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ImplicitConstantExprCases() =>
    [
        new("{ byte x = 255; return x; }", (byte)255) { TestName = "ConstExpr_Int_To_Byte_InRange" },
        new("{ byte x = 0; return x; }", (byte)0) { TestName = "ConstExpr_Int_To_Byte_Zero" },
        new("{ sbyte x = 127; return x; }", (sbyte)127) { TestName = "ConstExpr_Int_To_SByte_MaxRange" },
        new("{ sbyte x = -128; return x; }", (sbyte)-128) { TestName = "ConstExpr_Int_To_SByte_MinRange" },
        new("{ short x = 1000; return x; }", (short)1000) { TestName = "ConstExpr_Int_To_Short_InRange" },
        new("{ ushort x = 50000; return x; }", (ushort)50000) { TestName = "ConstExpr_Int_To_UShort_InRange" },
        new("{ uint x = 42; return x; }", 42u) { TestName = "ConstExpr_Int_To_UInt_InRange" },
        new("{ ulong x = 42; return x; }", 42ul) { TestName = "ConstExpr_Int_To_ULong_InRange" },
    ];

    /// <summary>
    /// ECMA-334 §10.2.11 -- Out-of-range constant expression conversions.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ConstantExprOutOfRangeCases() =>
    [
        new("{ byte x = 256; return x; }") { TestName = "ConstExpr_Int_To_Byte_OutOfRange" },
        new("{ byte x = -1; return x; }") { TestName = "ConstExpr_Int_To_Byte_Negative" },
        new("{ sbyte x = 128; return x; }") { TestName = "ConstExpr_Int_To_SByte_OutOfRange" },
    ];

    /// <summary>
    /// ECMA-334 §10.3.2 -- Explicit overflow narrowing (unchecked).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExplicitNarrowingOverflowCases() =>
    [
        new("unchecked((byte)256)", (byte)0) { TestName = "ExplicitOverflow_IntToByte_Wraps" },
        new("unchecked((sbyte)128)", (sbyte)-128) { TestName = "ExplicitOverflow_IntToSByte_Wraps" },
        new("unchecked((short)32768)", (short)-32768) { TestName = "ExplicitOverflow_IntToShort_Wraps" },
        new("unchecked((byte)-1)", (byte)255) { TestName = "ExplicitOverflow_NegativeIntToByte_Wraps" },
        new("unchecked((ushort)-1)", (ushort)65535) { TestName = "ExplicitOverflow_NegativeIntToUShort_Wraps" },
        new("unchecked((uint)-1)", 4294967295u) { TestName = "ExplicitOverflow_NegativeIntToUInt_Wraps" },
    ];

    /// <summary>
    /// ECMA-334 §10.2.3 -- Implicit conversion negative tests (forbidden paths).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ForbiddenImplicitCases() =>
    [
        // ECMA-334 §10.2.3: float -> decimal has NO implicit conversion.
        new("{ float f = 5.0f; decimal d = f; return d; }") { TestName = "ImplicitConversion_FloatToDecimal_Fails" },
        new("{ double d = 5.0; decimal m = d; return m; }") { TestName = "ImplicitConversion_DoubleToDecimal_Fails" },
        // Narrowing: no implicit conversion from wider to narrower types
        new("{ long l = 5L; int i = l; return i; }") { TestName = "ImplicitConversion_LongToInt_Fails" },
        new("{ double d = 5.0; float f = d; return f; }") { TestName = "ImplicitConversion_DoubleToFloat_Fails" },
        new("{ double d = 5.0; int i = d; return i; }") { TestName = "ImplicitConversion_DoubleToInt_Fails" },
        new("{ float f = 5.0f; int i = f; return i; }") { TestName = "ImplicitConversion_FloatToInt_Fails" },
    ];

    /// <summary>
    /// ECMA-334 §10.3.2 -- Explicit truncation/narrowing edge cases.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExplicitTruncationCases() =>
    [
        new("(int)3.9", 3) { TestName = "ExplicitTruncation_Double_3_9_ToInt_Truncates" },
        new("(int)(-3.9)", -3) { TestName = "ExplicitTruncation_Double_Neg3_9_ToInt_Truncates" },
        new("(char)65", 'A') { TestName = "ExplicitTruncation_IntToChar_65" },
        new("(int)'A'", 65) { TestName = "ExplicitTruncation_CharToInt_A" },
    ];

    /// <summary>
    /// ECMA-334 §10.3.2 -- Explicit truncation overflow wrapping (unchecked).
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExplicitTruncationOverflowCases() =>
    [
        new("unchecked((byte)256)", (byte)0) { TestName = "ExplicitTruncation_Byte256_Wraps" },
        new("unchecked((byte)(-1))", (byte)255) { TestName = "ExplicitTruncation_ByteNeg1_Wraps" },
        new("unchecked((short)32768)", (short)-32768) { TestName = "ExplicitTruncation_Short32768_Wraps" },
        new("unchecked((byte)1.5f)", (byte)1) { TestName = "ExplicitTruncation_Float1_5_ToByte" },
    ];

    /// <summary>
    /// ECMA-334 §12.18 -- Conditional operator type unification.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ConditionalTypeCases() =>
    [
        // Conditional with null literal
        new("true ? \"hello\" : null", "hello") { TestName = "Conditional_StringAndNull_TrueReturnsString" },
        new("false ? \"hello\" : null", null) { TestName = "Conditional_StringAndNull_FalseReturnsNull" },
        // Conditional with numeric type promotion
        new("true ? 5 : 10L", 5L) { TestName = "Conditional_IntAndLong_PromotesToLong" },
        new("false ? 5.0f : 3.0", 3.0) { TestName = "Conditional_FloatAndDouble_PromotesToDouble" },
        // Conditional with throw expression
        new("true ? 42 : throw new System.Exception(\"fail\")", 42) { TestName = "Conditional_IntAndThrow_TrueReturnsInt" },
        new("false ? throw new System.Exception(\"fail\") : 42", 42) { TestName = "Conditional_ThrowAndInt_FalseReturnsInt" },
    ];

    /// <summary>
    /// ECMA-334 §12.21.4 -- Compound assignment type preservation.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> CompoundAssignmentCases() =>
    [
        new("{ short x = 5; x += 3; return x; }", (short)8) { TestName = "CompoundAssign_Short_Value" },
        new("{ byte x = 200; x += 10; return x; }", (byte)210) { TestName = "CompoundAssign_Byte_Value" },
        new("{ int x = 5; x += 3; return x; }", 8) { TestName = "CompoundAssign_Int_Value" },
        new("{ long x = 5; x += 3; return x; }", 8L) { TestName = "CompoundAssign_Long_Value" },
        new("{ char x = 'A'; x += (char)1; return x; }", 'B') { TestName = "CompoundAssign_Char_Value" },
    ];
}
