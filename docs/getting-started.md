---
title: "Getting Started"
description: "Install Alder, evaluate your first C# code, and explore the API"
sidebar:
  order: 1
---

Alder is a C# runtime engine for .NET. Pass C# code as a string, get a result — from a simple calculation to a full program with variables, LINQ, control flow, pattern matching, and exception handling. ECMA-334 semantics, two execution backends, security sandboxing, AOT support. One package, zero dependencies.

```bash
dotnet add package Alder
```

## Evaluate an expression

```csharp
var engine = new AlderEngine();
int result = engine.Evaluate<int>("(5 + 3) * 2"); // 16
```

<!-- test: GettingStarted_SimpleExpression -->

## Query a collection

```csharp
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

double avg = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");
// 87.75
```

<!-- test: GettingStarted_SetVariable -->

The generic type parameter in `SetVariable<T>` tells the binder the variable's type during semantic analysis. `.Where()`, `.Average()`, and the lambda parameter `s` are all resolved at bind time — producing faster evaluation, AOT compatibility, and precise diagnostics when a member doesn't exist.

For one-off evaluations, pass an anonymous object — its properties become variables scoped to that single call:

```csharp
bool eligible = engine.Evaluate<bool>(
    "age >= 18 && country != null",
    new { age = 25, country = "US" }); // true
```

<!-- test: GettingStarted_AnonymousObject -->

## Statements, control flow, and beyond

```csharp
engine.SetVariable<int>("score", 82);

string grade = engine.Evaluate<string>("""
    var letter = score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
    var passed = score >= 60;
    return $"{letter} ({score}) — {(passed ? "Pass" : "Fail")}";
    """);
// "B (82) — Pass"
```

<!-- test: GettingStarted_ControlFlow -->

Variable declarations, switch expressions with relational patterns, ternary operators, string interpolation, and `return` — all in one evaluation. The code passes through a full compiler pipeline: lexer, parser, semantic binder with ECMA-334 type inference, optimization passes, then execution.

## Parse once, evaluate many

Every `Evaluate(string)` call re-lexes, re-parses, and re-binds. When the same expression runs repeatedly — a pricing formula across thousands of orders, a filter predicate on every row — parse once and reuse:

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");

engine.SetVariable<double>("price", 100.0);
engine.SetVariable<double>("discount", 0.1);
double result1 = engine.Evaluate<double>(expr); // 90.0

engine.SetVariable<double>("price", 250.0);
double result2 = engine.Evaluate<double>(expr); // 225.0
```

<!-- test: GettingStarted_ParseReuse -->

The `AlderExpression` caches the bound tree. When variable types haven't changed, binding is skipped entirely — only execution runs.

## Handle errors without exceptions

```csharp
if (!engine.TryEvaluate("items.Where(", out _))
    Console.WriteLine("Syntax error"); // no exception thrown
```

<!-- test: GettingStarted_TryEvaluate -->

`TryEvaluate` returns `false` for parse, binding, and runtime failures. `TryParse` checks syntax only. `TryValidate` performs full semantic analysis — catching type errors, missing members, and invalid operations — without executing:

```csharp
if (!engine.TryValidate("undefinedVar + 1", out var diagnostics))
    Console.WriteLine($"{diagnostics[0].FormattedCode}: {diagnostics[0].Message}");
    // CS0103: The name 'undefinedVar' does not exist in the current context
```

<!-- test: GettingStarted_TryValidate -->

## Compile to native IL

When an expression runs on a hot path, the engine compiles the bound tree into native .NET IL and caches the delegate:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

// First call: parse → bind → compile to IL → execute
// Subsequent calls: execute cached delegate
string result = engine.Evaluate<string>("""
    string.Join(", ", new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x))
    """);
// "1, 3, 4, 5"
```

<!-- test: GettingStarted_Compiled -->

The compiler backend is swappable — implement `IExpressionCompiler` to plug in any alternative LINQ expression tree compiler. See [Compilation](engine/compilation.md).

## LINQ Dynamic

Every `IEnumerable<T>` and `IQueryable<T>` becomes dynamically queryable at runtime. User-defined filters in dashboards, configuration-driven business rules, report builders where users choose projections and groupings — all with full type safety from `T`:

```csharp
AlderEval.Configure(o => o.UseCompiler());

var engineers = people.WhereDynamic("x => x.Department == \"Engineering\"");
var names = people.SelectDynamic<Person, string>("x => x.Name");
var totalSalary = people.SumDynamic("x => x.Salary");
```

<!-- test: GettingStarted_LinqDynamic -->

On `IQueryable<T>`, LINQ Dynamic produces `Expression<Func<T, bool>>` trees that EF Core translates to SQL. See [LINQ Dynamic](engine/linq-dynamic.md).

## Secure evaluation

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

Security is enforced as a pipeline pass before execution begins. The entire bound tree is validated against the policy — if any node violates a permission, evaluation never starts. No partial execution, no side effects.

Three presets: `Trusted()` (full access, the default), `Safe()` (property reads and assignment, no method calls or construction), `Strict()` (property reads only). Default deny lists cover file I/O, networking, process execution, reflection, and threading. See [Security](security/index.md).

## Step-by-step tracing

```csharp
var trace = engine.EvaluateWithTrace("""
    var items = new[] { 1, 2, 3 };
    var squared = items.Select(x => x * x).ToList();
    return squared.Sum();
    """);

Console.WriteLine(trace.Result); // 14
Console.WriteLine(trace.Tree);   // step-by-step evaluation tree
```

<!-- test: GettingStarted_Tracing -->

Each node in the trace shows the expression, computed value, runtime type, and any errors — a complete picture of how the engine arrived at its result.

## NativeAOT and Unity IL2CPP

Alder runs on every .NET platform, including NativeAOT and Unity IL2CPP. An incremental source generator emits typed dispatch code at compile time — property access, method invocation, constructor calls — with no reflection at runtime:

```csharp
[AlderRegistered(typeof(List<int>))]
[AlderRegistered(typeof(DateTime))]
public partial class MyTypeContext : AlderTypeContext { }

var engine = new AlderEngine(o =>
{
    o.Aot.UseGeneratedContext(new MyTypeContext());
});
```

<!-- test: GettingStarted_AOT -->

Same API, same behavior, single NuGet package. See [AOT](aot/index.md).

## Extended mode

A strict superset of Standard C#. Every Standard expression works in Extended mode, plus additional operators and sugar:

```csharp
var ext = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);

ext.Evaluate("2 ** 10");                                          // 1024.0 (power)
ext.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]");           // [4, 16, 36, 64, 100]
ext.Evaluate("5 |> (x => x * 2)");                                // 10 (pipeline)
ext.Evaluate("""new DateTime(2026, 1, 1) + 30.days""");            // date arithmetic
```

<!-- test: GettingStarted_ExtendedMode -->

Power operator, pipeline, chained comparisons, collection comprehensions, `let..in`, bare math functions (`sin`, `cos`, `sqrt`), aggregate built-ins (`sum`, `avg`), date/time sugar, SQL-style operators (`in`, `like`, `between`), and more. See [Extended Mode](language/extended.md).

## Async/Await

Alder supports `await` in expressions — call async .NET APIs directly from dynamically evaluated code:

```csharp
var engine = new AlderEngine();

object? result = await engine.EvaluateAsync("""
    var data = await Task.FromResult(new[] { 1, 2, 3 });
    data.Sum()
    """);
// 6
```

`EvaluateAsync` is required for expressions containing `await`. It also works for non-async expressions — making it a safe default for any evaluation path. Expressions with `await` always run on the interpreted backend; non-async expressions use whichever backend is configured (interpreted or compiled).

See [Async/Await](engine/async.md) for the full guide, including limitations and the LINQ expression tree constraint.

## Further reading

| Topic | Description |
|-------|-------------|
| [Engine API](engine/index.md) | AlderEngine, AlderOptions, variables, compilation, functions, modules, diagnostics |
| [Language Reference](language/index.md) | Every C# construct Alder supports — Standard and Extended modes |
| [Async/Await](engine/async.md) | Await async .NET APIs from dynamic expressions |
| [LINQ Dynamic](engine/linq-dynamic.md) | String-based LINQ on any IEnumerable\<T\> or IQueryable\<T\> |
| [Security](security/index.md) | Sandbox presets, type blocking, execution limits |
| [Architecture](architecture/index.md) | Compiler pipeline internals — binder, overload resolution, type inference, interpreter, IL compiler |
| [AOT](aot/index.md) | Source generators, two-tier dispatch, NativeAOT, Unity/IL2CPP |
