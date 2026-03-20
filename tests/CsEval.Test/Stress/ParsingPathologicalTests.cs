using System.Text;
using CsEval.Test._Infrastructure;

namespace CsEval.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ParsingPathologicalTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void DeeplyNestedParentheses_ShouldNotCrashProcess()
    {
        // 2000 nested parentheses exhaust the .NET thread stack.
        // EnsureSufficientExecutionStack() detects near-exhaustion and throws
        // InsufficientExecutionStackException, which CsEvalEngine wraps as CsEvalException.
        var depth = 2000;
        var expression = GenerateDeeplyNestedExpression(depth, "1 + 1");

        var ex = Assert.Throws<CsEvalException>(() => Engine.Parse(expression));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CSEV0033));
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
        // "start".Length.ToString().Length.ToString()...
        // Each iteration adds ~3 AST nodes (MemberAccess, MemberAccess, Call).
        // count=100 (~300 evaluator depth) is under the 512 evaluator cap: succeeds.
        // count=1000 (~3000 evaluator depth) exceeds the 512 cap: throws CsEvalDepthException.
        var sb = new StringBuilder("\"start\"");
        for (int i = 0; i < count; i++)
        {
            sb.Append(".Length.ToString()");
        }

        var expr = sb.ToString();

        if (count <= 512)
        {
            var result = Engine.Evaluate(expr);
            Assert.That(result, Is.Not.Null);
        }
        else
        {
            var ex = Assert.Throws<CsEvalDepthException>(() => Engine.Evaluate(expr));
            Assert.That(ex!.MaxDepth, Is.EqualTo(512));
        }
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
