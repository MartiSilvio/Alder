using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.14 -- Conditional logical operators (&&, ||).
/// Test data for short-circuit evaluation, operator precedence (S12.4.2),
/// logical NOT (S12.9.4), and type errors for non-boolean operands.
/// Shared across compiler backends.
/// </summary>
public static class LogicalData
{
    /// <summary>
    /// Value-producing logical operator expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // ECMA-334 S12.14: "The && and || operators are conditional versions of the & and | operators"
        // with short-circuit evaluation semantics.
        new("true && true", true) { TestName = "And_TrueTrue" },
        new("true && false", false) { TestName = "And_TrueFalse" },
        new("false && true", false) { TestName = "And_FalseTrue" },
        new("false && false", false) { TestName = "And_FalseFalse" },
        new("true || false", true) { TestName = "Or_TrueFalse" },
        new("false || true", true) { TestName = "Or_FalseTrue" },
        new("false || false", false) { TestName = "Or_FalseFalse" },
        new("true || true", true) { TestName = "Or_TrueTrue" },
        // ECMA-334 S12.9.4: Logical negation operator
        new("!true", false) { TestName = "Not_True" },
        new("!false", true) { TestName = "Not_False" },
    ];

    /// <summary>
    /// Value-producing precedence and chaining expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> PrecedenceCases() =>
    [
        // ECMA-334 S12.4.2: && binds tighter than ||
        new("true || false && false", true) { TestName = "Precedence_AndBeforeOr" },
        new("(true || false) && false", false) { TestName = "Precedence_ParenOverride" },
        new("true && true && true", true) { TestName = "And_Chained_AllTrue" },
        new("true && true && false", false) { TestName = "And_Chained_LastFalse" },
        new("false || false || true", true) { TestName = "Or_Chained_LastTrue" },
        new("!!true", true) { TestName = "Not_DoubleNegation" },
        new("!!!false", true) { TestName = "Not_TripleNegation" },
    ];

    /// <summary>
    /// Value-producing logical expressions combined with comparison operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> WithComparisonsCases() =>
    [
        // ECMA-334 S12.14 -- Combined with Comparison Operators
        new("1 < 2 && 3 < 4", true) { TestName = "And_WithComparisons" },
        new("1 > 2 || 3 < 4", true) { TestName = "Or_WithComparisons" },
        new("!(1 > 2)", true) { TestName = "Not_WithComparison" },
    ];

    /// <summary>
    /// Logical expressions that should throw (non-boolean operands).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        // ECMA-334 S12.14: "The operands of && and || shall be of type bool"
        new("5 && 3") { TestName = "And_IntWithInt" },
        new("5 || 3") { TestName = "Or_IntWithInt" },
        new("!5") { TestName = "Not_Int" },
        new("!\"hello\"") { TestName = "Not_String" },
    ];
}
