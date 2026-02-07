using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// For loop test data -- basic iteration, control flow (break/continue),
/// nested loops, and error cases.
/// Shared across compiler backends.
/// </summary>
public static class ForLoopData
{
    /// <summary>
    /// Value-producing for loop expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        new("var count = 0; for (var i = 0; i < 5; i++) { count++; } return count;", 5) { TestName = "Basic_CountsToFive" },
        new("var executed = false; for (var i = 0; i < 0; i++) { executed = true; } return executed;", false) { TestName = "Basic_ZeroIterations" },
        new("var count = 0; for (var i = 0; i < 1; i++) { count++; } return count;", 1) { TestName = "Basic_SingleIteration" },
        new("var sum = 0; for (var i = 10; i > 0; i--) { sum += i; } return sum;", 55) { TestName = "Basic_CountDown" },
        new("var result = 0; for (var i = 1; i <= 5; i++) { result += i * i; } return result;", 55) { TestName = "LoopVariable_AccessibleInBody" },
        new("var total = 0; for (var i = 0; i < 5; i++) { var squared = i * i; total += squared; } return total;", 30) { TestName = "MultipleVariables" },
        new("var i = 0; var count = 0; for (; i < 5; i++) { count++; } return count;", 5) { TestName = "NoInitializer" },
        new("var count = 0; for (var i = 0; ; i++) { count++; if (i >= 4) { break; } } return count;", 5) { TestName = "NoCondition_BreakExits" },
        new("var count = 0; for (var i = 0; i < 5;) { count++; i++; } return count;", 5) { TestName = "NoIncrement" },
        new("var count = 0; for (;;) { count++; if (count >= 10) { break; } } return count;", 10) { TestName = "EmptyParts_BreakExits" },
        new("var count = 0; for (var i = 0; i < 10 && count < 5; i++) { count++; } return count;", 5) { TestName = "AndCondition" },
        new("var a = 0; var b = 10; for (var i = 0; a < 5 || b > 5; i++) { a++; b--; } return a;", 5) { TestName = "OrCondition" },
        new("for (var i = 0; i < 100; i++) { if (i == 5) { return i; } } return -1;", 5) { TestName = "EarlyReturn" },
        new("var sum = 0; for (var i = 0; i < 10; i++) { if (i % 2 == 0) { sum += i; } } return sum;", 20) { TestName = "NestedIf" },
        new("var total = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 3; j++) { total++; } } return total;", 9) { TestName = "NestedLoops" },
        new("var product = 1; for (var i = 0; i < 2; i++) { for (var j = 0; j < 2; j++) { for (var k = 0; k < 2; k++) { product *= 2; } } } return product;", 256) { TestName = "DeeplyNested" },
        new("var total = 0; for (var i = 0; i < 3; i++) { var j = 0; while (j < 3) { total++; j++; } } return total;", 9) { TestName = "NestedWithWhile" },
        new("var str = \"\"; for (var i = 0; i < 5; i++) { str = str + i; } return str;", "01234") { TestName = "StringConcatenation" },
        new("var lines = \"\"; for (var i = 1; i <= 3; i++) { lines = $\"{lines}Line {i}\\n\"; } return lines;", "Line 1\nLine 2\nLine 3\n") { TestName = "InterpolatedString" },
        new("var sum = 0; for (var i = 0; i < 5; i++) sum += i; return sum;", 10) { TestName = "SingleStatementBody" },
        new("var x = 0; for (var i = 0; i < 5; i++) { } return x;", 0) { TestName = "EmptyBody" },
        new("var count = 0; for (var i = 0; i < 10; i += 2) { count++; } return count;", 5) { TestName = "StepByTwo" },
        new("var sum = 0; for (var i = 5; i > 0; i--) { sum += i; } return sum;", 15) { TestName = "NegativeStep" },
        new("var factorial = 1; for (var n = 5; n > 1; n--) { factorial *= n; } return factorial;", 120) { TestName = "Factorial" },
        new("var power = 1; for (var i = 0; i < 10; i++) { power *= 2; } return power;", 1024) { TestName = "PowerCalculation" },
        new("var a = 0; var b = 1; for (var i = 0; i < 10; i++) { var temp = a + b; a = b; b = temp; } return b;", 89) { TestName = "Fibonacci" },
        new("var lastI = -1; for (var i = 0; i < 100; i++) { lastI = i; if (i == 5) { break; } } return lastI;", 5) { TestName = "Break_ExitsLoop" },
        new("var count = 0; for (var i = 0; i < 100; i++) { break; count++; } return count;", 0) { TestName = "Break_AtStart" },
        new("var sum = 0; for (var i = 1; i <= 10; i++) { sum += i; if (sum > 10) { break; } } return sum;", 15) { TestName = "Break_PreservesState" },
        new("var outerCount = 0; var totalInner = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 10; j++) { if (j == 2) { break; } totalInner++; } outerCount++; } return outerCount * 100 + totalInner;", 306) { TestName = "Break_OnlyExitsInnerLoop" },
        new("var sum = 0; for (var i = 1; i <= 10; i++) { if (i % 2 == 0) { continue; } sum += i; } return sum;", 25) { TestName = "Continue_SkipsRemainingBody" },
        new("var skipped = 0; var processed = 0; for (var i = 0; i < 10; i++) { if (i < 5) { skipped++; continue; } processed++; } return skipped * 100 + processed;", 505) { TestName = "Continue_StillExecutesIncrement" },
        new("var total = 0; for (var i = 0; i < 3; i++) { for (var j = 0; j < 5; j++) { if (j == 3) { continue; } total++; } } return total;", 12) { TestName = "Continue_InNestedLoop" },
        new("var product = 1; for (var i = 1; i <= 10; i++) { if (i % 2 == 0) { continue; } product *= i; } return product;", 945) { TestName = "Continue_WithAccumulator" },
        new("var sum = 0; for (var i = 1; i <= 20; i++) { if (i % 2 == 0) { continue; } if (i > 10) { break; } sum += i; } return sum;", 25) { TestName = "BreakAndContinue_Combined" },
        new("var total = 0; for (var i = 1; i <= 5; i++) { if (i == 3) { continue; } for (var j = 1; j <= 5; j++) { if (j == 2) { break; } total++; } } return total;", 4) { TestName = "BreakAndContinue_InNestedLoops" },
    ];

    /// <summary>
    /// For loop expressions that should throw exceptions.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        new("{ for (var i = 0; 1; i++) { break; } return 0; }") { TestName = "NonBoolean_IntCondition" },
        new("{ for (var i = 0; \"true\"; i++) { break; } return 0; }") { TestName = "NonBoolean_StringCondition" },
        new("{ for (var i = 0; 3.14; i++) { break; } return 0; }") { TestName = "NonBoolean_DoubleCondition" },
    ];
}
