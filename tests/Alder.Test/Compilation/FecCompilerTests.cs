using Alder.Test._Infrastructure;

namespace Alder.Test.Compilation;

/// <summary>
/// Verifies that FastExpressionCompiler can be plugged in via IExpressionCompiler
/// and produces correct results for expression shapes it reliably handles.
///
/// Intentionally does NOT run the full parity suite — FEC has known gaps with
/// complex Block/ArrayAccess/loop patterns that cause InvalidProgramException
/// or native segfaults (exit 139). Those are FEC bugs, not Alder bugs.
/// </summary>
[TestFixture]
public class FecCompilerTests
{
    private static AlderEngine Engine(CompilationMode mode = CompilationMode.Compiled) =>
        TestEngineFactory.Create(mode, o => o.ExpressionCompiler = new FastExpressionCompilerAdapter());

    [TestCase("1 + 2", ExpectedResult = 3)]
    [TestCase("10 * 3 - 5", ExpectedResult = 25)]
    [TestCase("true && false", ExpectedResult = false)]
    [TestCase("true || false", ExpectedResult = true)]
    [TestCase("!true", ExpectedResult = false)]
    [TestCase("2 > 1 ? \"yes\" : \"no\"", ExpectedResult = "yes")]
    public object? Arithmetic(string expr) => Engine().Evaluate(expr);

    [TestCase("\"hello\".Length", ExpectedResult = 5)]
    [TestCase("\"hello\" + \" world\"", ExpectedResult = "hello world")]
    [TestCase("\"HELLO\".ToLower()", ExpectedResult = "hello")]
    public object? StringOps(string expr) => Engine().Evaluate(expr);

    [TestCase("var x = 1; var y = 2; x + y", ExpectedResult = 3)]
    [TestCase("var s = \"ab\"; s + s", ExpectedResult = "abab")]
    public object? LocalVariables(string expr) => Engine().Evaluate(expr);

    [Test]
    public void NullCoalescing()
    {
        var engine = Engine();
        engine.SetVariable("x", (object?)null);
        Assert.That(engine.Evaluate("x ?? 42"), Is.EqualTo(42));
    }

    [Test]
    public void PropertyAccess()
    {
        var engine = Engine();
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        Assert.That(engine.Evaluate("items.Count"), Is.EqualTo(3));
    }
}
