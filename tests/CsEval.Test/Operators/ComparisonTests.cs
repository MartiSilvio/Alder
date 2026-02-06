namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §12.12 — Relational and type-testing operators.
/// Tests equality (§12.12.7), relational comparisons (§12.12.2),
/// numeric promotion in mixed-type comparisons (§12.4.7.3), and null equality (§12.12.8).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ComparisonTests(CompilationMode mode)
{
    #region ECMA-334 §12.12.7 — Equality Operators

    [TestCase("1 == 1", true, TestName = "Equal_SameInt")]
    [TestCase("1 == 2", false, TestName = "Equal_DifferentInt")]
    [TestCase(@"""a"" == ""a""", true, TestName = "Equal_SameString")]
    [TestCase("1 != 2", true, TestName = "NotEqual_DifferentInt")]
    [TestCase("1 != 1", false, TestName = "NotEqual_SameInt")]
    public async Task Eval_Equality(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.12.2 — Integer Relational Operators

    [TestCase("1 < 2", true, TestName = "LessThan_True")]
    [TestCase("2 < 1", false, TestName = "LessThan_False")]
    [TestCase("2 > 1", true, TestName = "GreaterThan_True")]
    [TestCase("1 > 2", false, TestName = "GreaterThan_False")]
    [TestCase("1 <= 2", true, TestName = "LessOrEqual_LessThan")]
    [TestCase("2 <= 2", true, TestName = "LessOrEqual_Equal")]
    [TestCase("3 <= 2", false, TestName = "LessOrEqual_GreaterThan")]
    [TestCase("2 >= 1", true, TestName = "GreaterOrEqual_GreaterThan")]
    [TestCase("2 >= 2", true, TestName = "GreaterOrEqual_Equal")]
    [TestCase("1 >= 2", false, TestName = "GreaterOrEqual_LessThan")]
    public async Task Eval_Relational(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 — Mixed Type Comparisons (Numeric Promotion)

    [TestCase("1 < 2L", true, TestName = "LessThan_IntLong")]
    [TestCase("1L < 2", true, TestName = "LessThan_LongInt")]
    [TestCase("1 < 2.0", true, TestName = "LessThan_IntDouble")]
    [TestCase("1.0f < 2.0", true, TestName = "LessThan_FloatDouble")]
    public async Task Eval_MixedTypeComparison(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.12.8 — Reference Equality and Null

    [TestCase("null == null", true, TestName = "Equal_NullNull")]
    [TestCase("null != null", false, TestName = "NotEqual_NullNull")]
    public async Task Eval_NullEquality(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.12 — Char and Bool Comparisons

    // ECMA-334 §12.4.7.3: char operands are promoted to int for comparison
    [TestCase("'a' < 'b'", true, TestName = "LessThan_CharChar")]
    [TestCase("'Z' < 'a'", true, TestName = "LessThan_UpperLowerChar")]
    [TestCase("'a' == 'a'", true, TestName = "Equal_CharChar")]
    // ECMA-334 §12.12.7: Boolean equality (no ordering operators for bool)
    [TestCase("true == true", true, TestName = "Equal_BoolBool")]
    [TestCase("true != false", true, TestName = "NotEqual_BoolBool")]
    [TestCase("(1 < 2) == true", true, TestName = "ComparisonResult_IsBool")]
    public async Task Eval_CharAndBoolComparison(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.4.7.3 — Decimal/Float Comparison Errors

    // ECMA-334 §12.4.7.3 Rule 1: "a binding-time error occurs if the other operand is of type float or double."
    [TestCase("1.0m < 1.0", TestName = "LessThan_DecimalDouble")]
    [TestCase("1.0m > 1.0", TestName = "GreaterThan_DecimalDouble")]
    [TestCase("1.0m <= 1.0", TestName = "LessOrEqual_DecimalDouble")]
    [TestCase("1.0m >= 1.0", TestName = "GreaterOrEqual_DecimalDouble")]
    [TestCase("1.0m == 1.0", TestName = "Equal_DecimalDouble")]
    [TestCase("1.0m != 1.0", TestName = "NotEqual_DecimalDouble")]
    [TestCase("1.0 < 1.0m", TestName = "LessThan_DoubleDecimal")]
    [TestCase("1.0 > 1.0m", TestName = "GreaterThan_DoubleDecimal")]
    public async Task Eval_Comparison_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    #endregion
}
