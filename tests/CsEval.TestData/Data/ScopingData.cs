using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S7.7 -- Scopes, S13.4 -- Block statements.
/// Test data for variable scoping in for/while loops, if statements,
/// block expressions, parent scope access, and break/continue.
/// Shared across compiler backends.
/// </summary>
public static class ScopingData
{
    /// <summary>
    /// For loop scoping -- initializer accessible in body, condition, and increment.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ForLoopCases() =>
    [
        new("""
            {
                var sum = 0;
                for (var i = 1; i <= 5; i = i + 1) {
                    sum = sum + i;
                }
                return sum;
            }
            """, 15)
            { TestName = "ForLoop_InitializerAccessibleInBody" },
        new("""
            {
                var count = 0;
                for (var i = 0; i < 10; i = i + 1) {
                    count = count + 1;
                }
                return count;
            }
            """, 10)
            { TestName = "ForLoop_InitializerAccessibleInCondition" },
        new("""
            {
                var count = 0;
                for (var i = 0; i < 5; i = i + 2) {
                    count = count + 1;
                }
                return count;
            }
            """, 3)
            { TestName = "ForLoop_InitializerAccessibleInIncrement" },
    ];

    /// <summary>
    /// While loop scoping.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> WhileLoopCases() =>
    [
        new("""
            {
                var total = 0;
                var i = 0;
                while (i < 3)
                    i = i + 1;
                return i;
            }
            """, 3)
            { TestName = "WhileLoop_SingleStatementAssignment_ScopedCorrectly" },
    ];

    /// <summary>
    /// If statement scoping -- then/else branch variables independent.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> IfStatementCases() =>
    [
        new("""
            {
                var result = 0;
                if (true) {
                    var x = 10;
                    result = x;
                } else {
                    var x = 20;
                    result = x;
                }
                return result;
            }
            """, 10)
            { TestName = "IfStatement_ThenAndElseBranchVariables_Independent" },
    ];

    /// <summary>
    /// Parent scope access from child blocks.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> ParentScopeAccessCases() =>
    [
        new("""
            {
                var total = 0;
                for (var i = 0; i < 5; i = i + 1) {
                    total = total + i;
                }
                return total;
            }
            """, 10)
            { TestName = "ForLoop_CanAccessParentScopeVariables" },
        new("""
            {
                var x = 10;
                if (true) {
                    x = x + 5;
                }
                return x;
            }
            """, 15)
            { TestName = "IfStatement_CanAccessParentScopeVariables" },
    ];

    /// <summary>
    /// Block scoping via nested if statements.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BlockScopingCases() =>
    [
        new("""
            {
                var outer = 1;
                if (true) {
                    var middle = 2;
                    if (true) {
                        var inner = 3;
                        outer = outer + middle + inner;
                    }
                }
                return outer;
            }
            """, 6)
            { TestName = "NestedBlocks_ViaNestedIf_ProperScoping" },
    ];

    /// <summary>
    /// Break and continue with proper scoping in loops.
    /// Signature: (string expr, object expected)
    /// </summary>
    public static IEnumerable<TestCaseData> BreakContinueCases() =>
    [
        new("""
            {
                var sum = 0;
                for (var i = 0; i < 10; i = i + 1) {
                    var temp = i;
                    sum = sum + temp;
                    if (i == 4) {
                        break;
                    }
                }
                return sum;
            }
            """, 10)
            { TestName = "ForLoop_BreakPreservesParentScope" },
        new("""
            {
                var total = 0;
                var i = 0;
                while (true) {
                    var x = i * 2;
                    i = i + 1;
                    if (i == 3) {
                        continue;
                    }
                    if (i > 5) {
                        break;
                    }
                    total = total + x;
                }
                return total;
            }
            """, 16)
            { TestName = "WhileLoop_BreakAndContinue_ScopeCleanedUp" },
    ];
}
