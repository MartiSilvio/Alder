---
title: Execution model
description: Reference for Alder execution semantics: parse, bind, cache, dispatch, constraints, control-flow, and error propagation.
---

# Execution model

## Purpose
This page defines Alder runtime execution semantics: entry points, parse and bind stages, cache lifecycles, interpreter and compiled dispatch, context versioning, constraint enforcement, control-flow signaling, and exception propagation.

## Execution lifecycle summary
Execution follows this sequence:
1. Source is parsed into an AST and wrapped in `AlderExpression`.
2. Evaluation entry points create or reuse an `AlderContext`, set `ActiveCancellationToken`, and reset `ExecutionConstraintState`.
3. Binding resolves the AST into a `BoundExpr` for the current context type-inference version.
4. The bound tree is pipeline-processed and cached by bound-node identity.
5. Synchronous execution dispatches to compiled mode only when a compiler is configured; otherwise it dispatches to interpreter mode.
6. Asynchronous execution dispatches to interpreter mode.
7. Runtime checks enforce cancellation, statement limits, timeout limits, and loop-iteration limits.
8. Internal control flow propagates through `ControlFlowSignal`; construct evaluators/emitters consume only the signals they own.
9. Entry-point boundaries unwrap final control-flow state and propagate or normalize exceptions according to API contract.

## Entry points
Primary entry points on `AlderEngine`:

- Parsing: `Parse`, `TryParse`
- Validation: `TryValidate`
- Synchronous execution: `Evaluate(...)`, `Evaluate<T>(...)`, `TryEvaluate(...)`, `TryEvaluate<T>(...)`
- Asynchronous execution: `EvaluateAsync(...)`, `EvaluateAsync<T>(...)`
- Compilation control: `TryCompile`, `Compile`
- Trace execution: `EvaluateWithTrace`

Static facade entry points on `AlderEval` forward to a lazily initialized singleton `AlderEngine`.

## Parsing
`AlderEngine.Parse` executes this path:

1. `Lexer.Tokenize()` tokenizes source text.
2. `ExpressionParser.CreateForSubExpression(tokens, languageMode)` builds the parser.
3. `Parse()` produces `Expr`.
4. `AlderExpression` stores source plus AST.

`InsufficientExecutionStackException` is translated to `AlderException(ExpressionNestingDepthExceeded)`.

`TryParse` returns `false` for ordinary parse failures and rethrows `OperationCanceledException` and `ObjectDisposedException`.

## Binding
Binding is entered from `AlderExpression.GetOrCreateBoundExpression(AlderContext)`:

1. `BindingContext` is created from `AlderContext`.
2. `Binder.Bind(ast, bindingContext)` produces a bound tree.
3. On `AlderException`, binder recovery (`BindRecovering`) reruns for diagnostics.
4. If bound diagnostics exist, binding fails with `AlderException(BindingFailed)` and attached diagnostics.

Binder dispatch is generated from `[BindsNode]` registrations (`BinderDispatchGenerator`).

`TryGetOrCreateBoundExpression` converts `BindingNotSupportedException` into sticky unavailable state (`_bindingUnavailable` + `_bindingUnavailableReason`). After this state is set, subsequent calls return `false` without rebinding.

## Bound expression lifecycle
`AlderExpression` stores:

- source and parsed AST
- bind execution/fallback counters
- per-context bound cache entries
- latest compiled metadata (`CompiledInfo`)

A bound expression is reused only when both conditions are true:

- the same `AlderContext` instance is used
- cached `Version` equals `context.GetTypeInferenceVersion()`

When either condition fails, a new bound expression is created and the cache entry is replaced.

## Caching and invalidation
Bound cache (`AlderExpression._boundExpressionCacheByContext`):

- Type: `ConditionalWeakTable<AlderContext, CachedBoundExpression>`
- Key: `AlderContext` object identity
- Validity rule: `CachedBoundExpression.Version == context.GetTypeInferenceVersion()`
- Invalidation rule: cache entry is invalidated when version check fails; replacement occurs on next bind

Pipeline cache (`AlderEngine._pipelineCache`):

- Type: `ConditionalWeakTable<BoundExpr, BoundExpr>`
- Key: bound expression object reference
- Value: post-pipeline bound expression
- Reuse rule: cached value is reused only for the exact same bound-expression object
- Invalidation rule: any newly bound `BoundExpr` object bypasses the prior cache entry

Compiled-info cache (`AlderExpression.CompiledInfo`):

- Type: single `CompiledExpressionInfo` slot on `AlderExpression`
- Validity rule: compiled info is current only when `TypeVersion == null` or `TypeVersion == context.GetTypeInferenceVersion()`
- Invalidation rule: stale version triggers compile retry in compiled execution path

Compiled-text cache (`ExpressionCache`):

- Type: bounded `ConcurrentDictionary<string, CompiledExpressionInfo>` plus FIFO queue
- Key: expression source text
- Eviction: approximate FIFO after capacity is exceeded

## Execution dispatch
Synchronous dispatch (`AlderEngine.Evaluate(AlderExpression, ...)`):

- Initializes execution context state (`ActiveCancellationToken` + `ExecutionConstraintState.Reset`)
- Dispatches to compiled execution only when `_config.Compiler != null`
- Dispatches to interpreter execution when `_config.Compiler == null`

Asynchronous dispatch (`AlderEngine.EvaluateAsync(AlderExpression, ...)`):

- Initializes execution context state (`ActiveCancellationToken` + `ExecutionConstraintState.Reset`)
- Dispatches to `EvaluateAsyncCore`
- Does not dispatch to compiled execution in the current implementation

Trace dispatch (`EvaluateWithTrace`):

- binds expression
- runs security-only pipeline (`RunSecurityOnlyPipeline`)
- executes with `BoundEvaluator` and `EvaluationTracer`

## Interpreter execution
`EvaluateCore` / `EvaluateAsyncCore` executes this sequence:

1. `TryGetOrCreateBoundExpression(...)`
2. `_pipelineCache.GetValue(...)` (`RunPipeline` on cache miss)
3. `BoundEvaluator.Evaluate(...)` or `EvaluateAsync(...)`
4. expression counters update (`RecordBoundExecution`, `RecordBoundFallback`)
5. boundary unwrapping via `UnwrapControlFlowSignal`

Interpretation pipeline order is fixed:

- `SecurityValidationPass`
- `ConstantFoldingPass`
- `DeadBranchEliminationPass`

Evaluator dispatch is generated from `[EvaluatesNode]` registrations (`EvaluatorDispatchGenerator`) and routed by `EvaluationContext.Dispatch` / `DispatchAsync`.

## Compiled execution
`ExecuteCompiledExpression` executes this sequence:

1. read `expression.CompiledInfo`
2. validate freshness with `IsCompiledInfoCurrent`
3. call `TryCompileInternal` when metadata is missing or stale
4. invoke compiled delegate `CompiledExpressionDelegate(context, config, constraintState, cancellationToken)`

Compilation pipeline order is fixed:

- `SecurityValidationPass`
- `ConstantFoldingPass`
- `DeadBranchEliminationPass`
- `ConversionInsertionPass`

`TryCompileInternal` stores semantic compile failures into `CompiledInfo` and returns `false`; it does not throw for expected failures.

Compiled execution throws when no invocable delegate is available after compile attempt: it throws stored `FailureException` when present, otherwise `StrictCompilationFailed`.

Compiled emitter dispatch is generated from `[EmitsNode]` registrations (`EmitterDispatchGenerator`) and routed by `EmissionContext.Dispatch`.

## Context versioning
Context type-inference version comes from `AlderContext._variableTypeVersion` plus parent-chain composition in `GetTypeInferenceVersion()`.

Version increments:

- `Define(name, value, inferredType)` only when declared type changes
- `DefineNew(...)`
- `ClearScope()`

Version does not increment on value-only mutation through `Set(...)`.

Version gates reuse of:

- bound cache entries (`CachedBoundExpression.Version`)
- compiled metadata (`CompiledExpressionInfo.TypeVersion`)
- `AlderCompiledExpression<T>` invocation (`CompiledExpressionStale` on mismatch)

## Constraint enforcement
Per evaluation, the engine creates a new `ExecutionConstraintState` and calls `Reset(constraints)`.

`Reset` behavior:

- always clears statement and loop counters
- starts timeout stopwatch only when `MaxTimeout > 0`

Runtime enforcement APIs:

- `ExecutionRuntime.CheckExecutionConstraints(state, constraints, ct)`
- `ExecutionRuntime.CheckLoopIterationConstraint(state, constraints)`

`CheckExecutionConstraints` guarantees:

- throws `OperationCanceledException` when cancellation is requested
- increments statement count on each call
- throws `AlderExecutionLimitException(Statements)` when `MaxStatements` is exceeded
- throws `AlderExecutionLimitException(Timeout)` when timeout is exceeded

`CheckLoopIterationConstraint` guarantees:

- increments loop-iteration count on each call
- throws `AlderExecutionLimitException(LoopIterations)` when `MaxLoopIterations` is exceeded

Interpreter loop evaluators (`while`, `for`, `foreach`) call statement and loop checks inside loop execution.

Compiled emitters inject equivalent runtime checks through generated calls that use `ConstraintStateParam` and active constraints.

## Control flow handling
Internal non-local control flow is represented by `ControlFlowSignal` with kinds:

- `Return`
- `Break`
- `Continue`
- `GotoCase`
- `GotoDefault`
- `Goto`
- `YieldReturn`
- `YieldBreak`

Interpreter rules:

- evaluators propagate signals they do not own
- loop evaluators consume `Break` and `Continue`, and propagate other kinds
- block evaluator resolves in-block `Goto` by jumping to label index in the same block

Compiled rules:

- emission uses `SignalParam` to carry control-flow state
- block and loop emitters branch on signal kind to consume owned signals and propagate others

Engine boundary rule (`UnwrapControlFlowSignal`):

- non-`Goto` signals return `signal.Value`
- escaped `Goto` throws `AlderException(LabelNotFound)`

## Error handling
Parsing:

- parser/lexer failures propagate as `AlderException`
- nesting-depth stack faults are translated to `ExpressionNestingDepthExceeded`

Binding:

- binding failures are normalized to `AlderException(BindingFailed)` with diagnostics
- `BindingNotSupportedException` is converted to bound-fallback reason by `TryGetOrCreateBoundExpression`

Interpreter runtime:

- `BoundEvaluator` enriches `AlderException` location only when exception span is empty, using `LastEvaluatedExpr`

Compiled runtime:

- `ExecuteCompiledExpression` enriches location only when exception span is empty, using root expression span and source position

Try APIs (`TryParse`, `TryEvaluate`, `TryValidate`):

- return `false` for ordinary failures
- rethrow `OperationCanceledException`
- rethrow `ObjectDisposedException`

## Constraints and guarantees
- `AlderConfig` is captured at engine construction and used by execution contexts.
- Security validation runs in interpreter and compiled pipelines.
- Async execution uses interpreter dispatch in current implementation, regardless of compiler configuration.
- Root-engine disposal clears root-owned caches (`_expressionCache`, `_typeMetadata`) and marks engine disposed.
- Child engines share root disposal state and do not execute once root is disposed.
- Variable storage in contexts uses concurrent dictionaries, but parent-scope mutation does not provide atomic multi-variable snapshots.

## Related pages
- [Architecture](/explanation/architecture/)
- [ECMA-334 conformance](/reference/language/ecma-conformance/)
