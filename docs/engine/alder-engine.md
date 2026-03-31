---
title: "AlderEngine"
description: "Full API reference for AlderEngine — evaluate, parse, validate, compile, trace"
sidebar:
  order: 2
---

`AlderEngine` is the entry point for evaluating C# code at runtime. It owns the parser, binder, interpreter, and optional compiler — configured once at construction time and immutable after that. All evaluation methods are thread-safe.

```csharp
var engine = new AlderEngine(o =>
{
    o.UseCompiler();
    o.Sandbox = SandboxOptions.Safe();
    o.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxTimeout = TimeSpan.FromSeconds(5)
    };
});
```

The `Action<AlderOptions>` overload is the primary construction pattern. Configuration is frozen when the constructor returns. See [AlderOptions](alder-options.md) for the full configuration surface.

## Construction

| Signature | Description |
|-----------|-------------|
| `AlderEngine()` | Default: Standard mode, Trusted sandbox, interpreted |
| `AlderEngine(Action<AlderOptions>)` | Configure via builder lambda |
| `AlderEngine(AlderOptions)` | Configure via options object |

## Evaluation

### Execution Paths

When `UseCompiler()` is configured, `Evaluate` routes directly to the compiled delegate — there is no interpreted fallback. If the expression fails to compile, `AlderException` with `ALDR0001` is thrown. Without `UseCompiler()`, the interpreter creates a child execution context and evaluates the bound tree.

Both paths share the same front-end (lexer → parser → binder → pipeline passes). The compiled path applies one additional pass (`ConversionInsertionPass`) that the interpreter does not need.

### `Evaluate` — string in, result out

```csharp
var result = engine.Evaluate("""
    Enumerable.Range(1, 10)
        .Where(n => n % 2 == 0)
        .Select(n => n * n)
        .Sum()
    """);
// 220
```

When you know the return type, `Evaluate<T>` applies C# conversion rules via `Convert.ChangeType`. If the expression produces `int` and you ask for `long`, the implicit widening handles it. If the result is a `LambdaValue` and `T` is a delegate type, delegate conversion is attempted via `LambdaDelegateConverter`. If the types are genuinely incompatible, `InvalidCastException` propagates.

```csharp
long result = engine.Evaluate<long>("1 + 2"); // 3L — int widened to long
```

### Variable overloads

Each `Evaluate` method accepts variables through three patterns:

| Overload | Variable source |
|----------|----------------|
| `Evaluate(string)` | Engine's persistent variables only |
| `Evaluate(string, IDictionary<string, object?>)` | Persistent + dictionary (scoped to call) |
| `Evaluate(string, object)` | Persistent + anonymous object properties (scoped to call) |
| `Evaluate(AlderExpression, ...)` | Same patterns with pre-parsed expression |

When per-call variables are passed, a child engine is created internally. The parent engine's state is never modified. See [Variables](variables.md) for details.

All overloads accept an optional `CancellationToken` as the last parameter.

### `TryEvaluate` — no exceptions

```csharp
if (engine.TryEvaluate<int>("1 + 2", out int result))
    Console.WriteLine(result); // 3

if (!engine.TryEvaluate("invalid(", out _))
    Console.WriteLine("Failed"); // no exception thrown
```

`TryEvaluate` catches all exceptions — parse, binding, runtime, and conversion failures. Returns `false` with `result = default`. Useful for evaluating user-supplied input where most inputs may be invalid.

## Parsing

### `Parse` — reusable expression object

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");
```

Returns an `AlderExpression` containing the parsed AST. The expression can be evaluated multiple times with different variable values without re-parsing. The bound tree is cached per-context and invalidated when variable types change.

### `TryParse` — parse without throwing

```csharp
if (engine.TryParse("items.Where(x => x > 0)", out AlderExpression? expr))
    Console.WriteLine(expr!.Source);

if (!engine.TryParse("items.Where(x =>", out _, out string? error))
    Console.WriteLine(error); // syntax error message
```

Two overloads: one with the expression output only, one with an additional error message output.

### `AlderExpression` properties

| Member | Type | Description |
|--------|------|-------------|
| `Source` | `string` | The original expression string |
| `GetVariables()` | `IReadOnlyList<string>` | Unbound identifiers the expression references |
| `IsCompiled` | `bool` | Whether a compiled delegate exists |
| `IsCompilable` | `bool?` | Whether compilation is possible (`null` = not attempted) |
| `CompilationFailureReason` | `string?` | Why compilation failed, or `null` |

## Validation

### `TryValidate` — full semantic analysis without execution

```csharp
engine.SetVariable<string>("name", "Alice");

if (!engine.TryValidate("name.Foo()", out IReadOnlyList<AlderDiagnostic> diagnostics))
{
    foreach (var d in diagnostics)
        Console.WriteLine($"{d.FormattedCode}: {d.Message}");
    // CS1061: 'String' does not contain a definition for 'Foo'
}
```

`TryValidate` performs:
1. Lexing and parsing (syntax check)
2. Binding with the `recovering: true` binder (collects all diagnostics instead of throwing on first error)
3. Unbound identifier detection via `IdentifierOccurrenceCollector` (finds references to names not defined as variables, functions, modules, types, or namespaces)
4. Diagnostic deduplication

The distinction from `TryParse`: parsing checks syntax only. `TryValidate` also checks semantics — `name.Foo()` parses successfully (valid syntax) but fails validation (no `Foo` on `String`).

## Tracing

### `EvaluateWithTrace` — step-by-step evaluation

```csharp
var trace = engine.EvaluateWithTrace("new[] { 1, 2, 3 }.Select(x => x * x).Sum()");

Console.WriteLine(trace.Result); // 14
Console.WriteLine(trace.Tree);   // step-by-step evaluation tree
```

Returns `EvaluationTraceResult` with:

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `object?` | The evaluation result (same as `Evaluate` would return) |
| `Tree` | `TraceNode` | Root of the step-by-step evaluation tree |
| `Error` | `Exception?` | The exception if evaluation failed, `null` on success |

When evaluation fails, `Error` captures the exception and `Tree` contains the trace up to the point of failure.

`EvaluateWithTrace` uses the security-only pipeline (skipping constant folding and dead branch elimination) so the tracer sees every node, including subexpressions that would normally be folded to constants.

Tracing is always interpreted — it does not use the compiled path even when `UseCompiler()` is configured.

## Variables

| Method | Returns | Description |
|--------|---------|-------------|
| `SetVariable<T>(string, T)` | `AlderEngine` | Typed persistent variable (fluent) |
| `SetVariable(string, object?)` | `AlderEngine` | Untyped persistent variable (fluent) |
| `SetVariables(IDictionary<string, object?>)` | `AlderEngine` | Bulk load from dictionary |
| `CreateChild()` | `AlderEngine` | Isolated child with inherited config and variables |
| `GetRegisteredModules()` | `IReadOnlyDictionary<string, RegisteredModule>` | Snapshot of all registered modules with metadata |

See [Variables](variables.md) for the full variable system documentation.

## Compilation

| Method | Returns | Description |
|--------|---------|-------------|
| `TryCompile(AlderExpression)` | `bool` | Attempt compilation, return success/failure |
| `Compile(AlderExpression)` | `void` | Compile or throw |
| `ParseAndCompile(string)` | `AlderExpression` | Parse + compile in one step |
| `Compile<T>(string)` | `AlderCompiledExpression<T>` | Hot-path compiled wrapper |
| `CompileToFunc<T>(string)` | `Func<T?>` | Bare compiled delegate |
| `ParseAsExpression<TDelegate>(string)` | `Expression<TDelegate>` | LINQ expression tree for EF/IQueryable |

See [Compilation](compilation.md) for the full compilation documentation.

## Disposal

`AlderEngine` implements `IDisposable`. Disposing sets a volatile flag on the shared `DisposalToken`, then clears the expression cache and type metadata provider. After disposal, all public methods throw `ObjectDisposedException` — checked via `ThrowIfDisposed()` at the top of every entry point.

```csharp
using var engine = new AlderEngine();
var result = engine.Evaluate<int>("1 + 1"); // 2
// engine disposed at end of scope
```

Disposing a parent engine disposes all children (they share the same `DisposalToken`). The disposal is lightweight — it does not dispose `AlderContext` instances or wait for in-flight evaluations. The volatile flag ensures visibility across threads without locking.

## Thread Safety

- All evaluation methods (`Evaluate`, `TryEvaluate`, `EvaluateWithTrace`, `Parse`, `TryParse`, `TryValidate`, `Compile`, `TryCompile`) can be called concurrently.
- `SetVariable<T>` is thread-safe — the engine-level context uses `ConcurrentDictionary`. Variables set before the first evaluation are staged in a `Dictionary` protected by a lock (`_contextInitLock`) and flushed to the `AlderContext` when evaluation begins.
- Child engines created via `CreateChild()` can be evaluated concurrently with the parent and with each other. Child contexts use non-concurrent `Dictionary` storage for faster per-evaluation access.
- `AlderExpression` objects are thread-safe and shareable across threads.
- Bound tree caching on `AlderExpression` uses `ConditionalWeakTable` for thread-safe per-context caching.
- Compiled delegate caching uses `volatile` fields with double-checked locking. Compilation locks on the `AlderExpression` object itself, not the engine.
- Pipeline instances (`SecurityOnlyPipeline`, `InterpretationPipeline`) are static and reused across all evaluations. The compilation pipeline is lazy-initialized per engine.

## Static API

`AlderEval` provides a global static engine for convenience:

```csharp
// Configure once at startup
AlderEval.Configure(o => o.UseCompiler());

// Evaluate from anywhere
var result = AlderEval.Evaluate<int>("1 + 2"); // 3
```

`AlderEval.Configure()` can only be called once and must be called before the first evaluation. Calling after the first evaluation throws `InvalidOperationException`. The global engine is created lazily with double-checked locking on the first evaluation or `Configure` call.

Additional static methods:

| Method | Description |
|--------|-------------|
| `AlderEval.Reset()` | Resets the global engine, allowing `Configure()` to be called again |
| `AlderEval.TryValidate(string, out IReadOnlyList<AlderDiagnostic>)` | Full semantic analysis without execution on the global engine |

## String Extensions

```csharp
// Uses the global AlderEval engine
var result = "1 + 2".Evaluate<int>(); // 3
var ok = "Math.PI".TryEvaluate<double>(out var pi); // true, pi ≈ 3.14159
```

Extension methods on `string` delegate to `AlderEval`. Available overloads mirror the `AlderEval` API.
