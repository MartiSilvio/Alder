using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S13.8.3 -- Switch statement.
/// Test data for switch statement cases including basic matching, default case,
/// fall-through, string matching, nested switches, boolean cases, and real-world scenarios.
/// Shared across compiler backends.
/// </summary>
public static class SwitchStatementData
{
    /// <summary>
    /// Switch statement value-producing cases with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        // Basic switch matching
        new("""
            {
                var x = 2;
                var result = "";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    case 2:
                        result = "two";
                        break;
                    case 3:
                        result = "three";
                        break;
                }
                return result;
            }
            """,
            "two") { TestName = "BasicCase_MatchesMiddle" },
        new("""
            {
                var x = 1;
                var result = "";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    case 2:
                        result = "two";
                        break;
                }
                return result;
            }
            """,
            "one") { TestName = "BasicCase_MatchesFirst" },
        new("""
            {
                var x = 3;
                var result = "";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    case 2:
                        result = "two";
                        break;
                    case 3:
                        result = "three";
                        break;
                }
                return result;
            }
            """,
            "three") { TestName = "BasicCase_MatchesLast" },
        new("""
            {
                var x = 99;
                var result = "initial";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    case 2:
                        result = "two";
                        break;
                }
                return result;
            }
            """,
            "initial") { TestName = "NoMatch_ResultUnchanged" },

        // Default case
        new("""
            {
                var x = 99;
                var result = "";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    case 2:
                        result = "two";
                        break;
                    default:
                        result = "other";
                        break;
                }
                return result;
            }
            """,
            "other") { TestName = "DefaultCase_ExecutesWhenNoMatch" },
        new("""
            {
                var x = 1;
                var result = "";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    default:
                        result = "other";
                        break;
                }
                return result;
            }
            """,
            "one") { TestName = "DefaultCase_SkippedWhenCaseMatches" },
        new("""
            {
                var x = 99;
                var result = "";
                switch (x) {
                    default:
                        result = "other";
                        break;
                    case 1:
                        result = "one";
                        break;
                }
                return result;
            }
            """,
            "other") { TestName = "DefaultCaseFirst_StillWorks" },
        new("""
            {
                var x = 99;
                var result = "";
                switch (x) {
                    case 1:
                        result = "one";
                        break;
                    default:
                        result = "other";
                        break;
                    case 2:
                        result = "two";
                        break;
                }
                return result;
            }
            """,
            "other") { TestName = "DefaultCaseMiddle_StillWorks" },
        new("""
            {
                var x = 42;
                var result = "";
                switch (x) {
                    default:
                        result = "default only";
                        break;
                }
                return result;
            }
            """,
            "default only") { TestName = "OnlyDefaultCase_ExecutesAlways" },

        // Fall-through (empty cases)
        new("""
            {
                var x = 1;
                var result = "";
                switch (x) {
                    case 1:
                    case 2:
                        result = "one or two";
                        break;
                    case 3:
                        result = "three";
                        break;
                }
                return result;
            }
            """,
            "one or two") { TestName = "EmptyCaseFallThrough" },
        new("""
            {
                var x = 2;
                var count = 0;
                switch (x) {
                    case 1:
                    case 2:
                    case 3:
                        count = count + 1;
                        break;
                }
                return count;
            }
            """,
            1) { TestName = "MultipleEmptyCasesFallThrough" },

        // String matching
        new("""
            {
                var fruit = "apple";
                var category = "";
                switch (fruit) {
                    case "apple":
                        category = "pome";
                        break;
                    case "orange":
                        category = "citrus";
                        break;
                    case "banana":
                        category = "tropical";
                        break;
                    default:
                        category = "unknown";
                        break;
                }
                return category;
            }
            """,
            "pome") { TestName = "StringCase_MatchesExactly" },
        new("""
            {
                var fruit = "Apple";
                var category = "";
                switch (fruit) {
                    case "apple":
                        category = "lowercase";
                        break;
                    case "Apple":
                        category = "capitalized";
                        break;
                    default:
                        category = "unknown";
                        break;
                }
                return category;
            }
            """,
            "capitalized") { TestName = "StringCase_CaseSensitive" },

        // Expression in switch value and case pattern
        new("""
            {
                var a = 3;
                var b = 2;
                var result = "";
                switch (a + b) {
                    case 4:
                        result = "four";
                        break;
                    case 5:
                        result = "five";
                        break;
                    case 6:
                        result = "six";
                        break;
                }
                return result;
            }
            """,
            "five") { TestName = "ExpressionInSwitchValue" },
        new("""
            {
                var x = 10;
                var multiplier = 2;
                var result = "";
                switch (x) {
                    case 5 * 2:
                        result = "matched 5*2";
                        break;
                    case 3 * 4:
                        result = "matched 3*4";
                        break;
                }
                return result;
            }
            """,
            "matched 5*2") { TestName = "ExpressionInCasePattern" },

        // Return in case
        new("""
            {
                var x = 2;
                switch (x) {
                    case 1:
                        return "one";
                    case 2:
                        return "two";
                    case 3:
                        return "three";
                }
                return "no match";
            }
            """,
            "two") { TestName = "ReturnInCase_ExitsBlock" },
        new("""
            {
                var x = 99;
                switch (x) {
                    case 1:
                        return "one";
                    case 2:
                        return "two";
                    default:
                        return "default";
                }
                return "after switch";
            }
            """,
            "default") { TestName = "ReturnInDefault_ExitsBlock" },
        new("""
            {
                var x = 99;
                switch (x) {
                    case 1:
                        return "one";
                    case 2:
                        return "two";
                }
                return "no match";
            }
            """,
            "no match") { TestName = "NoMatchNoDefault_ContinuesAfter" },

        // Multiple statements and control flow in cases
        new("""
            {
                var x = 2;
                var total = 0;
                switch (x) {
                    case 2:
                        var a = 10;
                        var b = 20;
                        total = a + b;
                        break;
                }
                return total;
            }
            """,
            30) { TestName = "MultipleStatementsInCase" },
        new("""
            {
                var x = 1;
                var sum = 0;
                switch (x) {
                    case 1:
                        var i = 0;
                        while (i < 5) {
                            sum = sum + i;
                            i = i + 1;
                        }
                        break;
                }
                return sum;
            }
            """,
            10) { TestName = "LoopInsideCase" },
        new("""
            {
                var x = 1;
                var val = 15;
                var category = "";
                switch (x) {
                    case 1:
                        if (val > 10) {
                            category = "high";
                        } else {
                            category = "low";
                        }
                        break;
                }
                return category;
            }
            """,
            "high") { TestName = "IfInsideCase" },

        // Nested switch
        new("""
            {
                var outer = 1;
                var inner = 2;
                var result = "";
                switch (outer) {
                    case 1:
                        switch (inner) {
                            case 1:
                                result = "1-1";
                                break;
                            case 2:
                                result = "1-2";
                                break;
                        }
                        break;
                    case 2:
                        result = "2-x";
                        break;
                }
                return result;
            }
            """,
            "1-2") { TestName = "NestedSwitch" },
        new("""
            {
                var outer = 1;
                var inner = 1;
                var log = "";
                switch (outer) {
                    case 1:
                        log = log + "outer1-";
                        switch (inner) {
                            case 1:
                                log = log + "inner1";
                                break;
                        }
                        log = log + "-afterinner";
                        break;
                }
                return log;
            }
            """,
            "outer1-inner1-afterinner") { TestName = "BreakInNestedSwitch_OnlyExitsInner" },

        // Boolean cases
        new("""
            {
                var flag = true;
                var result = "";
                switch (flag) {
                    case true:
                        result = "is true";
                        break;
                    case false:
                        result = "is false";
                        break;
                }
                return result;
            }
            """,
            "is true") { TestName = "BooleanCase_True" },
        new("""
            {
                var flag = false;
                var result = "";
                switch (flag) {
                    case true:
                        result = "is true";
                        break;
                    case false:
                        result = "is false";
                        break;
                }
                return result;
            }
            """,
            "is false") { TestName = "BooleanCase_False" },

        // Edge cases
        new("""
            {
                var x = 1;
                switch (x) {
                }
                return "after switch";
            }
            """,
            "after switch") { TestName = "EmptySwitch_NoError" },
        new("""
            {
                var x = 1;
                var result = "";
                switch (x) {
                    case 1:
                    case 2:
                        result = "one or two";
                        break;
                    case 3:
                        result = "three";
                        break;
                }
                return result;
            }
            """,
            "one or two") { TestName = "EmptyCase_FallsThroughToNext" },
        new("""
            {
                var x = 2;
                var result = "";
                switch (x) {
                    case 1:
                    case 2:
                    case 3:
                        result = "1, 2, or 3";
                        break;
                    default:
                        result = "other";
                        break;
                }
                return result;
            }
            """,
            "1, 2, or 3") { TestName = "MultipleFallThroughCases" },

        // Type edge cases
        new("""
            {
                var x = 1000000000000;
                var result = "";
                switch (x) {
                    case 1000000000000:
                        result = "trillion";
                        break;
                    default:
                        result = "other";
                        break;
                }
                return result;
            }
            """,
            "trillion") { TestName = "LongValues" },
        new("""
            {
                var x = 3.14;
                var result = "";
                switch (x) {
                    case 3.14:
                        result = "pi";
                        break;
                    case 2.71:
                        result = "e";
                        break;
                    default:
                        result = "other";
                        break;
                }
                return result;
            }
            """,
            "pi") { TestName = "DoubleValues" },

        // Real-world scenarios
        new("""
            {
                var day = 3;
                var dayName = "";
                switch (day) {
                    case 0:
                        dayName = "Sunday";
                        break;
                    case 1:
                        dayName = "Monday";
                        break;
                    case 2:
                        dayName = "Tuesday";
                        break;
                    case 3:
                        dayName = "Wednesday";
                        break;
                    case 4:
                        dayName = "Thursday";
                        break;
                    case 5:
                        dayName = "Friday";
                        break;
                    case 6:
                        dayName = "Saturday";
                        break;
                    default:
                        dayName = "Invalid";
                        break;
                }
                return dayName;
            }
            """,
            "Wednesday") { TestName = "DayOfWeek_Scenario" },
        new("""
            {
                var category = 8;
                var grade = "";
                switch (category) {
                    case 10:
                    case 9:
                        grade = "A";
                        break;
                    case 8:
                        grade = "B";
                        break;
                    case 7:
                        grade = "C";
                        break;
                    case 6:
                        grade = "D";
                        break;
                    default:
                        grade = "F";
                        break;
                }
                return grade;
            }
            """,
            "B") { TestName = "GradeCalculation_Scenario" },
        new("""
            {
                var statusCode = 404;
                var message = "";
                switch (statusCode) {
                    case 200:
                        message = "OK";
                        break;
                    case 201:
                        message = "Created";
                        break;
                    case 400:
                        message = "Bad Request";
                        break;
                    case 401:
                        message = "Unauthorized";
                        break;
                    case 403:
                        message = "Forbidden";
                        break;
                    case 404:
                        message = "Not Found";
                        break;
                    case 500:
                        message = "Internal Server Error";
                        break;
                    default:
                        message = "Unknown Status";
                        break;
                }
                return message;
            }
            """,
            "Not Found") { TestName = "HttpStatus_Scenario" },

        // Zero and negative values
        new("""
            {
                var x = 0;
                var result = "";
                switch (x) {
                    case 0:
                        result = "zero";
                        break;
                    case 1:
                        result = "one";
                        break;
                }
                return result;
            }
            """,
            "zero") { TestName = "ZeroValue" },
        new("""
            {
                var x = -1;
                var result = "";
                switch (x) {
                    case -1:
                        result = "minus one";
                        break;
                    case 0:
                        result = "zero";
                        break;
                    case 1:
                        result = "one";
                        break;
                }
                return result;
            }
            """,
            "minus one") { TestName = "NegativeValue" },
        new("""
            {
                var x = "";
                var result = "";
                switch (x) {
                    case "":
                        result = "empty";
                        break;
                    default:
                        result = "not empty";
                        break;
                }
                return result;
            }
            """,
            "empty") { TestName = "EmptyString" },

        // Case value changes after match
        new("""
            {
                var x = 1;
                var count = 0;
                switch (x) {
                    case 1:
                        x = 2;
                        count = count + 1;
                        break;
                    case 2:
                        count = count + 10;
                        break;
                }
                return count;
            }
            """,
            1) { TestName = "CaseValueChangesAfterMatch" },
    ];

    /// <summary>
    /// Parity-only switch cases (no expected value -- Roslyn determines correct result).
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ParityCases() =>
    [
        new("""
            {
                var a = 10.0;
                var b = 3.0;
                var op = "/";
                var calcResult = 0.0;
                switch (op) {
                    case "+":
                        calcResult = a + b;
                        break;
                    case "-":
                        calcResult = a - b;
                        break;
                    case "*":
                        calcResult = a * b;
                        break;
                    case "/":
                        calcResult = a / b;
                        break;
                    default:
                        calcResult = 0;
                        break;
                }
                return calcResult;
            }
            """) { TestName = "Switch_CalculatorOperation_Scenario" },
    ];
}
