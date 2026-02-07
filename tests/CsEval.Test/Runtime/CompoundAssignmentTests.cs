using CsEval.TestData.Data;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CompoundAssignmentTests(CompilationMode mode)
{
    [TestCaseSource(typeof(CompoundAssignmentData), nameof(CompoundAssignmentData.ValueCases))]
    public async Task CompoundAssignment_Value(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CompoundAssignmentData), nameof(CompoundAssignmentData.ParityCases))]
    public async Task CompoundAssignment_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #region Float (Inline -- tolerance assertion)

    [Test]
    public async Task CompoundAssignment_Float_WorksCorrectly()
    {
        // Float needs Within() tolerance -- use parity-only form
        var expr = "{ float x = 3.14f; x += 2.86f; return x; }";
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(6.0f).Within(0.001f));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    #endregion

    #region Type Error (Inline -- error assertion)

    [Test]
    public void CompoundAssignment_IntPlusDouble_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var x = 10;
            x += 5.5;
            return x;
        }"));
    }

    #endregion

    #region ForEach (Inline -- CsEval-specific syntax)

    [Test]
    public void CompoundAssignment_InForEachLoop_Accumulates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in [1, 2, 3, 4, 5]) {
                sum += n;
            }
            return sum;
        }");

        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region With Expressions (Inline -- single-test methods)

    [Test]
    public async Task CompoundAssignment_WithTernaryRHS_WorksCorrectly()
    {
        var expr = "{ var x = 100; x += true ? 50 : 25; return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, 150, mode);
    }

    [Test]
    public async Task CompoundAssignment_WithNullCoalesceRHS_WorksCorrectly()
    {
        var expr = """
        {
            int? maybeNull = null;
            var x = 10;
            x += maybeNull ?? 5;
            return x;
        }
        """;
        await TestHelpers.RunCSharpParityTestAsync(expr, 15, mode);
    }

    #endregion

    #region External Variables (Inline -- SetVariable)

    [Test]
    public async Task CompoundAssignment_ExternalVariable_Updates()
    {
        var variables = new Dictionary<string, object?> { ["counter"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ counter += 50; return counter; }", variables, 150L, mode);
    }

    [Test]
    public async Task CompoundAssignment_CombiningExternalAndLocal()
    {
        var variables = new Dictionary<string, object?> { ["baseValue"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var local = 10L;
                local += baseValue;
                baseValue += local;
                return baseValue;
            }
            """, variables, 210L, mode);
    }

    [Test]
    public async Task CompoundAssignment_ConditionalPathSelection()
    {
        var variables = new Dictionary<string, object?> { ["shouldAdd"] = true };
        await TestHelpers.RunCSharpParityTestAsync("""
            {
                var amount = 0;
                if (shouldAdd) {
                    amount += 10;
                } else {
                    amount -= 10;
                }
                return amount;
            }
            """, variables, 10, mode);
    }

    #endregion

    #region Error Cases (Inline -- exception assertions)

    [Test]
    public void CompoundAssignment_UndefinedVariable_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                undefinedVar += 10;
                return undefinedVar;
            }"));
    }

    [Test]
    public void CompoundAssignment_IncompatibleTypes_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<CsEvalException>(() =>
            engine.Evaluate(@"
            {
                var s = ""hello"";
                s -= ""world"";
                return s;
            }"));
    }

    [Test]
    public void CompoundAssignment_DivisionByZero_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<DivideByZeroException>(() =>
            engine.Evaluate(@"
            {
                var x = 10;
                x /= 0;
                return x;
            }"));
    }

    [Test]
    public void CompoundAssignment_ModuloByZero_ThrowsException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });

        Assert.Throws<DivideByZeroException>(() =>
            engine.Evaluate(@"
            {
                var x = 10;
                x %= 0;
                return x;
            }"));
    }

    #endregion

    #region Pre-Parsed (Inline -- engine reuse)

    [Test]
    public void CompoundAssignment_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x += increment;
            return x;
        }");

        engine.SetVariable("startVal", 10L);
        engine.SetVariable("increment", 5L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(15L));

        engine.SetVariable("startVal", 100L);
        engine.SetVariable("increment", 50L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(150L));
    }

    #endregion
}
