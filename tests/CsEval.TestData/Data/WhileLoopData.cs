using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// While loop test data -- basic iteration, control flow (break/continue),
/// nested loops, and error cases.
/// Shared across compiler backends.
/// </summary>
public static class WhileLoopData
{
    /// <summary>
    /// Value-producing while loop expressions with expected results.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        new("var count = 0; var i = 0; while (i < 5) { count++; i++; } return count;", 5) { TestName = "Basic_CountsToFive" },
        new("var executed = false; while (false) { executed = true; } return executed;", false) { TestName = "Basic_FalseConditionNeverExecutes" },
        new("var count = 0; var done = false; while (done == false) { count++; done = true; } return count;", 1) { TestName = "Basic_SingleIteration" },
        new("var a = 0; var b = 1; var count = 0; while (count < 10) { var temp = a + b; a = b; b = temp; count++; } return b;", 89) { TestName = "Fibonacci" },
        new("var total = 0; var i = 0; while (i < 3) { var local = i * 2; total += local; i++; } return total;", 6) { TestName = "Variables_ScopedToBlock" },
        new("var x = 0; var y = 0; while (x < 5 && y < 3) { x++; y++; } return x;", 3) { TestName = "AndCondition" },
        new("var x = 0; var y = 10; while (x < 5 || y > 5) { x++; y--; } return x;", 5) { TestName = "OrCondition" },
        new("var text = \"test\"; var count = 0; while (text != null && count < 3) { count++; } return count;", 3) { TestName = "NullCheck" },
        new("var i = 0; while (i < 100) { if (i == 5) { return i; } i++; } return -1;", 5) { TestName = "EarlyReturn" },
        new("var sum = 0; var i = 0; while (i < 10) { if (i % 2 == 0) { sum += i; } i++; } return sum;", 20) { TestName = "NestedIf" },
        new("var total = 0; var i = 0; while (i < 3) { var j = 0; while (j < 3) { total++; j++; } i++; } return total;", 9) { TestName = "NestedLoops" },
        new("var product = 1; var i = 1; while (i <= 2) { var j = 1; while (j <= 2) { var k = 1; while (k <= 2) { product *= 2; k++; } j++; } i++; } return product;", 256) { TestName = "DeeplyNested" },
        new("var str = \"\"; var i = 0; while (i < 5) { str = str + i; i++; } return str;", "01234") { TestName = "StringConcatenation" },
        new("var lines = \"\"; var i = 1; while (i <= 3) { lines = $\"{lines}Line {i}\\n\"; i++; } return lines;", "Line 1\nLine 2\nLine 3\n") { TestName = "InterpolatedString" },
        new("var count = 0; var i = 0; while (i < 3) { var now = DateTime.Now; if (now != null) { count++; } i++; } return count;", 3) { TestName = "DateTimeModule" },
        new("var n = 5; var factorial = 1; while (n > 1) { factorial *= n; n--; } return factorial;", 120) { TestName = "Factorial" },
        new("var a = 48; var b = 18; while (b != 0) { var temp = b; b = a % b; a = temp; } return a;", 6) { TestName = "GCD" },
        new("var baseNum = 2; var exp = 10; var power = 1; while (exp > 0) { power *= baseNum; exp--; } return power;", 1024) { TestName = "PowerCalculation" },
        new("var i = 0; while (i < 100) { if (i == 5) { break; } i++; } return i;", 5) { TestName = "Break_ExitsLoop" },
        new("var count = 0; while (true) { break; count++; } return count;", 0) { TestName = "Break_AtStart" },
        new("var sum = 0; var i = 1; while (i <= 10) { sum += i; if (sum > 10) { break; } i++; } return sum;", 15) { TestName = "Break_PreservesState" },
        new("var i = 0; var found = -1; while (i < 20) { if (i % 7 == 0 && i > 0) { found = i; break; } i++; } return found;", 7) { TestName = "Break_InNestedIf" },
        new("var outerCount = 0; var totalInner = 0; var i = 0; while (i < 3) { var j = 0; while (j < 10) { if (j == 2) { break; } totalInner++; j++; } outerCount++; i++; } return outerCount * 100 + totalInner;", 306) { TestName = "Break_OnlyExitsInnerLoop" },
        new("var iterations = 0; var i = 0; while (i < 1000) { iterations++; i++; if (i >= 50) { break; } } return iterations;", 50) { TestName = "Break_AfterSomeIterations" },
        new("var sum = 0; var i = 0; while (i < 10) { i++; if (i % 2 == 0) { continue; } sum += i; } return sum;", 25) { TestName = "Continue_SkipsRemainingBody" },
        new("var sum = 0; var i = 0; while (i < 20) { i++; if (i % 2 == 0) { continue; } if (i % 3 == 0) { continue; } sum += i; } return sum;", 73) { TestName = "Continue_MultipleConditions" },
        new("var total = 0; var i = 0; while (i < 3) { var j = 0; while (j < 5) { j++; if (j == 3) { continue; } total++; } i++; } return total;", 12) { TestName = "Continue_InNestedLoop" },
        new("var skipped = 0; var processed = 0; var i = 0; while (i < 10) { i++; if (i <= 5) { skipped++; continue; } processed++; } return skipped * 100 + processed;", 505) { TestName = "Continue_SkipsToConditionCheck" },
        new("var product = 1; var i = 0; while (i < 10) { i++; if (i % 2 == 0) { continue; } product *= i; } return product;", 945) { TestName = "Continue_WithAccumulator" },
        new("var count = 0; var iterations = 0; var i = 0; while (i < 5) { iterations++; i++; continue; count++; } return iterations * 100 + count;", 500) { TestName = "Continue_DoesNotAffectCondition" },
    ];

    /// <summary>
    /// While loop expressions that should throw exceptions.
    /// Signature: (string expr)
    /// </summary>
    public static IEnumerable<TestCaseData> ErrorCases() =>
    [
        new("{ while (1) { break; } return 0; }") { TestName = "NonBoolean_IntCondition" },
        new("{ while (\"true\") { break; } return 0; }") { TestName = "NonBoolean_StringCondition" },
        new("{ while (3.14) { break; } return 0; }") { TestName = "NonBoolean_DoubleCondition" },
    ];
}
