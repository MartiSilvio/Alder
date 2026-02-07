using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §12.8.20 -- Default expression test data.
/// Tests default(T) for value types, reference types, and nullable types.
/// Shared across compiler backends.
/// </summary>
public static class DefaultExpressionData
{
    /// <summary>
    /// default(T) for value types.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueTypeCases() =>
    [
        new("default(int)", 0) { TestName = "Default_Int" },
        new("default(long)", 0L) { TestName = "Default_Long" },
        new("default(bool)", false) { TestName = "Default_Bool" },
        new("default(double)", 0.0) { TestName = "Default_Double" },
        new("default(float)", 0.0f) { TestName = "Default_Float" },
        new("default(char)", '\0') { TestName = "Default_Char" },
    ];
}
