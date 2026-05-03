using System.Text;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ParsingPathologicalTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void DeeplyNestedParentheses_ShouldNotCrashProcess()
    {
        // 2000 nested parens exceed the parser's recursion depth limit.
        // The parser throws InsufficientExecutionStackException, which the engine
        // converts to a managed AlderException — no native crash.
        var depth = 2000;
        var expression = GenerateDeeplyNestedExpression(depth, "1 + 1");

        var ex = Assert.Throws<AlderException>(() => Engine.Parse(expression));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8078));
    }

    [Test]
    public void ExtremelyLongExpression_EvaluatesWithIterativeLeftSpine()
    {
        var expression = GenerateLongExpression(10_000);

        var expr = Engine.Parse(expression);
        Assert.That(expr, Is.Not.Null);

        var result = Engine.Evaluate(expr);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void HugeStringLiteral_ShouldParseAndEvaluate()
    {
        var hugeString = new string('a', 1_000_000);
        var expression = $"\"{hugeString}\"";

        var result = Engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(hugeString));
    }

    [Test]
    public void UnclosedStringLiteral_Huge_ShouldFailGracefully()
    {
        var hugeString = new string('a', 1_000_000);
        var expression = $"\"{hugeString}";

        Assert.Throws<AlderException>(() => Engine.Parse(expression));
    }

    [Test]
    public void UnicodeIdentifier_ShouldEvaluateRegisteredVariable()
    {
        var variableName = "变_variable";
        Engine.SetVariable(variableName, 42);

        var result = Engine.Evaluate(variableName);
        Assert.That(result, Is.EqualTo(42));
    }

    [TestCase(100)]
    [TestCase(1000)]
    [CancelAfter(10_000)]
    public void ManyChainedPropertyAccesses_ShouldNotStackOverflow(int count)
    {
        // Postfix call-member-access chains are iterativized across the binder,
        // rewriter, interpreter, and emitter — no recursion regardless of chain length.
        var sb = new StringBuilder("\"start\"");
        for (int i = 0; i < count; i++)
            sb.Append(".Length.ToString()");

        var result = Engine.Evaluate(sb.ToString());
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void RandomFuzzing_ShouldNotCrash()
    {
        for (int i = 0; i < 1000; i++)
        {
            var fuzz = GenerateRandomString(Random.Next(10, 200), true, true);
            try
            {
                Engine.Parse(fuzz);
            }
            catch (AlderException) { }
            catch (Exception)
            {
                Assert.Fail($"Parser threw an unexpected exception for fuzz input at index {i}.");
            }
        }
    }
}
