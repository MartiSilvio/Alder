`AlderEngine` is the entry point for evaluating C# code at runtime. It owns the parser, binder, interpreter, and optional compiler. Configuration is frozen at construction time. All evaluation methods are thread-safe.

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

See [AlderOptions](alder-options.md) for the full configuration surface.

## Construction

| Signature | Description |
|-----------|-------------|
| `AlderEngine()` | Default: Standard mode, Trusted sandbox, interpreted |
| `AlderEngine(Action<AlderOptions>)` | Configure via builder lambda |
| `AlderEngine(AlderOptions)` | Configure via options object |

## Evaluation

### `Evaluate`

```csharp
var result = engine.Evaluate<string>("""
    var items = Enumerable.Range(1, 10)
        .Where(n => n % 2 == 0)
        .Select(n => n * n)
        .ToList();
    return $"Sum: {items.Sum()}, Count: {items.Count}";
    """);
// "Sum: 220, Count: 5"
```

`UseCompiler()` compiles expressions to native delegates on first execution and caches them. Without it, the interpreter evaluates the bound tree.

Both paths share the same front-end (lexer, parser, binder, pipeline passes). If compilation fails, `AlderException` with `ALDR0001` is thrown. There is no silent fallback to interpretation.

`Evaluate<T>` applies C# conversion rules. If the expression produces `int` and you request `long`, implicit widening handles it. Incompatible types raise `InvalidCastException`.

### Variable overloads

Each `Evaluate` method accepts variables through three patterns:

| Overload | Variable source |
|----------|----------------|
| `Evaluate(string)` | Engine's persistent variables only |
| `Evaluate(string, IDictionary<string, object?>)` | Persistent + dictionary (scoped to call) |
| `Evaluate(string, object)` | Persistent + anonymous object properties (scoped to call) |
| `Evaluate(AlderExpression, ...)` | Same patterns with pre-parsed expression |

Per-call variables create a child engine internally. The parent engine's state is never modified. See [Variables](variables.md).

All overloads accept an optional `CancellationToken` as the last parameter.

### `TryEvaluate`

```csharp
if (engine.TryEvaluate<int>("1 + 2", out int result))
    Console.WriteLine(result); // 3

if (!engine.TryEvaluate("invalid(", out _))
    Console.WriteLine("Failed"); // no exception thrown
```

Returns `false` for parse, binding, runtime, and conversion failures.

## Parsing

### `Parse`

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");
```

Returns an `AlderExpression` containing the parsed AST. The expression can be evaluated multiple times with different variable values without re-parsing. The bound tree is cached per-context and invalidated when variable types change.

### `TryParse`

```csharp
if (engine.TryParse("items.Where(x => x > 0)", out AlderExpression? expr))
    Console.WriteLine(expr!.Source);

if (!engine.TryParse("items.Where(x =>", out _, out string? error))
    Console.WriteLine(error); // syntax error message
```

### `AlderExpression` properties

| Member | Type | Description |
|--------|------|-------------|
| `Source` | `string` | The original expression string |
| `GetVariables()` | `IReadOnlyList<string>` | Unbound identifiers the expression references |
| `IsCompiled` | `bool` | Whether a compiled delegate exists |
| `IsCompilable` | `bool?` | Whether compilation is possible (`null` = not attempted) |
| `CompilationFailureReason` | `string?` | Why compilation failed, or `null` |

## Validation

### `TryValidate`

```csharp
engine.SetVariable<string>("name", "Alice");

if (!engine.TryValidate("name.Foo()", out IReadOnlyList<AlderDiagnostic> diagnostics))
{
    foreach (var d in diagnostics)
        Console.WriteLine($"{d.FormattedCode}: {d.Message}");
    // CS1061: 'String' does not contain a definition for 'Foo'
}
```

`TryValidate` performs lexing, parsing, binding (in recovering mode to collect all diagnostics), and unbound identifier detection. `TryParse` checks syntax only. `TryValidate` also checks semantics: `name.Foo()` parses successfully but fails validation.

## Tracing

### `EvaluateWithTrace`

```csharp
var trace = engine.EvaluateWithTrace("""
    var data = new[] { 1, 2, 3 };
    return data.Select(x => x * x).Sum();
    """);

Console.WriteLine(trace.Result); // 14
Console.WriteLine(trace.Tree);   // step-by-step evaluation tree
```

Returns `EvaluationTraceResult` with:

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `object?` | The evaluation result |
| `Tree` | `TraceNode` | Root of the step-by-step evaluation tree |
| `Error` | `Exception?` | The exception if evaluation failed |

Each `TraceNode` shows the node kind, source text, computed value, runtime type, description, error details, and child evaluations. Tracing skips optimization passes (no constant folding or dead branch elimination) so every subexpression is visible.

Tracing always uses the interpreter, even when `UseCompiler()` is configured.

## Variables

| Method | Returns | Description |
|--------|---------|-------------|
| `SetVariable<T>(string, T)` | `AlderEngine` | Typed persistent variable (fluent) |
| `SetVariable(string, object?)` | `AlderEngine` | Untyped persistent variable (fluent) |
| `SetVariables(IDictionary<string, object?>)` | `AlderEngine` | Bulk load from dictionary |
| `CreateChild()` | `AlderEngine` | Isolated child with inherited config and variables |
| `GetRegisteredModules()` | `IReadOnlyDictionary<string, RegisteredModule>` | Snapshot of all registered modules |

See [Variables](variables.md) for the full variable system.

## Compilation

| Method | Returns | Description |
|--------|---------|-------------|
| `TryCompile(AlderExpression)` | `bool` | Attempt compilation, return success/failure |
| `Compile(AlderExpression)` | `void` | Compile or throw |
| `ParseAndCompile(string)` | `AlderExpression` | Parse + compile in one step |
| `Compile<T>(string)` | `AlderCompiledExpression<T>` | Hot-path compiled wrapper |
| `CompileToFunc<T>(string)` | `Func<T?>` | Bare compiled delegate |
| `ParseAsExpression<TDelegate>(string)` | `Expression<TDelegate>` | LINQ expression tree for EF/IQueryable |
| `TryParseAsExpression<TDelegate>(string, ...)` | `bool` | Non-throwing expression tree parsing |
| `CompileExpression<TDelegate>(string)` | `TDelegate` | Parse + compile expression tree to delegate |

`ParseAndCompile`, `Compile<T>`, `CompileToFunc<T>`, `ParseAsExpression`, `TryParseAsExpression`, and `CompileExpression` are extension methods from `Alder.Compiled`. They require `UseCompiler()`.

See [Compilation](compilation.md) for details.

## Disposal

`AlderEngine` implements `IDisposable`. Disposing sets a flag on the shared `DisposalToken`, then clears the expression cache and type metadata. After disposal, all public methods throw `ObjectDisposedException`.

```csharp
using var engine = new AlderEngine();
var result = engine.Evaluate<int>("1 + 1"); // 2
// engine disposed at end of scope
```

Disposing a parent engine disposes all children (they share the same `DisposalToken`). Disposal is lightweight: it does not wait for in-flight evaluations.

## Thread Safety

- All evaluation methods can be called concurrently.
- `SetVariable<T>` is thread-safe. The engine uses `ConcurrentDictionary` for the runtime context. Variables set before the first evaluation are staged in a protected dictionary and bulk-flushed when evaluation begins.
- Child engines created via `CreateChild()` can be evaluated concurrently with the parent and each other.
- `AlderExpression` objects are thread-safe and shareable across threads.
- Bound tree caching uses `ConditionalWeakTable` for per-context thread safety.
- Compiled delegate caching uses volatile fields with double-checked locking.
- Pipeline instances are static and reused across evaluations. The compilation pipeline is lazy-initialized per engine.

## Static API

`AlderEval` provides a global static engine for convenience:

```csharp
// Configure once at startup
AlderEval.Configure(o => o.UseCompiler());

// Evaluate from anywhere
var result = AlderEval.Evaluate<int>("1 + 2"); // 3
```

`Configure()` can only be called once and must be called before the first evaluation. The global engine is created lazily on first use. `AlderEval.Reset()` allows reconfiguring (primarily for testing).

### String Extensions

```csharp
var result = "1 + 2".Evaluate<int>(); // 3
var ok = "Math.PI".TryEvaluate<double>(out var pi); // true
```

Extension methods on `string` delegate to `AlderEval`.
