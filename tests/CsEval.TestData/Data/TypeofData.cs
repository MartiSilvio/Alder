using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 §12.8.17 -- The typeof operator test data.
/// Tests member access on Type objects and equality comparison of Type singletons.
/// Shared across compiler backends.
/// </summary>
public static class TypeofData
{
    /// <summary>
    /// typeof(T).Name member access cases.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> MemberAccessCases() =>
    [
        new("typeof(int).Name", "Int32") { TestName = "Typeof_Int_Name" },
        new("typeof(string).Name", "String") { TestName = "Typeof_String_Name" },
        new("typeof(bool).Name", "Boolean") { TestName = "Typeof_Bool_Name" },
        new("typeof(double).Name", "Double") { TestName = "Typeof_Double_Name" },
        new("typeof(void).Name", "Void") { TestName = "Typeof_Void_Name" },
    ];

    /// <summary>
    /// typeof(T) equality comparison cases.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> EqualityCases() =>
    [
        new("typeof(int) == typeof(int)", true) { TestName = "Typeof_EqualsSameType" },
        new("typeof(int) != typeof(string)", true) { TestName = "Typeof_NotEqualsOtherType" },
    ];
}
