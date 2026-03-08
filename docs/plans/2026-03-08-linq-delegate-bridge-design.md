# LINQ Delegate Bridge Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove structural LINQ execution overhead by eliminating per-invocation lambda adapter allocations in compiled mode.

**Architecture:** Keep current expression/runtime architecture and redesign only the lambda-delegate bridge. Compiled lambdas will expose arity-specialized invoke delegates (0/1/2) and `LambdaDelegateConverter` will bind to those directly, falling back to object-array invocation for unsupported arities.

**Tech Stack:** C# (.NET 8), expression tree compilation, BenchmarkDotNet, xUnit.

---

### Task 1: Extend compiled-lambda runtime shape
**Files:**
- Modify: `src/CsEval/Runtime/ValueTypes.cs`
- Modify: `src/CsEval/Compilation/CompilerContext.cs`

Steps:
1. Extend `CompiledLambdaValue` with optional fast delegates for arities 0/1/2.
2. Update constructor reflection metadata in compiler context.
3. Keep existing object-array delegate for compatibility fallback.

### Task 2: Emit specialized compiled lambda delegates
**Files:**
- Modify: `src/CsEval/Compilation/CompilerUnits/ExpressionCompilerUnit.LambdaAndLiterals.cs`

Steps:
1. Keep existing generic compiled lambda emission.
2. For arities 0/1/2, additionally emit direct delegate forms without object-array indexing.
3. Store all delegate variants in `CompiledLambdaValue`.

### Task 3: Add fast runtime invoke entry points
**Files:**
- Modify: `src/CsEval/Runtime/MethodInvoker.cs`

Steps:
1. Add internal `InvokeCompiledLambda0/1/2` methods.
2. Use direct delegates when available, fallback to existing `InvokeCompiledLambda`.

### Task 4: Rewire delegate conversion to fast paths
**Files:**
- Modify: `src/CsEval/Runtime/LambdaDelegateConverter.cs`

Steps:
1. For compiled lambdas, generate wrapper delegates that call `InvokeCompiledLambda0/1/2` directly.
2. Keep generic object-array path only for higher arities.
3. Preserve behavior and signature validation.

### Task 5: Validation
**Files:**
- Test: `tests/CsEval.Test/*` (existing suite)

Steps:
1. Build project.
2. Run focused tests for runtime/compilation/lambda/LINQ coverage.
3. Run focused LINQ benchmarks (`WhereCount`, `WhereSelectSum`, `SelectSum`) and compare against previous local baseline.
