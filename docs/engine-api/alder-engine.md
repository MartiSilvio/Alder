# AlderEngine

`AlderEngine` is the entry point for all expression evaluation. It owns the parser, binder, interpreter, and optional compiler — configured once at construction time and immutable after that. All evaluation methods are thread-safe and can be called concurrently from any number of threads.

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

// Full C# — LINQ, lambdas, string interpolation, method chaining
string result = engine.Evaluate<string>("""
    new[] { "Alice", "Bob", "Charlie" }
        .Where(n => n.Length > 3)
        .Select(n => $"{n} ({n.Length})")
        .First()
    """);
// "Alice (5)"
```
<!-- test: Engine_Construction.csx -->

The `Action<AlderOptions>` overload is the primary construction pattern. Everything the engine needs is set through `AlderOptions` and frozen when the constructor returns. See [AlderOptions](alder-options.md) for the full configuration surface.

## Evaluating Expressions

### `Evaluate` — string in, result out

Pass a C# expression as a string. Alder lexes, parses, binds, and evaluates it in a single call:

```csharp
var result = engine.Evaluate("""
    Enumerable.Range(1, 10)
        .Where(n => n % 2 == 0)
        .Select(n => n * n)
        .Sum()
    """);
// 220
```
<!-- test: Engine_Evaluate_String.csx -->

When you know the return type, `Evaluate<T>` applies C# conversion rules and returns a typed result. If the expression produces `int` and you ask for `long`, the implicit widening conversion handles it. If the types are incompatible, you get a diagnostic naming the source and target types — not a silent `null`:

```csharp
bool valid = engine.Evaluate<bool>("""
    "user@example.com".Contains("@") && "user@example.com".Split("@").Length == 2
    """);
// true
```
<!-- test: Engine_Evaluate_Generic.csx -->

### Passing variables

Variables flow into expressions through three patterns, each with different scope and type semantics:

| Pattern | Best for | Trade-off |
|---------|----------|-----------|
| `SetVariable<T>` | Server apps, reused engines | Best performance — binder knows the type |
| Anonymous object | Quick one-off evaluations | Reflection cost per call |
| `IDictionary<string, object?>` | Dynamic keys from config/user input | Values typed as `object` |

```csharp
// Anonymous object — scoped to this call, type-inferred per property
bool allowed = engine.Evaluate<bool>(
    "user.Age >= 18 && user.Country == requiredCountry",
    new { user = new { Age = 25, Country = "US" }, requiredCountry = "US" }); // true

// Dictionary — scoped to this call, values typed as object
var vars = new Dictionary<string, object?> { ["radius"] = 5.0 };
double area = engine.Evaluate<double>("Math.PI * radius * radius", vars); // ~78.54
```
<!-- test: Engine_Evaluate_WithVariables.csx -->

Both anonymous objects and dictionaries are scoped to the single `Evaluate` call — they don't modify the engine's variable store. For persistent variables, see [Variables](#variables) below.

### `TryEvaluate` — no exceptions

`TryEvaluate` returns `false` instead of throwing, covering parse, binding, and runtime failures in one call. When you're evaluating user-supplied input and the expression might be invalid, this avoids the cost of exception handling on the failure path:

```csharp
if (engine.TryEvaluate<string>("""
    string.Join(", ", new[] { 1, 2, 3 }.Select(x => x.ToString()))
    """, out string? csv))
{
    Console.WriteLine(csv); // "1, 2, 3"
}
```
<!-- test: Engine_TryEvaluate.csx -->

`TryEvaluate<T>` applies the same type conversion as `Evaluate<T>`. Invalid expressions return `false` with no exception overhead — useful for validation loops or interactive editors where most input is partial or broken.

## Parsing and Reuse

Every `Evaluate(string)` call re-lexes and re-parses the expression from scratch. When you evaluate the same expression repeatedly — a pricing formula across thousands of orders, a filter predicate against every row — parse once and pass the `AlderExpression` to `Evaluate`:

```csharp
AlderExpression expr = engine.Parse("""
    items.Where(x => x.Amount > threshold).Sum(x => x.Amount)
    """);

// Lexing and parsing happen once. Binding is cached on the AlderExpression.
double q1 = engine.Evaluate<double>(expr, new { items = q1Orders, threshold = 100.0 });
double q2 = engine.Evaluate<double>(expr, new { items = q2Orders, threshold = 100.0 });
```
<!-- test: Engine_ParseAndReuse.csx -->

The `AlderExpression` also caches the bound tree (the result of semantic analysis). When the same expression is evaluated with the same variable types, binding is skipped entirely — only execution runs.

### `TryParse` — validation without evaluation

When you need to check whether an expression is syntactically valid without evaluating it:

```csharp
if (engine.TryParse("items.Where(x => x > 0).Sum()", out AlderExpression? expr))
    Console.WriteLine(expr!.Source); // the original expression string

if (!engine.TryParse("items.Where(x =>", out _, out string? error))
    Console.WriteLine(error); // "CS1003: Syntax error, ')' expected"
```
<!-- test: Engine_TryParse.csx -->

### `AlderExpression` properties

`GetVariables()` returns the names the expression expects the engine to provide. This is the foundation for building UIs that prompt for missing inputs, or for validating that a user-supplied expression only references permitted names:

```csharp
var expr = engine.Parse("""
    orders.Where(o => o.Total > minAmount && o.Region == region).Count()
    """);
var vars = expr.GetVariables(); // ["orders", "minAmount", "region"]
```
<!-- test: Expression_GetVariables.csx -->

| Property | Type | Description |
|----------|------|-------------|
| `Source` | `string` | The original expression string |
| `GetVariables()` | `IReadOnlyList<string>` | Unbound identifiers the expression references |
| `IsCompiled` | `bool` | Whether the expression has been compiled to IL |
| `IsCompilable` | `bool?` | Whether compilation is possible (`null` = not yet attempted) |

## Validation

`TryValidate` performs full semantic analysis — parsing, binding, and type checking — without executing the expression. It returns structured diagnostics with error codes, source positions, and messages:

```csharp
engine.SetVariable<string>("name", "Alice");

if (!engine.TryValidate("name.Foo()", out IReadOnlyList<AlderDiagnostic> diagnostics))
{
    // CS1061: 'String' does not contain a definition for 'Foo'
    foreach (var d in diagnostics)
        Console.WriteLine($"{d.Code}: {d.Message}");
}
```
<!-- test: Engine_TryValidate.csx -->

The distinction from `TryParse`: parsing only checks syntax, validation also checks semantics. `name.Foo()` parses successfully — it's syntactically valid C#. `TryValidate` catches that `Foo` doesn't exist on `String`.

## Variables

### `SetVariable<T>` — typed, persistent

When you provide the type parameter, Alder's binder resolves members and operators at bind time rather than deferring to runtime reflection. This means faster evaluation, AOT dispatch through the source generator, and precise diagnostics when a member doesn't exist on the declared type:

```csharp
engine.SetVariable<List<string>>("names", new List<string> { "Alice", "Bob", "Charlie" });

string result = engine.Evaluate<string>("""
    string.Join(", ", names.Where(n => n.StartsWith("A")))
    """);
// "Alice"
```
<!-- test: Variables_SetVariableGeneric.csx -->

`SetVariable<T>` returns the engine for fluent chaining. Variables persist across evaluations and can be updated at any time — the new value is visible to the next `Evaluate` call. This is thread-safe.

Because the binder knows the exact type, it resolves `Where`, `StartsWith`, and `Join` at bind time. With the untyped `SetVariable`, all of that falls back to runtime reflection — it works, but it's slower and error messages are less precise.

### `SetVariable` — untyped, persistent

The non-generic overload stores the value as `object`. Member resolution falls back to runtime reflection — this always works, but the strongly-typed alternative above is faster and produces better error messages:

```csharp
engine.SetVariable("config", new Dictionary<string, object?> { ["timeout"] = 30 });
```
<!-- test: Variables_SetVariable.csx -->

### `SetVariables` — bulk load from dictionary

For loading multiple variables at once, typically from configuration or a database:

```csharp
var context = new Dictionary<string, object?>
{
    ["basePrice"] = 100.0,
    ["discount"]  = 0.15,
    ["taxRate"]   = 0.08
};
engine.SetVariables(context);

double finalPrice = engine.Evaluate<double>("basePrice * (1 - discount) * (1 + taxRate)");
// 91.8
```
<!-- test: Variables_SetVariables.csx -->

All values are typed as `object` — the same trade-off as the untyped `SetVariable`.

## Child Engines

`CreateChild()` creates a new engine that inherits the parent's configuration and variables but can define additional variables without affecting the parent. This is the isolation model for multi-tenant scenarios — one parent engine with shared configuration, one child per tenant or request:

```csharp
var parent = new AlderEngine();
parent.SetVariable<double>("baseFee", 50.0);

var tenantA = parent.CreateChild();
tenantA.SetVariable<double>("discount", 0.1);

var tenantB = parent.CreateChild();
tenantB.SetVariable<double>("discount", 0.25);

// Each child sees baseFee from parent + its own discount
double a = tenantA.Evaluate<double>("baseFee * (1 - discount)"); // 45.0
double b = tenantB.Evaluate<double>("baseFee * (1 - discount)"); // 37.5

// Parent is unaffected — no discount variable exists here
double base_ = parent.Evaluate<double>("baseFee"); // 50.0
```
<!-- test: Variables_ChildEngine.csx -->

Child engines share the parent's expression cache, type metadata, and disposal token. Disposing the parent disposes all children.

## Compiled Evaluation

When `UseCompiler()` is configured, `Evaluate` automatically compiles expressions to IL on first execution. There is no manual step — the compiled delegate is cached on the `AlderExpression` and reused for every subsequent call:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

// First call: parse → bind → compile to IL → execute
// Subsequent calls with same expression: execute compiled delegate directly
var result = engine.Evaluate("""
    new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x).ToArray()
    """);
// int[] { 1, 3, 4, 5 }
```
<!-- test: Compilation_UseCompiler.csx -->

If compilation fails for a particular expression (not all constructs are compilable), `Evaluate` throws an `AlderException` with code `ALDR0001`. There is no silent fallback to interpretation — when you opt into compiled mode, you get compiled execution or an explicit error. Use `TryCompile` to check compilability before evaluation if you need to handle unsupported constructs gracefully.

### Explicit pre-compilation

When you want compilation to happen at startup rather than on first request — avoiding the latency spike in production — use `ParseAndCompile` or `Compile`:

```csharp
// ParseAndCompile: parse + compile in one step
AlderExpression expr = engine.ParseAndCompile("Math.Sqrt(x * x + y * y)");

// Compile: explicit compilation of an already-parsed expression (throws on failure)
var parsed = engine.Parse("items.Where(x => x > threshold).Count()");
engine.Compile(parsed);

// TryCompile: returns false instead of throwing
bool success = engine.TryCompile(parsed);
```
<!-- test: Compilation_ParseAndCompile.csx -->

### `Compile<T>` — lightweight compiled wrapper

`Compile<T>` returns an `AlderCompiledExpression<T>` that bypasses engine dispatch entirely. For hot paths — evaluating the same expression millions of times in a tight loop — this eliminates the overhead of variable scoping and constraint checking:

```csharp
var compiled = engine.Compile<int>("""
    Enumerable.Range(1, 100).Where(n => n % 3 == 0 || n % 5 == 0).Sum()
    """);
int result = compiled.Invoke(); // 2418
```
<!-- test: Compilation_CompiledExpression.csx -->

### `CompileToFunc<T>` — raw delegate

When you want a bare `Func<T>` with zero abstraction between your code and the compiled IL:

```csharp
engine.SetVariable<double>("r", 5.0);
Func<double?> circleArea = engine.CompileToFunc<double>("Math.PI * r * r");
double? area = circleArea(); // ~78.54
```
<!-- test: Compilation_CompileToFunc.csx -->

The delegate captures the engine context by reference — variables set via `SetVariable` after compilation are visible to subsequent invocations. Update `r` and the next `circleArea()` call sees the new value.

### `ParseAsExpression<TDelegate>` — LINQ expression trees

For Entity Framework, IQueryable providers, or any system that consumes `Expression<TDelegate>`, Alder can parse a lambda string directly into a typed expression tree:

```csharp
Expression<Func<int, bool>> predicate =
    engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");

// Pass to EF Core, IQueryable, or compile to a delegate
Func<int, bool> fn = predicate.Compile();
bool result = fn(25); // true
```
<!-- test: Compilation_ParseAsExpression.csx -->

`ParseAsExpression` always parses in Standard C# mode regardless of the engine's `LanguageMode` setting — expression trees follow C# semantics exactly.

| Method | Returns | Use case |
|--------|---------|----------|
| `Evaluate` (with `UseCompiler`) | `object?` / `T?` | General-purpose — auto-compiles transparently |
| `ParseAndCompile` | `AlderExpression` | Pre-compile at startup to avoid first-request latency |
| `Compile<T>` | `AlderCompiledExpression<T>` | Hot-path loops — no engine dispatch overhead |
| `CompileToFunc<T>` | `Func<T?>` | Bare delegate — minimum abstraction |
| `ParseAsExpression<TDelegate>` | `Expression<TDelegate>` | EF Core, IQueryable providers, expression tree consumers |

## Evaluation Tracing

`EvaluateWithTrace` returns both the result and a step-by-step trace tree showing how each subexpression was evaluated. This is a debugging and educational tool — it adds overhead and should not be used in production hot paths:

```csharp
var trace = engine.EvaluateWithTrace("""
    new[] { 1, 2, 3 }.Select(x => x * x).Sum()
    """);

Console.WriteLine(trace.Result); // 14
Console.WriteLine(trace.Tree);   // step-by-step evaluation tree
```
<!-- test: Engine_EvaluateWithTrace.csx -->

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `object?` | The evaluation result (same as `Evaluate` would return) |
| `Tree` | `TraceNode` | Root of the step-by-step evaluation tree |
| `Error` | `Exception?` | The exception if evaluation failed, `null` on success |

When evaluation fails, `Error` captures the exception and `Tree` contains the trace up to the point of failure — useful for understanding *where* in a complex expression things went wrong.

## Disposal

`AlderEngine` implements `IDisposable`. Disposing clears the expression cache and type metadata, releasing memory. After disposal, all method calls throw `ObjectDisposedException`:

```csharp
using var engine = new AlderEngine();
var result = engine.Evaluate<string>("""
    string.Concat(Enumerable.Repeat("ha", 3))
    """);
// "hahaha" — engine disposed at end of scope
```
<!-- test: Engine_Dispose.csx -->

In most applications, engines are long-lived singletons and disposal happens at shutdown. For short-lived engines in unit tests or batch processing, `using` ensures cleanup.

## Thread Safety

The engine is fully thread-safe:

- All evaluation methods (`Evaluate`, `TryEvaluate`, `EvaluateWithTrace`, `Parse`, `TryParse`, `TryValidate`, `Compile`, `TryCompile`) can be called concurrently from any number of threads.
- `SetVariable<T>` is thread-safe and can be called between evaluations. The new value is visible to the next evaluation on any thread.
- Child engines created via `CreateChild()` can be evaluated concurrently with the parent and with each other.
- `AlderExpression` objects are thread-safe and can be shared across threads.

The typical server pattern: one engine singleton configured at startup, concurrent `Evaluate` calls from request handlers. No locking required on your side.

## API Summary

### Construction

| Signature | Description |
|-----------|-------------|
| `AlderEngine()` | Default: Standard mode, Trusted sandbox, interpreted |
| `AlderEngine(Action<AlderOptions>)` | Configure via builder lambda |
| `AlderEngine(AlderOptions)` | Configure via options object |

### Evaluation

| Method | Returns | Throws on failure |
|--------|---------|-------------------|
| `Evaluate(string, ...)` | `object?` | Yes |
| `Evaluate<T>(string, ...)` | `T?` | Yes |
| `Evaluate(AlderExpression, ...)` | `object?` | Yes |
| `Evaluate<T>(AlderExpression, ...)` | `T?` | Yes |
| `TryEvaluate(string, out object?, ...)` | `bool` | No |
| `TryEvaluate<T>(string, out T?, ...)` | `bool` | No |

Each `Evaluate` overload accepts variables as either `IDictionary<string, object?>`, an anonymous `object`, or nothing. All accept an optional `CancellationToken` as the last parameter.

### Parsing and Validation

| Method | Returns | Description |
|--------|---------|-------------|
| `Parse(string)` | `AlderExpression` | Parse to reusable expression (throws on syntax error) |
| `TryParse(string, out AlderExpression?)` | `bool` | Parse without throwing |
| `TryValidate(string, out IReadOnlyList<AlderDiagnostic>)` | `bool` | Full semantic validation without execution |

### Compilation (requires `UseCompiler()`)

| Method | Returns | Description |
|--------|---------|-------------|
| `Compile(AlderExpression)` | `void` | Explicit pre-compilation (throws on failure) |
| `TryCompile(AlderExpression)` | `bool` | Pre-compilation without throwing |
| `ParseAndCompile(string)` | `AlderExpression` | Parse + compile in one step |
| `Compile<T>(string)` | `AlderCompiledExpression<T>` | Compiled wrapper for hot-path invocation |
| `CompileToFunc<T>(string)` | `Func<T?>` | Bare compiled delegate |
| `ParseAsExpression<TDelegate>(string)` | `Expression<TDelegate>` | LINQ expression tree for EF/IQueryable |

### Variables and Context

| Method | Returns | Description |
|--------|---------|-------------|
| `SetVariable<T>(string, T)` | `AlderEngine` | Typed persistent variable (fluent) |
| `SetVariable(string, object?)` | `AlderEngine` | Untyped persistent variable (fluent) |
| `SetVariables(IDictionary<string, object?>)` | `AlderEngine` | Bulk load from dictionary |
| `CreateChild()` | `AlderEngine` | Isolated child with inherited variables |

### Tracing and Disposal

| Method | Returns | Description |
|--------|---------|-------------|
| `EvaluateWithTrace(string, ...)` | `EvaluationTraceResult` | Step-by-step evaluation trace |
| `EvaluateWithTrace(AlderExpression, ...)` | `EvaluationTraceResult` | Trace with pre-parsed expression |
| `Dispose()` | `void` | Release cache and metadata |
