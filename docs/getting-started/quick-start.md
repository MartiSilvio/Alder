# Quick Start

## Installation

```bash
dotnet add package Alder
```

A single package — interpreted evaluation, IL compilation, and AOT source generators all included.

## Evaluating Expressions

`AlderEngine` is the entry point. Pass a C# expression as a string, get the result:

```csharp
var engine = new AlderEngine();

var result = engine.Evaluate("""
    new[] { "Alice", "Bob", "Charlie" }
        .Where(name => name.Length > 3)
        .Select(name => name.ToUpper())
        .ToList()
    """);
// List<string> { "ALICE", "CHARLIE" }
```
<!-- test: QuickStart_LinqChain.csx -->

This isn't string manipulation — Alder runs your expression through the same phases a production compiler uses: lexing, parsing, semantic binding, type resolution, and operator dispatch. LINQ lambdas, generic type inference, extension method resolution — it all works because Alder implements C# semantics, not a simplified subset.

When you know the expected return type, `Evaluate<T>` applies standard C# conversion rules and saves you the cast:

```csharp
string result = engine.Evaluate<string>("""
    $"Today is {DateTime.Now:dddd}, and 2^10 = {Math.Pow(2, 10)}"
    """);
// "Today is Thursday, and 2^10 = 1024"
```
<!-- test: QuickStart_StringInterpolation.csx -->

If the expression returns `int` and you ask for `long`, the implicit conversion handles it. If the types are genuinely incompatible, you get a diagnostic naming the exact source and target types — not a silent `null` or a vague `InvalidCastException`.

## Variable Injection

Most expressions need data from the host application. Alder provides three injection patterns:

| Pattern | Best for | Trade-off |
|---------|----------|-----------|
| `SetVariable<T>` | Server apps, reused engines | Best performance — binder knows the type |
| Anonymous object | Quick one-off evaluations | Reflection cost per call |
| `IDictionary<string, object?>` | Dynamic keys from config or user input | Values typed as `object` |

### `SetVariable<T>` — typed, persistent

When you provide the type explicitly, Alder's binder resolves members and operators at bind time instead of deferring to runtime reflection. This produces faster evaluation, enables AOT dispatch through the source generator, and gives you precise diagnostics when a member doesn't exist on the type.

```csharp
var engine = new AlderEngine();
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

double avg = engine.Evaluate<double>("""
    scores.Where(s => s >= 70).Average()
    """);
// 87.75
```
<!-- test: QuickStart_SetVariableTyped.csx -->

Variables persist across evaluations and `SetVariable<T>` returns the engine for fluent chaining. Because the binder knows `scores` is `List<int>`, it resolves `.Where()`, `.Average()`, and the lambda parameter types at bind time — no runtime guessing.

### Anonymous object — inline, scoped

For one-off evaluations where you don't want to touch the engine's state, pass an anonymous object. Its public properties become variables for that single call:

```csharp
bool eligible = engine.Evaluate<bool>(
    "age >= 18 && country != null && country.Length == 2",
    new { age = 25, country = "US" }); // true
```
<!-- test: QuickStart_AnonymousObject.csx -->

The engine's variable store is untouched — nothing is added, nothing persists. Internally, Alder reads the object's public properties via reflection on each call, so for tight loops prefer `SetVariable<T>` instead.

### `IDictionary<string, object?>` — dynamic keys, scoped

When variable names come from configuration, user input, or a database — anywhere the keys aren't known at compile time — pass a dictionary:

```csharp
var vars = new Dictionary<string, object?>
{
    ["threshold"] = 100,
    ["multiplier"] = 1.5
};
double result = engine.Evaluate<double>("threshold * multiplier", vars); // 150.0
```
<!-- test: QuickStart_DictionaryVariables.csx -->

Like anonymous objects, dictionary variables are scoped to the call and don't modify engine state. Because values are typed as `object`, member resolution happens through runtime reflection rather than at bind time.

## Parsing and Reuse

Every `Evaluate(string)` call re-lexes and re-parses the expression from scratch. For repeated evaluation of the same expression, parse once and pass the `AlderExpression`:

```csharp
AlderExpression expr = engine.Parse("""
    items.Where(x => x.Price > minPrice).Sum(x => x.Price * x.Quantity)
    """);

// Lexing and parsing happen once. Binding is cached on the expression.
var names = expr.GetVariables(); // ["items", "minPrice"]
```
<!-- test: QuickStart_ParseAndReuse.csx -->

The `AlderExpression` also caches the bound tree — the result of type resolution, overload selection, and operator dispatch. When the same expression is evaluated with the same variable types, binding is skipped entirely. In a web server evaluating the same formula across requests, the difference is significant.

## Error Handling

`TryEvaluate` returns `bool` instead of throwing, covering parse errors, binding errors, and runtime failures in one call:

```csharp
if (engine.TryEvaluate("""(string)null ?? "fallback" """, out object? result))
    Console.WriteLine(result); // "fallback"

// Invalid expressions return false — no exception overhead
if (!engine.TryEvaluate("items.Where(", out _))
    Console.WriteLine("Expression has a syntax error");
```
<!-- test: QuickStart_TryEvaluate.csx -->

For finer granularity — distinguishing syntax errors from type errors from runtime failures — use `TryParse` and `TryValidate` independently.

## Configuration

`AlderOptions` controls language mode, security policy, execution limits, and type access. Configuration is captured at construction time and cannot be changed afterward — the engine is immutable once created.

```csharp
var engine = new AlderEngine(o =>
{
    o.LanguageMode = LanguageMode.Standard;
    o.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxLoopIterations = 1_000,
        MaxTimeout = TimeSpan.FromSeconds(5)
    };
});

engine.Evaluate<int>("Math.Max(10, 20)"); // 20
```
<!-- test: QuickStart_OptionsConfiguration.csx -->

| Mode | Behavior |
|------|----------|
| `Standard` | C# expression semantics per ECMA-334 |
| `Extended` | Superset — adds comparison chaining, pipeline operators, built-in aggregates, ranges |

`ExecutionConstraints` caps statement count, loop iterations, and wall-clock time. In a multi-tenant system or anywhere you evaluate untrusted input, these limits are the difference between a responsive service and a hung process.

## Compiled Evaluation

By default, Alder interprets expressions by walking the bound tree. For expressions evaluated thousands of times — pricing formulas, rule engines, real-time filters — switch to compiled mode, which emits IL and produces a native delegate:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

// Evaluate automatically compiles on first call — no manual Compile step needed
string result = engine.Evaluate<string>("""
    string.Join(", ", new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x))
    """);
// "1, 3, 4, 5"
```
<!-- test: QuickStart_CompiledEvaluation.csx -->

When `UseCompiler()` is configured, every `Evaluate` call transparently compiles the expression on first execution and caches the delegate. Subsequent evaluations of the same expression skip compilation entirely and invoke the delegate directly.

For hot paths where you want to eliminate even the engine dispatch overhead, `CompileToFunc<T>` returns a bare delegate:

```csharp
Func<double?> area = engine.CompileToFunc<double>("Math.PI * r * r");
engine.SetVariable<double>("r", 5.0);
double? result = area(); // 78.539816...
```

See [AlderEngine](../engine-api/alder-engine.md) for the full API including `Compile<T>`, `CompileToFunc<T>`, `ParseAsExpression<TDelegate>`, and pre-compilation with `ParseAndCompile`.

## Further Reading

- [AlderEngine](../engine-api/alder-engine.md) — the full engine API
- [AlderOptions](../engine-api/alder-options.md) — sandbox presets, type registration, execution limits
