namespace CsEval.Test.ExceptionHandling;

/// <summary>
/// ECMA-334 §13.11 -- Exception handling via try/catch/finally statements.
/// Tests basic try/catch, typed catch with exception type filtering and inheritance,
/// finally block execution on normal and exceptional paths, nested try/catch propagation,
/// catch-when guards (section 13.11), throw/rethrow semantics (section 13.10.6),
/// and bare catch clauses across all three compilation modes.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ExceptionHandlingTests(CompilationMode mode)
{
    #region ECMA-334 §13.11 -- Basic Try/Catch

    [TestCase(
        "{ var r = \"\"; try { throw new Exception(\"test\"); } catch (Exception ex) { r = ex.Message; } return r; }",
        TestName = "TryCatch_CatchVariable_ReturnsMessage")]
    [TestCase(
        "{ var x = 0; try { x = 1; } catch (Exception) { x = 2; } return x; }",
        TestName = "TryCatch_NoException_NormalFlow")]
    [TestCase(
        "{ var x = 0; try { throw new Exception(); x = 1; } catch (Exception) { x = 2; } return x; }",
        TestName = "TryCatch_ExceptionThrown_CatchExecutes")]
    [TestCase(
        "{ var x = 0; try { x = 42; } catch (Exception) { x = 0; } return x; }",
        TestName = "TryCatch_NoException_TryBodyCompletes")]
    public async Task BasicTryCatch(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §13.11 -- Exception Type Filtering

    [TestCase(
        "{ var r = \"\"; try { throw new ArgumentException(); } catch (InvalidOperationException) { r = \"wrong\"; } catch (ArgumentException) { r = \"right\"; } return r; }",
        TestName = "TypeFilter_MatchesCorrectType")]
    [TestCase(
        "{ var r = \"\"; try { throw new ArgumentNullException(); } catch (ArgumentException) { r = \"base\"; } return r; }",
        TestName = "TypeFilter_InheritanceMatch")]
    [TestCase(
        "{ var r = \"\"; try { throw new ArgumentException(); } catch (ArgumentNullException) { r = \"derived\"; } catch (ArgumentException) { r = \"base\"; } return r; }",
        TestName = "TypeFilter_FirstMatchWins")]
    [TestCase(
        "{ var r = \"\"; try { throw new InvalidOperationException(); } catch (ArgumentException) { r = \"arg\"; } catch (Exception) { r = \"fallback\"; } return r; }",
        TestName = "TypeFilter_FallsToBaseException")]
    [TestCase(
        "{ var r = \"\"; try { throw new ArgumentException(\"msg\"); } catch (ArgumentException ex) { r = ex.Message; } return r; }",
        TestName = "TypeFilter_TypedCatchWithVariable")]

    public async Task ExceptionTypeFiltering(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §13.11 -- Finally Block Execution

    [TestCase(
        "{ var x = 0; try { x = 1; } finally { x = 2; } return x; }",
        TestName = "Finally_RunsOnNormalPath")]
    [TestCase(
        "{ var x = 0; try { throw new Exception(); } catch (Exception) { x = 1; } finally { x = 2; } return x; }",
        TestName = "Finally_RunsAfterCatch")]
    [TestCase(
        "{ var x = 0; try { throw new Exception(); } catch (Exception) { } finally { x = 42; } return x; }",
        TestName = "Finally_SideEffectsObservable")]
    [TestCase(
        "{ var x = 0; try { x = 1; } catch (Exception) { x = -1; } finally { x = x + 10; } return x; }",
        TestName = "Finally_RunsAfterNormalTry_ModifiesValue")]

    public async Task FinallyBlockExecution(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [Test]
    [TestCase(TestName = "Finally_TryFinallyWithoutCatch_FinallyRuns")]
    public async Task TryFinally_NoException_FinallyRuns()
    {
        var expr = "{ var x = 0; try { x = 5; } finally { x = x + 10; } return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Finally_TryFinallyWithException_FinallyRunsBeforePropagation")]
    public async Task TryFinally_WithException_FinallyRunsAndExceptionPropagates()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // The finally should still run even though the exception propagates.
        // We wrap in an outer try/catch to observe both the finally side-effect and the exception.
        var expr = "{ var x = 0; try { try { throw new Exception(\"inner\"); } finally { x = 99; } } catch (Exception) { } return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    #endregion

    #region ECMA-334 §13.11 -- Nested Try/Catch

    [TestCase(
        "{ var r = \"\"; try { try { throw new ArgumentException(); } catch (InvalidOperationException) { r = \"inner\"; } } catch (ArgumentException) { r = \"outer\"; } return r; }",
        TestName = "Nested_InnerCatchMisses_OuterCatches")]
    [TestCase(
        "{ var r = \"\"; try { try { throw new ArgumentException(); } catch (ArgumentException) { r = \"inner\"; } } catch (ArgumentException) { r = \"outer\"; } return r; }",
        TestName = "Nested_InnerCatchMatches")]
    [TestCase(
        "{ var r = \"\"; try { try { throw new Exception(\"orig\"); } catch (Exception) { throw new InvalidOperationException(\"new\"); } } catch (InvalidOperationException ex) { r = ex.Message; } return r; }",
        TestName = "Nested_ExceptionInCatch_CaughtByOuter")]
    [TestCase(
        "{ var x = 0; try { try { throw new Exception(); } catch (Exception) { x = 1; } finally { x = x + 10; } } catch (Exception) { x = -1; } return x; }",
        TestName = "Nested_InnerFinallyAndOuterCatch")]

    public async Task NestedTryCatch(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [Test]
    [TestCase(TestName = "Nested_BothFinallyBlocksExecute")]
    public async Task Nested_BothFinally_Execute()
    {
        var expr = "{ var x = 0; try { try { x = 1; } finally { x = x + 10; } } finally { x = x + 100; } return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    #endregion

    #region ECMA-334 §13.11 -- Catch-When Filters

    [TestCase(
        "{ var r = 0; try { throw new Exception(\"match\"); } catch (Exception ex) when (ex.Message == \"match\") { r = 1; } catch (Exception) { r = 2; } return r; }",
        TestName = "CatchWhen_GuardMatches")]
    [TestCase(
        "{ var r = 0; try { throw new Exception(\"other\"); } catch (Exception ex) when (ex.Message == \"match\") { r = 1; } catch (Exception) { r = 2; } return r; }",
        TestName = "CatchWhen_GuardFails_FallsToNextCatch")]
    [TestCase(
        "{ var r = 0; try { throw new ArgumentException(\"test\"); } catch (ArgumentException ex) when (ex.Message == \"test\") { r = 1; } return r; }",
        TestName = "CatchWhen_TypedCatchWithGuard")]
    [TestCase(
        "{ var r = 0; try { throw new Exception(\"a\"); } catch (Exception ex) when (ex.Message == \"b\") { r = 1; } catch (Exception ex) when (ex.Message == \"a\") { r = 2; } catch (Exception) { r = 3; } return r; }",
        TestName = "CatchWhen_MultipleGuards_SecondMatches")]

    public async Task CatchWhenFilters(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §13.10.6 -- Throw and Rethrow

    [TestCase(
        "{ var r = \"\"; try { try { throw new ArgumentException(\"inner\"); } catch (ArgumentException) { throw; } } catch (Exception ex) { r = ex.Message; } return r; }",
        TestName = "Rethrow_PreservesMessage")]
    [TestCase(
        "{ var r = \"\"; try { try { throw new Exception(\"orig\"); } catch (Exception ex) { throw ex; } } catch (Exception ex) { r = ex.Message; } return r; }",
        TestName = "ThrowEx_PreservesMessage")]
    [TestCase(
        "{ var r = \"\"; try { try { throw new ArgumentException(\"test\"); } catch (ArgumentException) { throw; } } catch (ArgumentException ex) { r = ex.Message; } return r; }",
        TestName = "Rethrow_CaughtByTypedOuterCatch")]

    public async Task ThrowAndRethrow(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region ECMA-334 §13.11 -- Bare Catch

    [TestCase(
        "{ var r = 0; try { throw new Exception(); } catch { r = 1; } return r; }",
        TestName = "BareCatch_CatchesAll")]
    [TestCase(
        "{ var r = 0; try { throw new ArgumentException(); } catch { r = 1; } return r; }",
        TestName = "BareCatch_CatchesDerivedTypes")]
    [TestCase(
        "{ var r = \"\"; try { throw new InvalidOperationException(); } catch (ArgumentException) { r = \"arg\"; } catch { r = \"bare\"; } return r; }",
        TestName = "BareCatch_AsLastFallback")]

    public async Task BareCatch(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    #endregion

    #region Control Flow Through Try Blocks

    [TestCase(
        "{ var x = 0; for (var i = 0; i < 3; i++) { try { if (i == 1) continue; x += i; } catch (Exception) { } } return x; }",
        TestName = "ControlFlow_ContinueInsideTry")]
    [TestCase(
        "{ var x = 0; for (var i = 0; i < 5; i++) { try { if (i == 2) break; x += i; } catch (Exception) { } } return x; }",
        TestName = "ControlFlow_BreakInsideTry")]
    [TestCase(
        "{ var x = 0; for (var i = 0; i < 3; i++) { try { x += i; } finally { x += 10; } } return x; }",
        TestName = "ControlFlow_FinallyInLoop")]

    public async Task ControlFlowThroughTry(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    [Test]
    [TestCase(TestName = "ControlFlow_ReturnFromTryBlock")]
    public async Task ReturnFromTryBlock()
    {
        var expr = "{ try { return 42; } catch (Exception) { return -1; } }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "ControlFlow_ReturnFromTryWithFinally")]
    public async Task ReturnFromTryWithFinally()
    {
        // In C#, finally runs but cannot change the return value.
        // We verify the finally runs by checking a side-effect wrapper.
        var expr = "{ var x = 0; try { x = 1; return x; } finally { x = 99; } }";
        // Roslyn scripting returns the value from the return statement, not the finally modification.
        // The return value is captured before the finally block modifies x.
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    #endregion

    #region Complex Scenarios

    [Test]
    [TestCase(TestName = "Complex_TryCatchFinally_AllParts")]
    public async Task TryCatchFinally_AllParts()
    {
        var expr = "{ var r = \"\"; try { r = \"try\"; throw new Exception(); } catch (Exception) { r = r + \"-catch\"; } finally { r = r + \"-finally\"; } return r; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_MultipleCatchClauses_OrderMatters")]
    public async Task MultipleCatchClauses_OrderMatters()
    {
        var expr = "{ var r = \"\"; try { throw new ArgumentNullException(); } catch (ArgumentNullException) { r = \"null\"; } catch (ArgumentException) { r = \"arg\"; } catch (Exception) { r = \"ex\"; } return r; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_DeepNesting_ThreeLevels")]
    public async Task DeepNesting_ThreeLevels()
    {
        var expr = @"{ var r = """";
            try {
                try {
                    try { throw new ArgumentException(""deep""); }
                    catch (InvalidOperationException) { r = ""level1""; }
                }
                catch (FormatException) { r = ""level2""; }
            }
            catch (ArgumentException ex) { r = ex.Message; }
            return r; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_CatchWhen_WithMemberAccess")]
    public async Task CatchWhen_WithMemberAccess()
    {
        var expr = "{ var r = 0; try { throw new ArgumentException(\"hello world\"); } catch (ArgumentException ex) when (ex.Message.Length > 5) { r = 1; } catch (Exception) { r = 2; } return r; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_CatchWhen_GuardFalse_FallsThroughAll")]
    public async Task CatchWhen_GuardFalse_FallsThroughAll()
    {
        // When guard fails on all typed catches, the bare catch handles it
        var expr = "{ var r = 0; try { throw new Exception(\"x\"); } catch (Exception ex) when (ex.Message == \"y\") { r = 1; } catch { r = 2; } return r; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_FinallyAfterRethrow")]
    public async Task FinallyAfterRethrow()
    {
        var expr = "{ var x = 0; try { try { throw new Exception(); } catch (Exception) { x = 1; throw; } finally { x = x + 10; } } catch (Exception) { x = x + 100; } return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_TryCatch_NoExceptionThrown_CatchSkipped")]
    public async Task TryCatch_NoExceptionThrown_CatchSkipped()
    {
        var expr = "{ var x = 0; try { x = 5; } catch (Exception) { x = -1; } return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_NestedTryFinally_BothRun")]
    public async Task NestedTryFinally_BothRun()
    {
        var expr = "{ var x = \"\"; try { x = x + \"a\"; try { x = x + \"b\"; } finally { x = x + \"c\"; } } finally { x = x + \"d\"; } return x; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    [Test]
    [TestCase(TestName = "Complex_ExceptionInFinally_ReplacesOriginal")]
    public async Task ExceptionInFinally_ReplacesOriginal()
    {
        // In C#, an exception thrown in a finally block replaces the original exception
        var expr = "{ var r = \"\"; try { try { throw new ArgumentException(\"first\"); } finally { throw new InvalidOperationException(\"second\"); } } catch (InvalidOperationException ex) { r = ex.Message; } return r; }";
        await TestHelpers.RunCSharpParityTestAsync(expr, mode);
    }

    #endregion
}
