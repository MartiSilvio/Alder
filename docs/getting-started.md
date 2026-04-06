Alder is a lightweight C# runtime engine. A complete compiler pipeline (lexer, parser, semantic binder, optimization passes, two execution backends) in a single NuGet package with zero dependencies. Alder delegates to .NET wherever possible: `.Where()` calls the real `Enumerable.Where`, `Math.Round` calls the real `Math.Round`, conversions follow CLR rules. The engine bridges dynamic evaluation to .NET, it doesn't reimplement it.

```bash
dotnet add package Alder
```

One package. .NET 8+ and .NET Standard 2.0. Zero dependencies on .NET 8+. AOT-compatible out of the box.

## Evaluate C# at runtime

```csharp
using Alder;

var engine = new AlderEngine();
int result = engine.Evaluate<int>("(5 + 3) * 2"); // 16
```

Three ways to evaluate: instance method, `AlderEval` static, or string extensions.

```csharp
AlderEval.Evaluate<int>("1 + 2");    // static
"1 + 2".Evaluate<int>();              // string extension
```

See [Evaluation](evaluation.md) for all overloads, variable patterns, compilation options, and async.

## Inject variables

```csharp
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });
double avg = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()"); // 87.75
```

`SetVariable<T>` gives the binder the variable's type. LINQ, member access, and overload resolution are resolved at bind time. Anonymous objects and dictionaries also work:

```csharp
engine.Evaluate<bool>("age >= 18", new { age = 25, country = "US" }); // true
```

See [Variables](engine/variables.md) for typed vs untyped injection, child engines, scoping, and concurrency.

## Statements, control flow, pattern matching

```csharp
engine.Evaluate<string>("""
    return score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        _ => "F"
    };
    """);
```

Variable declarations, LINQ (method + query syntax), pattern matching (11 pattern types), switch expressions, iterators (`yield return`/`yield break`), async/await, lambdas, exception handling, `using`/`lock`, `goto`. Full ECMA-334 semantics. See [Standard Mode](language/standard.md).

## Compile to native IL

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

var compiled = engine.Compile<int>("Enumerable.Range(1, n).Sum()");
engine.SetVariable<int>("n", 1000);
compiled.Invoke(); // 500500
```

Two backends: interpreter (76 source-generated evaluators) and IL compiler (local promotion, identifier hoisting, native delegates). `CompileToFunc<T>` for raw delegates, `ParseAsExpression<TDelegate>` for EF Core expression trees. See [Compilation](engine/compilation.md).

## LINQ Dynamic

```csharp
AlderEval.Configure(o => o.UseCompiler());

var engineers = people.WhereDynamic("x => x.Department == \"Engineering\"");
var total = people.SumDynamic("x => x.Salary");
```

String-based LINQ on any `IEnumerable<T>` or `IQueryable<T>`. On `IQueryable<T>`, produces expression trees that EF Core translates to SQL. See [LINQ Dynamic](engine/linq-dynamic.md).

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

Security is a pipeline pass that validates the entire expression tree **before execution**. Eight permission flags, four-layer type blocking, execution limits. See [Security](security/sandbox.md).

## NativeAOT and IL2CPP

```csharp
[AlderRegistered(typeof(List<int>))]
[AlderRegistered(typeof(DateTime))]
public partial class MyTypeContext : AlderTypeContext { }

var engine = new AlderEngine(o => o.Aot.UseGeneratedContext(new MyTypeContext()));
```

Incremental source generator emits reflection-free typed dispatch. Same API on every platform. See [AOT](aot/overview.md).

## Extended mode

```csharp
var ext = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);

ext.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]");  // [4, 16, 36, 64, 100]
ext.Evaluate("5 |> (x => x * 2)");                        // 10
ext.Evaluate("""now() + 30.days""");                       // date arithmetic
```

Power, pipeline, chained comparisons, comprehensions, bare math, aggregates, date/time sugar, SQL operators. See [Extended Mode](language/extended.md).

## Further reading

|                                            |                                                                 |
| ------------------------------------------ | --------------------------------------------------------------- |
| **[Evaluation](evaluation.md)**            | All evaluation methods, variable patterns, compilation, async   |
| **[Standard Mode](language/standard.md)**  | Full ECMA-334 language reference                                |
| **[Extended Mode](language/extended.md)**  | Power, pipeline, comprehensions, bare math, SQL operators       |
| **[Engine API](engine/index.md)**          | AlderEngine, AlderOptions, functions, modules, diagnostics      |
| **[LINQ Dynamic](engine/linq-dynamic.md)** | String-based LINQ on IEnumerable\<T\> and IQueryable\<T\>       |
| **[Security](security/sandbox.md)**        | Sandbox presets, type blocking, execution limits                |
| **[AOT](aot/overview.md)**                 | Source generators, typed dispatch, NativeAOT/IL2CPP             |
| **[Architecture](architecture/index.md)**  | Pipeline internals: binder, overload resolution, type inference |
