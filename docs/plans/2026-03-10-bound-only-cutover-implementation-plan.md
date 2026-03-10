# Bound-Only Cutover Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Deliver a production-safe bound-only runtime/compiler pipeline with no legacy AST fallback.

**Architecture:** Implement in three stages: semantic parity, hot-path parity, then cutover/deletion. Every stage is protected by targeted tests before moving forward.

**Tech Stack:** C#/.NET 8, NUnit, expression trees.

---

### Task 1: Add Bound Compiled Switch Statement Support

**Files:**
- Modify: `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`
- Modify: `src/CsEval.Compiled/Compilation/BoundRuntimeMethodCache.cs`
- Test: `tests/CsEval.Test/Compilation/ILCompilationTests.cs`
- Test: `tests/CsEval.Test/Runtime/SwitchStatementTests.cs`

**Step 1: Write failing coverage for bound switch compile path**
- Add tests that assert `CompiledPipeline.Bound` for switch statement scenarios in strict compiled mode.

**Step 2: Verify RED**
- Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj -c Debug --filter "FullyQualifiedName~ILCompilationTests|FullyQualifiedName~SwitchStatementTests" -v minimal`
- Expect: failures for switch in bound-only path.

**Step 3: Implement switch emission in bound emitter**
- Add `BoundSwitchStatementExpr` emission and correct control-flow semantics (`break`, `continue`, `return`, fall-through diagnostics).

**Step 4: Verify GREEN**
- Re-run switch/compilation filters; expect pass.

### Task 2: Restore Compiled Hot-Path Member/Index Planning

**Files:**
- Modify: `src/CsEval/Binding/Binder.cs`
- Modify: `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`
- Test: `tests/CsEval.Test/Compilation/CompiledHotPathRegressionTests.cs`

**Step 1: Lock red on hot-path regressions**
- Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj -c Debug --filter "FullyQualifiedName~CompiledHotPathRegressionTests" -v minimal`
- Expect: runtime dispatch/allocation failures on object graph chains when plans degrade.

**Step 2: Improve chain plan retention**
- Ensure member/index static types and plans propagate through chain nodes so compiled emitter can choose direct access for typed segments.

**Step 3: Verify green**
- Re-run hot-path regression tests; ensure allocation and runtime dispatch assertions pass.

### Task 3: Remove Legacy Execution/Compilation Fallbacks

**Files:**
- Modify: `src/CsEval/CsEvalEngine.cs`
- Modify: `src/CsEval/CsEvalExpression.cs`
- Modify: `src/CsEval.Compiled/Compilation/ILExpressionCompiler.cs`
- Delete/Modify: legacy compiler/evaluator units no longer referenced
- Test: `tests/CsEval.Test/Runtime/BoundExecutionTests.cs`
- Test: `tests/CsEval.Test/Compilation/BoundCompilationTests.cs`

**Step 1: Keep tests strict**
- Do not add ignores or weaken expectations; keep parity assertions.

**Step 2: Cut interpreted fallback**
- Route interpreted mode through bound execution only.

**Step 3: Cut compiled AST fallback**
- Route compiled mode through bound compilation only after Task 1/2 are green.

**Step 4: Remove dead legacy code**
- Delete unused evaluator/compiler units and references.

**Step 5: Verify green**
- Run core migration suites:
  - `dotnet test tests/CsEval.Test/CsEval.Test.csproj -c Debug --filter "FullyQualifiedName~BoundCompilationTests|FullyQualifiedName~BoundExecutionTests|FullyQualifiedName~CompiledHotPathRegressionTests|FullyQualifiedName~LinqTests|FullyQualifiedName~SwitchStatementTests|FullyQualifiedName~ILCompilationTests" -v minimal`

### Task 4: Full Validation

**Files:**
- No functional edits expected; fix only if failures remain.

**Step 1: Full suite**
- Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj -c Debug -v minimal`

**Step 2: Benchmark-critical check**
- Run targeted benchmark set for object graph/string chain and switch-heavy scenarios.

**Step 3: Final cleanup**
- Remove dead comments/unused members introduced during migration.
- Keep naming and folder structure consistent.
