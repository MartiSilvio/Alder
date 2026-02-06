// ECMA-334 §12.4.8 - Lifted Operator Compliance Tests for Small and Unsigned Types
// Validates lifted operator semantics for nullable types that undergo numeric promotion.
// byte?, sbyte?, short?, ushort? promote to int for arithmetic (ECMA-334 §12.4.7).
// uint?, ulong? have their own predefined operators.
// All tests validated against Roslyn for parity.

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiftedOperatorSmallTypeTests(CompilationMode mode)
{
    #region ECMA-334 §12.4.8 - Lifted Arithmetic: uint?

    // uint? uses its own predefined operators (no promotion).
    // TestCase doesn't support uint literals as expected values, so non-null tests use parity-only.

    [TestCase("{ uint? a = null; uint? b = 5u; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_UInt_LeftNull_ReturnsNull")]
    [TestCase("{ uint? a = 5u; uint? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_UInt_RightNull_ReturnsNull")]
    [TestCase("{ uint? a = null; uint? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_UInt_BothNull_ReturnsNull")]
    public async Task LiftedArithmetic_Add_UInt_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ uint? a = 5u; uint? b = 3u; return a + b; }",
        TestName = "LiftedArithmetic_Add_UInt_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add_UInt_NonNull(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ uint? a = null; uint? b = 5u; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_UInt_LeftNull_ReturnsNull")]
    [TestCase("{ uint? a = 5u; uint? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_UInt_RightNull_ReturnsNull")]
    [TestCase("{ uint? a = null; uint? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_UInt_BothNull_ReturnsNull")]
    public async Task LiftedArithmetic_Multiply_UInt_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ uint? a = 5u; uint? b = 3u; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_UInt_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply_UInt_NonNull(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Arithmetic: ulong?

    // ulong? uses its own predefined operators (no promotion).
    // TestCase doesn't support ulong literals as expected values, so non-null tests use parity-only.

    [TestCase("{ ulong? a = null; ulong? b = 5UL; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_ULong_LeftNull_ReturnsNull")]
    [TestCase("{ ulong? a = 5UL; ulong? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_ULong_RightNull_ReturnsNull")]
    [TestCase("{ ulong? a = null; ulong? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_ULong_BothNull_ReturnsNull")]
    public async Task LiftedArithmetic_Add_ULong_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ ulong? a = 5UL; ulong? b = 3UL; return a + b; }",
        TestName = "LiftedArithmetic_Add_ULong_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add_ULong_NonNull(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ ulong? a = null; ulong? b = 5UL; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_ULong_LeftNull_ReturnsNull")]
    [TestCase("{ ulong? a = 5UL; ulong? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_ULong_RightNull_ReturnsNull")]
    [TestCase("{ ulong? a = null; ulong? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_ULong_BothNull_ReturnsNull")]
    public async Task LiftedArithmetic_Multiply_ULong_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ ulong? a = 5UL; ulong? b = 3UL; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_ULong_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply_ULong_NonNull(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Arithmetic: byte? (promotes to int)

    // byte? arithmetic promotes both operands to int per ECMA-334 §12.4.7.
    // Result type is int?, not byte?.

    [TestCase("{ byte? a = null; byte? b = (byte?)3; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Byte_LeftNull_ReturnsNull")]
    [TestCase("{ byte? a = (byte?)5; byte? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Byte_RightNull_ReturnsNull")]
    [TestCase("{ byte? a = null; byte? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Byte_BothNull_ReturnsNull")]
    [TestCase("{ byte? a = (byte?)5; byte? b = (byte?)3; return a + b; }", 8,
        TestName = "LiftedArithmetic_Add_Byte_BothNonNull_ReturnsIntSum")]
    public async Task LiftedArithmetic_Add_Byte(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ byte? a = null; byte? b = (byte?)3; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Byte_LeftNull_ReturnsNull")]
    [TestCase("{ byte? a = (byte?)5; byte? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Byte_RightNull_ReturnsNull")]
    [TestCase("{ byte? a = null; byte? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Byte_BothNull_ReturnsNull")]
    [TestCase("{ byte? a = (byte?)5; byte? b = (byte?)3; return a * b; }", 15,
        TestName = "LiftedArithmetic_Multiply_Byte_BothNonNull_ReturnsIntProduct")]
    public async Task LiftedArithmetic_Multiply_Byte(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Arithmetic: sbyte? (promotes to int)

    // sbyte? arithmetic promotes both operands to int per ECMA-334 §12.4.7.

    [TestCase("{ sbyte? a = null; sbyte? b = (sbyte?)3; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_SByte_LeftNull_ReturnsNull")]
    [TestCase("{ sbyte? a = (sbyte?)5; sbyte? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_SByte_RightNull_ReturnsNull")]
    [TestCase("{ sbyte? a = null; sbyte? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_SByte_BothNull_ReturnsNull")]
    [TestCase("{ sbyte? a = (sbyte?)5; sbyte? b = (sbyte?)3; return a + b; }", 8,
        TestName = "LiftedArithmetic_Add_SByte_BothNonNull_ReturnsIntSum")]
    public async Task LiftedArithmetic_Add_SByte(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ sbyte? a = null; sbyte? b = (sbyte?)3; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_SByte_LeftNull_ReturnsNull")]
    [TestCase("{ sbyte? a = (sbyte?)10; sbyte? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_SByte_RightNull_ReturnsNull")]
    [TestCase("{ sbyte? a = null; sbyte? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_SByte_BothNull_ReturnsNull")]
    [TestCase("{ sbyte? a = (sbyte?)10; sbyte? b = (sbyte?)3; return a - b; }", 7,
        TestName = "LiftedArithmetic_Subtract_SByte_BothNonNull_ReturnsIntDifference")]
    public async Task LiftedArithmetic_Subtract_SByte(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Arithmetic: short? (promotes to int)

    // short? arithmetic promotes both operands to int per ECMA-334 §12.4.7.

    [TestCase("{ short? a = null; short? b = (short?)3; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Short_LeftNull_ReturnsNull")]
    [TestCase("{ short? a = (short?)5; short? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Short_RightNull_ReturnsNull")]
    [TestCase("{ short? a = null; short? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Short_BothNull_ReturnsNull")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)3; return a + b; }", 8,
        TestName = "LiftedArithmetic_Add_Short_BothNonNull_ReturnsIntSum")]
    public async Task LiftedArithmetic_Add_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ short? a = null; short? b = (short?)3; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Short_LeftNull_ReturnsNull")]
    [TestCase("{ short? a = (short?)5; short? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Short_RightNull_ReturnsNull")]
    [TestCase("{ short? a = null; short? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Short_BothNull_ReturnsNull")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)3; return a * b; }", 15,
        TestName = "LiftedArithmetic_Multiply_Short_BothNonNull_ReturnsIntProduct")]
    public async Task LiftedArithmetic_Multiply_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Arithmetic: ushort? (promotes to int)

    // ushort? arithmetic promotes both operands to int per ECMA-334 §12.4.7.

    [TestCase("{ ushort? a = null; ushort? b = (ushort?)3; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_UShort_LeftNull_ReturnsNull")]
    [TestCase("{ ushort? a = (ushort?)5; ushort? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_UShort_RightNull_ReturnsNull")]
    [TestCase("{ ushort? a = null; ushort? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_UShort_BothNull_ReturnsNull")]
    [TestCase("{ ushort? a = (ushort?)5; ushort? b = (ushort?)3; return a + b; }", 8,
        TestName = "LiftedArithmetic_Add_UShort_BothNonNull_ReturnsIntSum")]
    public async Task LiftedArithmetic_Add_UShort(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ ushort? a = null; ushort? b = (ushort?)3; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_UShort_LeftNull_ReturnsNull")]
    [TestCase("{ ushort? a = (ushort?)10; ushort? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_UShort_RightNull_ReturnsNull")]
    [TestCase("{ ushort? a = null; ushort? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_UShort_BothNull_ReturnsNull")]
    [TestCase("{ ushort? a = (ushort?)10; ushort? b = (ushort?)3; return a - b; }", 7,
        TestName = "LiftedArithmetic_Subtract_UShort_BothNonNull_ReturnsIntDifference")]
    public async Task LiftedArithmetic_Subtract_UShort(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Unary Operators (non-int types)

    // Extends unary operator coverage beyond the int? tests in LiftedOperatorTests.

    [TestCase("{ long? a = null; return -a; }", null,
        TestName = "LiftedUnary_Negate_Long_Null_ReturnsNull")]
    [TestCase("{ long? a = 5L; return -a; }", -5L,
        TestName = "LiftedUnary_Negate_Long_NonNull_ReturnsNegated")]
    public async Task LiftedUnary_Negate_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; return -a; }", null,
        TestName = "LiftedUnary_Negate_Double_Null_ReturnsNull")]
    public async Task LiftedUnary_Negate_Double_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = 3.14; return -a; }",
        TestName = "LiftedUnary_Negate_Double_NonNull_ReturnsNegated")]
    public async Task LiftedUnary_Negate_Double_NonNull(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ float? a = null; return -a; }", null,
        TestName = "LiftedUnary_Negate_Float_Null_ReturnsNull")]
    public async Task LiftedUnary_Negate_Float_Null(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ float? a = 2.5f; return -a; }",
        TestName = "LiftedUnary_Negate_Float_NonNull_ReturnsNegated")]
    public async Task LiftedUnary_Negate_Float_NonNull(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // short? unary negation promotes to int per ECMA-334 §12.4.7.2
    [TestCase("{ short? a = null; return -a; }", null,
        TestName = "LiftedUnary_Negate_Short_Null_ReturnsNull")]
    [TestCase("{ short? a = (short?)5; return -a; }", -5,
        TestName = "LiftedUnary_Negate_Short_NonNull_ReturnsIntNegated")]
    public async Task LiftedUnary_Negate_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Bitwise complement for long?
    [TestCase("{ long? a = null; return ~a; }", null,
        TestName = "LiftedUnary_BitwiseNot_Long_Null_ReturnsNull")]
    [TestCase("{ long? a = 5L; return ~a; }", -6L,
        TestName = "LiftedUnary_BitwiseNot_Long_NonNull_ReturnsComplement")]
    public async Task LiftedUnary_BitwiseNot_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Unary plus for long?
    [TestCase("{ long? a = null; return +a; }", null,
        TestName = "LiftedUnary_Plus_Long_Null_ReturnsNull")]
    [TestCase("{ long? a = 5L; return +a; }", 5L,
        TestName = "LiftedUnary_Plus_Long_NonNull_ReturnsSame")]
    public async Task LiftedUnary_Plus_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Equality: uint?

    // Equality returns bool, so TestCase expected values work for all types.

    [TestCase("{ uint? a = null; uint? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_UInt_BothNull_ReturnsTrue")]
    [TestCase("{ uint? a = null; uint? b = 5u; return a == b; }", false,
        TestName = "LiftedEquality_Eq_UInt_LeftNull_ReturnsFalse")]
    [TestCase("{ uint? a = 5u; uint? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_UInt_RightNull_ReturnsFalse")]
    [TestCase("{ uint? a = 5u; uint? b = 5u; return a == b; }", true,
        TestName = "LiftedEquality_Eq_UInt_EqualValues_ReturnsTrue")]
    [TestCase("{ uint? a = 5u; uint? b = 3u; return a == b; }", false,
        TestName = "LiftedEquality_Eq_UInt_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Eq_UInt(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ uint? a = null; uint? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_UInt_BothNull_ReturnsFalse")]
    [TestCase("{ uint? a = null; uint? b = 5u; return a != b; }", true,
        TestName = "LiftedEquality_Neq_UInt_LeftNull_ReturnsTrue")]
    [TestCase("{ uint? a = 5u; uint? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_UInt_RightNull_ReturnsTrue")]
    [TestCase("{ uint? a = 5u; uint? b = 5u; return a != b; }", false,
        TestName = "LiftedEquality_Neq_UInt_EqualValues_ReturnsFalse")]
    [TestCase("{ uint? a = 5u; uint? b = 3u; return a != b; }", true,
        TestName = "LiftedEquality_Neq_UInt_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_Neq_UInt(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Equality: ulong?

    [TestCase("{ ulong? a = null; ulong? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_ULong_BothNull_ReturnsTrue")]
    [TestCase("{ ulong? a = null; ulong? b = 5UL; return a == b; }", false,
        TestName = "LiftedEquality_Eq_ULong_LeftNull_ReturnsFalse")]
    [TestCase("{ ulong? a = 5UL; ulong? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_ULong_RightNull_ReturnsFalse")]
    [TestCase("{ ulong? a = 5UL; ulong? b = 5UL; return a == b; }", true,
        TestName = "LiftedEquality_Eq_ULong_EqualValues_ReturnsTrue")]
    [TestCase("{ ulong? a = 5UL; ulong? b = 3UL; return a == b; }", false,
        TestName = "LiftedEquality_Eq_ULong_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Eq_ULong(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ ulong? a = null; ulong? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_ULong_BothNull_ReturnsFalse")]
    [TestCase("{ ulong? a = null; ulong? b = 5UL; return a != b; }", true,
        TestName = "LiftedEquality_Neq_ULong_LeftNull_ReturnsTrue")]
    [TestCase("{ ulong? a = 5UL; ulong? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_ULong_RightNull_ReturnsTrue")]
    [TestCase("{ ulong? a = 5UL; ulong? b = 5UL; return a != b; }", false,
        TestName = "LiftedEquality_Neq_ULong_EqualValues_ReturnsFalse")]
    [TestCase("{ ulong? a = 5UL; ulong? b = 3UL; return a != b; }", true,
        TestName = "LiftedEquality_Neq_ULong_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_Neq_ULong(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Equality: short?

    // short? equality: operands promoted to int for comparison.

    [TestCase("{ short? a = null; short? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Short_BothNull_ReturnsTrue")]
    [TestCase("{ short? a = null; short? b = (short?)5; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Short_LeftNull_ReturnsFalse")]
    [TestCase("{ short? a = (short?)5; short? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Short_RightNull_ReturnsFalse")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)5; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Short_EqualValues_ReturnsTrue")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)3; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Short_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Eq_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ short? a = null; short? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Short_BothNull_ReturnsFalse")]
    [TestCase("{ short? a = null; short? b = (short?)5; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Short_LeftNull_ReturnsTrue")]
    [TestCase("{ short? a = (short?)5; short? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Short_RightNull_ReturnsTrue")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)5; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Short_EqualValues_ReturnsFalse")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)3; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Short_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_Neq_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Equality: byte?

    // byte? equality: operands promoted to int for comparison.

    [TestCase("{ byte? a = null; byte? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Byte_BothNull_ReturnsTrue")]
    [TestCase("{ byte? a = null; byte? b = (byte?)5; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Byte_LeftNull_ReturnsFalse")]
    [TestCase("{ byte? a = (byte?)5; byte? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Byte_RightNull_ReturnsFalse")]
    [TestCase("{ byte? a = (byte?)5; byte? b = (byte?)5; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Byte_EqualValues_ReturnsTrue")]
    [TestCase("{ byte? a = (byte?)5; byte? b = (byte?)3; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Byte_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Eq_Byte(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ byte? a = null; byte? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Byte_BothNull_ReturnsFalse")]
    [TestCase("{ byte? a = null; byte? b = (byte?)5; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Byte_LeftNull_ReturnsTrue")]
    [TestCase("{ byte? a = (byte?)5; byte? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Byte_RightNull_ReturnsTrue")]
    [TestCase("{ byte? a = (byte?)5; byte? b = (byte?)5; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Byte_EqualValues_ReturnsFalse")]
    [TestCase("{ byte? a = (byte?)5; byte? b = (byte?)3; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Byte_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_Neq_Byte(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Relational: uint?

    // Relational operators return false when any operand is null.

    [TestCase("{ uint? a = null; uint? b = 5u; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_UInt_LeftNull_ReturnsFalse")]
    [TestCase("{ uint? a = 5u; uint? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_UInt_RightNull_ReturnsFalse")]
    [TestCase("{ uint? a = null; uint? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_UInt_BothNull_ReturnsFalse")]
    [TestCase("{ uint? a = 3u; uint? b = 5u; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_UInt_LessThan_ReturnsTrue")]
    public async Task LiftedRelational_LessThan_UInt(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ uint? a = null; uint? b = 5u; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_UInt_LeftNull_ReturnsFalse")]
    [TestCase("{ uint? a = 5u; uint? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_UInt_RightNull_ReturnsFalse")]
    [TestCase("{ uint? a = null; uint? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_UInt_BothNull_ReturnsFalse")]
    [TestCase("{ uint? a = 5u; uint? b = 3u; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_UInt_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan_UInt(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Relational: short?

    // short? relational: operands promoted to int before comparison.

    [TestCase("{ short? a = null; short? b = (short?)5; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Short_LeftNull_ReturnsFalse")]
    [TestCase("{ short? a = (short?)5; short? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Short_RightNull_ReturnsFalse")]
    [TestCase("{ short? a = null; short? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Short_BothNull_ReturnsFalse")]
    [TestCase("{ short? a = (short?)3; short? b = (short?)5; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_Short_LessThan_ReturnsTrue")]
    public async Task LiftedRelational_LessThan_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ short? a = null; short? b = (short?)5; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Short_LeftNull_ReturnsFalse")]
    [TestCase("{ short? a = (short?)5; short? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Short_RightNull_ReturnsFalse")]
    [TestCase("{ short? a = null; short? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Short_BothNull_ReturnsFalse")]
    [TestCase("{ short? a = (short?)5; short? b = (short?)3; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_Short_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan_Short(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
