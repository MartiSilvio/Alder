namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 sections 10.2 and 10.3 — Implicit and Explicit conversion compliance tests.
/// Validates that CsEval's conversion behavior matches Roslyn and the ECMA-334 7th edition spec exactly.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ConversionTests(CompilationMode mode)
{
    #region ECMA-334 §10.2.3 — Implicit Numeric Conversions

    // ECMA-334 §10.2.3: "From sbyte to short, int, long, float, double, or decimal."
    [TestCase("{ sbyte x = 5; short y = x; return y; }", (short)5, TestName = "Implicit_SByte_To_Short")]
    [TestCase("{ sbyte x = 5; int y = x; return y; }", 5, TestName = "Implicit_SByte_To_Int")]
    [TestCase("{ sbyte x = 5; long y = x; return y; }", 5L, TestName = "Implicit_SByte_To_Long")]
    [TestCase("{ sbyte x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_SByte_To_Float")]
    [TestCase("{ sbyte x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_SByte_To_Double")]
    [TestCase("{ sbyte x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_SByte_To_Decimal")]
    // ECMA-334 §10.2.3: "From byte to short, ushort, int, uint, long, ulong, float, double, or decimal."
    [TestCase("{ byte x = 5; short y = x; return y; }", (short)5, TestName = "Implicit_Byte_To_Short")]
    [TestCase("{ byte x = 5; ushort y = x; return y; }", (ushort)5, TestName = "Implicit_Byte_To_UShort")]
    [TestCase("{ byte x = 5; int y = x; return y; }", 5, TestName = "Implicit_Byte_To_Int")]
    [TestCase("{ byte x = 5; uint y = x; return y; }", 5u, TestName = "Implicit_Byte_To_UInt")]
    [TestCase("{ byte x = 5; long y = x; return y; }", 5L, TestName = "Implicit_Byte_To_Long")]
    [TestCase("{ byte x = 5; ulong y = x; return y; }", 5ul, TestName = "Implicit_Byte_To_ULong")]
    [TestCase("{ byte x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_Byte_To_Float")]
    [TestCase("{ byte x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_Byte_To_Double")]
    [TestCase("{ byte x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_Byte_To_Decimal")]
    // ECMA-334 §10.2.3: "From short to int, long, float, double, or decimal."
    [TestCase("{ short x = 5; int y = x; return y; }", 5, TestName = "Implicit_Short_To_Int")]
    [TestCase("{ short x = 5; long y = x; return y; }", 5L, TestName = "Implicit_Short_To_Long")]
    [TestCase("{ short x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_Short_To_Float")]
    [TestCase("{ short x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_Short_To_Double")]
    [TestCase("{ short x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_Short_To_Decimal")]
    // ECMA-334 §10.2.3: "From ushort to int, uint, long, ulong, float, double, or decimal."
    [TestCase("{ ushort x = 5; int y = x; return y; }", 5, TestName = "Implicit_UShort_To_Int")]
    [TestCase("{ ushort x = 5; uint y = x; return y; }", 5u, TestName = "Implicit_UShort_To_UInt")]
    [TestCase("{ ushort x = 5; long y = x; return y; }", 5L, TestName = "Implicit_UShort_To_Long")]
    [TestCase("{ ushort x = 5; ulong y = x; return y; }", 5ul, TestName = "Implicit_UShort_To_ULong")]
    [TestCase("{ ushort x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_UShort_To_Float")]
    [TestCase("{ ushort x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_UShort_To_Double")]
    [TestCase("{ ushort x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_UShort_To_Decimal")]
    // ECMA-334 §10.2.3: "From int to long, float, double, or decimal."
    [TestCase("{ int x = 5; long y = x; return y; }", 5L, TestName = "Implicit_Int_To_Long")]
    [TestCase("{ int x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_Int_To_Float")]
    [TestCase("{ int x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_Int_To_Double")]
    [TestCase("{ int x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_Int_To_Decimal")]
    // ECMA-334 §10.2.3: "From uint to long, ulong, float, double, or decimal."
    [TestCase("{ uint x = 5; long y = x; return y; }", 5L, TestName = "Implicit_UInt_To_Long")]
    [TestCase("{ uint x = 5; ulong y = x; return y; }", 5ul, TestName = "Implicit_UInt_To_ULong")]
    [TestCase("{ uint x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_UInt_To_Float")]
    [TestCase("{ uint x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_UInt_To_Double")]
    [TestCase("{ uint x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_UInt_To_Decimal")]
    // ECMA-334 §10.2.3: "From long to float, double, or decimal."
    [TestCase("{ long x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_Long_To_Float")]
    [TestCase("{ long x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_Long_To_Double")]
    [TestCase("{ long x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_Long_To_Decimal")]
    // ECMA-334 §10.2.3: "From ulong to float, double, or decimal."
    [TestCase("{ ulong x = 5; float y = x; return y; }", 5.0f, TestName = "Implicit_ULong_To_Float")]
    [TestCase("{ ulong x = 5; double y = x; return y; }", 5.0d, TestName = "Implicit_ULong_To_Double")]
    [TestCase("{ ulong x = 5; decimal y = x; return y; }", 5, TestName = "Implicit_ULong_To_Decimal")]
    // ECMA-334 §10.2.3: "From char to ushort, int, uint, long, ulong, float, double, or decimal."
    [TestCase("{ char x = 'A'; ushort y = x; return y; }", (ushort)65, TestName = "Implicit_Char_To_UShort")]
    [TestCase("{ char x = 'A'; int y = x; return y; }", 65, TestName = "Implicit_Char_To_Int")]
    [TestCase("{ char x = 'A'; uint y = x; return y; }", 65u, TestName = "Implicit_Char_To_UInt")]
    [TestCase("{ char x = 'A'; long y = x; return y; }", 65L, TestName = "Implicit_Char_To_Long")]
    [TestCase("{ char x = 'A'; ulong y = x; return y; }", 65ul, TestName = "Implicit_Char_To_ULong")]
    [TestCase("{ char x = 'A'; float y = x; return y; }", 65.0f, TestName = "Implicit_Char_To_Float")]
    [TestCase("{ char x = 'A'; double y = x; return y; }", 65.0d, TestName = "Implicit_Char_To_Double")]
    [TestCase("{ char x = 'A'; decimal y = x; return y; }", 65, TestName = "Implicit_Char_To_Decimal")]
    // ECMA-334 §10.2.3: "From float to double."
    [TestCase("{ float x = 3.14f; double y = x; return y; }", (double)3.14f, TestName = "Implicit_Float_To_Double")]
    public async Task ImplicitNumericConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.3 — Guard: No Implicit Conversion TO char

    // ECMA-334 §10.2.3: "There are no predefined implicit conversions to the char type,
    // so values of the other integral types do not automatically convert to the char type."
    // Also ECMA-334 §8.3.6: "even though the byte and ushort types have ranges of values
    // that are fully representable using the char type, implicit conversions from
    // sbyte, byte, or ushort to char do not exist."

    [TestCase("{ byte x = 65; char c = x; return c; }", TestName = "Guard_NoImplicit_Byte_To_Char")]
    [TestCase("{ ushort x = 65; char c = x; return c; }", TestName = "Guard_NoImplicit_UShort_To_Char")]
    [TestCase("{ sbyte x = 65; char c = x; return c; }", TestName = "Guard_NoImplicit_SByte_To_Char")]
    [TestCase("{ short x = 65; char c = x; return c; }", TestName = "Guard_NoImplicit_Short_To_Char")]
    [TestCase("{ int x = 65; char c = x; return c; }", TestName = "Guard_NoImplicit_Int_To_Char")]
    [TestCase("{ long x = 65; char c = x; return c; }", TestName = "Guard_NoImplicit_Long_To_Char")]
    public async Task ImplicitConversionToChar_ShouldFail(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"Should not allow implicit conversion to char: {expr}");

        // Roslyn also rejects these
        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync(expr),
            Throws.Exception,
            $"Roslyn should also reject: {expr}");
    }

    #endregion

    #region ECMA-334 §10.3.2 — Explicit Numeric Conversions (Cast)

    // ECMA-334 §10.3.2: Explicit numeric conversions exist between all numeric type pairs
    // where an implicit conversion does not already exist.
    [TestCase("(byte)255", (byte)255, TestName = "Explicit_Int_To_Byte")]
    [TestCase("(sbyte)127", (sbyte)127, TestName = "Explicit_Int_To_SByte")]
    [TestCase("(short)1000", (short)1000, TestName = "Explicit_Int_To_Short")]
    [TestCase("(ushort)50000", (ushort)50000, TestName = "Explicit_Int_To_UShort")]
    [TestCase("(char)65", 'A', TestName = "Explicit_Int_To_Char")]
    [TestCase("(char)90", 'Z', TestName = "Explicit_Int_To_Char_Z")]
    [TestCase("(int)'A'", 65, TestName = "Explicit_Char_To_Int")]
    [TestCase("(byte)'A'", (byte)65, TestName = "Explicit_Char_To_Byte")]
    [TestCase("(int)3.7", 3, TestName = "Explicit_Double_To_Int_Truncates")]
    [TestCase("(int)-3.9", -3, TestName = "Explicit_NegativeDouble_To_Int_Truncates")]
    [TestCase("(long)3.9", 3L, TestName = "Explicit_Double_To_Long_Truncates")]
    [TestCase("(int)3.5f", 3, TestName = "Explicit_Float_To_Int_Truncates")]
    [TestCase("(float)3.14", 3.14f, TestName = "Explicit_Double_To_Float")]
    [TestCase("(double)3.14f", (double)3.14f, TestName = "Explicit_Float_To_Double")]
    [TestCase("(int)3.7m", 3, TestName = "Explicit_Decimal_To_Int")]
    [TestCase("(double)42", 42.0, TestName = "Explicit_Int_To_Double")]
    [TestCase("(long)42", 42L, TestName = "Explicit_Int_To_Long")]
    [TestCase("(int)100L", 100, TestName = "Explicit_Long_To_Int")]
    [TestCase("(uint)100", 100u, TestName = "Explicit_Int_To_UInt")]
    [TestCase("(ulong)100", 100ul, TestName = "Explicit_Int_To_ULong")]
    // Cross-type explicit casts
    [TestCase("(byte)(short)200", (byte)200, TestName = "Explicit_Short_To_Byte")]
    [TestCase("(sbyte)(ushort)100", (sbyte)100, TestName = "Explicit_UShort_To_SByte")]
    [TestCase("(char)(byte)65", 'A', TestName = "Explicit_Byte_To_Char")]
    [TestCase("(char)(ushort)65", 'A', TestName = "Explicit_UShort_To_Char")]
    public async Task ExplicitNumericConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.6 / §10.6.1 — Implicit Nullable Conversions

    // ECMA-334 §10.6.1: "An implicit or explicit conversion from S to T?"
    // T -> T? (identity lift)
    [TestCase("{ int x = 42; int? y = x; return y; }", 42, TestName = "Implicit_Int_To_NullableInt")]
    [TestCase("{ long x = 42; long? y = x; return y; }", 42L, TestName = "Implicit_Long_To_NullableLong")]
    [TestCase("{ double x = 3.14; double? y = x; return y; }", 3.14, TestName = "Implicit_Double_To_NullableDouble")]
    [TestCase("{ bool x = true; bool? y = x; return y; }", true, TestName = "Implicit_Bool_To_NullableBool")]
    [TestCase("{ char x = 'A'; char? y = x; return y; }", 'A', TestName = "Implicit_Char_To_NullableChar")]
    [TestCase("{ byte x = 5; byte? y = x; return y; }", (byte)5, TestName = "Implicit_Byte_To_NullableByte")]
    // ECMA-334 §10.6.1: "An implicit or explicit conversion from S to T?" (lifted widening)
    // S -> T? where S -> T is an implicit numeric conversion
    [TestCase("{ int x = 42; long? y = x; return y; }", 42L, TestName = "Implicit_Int_To_NullableLong")]
    [TestCase("{ int x = 42; double? y = x; return y; }", 42.0, TestName = "Implicit_Int_To_NullableDouble")]
    [TestCase("{ byte x = 5; int? y = x; return y; }", 5, TestName = "Implicit_Byte_To_NullableInt")]
    [TestCase("{ float x = 3.14f; double? y = x; return y; }", (double)3.14f, TestName = "Implicit_Float_To_NullableDouble")]
    [TestCase("{ char x = 'A'; int? y = x; return y; }", 65, TestName = "Implicit_Char_To_NullableInt")]
    // Null assignment to nullable
    [TestCase("{ int? x = null; return x; }", null, TestName = "Implicit_Null_To_NullableInt")]
    [TestCase("{ double? x = null; return x; }", null, TestName = "Implicit_Null_To_NullableDouble")]
    [TestCase("{ bool? x = null; return x; }", null, TestName = "Implicit_Null_To_NullableBool")]
    public async Task ImplicitNullableConversions(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.11 — Implicit Constant Expression Conversions

    // ECMA-334 §10.2.11: "A constant_expression of type int can be converted to type
    // sbyte, byte, short, ushort, uint, or ulong, provided the value of the
    // constant_expression is within the range of the destination type."

    [TestCase("{ byte x = 255; return x; }", (byte)255, TestName = "ConstExpr_Int_To_Byte_InRange")]
    [TestCase("{ byte x = 0; return x; }", (byte)0, TestName = "ConstExpr_Int_To_Byte_Zero")]
    [TestCase("{ sbyte x = 127; return x; }", (sbyte)127, TestName = "ConstExpr_Int_To_SByte_MaxRange")]
    [TestCase("{ sbyte x = -128; return x; }", (sbyte)-128, TestName = "ConstExpr_Int_To_SByte_MinRange")]
    [TestCase("{ short x = 1000; return x; }", (short)1000, TestName = "ConstExpr_Int_To_Short_InRange")]
    [TestCase("{ ushort x = 50000; return x; }", (ushort)50000, TestName = "ConstExpr_Int_To_UShort_InRange")]
    [TestCase("{ uint x = 42; return x; }", 42u, TestName = "ConstExpr_Int_To_UInt_InRange")]
    [TestCase("{ ulong x = 42; return x; }", 42ul, TestName = "ConstExpr_Int_To_ULong_InRange")]
    public async Task ImplicitConstantExpressionConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Out-of-range constant expression conversions should fail
    [TestCase("{ byte x = 256; return x; }", TestName = "ConstExpr_Int_To_Byte_OutOfRange")]
    [TestCase("{ byte x = -1; return x; }", TestName = "ConstExpr_Int_To_Byte_Negative")]
    [TestCase("{ sbyte x = 128; return x; }", TestName = "ConstExpr_Int_To_SByte_OutOfRange")]
    public async Task ImplicitConstantExpressionConversions_OutOfRange_ShouldFail(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"Should reject out-of-range constant expression: {expr}");

        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync(expr),
            Throws.Exception,
            $"Roslyn should also reject: {expr}");
    }

    #endregion

    #region ECMA-334 §10.3.2 — Explicit Overflow Narrowing (unchecked)

    // ECMA-334 §10.3.2: In unchecked context, narrowing wraps
    [TestCase("unchecked((byte)256)", (byte)0, TestName = "ExplicitOverflow_IntToByte_Wraps")]
    [TestCase("unchecked((sbyte)128)", (sbyte)-128, TestName = "ExplicitOverflow_IntToSByte_Wraps")]
    [TestCase("unchecked((short)32768)", (short)-32768, TestName = "ExplicitOverflow_IntToShort_Wraps")]
    [TestCase("unchecked((byte)-1)", (byte)255, TestName = "ExplicitOverflow_NegativeIntToByte_Wraps")]
    [TestCase("unchecked((ushort)-1)", (ushort)65535, TestName = "ExplicitOverflow_NegativeIntToUShort_Wraps")]
    [TestCase("unchecked((uint)-1)", 4294967295u, TestName = "ExplicitOverflow_NegativeIntToUInt_Wraps")]
    public async Task ExplicitNarrowingOverflow(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region CanImplicitlyConvert API — Direct Verification

    // Verify the CanImplicitlyConvert API returns correct results for key type pairs
    [Test]
    public void CanImplicitlyConvert_ByteToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from byte to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(byte), typeof(char)),
            Is.False,
            "byte -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_UShortToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from ushort to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(ushort), typeof(char)),
            Is.False,
            "ushort -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToUShort_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> ushort is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(ushort)),
            Is.True,
            "char -> ushort must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToInt_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> int is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(int)),
            Is.True,
            "char -> int must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToLong_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: int -> long is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long)),
            Is.True,
            "int -> long must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableInt_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: T -> T? is an implicit nullable conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(int?)),
            Is.True,
            "int -> int? must be an implicit conversion per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableLong_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: S -> T? where S -> T is implicit numeric
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long?)),
            Is.True,
            "int -> long? must be an implicit conversion (lifted from int -> long) per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_FloatToDouble_ReturnsTrue()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(float), typeof(double)),
            Is.True,
            "float -> double must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_DoubleToFloat_ReturnsFalse()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(double), typeof(float)),
            Is.False,
            "double -> float is NOT an implicit conversion (requires explicit cast)");
    }

    [Test]
    public void CanImplicitlyConvert_LongToInt_ReturnsFalse()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(long), typeof(int)),
            Is.False,
            "long -> int is NOT an implicit conversion (requires explicit cast)");
    }

    #endregion

    #region Compound Assignment Type Preservation

    // ECMA-334 §12.21.4: For compound assignment x op= y, the result of x op y is
    // implicitly converted back to the type of x. The declared type is preserved.
    [TestCase("{ short x = 5; x += 3; return x; }", (short)8, TestName = "CompoundAssign_Short_Value")]
    [TestCase("{ byte x = 200; x += 10; return x; }", (byte)210, TestName = "CompoundAssign_Byte_Value")]
    [TestCase("{ int x = 5; x += 3; return x; }", 8, TestName = "CompoundAssign_Int_Value")]
    [TestCase("{ long x = 5; x += 3; return x; }", 8L, TestName = "CompoundAssign_Long_Value")]
    [TestCase("{ char x = 'A'; x += (char)1; return x; }", 'B', TestName = "CompoundAssign_Char_Value")]
    public async Task CompoundAssignment_TypePreservation(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.3 — Implicit Conversion Negative Tests (Forbidden Paths)

    // ECMA-334 §10.2.3: float -> decimal has NO implicit conversion.
    // ECMA-334 §12.4.7.3 Rule 1: "a binding-time error occurs if the other operand is of type float or double."
    [TestCase("{ float f = 5.0f; decimal d = f; return d; }", TestName = "ImplicitConversion_FloatToDecimal_Fails")]
    [TestCase("{ double d = 5.0; decimal m = d; return m; }", TestName = "ImplicitConversion_DoubleToDecimal_Fails")]
    // Narrowing: no implicit conversion from wider to narrower types
    [TestCase("{ long l = 5L; int i = l; return i; }", TestName = "ImplicitConversion_LongToInt_Fails")]
    [TestCase("{ double d = 5.0; float f = d; return f; }", TestName = "ImplicitConversion_DoubleToFloat_Fails")]
    [TestCase("{ double d = 5.0; int i = d; return i; }", TestName = "ImplicitConversion_DoubleToInt_Fails")]
    [TestCase("{ float f = 5.0f; int i = f; return i; }", TestName = "ImplicitConversion_FloatToInt_Fails")]
    public async Task ImplicitConversion_ForbiddenPaths_ShouldFail(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"Should not allow implicit conversion: {expr}");

        // Roslyn also rejects these
        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync(expr),
            Throws.Exception,
            $"Roslyn should also reject: {expr}");
    }

    #endregion

    #region ECMA-334 §10.3.2 — Explicit Truncation/Narrowing Edge Cases

    // Additional explicit cast truncation and float-to-int truncation tests.
    // Note: Roslyn scripting uses checked context by default, so overflow casts like
    // (byte)256 are compile errors there. CsEval defaults to unchecked, so wrapping occurs.
    // Overflow wrapping casts use unchecked() for Roslyn parity (see §10.3.2 overflow region).
    // Float-to-int truncation works the same in both checked and unchecked contexts.
    [TestCase("(int)3.9", 3, TestName = "ExplicitTruncation_Double_3_9_ToInt_Truncates")]
    [TestCase("(int)(-3.9)", -3, TestName = "ExplicitTruncation_Double_Neg3_9_ToInt_Truncates")]
    [TestCase("(char)65", 'A', TestName = "ExplicitTruncation_IntToChar_65")]
    [TestCase("(int)'A'", 65, TestName = "ExplicitTruncation_CharToInt_A")]
    public async Task ExplicitTruncation_EdgeCases(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Overflow narrowing casts: CsEval uses unchecked context by default.
    // These verify wrapping behavior matches unchecked C# semantics.
    [TestCase("unchecked((byte)256)", (byte)0, TestName = "ExplicitTruncation_Byte256_Wraps")]
    [TestCase("unchecked((byte)(-1))", (byte)255, TestName = "ExplicitTruncation_ByteNeg1_Wraps")]
    [TestCase("unchecked((short)32768)", (short)-32768, TestName = "ExplicitTruncation_Short32768_Wraps")]
    [TestCase("unchecked((byte)1.5f)", (byte)1, TestName = "ExplicitTruncation_Float1_5_ToByte")]
    public async Task ExplicitTruncation_OverflowWrapping(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.18 — Conditional Operator Type Unification

    // Conditional with null literal: result type should be the non-null branch's type
    [TestCase("true ? \"hello\" : null", "hello", TestName = "Conditional_StringAndNull_TrueReturnsString")]
    [TestCase("false ? \"hello\" : null", null, TestName = "Conditional_StringAndNull_FalseReturnsNull")]
    // Conditional with numeric type promotion
    [TestCase("true ? 5 : 10L", 5L, TestName = "Conditional_IntAndLong_PromotesToLong")]
    [TestCase("false ? 5.0f : 3.0", 3.0, TestName = "Conditional_FloatAndDouble_PromotesToDouble")]
    // Conditional with throw expression: result type is the non-throw branch's type
    [TestCase("true ? 42 : throw new System.Exception(\"fail\")", 42, TestName = "Conditional_IntAndThrow_TrueReturnsInt")]
    [TestCase("false ? throw new System.Exception(\"fail\") : 42", 42, TestName = "Conditional_ThrowAndInt_FalseReturnsInt")]
    public async Task ConditionalOperator_TypeUnification(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Verify conditional with string+null returns correct type at runtime
    [Test]
    public async Task ConditionalOperator_StringAndNull_TypeIsString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true ? \"hello\" : null");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("true ? \"hello\" : null");

        Assert.That(result, Is.EqualTo("hello"), "Value should be 'hello'");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(string)), "Type should be string");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    #endregion

    #region ECMA-334 Type System Compliance Verification

    // Criterion 1: "(byte)5 + (short)3 returns int" — binary numeric promotion
    [Test]
    public async Task S12_4_7_BytePlusShort_ReturnsInt()
    {
        await TestHelpers.RunCSharpParityTestAsync("(byte)5 + (short)3", 8, mode);
    }

    // Criterion 2: "decimal d = 5.0f fails at evaluation time" — no implicit float-to-decimal
    [Test]
    public async Task S10_2_FloatToDecimal_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate("{ float f = 5.0f; decimal d = f; return d; }"),
            "Implicit float -> decimal should be rejected");

        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync("{ float f = 5.0f; decimal d = f; return d; }"),
            Throws.Exception,
            "Roslyn should also reject float -> decimal");
    }

    // Criterion 3: "(byte)256 returns 0" and "(int)3.9 returns 3" — explicit cast truncation
    // Note: (byte)256 requires unchecked context for Roslyn parity since Roslyn scripting
    // defaults to checked context. CsEval defaults to unchecked, matching normal C# behavior.
    [Test]
    public async Task S10_3_ExplicitCastTruncation()
    {
        await TestHelpers.RunCSharpParityTestAsync("unchecked((byte)256)", (byte)0, mode);
        await TestHelpers.RunCSharpParityTestAsync("(int)3.9", 3, mode);
    }

    // Criterion 4: "5U + 3 returns uint" — constant expression conversion with uint
    [Test]
    public async Task S10_2_11_UIntPlusIntLiteral_ReturnsUInt()
    {
        await TestHelpers.RunCSharpParityTestAsync("5U + 3", 8U, mode);
    }

    // Criterion 5: "short x = 5; x += 3; x.GetType().FullName returns System.Int16" — compound assignment type preservation
    [Test]
    public async Task S12_18_CompoundAssign_PreservesShortType()
    {
        await TestHelpers.RunCSharpParityTestAsync("{ short x = 5; x += 3; return x; }", (short)8, mode);
    }

    #endregion
}
