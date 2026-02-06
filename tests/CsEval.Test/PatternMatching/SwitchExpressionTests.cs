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

    [TestCase("{ object x = 1; return x switch { 1 => \"one\", 2 => \"two\", _ => \"other\" }; }", TestName = "SwitchExpr_ConstantArm_MatchFirst")]
    [TestCase("{ object x = 2; return x switch { 1 => \"one\", 2 => \"two\", _ => \"other\" }; }", TestName = "SwitchExpr_ConstantArm_MatchSecond")]
    [TestCase("{ object x = 99; return x switch { 1 => \"one\", 2 => \"two\", _ => \"other\" }; }", TestName = "SwitchExpr_ConstantArm_MatchDiscard")]
    [TestCase("{ object x = \"hello\"; return x switch { \"hello\" => 1, \"world\" => 2, _ => 0 }; }", TestName = "SwitchExpr_StringArms")]
    [TestCase("{ object x = \"world\"; return x switch { \"hello\" => 1, \"world\" => 2, _ => 0 }; }", TestName = "SwitchExpr_StringArms_Second")]
    [TestCase("{ object x = \"other\"; return x switch { \"hello\" => 1, \"world\" => 2, _ => 0 }; }", TestName = "SwitchExpr_StringArms_Default")]
    [TestCase("{ object x = true; return x switch { true => \"yes\", false => \"no\", _ => \"unknown\" }; }", TestName = "SwitchExpr_BoolArms_True")]
    [TestCase("{ object x = false; return x switch { true => \"yes\", false => \"no\", _ => \"unknown\" }; }", TestName = "SwitchExpr_BoolArms_False")]
    public async Task SwitchExpression_ConstantArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 + section 11.2.2 -- Switch with Type Patterns

    [TestCase("{ object x = 5; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }", TestName = "SwitchExpr_TypePattern_Int")]
    [TestCase("{ object x = \"hello\"; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }", TestName = "SwitchExpr_TypePattern_String")]
    [TestCase("{ object x = 3.14; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }", TestName = "SwitchExpr_TypePattern_Default")]
    public async Task SwitchExpression_TypePatternArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 + section 11.2.5/11.2.6 -- Switch with Relational and Logical Patterns

    [TestCase("{ object x = 150; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }", TestName = "SwitchExpr_Relational_High")]
    [TestCase("{ object x = 75; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }", TestName = "SwitchExpr_Relational_Medium")]
    [TestCase("{ object x = 25; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }", TestName = "SwitchExpr_Relational_Low")]
    [TestCase("{ object x = -5; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }", TestName = "SwitchExpr_Relational_Negative")]
    [TestCase("{ object x = 100; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }", TestName = "SwitchExpr_Relational_Boundary100")]
    [TestCase("{ object x = 0; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }", TestName = "SwitchExpr_Relational_Boundary0")]
    public async Task SwitchExpression_RelationalArms(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21.3 -- When Guards

    [TestCase("{ object x = 5; return x switch { int n when n > 0 => \"positive\", int n when n < 0 => \"negative\", _ => \"zero\" }; }", TestName = "SwitchExpr_WhenGuard_Positive")]
    [TestCase("{ object x = -3; return x switch { int n when n > 0 => \"positive\", int n when n < 0 => \"negative\", _ => \"zero\" }; }", TestName = "SwitchExpr_WhenGuard_Negative")]
    [TestCase("{ object x = 0; return x switch { int n when n > 0 => \"positive\", int n when n < 0 => \"negative\", _ => \"zero\" }; }", TestName = "SwitchExpr_WhenGuard_Zero")]
    public async Task SwitchExpression_WhenGuards(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // When guard failure falls through to next arm
    [TestCase("{ object x = 50; return x switch { int n when n > 100 => \"big\", int n => \"small\", _ => \"not int\" }; }", TestName = "SwitchExpr_WhenGuard_Fallthrough")]
    public async Task SwitchExpression_WhenGuard_Fallthrough(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // When guard with string member access
    [TestCase("{ object x = \"hello\"; return x switch { string s when s.Length > 3 => \"long\", string s => \"short\", _ => \"not string\" }; }", TestName = "SwitchExpr_WhenGuard_StringLength")]
    [TestCase("{ object x = \"hi\"; return x switch { string s when s.Length > 3 => \"long\", string s => \"short\", _ => \"not string\" }; }", TestName = "SwitchExpr_WhenGuard_StringLength_Short")]
    [TestCase("{ object x = 42; return x switch { string s when s.Length > 3 => \"long\", string s => \"short\", _ => \"not string\" }; }", TestName = "SwitchExpr_WhenGuard_StringLength_NotString")]
    public async Task SwitchExpression_WhenGuard_StringLength(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.8 -- Discard Pattern in Switch Arms

    [TestCase("{ object x = \"anything\"; return x switch { _ => \"default\" }; }", TestName = "SwitchExpr_DiscardOnly")]
    [TestCase("{ object x = null; return x switch { _ => \"default\" }; }", TestName = "SwitchExpr_DiscardOnly_Null")]
    [TestCase("{ object x = 42; return x switch { _ => \"default\" }; }", TestName = "SwitchExpr_DiscardOnly_Int")]
    public async Task SwitchExpression_DiscardArm(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §12.8.21 + section 11.2.7 -- Property Patterns in Switch Arms

    [TestCase("{ object x = \"\"; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }", TestName = "SwitchExpr_PropertyPattern_Empty")]
    [TestCase("{ object x = \"hello\"; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }", TestName = "SwitchExpr_PropertyPattern_HasContent")]
    [TestCase("{ object x = null; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }", TestName = "SwitchExpr_PropertyPattern_Null")]
    [TestCase("{ object x = 42; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }", TestName = "SwitchExpr_PropertyPattern_NotString")]
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
    [TestCase("{ object x = 42; return x switch { int => \"int\", object => \"object\", _ => \"other\" }; }", TestName = "SwitchExpr_FirstMatch_IntBeforeObject")]
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

    [TestCase("{ object x = 1; return (x switch { 1 => 10, _ => 0 }) + 5; }", TestName = "SwitchExpr_InExpression_Add")]
    [TestCase("{ object x = 2; return (x switch { 1 => 10, _ => 0 }) + 5; }", TestName = "SwitchExpr_InExpression_Default")]
    [TestCase("{ object x = 3; return (x switch { 3 => \"hello\", _ => \"world\" }).Length; }", TestName = "SwitchExpr_InExpression_MemberAccess")]
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

    [TestCase("{ object x = null; return x switch { null => \"null\", _ => \"not null\" }; }", TestName = "SwitchExpr_NullArm_Match")]
    [TestCase("{ object x = 42; return x switch { null => \"null\", _ => \"not null\" }; }", TestName = "SwitchExpr_NullArm_NoMatch")]
    [TestCase("{ object x = null; return x switch { string s => \"string\", null => \"null\", _ => \"other\" }; }", TestName = "SwitchExpr_NullArm_AfterTypePattern")]
    public async Task SwitchExpression_NullHandling(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion
}
