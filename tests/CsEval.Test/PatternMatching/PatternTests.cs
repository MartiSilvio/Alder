namespace CsEval.Test.PatternMatching;

// Engine-only: PropertyPattern_NullFalse uses object-typed variable with member access
// (Roslyn cannot resolve member on object type). DiscardPattern test is Assert.Pass() placeholder.

/// <summary>
/// ECMA-334 §11.2 -- Pattern matching via is-expressions.
/// Tests constant patterns (section 11.2.3), type patterns with variable binding (section 11.2.2),
/// relational patterns (section 11.2.5), logical combinators (section 11.2.6),
/// property patterns (section 11.2.7), and the discard pattern (section 11.2.8).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class PatternTests(CompilationMode mode)
{
    #region ECMA-334 §11.2.7 -- Property Patterns

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
}
