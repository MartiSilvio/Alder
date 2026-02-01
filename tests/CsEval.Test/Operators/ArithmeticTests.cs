namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ArithmeticTests(CompilationMode mode)
{
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
    public async Task Eval_Arithmetic(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }
}
