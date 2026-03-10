# Bound-Only Cutover Design

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate CsEval to a bound-only interpreter/compiler pipeline with no legacy AST evaluator/compiler fallback, while preserving behavior and performance.

**Architecture:** Keep the migration phased but code changes incremental and test-gated. First close missing bound semantics and hot-path planning gaps, then remove fallback plumbing and legacy compilation/evaluation units. Validation gates are mandatory at each phase.

**Tech Stack:** C#/.NET 8, NUnit, BenchmarkDotNet, expression trees.

---

## Problem Summary

The codebase already has strong bound infrastructure (`Binder`, `BoundEvaluator`, `BoundExpressionEmitter`) but still relies on legacy fallback for unsupported bound cases. A previous hard cutover regressed switch compilation and hot-path member/index dispatch allocations.

Root issues:

1. Missing bound compiled coverage for switch statements.
2. Insufficient typed plan retention for chained member/index access in hot paths.
3. Legacy fallback still participates in execution and strict-compile diagnostics.

## Target End State

1. Interpreted mode executes only `Binder` + `BoundEvaluator`.
2. Compiled/StrictCompiled execute only `Binder` + `BoundExpressionEmitter`.
3. No runtime fallback to legacy AST evaluator/compiler paths.
4. Full test parity maintained; hot-path regression tests remain green.

## Migration Strategy

### Phase 1: Bound Semantic Parity

- Implement compiled emission for `BoundSwitchStatementExpr`.
- Preserve diagnostic behavior for switch fall-through (`CS0163`) and strict-compile errors.
- Keep fallback paths in place during this phase for risk isolation.

### Phase 2: Bound Hot Path Parity

- Improve binder/emitter planning for member/index chains to avoid runtime `MemberAccess.GetMember/GetIndex` where static plans are available.
- Ensure object graph and string chain compiled hot-path tests remain green for both behavior and allocation thresholds.

### Phase 3: Bound-Only Cutover

- Remove legacy evaluator fallback from engine interpreted execution.
- Remove AST compiler fallback from compiled pipeline.
- Delete obsolete compiler units and evaluator implementations no longer referenced.

## Non-Goals

- Changing public API semantics.
- Introducing benchmark-only special cases.
- Hiding algorithmic issues behind caches.

## Validation Gates

Before cutover:

1. `BoundCompilationTests`, `SwitchStatementTests`, `ILCompilationTests` pass.
2. `CompiledHotPathRegressionTests` pass including allocation assertions.
3. `LinqTests` pass (dynamic selector compatibility).

After cutover:

1. Full test suite passes.
2. Benchmark-critical slices match or beat pre-cutover warm-path baselines.
