using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S8.18 -- Assignment operators.
/// Test data for basic variable assignment, reassignment, typed assignment,
/// assignment expressions, conditional assignment, and scoping behavior.
/// Shared across compiler backends.
/// </summary>
public static class AssignmentData
{
    /// <summary>
    /// Basic variable assignment and reassignment expressions.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BasicCases() =>
    [
        new("{ var x = 10; x = 20; return x; }", 20)
            { TestName = "Assignment_SimpleVariable_UpdatesValue" },
        new("{ var x = 1; x = 2; x = 3; x = 4; return x; }", 4)
            { TestName = "Assignment_MultipleAssignments_TracksLatestValue" },
        new("{ var x = 5; x = x + 10; return x; }", 15)
            { TestName = "Assignment_ToExpressionResult_WorksCorrectly" },
        new("{ var x = 1; x = x + 1; x = x * 2; x = x - 1; return x; }", 3)
            { TestName = "Assignment_ChainedArithmetic_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment with different value types (string, bool, double).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> TypeCases() =>
    [
        new("""
            {
                var s = "hello";
                s = "world";
                return s;
            }
            """, "world")
            { TestName = "Assignment_StringValue_WorksCorrectly" },
        new("{ var flag = true; flag = false; return flag; }", false)
            { TestName = "Assignment_BooleanValue_WorksCorrectly" },
        new("{ var d = 1.5; d = 3.14; return d; }", 3.14)
            { TestName = "Assignment_DoubleValue_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment with null values.
    /// Signature: (string expr, object? expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NullCases() =>
    [
        new("""
            {
                var obj = "something";
                obj = null;
                return obj;
            }
            """, null)
            { TestName = "Assignment_NullValue_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment as expression returning value.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ExpressionReturnCases() =>
    [
        new("""
            {
                var x = 0;
                var y = x = 42;
                return y;
            }
            """, 42)
            { TestName = "Assignment_ReturnsAssignedValue" },
        new("""
            {
                var a = 0;
                var b = 0;
                var c = 0;
                a = b = c = 100;
                return a + b + c;
            }
            """, 300)
            { TestName = "Assignment_ChainedAssignment_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment inside conditional blocks.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ConditionalCases() =>
    [
        new("""
            {
                var x = 0;
                if (true) {
                    x = 100;
                }
                return x;
            }
            """, 100)
            { TestName = "Assignment_InsideIf_WorksCorrectly" },
        new("""
            {
                var x = 0;
                if (false) {
                    x = 50;
                } else {
                    x = 100;
                }
                return x;
            }
            """, 100)
            { TestName = "Assignment_InsideElse_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment with module results and string concatenation.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ModuleCases() =>
    [
        new("""
            {
                var absValue = 0.0;
                absValue = Math.Abs(-42.5);
                return absValue;
            }
            """, 42.5)
            { TestName = "Assignment_WithMathResult_WorksCorrectly" },
        new("""
            {
                var greeting = "Hello";
                greeting = greeting + " World";
                return greeting;
            }
            """, "Hello World")
            { TestName = "Assignment_WithStringConcat_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment scoping with nested blocks and multiple variables.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ScopingCases() =>
    [
        new("""
            {
                var x = 1;
                if (true) {
                    x = 2;
                    if (true) {
                        x = 3;
                    }
                }
                return x;
            }
            """, 3)
            { TestName = "Assignment_InnerBlockModifiesOuter_WorksCorrectly" },
        new("""
            {
                var a = 1;
                var b = 10;
                var c = 100;
                a = a + 1;
                b = b + 1;
                c = c + 1;
                return a + b + c;
            }
            """, 114)
            { TestName = "Assignment_MultipleVariables_TracksIndependently" },
    ];

    /// <summary>
    /// Assignment with interpolated strings.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> InterpolatedStringCases() =>
    [
        new("""
            {
                var name = "Alice";
                var greeting = "";
                greeting = $"Hello, {name}!";
                name = "Bob";
                greeting = $"Hello, {name}!";
                return greeting;
            }
            """, "Hello, Bob!")
            { TestName = "Assignment_InterpolatedString_WorksCorrectly" },
    ];

    /// <summary>
    /// Assignment with null coalescing operators.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> NullCoalesceCases() =>
    [
        new("""
            {
                int? maybeNull = null;
                var fallback = 0;
                fallback = maybeNull ?? 42;
                return fallback;
            }
            """, 42)
            { TestName = "Assignment_FromNullCoalesce_WorksCorrectly" },
        new("""
            {
                int? a = null;
                int? b = null;
                a = 10;
                b ??= 20;
                return a + b;
            }
            """, 30)
            { TestName = "Assignment_VsNullCoalesceAssign_BothWork" },
    ];
}
