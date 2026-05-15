---
title: Async execution
description: How Alder evaluates asynchronous expressions through its interpreter-backed async execution path, and how that path relates to await semantics, constraints, cancellation, and the compiled backend.
---

# Async execution

Alder's async execution path is a dedicated interpreter-backed runtime path. It evaluates expressions that contain asynchronous work, preserves `await` semantics inside evaluation, carries cancellation and execution limits through that work, and returns results through `ValueTask`-based APIs that match the shape of the computation being performed.

The entry point is `EvaluateAsync(...)`. Alder parses and binds asynchronous expressions through the same front end it uses for synchronous execution, then runs the bound tree through the interpreter so asynchronous operations can suspend and resume inside expression nodes, loops, conditionals, and user-provided call targets. `System.Linq.Expressions` does not provide that async execution model, so async evaluation remains on the interpreter even when the engine is configured with the compiled backend.

## Execution boundary

Async evaluation begins at the same semantic boundary as synchronous evaluation:

1. parse source into Alder syntax
2. bind syntax against the active context
3. run security validation and optimization passes
4. execute the processed bound tree through the async interpreter path

The parser, binder, diagnostics, security policy, and execution constraints are shared. What changes is the execution mechanism after binding. `Evaluate(...)` dispatches to the compiled backend when a compiler is configured. `EvaluateAsync(...)` dispatches to the interpreter regardless of compiler configuration.

Alder's compiled backend lowers bound code into `System.Linq.Expressions` and then into delegates. That route is well-suited to synchronous execution; separate export APIs use expression trees for provider integration. `System.Linq.Expressions` does not provide the async execution model Alder needs, so asynchronous evaluation awaits inside the interpreter. The interpreter can await intermediate results inside the runtime tree, propagate asynchronous control flow, and continue evaluation after the awaited operation completes.

## `EvaluateAsync(...)` as a public surface

`AlderEngine` exposes async evaluation in the same shapes as synchronous evaluation:

- expression text or pre-parsed `AlderExpression`
- dictionary variables
- anonymous-object variables whose property types are preserved for binding
- positional `@0`, `@1`, ... variables
- typed result conversion through `EvaluateAsync<T>(...)`

The return type is `ValueTask`, which fits the execution model well. Some expressions complete without suspension, and Alder can return those results without allocating a fresh `Task` for every call. Other expressions await asynchronous operations and complete asynchronously in the ordinary way.

<!-- test: EvaluateAsync_ExecutesTextAndParsedExpressions -->
```csharp
var engine = new AlderEngine();

var total = await engine.EvaluateAsync<int>("""
    var a = await Task.FromResult(20);
    var b = await Task.FromResult(22);
    return a + b;
    """);
```

Pre-parsed expressions remain reusable on the async path:

<!-- test: EvaluateAsync_ExecutesTextAndParsedExpressions -->
```csharp
var engine = new AlderEngine();
var expr = engine.Parse("await Task.FromResult(@0 + @1)");

var result = await engine.EvaluateAsync<int>(expr, 30, 12);
```

Per-call variables follow the same scoping rule as synchronous evaluation. Alder applies them in a child execution context, so the engine's shared scope remains unchanged after the call returns.

## `await` support

Alder binds `await` as a first-class language construct. The binder validates legal placement, infers the awaited result type when it can, and emits a dedicated bound await node. That matters because `await` participates in the language, not in host-side wrapping.

The runtime consequences are the ones an experienced C# user expects:

- `await Task<T>` produces `T`
- `await Task` produces no intermediate value and yields `null` at the expression boundary when it is the final result
- `await ValueTask<T>` produces `T`
- `await ValueTask` produces `null`
- awaiting a non-awaitable value is a diagnostic error
- `await` is disallowed inside the body of a `lock` statement

<!-- test: Await_ProducesValuesAndNullForTaskResults -->
```csharp
var result = await engine.EvaluateAsync("""await Task.FromResult("hello")""");
// result == "hello"
```

<!-- test: Await_ProducesValuesAndNullForTaskResults -->
```csharp
var result = await engine.EvaluateAsync("await Task.Delay(1)");
// result == null
```

If the operand is not awaitable, Alder reports `CS4001`. If `await` appears inside a `lock` body, Alder reports `CS1996`. Those failures come from Alder's normal diagnostic pipeline; async execution does not switch to a looser or more ad hoc error model.

## Task and `ValueTask` behavior

The async runtime distinguishes between two cases:

1. the expression explicitly awaits an asynchronous value
2. the expression merely returns one

Only `await` unwraps. If an expression calls an async method but does not await the returned task, Alder returns that task object as the expression result.

<!-- test: EvaluateAsync_ReturnsRawTask_WhenExpressionDoesNotAwait -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Modules.Register<PricingModule>("pricing");
});

var raw = await engine.EvaluateAsync("pricing.ComputeAsync(10, 20)");
// raw is a Task<int>

var value = await engine.EvaluateAsync("await pricing.ComputeAsync(10, 20)");
// value == 30
```

That rule is important when embedding Alder into larger systems. `EvaluateAsync(...)` does not recursively await every task-shaped value it encounters. It preserves ordinary language semantics:

- `await x` unwraps `x`
- `x` returns `x`

The same rule applies to injected variables. A host can pass `Task<T>` or `ValueTask<T>` into the expression context and let the expression decide whether to await them.

## Async control flow

Because async execution runs through the interpreter, `await` can appear inside ordinary language constructs and suspend at that point in evaluation:

- blocks
- conditionals
- null-coalescing expressions
- loops
- local variable initialization
- delegate and module calls that return tasks

<!-- test: Await_CanSuspendInsideControlFlow -->
```csharp
var result = await engine.EvaluateAsync("""
    var sum = 0;
    for (var i = 1; i <= 3; i++)
    {
        sum += await doubleAsync(i);
    }
    return sum;
    """);
```

This is the operational reason Alder keeps async execution on the interpreter. Evaluation can suspend inside the control-flow graph, resume with the awaited value, and continue through the remaining bound nodes with the same execution context, loop state, and diagnostics machinery.

The control-flow model remains the same one Alder uses elsewhere. Internal control-flow signals propagate through the tree and unwrap at the evaluation boundary. Async execution changes when a node completes, not what control flow means.

## Relationship to the compiled backend

An engine configured with `UseCompiler()` still uses the interpreter for `EvaluateAsync(...)`.

- synchronous `Evaluate(...)` uses the compiled backend when a compiler is configured
- asynchronous `EvaluateAsync(...)` uses the interpreter
- tracing uses the interpreter

Alder's compiled backend lowers bound trees through `System.Linq.Expressions`, which fits synchronous delegates and expression-tree export. It does not provide a runtime evaluator that can suspend and resume across awaited operations inside an arbitrary bound tree. Async expressions execute through the interpreter while continuing to share the same parser, binder, validation passes, security checks, and constraint model.

## Cancellation behavior

`EvaluateAsync(...)` accepts a `CancellationToken`, and Alder carries that token through the async interpreter path. Cancellation is checked before execution begins and again during runtime evaluation at the same execution checkpoints Alder uses for constraints and statement progression.

Operationally:

- an already-cancelled token causes `OperationCanceledException`
- long-running loops are interruptible
- cancellation is not wrapped in `TargetInvocationException`
- try-style APIs rethrow cancellation; cancellation is host control flow, not a `false` expression result

That behavior matters for host integration. Cancellation is treated as control flow owned by the host, not as an ordinary expression failure. If the host requests cancellation, Alder stops evaluation and propagates `OperationCanceledException`.

## Execution constraints in async flows

Async execution uses the same `ExecutionConstraints` model as synchronous interpretation:

- `MaxStatements`
- `MaxLoopIterations`
- `MaxTimeout`

Those checks remain part of the evaluation while the expression is suspended and resumed through awaited work. Statement counting, loop-iteration counting, timeout measurement, and cancellation checks all belong to the shared runtime model, not to one backend only.

This gives async execution the same operational guardrails as the rest of Alder:

- runaway loops can be interrupted
- long-running evaluations can time out
- bounded environments can enforce statement and iteration budgets

Awaited external work remains external work. Alder measures the evaluation's wall-clock duration through its constraint timer and enforces timeout at evaluation checkpoints. It does not forcibly interrupt an external task that ignores the host's cancellation token.

## Exceptions and diagnostics

Async execution converges on the same diagnostic model as the rest of Alder. Parse errors, binding failures, security policy rejections, semantic errors, and execution-limit failures still surface as `AlderException` or `AlderExecutionLimitException` with structured diagnostic information.

That includes source enrichment. When interpreted async evaluation throws an `AlderException` without a populated span, the evaluator enriches the exception from the most recently evaluated bound expression so callers still get useful line, column, and span data.

The main exception classes you see on the async path are:

- `AlderException` for parse, bind, semantic, and runtime diagnostic failures
- `AlderExecutionLimitException` for statement, timeout, and loop-iteration limits
- `OperationCanceledException` for host-driven cancellation

Two usage rules follow from that:

- use `TryValidate(...)` when you want diagnostics without executing
- treat `OperationCanceledException` as cancellation, not as an Alder language failure

## Practical usage patterns

### Await task-returning modules and delegates

Async execution is the natural choice when expressions call host-provided services that already expose `Task` or `ValueTask`.

<!-- test: AsyncModuleMethods_CanBeAwaited -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Modules.Register<PricingModule>("pricing");
});

var result = await engine.EvaluateAsync("""
    var total = 0;
    for (var i = 0; i < 3; i++)
    {
        total += await pricing.ComputeAsync(i, 1);
    }
    return total;
    """);
```

### Keep reusable expressions pre-parsed

If the same async rule runs repeatedly, parse it once and evaluate the `AlderExpression` repeatedly. That removes repeated parse work while preserving the async execution path.

<!-- test: AsyncExpressions_CanReuseParsedRulesWithTypedPerCallVariables -->
```csharp
var expr = engine.Parse("""
    var value = await source();
    return value >= threshold;
    """);
```

### Use typed variables when binding quality matters

Anonymous-object variables preserve property types for binding. That produces better semantic resolution than treating every per-call value as `object`.

<!-- test: AsyncExpressions_CanReuseParsedRulesWithTypedPerCallVariables -->
```csharp
var ok = await engine.EvaluateAsync<bool>(
    "await job.IsReadyAsync() && retries < maxRetries",
    new { job, retries = 1, maxRetries = 3 });
```

### Distinguish task-returning results from awaited results

If the host wants the task object, return it. If the host wants the completed value, await it inside the expression. The difference is semantic and observable.

## When to choose sync versus async

Choose `Evaluate(...)` when the expression is synchronous and you want Alder's synchronous backend behavior, including compiled execution when configured.

Choose `EvaluateAsync(...)` when:

- the expression contains `await`
- the expression calls task-returning services, modules, or delegates
- the host wants cancellation and execution limits carried through awaited work
- suspension inside loops, conditionals, or local assignments is part of the runtime requirement

As a rule of thumb, synchronous business rules, hot paths, and delegate-oriented execution fit `Evaluate(...)`; task-aware expression logic fits `EvaluateAsync(...)`.

If the expression is synchronous, `EvaluateAsync(...)` still works. Alder can evaluate a non-async expression through the async API and return the result through `ValueTask`. That is useful for unified host pipelines.

## Practical boundary

Async execution is Alder's interpreter-backed subsystem for expressions whose runtime contract includes suspension. It shares parsing, binding, diagnostics, security validation, and execution constraints with the rest of the engine, while providing an execution path that can await asynchronous operations inside the expression tree itself.

## Related pages

- [Compiled backend](./compiled-backend.md)
- [Architecture](./architecture.md)
- [Execution model](../reference/execution-model.md)
- [Security model](../operations/security-model.md)
