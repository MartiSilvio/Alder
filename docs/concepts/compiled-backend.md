---
title: Compiled backend
description: How Alder's compiled backend changes synchronous execution, delegate generation, and LINQ expression export.
---

# Compiled backend

Alder has two execution backends: the interpreter and the compiled backend. They share the same parser, binder, security validation, and execution constraints. Compilation changes how a bound tree runs, not what the language means. For synchronous workloads on JIT-capable runtimes, the compiled backend turns bound code into reusable delegates and supports typed delegate integration. The `Alder.Compiled` API surface also exposes typed LINQ expression-tree export for query-oriented workflows.

## Execution architecture

The interpreter walks the bound tree directly. It is used when no compiler is configured, by `EvaluateAsync(...)`, by `EvaluateWithTrace(...)`, and by AOT deployments that run with generated dispatch metadata.

The compilation path is a separate lowering pipeline over the same bound tree. When a compiler is configured, synchronous `Evaluate(...)` dispatches there. Alder still binds first and still runs semantic work before delegate generation. The pipeline combines shared preparation stages with compiler-specific lowering:

- security validation, constant folding, and dead-branch elimination shared with the interpreter path
- conversion insertion, runtime lowering, and expression-tree emission for compiled execution

The compiled provider emits a LINQ expression tree rooted in Alder's runtime contracts. The configured `IExpressionCompiler` then turns that form into a delegate. Inside emission, Alder plans local promotion and repeated-identifier hoisting before producing the root lambda.

Both backends consume the same semantic form. Security policy, execution limits, and cancellation still apply.

## Enabling compilation with `UseCompiler()`

This backend is opt-in. It is enabled by importing the compiled namespace and configuring the engine with `UseCompiler()`:

<!-- test: UseCompiler -->
```csharp
using Alder.Compiled;

var engine = new AlderEngine(options => options.UseCompiler());
```

`UseCompiler()` installs Alder's compiled provider. The overload that accepts `IExpressionCompiler` keeps Alder's lowering pipeline and replaces only the final expression-tree-to-delegate compiler. Interpreted evaluation and generated dispatch remain the AOT-oriented route; compiled execution is the additional JIT-dependent route exposed through the `Alder.Compiled` namespace.

The supported starting point is plain `UseCompiler()`. It uses Alder's default route: Alder lowers the bound tree through its compiled provider, then `DefaultExpressionCompiler` delegates final delegate generation to `LambdaExpression.Compile()`.

Compilation requires dynamic code support. `UseCompiler()` throws `PlatformNotSupportedException` when the runtime does not support dynamic code generation. In NativeAOT, IL2CPP-style, and other dynamic-code-restricted deployments, Alder's supported route is interpreted evaluation with generated dispatch metadata.

## What changes when compilation is enabled

With no compiler configured, synchronous `Evaluate(...)` binds, validates, optimizes, and executes through the interpreter.

With a compiler configured, synchronous `Evaluate(...)` binds, validates, optimizes, compiles when needed, and then invokes the generated delegate. Parsed expressions can also be compiled explicitly through `TryCompile(...)`, `Compile(...)`, or `ParseAndCompile(...)`.

Compilation changes more than execution speed. It turns parsed code into reusable runtime artifacts: cached delegates for repeated `Evaluate(...)`, `AlderCompiledExpression<T>` wrappers, and native typed delegates whose parameter types come from delegate signatures. Expression-tree export is a separate `Alder.Compiled` integration surface for provider-facing workflows; it prepares LINQ trees without selecting synchronous compiled evaluation.

One boundary matters operationally: once the engine is configured for compilation, synchronous evaluation commits to that backend. If Alder cannot produce an invocable delegate, it surfaces the stored compilation failure for that call as an `AlderException`.

## Synchronous and asynchronous behavior

Compilation changes synchronous evaluation only.

- `Evaluate(...)` uses the compiled backend when a compiler is configured.
- `EvaluateAsync(...)` uses the interpreter, even when the engine is configured with `UseCompiler()`.
- `EvaluateWithTrace(...)` runs through the interpreter after security validation.

`System.Linq.Expressions` does not provide the async execution model Alder needs, so asynchronous evaluation awaits inside the interpreter. Compiled synchronous execution stays synchronous; hosts own any background scheduling. Tracing follows the same boundary: `EvaluateWithTrace(...)` uses the interpreter so it can capture the evaluated tree, values, types, and errors directly.

## Public compiled forms

The `Alder.Compiled` API surface exposes three distinct public forms.

### Compiled expression wrappers

`engine.Compile<T>(...)` returns `AlderCompiledExpression<T>`, a reusable wrapper around the generated delegate:

```csharp
var engine = new AlderEngine(options => options.UseCompiler());
engine.SetVariable("offset", 10);

var compiled = engine.Compile<int>("x + offset");
var result = compiled.Invoke(new Dictionary<string, object?> { ["x"] = 5 });
```

The wrapper closes over the engine's context by reference. Later value updates remain visible at invocation time. Per-call variables are applied in a child context, so they do not mutate the engine's shared scope.

### Plain delegates

`CompileToFunc<T>(...)` returns a `Func<T?>` for repeated zero-parameter invocation.

`Compile<TDelegate>(code, parameterNames...)` compiles a code body into a native delegate whose parameter types come from the delegate signature. The parameter names are supplied separately:

```csharp
var fn = engine.Compile<Func<int, int>>("x * 2", "x");
```

This path supports both `Func<...>` and `Action<...>` delegates, including custom delegate types.

### Expression-tree export

`ParseAsExpression<TDelegate>(...)` exports a typed `Expression<TDelegate>` for LINQ provider and expression-tree integration:

<!-- test: ParseAsExpression -->
```csharp
Expression<Func<int, bool>> predicate =
    engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");
```

`CompileExpression<TDelegate>(...)` is the direct `ParseAsExpression(...).Compile()` path.

## Expression export and delegate generation

Typed delegate compilation and expression-tree export solve different problems.

`Compile<TDelegate>(...)` produces a native delegate for repeated in-process invocation. It accepts a code body, not lambda syntax, and binds parameter types from the delegate signature. That makes the delegate signature the stable API between the host and the compiled expression.

`ParseAsExpression<TDelegate>(...)` produces a LINQ expression tree for providers, `IQueryable`, and explicit downstream compilation. It parses in Standard mode regardless of the engine's `LanguageMode`. Engine variables visible during export are captured into the resulting tree as constants. That gives Alder a direct integration boundary with external LINQ systems.

The export flow has its own preparation sequence. Alder creates typed parameter bindings from the target delegate, binds the lambda against a query-specific runtime context, reruns the compilation pipeline over the bound body, and then exports the supported node set into a provider-facing tree. Zero-parameter delegates may use body-only input; parameterized delegates use lambda syntax.

This export path serves a different integration target than direct evaluation. Unsupported shapes are rejected during export. Dynamic call shapes, unsupported node kinds, and expression-tree-incompatible constructs fail there. Block-bodied lambdas are one concrete example. Reflection-oriented types are also blocked from this route.

Provider translation is a separate boundary. Alder can export a valid tree and an `IQueryable` provider can still reject it according to that provider's translation rules.

### Custom delegate compiler

`IExpressionCompiler` is a narrow extension point for hosts with a specific reason to replace `LambdaExpression.Compile()`. Alder still parses, binds, validates, optimizes, and lowers the expression through its own compiled provider; the custom component receives the generated `Expression<TDelegate>` and returns a delegate.

For example, a custom adapter can target FastExpressionCompiler by forwarding delegate compilation to its `CompileFast(...)` API. See <https://github.com/dadhi/FastExpressionCompiler>.

```csharp
public sealed class FastExpressionCompilerAdapter : IExpressionCompiler
{
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate =>
        expression.CompileFast<TDelegate>(
            ifFastFailedReturnNull: false,
            CompilerFlags.ThrowOnNotSupportedExpression);
}

var engine = new AlderEngine(options =>
    options.UseCompiler(new FastExpressionCompilerAdapter()));
```

Third-party expression compilers are optional and external to Alder's supported default compiler path. Alder does not bundle or certify them. Validate expression-shape support, failure behavior, semantic parity, diagnostics, performance, and deployment fit against the workloads that will run through this backend.

## Reuse and invalidation

Compiled reuse is tied to the engine and to the visible type surface of the context. That policy is explicit runtime behavior, not an incidental cache.

For parsed expressions, Alder stores compiled output in per-expression runtime state. Synchronous `Evaluate(AlderExpression)` reuses that result while the relevant type-inference version remains current. If visible declared types change, Alder recompiles before using this backend again.

Bound trees are cached separately from compiled delegates, and lowered output is cached per bound tree. Value changes follow a different rule. Changing a variable's value without changing its declared type does not invalidate compiled output. The next invocation sees the new value because compiled delegates close over the engine context by reference and run inside a fresh child execution context.

`AlderCompiledExpression<T>` is stricter than `Evaluate(AlderExpression)`. It captures the type version from the moment of compilation. If visible variable types change later, `Invoke(...)` throws `ALDR0003`. In that case, use normal `Evaluate(AlderExpression)` for automatic recompilation or compile again explicitly.

## Where compilation pays off

The compiled backend pays off most when the same synchronous logic runs many times against a stable type surface. Expression-tree export matters when the next stage consumes a LINQ tree for provider translation or downstream compilation.

- hot synchronous evaluation paths
- reusable business rules exposed as typed delegates
- zero-parameter rules reused through `CompileToFunc(...)`
- in-process Dynamic LINQ over materialized sequences
- `IQueryable` composition through expression-tree export

It also changes how Alder fits into a host application. `Compile<TDelegate>(...)` moves parameter typing to the delegate signature and avoids rebinding parameter shapes on each call. `ParseAsExpression<TDelegate>(...)` lets Alder participate in LINQ pipelines and query providers without forcing the host to treat every rule as a string to be evaluated at the edge.

## Dispatch and failure rules

Backend selection is narrow and deterministic.

- synchronous `Evaluate(...)` uses the compiled backend when a compiler is configured
- `EvaluateAsync(...)` stays on the interpreter
- `EvaluateWithTrace(...)` stays on the interpreter after security validation
- AOT and dynamic-code-restricted environments run through the interpreter with generated dispatch metadata

Failure behavior is equally explicit. `TryCompile(...)` is a probe: it records binding or lowering failure and returns `false`. `Compile(...)` and synchronous `Evaluate(...)` are strict once this backend is selected. If Alder cannot produce an invocable delegate, it surfaces the stored compilation failure as an `AlderException`. Compiled exceptions are also enriched with source positions before they are rethrown when the emitted code path only has an empty span.

## Practical boundary

The compiled backend is Alder's supported synchronous delegate-oriented execution path. It has its own lowering stages and reuse rules. Expression-tree export is a separate `Alder.Compiled` integration surface for query providers and downstream compilation. The interpreter remains the async path, the tracing path, and the execution mode for runtimes where dynamic code generation is unavailable or intentionally avoided.

## Related pages

- [Architecture](./architecture.md)
- [Execution model](../reference/execution-model.md)
