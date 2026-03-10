namespace CsEval.Test.Compliance;

/// <summary>
/// Verifies CsEval Standard mode matches Roslyn across 6 semantic areas:
/// numeric coercion, IComparable comparisons, cross-type equality,
/// string concatenation, bool enforcement, and assignment targets.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class SemanticLeniencyTests(CompilationMode mode)
{
    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        LanguageMode = LanguageMode.Standard
    };

    #region Helpers

    private async Task AssertMatchesRoslyn(string expr)
    {
        var roslynResult = await TestHelpers.EvaluateCSharpAsync(expr);

        var engine = new CsEvalEngine(Options);
        var csEvalResult = engine.Evaluate(expr);

        Assert.That(csEvalResult, Is.EqualTo(roslynResult),
            $"Value mismatch for: {expr}\n  CsEval={csEvalResult} ({csEvalResult?.GetType()?.Name})\n  Roslyn={roslynResult} ({roslynResult?.GetType()?.Name})");
        Assert.That(csEvalResult?.GetType(), Is.EqualTo(roslynResult?.GetType()),
            $"Type mismatch for: {expr}\n  CsEval type={csEvalResult?.GetType()?.Name}\n  Roslyn type={roslynResult?.GetType()?.Name}");
    }

    private async Task AssertBothThrow(string expr)
    {
        Exception? roslynEx = null;
        try { await TestHelpers.EvaluateCSharpAsync(expr); }
        catch (Exception ex) { roslynEx = ex; }

        Assert.That(roslynEx, Is.Not.Null,
            $"Expected Roslyn to reject: {expr}");

        var engine = new CsEvalEngine(Options);
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"CsEval accepted but Roslyn rejected: {expr}");
    }

    #endregion

    #region Area 1: Method Argument Coercion (CoerceNumeric)

    [Test]
    public async Task Area1_MathMax_IntInt()
    {
        await AssertMatchesRoslyn("Math.Max(1, 2)");
    }

    [Test]
    public async Task Area1_MathMax_LongLong()
    {
        await AssertMatchesRoslyn("Math.Max(1L, 2L)");
    }

    [Test]
    public async Task Area1_MathMax_DoubleDouble()
    {
        await AssertMatchesRoslyn("Math.Max(1.0, 2.0)");
    }

    [Test]
    public async Task Area1_MathAbs_Double()
    {
        await AssertMatchesRoslyn("Math.Abs(-5.0)");
    }

    [Test]
    public async Task Area1_MathRound_DoubleInt()
    {
        await AssertMatchesRoslyn("Math.Round(3.14159, 2)");
    }

    #endregion

    #region Area 2: Comparison Operators (IComparable)

    [Test]
    public async Task Area2_DateTime_GreaterThan()
    {
        await AssertMatchesRoslyn("DateTime.Now > DateTime.MinValue");
    }

    [Test]
    public async Task Area2_TimeSpan_GreaterThan()
    {
        await AssertMatchesRoslyn("TimeSpan.FromHours(1) > TimeSpan.FromMinutes(30)");
    }

    [Test]
    public async Task Area2_TimeSpan_LessThan()
    {
        await AssertMatchesRoslyn("TimeSpan.FromSeconds(1) < TimeSpan.FromMinutes(1)");
    }

    [Test]
    public async Task Area2_DateTime_Equality()
    {
        await AssertMatchesRoslyn("DateTime.MinValue == DateTime.MinValue");
    }

    #endregion

    #region Area 3: Cross-Type Equality

    [Test]
    public async Task Area3_IntEqualsLong()
    {
        await AssertMatchesRoslyn("1 == 1L");
    }

    [Test]
    public async Task Area3_LongEqualsInt()
    {
        await AssertMatchesRoslyn("1L == 1");
    }

    [Test]
    public async Task Area3_UintEqualsInt()
    {
        await AssertMatchesRoslyn("1u == 1");
    }

    [Test]
    public async Task Area3_ByteEqualsShort()
    {
        await AssertMatchesRoslyn("(byte)1 == (short)1");
    }

    [Test]
    public async Task Area3_IntNotEqualsLong()
    {
        await AssertMatchesRoslyn("1 != 2L");
    }

    [Test]
    public async Task Area3_IntLessThanLong()
    {
        await AssertMatchesRoslyn("1 < 2L");
    }

    #endregion

    #region Area 4: String Concatenation

    [Test]
    public async Task Area4_StringPlusInt()
    {
        await AssertMatchesRoslyn("\"hello\" + 42");
    }

    [Test]
    public async Task Area4_StringPlusNull()
    {
        await AssertMatchesRoslyn("\"hello\" + null");
    }

    [Test]
    public async Task Area4_NullPlusString()
    {
        await AssertMatchesRoslyn("null + \"world\"");
    }

    [Test]
    public async Task Area4_StringPlusBool()
    {
        await AssertMatchesRoslyn("\"value: \" + true");
    }

    [Test]
    public async Task Area4_EmptyStringPlusDouble()
    {
        await AssertMatchesRoslyn("\"\" + 3.14");
    }

    #endregion

    #region Area 5: Logical Operators Bool Enforcement

    [Test]
    public async Task Area5_LogicalNot_True()
    {
        await AssertMatchesRoslyn("!true");
    }

    [Test]
    public async Task Area5_LogicalAnd()
    {
        await AssertMatchesRoslyn("true && false");
    }

    [Test]
    public async Task Area5_LogicalOr()
    {
        await AssertMatchesRoslyn("true || false");
    }

    [Test]
    public async Task Area5_LogicalNot_OnInt_BothThrow()
    {
        // !(1) should fail in both -- C# does not have implicit int->bool
        await AssertBothThrow("!(1)");
    }

    #endregion

    #region Area 6: Assignment Target Validation

    [Test]
    public async Task Area6_InvalidAssignmentTarget_BothThrow()
    {
        // (1 + 2) = 5 should fail in both -- not a valid assignment target
        await AssertBothThrow("(1 + 2) = 5");
    }

    #endregion
}
