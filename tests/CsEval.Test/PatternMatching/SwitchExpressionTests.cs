namespace CsEval.Test.PatternMatching;

// Engine-only: All tests use SetVariable, verify SwitchExpressionException (error tests),
// or test Roslyn-rejected patterns (CS8510 unreachable pattern arms, variable scoping)

/// <summary>
/// ECMA-334 §12.8.21 -- Switch expressions.
/// Tests switch expression parsing and evaluation, constant/type/relational/property pattern arms,
/// when guards (section 12.8.21.3), discard catch-all (section 11.2.8),
/// and SwitchExpressionException for non-exhaustive matches.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class SwitchExpressionTests(CompilationMode mode)
{
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
}
