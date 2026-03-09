# Unified Binding + Dual Executor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign CsEval so interpreted (AOT-safe) and compiled (IL) modes share one semantic binding pipeline, eliminating duplicate resolution logic and preserving full feature parity.

**Architecture:** Add a new core binding layer that lowers AST into typed bound nodes (`BoundExpr`) with deterministic conversion/call/member/index plans. Interpreted execution becomes a bound-node executor in core, while the compiled addon emits IL from the same bound representation. Runtime helpers remain low-level primitives; semantic policy decisions move into binder services.

**Tech Stack:** C# 12, .NET 8, NUnit, BenchmarkDotNet

---

### Task 1: Establish parity and perf safety net before refactor

**Files:**
- Create: `tests/CsEval.Test/Parity/ExecutionModeParityTests.cs`
- Create: `tests/CsEval.Test/Binding/BinderParityFixture.cs`
- Modify: `benchmarks/CsEval.Benchmarks/MicroBenchmarks.cs`
- Create: `benchmarks/CsEval.Benchmarks/BindingMicroBenchmarks.cs`

**Step 1: Write failing interpreted-vs-compiled parity tests**

```csharp
[Test]
public void StandardSyntax_ShouldMatchAcrossModes()
{
    var expr = "Math.Abs(x - y) + z";
    var vars = new Dictionary<string, object?> { ["x"] = -5, ["y"] = 10, ["z"] = 3 };

    var interpreted = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Interpreted });
    var compiled = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.StrictCompiled }.UseCompiled());

    Assert.That(interpreted.Evaluate(expr, vars), Is.EqualTo(compiled.Evaluate(expr, vars)));
}
```

**Step 2: Run the new parity test and confirm initial red**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~StandardSyntax_ShouldMatchAcrossModes" -v minimal`  
Expected: FAIL (temporary while suite scaffolding and mode wiring are being introduced).

**Step 3: Add binding-cost and call-site micro-benchmarks**

```csharp
[MemoryDiagnoser]
public class BindingMicroBenchmarks
{
    [Benchmark] public object Bind_MethodHeavy();
    [Benchmark] public object Bind_ExtendedAliasHeavy();
    [Benchmark] public object Execute_Bound_MethodHeavy();
}
```

**Step 4: Run benchmark smoke to capture baseline**

Run: `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*BindingMicroBenchmarks*"`  
Expected: Benchmark table generated for baseline comparison.

**Step 5: Commit**

```bash
git add tests/CsEval.Test/Parity/ExecutionModeParityTests.cs tests/CsEval.Test/Binding/BinderParityFixture.cs benchmarks/CsEval.Benchmarks/BindingMicroBenchmarks.cs benchmarks/CsEval.Benchmarks/MicroBenchmarks.cs
git commit -m "test: add execution parity and binding microbenchmark safety net"
```

### Task 2: Introduce bound-node model and binder entry point

**Files:**
- Create: `src/CsEval/Binding/BoundExpr.cs`
- Create: `src/CsEval/Binding/BoundNodes/BoundLiteralExpr.cs`
- Create: `src/CsEval/Binding/BoundNodes/BoundIdentifierExpr.cs`
- Create: `src/CsEval/Binding/BoundNodes/BoundBinaryExpr.cs`
- Create: `src/CsEval/Binding/BindingContext.cs`
- Create: `src/CsEval/Binding/Binder.cs`
- Modify: `src/CsEval/CsEvalExpression.cs`

**Step 1: Write failing binder shape tests**

```csharp
[Test]
public void Binder_ShouldProduceBoundBinary_ForSimpleArithmetic()
{
    var bound = Bind("x + 2");
    Assert.That(bound, Is.TypeOf<BoundBinaryExpr>());
}
```

**Step 2: Run test to verify red**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~Binder_ShouldProduceBoundBinary_ForSimpleArithmetic" -v minimal`  
Expected: FAIL because binder classes do not exist yet.

**Step 3: Implement minimal binder + bound node skeleton**

```csharp
internal abstract record BoundExpr(Type StaticType);
internal sealed record BoundBinaryExpr(TokenType Op, BoundExpr Left, BoundExpr Right, Type ResultType) : BoundExpr(ResultType);

internal sealed class Binder
{
    public BoundExpr Bind(Expr expr, BindingContext context) => expr switch
    {
        LiteralExpr l => new BoundLiteralExpr(l.Value),
        IdentifierExpr i => BindIdentifier(i, context),
        BinaryExpr b => BindBinary(b, context),
        _ => throw new CsEvalException($"Unsupported binding node: {expr.GetType().Name}")
    };
}
```

**Step 4: Run binder tests to verify green**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Binding" -v minimal`  
Expected: PASS for initial binder-node shape tests.

**Step 5: Commit**

```bash
git add src/CsEval/Binding src/CsEval/CsEvalExpression.cs tests/CsEval.Test/Binding
git commit -m "feat: add bound expression model and binder entry point"
```

### Task 3: Centralize method/member/index binding decisions

**Files:**
- Create: `src/CsEval/Binding/Plans/BoundCallPlan.cs`
- Create: `src/CsEval/Binding/Plans/BoundMemberPlan.cs`
- Create: `src/CsEval/Binding/Plans/BoundIndexPlan.cs`
- Create: `src/CsEval/Binding/Services/CallBinderService.cs`
- Create: `src/CsEval/Binding/Services/MemberBinderService.cs`
- Modify: `src/CsEval/Binding/Binder.cs`
- Modify: `src/CsEval/Runtime/MethodInvoker.cs`
- Modify: `src/CsEval/Runtime/MemberAccess.cs`

**Step 1: Write failing tests for overload/conversion/member-plan determinism**

```csharp
[Test]
public void CallBinder_ShouldChooseSameOverload_AsRuntimeInvoker()
{
    var plan = BindCall("Math.Max(x, y)", x: 1, y: 2L);
    Assert.That(plan.SelectedMethod.Name, Is.EqualTo("Max"));
    Assert.That(plan.ArgumentConversions.Count, Is.EqualTo(2));
}
```

**Step 2: Run targeted tests and confirm red**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~CallBinder_ShouldChooseSameOverload_AsRuntimeInvoker" -v minimal`  
Expected: FAIL while call/member plan services are not implemented.

**Step 3: Implement binder services and plan records**

```csharp
internal sealed record BoundCallPlan(
    MethodInfo Method,
    ImmutableArray<BoundConversionPlan> ArgumentConversions,
    bool RequiresInstanceTarget);
```

**Step 4: Run runtime + binding suites**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Binding" -v minimal`
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Runtime.OverloadResolutionTests" -v minimal`  
Expected: PASS, with identical behavior to previous resolution semantics.

**Step 5: Commit**

```bash
git add src/CsEval/Binding src/CsEval/Runtime/MethodInvoker.cs src/CsEval/Runtime/MemberAccess.cs tests/CsEval.Test/Binding tests/CsEval.Test/Runtime/OverloadResolutionTests.cs
git commit -m "refactor: centralize call/member/index binding decisions in core binder"
```

### Task 4: Move interpreted execution to bound executor

**Files:**
- Create: `src/CsEval/Interpretation/BoundEvaluator.cs`
- Modify: `src/CsEval/Interpretation/Evaluator.cs`
- Modify: `src/CsEval/CsEvalEngine.cs`
- Modify: `src/CsEval/CsEvalExpression.cs`

**Step 1: Write failing tests proving interpreted path uses bound execution**

```csharp
[Test]
public void Interpreted_ShouldExecuteBoundPlan_ForMethodHeavyExpression()
{
    var result = EvalInterpreted("Math.Abs(x) + Math.Max(y, z)", -4, 2, 3);
    Assert.That(result, Is.EqualTo(7));
    Assert.That(GetBoundPlanCacheHits(), Is.GreaterThan(0));
}
```

**Step 2: Run test for red**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~Interpreted_ShouldExecuteBoundPlan_ForMethodHeavyExpression" -v minimal`  
Expected: FAIL until engine/evaluator pipeline is rewired.

**Step 3: Implement bound executor path in interpreted mode**

```csharp
// CsEvalEngine.Evaluate
var bound = expression.GetOrCreateBound(context, _options);
return _options.CompilationMode == CompilationMode.Interpreted
    ? BoundEvaluator.Execute(bound, executionContext, _options, cancellationToken)
    : ExistingCompiledOrFallbackFlow(...);
```

**Step 4: Run interpreted and parity suites**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Interpretation" -v minimal`
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Parity.ExecutionModeParityTests" -v minimal`  
Expected: PASS.

**Step 5: Commit**

```bash
git add src/CsEval/Interpretation src/CsEval/CsEvalEngine.cs src/CsEval/CsEvalExpression.cs tests/CsEval.Test/Interpretation tests/CsEval.Test/Parity/ExecutionModeParityTests.cs
git commit -m "refactor: execute interpreted mode via shared bound expression pipeline"
```

### Task 5: Switch compiled addon to consume bound expressions

**Files:**
- Modify: `src/CsEval.Compiled/Compilation/ILExpressionCompiler.cs`
- Create: `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`
- Modify: `src/CsEval.Compiled/Compilation/CompilerContext.cs`
- Modify: `src/CsEval.Compiled/Compilation/CompilerUnits/*` (remove AST re-binding where replaced)
- Modify: `src/CsEval/Compilation/CompiledProviderRegistry.cs` (if API needs bound entry)

**Step 1: Write failing strict-compiled parity tests for bound pipeline**

```csharp
[Test]
public void StrictCompiled_ShouldUseBoundPlan_AndMatchInterpreted()
{
    var expr = "numbers.Where(n => n > 10).Select(n => n * 2).Sum()";
    Assert.That(EvalCompiled(expr), Is.EqualTo(EvalInterpreted(expr)));
}
```

**Step 2: Run test to confirm red**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~StrictCompiled_ShouldUseBoundPlan_AndMatchInterpreted" -v minimal`  
Expected: FAIL until compiled emitter consumes bound nodes.

**Step 3: Implement bound-to-IL emitter**

```csharp
internal sealed class BoundExpressionEmitter
{
    public LinqExpression Emit(BoundExpr expr) => expr switch
    {
        BoundLiteralExpr l => LinqExpression.Constant(l.Value, l.StaticType),
        BoundBinaryExpr b => EmitBinary(b),
        BoundCallExpr c => EmitCall(c),
        _ => throw new CsEvalException($"Unsupported bound emit node: {expr.GetType().Name}")
    };
}
```

**Step 4: Run compiled + parity suites**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Compilation" -v minimal`
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Parity.ExecutionModeParityTests" -v minimal`  
Expected: PASS with strict-compiled parity maintained.

**Step 5: Commit**

```bash
git add src/CsEval.Compiled src/CsEval/Compilation/CompiledProviderRegistry.cs tests/CsEval.Test/Compilation tests/CsEval.Test/Parity/ExecutionModeParityTests.cs
git commit -m "refactor(compiled): consume shared bound expression pipeline for IL emission"
```

### Task 6: Canonicalize Extended syntax through binder lowering

**Files:**
- Modify: `src/CsEval/Binding/Binder.cs`
- Create: `src/CsEval/Binding/Lowering/ExtendedSyntaxLowering.cs`
- Modify: `src/CsEval/Parsing/TokenLexemes.cs` (central canonical lexeme usage only if needed)
- Modify: `tests/CsEval.Test/Parsing/*` and `tests/CsEval.Test/Parity/*`
- Modify: `benchmarks/CsEval.Benchmarks/ExtendedSyntaxParityBenchmarks.cs`

**Step 1: Write failing alias-parity tests**

```csharp
[TestCase("a |> inc()", "inc(a)")]
[TestCase("x not in y", "!(x in y)")]
public void ExtendedAlias_ShouldLowerToCanonicalBehavior(string extended, string canonical)
{
    Assert.That(EvalExtended(extended), Is.EqualTo(EvalExtended(canonical)));
}
```

**Step 2: Run tests and confirm red**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~ExtendedAlias_ShouldLowerToCanonicalBehavior" -v minimal`  
Expected: FAIL until lowering path is consolidated in binder.

**Step 3: Implement canonical lowering in binder**

```csharp
internal static class ExtendedSyntaxLowering
{
    public static Expr Lower(Expr expr) { /* map alias forms to canonical semantic nodes */ }
}
```

**Step 4: Run extended parity tests + benchmark smoke**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Parity" -v minimal`
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*ExtendedSyntaxParityBenchmarks*"`  
Expected: PASS and no structural overhead gaps from alias syntax.

**Step 5: Commit**

```bash
git add src/CsEval/Binding src/CsEval/Parsing/TokenLexemes.cs tests/CsEval.Test/Parity benchmarks/CsEval.Benchmarks/ExtendedSyntaxParityBenchmarks.cs
git commit -m "refactor: canonicalize extended syntax in binder lowering pipeline"
```

### Task 7: AOT hardening and dead-code cleanup

**Files:**
- Modify: `src/CsEval/Runtime/TypeCache.cs`
- Modify: `src/CsEval/Runtime/TypeResolver.cs`
- Modify: `src/CsEval/Runtime/MethodDispatchCache.cs`
- Modify: `src/CsEval/Interpretation/TypeInferrer.cs`
- Modify: `scripts/aot-smoke.sh` (only if warning-report formatting needs improvement)
- Remove/modify obsolete code paths identified during binder migration

**Step 1: Write failing AOT regression test or smoke assertion**

```csharp
[Test]
public void AotSmoke_ShouldRunInterpretedPipeline_WithoutCompiledProvider()
{
    var engine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Interpreted });
    Assert.That(engine.Evaluate("1 + 2 * 3"), Is.EqualTo(7));
}
```

**Step 2: Run AOT smoke to confirm current warnings baseline**

Run: `bash scripts/aot-smoke.sh`  
Expected: PASS output plus warning baseline captured.

**Step 3: Remove dead paths and trim/AOT-fragile reflection usage where possible**

```csharp
// Prefer deterministic metadata path + static shape checks over late-bound reflection branches
// in hot interpreter/binder primitives.
```

**Step 4: Re-run AOT smoke and relevant tests**

Run:
- `bash scripts/aot-smoke.sh`
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CsEval.Test.Runtime|FullyQualifiedName~CsEval.Test.Interpretation" -v minimal`  
Expected: PASS with no new AOT warning regressions in core-critical files.

**Step 5: Commit**

```bash
git add src/CsEval/Runtime src/CsEval/Interpretation scripts/aot-smoke.sh tests/CsEval.Test
git commit -m "refactor(aot): harden core interpreted pipeline and remove dead code paths"
```

### Task 8: End-to-end verification and benchmark convergence

**Files:**
- Modify: `benchmarks/CsEval.Benchmarks/*` (only if final benchmark coverage gaps found)
- Modify: `docs/plans/2026-03-09-unified-binding-dual-executor-design.md`
- Modify: `docs/plans/2026-03-09-unified-binding-dual-executor-implementation-plan.md`

**Step 1: Run full test suite**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj -v minimal`  
Expected: PASS all tests.

**Step 2: Run targeted benchmark suites for quality gates**

Run:
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*ComparableExecutionBenchmarks*"`
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*MicroBenchmarks*"`
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*ExtendedSyntaxParityBenchmarks*"`  
Expected: compiled mode remains competitive; interpreted mode shows directional gains; extended parity remains tight.

**Step 3: Run AOT smoke one final time**

Run: `bash scripts/aot-smoke.sh`  
Expected: PASS.

**Step 4: Update plan docs with final notes**

```markdown
- record benchmark deltas
- record any accepted tradeoffs
- record deferred work (if any) with explicit rationale
```

**Step 5: Final commit**

```bash
git add src/CsEval src/CsEval.Compiled tests/CsEval.Test benchmarks/CsEval.Benchmarks docs/plans
git commit -m "refactor: unify binding pipeline across interpreted and compiled executors"
```
