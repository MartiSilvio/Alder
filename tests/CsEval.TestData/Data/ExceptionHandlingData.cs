using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S13.11 -- Exception handling via try/catch/finally statements.
/// Test data for basic try/catch, typed catch with exception type filtering,
/// finally block execution, nested try/catch, catch-when guards (S13.11),
/// throw/rethrow semantics (S13.10.6), bare catch, and control flow through try blocks.
/// Shared across compiler backends.
/// </summary>
public static class ExceptionHandlingData
{
    /// <summary>
    /// ECMA-334 S13.11 -- Basic try/catch.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> BasicTryCatchCases() =>
    [
        new("{ var r = \"\"; try { throw new Exception(\"test\"); } catch (Exception ex) { r = ex.Message; } return r; }") { TestName = "TryCatch_CatchVariable_ReturnsMessage" },
        new("{ var x = 0; try { x = 1; } catch (Exception) { x = 2; } return x; }") { TestName = "TryCatch_NoException_NormalFlow" },
        new("{ var x = 0; try { throw new Exception(); x = 1; } catch (Exception) { x = 2; } return x; }") { TestName = "TryCatch_ExceptionThrown_CatchExecutes" },
        new("{ var x = 0; try { x = 42; } catch (Exception) { x = 0; } return x; }") { TestName = "TryCatch_NoException_TryBodyCompletes" },
    ];

    /// <summary>
    /// ECMA-334 S13.11 -- Exception type filtering.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> TypeFilterCases() =>
    [
        new("{ var r = \"\"; try { throw new ArgumentException(); } catch (InvalidOperationException) { r = \"wrong\"; } catch (ArgumentException) { r = \"right\"; } return r; }") { TestName = "TypeFilter_MatchesCorrectType" },
        new("{ var r = \"\"; try { throw new ArgumentNullException(); } catch (ArgumentException) { r = \"base\"; } return r; }") { TestName = "TypeFilter_InheritanceMatch" },
        new("{ var r = \"\"; try { throw new ArgumentException(); } catch (ArgumentNullException) { r = \"derived\"; } catch (ArgumentException) { r = \"base\"; } return r; }") { TestName = "TypeFilter_FirstMatchWins" },
        new("{ var r = \"\"; try { throw new InvalidOperationException(); } catch (ArgumentException) { r = \"arg\"; } catch (Exception) { r = \"fallback\"; } return r; }") { TestName = "TypeFilter_FallsToBaseException" },
        new("{ var r = \"\"; try { throw new ArgumentException(\"msg\"); } catch (ArgumentException ex) { r = ex.Message; } return r; }") { TestName = "TypeFilter_TypedCatchWithVariable" },
    ];

    /// <summary>
    /// ECMA-334 S13.11 -- Finally block execution.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> FinallyCases() =>
    [
        new("{ var x = 0; try { x = 1; } finally { x = 2; } return x; }") { TestName = "Finally_RunsOnNormalPath" },
        new("{ var x = 0; try { throw new Exception(); } catch (Exception) { x = 1; } finally { x = 2; } return x; }") { TestName = "Finally_RunsAfterCatch" },
        new("{ var x = 0; try { throw new Exception(); } catch (Exception) { } finally { x = 42; } return x; }") { TestName = "Finally_SideEffectsObservable" },
        new("{ var x = 0; try { x = 1; } catch (Exception) { x = -1; } finally { x = x + 10; } return x; }") { TestName = "Finally_RunsAfterNormalTry_ModifiesValue" },
    ];

    /// <summary>
    /// ECMA-334 S13.11 -- Nested try/catch.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> NestedTryCatchCases() =>
    [
        new("{ var r = \"\"; try { try { throw new ArgumentException(); } catch (InvalidOperationException) { r = \"inner\"; } } catch (ArgumentException) { r = \"outer\"; } return r; }") { TestName = "Nested_InnerCatchMisses_OuterCatches" },
        new("{ var r = \"\"; try { try { throw new ArgumentException(); } catch (ArgumentException) { r = \"inner\"; } } catch (ArgumentException) { r = \"outer\"; } return r; }") { TestName = "Nested_InnerCatchMatches" },
        new("{ var r = \"\"; try { try { throw new Exception(\"orig\"); } catch (Exception) { throw new InvalidOperationException(\"new\"); } } catch (InvalidOperationException ex) { r = ex.Message; } return r; }") { TestName = "Nested_ExceptionInCatch_CaughtByOuter" },
        new("{ var x = 0; try { try { throw new Exception(); } catch (Exception) { x = 1; } finally { x = x + 10; } } catch (Exception) { x = -1; } return x; }") { TestName = "Nested_InnerFinallyAndOuterCatch" },
    ];

    /// <summary>
    /// ECMA-334 S13.11 -- Catch-when filters.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> CatchWhenCases() =>
    [
        new("{ var r = 0; try { throw new Exception(\"match\"); } catch (Exception ex) when (ex.Message == \"match\") { r = 1; } catch (Exception) { r = 2; } return r; }") { TestName = "CatchWhen_GuardMatches" },
        new("{ var r = 0; try { throw new Exception(\"other\"); } catch (Exception ex) when (ex.Message == \"match\") { r = 1; } catch (Exception) { r = 2; } return r; }") { TestName = "CatchWhen_GuardFails_FallsToNextCatch" },
        new("{ var r = 0; try { throw new ArgumentException(\"test\"); } catch (ArgumentException ex) when (ex.Message == \"test\") { r = 1; } return r; }") { TestName = "CatchWhen_TypedCatchWithGuard" },
        new("{ var r = 0; try { throw new Exception(\"a\"); } catch (Exception ex) when (ex.Message == \"b\") { r = 1; } catch (Exception ex) when (ex.Message == \"a\") { r = 2; } catch (Exception) { r = 3; } return r; }") { TestName = "CatchWhen_MultipleGuards_SecondMatches" },
    ];

    /// <summary>
    /// ECMA-334 S13.10.6 -- Throw and rethrow.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> ThrowRethrowCases() =>
    [
        new("{ var r = \"\"; try { try { throw new ArgumentException(\"inner\"); } catch (ArgumentException) { throw; } } catch (Exception ex) { r = ex.Message; } return r; }") { TestName = "Rethrow_PreservesMessage" },
        new("{ var r = \"\"; try { try { throw new Exception(\"orig\"); } catch (Exception ex) { throw ex; } } catch (Exception ex) { r = ex.Message; } return r; }") { TestName = "ThrowEx_PreservesMessage" },
        new("{ var r = \"\"; try { try { throw new ArgumentException(\"test\"); } catch (ArgumentException) { throw; } } catch (ArgumentException ex) { r = ex.Message; } return r; }") { TestName = "Rethrow_CaughtByTypedOuterCatch" },
    ];

    /// <summary>
    /// ECMA-334 S13.11 -- Bare catch.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> BareCatchCases() =>
    [
        new("{ var r = 0; try { throw new Exception(); } catch { r = 1; } return r; }") { TestName = "BareCatch_CatchesAll" },
        new("{ var r = 0; try { throw new ArgumentException(); } catch { r = 1; } return r; }") { TestName = "BareCatch_CatchesDerivedTypes" },
        new("{ var r = \"\"; try { throw new InvalidOperationException(); } catch (ArgumentException) { r = \"arg\"; } catch { r = \"bare\"; } return r; }") { TestName = "BareCatch_AsLastFallback" },
    ];

    /// <summary>
    /// Control flow through try blocks.
    /// Signature: (string expr) -- parity-only
    /// </summary>
    public static IEnumerable<TestCaseData> ControlFlowCases() =>
    [
        new("{ var x = 0; for (var i = 0; i < 3; i++) { try { if (i == 1) continue; x += i; } catch (Exception) { } } return x; }") { TestName = "ControlFlow_ContinueInsideTry" },
        new("{ var x = 0; for (var i = 0; i < 5; i++) { try { if (i == 2) break; x += i; } catch (Exception) { } } return x; }") { TestName = "ControlFlow_BreakInsideTry" },
        new("{ var x = 0; for (var i = 0; i < 3; i++) { try { x += i; } finally { x += 10; } } return x; }") { TestName = "ControlFlow_FinallyInLoop" },
    ];
}
