using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S11.2 -- Pattern matching via is-expressions.
/// Test data for constant patterns (S11.2.3), relational patterns (S11.2.5),
/// logical combinators (S11.2.6), property patterns (S11.2.7),
/// type patterns with variable binding (S11.2.2), and combination patterns.
/// Shared across compiler backends.
/// </summary>
public static class PatternData
{
    /// <summary>
    /// ECMA-334 S11.2.3 -- Constant patterns in is-expressions.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> ConstantPatternCases() =>
    [
        new("{ object x = 42; return x is 42; }") { TestName = "ConstantPattern_IntMatch" },
        new("{ object x = 42; return x is 99; }") { TestName = "ConstantPattern_IntNoMatch" },
        new("{ object x = \"hello\"; return x is \"hello\"; }") { TestName = "ConstantPattern_StringMatch" },
        new("{ object x = \"hello\"; return x is \"world\"; }") { TestName = "ConstantPattern_StringNoMatch" },
        new("{ object x = true; return x is true; }") { TestName = "ConstantPattern_BoolMatch" },
        new("{ object x = true; return x is false; }") { TestName = "ConstantPattern_BoolNoMatch" },
        new("{ object x = null; return x is null; }") { TestName = "ConstantPattern_NullMatch" },
        new("{ object x = 42; return x is null; }") { TestName = "ConstantPattern_NullNoMatch" },
        new("{ object x = \"hello\"; return x is null; }") { TestName = "ConstantPattern_StringIsNull_False" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.5 -- Relational patterns.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> RelationalPatternCases() =>
    [
        new("{ object x = 50; return x is > 0; }") { TestName = "RelationalPattern_GreaterThan_True" },
        new("{ object x = -1; return x is > 0; }") { TestName = "RelationalPattern_GreaterThan_False" },
        new("{ object x = 0; return x is > 0; }") { TestName = "RelationalPattern_GreaterThan_Boundary" },
        new("{ object x = 100; return x is >= 100; }") { TestName = "RelationalPattern_GreaterOrEqual_True" },
        new("{ object x = 99; return x is >= 100; }") { TestName = "RelationalPattern_GreaterOrEqual_False" },
        new("{ object x = 5; return x is < 10; }") { TestName = "RelationalPattern_LessThan_True" },
        new("{ object x = 10; return x is < 10; }") { TestName = "RelationalPattern_LessThan_False" },
        new("{ object x = 10; return x is <= 10; }") { TestName = "RelationalPattern_LessOrEqual_True" },
        new("{ object x = 11; return x is <= 10; }") { TestName = "RelationalPattern_LessOrEqual_False" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.6 -- Logical pattern combinators: and.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> LogicalAndCases() =>
    [
        new("{ object x = 50; return x is > 0 and <= 100; }") { TestName = "LogicalPattern_And_InRange" },
        new("{ object x = 0; return x is > 0 and <= 100; }") { TestName = "LogicalPattern_And_OutOfRange_Low" },
        new("{ object x = 150; return x is > 0 and <= 100; }") { TestName = "LogicalPattern_And_OutOfRange_High" },
        new("{ object x = 100; return x is > 0 and <= 100; }") { TestName = "LogicalPattern_And_UpperBoundary" },
        new("{ object x = 1; return x is > 0 and <= 100; }") { TestName = "LogicalPattern_And_LowerBoundary" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.6 -- Logical pattern combinators: or.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> LogicalOrCases() =>
    [
        new("{ object x = 42; return x is int or string; }") { TestName = "LogicalPattern_Or_MatchesInt" },
        new("{ object x = \"hello\"; return x is int or string; }") { TestName = "LogicalPattern_Or_MatchesString" },
        new("{ object x = 3.14; return x is int or string; }") { TestName = "LogicalPattern_Or_NoMatch" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.6 -- Logical pattern combinators: not.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> LogicalNotCases() =>
    [
        new("{ object x = \"hello\"; return x is not int; }") { TestName = "LogicalPattern_Not_True" },
        new("{ object x = 42; return x is not int; }") { TestName = "LogicalPattern_Not_False" },
        new("{ object x = null; return x is not null; }") { TestName = "LogicalPattern_NotNull_False" },
        new("{ object x = 42; return x is not null; }") { TestName = "LogicalPattern_NotNull_True" },
        new("{ object x = \"hello\"; return x is not null; }") { TestName = "LogicalPattern_NotNull_String_True" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.6 -- Nested logical combinators.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> LogicalNestedCases() =>
    [
        new("{ object x = 50; return x is > 0 and < 100 or > 200; }") { TestName = "LogicalPattern_AndOr_InLowRange" },
        new("{ object x = 250; return x is > 0 and < 100 or > 200; }") { TestName = "LogicalPattern_AndOr_InHighRange" },
        new("{ object x = 150; return x is > 0 and < 100 or > 200; }") { TestName = "LogicalPattern_AndOr_InMiddle" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.7 -- Property patterns with type prefix (parity).
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> PropertyPatternWithTypeCases() =>
    [
        new("{ object x = \"hello\"; return x is string { Length: 5 }; }") { TestName = "PropertyPattern_WithTypeCheck" },
        new("{ object x = \"hello\"; return x is string { Length: > 3 }; }") { TestName = "PropertyPattern_WithTypeAndRelational" },
        new("{ object x = 42; return x is string { Length: > 0 }; }") { TestName = "PropertyPattern_TypeMismatch_False" },
        new("{ object x = \"hello\"; return x is { }; }") { TestName = "PropertyPattern_EmptyNonNull_True" },
        new("{ object x = null; return x is { }; }") { TestName = "PropertyPattern_EmptyNull_False" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.7 -- Property patterns with member access (string-typed for parity).
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> PropertyPatternMemberAccessCases() =>
    [
        new("{ string x = \"hello\"; return x is { Length: 5 }; }") { TestName = "PropertyPattern_ExactMatch" },
        new("{ string x = \"hello\"; return x is { Length: 4 }; }") { TestName = "PropertyPattern_ExactNoMatch" },
        new("{ string x = \"hello\"; return x is { Length: > 0 }; }") { TestName = "PropertyPattern_RelationalSub" },
        new("{ string x = \"\"; return x is { Length: > 0 }; }") { TestName = "PropertyPattern_NoMatch" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.2 -- Type patterns with variable binding.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> TypePatternCases() =>
    [
        new("{ object x = \"hello\"; return x is string s ? s.Length : -1; }") { TestName = "TypePattern_VarBinding_Match" },
        new("{ object x = 42; return x is string s ? s.Length : -1; }") { TestName = "TypePattern_VarBinding_NoMatch" },
        new("{ object x = 42; return x is int i ? i * 2 : 0; }") { TestName = "TypePattern_VarBinding_IntMatch" },
        new("{ object x = null; return x is string s ? s.Length : -1; }") { TestName = "TypePattern_VarBinding_Null" },
        new("{ object x = \"test\"; return x is string s ? s.ToUpper() : \"N/A\"; }") { TestName = "TypePattern_VarBinding_MethodCall" },
    ];

    /// <summary>
    /// ECMA-334 S11.2 -- Combination patterns in is-expressions.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> CombinedPatternCases() =>
    [
        new("{ object x = 42; return x is int and > 0; }") { TestName = "CombinedPattern_TypeAndRelational" },
        new("{ object x = -5; return x is int and > 0; }") { TestName = "CombinedPattern_TypeAndRelational_Fail" },
        new("{ object x = \"hi\"; return x is int and > 0; }") { TestName = "CombinedPattern_TypeMismatch" },
        new("{ object x = \"hello\"; return x is string and not null; }") { TestName = "CombinedPattern_TypeAndNotNull" },
        new("{ object x = null; return x is string and not null; }") { TestName = "CombinedPattern_NullWithTypeAndNotNull" },
    ];
}
