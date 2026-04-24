---
title: Compiled backend
description: How Alder's compiled backend changes synchronous execution, delegate generation, and expression-tree export.
---

# Compiled backend

Alder has two execution backends: the interpreter and the compiled backend. They share the same parser, binder, security validation, and execution constraints. Compilation changes how a bound expression runs, not what the language means. For synchronous workloads, it is a first-class execution path: it turns bound expressions into reusable delegates, supports delegate-first integration, and exports typed expression trees for LINQ-oriented workflows.

## Interpreter and compiled execution

The interpreter walks the bound expression directly. It is the default runtime and the only path used by `EvaluateAsync(...)`.

The compiled backend lowers the bound expression into a compiled delegate. When a compiler is configured, synchronous `Evaluate(...)` dispatches to that delegate path instead of the interpreter. The compilation pipeline still runs security validation, constant folding, and dead-branch elimination, then adds conversion insertion before delegate generation.

This does not create a second language contract. Both backends consume the same semantic form. Security policy, execution limits, and cancellation still apply.

## Enabling compilation with `UseCompiler()`

The compiled backend is opt-in. It is enabled through `Alder.Compiled`:

```csharp
using Alder.Compiled;

var engine = new AlderEngine(options => options.UseCompiler());
```

`UseCompiler()` installs Alder's compiled provider. The overload that accepts `IExpressionCompiler` keeps the same compiled pipeline but swaps the final expression-tree-to-delegate compiler.

Compilation requires dynamic code support. `UseCompiler()` throws `PlatformNotSupportedException` when the runtime does not support dynamic code generation. In AOT environments, Alder's supported execution path is the interpreter with AOT metadata rather than runtime compilation.

## What compilation changes

With no compiler configured, synchronous `Evaluate(...)` binds, validates, optimizes, and executes through the interpreter.

With a compiler configured, synchronous `Evaluate(...)` binds, validates, optimizes, compiles when needed, and then invokes the compiled delegate. Parsed expressions can also be compiled explicitly through `TryCompile(...)`, `Compile(...)`, or `ParseAndCompile(...)`.

The practical effect is not only execution speed. Compilation turns parsed code into reusable runtime artifacts: cached delegates for repeated `Evaluate(...)`, `AlderCompiledExpression<T>` wrappers, native typed delegates, and exported expression trees.

One boundary is easy to miss: once the engine is configured for compilation, synchronous evaluation commits to that backend. If Alder cannot produce an invocable compiled delegate, it surfaces the stored compilation failure instead of stepping back to the interpreter for that call.

## Synchronous and asynchronous behavior

Compilation only changes synchronous evaluation.

- `Evaluate(...)` uses the compiled backend when a compiler is configured.
- `EvaluateAsync(...)` stays on the interpreter, even when the engine is configured with `UseCompiler()`.
- `EvaluateWithTrace(...)` also runs through the interpreter after security validation.

That split is deliberate. The compiled backend is Alder's synchronous optimization and integration path. Async execution keeps the interpreter's runtime model.

## Compiled forms

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

## Delegate-first and expression-tree workflows

Typed delegate compilation and expression-tree export solve different problems.

`Compile<TDelegate>(...)` produces a native delegate for repeated in-process invocation. It accepts a code body, not lambda syntax, and binds parameter types from the delegate signature.

`ParseAsExpression<TDelegate>(...)` produces an expression tree for LINQ providers, `IQueryable`, and explicit downstream compilation. It parses in Standard mode regardless of the engine's `LanguageMode`. Engine variables visible during export are captured into the resulting tree as constants.

The expression-tree path serves a different integration target than direct evaluation. Unsupported shapes are rejected instead of interpreted. Dynamic call shapes, unsupported node kinds, and expression-tree-incompatible constructs fail during export. Block-bodied lambdas are one concrete example.

Provider translation is a separate boundary. Alder can export a valid expression tree and an `IQueryable` provider can still reject it.

## Reuse and invalidation

Compiled reuse is tied to the engine and to the visible type surface of the context.

For parsed expressions, Alder stores compiled output in per-expression runtime state. Synchronous `Evaluate(AlderExpression)` reuses that compiled output while the relevant type-inference version remains current. If visible declared types change, Alder recompiles before using the compiled path again.

Value changes are different. Changing a variable's value without changing its declared type does not invalidate compiled output. The next invocation sees the new value.

`AlderCompiledExpression<T>` is stricter than `Evaluate(AlderExpression)`. It captures the type version from the moment of compilation. If visible variable types change later, `Invoke(...)` throws `ALDR0003` instead of recompiling automatically. In that case, use normal `Evaluate(AlderExpression)` for automatic recompilation or compile again explicitly.

## Where compilation pays off

The compiled backend pays off most when the same synchronous logic runs many times against a stable type surface.

- hot synchronous evaluation paths
- reusable business rules exposed as typed delegates
- zero-parameter rules reused through `CompileToFunc(...)`
- Dynamic LINQ and `IQueryable` composition through expression-tree export

It also changes how Alder fits into a host application. `Compile<TDelegate>(...)` moves parameter typing to the delegate signature and avoids rebinding parameter shapes on each call. `ParseAsExpression<TDelegate>(...)` lets Alder participate in LINQ pipelines and query providers without forcing the host to treat every rule as a string to be evaluated at the edge.

## Where the interpreter remains preferable

The interpreter remains the better fit when any of these conditions matter:

- the expression is asynchronous or uses `await`
- the runtime is AOT and does not support dynamic code generation
- you need tracing through `EvaluateWithTrace(...)`
- you need the most permissive execution path for shapes that are valid to evaluate but not export or emit
- you prefer synchronous evaluation to keep working without a compilation boundary

That last point is operationally important. If a compiler is configured, synchronous evaluation uses the compiled backend. When an expression cannot be compiled, Alder does not step back to the interpreter for that call.

## Practical boundary

The compiled backend is best understood as Alder's synchronous delegate-oriented backend within a shared semantic model. It is valuable not only because it can reuse work, but because it lets Alder participate naturally in delegate-based APIs and expression-tree-driven systems. The interpreter remains the async path, the tracing path, the AOT path, and the better fit for scenarios that depend on runtime evaluation breadth over compiled execution.

## Related pages

- [Architecture](/explanation/architecture/)
- [Execution model](/reference/execution-model/)
