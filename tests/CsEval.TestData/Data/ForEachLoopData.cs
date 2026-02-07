using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ForEach loop test data -- basic iteration, control flow (break/continue),
/// nested loops, closures, and mixed type handling.
/// Note: Uses CsEval collection expression syntax [1,2,3] -- engine-only (not Roslyn parity).
/// Shared across compiler backends.
/// </summary>
public static class ForEachLoopData
{
    /// <summary>
    /// Value-producing foreach loop expressions with expected results.
    /// Note: Engine-only tests (collection expressions not supported in Roslyn scripting foreach).
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ValueCases() =>
    [
        new("var sum = 0; foreach (var item in [1, 2, 3, 4, 5]) { sum = sum + item; } return sum;", 15) { TestName = "Basic_Iteration" },
        new("var executed = false; foreach (var item in []) { executed = true; } return executed;", false) { TestName = "EmptyCollection_NeverExecutes" },
        new("var count = 0; foreach (var item in [42]) { count = count + 1; } return count;", 1) { TestName = "SingleElement_ExecutesOnce" },
        new("int? lastItem = null; foreach (var item in [10, 20, 30]) { lastItem = item; } return lastItem;", 30) { TestName = "Item_AccessibleInBody" },
        new("foreach (var item in [1, 2, 3, 4, 5]) { if (item == 3) { return item; } } return -1;", 3) { TestName = "EarlyReturn" },
        new("var evenSum = 0; foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) { if (item % 2 == 0) { evenSum = evenSum + item; } } return evenSum;", 30) { TestName = "NestedIf_SumOfEvenNumbers" },
        new("var total = 0; foreach (var i in [1, 2, 3]) { foreach (var j in [10, 20, 30]) { total = total + i * j; } } return total;", 360) { TestName = "NestedForeach" },
        new("var total = 0; foreach (var item in [1, 2, 3]) { for (var i = 0; i < 3; i = i + 1) { total = total + item; } } return total;", 18) { TestName = "NestedWithFor" },
        new("var total = 0; foreach (var item in [1, 2, 3]) { var count = 0; while (count < 3) { total = total + item; count = count + 1; } } return total;", 18) { TestName = "NestedWithWhile" },
        new("var types = \"\"; foreach (var item in [1, \"hello\", true, null]) { if (item == null) { types = types + \"null,\"; } else { types = types + \"val,\"; } } return types;", "val,val,val,null,") { TestName = "MixedTypes_WithNullCheck" },
        new("var str = \"\"; foreach (var item in [1, 2, 3, 4, 5]) { str = str + item; } return str;", "12345") { TestName = "StringConcatenation" },
        new("var lines = \"\"; foreach (var i in [1, 2, 3]) { lines = $\"{lines}Line {i}\\n\"; } return lines;", "Line 1\nLine 2\nLine 3\n") { TestName = "InterpolatedString" },
        new("var sum = 0; foreach (var item in [1, 2, 3, 4, 5]) sum = sum + item; return sum;", 15) { TestName = "SingleStatementBody" },
        new("var lastItem = 0; foreach (var item in [1, 2, 3, 4, 5]) { lastItem = item; if (item == 3) { break; } } return lastItem;", 3) { TestName = "Break_ExitsLoop" },
        new("var count = 0; foreach (var item in [1, 2, 3, 4, 5]) { count = count + 1; break; } return count;", 1) { TestName = "Break_AtStartExitsAfterFirstIteration" },
        new("var sum = 0; foreach (var item in [1, 2, 3, 4, 5]) { sum = sum + item; if (sum > 6) { break; } } return sum;", 10) { TestName = "Break_PreservesVariableState" },
        new("var outerCount = 0; var totalInner = 0; foreach (var i in [1, 2, 3]) { foreach (var j in [10, 20, 30, 40, 50]) { if (j == 30) { break; } totalInner = totalInner + 1; } outerCount = outerCount + 1; } return outerCount * 100 + totalInner;", 306) { TestName = "Break_OnlyExitsInnerLoop" },
        new("var sum = 0; foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) { if (item % 2 == 0) { continue; } sum = sum + item; } return sum;", 25) { TestName = "Continue_SkipsRemainingBody" },
        new("var skipped = 0; var processed = 0; foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]) { if (item <= 5) { skipped = skipped + 1; continue; } processed = processed + 1; } return skipped * 100 + processed;", 505) { TestName = "Continue_SkipsToNextItem" },
        new("var total = 0; foreach (var i in [1, 2, 3]) { foreach (var j in [1, 2, 3, 4, 5]) { if (j == 3) { continue; } total = total + 1; } } return total;", 12) { TestName = "Continue_InNestedLoopOnlyAffectsInner" },
        new("var funcs = []; foreach (var i in [1, 2, 3]) { funcs = [..funcs, () => i]; } return funcs[0]() * 100 + funcs[1]() * 10 + funcs[2]();", 123) { TestName = "Closure_CapturesCorrectValuePerIteration" },
        new("var funcs = []; var multiplier = 10; foreach (var i in [1, 2, 3]) { funcs = [..funcs, () => i * multiplier]; } return funcs[0]() + funcs[1]() + funcs[2]();", 60) { TestName = "Closure_WithExternalVariable" },
        new("var funcs = []; foreach (var i in [1, 2]) { foreach (var j in [10, 20]) { funcs = [..funcs, () => i * j]; } } return funcs[0]() + funcs[1]() + funcs[2]() + funcs[3]();", 90) { TestName = "Closure_InNestedLoop" },
        new("var sum = 0; foreach (var item in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]) { if (item % 2 == 0) { continue; } if (item > 10) { break; } sum = sum + item; } return sum;", 25) { TestName = "BreakAndContinue_Combined" },
        new("var total = 0; foreach (var i in [1, 2, 3, 4, 5]) { if (i == 3) { continue; } foreach (var j in [1, 2, 3, 4, 5]) { if (j == 2) { break; } total = total + 1; } } return total;", 4) { TestName = "BreakAndContinue_InNestedLoops" },
    ];
}
