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
