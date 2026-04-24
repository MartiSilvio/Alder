---
title: Execution model
description: Reference for Alder execution semantics: parsing, binding, caching, backend selection, constraints, control flow, and error propagation.
---

# Execution model

This page defines Alder's runtime model: expression lifecycle, cache invalidation, backend selection, constraint enforcement, control-flow handling, and error propagation.

## Evaluation lifecycle

Execution proceeds in this order:

1. Parse source text into an expression representation.
2. Create or reuse an execution context.
3. Bind the expression against the current context type shape.
4. Apply validation and optimization passes.
5. Execute through the compiled backend when available for synchronous evaluation; otherwise execute through the interpreter.
6. Enforce cancellation and execution limits during runtime.
7. Unwrap final control-flow state at the evaluation boundary.

## Entry points

Primary `AlderEngine` entry points:

- `Parse`, `TryParse`
- `TryValidate`
- `Evaluate(...)`, `Evaluate<T>(...)`, `TryEvaluate(...)`, `TryEvaluate<T>(...)`
- `EvaluateAsync(...)`, `EvaluateAsync<T>(...)`
- `TryCompile`, `Compile`
- `EvaluateWithTrace`

`AlderEval` exposes the same main operations through a lazily initialized global engine.

## Parsing

Parsing converts source text into Alder's internal expression form.

### Guarantees

- successful parsing preserves the source and parsed structure for later stages
- ordinary parse failures surface as Alder diagnostics
- `TryParse` returns `false` for ordinary parse failures
- `TryParse` rethrows cancellation and disposal exceptions
- excessive nesting that exhausts the execution stack is normalized to `ExpressionNestingDepthExceeded`

## Binding

Binding resolves the expression against the active context.

### Behavior

- binding determines types, conversions, call targets, member access, and legality rules
- binding is sensitive to the current context type shape, not only the source text
- semantic binding failures surface as `BindingFailed` with diagnostics
- unsupported binding paths are recorded as unavailable rather than retried indefinitely

### Dynamic execution

Binding may intentionally preserve runtime operations when static information is insufficient to make a deterministic decision. That is part of the execution model, not a bind failure.

## Reuse and invalidation

Alder reuses work conservatively.

### Bound reuse

A bound result is reused only when:

- the same `AlderContext` instance is used
- the context's type-inference version still matches

Otherwise Alder rebinds.

### Pipeline reuse

Post-binding pipeline output is reused only for the exact bound-expression instance that produced it.

### Compiled reuse

Compiled output is reused only while it remains current for the relevant context type surface. When that surface changes, Alder recompiles before using compiled execution again.

## Backend selection

### Synchronous evaluation

Synchronous `Evaluate(...)` uses:

- compiled execution when a compiler is configured
- interpreter execution when no compiler is configured

### Asynchronous evaluation

`EvaluateAsync(...)` uses the interpreter in the current implementation, regardless of compiler configuration.

### Trace evaluation

`EvaluateWithTrace` binds the expression, applies security validation, and executes with tracing enabled.

## Runtime pipeline

Interpreter evaluation runs:

1. bind or reuse a bound expression
2. apply the bound-tree pipeline
3. execute through the interpreter
4. update execution counters and fallback metrics
5. unwrap final control-flow state

Pipeline order:

- security validation
- constant folding
- dead-branch elimination

Compiled evaluation runs:

1. check whether compiled output is present and current
2. compile when required
3. invoke the compiled delegate

Compilation pipeline order:

- security validation
- constant folding
- dead-branch elimination
- conversion insertion

If Alder cannot produce an invocable compiled delegate, compiled execution throws the stored failure when available, otherwise `StrictCompilationFailed`.

## Context versioning

Context versioning determines whether prior semantic and compiled work remains valid.

The type-inference version increases when the visible declared-type surface changes, including:

- defining a variable with a different declared type
- introducing a new variable
- clearing a scope

Value-only updates through `Set(...)` do not change the type-inference version.

Version changes invalidate:

- bound reuse for that context
- compiled output tied to that type surface
- precompiled expressions that depend on the old version

## Constraints

Each evaluation resets constraint state from the configured limits.

### Statement and timeout checks

Runtime checks:

- increment the statement count
- throw `OperationCanceledException` when cancellation is requested
- throw `AlderExecutionLimitException(Statements)` when the statement limit is exceeded
- throw `AlderExecutionLimitException(Timeout)` when the timeout limit is exceeded

### Loop checks

Loop enforcement:

- increments the loop-iteration count
- throws `AlderExecutionLimitException(LoopIterations)` when the loop limit is exceeded

Equivalent checks are applied by both execution backends.

## Control flow

Alder represents non-local control flow internally and propagates it until an owning construct consumes it.

### Behavior

- loops consume `Break` and `Continue` that belong to them
- blocks resolve in-block `goto` targets
- other constructs propagate control flow they do not own
- the evaluation boundary unwraps the final control-flow state

### Boundary rule

At the outer evaluation boundary:

- non-`Goto` control flow returns its associated value
- escaped `Goto` becomes `LabelNotFound`

## Error handling

### Parse failures

- ordinary parse failures surface as Alder exceptions
- stack-exhausting nesting is normalized as described above

### Binding failures

- semantic failures are normalized to `BindingFailed` with diagnostics
- unsupported binding is recorded as unavailable state

### Runtime failures

- interpreted execution enriches missing source locations from the most recently evaluated expression
- compiled execution enriches missing source locations from the root expression span

### Try APIs

- `TryParse`, `TryEvaluate`, and `TryValidate` return `false` for ordinary failures
- they rethrow `OperationCanceledException`
- they rethrow `ObjectDisposedException`

## Guarantees

- `AlderConfig` is fixed at engine construction time
- security validation runs in both interpreter and compiled pipelines
- asynchronous execution uses the interpreter in the current implementation
- root-engine disposal prevents further execution through dependent child engines
- context storage is concurrent, but Alder does not promise atomic multi-variable snapshots across parent-scope mutation

## Related pages

- [Architecture](/explanation/architecture/)
- [Binding system](/explanation/binding-system/)
- [Configuration](/reference/configuration/)
