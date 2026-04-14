---
title: Architecture
description: Architectural explanation of Alder’s parse-bind-execute pipeline, backend split, and extension surfaces.
---

# Architecture

## Context

Alder is a runtime C# expression engine with one language pipeline and two execution backends. The public surface centers on `AlderEngine` (`src/Alder/AlderEngine*.cs`) and the static facade `AlderEval` (`src/Alder/AlderEval.cs`).

The architecture enforces semantic consistency: parse once, bind once per context version, then execute through interpreter or compiled backend without changing language semantics. Parity tests verify this across interpreted and compiled modes (`tests/Alder.Test/Parity/ParityTests.cs`).

## Core design

The system uses a staged pipeline:

1. Parse source text into AST.
2. Bind AST into typed bound nodes.
3. Run bound-tree pipeline passes.
4. Execute with interpreter or compiled delegate.

The binder is the semantic pivot. It emits resolved nodes when static type information is available and dynamic nodes when it is not.

- Resolved call path: `BoundResolvedCallExpr` with selected method (`src/Alder/Binding/BoundNodes/BoundResolvedCallExpr.cs`).
- Dynamic call path: `BoundDynamicCallExpr` fallback (`src/Alder/Binding/Binders/CallBinder.cs`).

Security is enforced as a bound-tree pipeline pass (`SecurityValidationPass`) before backend execution. Policy checks are backend-agnostic (`src/Alder/Security/SecurityValidationPass.cs`).

## Component breakdown

`Alder` (core runtime)

- Parsing: lexer/parser produce `Expr` AST (`src/Alder/Parsing/ExpressionParser.cs` and related parsers).
- Binding: binder and binders produce `BoundExpr` trees (`src/Alder/Binding/*`).
- Runtime core: context, type resolution, reflection metadata, method/member/index access (`src/Alder/Runtime/*`).
- Interpreter: `BoundEvaluator` and `EvaluationContext` execute bound nodes (`src/Alder/Interpretation/*`).
- Security and constraints: sandbox validation pass and runtime enforcement (`src/Alder/Security/*`, `src/Alder/Runtime/Semantics/ExecutionRuntime.cs`).

`Alder.Compiled` (compiled backend)

- Compiler registration: `UseCompiler()` (`src/Alder.Compiled/AlderCompiledExtensions.cs`).
- Compilation path: expression-tree emission and delegate compilation (`src/Alder.Compiled/Compilation/ILExpressionCompiler.cs`, `Emission/*`).
- Compiled APIs: precompiled wrappers/delegates with standard context semantics (`src/Alder.Compiled/AlderCompiledEngineExtensions.cs`).

`Alder.Generators` (source generators)

- Dispatch generators emit switch-based dispatch for binder/evaluator/emitter from attribute-marked classes.
- AOT generator emits `TypedDispatch` metadata and context glue (`src/Alder.Generators/AlderSourceGenerator.cs`).

## Execution flow

Entry surfaces:

- Instance API: `AlderEngine.Evaluate(...)`, `EvaluateAsync(...)`, `Parse(...)`, `TryValidate(...)`.
- Global API: `AlderEval` mirrors engine methods with lazy singleton initialization.

Synchronous evaluation (`AlderEngine.Evaluation.cs`):

1. Parse source to `AlderExpression` when needed.
2. Resolve target engine/context; per-call variables run in a child engine.
3. Create and reset `ExecutionConstraintState`.
4. Execute compiled path when compiler is configured; otherwise execute interpreter path.
5. Unwrap top-level `ControlFlowSignal` at boundary.

Interpreter path:

1. Retrieve or create bound expression per `AlderContext` type version (`AlderExpression` cache keyed by context).
2. Run interpretation pipeline: security, constant folding, dead-branch elimination.
3. Execute via `BoundEvaluator` and `EvaluationContext.Dispatch(...)`.

Compiled path:

1. Validate compiled info against context type version.
2. If stale or missing, bind and run compilation pipeline (includes conversion insertion pass).
3. Emit expression tree and compile `CompiledExpressionDelegate`.
4. Invoke delegate with `(AlderContext, AlderConfig, ExecutionConstraintState, CancellationToken)`.

AOT and runtime dispatch path:

- Method/member/index/constructor operations try `TypedDispatch` first (`TypedDispatchHelper`, `MethodInvoker`, `MemberAccess`).
- On miss, runtime falls back to reflection-based resolution and invocation.
- Generated contexts integrate through `AlderOptions.Aot.UseGeneratedContext(...)`; tests cover generated dispatch and reflection fallback (`tests/Alder.Test/AOT/GeneratedContextIntegrationTests.cs`).

## Extension points

Engine configuration (`AlderOptions`):

- `Modules`: register module types and members.
- `Functions`: register delegate-based global functions.
- `Types`: add assemblies/namespaces and extension-method containers.
- `Aot`: add generated type contexts.
- `ServiceProvider`: integrate DI-based module instance resolution.

Execution backend:

- `UseCompiler()` enables compiled backend.
- `IExpressionCompiler` selects expression-tree compilation strategy.

Compile-time extension points:

- `[BindsNode]` contributes binder dispatch cases.
- `[EvaluatesNode]` contributes interpreter dispatch cases.
- `[EmitsNode]` contributes compiled emitter dispatch cases.
- `[AlderRegistered]` contributes AOT type registrations for generated contexts.

## Constraints and invariants

- Context and type-versioning:
  - Variable declared-type changes increment context version (`AlderContext.Define`).
  - Bound and compiled artifacts are reused only when version matches.
- Child scoping model:
  - Root context uses concurrent storage; child scopes use dictionary storage.
  - Child engines inherit config and visible variables while isolating additional variable definitions.
- Try-API behavior:
  - `TryParse` and `TryEvaluate` suppress ordinary failures.
  - Cancellation and disposal are never swallowed (`ShouldRethrowTryApiException`).
- Security invariant:
  - Security validation runs as a bound-tree pass in both backends.
- Control-flow invariant:
  - `ControlFlowSignal` propagates through constructs and is unwrapped only at function/evaluation boundaries.
- Constraint enforcement:
  - Statement, loop-iteration, timeout, and cancellation checks execute in runtime semantics (`ExecutionRuntime`).
  - Compiled invocation uses `ExecutionConstraintState`; tests verify limit enforcement in compiled delegates (`tests/Alder.Test/Core/CompiledDelegateTests.cs`).

## Tradeoffs

Single semantic pipeline with dual execution backends:

- Benefit: one language front-end and shared diagnostics across interpreter and compiler.
- Cost: backend-specific gaps require continuous parity auditing.

Resolved-first binding with dynamic fallback:

- Benefit: bind-time method/member selection when static types are known.
- Cost: mixed static/dynamic expressions increase runtime dispatch complexity.

Typed dispatch with reflection fallback:

- Benefit: AOT-safe execution when generated metadata exists.
- Cost: runtime maintains multiple invocation tiers.

Static facade (`AlderEval`) and instance engine coexistence:

- Benefit: low-friction API plus explicit-engine API.
- Cost: `AlderEval` configuration is one-shot and stateful.

## Related pages

- [ECMA-334 conformance](/reference/language/ecma-conformance/)
