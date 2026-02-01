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
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }
}
