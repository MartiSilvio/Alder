// ECMA-334 §12.4.8 - Lifted Operator Compliance Tests
// Validates that CsEval handles all four categories of lifted operator semantics correctly:
//   1. Arithmetic/bitwise: null operand -> null result
//   2. Equality (==, !=): null-aware comparison
//   3. Relational (<, >, <=, >=): null operand -> false result
//   4. Three-value bool? logic (&, |, ^): ECMA-334 §12.13.5
// All tests validated against Roslyn for parity.

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiftedOperatorTests(CompilationMode mode)
{
    #region ECMA-334 §12.4.8 - Lifted Arithmetic Operators

    // "For the binary operators + - * / % & | ^ << >>,
    //  a lifted form exists when the operand and result types are all non-nullable value types.
    //  The lifted form is constructed by adding a single ? modifier to each operand and result type.
    //  If one or both operands are null, the result is null."

    [TestCase("{ int? a = null; int? b = 5; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = 3; return a + b; }", 8,
        TestName = "LiftedArithmetic_Add_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 10; int? b = 3; return a - b; }", 7,
        TestName = "LiftedArithmetic_Subtract_BothNonNull_ReturnsDifference")]
    public async Task LiftedArithmetic_Subtract(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = 3; return a * b; }", 15,
        TestName = "LiftedArithmetic_Multiply_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 10; int? b = null; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 10; int? b = 2; return a / b; }", 5,
        TestName = "LiftedArithmetic_Divide_BothNonNull_ReturnsQuotient")]
    public async Task LiftedArithmetic_Divide(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 10; int? b = null; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 10; int? b = 3; return a % b; }", 1,
        TestName = "LiftedArithmetic_Modulo_BothNonNull_ReturnsRemainder")]
    public async Task LiftedArithmetic_Modulo(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region long? Arithmetic

    [TestCase("{ long? a = null; long? b = 5L; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Long_LeftNull_ReturnsNull")]
    [TestCase("{ long? a = 5L; long? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Long_RightNull_ReturnsNull")]
    [TestCase("{ long? a = null; long? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Long_BothNull_ReturnsNull")]
    [TestCase("{ long? a = 5L; long? b = 3L; return a + b; }", 8L,
        TestName = "LiftedArithmetic_Add_Long_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_Long_LeftNull_ReturnsNull")]
    [TestCase("{ long? a = 5L; long? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_Long_RightNull_ReturnsNull")]
    [TestCase("{ long? a = null; long? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_Long_BothNull_ReturnsNull")]
    [TestCase("{ long? a = 10L; long? b = 3L; return a - b; }", 7L,
        TestName = "LiftedArithmetic_Subtract_Long_BothNonNull_ReturnsDifference")]
    public async Task LiftedArithmetic_Subtract_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Long_LeftNull_ReturnsNull")]
    [TestCase("{ long? a = 5L; long? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Long_RightNull_ReturnsNull")]
    [TestCase("{ long? a = null; long? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Long_BothNull_ReturnsNull")]
    [TestCase("{ long? a = 5L; long? b = 3L; return a * b; }", 15L,
        TestName = "LiftedArithmetic_Multiply_Long_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_Long_LeftNull_ReturnsNull")]
    [TestCase("{ long? a = 10L; long? b = null; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_Long_RightNull_ReturnsNull")]
    [TestCase("{ long? a = null; long? b = null; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_Long_BothNull_ReturnsNull")]
    [TestCase("{ long? a = 10L; long? b = 2L; return a / b; }", 5L,
        TestName = "LiftedArithmetic_Divide_Long_BothNonNull_ReturnsQuotient")]
    public async Task LiftedArithmetic_Divide_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_Long_LeftNull_ReturnsNull")]
    [TestCase("{ long? a = 10L; long? b = null; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_Long_RightNull_ReturnsNull")]
    [TestCase("{ long? a = null; long? b = null; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_Long_BothNull_ReturnsNull")]
    [TestCase("{ long? a = 10L; long? b = 3L; return a % b; }", 1L,
        TestName = "LiftedArithmetic_Modulo_Long_BothNonNull_ReturnsRemainder")]
    public async Task LiftedArithmetic_Modulo_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region double? Arithmetic

    [TestCase("{ double? a = null; double? b = 5.0; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Double_LeftNull_ReturnsNull")]
    [TestCase("{ double? a = 5.0; double? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Double_RightNull_ReturnsNull")]
    [TestCase("{ double? a = null; double? b = null; return a + b; }", null,
        TestName = "LiftedArithmetic_Add_Double_BothNull_ReturnsNull")]
    [TestCase("{ double? a = 5.0; double? b = 3.0; return a + b; }", 8.0,
        TestName = "LiftedArithmetic_Add_Double_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_Double_LeftNull_ReturnsNull")]
    [TestCase("{ double? a = 5.0; double? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_Double_RightNull_ReturnsNull")]
    [TestCase("{ double? a = null; double? b = null; return a - b; }", null,
        TestName = "LiftedArithmetic_Subtract_Double_BothNull_ReturnsNull")]
    [TestCase("{ double? a = 10.0; double? b = 3.0; return a - b; }", 7.0,
        TestName = "LiftedArithmetic_Subtract_Double_BothNonNull_ReturnsDifference")]
    public async Task LiftedArithmetic_Subtract_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Double_LeftNull_ReturnsNull")]
    [TestCase("{ double? a = 5.0; double? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Double_RightNull_ReturnsNull")]
    [TestCase("{ double? a = null; double? b = null; return a * b; }", null,
        TestName = "LiftedArithmetic_Multiply_Double_BothNull_ReturnsNull")]
    [TestCase("{ double? a = 5.0; double? b = 3.0; return a * b; }", 15.0,
        TestName = "LiftedArithmetic_Multiply_Double_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_Double_LeftNull_ReturnsNull")]
    [TestCase("{ double? a = 10.0; double? b = null; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_Double_RightNull_ReturnsNull")]
    [TestCase("{ double? a = null; double? b = null; return a / b; }", null,
        TestName = "LiftedArithmetic_Divide_Double_BothNull_ReturnsNull")]
    [TestCase("{ double? a = 10.0; double? b = 2.0; return a / b; }", 5.0,
        TestName = "LiftedArithmetic_Divide_Double_BothNonNull_ReturnsQuotient")]
    public async Task LiftedArithmetic_Divide_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_Double_LeftNull_ReturnsNull")]
    [TestCase("{ double? a = 10.0; double? b = null; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_Double_RightNull_ReturnsNull")]
    [TestCase("{ double? a = null; double? b = null; return a % b; }", null,
        TestName = "LiftedArithmetic_Modulo_Double_BothNull_ReturnsNull")]
    [TestCase("{ double? a = 10.0; double? b = 3.0; return a % b; }", 1.0,
        TestName = "LiftedArithmetic_Modulo_Double_BothNonNull_ReturnsRemainder")]
    public async Task LiftedArithmetic_Modulo_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region decimal? Arithmetic

    // TestCase attribute does not support decimal literals; use parity-only overload

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a + b; }",
        TestName = "LiftedArithmetic_Add_Decimal_LeftNull_ReturnsNull")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a + b; }",
        TestName = "LiftedArithmetic_Add_Decimal_RightNull_ReturnsNull")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a + b; }",
        TestName = "LiftedArithmetic_Add_Decimal_BothNull_ReturnsNull")]
    [TestCase("{ decimal? a = 5m; decimal? b = 3m; return a + b; }",
        TestName = "LiftedArithmetic_Add_Decimal_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Decimal_LeftNull_ReturnsNull")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Decimal_RightNull_ReturnsNull")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Decimal_BothNull_ReturnsNull")]
    [TestCase("{ decimal? a = 10m; decimal? b = 3m; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Decimal_BothNonNull_ReturnsDifference")]
    public async Task LiftedArithmetic_Subtract_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Decimal_LeftNull_ReturnsNull")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Decimal_RightNull_ReturnsNull")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Decimal_BothNull_ReturnsNull")]
    [TestCase("{ decimal? a = 5m; decimal? b = 3m; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Decimal_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Decimal_LeftNull_ReturnsNull")]
    [TestCase("{ decimal? a = 10m; decimal? b = null; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Decimal_RightNull_ReturnsNull")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Decimal_BothNull_ReturnsNull")]
    [TestCase("{ decimal? a = 10m; decimal? b = 2m; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Decimal_BothNonNull_ReturnsQuotient")]
    public async Task LiftedArithmetic_Divide_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Decimal_LeftNull_ReturnsNull")]
    [TestCase("{ decimal? a = 10m; decimal? b = null; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Decimal_RightNull_ReturnsNull")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Decimal_BothNull_ReturnsNull")]
    [TestCase("{ decimal? a = 10m; decimal? b = 3m; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Decimal_BothNonNull_ReturnsRemainder")]
    public async Task LiftedArithmetic_Modulo_Decimal(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region float? Arithmetic

    // TestCase attribute does not support float literals; use parity-only overload

    [TestCase("{ float? a = null; float? b = 5.0f; return a + b; }",
        TestName = "LiftedArithmetic_Add_Float_LeftNull_ReturnsNull")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a + b; }",
        TestName = "LiftedArithmetic_Add_Float_RightNull_ReturnsNull")]
    [TestCase("{ float? a = null; float? b = null; return a + b; }",
        TestName = "LiftedArithmetic_Add_Float_BothNull_ReturnsNull")]
    [TestCase("{ float? a = 5.0f; float? b = 3.0f; return a + b; }",
        TestName = "LiftedArithmetic_Add_Float_BothNonNull_ReturnsSum")]
    public async Task LiftedArithmetic_Add_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Float_LeftNull_ReturnsNull")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Float_RightNull_ReturnsNull")]
    [TestCase("{ float? a = null; float? b = null; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Float_BothNull_ReturnsNull")]
    [TestCase("{ float? a = 10.0f; float? b = 3.0f; return a - b; }",
        TestName = "LiftedArithmetic_Subtract_Float_BothNonNull_ReturnsDifference")]
    public async Task LiftedArithmetic_Subtract_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Float_LeftNull_ReturnsNull")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Float_RightNull_ReturnsNull")]
    [TestCase("{ float? a = null; float? b = null; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Float_BothNull_ReturnsNull")]
    [TestCase("{ float? a = 5.0f; float? b = 3.0f; return a * b; }",
        TestName = "LiftedArithmetic_Multiply_Float_BothNonNull_ReturnsProduct")]
    public async Task LiftedArithmetic_Multiply_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Float_LeftNull_ReturnsNull")]
    [TestCase("{ float? a = 10.0f; float? b = null; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Float_RightNull_ReturnsNull")]
    [TestCase("{ float? a = null; float? b = null; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Float_BothNull_ReturnsNull")]
    [TestCase("{ float? a = 10.0f; float? b = 2.0f; return a / b; }",
        TestName = "LiftedArithmetic_Divide_Float_BothNonNull_ReturnsQuotient")]
    public async Task LiftedArithmetic_Divide_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Float_LeftNull_ReturnsNull")]
    [TestCase("{ float? a = 10.0f; float? b = null; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Float_RightNull_ReturnsNull")]
    [TestCase("{ float? a = null; float? b = null; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Float_BothNull_ReturnsNull")]
    [TestCase("{ float? a = 10.0f; float? b = 3.0f; return a % b; }",
        TestName = "LiftedArithmetic_Modulo_Float_BothNonNull_ReturnsRemainder")]
    public async Task LiftedArithmetic_Modulo_Float(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Equality Operators

    // "For the equality operators == !=,
    //  a lifted form exists when the operand types are both non-nullable value types
    //  and the result type is bool.
    //  If both operands are null, the result is true (for ==) or false (for !=).
    //  If one operand is null, the result is false (for ==) or true (for !=).
    //  Otherwise, both operands are unwrapped and compared."

    [TestCase("{ int? a = null; int? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_BothNull_ReturnsTrue")]
    [TestCase("{ int? a = null; int? b = 5; return a == b; }", false,
        TestName = "LiftedEquality_Eq_LeftNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_RightNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 5; return a == b; }", true,
        TestName = "LiftedEquality_Eq_EqualValues_ReturnsTrue")]
    [TestCase("{ int? a = 5; int? b = 3; return a == b; }", false,
        TestName = "LiftedEquality_Eq_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Equals(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_BothNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = 5; return a != b; }", true,
        TestName = "LiftedEquality_Neq_LeftNull_ReturnsTrue")]
    [TestCase("{ int? a = 5; int? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_RightNull_ReturnsTrue")]
    [TestCase("{ int? a = 5; int? b = 5; return a != b; }", false,
        TestName = "LiftedEquality_Neq_EqualValues_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 3; return a != b; }", true,
        TestName = "LiftedEquality_Neq_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_NotEquals(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region long? Equality

    [TestCase("{ long? a = null; long? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Long_BothNull_ReturnsTrue")]
    [TestCase("{ long? a = null; long? b = 5L; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Long_LeftNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Long_RightNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = 5L; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Long_EqualValues_ReturnsTrue")]
    [TestCase("{ long? a = 5L; long? b = 3L; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Long_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Equals_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Long_BothNull_ReturnsFalse")]
    [TestCase("{ long? a = null; long? b = 5L; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Long_LeftNull_ReturnsTrue")]
    [TestCase("{ long? a = 5L; long? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Long_RightNull_ReturnsTrue")]
    [TestCase("{ long? a = 5L; long? b = 5L; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Long_EqualValues_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = 3L; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Long_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_NotEquals_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region double? Equality

    [TestCase("{ double? a = null; double? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Double_BothNull_ReturnsTrue")]
    [TestCase("{ double? a = null; double? b = 5.0; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Double_LeftNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Double_RightNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = 5.0; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Double_EqualValues_ReturnsTrue")]
    [TestCase("{ double? a = 5.0; double? b = 3.0; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Double_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Equals_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Double_BothNull_ReturnsFalse")]
    [TestCase("{ double? a = null; double? b = 5.0; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Double_LeftNull_ReturnsTrue")]
    [TestCase("{ double? a = 5.0; double? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Double_RightNull_ReturnsTrue")]
    [TestCase("{ double? a = 5.0; double? b = 5.0; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Double_EqualValues_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = 3.0; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Double_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_NotEquals_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region decimal? Equality

    [TestCase("{ decimal? a = null; decimal? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Decimal_BothNull_ReturnsTrue")]
    [TestCase("{ decimal? a = null; decimal? b = 5m; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Decimal_LeftNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Decimal_RightNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = 5m; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Decimal_EqualValues_ReturnsTrue")]
    [TestCase("{ decimal? a = 5m; decimal? b = 3m; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Decimal_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Equals_Decimal(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ decimal? a = null; decimal? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Decimal_BothNull_ReturnsFalse")]
    [TestCase("{ decimal? a = null; decimal? b = 5m; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Decimal_LeftNull_ReturnsTrue")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Decimal_RightNull_ReturnsTrue")]
    [TestCase("{ decimal? a = 5m; decimal? b = 5m; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Decimal_EqualValues_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = 3m; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Decimal_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_NotEquals_Decimal(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region float? Equality

    [TestCase("{ float? a = null; float? b = null; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Float_BothNull_ReturnsTrue")]
    [TestCase("{ float? a = null; float? b = 5.0f; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Float_LeftNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Float_RightNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = 5.0f; return a == b; }", true,
        TestName = "LiftedEquality_Eq_Float_EqualValues_ReturnsTrue")]
    [TestCase("{ float? a = 5.0f; float? b = 3.0f; return a == b; }", false,
        TestName = "LiftedEquality_Eq_Float_DifferentValues_ReturnsFalse")]
    public async Task LiftedEquality_Equals_Float(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ float? a = null; float? b = null; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Float_BothNull_ReturnsFalse")]
    [TestCase("{ float? a = null; float? b = 5.0f; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Float_LeftNull_ReturnsTrue")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Float_RightNull_ReturnsTrue")]
    [TestCase("{ float? a = 5.0f; float? b = 5.0f; return a != b; }", false,
        TestName = "LiftedEquality_Neq_Float_EqualValues_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = 3.0f; return a != b; }", true,
        TestName = "LiftedEquality_Neq_Float_DifferentValues_ReturnsTrue")]
    public async Task LiftedEquality_NotEquals_Float(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Relational Operators

    // "For the relational operators < > <= >=,
    //  a lifted form exists when the operand types are both non-nullable value types
    //  and the result type is bool.
    //  If one or both operands are null, the result is false."

    [TestCase("{ int? a = null; int? b = 5; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_LeftNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_RightNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_BothNull_ReturnsFalse")]
    [TestCase("{ int? a = 3; int? b = 5; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_LessThan_ReturnsTrue")]
    [TestCase("{ int? a = 5; int? b = 3; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_GreaterThan_ReturnsFalse")]
    public async Task LiftedRelational_LessThan(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_LeftNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_RightNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_BothNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 3; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_LeftNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_RightNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_BothNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 5; return a <= b; }", true,
        TestName = "LiftedRelational_LessOrEqual_Equal_ReturnsTrue")]
    public async Task LiftedRelational_LessOrEqual(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_LeftNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_RightNull_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_BothNull_ReturnsFalse")]
    [TestCase("{ int? a = 5; int? b = 5; return a >= b; }", true,
        TestName = "LiftedRelational_GreaterOrEqual_Equal_ReturnsTrue")]
    public async Task LiftedRelational_GreaterOrEqual(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region long? Relational

    [TestCase("{ long? a = null; long? b = 5L; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Long_LeftNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Long_RightNull_ReturnsFalse")]
    [TestCase("{ long? a = null; long? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Long_BothNull_ReturnsFalse")]
    [TestCase("{ long? a = 3L; long? b = 5L; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_Long_LessThan_ReturnsTrue")]
    [TestCase("{ long? a = 5L; long? b = 3L; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Long_GreaterThan_ReturnsFalse")]
    public async Task LiftedRelational_LessThan_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Long_LeftNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Long_RightNull_ReturnsFalse")]
    [TestCase("{ long? a = null; long? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Long_BothNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = 3L; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_Long_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Long_LeftNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Long_RightNull_ReturnsFalse")]
    [TestCase("{ long? a = null; long? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Long_BothNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = 5L; return a <= b; }", true,
        TestName = "LiftedRelational_LessOrEqual_Long_Equal_ReturnsTrue")]
    public async Task LiftedRelational_LessOrEqual_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ long? a = null; long? b = 5L; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Long_LeftNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Long_RightNull_ReturnsFalse")]
    [TestCase("{ long? a = null; long? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Long_BothNull_ReturnsFalse")]
    [TestCase("{ long? a = 5L; long? b = 5L; return a >= b; }", true,
        TestName = "LiftedRelational_GreaterOrEqual_Long_Equal_ReturnsTrue")]
    public async Task LiftedRelational_GreaterOrEqual_Long(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region double? Relational

    [TestCase("{ double? a = null; double? b = 5.0; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Double_LeftNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Double_RightNull_ReturnsFalse")]
    [TestCase("{ double? a = null; double? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Double_BothNull_ReturnsFalse")]
    [TestCase("{ double? a = 3.0; double? b = 5.0; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_Double_LessThan_ReturnsTrue")]
    [TestCase("{ double? a = 5.0; double? b = 3.0; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Double_GreaterThan_ReturnsFalse")]
    public async Task LiftedRelational_LessThan_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Double_LeftNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Double_RightNull_ReturnsFalse")]
    [TestCase("{ double? a = null; double? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Double_BothNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = 3.0; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_Double_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Double_LeftNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Double_RightNull_ReturnsFalse")]
    [TestCase("{ double? a = null; double? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Double_BothNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = 5.0; return a <= b; }", true,
        TestName = "LiftedRelational_LessOrEqual_Double_Equal_ReturnsTrue")]
    public async Task LiftedRelational_LessOrEqual_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ double? a = null; double? b = 5.0; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Double_LeftNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Double_RightNull_ReturnsFalse")]
    [TestCase("{ double? a = null; double? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Double_BothNull_ReturnsFalse")]
    [TestCase("{ double? a = 5.0; double? b = 5.0; return a >= b; }", true,
        TestName = "LiftedRelational_GreaterOrEqual_Double_Equal_ReturnsTrue")]
    public async Task LiftedRelational_GreaterOrEqual_Double(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region decimal? Relational

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Decimal_LeftNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Decimal_RightNull_ReturnsFalse")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Decimal_BothNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 3m; decimal? b = 5m; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_Decimal_LessThan_ReturnsTrue")]
    [TestCase("{ decimal? a = 5m; decimal? b = 3m; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Decimal_GreaterThan_ReturnsFalse")]
    public async Task LiftedRelational_LessThan_Decimal(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Decimal_LeftNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Decimal_RightNull_ReturnsFalse")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Decimal_BothNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = 3m; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_Decimal_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan_Decimal(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Decimal_LeftNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Decimal_RightNull_ReturnsFalse")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Decimal_BothNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = 5m; return a <= b; }", true,
        TestName = "LiftedRelational_LessOrEqual_Decimal_Equal_ReturnsTrue")]
    public async Task LiftedRelational_LessOrEqual_Decimal(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ decimal? a = null; decimal? b = 5m; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Decimal_LeftNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Decimal_RightNull_ReturnsFalse")]
    [TestCase("{ decimal? a = null; decimal? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Decimal_BothNull_ReturnsFalse")]
    [TestCase("{ decimal? a = 5m; decimal? b = 5m; return a >= b; }", true,
        TestName = "LiftedRelational_GreaterOrEqual_Decimal_Equal_ReturnsTrue")]
    public async Task LiftedRelational_GreaterOrEqual_Decimal(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region float? Relational

    [TestCase("{ float? a = null; float? b = 5.0f; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Float_LeftNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Float_RightNull_ReturnsFalse")]
    [TestCase("{ float? a = null; float? b = null; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Float_BothNull_ReturnsFalse")]
    [TestCase("{ float? a = 3.0f; float? b = 5.0f; return a < b; }", true,
        TestName = "LiftedRelational_LessThan_Float_LessThan_ReturnsTrue")]
    [TestCase("{ float? a = 5.0f; float? b = 3.0f; return a < b; }", false,
        TestName = "LiftedRelational_LessThan_Float_GreaterThan_ReturnsFalse")]
    public async Task LiftedRelational_LessThan_Float(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Float_LeftNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Float_RightNull_ReturnsFalse")]
    [TestCase("{ float? a = null; float? b = null; return a > b; }", false,
        TestName = "LiftedRelational_GreaterThan_Float_BothNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = 3.0f; return a > b; }", true,
        TestName = "LiftedRelational_GreaterThan_Float_GreaterThan_ReturnsTrue")]
    public async Task LiftedRelational_GreaterThan_Float(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Float_LeftNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Float_RightNull_ReturnsFalse")]
    [TestCase("{ float? a = null; float? b = null; return a <= b; }", false,
        TestName = "LiftedRelational_LessOrEqual_Float_BothNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = 5.0f; return a <= b; }", true,
        TestName = "LiftedRelational_LessOrEqual_Float_Equal_ReturnsTrue")]
    public async Task LiftedRelational_LessOrEqual_Float(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ float? a = null; float? b = 5.0f; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Float_LeftNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Float_RightNull_ReturnsFalse")]
    [TestCase("{ float? a = null; float? b = null; return a >= b; }", false,
        TestName = "LiftedRelational_GreaterOrEqual_Float_BothNull_ReturnsFalse")]
    [TestCase("{ float? a = 5.0f; float? b = 5.0f; return a >= b; }", true,
        TestName = "LiftedRelational_GreaterOrEqual_Float_Equal_ReturnsTrue")]
    public async Task LiftedRelational_GreaterOrEqual_Float(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Bitwise Operators

    // Integer bitwise operators follow the same lifted rule as arithmetic:
    // if either operand is null, result is null.

    [TestCase("{ int? a = null; int? b = 5; return a & b; }", null,
        TestName = "LiftedBitwise_And_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a & b; }", null,
        TestName = "LiftedBitwise_And_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a & b; }", null,
        TestName = "LiftedBitwise_And_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = 3; return a & b; }", 1,
        TestName = "LiftedBitwise_And_BothNonNull_ReturnsResult")]
    public async Task LiftedBitwise_And(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a | b; }", null,
        TestName = "LiftedBitwise_Or_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a | b; }", null,
        TestName = "LiftedBitwise_Or_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a | b; }", null,
        TestName = "LiftedBitwise_Or_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = 3; return a | b; }", 7,
        TestName = "LiftedBitwise_Or_BothNonNull_ReturnsResult")]
    public async Task LiftedBitwise_Or(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 5; return a ^ b; }", null,
        TestName = "LiftedBitwise_Xor_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a ^ b; }", null,
        TestName = "LiftedBitwise_Xor_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a ^ b; }", null,
        TestName = "LiftedBitwise_Xor_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = 3; return a ^ b; }", 6,
        TestName = "LiftedBitwise_Xor_BothNonNull_ReturnsResult")]
    public async Task LiftedBitwise_Xor(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Shift Operators

    [TestCase("{ int? a = null; int? b = 2; return a << b; }", null,
        TestName = "LiftedShift_Left_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 1; int? b = null; return a << b; }", null,
        TestName = "LiftedShift_Left_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a << b; }", null,
        TestName = "LiftedShift_Left_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 1; int? b = 2; return a << b; }", 4,
        TestName = "LiftedShift_Left_BothNonNull_ReturnsResult")]
    public async Task LiftedShift_Left(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; int? b = 2; return a >> b; }", null,
        TestName = "LiftedShift_Right_LeftNull_ReturnsNull")]
    [TestCase("{ int? a = 8; int? b = null; return a >> b; }", null,
        TestName = "LiftedShift_Right_RightNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = null; return a >> b; }", null,
        TestName = "LiftedShift_Right_BothNull_ReturnsNull")]
    [TestCase("{ int? a = 8; int? b = 2; return a >> b; }", 2,
        TestName = "LiftedShift_Right_BothNonNull_ReturnsResult")]
    public async Task LiftedShift_Right(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 - Lifted Unary Operators

    // "For the unary operators + ++ - -- ! ~,
    //  a lifted form exists when the operand and result types are both non-nullable value types.
    //  If the operand is null, the result is null."

    [TestCase("{ int? a = null; return -a; }", null,
        TestName = "LiftedUnary_Negate_Null_ReturnsNull")]
    [TestCase("{ int? a = 5; return -a; }", -5,
        TestName = "LiftedUnary_Negate_NonNull_ReturnsNegated")]
    public async Task LiftedUnary_Negate(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; return +a; }", null,
        TestName = "LiftedUnary_Plus_Null_ReturnsNull")]
    [TestCase("{ int? a = 5; return +a; }", 5,
        TestName = "LiftedUnary_Plus_NonNull_ReturnsSame")]
    public async Task LiftedUnary_Plus(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ int? a = null; return ~a; }", null,
        TestName = "LiftedUnary_BitwiseNot_Null_ReturnsNull")]
    [TestCase("{ int? a = 5; return ~a; }", -6,
        TestName = "LiftedUnary_BitwiseNot_NonNull_ReturnsComplement")]
    public async Task LiftedUnary_BitwiseNot(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCase("{ bool? a = null; return !a; }", null,
        TestName = "LiftedUnary_LogicalNot_Null_ReturnsNull")]
    [TestCase("{ bool? a = true; return !a; }", false,
        TestName = "LiftedUnary_LogicalNot_True_ReturnsFalse")]
    [TestCase("{ bool? a = false; return !a; }", true,
        TestName = "LiftedUnary_LogicalNot_False_ReturnsTrue")]
    public async Task LiftedUnary_LogicalNot(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.13.5 - Three-Value Bool? Logic (& and |)

    // ECMA-334 §12.13.5: "The result of x & y is true if both x and y are true.
    //  Otherwise, the result is false if either x or y is false (even if the other
    //  operand is null). Otherwise, the result is null."

    [TestCase("{ bool? a = false; bool? b = null; return a & b; }", false,
        TestName = "BoolThreeValue_And_FalseNull_ReturnsFalse")]
    [TestCase("{ bool? a = null; bool? b = false; return a & b; }", false,
        TestName = "BoolThreeValue_And_NullFalse_ReturnsFalse")]
    [TestCase("{ bool? a = true; bool? b = null; return a & b; }", null,
        TestName = "BoolThreeValue_And_TrueNull_ReturnsNull")]
    [TestCase("{ bool? a = null; bool? b = true; return a & b; }", null,
        TestName = "BoolThreeValue_And_NullTrue_ReturnsNull")]
    [TestCase("{ bool? a = null; bool? b = null; return a & b; }", null,
        TestName = "BoolThreeValue_And_NullNull_ReturnsNull")]
    [TestCase("{ bool? a = true; bool? b = true; return a & b; }", true,
        TestName = "BoolThreeValue_And_TrueTrue_ReturnsTrue")]
    [TestCase("{ bool? a = true; bool? b = false; return a & b; }", false,
        TestName = "BoolThreeValue_And_TrueFalse_ReturnsFalse")]
    [TestCase("{ bool? a = false; bool? b = false; return a & b; }", false,
        TestName = "BoolThreeValue_And_FalseFalse_ReturnsFalse")]
    [TestCase("{ bool? a = false; bool? b = true; return a & b; }", false,
        TestName = "BoolThreeValue_And_FalseTrue_ReturnsFalse")]
    public async Task BoolThreeValue_And(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // ECMA-334 §12.13.5: "The result of x | y is true if either x or y is true
    //  (even if the other operand is null). Otherwise, the result is false if both
    //  x and y are false. Otherwise, the result is null."

    [TestCase("{ bool? a = true; bool? b = null; return a | b; }", true,
        TestName = "BoolThreeValue_Or_TrueNull_ReturnsTrue")]
    [TestCase("{ bool? a = null; bool? b = true; return a | b; }", true,
        TestName = "BoolThreeValue_Or_NullTrue_ReturnsTrue")]
    [TestCase("{ bool? a = false; bool? b = null; return a | b; }", null,
        TestName = "BoolThreeValue_Or_FalseNull_ReturnsNull")]
    [TestCase("{ bool? a = null; bool? b = false; return a | b; }", null,
        TestName = "BoolThreeValue_Or_NullFalse_ReturnsNull")]
    [TestCase("{ bool? a = null; bool? b = null; return a | b; }", null,
        TestName = "BoolThreeValue_Or_NullNull_ReturnsNull")]
    [TestCase("{ bool? a = true; bool? b = true; return a | b; }", true,
        TestName = "BoolThreeValue_Or_TrueTrue_ReturnsTrue")]
    [TestCase("{ bool? a = true; bool? b = false; return a | b; }", true,
        TestName = "BoolThreeValue_Or_TrueFalse_ReturnsTrue")]
    [TestCase("{ bool? a = false; bool? b = false; return a | b; }", false,
        TestName = "BoolThreeValue_Or_FalseFalse_ReturnsFalse")]
    [TestCase("{ bool? a = false; bool? b = true; return a | b; }", true,
        TestName = "BoolThreeValue_Or_FalseTrue_ReturnsTrue")]
    public async Task BoolThreeValue_Or(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // ECMA-334 §12.13.5: bool? XOR - if either operand is null, result is null

    [TestCase("{ bool? a = null; bool? b = null; return a ^ b; }", null,
        TestName = "BoolThreeValue_Xor_NullNull_ReturnsNull")]
    [TestCase("{ bool? a = true; bool? b = null; return a ^ b; }", null,
        TestName = "BoolThreeValue_Xor_TrueNull_ReturnsNull")]
    [TestCase("{ bool? a = null; bool? b = false; return a ^ b; }", null,
        TestName = "BoolThreeValue_Xor_NullFalse_ReturnsNull")]
    [TestCase("{ bool? a = true; bool? b = false; return a ^ b; }", true,
        TestName = "BoolThreeValue_Xor_TrueFalse_ReturnsTrue")]
    [TestCase("{ bool? a = true; bool? b = true; return a ^ b; }", false,
        TestName = "BoolThreeValue_Xor_TrueTrue_ReturnsFalse")]
    [TestCase("{ bool? a = false; bool? b = false; return a ^ b; }", false,
        TestName = "BoolThreeValue_Xor_FalseFalse_ReturnsFalse")]
    [TestCase("{ bool? a = false; bool? b = true; return a ^ b; }", true,
        TestName = "BoolThreeValue_Xor_FalseTrue_ReturnsTrue")]
    [TestCase("{ bool? a = null; bool? b = true; return a ^ b; }", null,
        TestName = "BoolThreeValue_Xor_NullTrue_ReturnsNull")]
    [TestCase("{ bool? a = false; bool? b = null; return a ^ b; }", null,
        TestName = "BoolThreeValue_Xor_FalseNull_ReturnsNull")]
    public async Task BoolThreeValue_Xor(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion
}
