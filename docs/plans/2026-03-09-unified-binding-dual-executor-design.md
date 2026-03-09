# Unified Binding + Dual Executor Design

## Context

CsEval currently evaluates through two engines:

- Interpreted tree-walking in core (`CsEval`)
- IL compilation in addon (`CsEval.Compiled`)

Both paths work, but semantic and performance-critical logic is still spread across evaluator/runtime/compiler surfaces. This increases maintenance burden and makes parity harder to guarantee for Standard/Extended syntax and Interpreted/Compiled modes.

The product is pre-release, so we can redesign structurally instead of applying incremental patching.

## Goals

1. Preserve existing language behavior and features (ECMA-focused Standard mode + Extended mode features).
2. Keep core package AOT-safe end-to-end for interpreted execution.
3. Achieve strict semantic parity between interpreted and compiled execution by sharing binding decisions.
4. Improve warm-path performance via algorithmic simplification of hot paths (binding and invocation), not cache-first shortcuts.
5. Keep architecture understandable and scalable for future syntax/features.

## Non-Goals

1. Forcing interpreted mode to match IL engines on absolute nanosecond throughput.
2. Benchmark-specific shortcuts or feature restrictions.
3. Replacing correctness safeguards with ad-hoc fast paths.

## Current Issues

1. Binding decisions (method/member/operator resolution + conversion choices) are repeated across runtime paths.
2. Evaluator and compiled pipeline still carry overlapping semantic logic, increasing drift risk.
3. Hot paths repeatedly traverse reflection-heavy resolution pipelines in method/member heavy expressions.
4. Extended syntax parity depends on multiple lowering/dispatch locations instead of one authoritative semantic layer.

## Proposed Architecture

Introduce a shared **Binding Pipeline** in core:

1. Parse expression to AST (existing parser).
2. Bind AST to a typed `BoundExpr` tree in a new binding subsystem.
3. Execute `BoundExpr` through one of two executors:
   - Interpreted executor (AOT-safe, no dynamic codegen)
   - Compiled executor (IL backend in `CsEval.Compiled`)

### Binding Subsystem

New namespace: `CsEval.Binding` (in core)

Primary components:

- `Binder`: transforms `Expr` -> `BoundExpr`
- `BindingContext`: options, resolver, sandbox, symbols, module/function metadata
- Bound node families:
  - `BoundCall`
  - `BoundMemberRead` / `BoundMemberWrite`
  - `BoundIndexRead` / `BoundIndexWrite`
  - `BoundUnary` / `BoundBinary` / `BoundLogical`
  - `BoundConversion`
  - control-flow/pattern nodes as needed
- Deterministic overload and conversion ranking in one place
- Canonical lowering for Extended aliases/operators into the same operation set used by Standard equivalents when semantics are identical

### Executor Surfaces

1. `CsEval.Interpretation`
   - Evaluator becomes an executor of `BoundExpr`.
   - Removes duplicated overload/conversion policy logic.
   - Keeps sandbox/constraints/cancellation behavior.

2. `CsEval.Compiled`
   - IL emitter consumes `BoundExpr`, not raw AST semantic re-resolution.
   - Reuses binder decisions for call/member/conversion semantics.
   - Keeps strict compile mode behavior and diagnostics.

### Runtime Primitives

`CsEval.Runtime` remains as low-level primitives:

- invocation primitives
- member/index helpers
- numeric conversion helpers
- guardrails (sandbox safety, reflection leak checks)

Policy-level selection (what to call, how to coerce) belongs to binder; runtime focuses on execution helpers.

## Data Flow

1. `CsEvalEngine.Parse` produces `CsEvalExpression` with AST.
2. First execute/compile:
   - Build or fetch bound form from expression+context version.
3. Mode dispatch:
   - Interpreted: execute bound tree
   - Compiled: emit IL from bound tree, cache compiled delegate
4. Subsequent warm calls:
   - reuse bound tree (and compiled delegate when relevant)

## AOT/IL Parity Strategy

1. Core interpreted path is fully functional with no compiled provider package.
2. Compiled provider registration remains explicit via `CsEval.Compiled`.
3. Semantic truth lives in binder (core), so AOT and IL share behavior by construction.
4. Any mode-specific unsupported feature remains explicit and tested.

## Performance Strategy

1. Eliminate repeated bind-time decision work during execution.
2. Use strongly typed call-site plans from binder to reduce reflection churn.
3. Keep targeted fast invocation primitives where structurally correct (not benchmark-specialized).
4. Cache as a secondary optimization for stable plans/delegates, never as a substitute for poor algorithms.

## Validation Plan

1. Add semantic parity tests comparing interpreted vs strict compiled across shared scenario corpus.
2. Add binder unit tests for overload resolution, conversions, extension binding, named/default/params behavior.
3. Extend micro-benchmarks to isolate:
   - binding cost
   - invocation cost
   - chained member/index access cost
   - extended syntax canonicalization overhead
4. Maintain AOT smoke checks and prevent new trim/AOT warning regressions in core-critical paths.

## Risks and Mitigations

1. **Risk:** large refactor may introduce behavior regressions.
   - **Mitigation:** TDD with parity and binder-focused tests before each subsystem rewrite step.
2. **Risk:** migration complexity between AST-based and bound-based execution.
   - **Mitigation:** staged rollout with dual-path validation during transition.
3. **Risk:** overcomplication.
   - **Mitigation:** keep binder focused on semantic decisions only; avoid speculative abstractions.

## Deliverable Outcome

After this redesign, CsEval should have:

1. One semantic decision layer for both AOT-safe interpreted and IL compiled execution.
2. Cleaner boundaries between policy (binding) and execution primitives.
3. Better maintainability for future features.
4. Performance improvements from architectural simplification of hot paths rather than shortcuts.
