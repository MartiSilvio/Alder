using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.9 -- Arithmetic operators, S12.4.2 -- Operator precedence.
/// Test data for addition, subtraction, multiplication, division, modulo, unary operators,
/// lifted nullable arithmetic (S12.4.8), and char arithmetic (S12.4.7).
/// Shared across compiler backends.
/// </summary>
public static class ArithmeticData
{
    /// <summary>
    /// Value-producing arithmetic expressions with expected results.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.9 -- Binary Arithmetic Operators
        new("1 + 2", 3) { TestName = "Add_Integers" },
        new("1.5 + 2.5", 4.0) { TestName = "Add_Doubles" },
        new("5 - 3", 2) { TestName = "Subtract_Integers" },
        new("3 * 4", 12) { TestName = "Multiply_Integers" },
        new("10 / 4", 2) { TestName = "Divide_IntegerTruncates" },
        new("10.0 / 4.0", 2.5) { TestName = "Divide_DoublesPreservesFraction" },
        new("10 % 3", 1) { TestName = "Modulo_Integers" },
        new("1 + 2 * 3", 7) { TestName = "Precedence_MultiplyBeforeAdd" },
        new("(1 + 2) * 3", 9) { TestName = "Parentheses_OverridePrecedence" },
        new("-5", -5) { TestName = "Negate_Integer" },
        new("-3.14", -3.14) { TestName = "Negate_Double" },
        new("+5", 5) { TestName = "UnaryPlus_Integer" },
        new("+3.14", 3.14) { TestName = "UnaryPlus_Double" },
        new("+(-5)", -5) { TestName = "UnaryPlus_NegativeInteger" },

        // ECMA-334 S12.4.8 -- Lifted Nullable Arithmetic
        new("{ int? a = 5; int? b = null; return a + b; }", null) { TestName = "NullableAdd_ReturnsNull" },
        new("{ int? a = 5; int? b = null; return a > b; }", false) { TestName = "NullableComparison_ReturnsFalse" },
        new("{ int? a = null; int? b = null; return a + b; }", null) { TestName = "NullableAdd_BothNull_ReturnsNull" },
        new("{ int? a = null; int? b = 5; return a < b; }", false) { TestName = "NullableLessThan_ReturnsFalse" },

        // ECMA-334 S12.4.7 -- Char Arithmetic (Numeric Promotion)
        // S12.4.7.3: char operands are promoted to int for binary arithmetic.
        new("'A' + 1", 66) { TestName = "CharPlusInt_NumericResult" },
        new("'A' - 'A'", 0) { TestName = "CharMinusChar_IntResult" },
        new("'B' - 'A'", 1) { TestName = "CharMinusChar_Difference" },
        new("1 + 'A'", 66) { TestName = "IntPlusChar_NumericResult" },

        // ECMA-334 S12.9.2, S12.9.3 -- Unary Plus and Minus
        new("+5", 5) { TestName = "UnaryPlus_Int" },
        new("+5L", 5L) { TestName = "UnaryPlus_Long" },
        new("+5.0", 5.0) { TestName = "UnaryPlus_Double" },
        new("+5.0f", 5.0f) { TestName = "UnaryPlus_Float" },
        new("+-5", -5) { TestName = "UnaryPlus_NegativeValue" },
        new("-+5", -5) { TestName = "UnaryMinus_AfterPlus" },
        new("-(-5)", 5) { TestName = "DoubleNegation_Parenthesized" },
        new("+(+5)", 5) { TestName = "DoublePlus_Parenthesized" },

        // ECMA-334 S12.4.2 -- Logical vs Bitwise Precedence
        new("true & true && false", false) { TestName = "Precedence_BitwiseVsLogicalAnd" },
        new("false | false || true", true) { TestName = "Precedence_BitwiseVsLogicalOr" },
    ];

    /// <summary>
    /// Arithmetic expressions that should throw exceptions.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        // ECMA-334 S12.4.7.3 -- Decimal/Float Incompatibility and Division by Zero
        // S12.4.7.3 Rule 1: "a binding-time error occurs if the other operand is of type float or double."
        new("10 / 0") { TestName = "DivideByZero_Int" },
        new("10 % 0") { TestName = "ModuloByZero_Int" },
        new("1.0m + 1.0") { TestName = "DecimalPlusDouble" },
        new("1.0m - 1.0") { TestName = "DecimalMinusDouble" },
        new("1.0m * 1.0") { TestName = "DecimalTimesDouble" },
        new("1.0m / 1.0") { TestName = "DecimalDivideDouble" },
        new("1.0 + 1.0m") { TestName = "DoublePlusDecimal" },
        new("1.0 - 1.0m") { TestName = "DoubleMinusDecimal" },
        new("1.0 * 1.0m") { TestName = "DoubleTimesDecimal" },
        new("1.0 / 1.0m") { TestName = "DoubleDivideDecimal" },
    ];
}
