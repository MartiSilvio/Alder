namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LogicalTests(CompilationMode mode)
{
    [TestCase("true && true", true, TestName = "And_TrueTrue")]
    [TestCase("true && false", false, TestName = "And_TrueFalse")]
    [TestCase("false && true", false, TestName = "And_FalseTrue")]
    [TestCase("false && false", false, TestName = "And_FalseFalse")]
    [TestCase("true || false", true, TestName = "Or_TrueFalse")]
    [TestCase("false || true", true, TestName = "Or_FalseTrue")]
    [TestCase("false || false", false, TestName = "Or_FalseFalse")]
    [TestCase("true || true", true, TestName = "Or_TrueTrue")]
    [TestCase("!true", false, TestName = "Not_True")]
    [TestCase("!false", true, TestName = "Not_False")]
    public async Task Eval_Logical(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }
}
