using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.8.16.2 -- Object creation expressions.
/// Test data for constructor invocation with member access on results,
/// including fully qualified type names.
/// Shared across compiler backends.
/// </summary>
public static class ConstructorData
{
    /// <summary>
    /// Constructor with member access on result.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> MemberAccessCases() =>
    [
        new("new Exception(\"test\").Message", "test")
            { TestName = "Constructor_Exception_Message" },
        new("new ArgumentException(\"msg\", \"param\").ParamName", "param")
            { TestName = "Constructor_ArgumentException_ParamName" },
        new("new ArgumentException(\"msg\", \"param\").Message", "msg (Parameter 'param')")
            { TestName = "Constructor_ArgumentException_Message" },
    ];

    /// <summary>
    /// Fully qualified type name constructor.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> FullyQualifiedCases() =>
    [
        new("new System.Exception(\"test\").Message", "test")
            { TestName = "Constructor_FullyQualified" },
    ];
}
