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
    [TestCase("0xFF", 255, TestName = "HexLiteral")]
    [TestCase("0x1A", 26, TestName = "HexLiteral2")]
    [TestCase("0b1010", 10, TestName = "BinaryLiteral")]
    [TestCase("0b11111111", 255, TestName = "BinaryLiteral2")]
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

    [TestCase(@"""\0""", "\0", TestName = "EscapeNull")]
    [TestCase(@"""\a""", "\a", TestName = "EscapeAlert")]
    [TestCase(@"""\b""", "\b", TestName = "EscapeBackspace")]
    [TestCase(@"""\f""", "\f", TestName = "EscapeFormFeed")]
    [TestCase(@"""\v""", "\v", TestName = "EscapeVerticalTab")]
    public async Task Eval_EscapeSequence_MatchesCSharp(string expr, string expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("0xFF + 1", 256, TestName = "HexArithmetic")]
    [TestCase("0b1010 * 2", 20, TestName = "BinaryArithmetic")]
    [TestCase("0xFF == 255", true, TestName = "HexComparison")]
    [TestCase("0b1010 == 10", true, TestName = "BinaryComparison")]
    public async Task Eval_HexBinaryArithmetic_MatchesCSharp(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }

    [TestCase("'a'", 'a', TestName = "CharLiteralA")]
    [TestCase("'Z'", 'Z', TestName = "CharLiteralZ")]
    [TestCase("'0'", '0', TestName = "CharDigit")]
    [TestCase(@"'\n'", '\n', TestName = "CharEscapeNewline")]
    [TestCase(@"'\t'", '\t', TestName = "CharEscapeTab")]
    public async Task Eval_CharLiteral_MatchesCSharp(string expr, char expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(char)), $"Type should be char for: {expr}");
    }

    [TestCase("'a' == 'a'", true, TestName = "CharComparison")]
    [TestCase("'a' < 'b'", true, TestName = "CharLessThan")]
    [TestCase("'a' != 'b'", true, TestName = "CharNotEqual")]
    public async Task Eval_CharOperations_MatchesCSharp(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
    }
}
