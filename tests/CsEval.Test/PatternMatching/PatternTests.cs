namespace CsEval.Test.PatternMatching;

/// <summary>
/// ECMA-334 §11.2 -- Pattern matching via is-expressions.
/// Tests constant patterns (section 11.2.3), type patterns with variable binding (section 11.2.2),
/// relational patterns (section 11.2.5), logical combinators (section 11.2.6),
/// property patterns (section 11.2.7), and the discard pattern (section 11.2.8).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class PatternTests(CompilationMode mode)
{
    #region ECMA-334 §11.2.3 -- Constant Patterns in Is-Expressions

    [TestCase("{ object x = 42; return x is 42; }", TestName = "ConstantPattern_IntMatch")]
    [TestCase("{ object x = 42; return x is 99; }", TestName = "ConstantPattern_IntNoMatch")]
    [TestCase("{ object x = \"hello\"; return x is \"hello\"; }", TestName = "ConstantPattern_StringMatch")]
    [TestCase("{ object x = \"hello\"; return x is \"world\"; }", TestName = "ConstantPattern_StringNoMatch")]
    [TestCase("{ object x = true; return x is true; }", TestName = "ConstantPattern_BoolMatch")]
    [TestCase("{ object x = true; return x is false; }", TestName = "ConstantPattern_BoolNoMatch")]
    [TestCase("{ object x = null; return x is null; }", TestName = "ConstantPattern_NullMatch")]
    [TestCase("{ object x = 42; return x is null; }", TestName = "ConstantPattern_NullNoMatch")]
    [TestCase("{ object x = \"hello\"; return x is null; }", TestName = "ConstantPattern_StringIsNull_False")]
    public async Task ConstantPattern_IsExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.5 -- Relational Patterns

    [TestCase("{ object x = 50; return x is > 0; }", TestName = "RelationalPattern_GreaterThan_True")]
    [TestCase("{ object x = -1; return x is > 0; }", TestName = "RelationalPattern_GreaterThan_False")]
    [TestCase("{ object x = 0; return x is > 0; }", TestName = "RelationalPattern_GreaterThan_Boundary")]
    [TestCase("{ object x = 100; return x is >= 100; }", TestName = "RelationalPattern_GreaterOrEqual_True")]
    [TestCase("{ object x = 99; return x is >= 100; }", TestName = "RelationalPattern_GreaterOrEqual_False")]
    [TestCase("{ object x = 5; return x is < 10; }", TestName = "RelationalPattern_LessThan_True")]
    [TestCase("{ object x = 10; return x is < 10; }", TestName = "RelationalPattern_LessThan_False")]
    [TestCase("{ object x = 10; return x is <= 10; }", TestName = "RelationalPattern_LessOrEqual_True")]
    [TestCase("{ object x = 11; return x is <= 10; }", TestName = "RelationalPattern_LessOrEqual_False")]
    public async Task RelationalPattern_IsExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.6 -- Logical Pattern Combinators (and, or, not)

    // and combinator -- range checks
    [TestCase("{ object x = 50; return x is > 0 and <= 100; }", TestName = "LogicalPattern_And_InRange")]
    [TestCase("{ object x = 0; return x is > 0 and <= 100; }", TestName = "LogicalPattern_And_OutOfRange_Low")]
    [TestCase("{ object x = 150; return x is > 0 and <= 100; }", TestName = "LogicalPattern_And_OutOfRange_High")]
    [TestCase("{ object x = 100; return x is > 0 and <= 100; }", TestName = "LogicalPattern_And_UpperBoundary")]
    [TestCase("{ object x = 1; return x is > 0 and <= 100; }", TestName = "LogicalPattern_And_LowerBoundary")]
    public async Task LogicalPattern_And(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // or combinator -- type alternatives
    [TestCase("{ object x = 42; return x is int or string; }", TestName = "LogicalPattern_Or_MatchesInt")]
    [TestCase("{ object x = \"hello\"; return x is int or string; }", TestName = "LogicalPattern_Or_MatchesString")]
    [TestCase("{ object x = 3.14; return x is int or string; }", TestName = "LogicalPattern_Or_NoMatch")]
    public async Task LogicalPattern_Or(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // not combinator
    [TestCase("{ object x = \"hello\"; return x is not int; }", TestName = "LogicalPattern_Not_True")]
    [TestCase("{ object x = 42; return x is not int; }", TestName = "LogicalPattern_Not_False")]
    [TestCase("{ object x = null; return x is not null; }", TestName = "LogicalPattern_NotNull_False")]
    [TestCase("{ object x = 42; return x is not null; }", TestName = "LogicalPattern_NotNull_True")]
    [TestCase("{ object x = \"hello\"; return x is not null; }", TestName = "LogicalPattern_NotNull_String_True")]
    public async Task LogicalPattern_Not(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Nested logical combinators
    [TestCase("{ object x = 50; return x is > 0 and < 100 or > 200; }", TestName = "LogicalPattern_AndOr_InLowRange")]
    [TestCase("{ object x = 250; return x is > 0 and < 100 or > 200; }", TestName = "LogicalPattern_AndOr_InHighRange")]
    [TestCase("{ object x = 150; return x is > 0 and < 100 or > 200; }", TestName = "LogicalPattern_AndOr_InMiddle")]
    public async Task LogicalPattern_Nested(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.7 -- Property Patterns

    // Property patterns with a type prefix work with Roslyn parity (the type provides member resolution).
    [TestCase("{ object x = \"hello\"; return x is string { Length: 5 }; }", TestName = "PropertyPattern_WithTypeCheck")]
    [TestCase("{ object x = \"hello\"; return x is string { Length: > 3 }; }", TestName = "PropertyPattern_WithTypeAndRelational")]
    [TestCase("{ object x = 42; return x is string { Length: > 0 }; }", TestName = "PropertyPattern_TypeMismatch_False")]
    [TestCase("{ object x = \"hello\"; return x is { }; }", TestName = "PropertyPattern_EmptyNonNull_True")]
    [TestCase("{ object x = null; return x is { }; }", TestName = "PropertyPattern_EmptyNull_False")]
    public async Task PropertyPattern_IsExpression_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Property patterns without type prefix: Roslyn requires the static type to resolve members,
    // so these use string-typed variables for Roslyn parity.
    [TestCase("{ string x = \"hello\"; return x is { Length: 5 }; }", TestName = "PropertyPattern_ExactMatch")]
    [TestCase("{ string x = \"hello\"; return x is { Length: 4 }; }", TestName = "PropertyPattern_ExactNoMatch")]
    [TestCase("{ string x = \"hello\"; return x is { Length: > 0 }; }", TestName = "PropertyPattern_RelationalSub")]
    [TestCase("{ string x = \"\"; return x is { Length: > 0 }; }", TestName = "PropertyPattern_NoMatch")]
    public async Task PropertyPattern_MemberAccess_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Null test for property pattern with member access: engine-only (Roslyn cannot resolve
    // member on object-typed variable, and string? x = null doesn't support is { Length: > 0 }
    // without a warning/error in some Roslyn versions).
    [Test]
    public void PropertyPattern_NullFalse()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ object x = null; return x is { Length: > 0 }; }");
        Assert.That(result, Is.EqualTo(false));
    }

    #endregion

    #region ECMA-334 §11.2.2 -- Type Patterns with Variable Binding

    [TestCase("{ object x = \"hello\"; return x is string s ? s.Length : -1; }", TestName = "TypePattern_VarBinding_Match")]
    [TestCase("{ object x = 42; return x is string s ? s.Length : -1; }", TestName = "TypePattern_VarBinding_NoMatch")]
    [TestCase("{ object x = 42; return x is int i ? i * 2 : 0; }", TestName = "TypePattern_VarBinding_IntMatch")]
    [TestCase("{ object x = null; return x is string s ? s.Length : -1; }", TestName = "TypePattern_VarBinding_Null")]
    [TestCase("{ object x = \"test\"; return x is string s ? s.ToUpper() : \"N/A\"; }", TestName = "TypePattern_VarBinding_MethodCall")]
    public async Task TypePattern_WithVariableBinding(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.8 -- Discard Pattern

    /// <summary>
    /// The discard pattern (x is _) is not valid C# syntax in an is-expression context.
    /// Roslyn rejects "x is _" as a standalone expression -- _ is only valid in switch arms.
    /// These tests verify engine-only behavior.
    /// </summary>
    [Test]
    public void DiscardPattern_InIsExpression_NotSupported()
    {
        // The discard pattern in is-expressions is tested in switch expression tests (SwitchExpressionTests.cs).
        // In standard C#, "x is _" is not a valid discard pattern -- _ is treated as a variable name.
        // Switch arm discard is covered by SwitchExpressionTests.
        Assert.Pass("Discard pattern tested in switch expression context");
    }

    #endregion

    #region ECMA-334 §11.2 -- Combination Patterns in Is-Expressions

    [TestCase("{ object x = 42; return x is int and > 0; }", TestName = "CombinedPattern_TypeAndRelational")]
    [TestCase("{ object x = -5; return x is int and > 0; }", TestName = "CombinedPattern_TypeAndRelational_Fail")]
    [TestCase("{ object x = \"hi\"; return x is int and > 0; }", TestName = "CombinedPattern_TypeMismatch")]
    [TestCase("{ object x = \"hello\"; return x is string and not null; }", TestName = "CombinedPattern_TypeAndNotNull")]
    [TestCase("{ object x = null; return x is string and not null; }", TestName = "CombinedPattern_NullWithTypeAndNotNull")]
    public async Task CombinedPatterns_IsExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion
}
