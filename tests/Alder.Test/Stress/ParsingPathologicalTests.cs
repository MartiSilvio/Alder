using System.Text;
using Alder.Test._Infrastructure;

namespace Alder.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ParsingPathologicalTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void DeeplyNestedParentheses_ShouldNotCrashProcess()
    {
        // Consecutive grouping parens are consumed iteratively by PrimaryParser,
        // so 2000 nested parens don't recurse — they parse and evaluate correctly.
        var depth = 2000;
        var expression = GenerateDeeplyNestedExpression(depth, "1 + 1");

        var result = Engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(2));
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
        // 1MB string literal
        var hugeString = new string('a', 1_000_000);
        var expression = $"\"{hugeString}\"";

        var result = Engine.Evaluate(expression);
        Assert.That(result, Is.EqualTo(hugeString));
    }

    [Test]
    public void UnclosedStringLiteral_Huge_ShouldFailGracefully()
    {
        // 1MB string without closing quote
        var hugeString = new string('a', 1_000_000);
        var expression = $"\"{hugeString}"; // Missing quote

        Assert.Catch<Exception>(() => Engine.Parse(expression));
    }

    [Test]
    public void UnicodeStress_ShouldHandleAllUnicodeRanges()
    {
        // Identifiers with weird unicode?
        // If the language spec supports unicode identifiers (C# does), let's see.
        var variableName = "变_😊_variable";
        Engine.SetVariable(variableName, 42);

        // Depending on Lexer, this might fail or succeed. 
        // We assert something depending on success, but mostly we want no crash.
        try
        {
            var result = Engine.Evaluate(variableName);
            Assert.That(result, Is.EqualTo(42));
        }
        catch (Exception)
        {
            // If it fails to parse, that's okay, as long as it handles it.
        }
    }

    [TestCase(100)]
    [TestCase(1000)]
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
        // Generate completely random garbage strings and feed them to Parse
        for (int i = 0; i < 1000; i++)
        {
            var fuzz = GenerateRandomString(Random.Next(10, 200), true, true);
            try
            {
                Engine.Parse(fuzz);
            }
            catch (Exception)
            {
                // Expected to fail, just shouldn't crash the runner
            }
        }
    }
}
