using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// Do-while loop test data -- basic iteration, control flow (break/continue),
/// nested loops, and error cases.
/// Shared across compiler backends.
/// </summary>
public static class DoWhileLoopData
{
    /// <summary>
    /// Value-producing do-while loop expressions with expected results.
    /// Note: Test method wraps expression in "{ {expr} }" for block scope.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        new("var count = 0; var i = 0; do { count = count + 1; i = i + 1; } while (i < 5); return count;", 5) { TestName = "Basic_CountsToFive" },
        new("var executed = 0; do { executed = executed + 1; } while (false); return executed;", 1) { TestName = "FalseCondition_ExecutesOnce" },
        new("var count = 0; var done = false; do { count = count + 1; if (count >= 3) { done = true; } } while (done == false); return count;", 3) { TestName = "TrueCondition_LoopsUntilFalse" },
        new("var sum = 0; var i = 10; do { sum = sum + i; i = i - 1; } while (i > 0); return sum;", 55) { TestName = "CountDown" },
        new("var i = 10; var bodyExecuted = false; do { bodyExecuted = true; } while (i < 5); return bodyExecuted;", true) { TestName = "Condition_CheckedAfterBody" },
        new("var a = 0; var b = 1; var count = 0; do { var temp = a + b; a = b; b = temp; count = count + 1; } while (count < 10); return b;", 89) { TestName = "Fibonacci" },
        new("var x = 0; var y = 0; do { x = x + 1; y = y + 1; } while (x < 5 && y < 3); return x;", 3) { TestName = "AndCondition" },
        new("var x = 0; var y = 10; do { x = x + 1; y = y - 1; } while (x < 5 || y > 5); return x;", 5) { TestName = "OrCondition" },
        new("var i = 0; do { if (i == 5) { return i; } i = i + 1; } while (i < 100); return -1;", 5) { TestName = "EarlyReturn" },
        new("var sum = 0; var i = 0; do { if (i % 2 == 0) { sum = sum + i; } i = i + 1; } while (i < 10); return sum;", 20) { TestName = "NestedIf_SumOfEvenNumbers" },
        new("var total = 0; var i = 0; do { var j = 0; do { total = total + 1; j = j + 1; } while (j < 3); i = i + 1; } while (i < 3); return total;", 9) { TestName = "NestedDoWhile" },
        new("var total = 0; var i = 0; do { var j = 0; while (j < 3) { total = total + 1; j = j + 1; } i = i + 1; } while (i < 3); return total;", 9) { TestName = "NestedWithWhile" },
        new("var total = 0; var i = 0; do { for (var j = 0; j < 3; j = j + 1) { total = total + 1; } i = i + 1; } while (i < 3); return total;", 9) { TestName = "NestedWithFor" },
        new("var str = \"\"; var i = 0; do { str = str + i; i = i + 1; } while (i < 5); return str;", "01234") { TestName = "StringConcatenation" },
        new("var lines = \"\"; var i = 1; do { lines = $\"{lines}Line {i}\\n\"; i = i + 1; } while (i <= 3); return lines;", "Line 1\nLine 2\nLine 3\n") { TestName = "InterpolatedString" },
        new("var i = 0; do i = i + 1; while (i < 5); return i;", 5) { TestName = "SingleStatementBody" },
        new("var n = 5; var factorial = 1; do { factorial = factorial * n; n = n - 1; } while (n > 1); return factorial;", 120) { TestName = "Factorial" },
        new("var power = 1; var i = 0; do { power = power * 2; i = i + 1; } while (i < 10); return power;", 1024) { TestName = "PowerOfTwo" },
        new("var i = 0; do { if (i == 5) { break; } i = i + 1; } while (i < 100); return i;", 5) { TestName = "Break_ExitsLoop" },
        new("var count = 0; do { count = count + 1; break; } while (true); return count;", 1) { TestName = "Break_AtStartExitsAfterFirstIteration" },
        new("var sum = 0; var i = 1; do { sum = sum + i; if (sum > 10) { break; } i = i + 1; } while (i <= 10); return sum;", 15) { TestName = "Break_PreservesVariableState" },
        new("var outerCount = 0; var totalInner = 0; var i = 0; do { var j = 0; do { if (j == 2) { break; } totalInner = totalInner + 1; j = j + 1; } while (j < 10); outerCount = outerCount + 1; i = i + 1; } while (i < 3); return outerCount * 100 + totalInner;", 306) { TestName = "Break_OnlyExitsInnerLoop" },
        new("var sum = 0; var i = 0; do { i = i + 1; if (i % 2 == 0) { continue; } sum = sum + i; } while (i < 10); return sum;", 25) { TestName = "Continue_SkipsRemainingBody" },
        new("var skipped = 0; var processed = 0; var i = 0; do { i = i + 1; if (i <= 5) { skipped = skipped + 1; continue; } processed = processed + 1; } while (i < 10); return skipped * 100 + processed;", 505) { TestName = "Continue_JumpsToCondition" },
        new("var total = 0; var i = 0; do { var j = 0; do { j = j + 1; if (j == 3) { continue; } total = total + 1; } while (j < 5); i = i + 1; } while (i < 3); return total;", 12) { TestName = "Continue_InNestedLoopOnlyAffectsInner" },
        new("var sum = 0; var i = 0; do { i = i + 1; if (i % 2 == 0) { continue; } if (i > 10) { break; } sum = sum + i; } while (true); return sum;", 25) { TestName = "BreakAndContinue_Combined" },
        new("var total = 0; var i = 0; do { i = i + 1; if (i == 3) { continue; } var j = 0; do { j = j + 1; if (j == 2) { break; } total = total + 1; } while (j < 5); } while (i < 5); return total;", 4) { TestName = "BreakAndContinue_InNestedLoops" },
    ];

    /// <summary>
    /// Do-while loop expressions that should throw exceptions.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        new("{ var i = 0; do { if (++i > 1) break; } while (1); return 0; }") { TestName = "NonBoolean_IntCondition" },
        new("{ var i = 0; do { if (++i > 1) break; } while (\"true\"); return 0; }") { TestName = "NonBoolean_StringCondition" },
        new("{ var i = 0; do { if (++i > 1) break; } while (3.14); return 0; }") { TestName = "NonBoolean_DoubleCondition" },
    ];
}
