using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.8.21 -- Switch expressions.
/// Test data for constant/type/relational/property pattern arms,
/// when guards (S12.8.21.3), discard catch-all (S11.2.8),
/// null handling, and first-match semantics.
/// Shared across compiler backends.
/// </summary>
public static class SwitchExpressionData
{
    /// <summary>
    /// ECMA-334 S12.8.21 -- Basic switch expression with constant arms.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> ConstantArmCases() =>
    [
        new("{ object x = 1; return x switch { 1 => \"one\", 2 => \"two\", _ => \"other\" }; }") { TestName = "SwitchExpr_ConstantArm_MatchFirst" },
        new("{ object x = 2; return x switch { 1 => \"one\", 2 => \"two\", _ => \"other\" }; }") { TestName = "SwitchExpr_ConstantArm_MatchSecond" },
        new("{ object x = 99; return x switch { 1 => \"one\", 2 => \"two\", _ => \"other\" }; }") { TestName = "SwitchExpr_ConstantArm_MatchDiscard" },
        new("{ object x = \"hello\"; return x switch { \"hello\" => 1, \"world\" => 2, _ => 0 }; }") { TestName = "SwitchExpr_StringArms" },
        new("{ object x = \"world\"; return x switch { \"hello\" => 1, \"world\" => 2, _ => 0 }; }") { TestName = "SwitchExpr_StringArms_Second" },
        new("{ object x = \"other\"; return x switch { \"hello\" => 1, \"world\" => 2, _ => 0 }; }") { TestName = "SwitchExpr_StringArms_Default" },
        new("{ object x = true; return x switch { true => \"yes\", false => \"no\", _ => \"unknown\" }; }") { TestName = "SwitchExpr_BoolArms_True" },
        new("{ object x = false; return x switch { true => \"yes\", false => \"no\", _ => \"unknown\" }; }") { TestName = "SwitchExpr_BoolArms_False" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21 + S11.2.2 -- Switch with type patterns.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> TypePatternCases() =>
    [
        new("{ object x = 5; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }") { TestName = "SwitchExpr_TypePattern_Int" },
        new("{ object x = \"hello\"; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }") { TestName = "SwitchExpr_TypePattern_String" },
        new("{ object x = 3.14; return x switch { int i => i * 2, string s => s.Length, _ => -1 }; }") { TestName = "SwitchExpr_TypePattern_Default" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21 + S11.2.5/S11.2.6 -- Switch with relational and logical patterns.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> RelationalArmCases() =>
    [
        new("{ object x = 150; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }") { TestName = "SwitchExpr_Relational_High" },
        new("{ object x = 75; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }") { TestName = "SwitchExpr_Relational_Medium" },
        new("{ object x = 25; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }") { TestName = "SwitchExpr_Relational_Low" },
        new("{ object x = -5; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }") { TestName = "SwitchExpr_Relational_Negative" },
        new("{ object x = 100; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }") { TestName = "SwitchExpr_Relational_Boundary100" },
        new("{ object x = 0; return x switch { > 100 => \"high\", > 50 => \"medium\", >= 0 => \"low\", _ => \"negative\" }; }") { TestName = "SwitchExpr_Relational_Boundary0" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21.3 -- When guards.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> WhenGuardCases() =>
    [
        new("{ object x = 5; return x switch { int n when n > 0 => \"positive\", int n when n < 0 => \"negative\", _ => \"zero\" }; }") { TestName = "SwitchExpr_WhenGuard_Positive" },
        new("{ object x = -3; return x switch { int n when n > 0 => \"positive\", int n when n < 0 => \"negative\", _ => \"zero\" }; }") { TestName = "SwitchExpr_WhenGuard_Negative" },
        new("{ object x = 0; return x switch { int n when n > 0 => \"positive\", int n when n < 0 => \"negative\", _ => \"zero\" }; }") { TestName = "SwitchExpr_WhenGuard_Zero" },
    ];

    /// <summary>
    /// When guard failure falls through to next arm.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> WhenGuardFallthroughCases() =>
    [
        new("{ object x = 50; return x switch { int n when n > 100 => \"big\", int n => \"small\", _ => \"not int\" }; }") { TestName = "SwitchExpr_WhenGuard_Fallthrough" },
    ];

    /// <summary>
    /// When guard with string member access.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> WhenGuardStringLengthCases() =>
    [
        new("{ object x = \"hello\"; return x switch { string s when s.Length > 3 => \"long\", string s => \"short\", _ => \"not string\" }; }") { TestName = "SwitchExpr_WhenGuard_StringLength" },
        new("{ object x = \"hi\"; return x switch { string s when s.Length > 3 => \"long\", string s => \"short\", _ => \"not string\" }; }") { TestName = "SwitchExpr_WhenGuard_StringLength_Short" },
        new("{ object x = 42; return x switch { string s when s.Length > 3 => \"long\", string s => \"short\", _ => \"not string\" }; }") { TestName = "SwitchExpr_WhenGuard_StringLength_NotString" },
    ];

    /// <summary>
    /// ECMA-334 S11.2.8 -- Discard pattern in switch arms.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> DiscardArmCases() =>
    [
        new("{ object x = \"anything\"; return x switch { _ => \"default\" }; }") { TestName = "SwitchExpr_DiscardOnly" },
        new("{ object x = null; return x switch { _ => \"default\" }; }") { TestName = "SwitchExpr_DiscardOnly_Null" },
        new("{ object x = 42; return x switch { _ => \"default\" }; }") { TestName = "SwitchExpr_DiscardOnly_Int" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21 + S11.2.7 -- Property patterns in switch arms.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> PropertyPatternArmCases() =>
    [
        new("{ object x = \"\"; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }") { TestName = "SwitchExpr_PropertyPattern_Empty" },
        new("{ object x = \"hello\"; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }") { TestName = "SwitchExpr_PropertyPattern_HasContent" },
        new("{ object x = null; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }") { TestName = "SwitchExpr_PropertyPattern_Null" },
        new("{ object x = 42; return x switch { string { Length: 0 } => \"empty\", string { Length: > 0 } => \"has content\", _ => \"null or not string\" }; }") { TestName = "SwitchExpr_PropertyPattern_NotString" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21 -- First-match semantics.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> FirstMatchCases() =>
    [
        new("{ object x = 42; return x switch { int => \"int\", object => \"object\", _ => \"other\" }; }") { TestName = "SwitchExpr_FirstMatch_IntBeforeObject" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21 -- Switch expression in larger expressions.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> InExpressionCases() =>
    [
        new("{ object x = 1; return (x switch { 1 => 10, _ => 0 }) + 5; }") { TestName = "SwitchExpr_InExpression_Add" },
        new("{ object x = 2; return (x switch { 1 => 10, _ => 0 }) + 5; }") { TestName = "SwitchExpr_InExpression_Default" },
        new("{ object x = 3; return (x switch { 3 => \"hello\", _ => \"world\" }).Length; }") { TestName = "SwitchExpr_InExpression_MemberAccess" },
    ];

    /// <summary>
    /// ECMA-334 S12.8.21 -- Null handling in switch.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> NullHandlingCases() =>
    [
        new("{ object x = null; return x switch { null => \"null\", _ => \"not null\" }; }") { TestName = "SwitchExpr_NullArm_Match" },
        new("{ object x = 42; return x switch { null => \"null\", _ => \"not null\" }; }") { TestName = "SwitchExpr_NullArm_NoMatch" },
        new("{ object x = null; return x switch { string s => \"string\", null => \"null\", _ => \"other\" }; }") { TestName = "SwitchExpr_NullArm_AfterTypePattern" },
    ];
}
