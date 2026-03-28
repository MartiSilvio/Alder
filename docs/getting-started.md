---
title: "Getting Started"
description: "Install Alder, evaluate your first expression, and explore the API"
sidebar:
  order: 1
---

Alder evaluates C# expressions at runtime. Pass a string, get a result — with full LINQ, lambdas, pattern matching, generic type inference, and overload resolution. Expressions run through a real compilation pipeline (lexer, parser, semantic binder, optimization passes) and execute through either a tree-walking interpreter or an IL compiler that produces native delegates. Security, AOT, and NativeAOT/Unity IL2CPP are built in.

```bash
dotnet add package Alder
```

One package, zero dependencies. Interpreted evaluation, IL compilation, AOT source generators, security sandbox, LINQ Dynamic, and Extended mode — all included with no third-party dependencies to manage.

## Evaluate an expression

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

<!-- test: GettingStarted_LinqChain -->

The expression passes through a full compiler pipeline. The binder resolves `Where` as `System.Linq.Enumerable.Where<string>`, infers the lambda parameter type from the generic constraint, and the engine invokes the real LINQ methods on the real .NET types.

## Inject variables

```csharp
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

double avg = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");
// 87.75
```

<!-- test: GettingStarted_SetVariable -->

The generic type parameter in `SetVariable<T>` is significant. It tells the binder the variable's type during semantic analysis — `.Where()`, `.Average()`, and the lambda parameter `s` are all resolved at bind time, not through runtime reflection. This produces faster evaluation, enables AOT dispatch, and gives precise diagnostics when a member doesn't exist.

For one-off evaluations, pass an anonymous object — its properties become variables scoped to that single call:

```csharp
bool eligible = engine.Evaluate<bool>(
    "age >= 18 && country != null",
    new { age = 25, country = "US" }); // true
```

<!-- test: GettingStarted_AnonymousObject -->

## Parse once, evaluate many

Every `Evaluate(string)` call re-lexes, re-parses, and re-binds. When you evaluate the same expression repeatedly (a pricing formula across thousands of orders, a filter predicate on every row), parse once:

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");

engine.SetVariable<double>("price", 100.0);
engine.SetVariable<double>("discount", 0.1);
double result1 = engine.Evaluate<double>(expr); // 90.0

engine.SetVariable<double>("price", 250.0);
double result2 = engine.Evaluate<double>(expr); // 225.0
```

<!-- test: GettingStarted_ParseReuse -->

The `AlderExpression` caches the bound tree. When the same expression is evaluated with the same variable types, binding is skipped entirely — only execution runs.

## Handle errors without exceptions

```csharp
if (!engine.TryEvaluate("items.Where(", out _))
    Console.WriteLine("Syntax error"); // no exception thrown
```

<!-- test: GettingStarted_TryEvaluate -->

`TryEvaluate` returns `false` for parse, binding, and runtime failures. For finer granularity, `TryParse` checks syntax only, and `TryValidate` performs full semantic analysis without executing.

## Compile to IL

For expressions evaluated thousands of times, switch to compiled mode. The engine emits IL through LINQ expression trees and caches the native delegate:

```csharp
var compiled = new AlderEngine(o => o.UseCompiler());

// First call: parse → bind → compile to IL → execute
// Subsequent calls: execute cached delegate
string result = compiled.Evaluate<string>("""
    string.Join(", ", new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x))
    """);
// "1, 3, 4, 5"
```

<!-- test: GettingStarted_Compiled -->

When `UseCompiler()` is configured, `Evaluate` automatically compiles on first execution. There is no manual compilation step for standard use.

## Configure security and limits

```csharp
var engine = new AlderEngine(o =>
{
    o.Sandbox = SandboxOptions.Safe();
    o.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxLoopIterations = 1_000,
        MaxTimeout = TimeSpan.FromSeconds(5)
    };
});
```

<!-- test: GettingStarted_SecurityAndLimits -->

`SandboxOptions.Safe()` blocks method calls and object construction while allowing property reads (instance and static) and assignment. Three presets are available: `Trusted()` (no restrictions), `Safe()`, and `Strict()` (read-only). Type and namespace blocking provides finer control. See [Security](security/index.md).

## Further reading

| Topic | Description |
|-------|-------------|
| [Engine API](engine/index.md) | AlderEngine, AlderOptions, variables, compilation, functions, modules, diagnostics |
| [LINQ Dynamic](engine/linq-dynamic.md) | String-based LINQ on any IEnumerable\<T\> or IQueryable\<T\> — runtime filtering, projection, aggregation |
| [Language Reference](language/index.md) | Every C# construct Alder supports — Standard and Extended modes |
| [Security](security/index.md) | Sandbox presets, type blocking, execution limits — evaluating untrusted expressions safely |
| [Architecture](architecture/index.md) | Compiler pipeline internals — binder, overload resolution, type inference, interpreter, IL compiler |
| [AOT](aot/index.md) | Source generators, two-tier dispatch, NativeAOT, Unity/IL2CPP |
