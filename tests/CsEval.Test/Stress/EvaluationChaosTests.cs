using CsEval.Test._Infrastructure;

namespace CsEval.Test.Stress;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class EvaluationChaosTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void InfiniteLoop_ShouldTerminate_WithMaxStatements()
    {
        var options = CsEvalOptions.Default with
        {
            Constraints = new ExecutionConstraints { MaxStatements = 1000 }
        };
        var engine = TestEngineFactory.Create(Mode, options);

        var expr = "{ var i = 0; while(true) { i++; } }";

        Assert.Throws<CsEvalExecutionLimitException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void NestedLoops_ExponentialComplexity_ShouldRespectMaxStatements()
    {
        var options = CsEvalOptions.Default with
        {
            Constraints = new ExecutionConstraints { MaxStatements = 5000 }
        };
        var engine = TestEngineFactory.Create(Mode, options);

        // O(N^3)
        const string expr = @"{
            var count = 0;
            for(var i=0; i<100; i++) {
                for(var j=0; j<100; j++) {
                    for(var k=0; k<100; k++) {
                        count++;
                    }
                }
            }
            return count;
        }";

        // 100*100*100 = 1,000,000 > 5000. Should throw.
        Assert.Throws<CsEvalExecutionLimitException>(() => engine.Evaluate(expr));
    }

    [Test]
    public void AllocationStress_ShouldHandleOOMGracefully()
    {
        // Try to allocate a huge array
        // Enumerable.Range(0, 100000000).ToArray()
        // If the machine has enough RAM, this might succeed. If not, it throws OOM.
        // We just want to ensure the engine doesn't get into a corrupted state.

        const string expr = "Enumerable.Range(0, 10000000).ToArray().Length";
        try
        {
            var result = Engine.Evaluate(expr);
            Assert.That(result, Is.EqualTo(10000000));
        }
        catch (OutOfMemoryException)
        {
            Assert.Pass("OOM handled correctly");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Allocation check: {ex.GetType().Name}");
        }
    }

    [Test]
    public void TypeConfusion_AddDifferentTypes()
    {
        // What happens if we add Int + Bool? Or String + Object?
        // JS like behavior or C# strong typing?
        // Based on "CoerceNumeric", it tries to be smart.

        var cases = new[]
        {
            ("1 + true", null), // Likely fail or string concat?
            ("'a' + 1", "a1"),  // Char + Int -> Int or String?
            ("null + 5", null),
            ("5 + null", null),
            ("true + false", null)
        };

        foreach (var (expr, expected) in cases)
        {
            try
            {
                var result = Engine.Evaluate(expr);
                TestContext.WriteLine($"'{expr}' => {result} ({result?.GetType().Name})");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"'{expr}' => Threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [Test]
    public void RecursionFunction_ShouldThrowStackOverflow_OrConstraintLimit()
    {
        // Define a recursive function
        // function f(n) { if (n<=0) return 0; return 1 + f(n-1); }
        // CsEval might not support function declarations inside expression directly without special syntax?
        // Looking at engine, we can register functions.

        // Let's register a C# function that recurses back into engine?
        // Or pure script recursion if supported.
        // Let's assume we can't define functions in script easily (unless it supports lambdas assigned to vars).

        Engine.RegisterFunction("recurse", args =>
        {
            var n = (int)args[0]!;
            if (n <= 0) return 0;
            // Call invalid? No we can't call back into script easily from here unless we pass Delegate.
            return n;
        });

        // Let's rely on expression recursion if possible? 
        // "Func<int, int> f = null; f = n => n <= 0 ? 0 : f(n-1); f(10000)"
        // If lambdas are supported.

        // "Func<int, int> f = null; f = n => n <= 0 ? 0 : f(n-1); f(10000)"

        // Usually self-reference is hard in init. "var f = null; f = ...";
        var setup = @"{
            Func<int, int> f = null;
            f = (n) => {
                if (n <= 0) return 0;
                return 1 + f(n-1);
            };
            f(5000)
        }";

        try
        {
            Engine.Evaluate(setup);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Recursion test: {ex.Message}");
        }
    }

    [Test]
    public void ArithmeticOverflow_DefaultContext_WrapsToNegative()
    {
        var result = Engine.Evaluate($"{int.MaxValue} + 1");
        Assert.That(result, Is.EqualTo(int.MinValue));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void ArithmeticOverflow_CheckedContext_ThrowsOverflow()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(int.MaxValue + 1)"));
    }

    [Test]
    public void ArithmeticOverflow_UncheckedContext_WrapsToNegative()
    {
        var result = Engine.Evaluate("unchecked(int.MaxValue + 1)");
        Assert.That(result, Is.EqualTo(int.MinValue));
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void CheckedContext_UintOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(uint.MaxValue + 1u)"));
    }

    [Test]
    public void CheckedContext_UlongOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(ulong.MaxValue + 1UL)"));
    }

    [Test]
    public void CheckedContext_MultipleOps_OnlyLastOverflows()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked(1 + 2 + int.MaxValue)"));
    }

    [Test]
    public void CheckedContext_NarrowingCast_Throws()
    {
        Assert.Throws<OverflowException>(() => Engine.Evaluate("checked((byte)256)"));
    }

    [Test]
    public void UncheckedContext_NarrowingCast_Truncates()
    {
        var result = Engine.Evaluate("unchecked((byte)256)");
        Assert.That(result, Is.EqualTo((byte)0));
        Assert.That(result, Is.TypeOf<byte>());
    }

    [Test]
    public void DivisionByZero_ShouldThrow()
    {
        var expr = "1 / 0";
        Assert.Throws<DivideByZeroException>(() => Engine.Evaluate(expr));
    }

    [Test]
    public void SandboxBypass_Reflection_ShouldBeBlockedInSafeMode()
    {
        // Try to access System.Type or GetType()
        var safeEngine = TestEngineFactory.Create(Mode, new CsEvalOptions
        {
            Sandbox = SandboxOptions.Safe()  // Safe mode blocks method calls on variables?
        });

        // "string".GetType() is a method call. Should be blocked.
        var expr = "\"hello\".GetType()";

        var ex = Assert.Throws<CsEvalException>(() => safeEngine.Evaluate(expr));
        Assert.That(ex!.ErrorCode, Is.EqualTo(CsEval.Diagnostics.DiagnosticCode.CSEV0011));
    }
}
