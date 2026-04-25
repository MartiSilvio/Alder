---
title: Compiled backend
description: How Alder's compiled backend changes synchronous execution, delegate generation, and expression-tree export.
---

# Compiled backend

Alder has two execution backends: the interpreter and the compiled backend. They share the same parser, binder, security validation, and execution constraints. Compilation changes how a bound expression runs, not what the language means. For synchronous workloads, it is a first-class execution path: it turns bound expressions into reusable delegates, supports delegate-first integration, and exports typed expression trees for LINQ-oriented workflows.

## Execution architecture

The interpreter walks the bound expression directly. It is used when no compiler is configured and by `EvaluateAsync(...)`.

The compiled path is a separate lowering pipeline over the same bound tree. When a compiler is configured, synchronous `Evaluate(...)` dispatches to that path. Alder still binds first and still runs semantic work before delegate generation. The compilation-specific pipeline is:

- security validation
- constant folding
- dead-branch elimination
- conversion insertion

After those passes, the compiled provider emits a LINQ expression tree rooted in Alder's runtime contracts. The configured `IExpressionCompiler` then turns that tree into a delegate. Inside emission, Alder plans local promotion and repeated-identifier hoisting before producing the root lambda. This is a real lowering stage, not a thin wrapper around `LambdaExpression.Compile()`.

This does not create a second language contract. Both backends consume the same semantic form. Security policy, execution limits, and cancellation still apply.

## Enabling compilation with `UseCompiler()`

The compiled backend is opt-in. It is enabled through `Alder.Compiled`:

```csharp
using Alder.Compiled;

var engine = new AlderEngine(options => options.UseCompiler());
```

`UseCompiler()` installs Alder's compiled provider. The overload that accepts `IExpressionCompiler` keeps Alder's lowering pipeline and replaces only the final expression-tree-to-delegate compiler.

The supported starting point is plain `UseCompiler()`. It uses Alder's default path: Alder lowers the bound tree through its compiled provider, then `DefaultExpressionCompiler` delegates final delegate generation to `LambdaExpression.Compile()`.

Compilation requires dynamic code support. `UseCompiler()` throws `PlatformNotSupportedException` when the runtime does not support dynamic code generation. In AOT environments, Alder's supported execution path is the interpreter with AOT metadata.

## What changes when compilation is enabled

With no compiler configured, synchronous `Evaluate(...)` binds, validates, optimizes, and executes through the interpreter.

With a compiler configured, synchronous `Evaluate(...)` binds, validates, optimizes, compiles when needed, and then invokes the compiled delegate. Parsed expressions can also be compiled explicitly through `TryCompile(...)`, `Compile(...)`, or `ParseAndCompile(...)`.

Compilation changes more than execution speed. It turns parsed code into reusable runtime artifacts: cached delegates for repeated `Evaluate(...)`, `AlderCompiledExpression<T>` wrappers, native typed delegates, and exported expression trees.

One boundary matters operationally: once the engine is configured for compilation, synchronous evaluation commits to that path. If Alder cannot produce an invocable compiled delegate, it surfaces the stored compilation failure for that call.

## Synchronous and asynchronous behavior

Compilation changes synchronous evaluation only.

- `Evaluate(...)` uses the compiled backend when a compiler is configured.
- `EvaluateAsync(...)` uses the interpreter, even when the engine is configured with `UseCompiler()`.
- `EvaluateWithTrace(...)` runs through the interpreter after security validation.

`EvaluateAsync(...)` uses the interpreter because `System.Linq.Expressions` does not provide the async execution model Alder requires. Alder awaits asynchronous expression work inside the evaluator; it does not turn synchronous compiled execution into asynchronous work by scheduling it through `Task.Run`.

## Public compiled forms

The compiled backend exposes three distinct public outputs.

### Compiled expression wrappers

`engine.Compile<T>(...)` returns `AlderCompiledExpression<T>`, a reusable wrapper around the compiled delegate:

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

`ParseAsExpression<TDelegate>(...)` exports a typed `Expression<TDelegate>` instead of a compiled runtime delegate:

```csharp
Expression<Func<int, bool>> predicate =
    engine.ParseAsExpression<Func<int, bool>>("x => x > minAge");
```

`CompileExpression<TDelegate>(...)` is the direct `ParseAsExpression(...).Compile()` path.

## Expression export and delegate generation

Typed delegate compilation and expression-tree export solve different problems.

`Compile<TDelegate>(...)` produces a native delegate for repeated in-process invocation. It accepts a code body, not lambda syntax, and binds parameter types from the delegate signature.

`ParseAsExpression<TDelegate>(...)` produces an expression tree for LINQ providers, `IQueryable`, and explicit downstream compilation. It parses in Standard mode regardless of the engine's `LanguageMode`. Engine variables visible during export are captured into the resulting tree as constants. That gives Alder a direct integration boundary with external LINQ systems.

The export path has its own preparation flow. Alder creates typed parameter bindings from the target delegate. It binds the lambda or body-only form against a query-specific runtime context, reruns the compilation pipeline over the bound body, and then exports the supported node set into a provider-facing tree.

The expression-tree path serves a different integration target than direct evaluation. Unsupported shapes are rejected during export. Dynamic call shapes, unsupported node kinds, and expression-tree-incompatible constructs fail there. Block-bodied lambdas are one concrete example. Reflection-oriented types are also blocked from this path.

Provider translation is a separate boundary. Alder can export a valid expression tree and an `IQueryable` provider can still reject it.

### Advanced compiler hook

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

Third-party expression compilers are optional and external to Alder's supported default compiler path. Alder does not bundle or certify them. Validate expression-shape support, failure behavior, semantic parity, diagnostics, performance, and deployment fit against the workloads that will run through the compiled backend.

## Reuse and invalidation

Compiled reuse is tied to the engine and to the visible type surface of the context. That policy is explicit runtime behavior, not an incidental cache.

For parsed expressions, Alder stores compiled output in per-expression runtime state. Synchronous `Evaluate(AlderExpression)` reuses that compiled output while the relevant type-inference version remains current. If visible declared types change, Alder recompiles before using the compiled path again.

Bound expressions are cached separately from compiled delegates, and pipeline output is cached per bound tree. Value changes follow a different rule. Changing a variable's value without changing its declared type does not invalidate compiled output. The next invocation sees the new value because compiled delegates close over the engine context by reference and run inside a fresh child execution context.

`AlderCompiledExpression<T>` is stricter than `Evaluate(AlderExpression)`. It captures the type version from the moment of compilation. If visible variable types change later, `Invoke(...)` throws `ALDR0003` instead of recompiling automatically. In that case, use normal `Evaluate(AlderExpression)` for automatic recompilation or compile again explicitly.

## Where compilation pays off

The compiled backend pays off most when the same synchronous logic runs many times against a stable type surface.

- hot synchronous evaluation paths
- reusable business rules exposed as typed delegates
- zero-parameter rules reused through `CompileToFunc(...)`
- Dynamic LINQ and `IQueryable` composition through expression-tree export

It also changes how Alder fits into a host application. `Compile<TDelegate>(...)` moves parameter typing to the delegate signature and avoids rebinding parameter shapes on each call. `ParseAsExpression<TDelegate>(...)` lets Alder participate in LINQ pipelines and query providers without forcing the host to treat every rule as a string to be evaluated at the edge.

## Dispatch and failure rules

Backend selection is narrow and deterministic.

- synchronous `Evaluate(...)` uses the compiled backend when a compiler is configured
- `EvaluateAsync(...)` stays on the interpreter
- `EvaluateWithTrace(...)` stays on the interpreter after security validation
- AOT environments cannot enable `UseCompiler()` and run through the interpreter with AOT metadata instead

Failure behavior is equally explicit. `TryCompile(...)` is a probe: it records binding or lowering failure and returns `false`. `Compile(...)` and synchronous `Evaluate(...)` are strict once this path is selected. If Alder cannot produce an invocable delegate, it surfaces the stored compilation failure. Compiled exceptions are also enriched with source positions before they are rethrown when the emitted path only has an empty span.

## Practical boundary

The compiled backend is Alder's supported synchronous delegate-oriented execution path. It has its own lowering pipeline, its own reuse and invalidation rules, and a separate export surface for expression-tree-driven integration. The interpreter remains the async path, the tracing path, and the execution path for runtimes where dynamic code generation is unavailable or intentionally avoided.

## Related pages

- [Architecture](/explanation/architecture/)
- [Execution model](/reference/execution-model/)
