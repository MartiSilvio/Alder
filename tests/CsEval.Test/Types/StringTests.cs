namespace CsEval.Test.Types;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class StringTests(CompilationMode mode)
{
    [TestCase(@"""Hello"" + "" "" + ""World""", "Hello World", TestName = "Concatenation")]
    [TestCase(@"""HELLO"".ToLower()", "hello", TestName = "ToLower")]
    [TestCase(@"""hello"".ToUpper()", "HELLO", TestName = "ToUpper")]
    [TestCase(@"""hello"".Length", 5, TestName = "Length")]
    [TestCase(@"""  hello  "".Trim()", "hello", TestName = "Trim")]
    [TestCase(@"""hello world"".Contains(""world"")", true, TestName = "Contains_True")]
    [TestCase(@"""hello world"".Contains(""foo"")", false, TestName = "Contains_False")]
    [TestCase(@"""hello world"".StartsWith(""hello"")", true, TestName = "StartsWith_True")]
    [TestCase(@"""hello world"".StartsWith(""world"")", false, TestName = "StartsWith_False")]
    [TestCase(@"""hello world"".EndsWith(""world"")", true, TestName = "EndsWith_True")]
    [TestCase(@"""hello world"".EndsWith(""hello"")", false, TestName = "EndsWith_False")]
    [TestCase(@"""hello world"".Replace(""world"", ""there"")", "hello there", TestName = "Replace")]
    [TestCase(@"""hello world"".Substring(0, 5)", "hello", TestName = "Substring")]
    [TestCase(@"""hello world"".IndexOf(""o"")", 4, TestName = "IndexOf")]
    [TestCase(@""""".Length", 0, TestName = "EmptyString_Length")]
    [TestCase(@"""a"" + ""b"" + ""c""", "abc", TestName = "MultiConcatenation")]
    public async Task Eval_StringOperations(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    // Interpolation Tests

    [Test]
    public void Eval_InterpolatedString_Basic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("name", "World");
        Assert.That(engine.Evaluate("$\"Hello {name}!\""), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_InterpolatedString_WithExpression()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5);
        engine.SetVariable("y", 3);
        Assert.That(engine.Evaluate("$\"Sum: {x + y}\""), Is.EqualTo("Sum: 8"));
    }

    [Test]
    public void Eval_InterpolatedString_Multiple()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("first", "John");
        engine.SetVariable("last", "Doe");
        Assert.That(engine.Evaluate("$\"{first} {last}\""), Is.EqualTo("John Doe"));
    }
}
