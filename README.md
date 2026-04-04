<!-- logo placeholder -->

<h1 align="center">Alder</h1>

<p align="center">
<b>The embeddable C# runtime for .NET.</b><br>
Full ECMA-334 language support, two execution backends, safe execution, AOT-ready, zero dependencies.
</p>

<p align="center">
  <a href="#install">Install</a> &middot;
  <a href="#why-alder">Why Alder</a> &middot;
  <a href="#documentation">Docs</a> &middot;
  <a href="#license">License</a>
</p>

---

From simple math to LINQ pipelines, pattern matching, iterators, and async/await:

```csharp
var engine = new AlderEngine();

engine.Evaluate<int>("1 + 2"); // 3
```

```csharp
engine.Evaluate<double>("scores.Where(s => s >= 70).Average()"); // 87.75
```

```csharp
engine.Evaluate<string>("""
    var letter = score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        _ => "F"
    };
    return $"{letter} ({score})";
    """);
// "B (82)"
```

```csharp
engine.Evaluate<List<int>>("""
    IEnumerable<int> Fib()
    {
        var a = 0; var b = 1;
        while (true) { yield return a; var t = a; a = b; b = t + b; }
    }
    return Fib().Take(10).ToList();
    """);
// [0, 1, 1, 2, 3, 5, 8, 13, 21, 34]
```

## Install

```
dotnet add package Alder
```

One package. Zero dependencies on .NET 8+. No Roslyn dependency. No runtime code generation required. Targets `net8.0` and `netstandard2.0`. AOT-compatible out of the box.

## Why Alder

**Full language.** Expressions, statements, LINQ with generic type inference, pattern matching (11 pattern types), switch expressions, async/await, iterators, lambdas, exception handling with `when` guards, `using`/`lock`, `goto`. Per ECMA-334.

**Safe by design.** Security is a pipeline pass, not a runtime check. The entire expression tree is validated against your policy before a single instruction executes. No partial execution, no side effects. Eight permission flags, four-layer type blocking, execution limits.

**Lightweight.** A single NuGet package with zero dependencies on .NET 8+. No compiler SDK. No heavy runtime. The AOT source generator ships inside the same package.

**Two backends.** An interpreter with 76 source-generated evaluators for flexibility. An IL compiler that emits native delegates for speed. Same API, same results, your choice.

**Runs everywhere.** .NET 8+, .NET Standard 2.0, NativeAOT, Unity IL2CPP. The included AOT source generator emits reflection-free typed dispatch. Same behavior on every platform.

**Delegates to .NET.** `.Where()` calls the real `Enumerable.Where`. `Math.Round` calls the real `Math.Round`. Conversions follow CLR rules. Alder bridges dynamic evaluation to .NET, it doesn't reimplement it.

## Capabilities

<details>
<summary><b>Compile to native IL</b></summary>
<br>

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

var compiled = engine.Compile<int>(
    "Enumerable.Range(1, n).Where(x => x % 3 == 0 || x % 5 == 0).Sum()");
engine.SetVariable<int>("n", 1000);
compiled.Invoke(); // 233168
```

First call: parse, bind, emit expression tree, compile to IL, execute. After: execute cached native delegate. Backend is swappable via `IExpressionCompiler`.

</details>

<details>
<summary><b>LINQ Dynamic</b></summary>
<br>

String-based LINQ on any `IEnumerable<T>` or `IQueryable<T>`. On `IQueryable<T>`, produces expression trees that EF Core translates to SQL.

```csharp
AlderEval.Configure(o => o.UseCompiler());

var engineers = people.WhereDynamic("x => x.Department == \"Engineering\"");
var total = people.SumDynamic("x => x.Salary");
```

</details>

<details>
<summary><b>Secure evaluation</b></summary>
<br>

Three presets: `Trusted()` (full access), `Safe()` (property reads + assignment), `Strict()` (read-only). Default deny lists cover file I/O, networking, process execution, reflection, threading.

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

</details>

<details>
<summary><b>Typed variables</b></summary>
<br>

`SetVariable<T>` gives the binder the variable's type at semantic analysis time. Member access, LINQ, and overload resolution are resolved at bind time. Child engines isolate variables for multi-tenant scenarios.

```csharp
engine.SetVariable<List<Order>>("orders", orderList);

var result = engine.Evaluate<string>("""
    var shipped = orders.Where(o => o.Status == "Shipped").ToList();
    return $"{shipped.Count} shipped, ${shipped.Sum(o => o.Total):F2} total";
    """);
```

</details>

<details>
<summary><b>Async/await</b></summary>
<br>

```csharp
var result = await engine.EvaluateAsync<int>("""
    var a = await Task.FromResult(10);
    var b = await Task.FromResult(20);
    return a + b;
    """);
// 30
```

`CancellationToken` is auto-injected into method calls that accept one.

</details>

<details>
<summary><b>NativeAOT and IL2CPP</b></summary>
<br>

Incremental source generator emits typed dispatch at compile time. No reflection on AOT platforms.

```csharp
[AlderRegistered(typeof(List<int>))]
[AlderRegistered(typeof(DateTime))]
public partial class MyTypeContext : AlderTypeContext { }

var engine = new AlderEngine(o => o.Aot.UseGeneratedContext(new MyTypeContext()));
```

</details>

<details>
<summary><b>Extended mode</b></summary>
<br>

Features we love from other languages, brought to C#. Comprehensions, pipeline operator, chained comparisons, bare math functions, and more. All valid Standard C# still works unchanged.

```csharp
var ext = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);

ext.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]");  // [4, 16, 36, 64, 100]
ext.Evaluate("5 |> (x => x * 2)");                        // 10
ext.Evaluate("0 <= score <= 100");                         // chained comparison
ext.Evaluate("""now() + 30.days""");                       // date arithmetic
```

Power (`**`), pipeline (`|>`), comprehensions, bare math (`sin`, `cos`, `sqrt`), aggregates (`sum`, `avg`), date/time sugar, SQL operators (`in`, `like`, `between`), slicing, `let..in`, `unless`/`until`.

</details>

## Documentation

- **[Getting Started](docs/getting-started.md)** - Install, evaluate, inject variables, compile, secure, deploy
- **[Standard Mode](docs/language/standard.md)** - Full ECMA-334 language reference
- **[Extended Mode](docs/language/extended.md)** - Power, pipeline, comprehensions, bare math, SQL operators
- **[Engine API](docs/engine/index.md)** - AlderEngine, AlderOptions, variables, compilation, modules, diagnostics
- **[Security](docs/security/sandbox.md)** - Sandbox presets, type blocking, execution limits
- **[AOT](docs/aot/overview.md)** - Source generators, typed dispatch, NativeAOT/IL2CPP
- **[Architecture](docs/architecture/index.md)** - Pipeline internals: binder, overload resolution, type inference, compiler

## License

[MIT](LICENSE)
