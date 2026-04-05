The `Evaluate` method executes C# code at runtime and returns the result. From simple expressions to multi-statement programs with LINQ, pattern matching, and control flow.

```csharp
var engine = new AlderEngine();
var result = engine.Evaluate<int>("1 + 2"); // 3
```

## Three ways to evaluate

### Instance method

Create an engine, evaluate against it. Use this when you need custom configuration, persistent variables, or isolated evaluation contexts.

```csharp
var engine = new AlderEngine(o =>
{
    o.Sandbox = SandboxOptions.Safe();
    o.UseCompiler();
});

var result = engine.Evaluate<List<int>>("new List<int> { 1, 2, 3 }");
```

### Static method

`AlderEval` provides a shared global engine. Configure once at startup, evaluate from anywhere.

```csharp
AlderEval.Configure(o => o.UseCompiler());

var result = AlderEval.Evaluate<int>("Enumerable.Range(1, 10).Sum()"); // 55
```

### String extension

Every string becomes evaluable. Delegates to `AlderEval`.

```csharp
var result = "1 + 2".Evaluate<int>(); // 3
var ok = "Math.PI".TryEvaluate<double>(out var pi); // true
```

## Return type

Omit the generic parameter to get `object?`. Specify `<T>` for typed results with automatic conversion.

```csharp
object? untyped = engine.Evaluate("1 + 2");          // boxed int 3
int typed = engine.Evaluate<int>("1 + 2");            // 3
double widened = engine.Evaluate<double>("1 + 2");    // 3.0 (implicit widening)
```

## Variables

### Typed variables (recommended)

`SetVariable<T>` gives the binder the variable's type. Member access, LINQ, and overload resolution are resolved at bind time.

```csharp
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });
engine.SetVariable<int>("threshold", 70);

double avg = engine.Evaluate<double>("scores.Where(s => s >= threshold).Average()");
// 87.75
```

Variables persist across evaluations. Updating a value is visible to the next `Evaluate` call on any thread.

### Anonymous object (per-call, typed)

Properties become variables scoped to that single evaluation. Property types are preserved for type-aware binding.

```csharp
var result = engine.Evaluate<bool>(
    "age >= 18 && country != null",
    new { age = 25, country = "US" });
// true
```

```csharp
var result = engine.Evaluate<List<int>>(
    "new List<int> { 1, 2, 3, 4, 5 }.Where(x => x > min && x < max).ToList()",
    new { min = 1, max = 5 });
// [2, 3, 4]
```

### Dictionary (per-call, untyped)

For dynamic keys from configuration or user input. Values are typed as `object`.

```csharp
var vars = new Dictionary<string, object?>
{
    ["threshold"] = 100,
    ["multiplier"] = 1.5
};
double result = engine.Evaluate<double>("threshold * multiplier", vars);
// 150.0
```

### Class instance (per-call, typed)

Any object works. Public properties become variables.

```csharp
public class PricingParams
{
    public double BasePrice { get; set; }
    public double TaxRate { get; set; }
    public double Discount { get; set; }
}

var pricing = new PricingParams { BasePrice = 100, TaxRate = 0.08, Discount = 0.1 };
double total = engine.Evaluate<double>(
    "BasePrice * (1 + TaxRate) * (1 - Discount)",
    pricing);
// 97.2
```

## Multi-statement programs

Separate statements with semicolons. Use `return` to produce a result.

```csharp
var result = engine.Evaluate<string>("""
    var items = new[] { 3, 1, 4, 1, 5, 9 };
    var unique = items.Distinct().OrderByDescending(x => x).Take(3).ToList();
    return $"Top 3: {string.Join(", ", unique)}";
    """);
// "Top 3: 9, 5, 4"
```

```csharp
var result = engine.Evaluate<List<string>>("""
    var answers = new List<string>();
    for (var i = 1; i <= 20; i++)
    {
        if (i % 15 == 0) answers.Add($"{i} => FizzBuzz");
        else if (i % 3 == 0) answers.Add($"{i} => Fizz");
        else if (i % 5 == 0) answers.Add($"{i} => Buzz");
        else answers.Add($"{i} => {i}");
    }
    return answers;
    """);
```

## Try methods (no exceptions)

`TryEvaluate` returns `false` on any failure. `TryValidate` runs full semantic analysis without executing.

```csharp
if (engine.TryEvaluate<int>("1 + 2", out var result))
    Console.WriteLine(result); // 3

if (!engine.TryEvaluate("invalid(", out _))
    Console.WriteLine("Failed");
```

```csharp
if (!engine.TryValidate("name.Foo()", out var diagnostics))
    Console.WriteLine($"{diagnostics[0].FormattedCode}: {diagnostics[0].Message}");
    // CS1061: 'String' does not contain a definition for 'Foo'
```

## Compile for performance

### Auto-compilation

With `UseCompiler()`, every expression is compiled to native IL on first execution and cached.

```csharp
var engine = new AlderEngine(o => o.UseCompiler());
engine.Evaluate<int>("1 + 2"); // compiled and cached on first call
```

### Compile\<T\> (reusable wrapper)

Bypasses engine dispatch. Context is captured by reference: variable updates are visible.

```csharp
var compiled = engine.Compile<int>("Enumerable.Range(1, n).Sum()");
engine.SetVariable<int>("n", 100);
compiled.Invoke();     // 5050
engine.SetVariable<int>("n", 10);
compiled.Invoke();     // 55
```

Per-invocation variables via dictionary:

```csharp
compiled.Invoke(new Dictionary<string, object?> { ["n"] = 50 }); // 1275
```

### CompileToFunc\<T\> (raw delegate)

```csharp
engine.SetVariable<double>("r", 5.0);
Func<double?> area = engine.CompileToFunc<double>("Math.PI * r * r");
area();                              // ~78.54
engine.SetVariable<double>("r", 10.0);
area();                              // ~314.16
```

### ParseAsExpression\<TDelegate\> (LINQ expression tree)

For EF Core, IQueryable providers, or any system that consumes expression trees.

```csharp
Expression<Func<int, bool>> predicate =
    engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");

// Pass to EF Core
var adults = dbContext.People.Where(predicate);

// Or compile to a delegate
Func<int, bool> fn = predicate.Compile();
fn(25);  // true
fn(10);  // false
```

## Async

Use `EvaluateAsync` for expressions containing `await`.

```csharp
var result = await engine.EvaluateAsync<int>("""
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    return a + b;
    """);
// 30
```

All variable patterns work with `EvaluateAsync`:

```csharp
var html = await engine.EvaluateAsync<string>(
    "await http.GetStringAsync(url)",
    new { http = new HttpClient(), url = "https://example.com" });
```

## Parse once, evaluate many

`Evaluate(string)` re-parses every call. For repeated evaluation, parse once:

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");

engine.SetVariable<double>("price", 100.0);
engine.SetVariable<double>("discount", 0.1);
engine.Evaluate<double>(expr); // 90.0

engine.SetVariable<double>("price", 250.0);
engine.Evaluate<double>(expr); // 225.0
```

The bound tree is cached. When variable types haven't changed, binding is skipped entirely.

## All overloads

### AlderEngine instance

| Method | Returns |
|--------|---------|
| `Evaluate(string)` | `object?` |
| `Evaluate(string, IDictionary<string, object?>)` | `object?` |
| `Evaluate(string, object)` | `object?` (typed from properties) |
| `Evaluate(AlderExpression)` | `object?` |
| `Evaluate(AlderExpression, IDictionary<string, object?>)` | `object?` |
| `Evaluate(AlderExpression, object)` | `object?` (typed from properties) |
| `Evaluate<T>(string)` | `T?` |
| `Evaluate<T>(string, IDictionary<string, object?>)` | `T?` |
| `Evaluate<T>(string, object)` | `T?` (typed from properties) |
| `Evaluate<T>(AlderExpression)` | `T?` |
| `Evaluate<T>(AlderExpression, IDictionary<string, object?>)` | `T?` |
| `Evaluate<T>(AlderExpression, object)` | `T?` (typed from properties) |
| `TryEvaluate(string, out object?)` | `bool` |
| `TryEvaluate<T>(string, out T?)` | `bool` |
| `EvaluateAsync(string)` | `ValueTask<object?>` |
| `EvaluateAsync<T>(string)` | `ValueTask<T?>` |

All accept optional `CancellationToken` as the last parameter.

### AlderEval static

Same signatures as instance methods, prefixed with `AlderEval.`:

```csharp
AlderEval.Evaluate<int>("1 + 2")
AlderEval.Evaluate<int>("x + y", new { x = 1, y = 2 })
AlderEval.TryEvaluate<int>("1 + 2", out var result)
await AlderEval.EvaluateAsync<int>("await Task.FromResult(42)")
```

### String extensions

Same signatures as static methods, called on any string:

```csharp
"1 + 2".Evaluate<int>()
"x + y".Evaluate<int>(new { x = 1, y = 2 })
"1 + 2".TryEvaluate<int>(out var result)
```

## Construction

| Signature | Description |
|-----------|-------------|
| `AlderEngine()` | Default: Standard mode, Trusted sandbox, interpreted |
| `AlderEngine(Action<AlderOptions>)` | Configure via builder lambda |
| `AlderEngine(AlderOptions)` | Configure via options object |

Configuration is frozen at construction time. See [AlderOptions](engine/alder-options.md).

## Parsing

`Parse` returns a reusable `AlderExpression`. `TryParse` is the non-throwing variant.

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");

if (!engine.TryParse("items.Where(x =>", out _, out string? error))
    Console.WriteLine(error);
```

### AlderExpression properties

| Member | Type | Description |
|--------|------|-------------|
| `Source` | `string` | The original expression string |
| `GetVariables()` | `IReadOnlyList<string>` | Unbound identifiers the expression references |
| `IsCompiled` | `bool` | Whether a compiled delegate exists |
| `IsCompilable` | `bool?` | Whether compilation is possible (`null` = not attempted) |
| `CompilationFailureReason` | `string?` | Why compilation failed |

## Validation

`TryValidate` performs full semantic analysis (lexing, parsing, binding in recovering mode) without executing. `TryParse` checks syntax only. `TryValidate` also catches type errors and missing members.

```csharp
engine.SetVariable<string>("name", "Alice");

if (!engine.TryValidate("name.Foo()", out IReadOnlyList<AlderDiagnostic> diagnostics))
{
    foreach (var d in diagnostics)
        Console.WriteLine($"{d.FormattedCode}: {d.Message}");
    // CS1061: 'String' does not contain a definition for 'Foo'
}
```

## Tracing

`EvaluateWithTrace` returns the result plus a step-by-step evaluation tree.

```csharp
var trace = engine.EvaluateWithTrace("""
    var data = new[] { 1, 2, 3 };
    return data.Select(x => x * x).Sum();
    """);

Console.WriteLine(trace.Result); // 14
Console.WriteLine(trace.Tree);   // evaluation tree
```

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `object?` | The evaluation result |
| `Tree` | `TraceNode` | Root of the evaluation tree |
| `Error` | `Exception?` | The exception if evaluation failed |

Each `TraceNode` shows the node kind, source text, computed value, runtime type, and child evaluations. Tracing skips optimization passes so every subexpression is visible. Always uses the interpreter.

## Disposal

`AlderEngine` implements `IDisposable`. After disposal, all methods throw `ObjectDisposedException`.

```csharp
using var engine = new AlderEngine();
var result = engine.Evaluate<int>("1 + 1"); // 2
```

Disposing a parent disposes all children (shared `DisposalToken`). Lightweight: does not wait for in-flight evaluations.

## Thread Safety

- All evaluation methods can be called concurrently.
- `SetVariable<T>` is thread-safe (`ConcurrentDictionary` storage).
- Child engines (`CreateChild()`) can be evaluated concurrently with parent and each other.
- `AlderExpression` objects are thread-safe and shareable.
- Bound tree caching uses `ConditionalWeakTable`.
- Compiled delegate caching uses volatile fields with double-checked locking.
- Pipeline instances are static and reused across evaluations.
