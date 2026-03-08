# Method-Heavy Warm Path Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign compiled method-call execution so `Functions/MathMix` and other method-heavy warm paths avoid runtime object-dispatch overhead and become competitive with DynamicExpresso.

**Architecture:** Introduce a typed call-lowering pipeline in compiled mode that binds callable targets and argument conversions at compile time, emitting direct expression-tree calls for statically-known call sites. Keep dynamic/runtime invoker behavior as a strict fallback path for unsupported/ambiguous sites to preserve semantics. Add micro-benchmarks to isolate dispatch, conversion, and overload costs so optimization is evidence-driven.

**Tech Stack:** C# 12, .NET 8, Expression Trees, BenchmarkDotNet, NUnit

---

### Task 1: Baseline Profiling + Isolation Benchmarks

**Files:**
- Modify: `benchmarks/CsEval.Benchmarks/BenchmarkScenarioCatalog.Comparable.cs`
- Modify: `benchmarks/CsEval.Benchmarks/ComparableExecutionBenchmarks.cs`
- Create: `benchmarks/CsEval.Benchmarks/MethodDispatchMicroBenchmarks.cs`
- Modify: `benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj` (if include needed)

**Step 1: Write the failing benchmark-isolation test coverage**

```csharp
[Test]
public void MathMix_CompiledPath_DoesNotUseRuntimeInvoker_WhenDirectBindable()
{
    // Assert compiled tree for MathMix has no Runtime.MethodInvoker.Invoke* call.
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~MathMix_CompiledPath_DoesNotUseRuntimeInvoker_WhenDirectBindable" -v minimal`
Expected: FAIL because current compiled path still emits runtime invoker call(s).

**Step 3: Add micro-benchmarks to isolate bottlenecks**

```csharp
[MemoryDiagnoser]
public class MethodDispatchMicroBenchmarks
{
    [Benchmark] public object RuntimeInvoker_StaticMathAbsMax();
    [Benchmark] public object DirectEmit_StaticMathAbsMax();
    [Benchmark] public object RuntimeInvoker_OverloadResolutionOnly();
    [Benchmark] public object RuntimeInvoker_CoercionOnly();
}
```

**Step 4: Run benchmarks to capture baseline**

Run: `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*MethodDispatchMicroBenchmarks*"`
Expected: Baseline numbers identifying dominant hot path(s).

**Step 5: Commit**

```bash
git add benchmarks/CsEval.Benchmarks/MethodDispatchMicroBenchmarks.cs tests/CsEval.Test/Compilation/CompiledHotPathRegressionTests.cs benchmarks/CsEval.Benchmarks/BenchmarkScenarioCatalog.Comparable.cs
git commit -m "bench: add method dispatch microbenchmarks and hot-path regression guard"
```

### Task 2: Typed Call-Lowering Pipeline (Core Rewrite)

**Files:**
- Modify: `src/CsEval/Compilation/CompilerUnits/DirectEmitCompilerUnit.cs`
- Modify: `src/CsEval/Compilation/CompilerUnits/ExpressionCompilerUnit.cs`
- Modify: `src/CsEval/Compilation/CompilerContext.cs`
- Modify: `src/CsEval/Interpretation/TypeInferrer.cs` (only if required by typed-call inference consistency)
- Test: `tests/CsEval.Test/Compilation/CompiledHotPathRegressionTests.cs`

**Step 1: Write failing tests for direct-bind method-heavy path**

```csharp
[Test]
public void MathMix_CompiledPath_UsesDirectBoundStaticCalls()
{
    // Verify no MethodInvoker.InvokeCall/InvokeMemberCall for Math.Abs + Math.Max pattern.
}

[Test]
public void MethodHeavyChain_CompiledPath_UsesDirectBoundCalls_WithTypedArgs()
{
    // Verify no runtime method/member dispatch for bindable chain.
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~MathMix_CompiledPath_UsesDirectBoundStaticCalls|Name~MethodHeavyChain_CompiledPath_UsesDirectBoundCalls_WithTypedArgs" -v minimal`
Expected: FAIL on current runtime invoker calls.

**Step 3: Implement minimal typed call lowering**

```csharp
// In CompileCall: try typed-call lowering first.
if (_directEmit.TryEmitTypedDirectCall(call, out var direct))
    return direct;

// In DirectEmitCompilerUnit:
// - bind method once using inferred argument types + target type
// - emit typed argument conversion expressions
// - emit direct Expression.Call and return object-converted result
```

**Step 4: Run tests and regression suite**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CompiledHotPathRegressionTests" -v minimal`
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~ManyChainedPropertyAccesses_ShouldNotStackOverflow" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/CsEval/Compilation/CompilerUnits/DirectEmitCompilerUnit.cs src/CsEval/Compilation/CompilerUnits/ExpressionCompilerUnit.cs src/CsEval/Compilation/CompilerContext.cs tests/CsEval.Test/Compilation/CompiledHotPathRegressionTests.cs
git commit -m "perf: rewrite compiled call lowering to typed direct pipeline for bindable sites"
```

### Task 3: Conversion + Invocation Cleanup (No Redundant Work)

**Files:**
- Modify: `src/CsEval/Runtime/MethodInvoker.cs`
- Modify: `src/CsEval/Runtime/TypeHelpers.cs`
- Modify: `src/CsEval/Runtime/RuntimeHelpers.cs`
- Test: `tests/CsEval.Test/Runtime/OverloadResolutionTests.cs`
- Test: `tests/CsEval.Test/Types/NumericPromotionTests.cs`

**Step 1: Write failing tests for conversion churn / overload behavior**

```csharp
[Test]
public void RuntimeInvoker_DoesNotReCoerceAlreadyTypedArgs()
{
    // Ensure semantics identical while avoiding redundant coercion path.
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "Name~RuntimeInvoker_DoesNotReCoerceAlreadyTypedArgs" -v minimal`
Expected: FAIL.

**Step 3: Refactor runtime path cleanly**

```csharp
// Keep behavior; remove duplicated per-argument conversion loops.
// Consolidate into one conversion utility with early-exit when arg type already matches.
```

**Step 4: Run correctness tests**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~OverloadResolutionTests" -v minimal`
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~NumericPromotionTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/CsEval/Runtime/MethodInvoker.cs src/CsEval/Runtime/TypeHelpers.cs src/CsEval/Runtime/RuntimeHelpers.cs tests/CsEval.Test/Runtime/OverloadResolutionTests.cs tests/CsEval.Test/Types/NumericPromotionTests.cs
git commit -m "refactor: unify runtime argument conversion and trim invocation overhead"
```

### Task 4: Benchmark Iteration Loop Until Competitive

**Files:**
- Modify: `benchmarks/CsEval.Benchmarks/*` (only if new isolations needed)
- Modify: `tests/CsEval.Test/Compilation/CompiledHotPathRegressionTests.cs` (if new guard assertions required)

**Step 1: Run core benchmark set**

Run:
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*ComparableExecutionBenchmarks*Functions/MathMix*"`
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*ComparableExecutionBenchmarks*StaticMethodCall*"`
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*ComparableExecutionBenchmarks*ChainedStaticCalls*"`
Expected: clear latency gap reduction vs DynamicExpresso/Flee.

**Step 2: If still behind, add one focused micro-benchmark and repeat**

```csharp
[Benchmark] public object BoundCall_Only();
[Benchmark] public object BoundCallPlusConversion();
```

**Step 3: Re-run targeted tests + benchmark after each tweak**

Run:
- `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CompiledHotPathRegressionTests" -v minimal`
- `dotnet run -c Release --project benchmarks/CsEval.Benchmarks/CsEval.Benchmarks.csproj -- --filter "*MethodDispatchMicroBenchmarks*"`

**Step 4: Run full safety net at convergence**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj -v minimal`
Expected: PASS all tests.

**Step 5: Final commit**

```bash
git add src/CsEval benchmarks/CsEval.Benchmarks tests/CsEval.Test docs/plans/2026-03-08-method-heavy-warm-path-redesign.md
git commit -m "perf: redesign method-heavy warm path to typed direct invocation pipeline"
```
