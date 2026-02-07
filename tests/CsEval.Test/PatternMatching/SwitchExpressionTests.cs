using CsEval.TestData.Data;

namespace CsEval.Test.PatternMatching;

/// <summary>
/// ECMA-334 §12.8.21 -- Switch expressions.
/// Tests switch expression parsing and evaluation, constant/type/relational/property pattern arms,
/// when guards (section 12.8.21.3), discard catch-all (section 11.2.8),
/// and SwitchExpressionException for non-exhaustive matches.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SwitchExpressionTests(CompilationMode mode)
{
    #region ECMA-334 §12.8.21 -- Basic Switch Expression with Constant Arms

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.ConstantArmCases))]
    public async Task SwitchExpression_ConstantArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 + section 11.2.2 -- Switch with Type Patterns

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.TypePatternCases))]
    public async Task SwitchExpression_TypePatternArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 + section 11.2.5/11.2.6 -- Switch with Relational and Logical Patterns

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.RelationalArmCases))]
    public async Task SwitchExpression_RelationalArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21.3 -- When Guards

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.WhenGuardCases))]
    public async Task SwitchExpression_WhenGuards(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // When guard failure falls through to next arm
    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.WhenGuardFallthroughCases))]
    public async Task SwitchExpression_WhenGuard_Fallthrough(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // When guard with string member access
    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.WhenGuardStringLengthCases))]
    public async Task SwitchExpression_WhenGuard_StringLength(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.8 -- Discard Pattern in Switch Arms

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.DiscardArmCases))]
    public async Task SwitchExpression_DiscardArm(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 + section 11.2.7 -- Property Patterns in Switch Arms

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.PropertyPatternArmCases))]
    public async Task SwitchExpression_PropertyPatternArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 -- SwitchExpressionException for Non-Exhaustive Match

    [Test]
    public void SwitchExpression_NoMatch_ThrowsSwitchExpressionException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object)99);
        Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(
            () => engine.Evaluate("x switch { 1 => \"one\" }"));
    }

    [Test]
    public void SwitchExpression_NoMatch_NullValue_ThrowsSwitchExpressionException()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object?)null);
        Assert.Throws<System.Runtime.CompilerServices.SwitchExpressionException>(
            () => engine.Evaluate("x switch { 1 => \"one\", \"hello\" => \"two\" }"));
    }

    #endregion

    #region ECMA-334 §12.8.21 -- First-Match Semantics

    // When multiple arms could match, the first one wins
    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.FirstMatchCases))]
    public async Task SwitchExpression_FirstMatchSemantics(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Roslyn rejects unreachable pattern arms (CS8510), so this is engine-only.
    // Verifies that when object precedes string, the object arm wins (first-match semantics).
    [Test]
    public void SwitchExpression_FirstMatch_ObjectBeforeString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object)"hello");
        var result = engine.Evaluate("x switch { object => \"object\", string => \"string\", _ => \"other\" }");
        Assert.That(result, Is.EqualTo("object"));
    }

    #endregion

    #region ECMA-334 §12.8.21 -- Switch Expression in Larger Expressions

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.InExpressionCases))]
    public async Task SwitchExpression_InLargerExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 -- Variable Scoping in Switch Arms

    // Pattern variables in one arm should not leak to other arms
    [Test]
    public void SwitchExpression_PatternVariableNotLeaking()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (object)"hello");
        // The variable 's' from the first arm should not be accessible outside
        var result = engine.Evaluate("x switch { string s => s.Length, _ => -1 }");
        Assert.That(result, Is.EqualTo(5));
        // 's' should not be accessible in the engine context after switch
        Assert.Throws<CsEvalException>(() => engine.Evaluate("s"));
    }

    #endregion

    #region ECMA-334 §12.8.21 -- Null Handling in Switch

    [TestCaseSource(typeof(SwitchExpressionData), nameof(SwitchExpressionData.NullHandlingCases))]
    public async Task SwitchExpression_NullHandling(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion
}
