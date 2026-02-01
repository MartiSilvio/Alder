namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class LiteralTests(CompilationMode mode)
{
    [TestCase("42", 42, TestName = "Integer")]
    [TestCase("3.14", 3.14, TestName = "Double")]
    [TestCase("\"hello\"", "hello", TestName = "String")]
    [TestCase("true", true, TestName = "True")]
    [TestCase("false", false, TestName = "False")]
    public async Task Eval_Literal(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    [Test]
    public async Task Eval_Null_ReturnsNull()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        const string expr = "null";
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
        Assert.That(result, Is.Null);
        Assert.That(result, Is.EqualTo(csharpResult));
    }
}
