namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ComparisonTests(CompilationMode mode)
{
    [TestCase("1 == 1", true, TestName = "Equal_SameInt")]
    [TestCase("1 == 2", false, TestName = "Equal_DifferentInt")]
    [TestCase(@"""a"" == ""a""", true, TestName = "Equal_SameString")]
    [TestCase("1 != 2", true, TestName = "NotEqual_DifferentInt")]
    [TestCase("1 != 1", false, TestName = "NotEqual_SameInt")]
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
    // ECMA-334 §12.12 - Mixed type comparisons with numeric promotion
    [TestCase("1 < 2L", true, TestName = "LessThan_IntLong")]
    [TestCase("1L < 2", true, TestName = "LessThan_LongInt")]
    [TestCase("1 < 2.0", true, TestName = "LessThan_IntDouble")]
    [TestCase("1.0f < 2.0", true, TestName = "LessThan_FloatDouble")]
    // ECMA-334 §12.12.8 - Reference equality
    [TestCase("null == null", true, TestName = "Equal_NullNull")]
    [TestCase("null != null", false, TestName = "NotEqual_NullNull")]
    // ECMA-334 §12.12 - Char comparison (lexicographic)
    [TestCase("'a' < 'b'", true, TestName = "LessThan_CharChar")]
    [TestCase("'Z' < 'a'", true, TestName = "LessThan_UpperLowerChar")]
    [TestCase("'a' == 'a'", true, TestName = "Equal_CharChar")]
    // ECMA-334 §12.12 - Boolean equality only (no ordering)
    [TestCase("true == true", true, TestName = "Equal_BoolBool")]
    [TestCase("true != false", true, TestName = "NotEqual_BoolBool")]
    // Edge case: comparison result type is always bool
    [TestCase("(1 < 2) == true", true, TestName = "ComparisonResult_IsBool")]
    public async Task Eval_Comparison(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

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
}
