using CsEval.TestData.Data;

namespace CsEval.Test.Runtime;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class IncrementDecrementTests(CompilationMode mode)
{
    [TestCaseSource(typeof(IncrementDecrementData), nameof(IncrementDecrementData.ValueCases))]
    public async Task IncrementDecrement_Value(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(IncrementDecrementData), nameof(IncrementDecrementData.ParityCases))]
    public async Task IncrementDecrement_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #region Float (Inline -- type assertion)

    [Test]
    public async Task PrefixIncrement_Float_WorksCorrectly()
    {
        var expr = "{ float x = 2.5f; ++x; return x; }";
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(3.5f));
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()));
    }

    #endregion

    #region External Variables (Inline -- SetVariable)

    [Test]
    public async Task Increment_ExternalVariable()
    {
        var variables = new Dictionary<string, object?> { ["counter"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ counter++; return counter; }", variables, 101L, mode);
    }

    [Test]
    public async Task Decrement_ExternalVariable()
    {
        var variables = new Dictionary<string, object?> { ["counter"] = 100L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ counter--; return counter; }", variables, 99L, mode);
    }

    [Test]
    public async Task PrefixIncrement_ExternalVariable_ReturnsNewValue()
    {
        var variables = new Dictionary<string, object?> { ["val"] = 50L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ var captured = ++val; return captured; }", variables, 51L, mode);
    }

    [Test]
    public async Task PostfixIncrement_ExternalVariable_ReturnsOldValue()
    {
        var variables = new Dictionary<string, object?> { ["val"] = 50L };
        await TestHelpers.RunCSharpParityTestAsync(
            "{ var captured = val++; return captured; }", variables, 50L, mode);
    }

    #endregion

    #region Pre-Parsed (Inline -- engine reuse)

    [Test]
    public void IncrementDecrement_PreParsed_CanBeReused()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var expr = engine.Parse(@"
        {
            var x = startVal;
            x++;
            ++x;
            return x;
        }");

        engine.SetVariable("startVal", 0L);
        var result1 = engine.Evaluate(expr);
        Assert.That(result1, Is.EqualTo(2L));

        engine.SetVariable("startVal", 100L);
        var result2 = engine.Evaluate(expr);
        Assert.That(result2, Is.EqualTo(102L));
    }

    #endregion
}
