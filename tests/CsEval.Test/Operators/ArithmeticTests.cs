namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.9 — Arithmetic operators, §12.4.2 — Operator precedence.
/// Tests addition, subtraction, multiplication, division, modulo, unary operators,
/// lifted nullable arithmetic (§12.4.8), and char arithmetic (§12.4.7).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ArithmeticTests(CompilationMode mode)
{
    #region ECMA-334 §12.9 — Binary Arithmetic Operators

    [TestCase("1 + 2", 3, TestName = "Add_Integers")]
    [TestCase("1.5 + 2.5", 4.0, TestName = "Add_Doubles")]
    [TestCase("5 - 3", 2, TestName = "Subtract_Integers")]
    [TestCase("3 * 4", 12, TestName = "Multiply_Integers")]
    [TestCase("10 / 4", 2, TestName = "Divide_IntegerTruncates")]
    [TestCase("10.0 / 4.0", 2.5, TestName = "Divide_DoublesPreservesFraction")]
    [TestCase("10 % 3", 1, TestName = "Modulo_Integers")]
    [TestCase("1 + 2 * 3", 7, TestName = "Precedence_MultiplyBeforeAdd")]
    [TestCase("(1 + 2) * 3", 9, TestName = "Parentheses_OverridePrecedence")]
    [TestCase("-5", -5, TestName = "Negate_Integer")]
    [TestCase("-3.14", -3.14, TestName = "Negate_Double")]
    [TestCase("+5", 5, TestName = "UnaryPlus_Integer")]
    [TestCase("+3.14", 3.14, TestName = "UnaryPlus_Double")]
    [TestCase("+(-5)", -5, TestName = "UnaryPlus_NegativeInteger")]
    public async Task Eval_Arithmetic(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.8 — Lifted Nullable Arithmetic

    [TestCase("{ int? a = 5; int? b = null; return a + b; }", null, TestName = "NullableAdd_ReturnsNull")]
    [TestCase("{ int? a = 5; int? b = null; return a > b; }", false, TestName = "NullableComparison_ReturnsFalse")]
    [TestCase("{ int? a = null; int? b = null; return a + b; }", null, TestName = "NullableAdd_BothNull_ReturnsNull")]
    [TestCase("{ int? a = null; int? b = 5; return a < b; }", false, TestName = "NullableLessThan_ReturnsFalse")]
    public async Task Eval_NullableArithmetic(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.7 — Char Arithmetic (Numeric Promotion)

    // ECMA-334 §12.4.7.3: char operands are promoted to int for binary arithmetic.
    [TestCase("'A' + 1", 66, TestName = "CharPlusInt_NumericResult")]
    [TestCase("'A' - 'A'", 0, TestName = "CharMinusChar_IntResult")]
    [TestCase("'B' - 'A'", 1, TestName = "CharMinusChar_Difference")]
    [TestCase("1 + 'A'", 66, TestName = "IntPlusChar_NumericResult")]
    public async Task Eval_CharArithmetic(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.9.2, §12.9.3 — Unary Plus and Minus

    [TestCase("+5", 5, TestName = "UnaryPlus_Int")]
    [TestCase("+5L", 5L, TestName = "UnaryPlus_Long")]
    [TestCase("+5.0", 5.0, TestName = "UnaryPlus_Double")]
    [TestCase("+5.0f", 5.0f, TestName = "UnaryPlus_Float")]
    [TestCase("+-5", -5, TestName = "UnaryPlus_NegativeValue")]
    [TestCase("-+5", -5, TestName = "UnaryMinus_AfterPlus")]
    [TestCase("-(-5)", 5, TestName = "DoubleNegation_Parenthesized")]
    [TestCase("+(+5)", 5, TestName = "DoublePlus_Parenthesized")]
    public async Task UnaryOperators_Arithmetic(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.2 — Logical vs Bitwise Precedence

    [TestCase("true & true && false", false, TestName = "Precedence_BitwiseVsLogicalAnd")]
    [TestCase("false | false || true", true, TestName = "Precedence_BitwiseVsLogicalOr")]
    public async Task Precedence_BitwiseVsLogical(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 — Decimal/Float Incompatibility and Division by Zero

    // ECMA-334 §12.4.7.3 Rule 1: "a binding-time error occurs if the other operand is of type float or double."
    [TestCase("10 / 0", TestName = "DivideByZero_Int")]
    [TestCase("10 % 0", TestName = "ModuloByZero_Int")]
    [TestCase("1.0m + 1.0", TestName = "DecimalPlusDouble")]
    [TestCase("1.0m - 1.0", TestName = "DecimalMinusDouble")]
    [TestCase("1.0m * 1.0", TestName = "DecimalTimesDouble")]
    [TestCase("1.0m / 1.0", TestName = "DecimalDivideDouble")]
    [TestCase("1.0 + 1.0m", TestName = "DoublePlusDecimal")]
    [TestCase("1.0 - 1.0m", TestName = "DoubleMinusDecimal")]
    [TestCase("1.0 * 1.0m", TestName = "DoubleTimesDecimal")]
    [TestCase("1.0 / 1.0m", TestName = "DoubleDivideDecimal")]
    public async Task Eval_Arithmetic_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #endregion
}
