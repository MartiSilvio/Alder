Alder is a C# runtime engine. A complete compiler pipeline (lexer, parser, semantic binder, optimization passes, two execution backends) in a single NuGet package with zero dependencies. Pass C# code as a string, get a result. From `1 + 2` to multi-statement programs with LINQ, pattern matching, generic type inference, async/await, iterators, exception handling, and ECMA-334 overload resolution. Alder delegates to .NET wherever possible: `.Where()` calls the real `Enumerable.Where`, `Math.Round` calls the real `Math.Round`, type conversions follow the real CLR rules. The engine bridges dynamic evaluation to .NET's actual runtime, it doesn't reimplement it.

```bash
dotnet add package Alder
```

One package. .NET 8+ and .NET Standard 2.0. Zero dependencies on .NET 8+. AOT-compatible out of the box.

## Evaluate an expression

```csharp
using Alder;

var engine = new AlderEngine();
int result = engine.Evaluate<int>("(5 + 3) * 2"); // 16
```

## Query a collection

```csharp
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

double avg = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");
// 87.75
```

`SetVariable<T>` gives the binder the variable's type at semantic analysis time. The binder resolves `.Where()` to `Enumerable.Where<int>`, infers `s` as `int` through §12.6.3 generic type inference, and selects `.Average()` via overload resolution. All at bind time.

For one-off evaluations, pass an anonymous object:

```csharp
bool eligible = engine.Evaluate<bool>(
    "age >= 18 && country != null",
    new { age = 25, country = "US" }); // true
```

## Statements and control flow

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
    return $"{letter} ({score})";
    """);
// "B (82)"
```

Variable declarations, switch expressions with relational patterns (§11.2.3), string interpolation, `return`. Five-stage pipeline: lexing, parsing, semantic binding with type inference, optimization passes (constant folding, dead branch elimination), execution.

## Pattern matching

The complete ECMA-334 §11.2 pattern system: constant, type, relational, logical (`and`/`or`/`not`), property, positional, list, slice, var, discard.

```csharp
engine.SetVariable<object>("shape", new { Kind = "circle", Radius = 5.0 });

string desc = engine.Evaluate<string>("""
    return shape switch
    {
        { Kind: "circle", Radius: > 10 } => "large circle",
        { Kind: "circle", Radius: var r } => $"circle r={r}",
        { Kind: "rect" } => "rectangle",
        _ => "unknown"
    };
    """);
// "circle r=5"
```

## Iterators

Local functions with `yield return` produce lazy sequences:

```csharp
var result = engine.Evaluate<List<int>>("""
    IEnumerable<int> Fib()
    {
        var a = 0;
        var b = 1;
        while (true)
        {
            yield return a;
            var temp = a;
            a = b;
            b = temp + b;
        }
    }
    return Fib().Take(10).ToList();
    """);
// [0, 1, 1, 2, 3, 5, 8, 13, 21, 34]
```

Infinite sequences, lazy evaluation, full control flow inside the body. The returned `IEnumerable<T>` works with every LINQ operator.

## Async/await

```csharp
var result = await engine.EvaluateAsync<int>("""
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    return a + b;
    """);
// 30
```

Unwraps `Task<T>`, `Task`, `ValueTask<T>`, `ValueTask`. Async lambdas: `async x => await Task.FromResult(x * 2)`. `CancellationToken` is automatically injected into method calls that accept one as their last parameter.

## Global static engine

`AlderEval` provides a shared engine for quick evaluation anywhere in your app:

```csharp
AlderEval.Configure(o => o.UseCompiler());

int result = AlderEval.Evaluate<int>("1 + 2"); // 3
```

Configure once at startup. Evaluate from anywhere. String extensions work too:

```csharp
int result = "1 + 2".Evaluate<int>(); // 3
```

## Compile to native IL

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

// First call: parse, bind, emit expression tree, compile to IL, execute
// Subsequent calls: execute cached native delegate
engine.Evaluate<int>("Enumerable.Range(1, 10).Sum()"); // 55
```

For hot paths, `Compile<T>` returns a reusable wrapper that bypasses engine dispatch:

```csharp
var compiled = engine.Compile<int>("""
    Enumerable.Range(1, n).Where(x => x % 3 == 0 || x % 5 == 0).Sum()
    """);
engine.SetVariable<int>("n", 1000);
compiled.Invoke(); // 233168
```

`CompileToFunc<T>` gives you a raw `Func<T?>` with zero abstraction:

```csharp
engine.SetVariable<double>("r", 5.0);
Func<double?> area = engine.CompileToFunc<double>("Math.PI * r * r");
area(); // ~78.54
```

The compiler backend is swappable via `IExpressionCompiler`. See [Compilation](engine/compilation.md).

## Error handling

```csharp
if (!engine.TryEvaluate("items.Where(", out _))
    Console.WriteLine("Syntax error"); // no exception thrown
```

`TryEvaluate` returns `false` on any failure. `TryValidate` runs full semantic analysis without executing:

```csharp
engine.SetVariable<string>("name", "Alice");

if (!engine.TryValidate("name.Foo()", out var diagnostics))
    Console.WriteLine($"{diagnostics[0].FormattedCode}: {diagnostics[0].Message}");
    // CS1061: 'String' does not contain a definition for 'Foo'
```

Roslyn-compatible error codes (`CS0103`, `CS1061`, `CS0029`), structured diagnostics with line/column positions.

## Parse once, evaluate many

For repeated evaluation of the same expression with different variable values:

```csharp
AlderExpression expr = engine.Parse("price * (1 - discount)");

engine.SetVariable<double>("price", 100.0);
engine.SetVariable<double>("discount", 0.1);
double r1 = engine.Evaluate<double>(expr); // 90.0

engine.SetVariable<double>("price", 250.0);
double r2 = engine.Evaluate<double>(expr); // 225.0
```

The bound tree is cached. When variable types haven't changed, binding is skipped entirely.

## LINQ Dynamic

Every `IEnumerable<T>` and `IQueryable<T>` becomes dynamically queryable with string-based lambdas:

```csharp
AlderEval.Configure(o => o.UseCompiler());

var engineers = people.WhereDynamic("x => x.Department == \"Engineering\"");
var names = people.SelectDynamic<Person, string>("x => x.Name");
var totalSalary = people.SumDynamic("x => x.Salary");
```

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

Security is enforced as a bound tree pipeline pass **before execution begins**. The entire expression tree is validated against the policy. If any node violates a permission, evaluation never starts.

Three presets: `Trusted()` (full access, default), `Safe()` (property reads and assignment, no method calls or construction), `Strict()` (read-only). Default deny lists cover file I/O, networking, process execution, reflection, threading. See [Security](security/sandbox.md).

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

Each trace node shows the expression text, computed value, and runtime type.

## NativeAOT and Unity IL2CPP

An incremental source generator emits typed dispatch code at compile time:

```csharp
[AlderRegistered(typeof(List<int>))]
[AlderRegistered(typeof(DateTime))]
public partial class MyTypeContext : AlderTypeContext { }

var engine = new AlderEngine(o =>
{
    o.Aot.UseGeneratedContext(new MyTypeContext());
});
```

Same API, same behavior. See [AOT](aot/overview.md).

## Extended mode

A strict superset of Standard C#:

```csharp
var ext = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);

ext.Evaluate("2 ** 10");                                          // 1024.0
ext.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]");           // [4, 16, 36, 64, 100]
ext.Evaluate("5 |> (x => x * 2)");                                // 10
ext.Evaluate("""new DateTime(2026, 1, 1) + 30.days""");           // 2026-01-31
```

Power, pipeline, chained comparisons (`0 <= x <= 100`), comprehensions, `let..in`, bare math (`sin`, `cos`, `sqrt`), aggregates (`sum`, `avg`), date/time sugar, SQL operators (`in`, `like`, `between`), slicing. See [Extended Mode](language/extended.md).

## Under the hood

| Component | |
|-----------|--|
| **Lexer** | Single-pass. Six string forms including C# 11 raw strings and multi-dollar interpolation. |
| **Parser** | Precedence-climbing with five sub-parsers. LINQ query syntax desugars to method calls at parse time. |
| **Binder** | ECMA-334 semantic analysis. Overload resolution (§12.6.4) with 7 tie-breaking rules. Generic type inference (§12.6.3) with variance-aware bounds and lambda return inference. |
| **Passes** | Pre-execution security validation, constant folding, dead branch elimination, conversion insertion. |
| **Interpreter** | 76 per-node evaluators, source-generated dispatch. Pre-built delegate tables for unboxed numeric operations. Signal-based control flow. |
| **Compiler** | Bound tree to `System.Linq.Expressions`. Local promotion, identifier hoisting. Swappable via `IExpressionCompiler`. |
| **AOT** | Incremental source generator. `switch`/`is` dispatch. Pre-instantiated delegate factories. Generic rooting. |

## Further reading

| | |
|--|--|
| **[Standard Mode](language/standard.md)** | Literals, operators, expressions, statements, pattern matching, LINQ, async/await, iterators, type system |
| **[Extended Mode](language/extended.md)** | Power, pipeline, comprehensions, bare math, aggregates, date/time sugar, SQL operators |
| **[Engine API](engine/index.md)** | AlderEngine, AlderOptions, variables, compilation, functions, modules, diagnostics |
| **[LINQ Dynamic](engine/linq-dynamic.md)** | String-based LINQ on IEnumerable\<T\> and IQueryable\<T\> |
| **[Security](security/sandbox.md)** | Sandbox presets, type blocking, execution limits |
| **[AOT](aot/overview.md)** | Source generators, typed dispatch, NativeAOT/IL2CPP |
| **[Architecture](architecture/index.md)** | Pipeline internals: binder, overload resolution, type inference, interpreter, compiler |
