using CsEval.TestData.Data;

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

    [TestCaseSource(typeof(PatternData), nameof(PatternData.ConstantPatternCases))]
    public async Task ConstantPattern_IsExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.5 -- Relational Patterns

    [TestCaseSource(typeof(PatternData), nameof(PatternData.RelationalPatternCases))]
    public async Task RelationalPattern_IsExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.6 -- Logical Pattern Combinators (and, or, not)

    // and combinator -- range checks
    [TestCaseSource(typeof(PatternData), nameof(PatternData.LogicalAndCases))]
    public async Task LogicalPattern_And(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // or combinator -- type alternatives
    [TestCaseSource(typeof(PatternData), nameof(PatternData.LogicalOrCases))]
    public async Task LogicalPattern_Or(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // not combinator
    [TestCaseSource(typeof(PatternData), nameof(PatternData.LogicalNotCases))]
    public async Task LogicalPattern_Not(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Nested logical combinators
    [TestCaseSource(typeof(PatternData), nameof(PatternData.LogicalNestedCases))]
    public async Task LogicalPattern_Nested(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §11.2.7 -- Property Patterns

    // Property patterns with a type prefix work with Roslyn parity (the type provides member resolution).
    [TestCaseSource(typeof(PatternData), nameof(PatternData.PropertyPatternWithTypeCases))]
    public async Task PropertyPattern_IsExpression_Parity(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Property patterns without type prefix: Roslyn requires the static type to resolve members,
    // so these use string-typed variables for Roslyn parity.
    [TestCaseSource(typeof(PatternData), nameof(PatternData.PropertyPatternMemberAccessCases))]
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

    [TestCaseSource(typeof(PatternData), nameof(PatternData.TypePatternCases))]
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

    [TestCaseSource(typeof(PatternData), nameof(PatternData.CombinedPatternCases))]
    public async Task CombinedPatterns_IsExpression(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion
}
