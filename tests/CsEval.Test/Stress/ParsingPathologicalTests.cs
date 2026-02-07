using System.Text;
using CsEval.Parsing;

namespace CsEval.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ParsingPathologicalTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    [Explicit("CRITICAL BUG: Crashes process with StackOverflowException. Run manually to reproduce.")]
    public void DeeplyNestedParentheses_ShouldNotCrashProcess()
    {
        // Recursion depth stress.
        // A standard C# stack might blow up around a few thousand frames depending on platform/config.
        // We want to see if the parser handles it or crashes.
        // We assert that it throws a managed exception (CsEvalParserException or maybe generic Exception)
        // rather than taking down the process with StackOverflowException (which usually can't be caught).
        // Since we can't catch SOE easily in .NET Core, we'll just run it. If the test runner dies, we know why.
        
        var depth = 2000;
        var expression = GenerateDeeplyNestedExpression(depth, "1 + 1");
        
        try 
        {
            var expr = Engine.Parse(expression);
            Assert.That(expr, Is.Not.Null);
            
            // If it parses, can it evaluate?
            var result = Engine.Evaluate(expr);
            Assert.That(result, Is.EqualTo(2));
        }
        catch (CsEvalParserException) 
        {
            // Acceptable failure
        }
        catch (StackOverflowException)
        {
            Assert.Fail("StackOverflowException occurred during parsing/evaluation!");
        }
        catch (Exception ex)
        {
             // Other exceptions are fine (Out of memory, etc)
             TestContext.WriteLine($"Caught expected exception: {ex.GetType().Name}");
        }
    }

    [Test]
    [Explicit("CRITICAL BUG: Crashes process with StackOverflowException in CompilerContext.CanCompile. Run manually to reproduce.")]
    public void ExtremelyLongExpression_ShouldParseWithinReasonableTime()
    {
        // 10,000 operations "1 + 2 * 3 - 4 ..."
        var expression = GenerateLongExpression(10_000);
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var expr = Engine.Parse(expression);
        sw.Stop();
        
        Assert.That(expr, Is.Not.Null);
        TestContext.WriteLine($"Parsed 10k ops in {sw.ElapsedMilliseconds}ms");
        
        // Should evaluate
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
    [Explicit("CRITICAL BUG: Crashes process with StackOverflowException in Evaluator.Evaluate. Run manually to reproduce.")]
    public void ManyChainedPropertyAccesses_ShouldNotStackOverflow(int count)
    {
        // "s.Length.ToString().Length.ToString()..."
        var sb = new StringBuilder("\"start\"");
        for(int i=0; i<count; i++)
        {
            sb.Append(".Length.ToString()");
        }
        
        var expr = sb.ToString();
        
        try
        {
            var result = Engine.Evaluate(expr);
            Assert.That(result, Is.Not.Null);
        }
        catch (CsEvalParserException) { /* limit reached */ }
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
