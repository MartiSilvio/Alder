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
object? result = engine.Evaluate("2 + 3 * 4"); // 14
```
<!-- test: QuickStart_BasicEvaluation.csx -->

This isn't string manipulation — Alder runs your expression through the same phases a production compiler uses: lexing, parsing, semantic binding, type resolution, and operator dispatch. The difference is it happens at runtime, not compile time.

When you know the expected return type, `Evaluate<T>` applies standard C# conversion rules and saves you the cast:

```csharp
string upper = engine.Evaluate<string>("""
    "hello".ToUpper()
    """); // "HELLO"
```
<!-- test: QuickStart_GenericEvaluation.csx -->

If the expression returns `int` and you ask for `long`, the implicit conversion handles it. If the types are genuinely incompatible, you get a diagnostic naming the exact source and target types — not a silent `null` or a vague `InvalidCastException`.

## Variable Injection

Most expressions need data from the host application. Alder provides three injection patterns:

| Pattern | Scope | Type info | Best for |
|---------|-------|-----------|----------|
| `SetVariable<T>` | Persistent on engine | Compile-time type | Server apps, reused engines |
| Anonymous object | Single `Evaluate` call | Reflected per call | Quick one-off evaluations |
| `IDictionary<string, object?>` | Single `Evaluate` call | Runtime (`object`) | Dynamic keys from config or user input |

### `SetVariable<T>` — typed, persistent

When you provide the type explicitly, Alder's binder resolves members and operators at bind time instead of deferring to runtime reflection. This produces faster evaluation, enables AOT dispatch through the source generator, and gives you precise diagnostics when a member doesn't exist on the type.

```csharp
var engine = new AlderEngine();
engine.SetVariable<int>("x", 10)
      .SetVariable<int>("y", 5);

int sum = engine.Evaluate<int>("x + y"); // 15
```
<!-- test: QuickStart_VariableInjection.csx -->

Variables persist across evaluations and `SetVariable<T>` returns the engine for fluent chaining. This is the pattern you want for long-lived engines in server applications.

### Anonymous object — inline, scoped

For one-off evaluations where you don't want to touch the engine's state, pass an anonymous object. Its public properties become variables for that single call:

```csharp
double total = engine.Evaluate<double>(
    "price * (1 + tax)",
    new { price = 100, tax = 0.2 });  // 120.0
```
<!-- test: QuickStart_AnonymousObjectVariables.csx -->

The engine's variable store is untouched — nothing is added, nothing persists. Internally, Alder reads the object's public properties via `GetProperties` + `GetValue` on each call, so for tight loops prefer `SetVariable<T>` instead.

### `IDictionary<string, object?>` — dynamic keys, scoped

When variable names come from configuration, user input, or a database — anywhere the keys aren't known at compile time — pass a dictionary:

```csharp
var vars = new Dictionary<string, object?> { ["price"] = 100, ["tax"] = 0.2 };
double total = engine.Evaluate<double>("price * (1 + tax)", vars); // 120.0
```
<!-- test: QuickStart_AnonymousObjectVariables.csx -->

Like anonymous objects, dictionary variables are scoped to the call and don't modify engine state. Because values are typed as `object`, member resolution happens through runtime reflection rather than at bind time.

## Parsing and Reuse

Parsing and binding are the expensive phases — type resolution, overload selection, and operator dispatch all happen here. For repeated evaluation of the same expression, parse once and reuse the `AlderExpression`:

```csharp
AlderExpression expr = engine.Parse("""$"Result: {42 * 2}" """);

string a = engine.Evaluate<string>(expr); // "Result: 84"
string b = engine.Evaluate<string>(expr); // "Result: 84"
```
<!-- test: QuickStart_ParseAndReuse.csx -->

The `AlderExpression` caches the fully bound tree. Subsequent evaluations skip parsing and binding entirely and go straight to execution. In a web server evaluating the same formula across requests, the difference is significant.

## Error Handling

`TryEvaluate` returns `bool` instead of throwing, covering parse errors, binding errors, and runtime failures in one call:

```csharp
if (engine.TryEvaluate("""(string)null ?? "default" """, out object? result))
    Console.WriteLine(result); // "default"
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

var expr = engine.Parse("x * x + y * y");
engine.Compile(expr);

// Now runs as compiled IL, not interpretation
int result = engine.Evaluate<int>(expr,
    new Dictionary<string, object?> { ["x"] = 3, ["y"] = 4 }); // 25
```

Compiled expressions run at near-native speed — typically 4–9x the cost of a raw C# delegate, compared to 300–500x for interpreted mode. The compilation cost is paid once; every subsequent evaluation reuses the compiled delegate.

See [Compilation Modes](../engine-api/compilation-modes.md) for the full API including `AlderCompiledExpression<T>` and LINQ Dynamic extensions.

## Further Reading

- [AlderEngine](../engine-api/alder-engine.md) — complete API reference
- [Variables and Context](../engine-api/variables-and-context.md) — scoping, precedence, child engines
- [AlderOptions](../engine-api/alder-options.md) — sandbox presets, type registration, execution limits
- [Thread Safety](../engine-api/thread-safety.md) — concurrent evaluation patterns
