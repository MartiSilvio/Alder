using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// Verbatim string (@"...") test data.
/// Tests that verbatim strings treat backslashes as literal characters,
/// doubled quotes as escaped quotes, and regex patterns are preserved.
/// Shared across compiler backends.
/// </summary>
public static class VerbatimStringData
{
    /// <summary>
    /// Verbatim string value cases.
    /// Signature: (string expr, string expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        new(@"@""path\to\file""", @"path\to\file") { TestName = "Verbatim_BackslashesAreLiteral" },
        new(@"@""C:\Users\John\Documents""", @"C:\Users\John\Documents") { TestName = "Verbatim_MultipleBackslashes" },
        new(@"@""She said """"Hello"""".""", @"She said ""Hello"".") { TestName = "Verbatim_EscapedQuote" },
        new(@"@""""", "") { TestName = "Verbatim_EmptyString" },
        new(@"@""It's fine""", "It's fine") { TestName = "Verbatim_SingleQuoteNoEscape" },
        new(@"@""^\d{3}-\d{4}$""", @"^\d{3}-\d{4}$") { TestName = "Verbatim_RegexPattern" },
        // Comparison with regular strings
        new(@"""path\\to\\file""", @"path\to\file") { TestName = "Regular_BackslashNeedsEscape" },
    ];
}
