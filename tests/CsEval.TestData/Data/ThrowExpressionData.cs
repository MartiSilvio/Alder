using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.16 -- Throw expression operator.
/// Test data for throw expressions in null-coalescing (??) and conditional (?:) contexts.
/// Shared across compiler backends.
/// </summary>
public static class ThrowExpressionData
{
    /// <summary>
    /// Value-producing throw expression contexts (non-throw branch evaluated).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.16 -- Null-coalescing with throw: non-null left side returns value
        new("\"hello\" ?? throw new Exception(\"fail\")", "hello") { TestName = "NullCoalesce_NonNullString_ReturnsValue" },
        // ECMA-334 S12.16 -- Conditional with throw: non-throw branch returns value
        new("true ? 42 : throw new Exception(\"fail\")", 42) { TestName = "Conditional_TrueCondition_ReturnsThenValue" },
        new("false ? throw new Exception(\"fail\") : 42", 42) { TestName = "Conditional_FalseCondition_ReturnsElseValue" },
    ];
}
