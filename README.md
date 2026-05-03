# Alder

<p align="center">
  <img src="https://raw.githubusercontent.com/MartiSilvio/Alder/master/assets/brand/alder-icon.png" alt="Alder" width="96" height="96">
</p>

<p align="center">
  <a href="https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml"><img src="https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml/badge.svg?branch=master" alt=".NET CI"></a>
  <img src="https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet&logoColor=white" alt=".NET 8+">
  <img src="https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white" alt=".NET Standard 2.0">
  <img src="https://img.shields.io/badge/NativeAOT-generated%20dispatch-brightgreen" alt="NativeAOT generated dispatch">
  <img src="https://img.shields.io/badge/dependencies-none-brightgreen" alt="No third-party runtime dependencies">
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>

<p align="center">
  <b>Alder is a C# runtime engine for .NET.</b><br>
  <sub>Stored rules, configurable reports, runtime queries, policy gates, formula engines, tenant-authored expressions, workflow conditions, AI tool-call backends.</sub>
</p>

<p align="center">
  Full C# semantics&nbsp; · &nbsp;Two execution backends&nbsp; · &nbsp;Dynamic LINQ&nbsp; · &nbsp;Expression-tree export&nbsp; · &nbsp;Async&nbsp; · &nbsp;Sandboxed&nbsp; · &nbsp;NativeAOT&nbsp; · &nbsp;Zero dependencies
</p>

Alder accepts C# at runtime: expressions, statement blocks, lambdas, queries. A compiler-style pipeline of lexing, parsing, binding against the host's CLR type surface, sandbox validation, optimization, and evaluation feeds one of two backends. The interpreter runs the bound tree directly. The opt-in compiled backend lowers the same tree to a typed delegate through `System.Linq.Expressions`. Both produce identical results.

## At a glance

- **Standard-mode C#:** statements, control flow, `switch` expressions and statements, `try`/`catch`/`finally`, `using`, `lock`, lambdas, local functions, query expressions, pattern matching, tuples and deconstruction, string interpolation, `typeof`, `nameof`, `default`, `await`, iterators, user-defined operators and conversions, extension methods, and ECMA-334 conversions.
- **Two execution backends:** an interpreter and an opt-in compiled backend, sharing parser, binder, validation pipeline, sandbox, and execution limits. They produce identical results; divergence is a defect.
- **Async-native:** `EvaluateAsync(...)` awaits expression-level work directly inside the bound tree and cooperates with `CancellationToken` and execution constraints. `IAsyncEnumerable<T>`, `await foreach`, and iterators are first-class.
- **Dynamic LINQ:** `IEnumerable<T>`, `IQueryable<T>`, and `IAsyncEnumerable<T>`; filter, project, order, page, group, join, set, element, quantifier, and aggregate operators; reusable `DynamicQueryPlan` fragments.
- **LINQ expression-tree export:** `Expression<TDelegate>` trees verified against EF Core, including grouping, joins, paging, string methods, null-coalescing predicates, and `EF.Property<T>(...)`.
- **Host-controlled sandbox:** type, namespace, and operation-level policy; reflection metadata blocked at evaluation boundaries; execution limits on statements, loops, wall-clock time, and collection size, surfaced as `AlderExecutionLimitException`.
- **NativeAOT-native:** source-generator-backed dispatch metadata via `[AlderRegistered]` declarations on `AlderTypeContext` partials. Reflection fallback under JIT, generated dispatch under AOT.
- **Reuse-first lifecycle:** parsed expressions, cached bound state, compiled delegates, typed delegate compilation through `Compile<TDelegate>(...)`, and `DynamicQueryPlan` for query reuse.
- **Structured diagnostics:** codes (Roslyn `CS####` where applicable, `ALDR####` otherwise), messages, source spans, and `EvaluateWithTrace(...)` for evaluation introspection.
- **Zero third-party runtime dependencies.** Targets `net8.0` and `netstandard2.0`.

## A first look

```csharp
using Alder;

AlderEval.Evaluate<int>("1 + 2");                                   // 3
AlderEval.Evaluate<decimal>("price * 1.2m", new { price = 100m });  // 120m

using var engine = new AlderEngine();

var tier = engine.Evaluate<string>("""
    var t = order switch
    {
        { Total: > 1000m, IsRush: true } => "premium-express",
        { Total: > 1000m }               => "premium",
        { IsRush: true }                 => "express",
        _                                => "standard"
    };
    return t;
    """, new { order });
```

The same engine handles `1 + 2`, ten thousand stored business rules, and a sandboxed async Dynamic LINQ pipeline over an EF Core query. One pipeline, the full spectrum.

## End-to-end integration

```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options =>
{
    options.UseCompiler();
    options.Sandbox = SandboxOptions.Safe();
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});

// Validate untrusted input before evaluation begins.
if (!engine.TryValidate(rule, out var diagnostics))
    return diagnostics;

// Compiled synchronous evaluation against host-shaped types.
var accepted = engine.Evaluate<bool>(rule, new { order, minimum = 500m });

// Awaitable expression body, cooperating with cancellation and constraints.
var quote = await engine.EvaluateAsync<decimal>(
    "await pricing.QuoteAsync(order)",
    new { order, pricing });

// Runtime fragments exported as an Expression tree, translated by EF Core to SQL.
var report = await db.Orders
    .WhereDynamic(engine, """Status == "Open" && Total >= @0""", 250m)
    .OrderByDynamic<Order, decimal>(engine, "Total")
    .SelectDynamic<Order, OrderSummary>(engine, "new { Id, Total }")
    .ToListAsync();
```

One engine. Synchronous compiled, asynchronous interpreted, and provider-translated query, all over the same parser, binder, sandbox, and execution policy.

## Install

```bash
dotnet add package Alder
```

The `Alder` package is the single public package. It ships the runtime, the optional `Alder.Compiled` API surface for JIT-capable consumers, and the source generator that produces AOT generated dispatch metadata.

## What Alder runs

Standard mode (`LanguageMode.Standard`) accepts C# at the expression and statement-block level: locals, assignment, control flow, loops, `switch` statements and expressions, `try`/`catch`/`finally`, `using`, `lock`, lambdas and local functions, query expressions, pattern matching, tuples and deconstruction, string interpolation, `typeof`, `nameof`, `default`, `await`, iterators, user-defined operators and conversions, extension methods, and the implicit and explicit conversions defined by ECMA-334.

The full support matrix is in [Standard mode language support](docs/reference/language/standard-mode-language-support.md).

[Extended mode](docs/concepts/extended-language-mode.md) layers scripting forms on the same parser: pipelines, regex predicates, SQL-style comparisons, ranges, date/time sugar, aggregate helpers. A valid C# expression produces the same result in either mode.

## The runtime engine

The binder is Alder's architectural boundary. Everything before the binder determines what an expression *means*: types, conversions, overload resolution, member targets, assignment legality, control-flow shape, and the points where runtime dispatch is still required. Everything after executes those decisions while preserving sandbox policy and execution limits.

The **interpreter** evaluates the bound tree directly. It is the default synchronous path, the engine for `EvaluateAsync(...)`, and the path used under NativeAOT and trimming-sensitive deployments.

The **compiled backend** lowers the same bound tree to a reusable delegate through `System.Linq.Expressions`. With `UseCompiler()` configured, synchronous `Evaluate(...)` uses that delegate path and recompiles when the relevant type surface changes.

Both backends share the same parser, binder, validation pipeline, sandbox policy, execution limits, and language semantics. They produce identical results. Divergence is a defect.

Architecture: [Architecture](docs/concepts/architecture.md), [Binding system](docs/concepts/binding-system.md), [Execution model](docs/reference/execution-model.md).

## Async expressions

`EvaluateAsync(...)` runs through the interpreter and awaits expression-level asynchronous work directly inside the bound tree.

```csharp
var prices = await engine.EvaluateAsync<decimal[]>(
    """
    var quotes = await pricing.FetchAsync(symbols);
    return quotes.Select(q => q.Bid).ToArray();
    """,
    new { symbols, pricing });
```

`await` cooperates with `CancellationToken` and execution constraints. Long-running expressions surface `OperationCanceledException` or `AlderExecutionLimitException` at expression-level checkpoints. Iterators, `await foreach`, and `IAsyncEnumerable<T>` are first-class inside the same evaluation tree.

See [Async execution](docs/concepts/async-execution.md).

## Dynamic LINQ

Dynamic LINQ adapts runtime fragments into LINQ pipelines across three execution surfaces.

```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options => options.UseCompiler());

var page = orders
    .WhereDynamic(engine, """Status == "Open" && Total >= @0""", 250m)
    .OrderByDynamic<Order, decimal>(engine, "Total")
    .SelectDynamic<Order, OrderSummary>(
        engine,
        "new { Id, CustomerName = Customer.Name, Total }")
    .TakeDynamic(25)
    .ToList();
```

`IEnumerable<T>` runs in process through compiled delegates. `IQueryable<T>` exports expression trees and calls the matching `Queryable` operators; provider translation belongs to the provider. `IAsyncEnumerable<T>` streams through compiled delegates during asynchronous enumeration.

Filtering, ordering, projection, flattening, grouping, joins, group joins, paging, set operations, element operators, quantifiers, and aggregates are covered. `DynamicQueryPlan` captures a parsed fragment and exposes both the expression-tree view and the compiled delegate view for reuse across operators, provider-backed query assembly, and validation.

The full operator matrix is in [Use Dynamic LINQ](docs/guides/use-dynamic-linq.md).

## LINQ expression-tree export

Alder produces `Expression<TDelegate>` trees that LINQ providers translate.

```csharp
using System.Linq.Expressions;

Expression<Func<Order, bool>> predicate =
    engine.ParseAsExpression<Func<Order, bool>>(
        """order => order.Total >= 500m && order.Status == "Open" """);
```

EF Core can translate filtering, ordering, projection, grouping, flattening, joins, group joins, paging, null-coalescing predicates, string methods, and `EF.Property<T>(...)` for the verified shapes Alder emits. The export surface is narrower than runtime evaluation: statement-bodied lambdas, assignments, dynamic call shapes, collection expressions, and reflection-leaking members are rejected before provider translation begins.

Details in [Compiled backend](docs/concepts/compiled-backend.md).

## Sandbox

`SandboxOptions` controls authority. `Trusted()`, `Safe()`, and `Strict()` presets cover most policies; allow and deny lists cover concrete CLR types and namespaces. Reflection metadata is blocked at evaluation boundaries so expressions can compare types and read names without escaping into reflective discovery or invocation.

```csharp
options.Sandbox = SandboxOptions.Safe() with
{
    AllowConstruction = true,
    TrustedTypes = [typeof(StringBuilder)],
};
```

The default deny surface is broad: reflection, file and process access, networking, interop, security-sensitive runtime services, and data access are denied by default. The boundary is in-process. Alder constrains expression behavior inside the host runtime; it does not provide process or operating-system isolation.

See [Security model](docs/operations/security-model.md).

## Execution limits

`ExecutionConstraints` bounds work. Limits apply across the interpreter, the compiled backend, and generated dispatch.

```csharp
options.Constraints = new ExecutionConstraints
{
    MaxStatements     = 10_000,
    MaxLoopIterations = 1_000,
    MaxTimeout        = TimeSpan.FromSeconds(2),
};
```

Exceeded limits surface as `AlderExecutionLimitException` carrying the limit type, configured value, observed value, executed statement count, and elapsed time. `SandboxOptions.MaxCollectionSize` bounds collection-producing results separately.

## NativeAOT

Alder runs under NativeAOT through interpreted evaluation backed by generated dispatch metadata. A source generator produces reflection-free dispatch code from `[AlderRegistered]` declarations on a partial `AlderTypeContext`.

```csharp
using Alder.Aot;

[AlderRegistered(typeof(Order))]
[AlderRegistered(typeof(Customer))]
public partial class RulesAotContext : AlderTypeContext;
```

```csharp
var engine = new AlderEngine(options =>
{
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

JIT deployments adopt generated coverage incrementally because reflection fallback remains available. NativeAOT deployments use generated dispatch as the authoritative route for reflection-sensitive operations.

See [Deploy with NativeAOT](docs/guides/nativeaot-deployment.md) and [AOT and generated dispatch](docs/operations/aot-and-generated-dispatch.md).

## Reuse and performance

Parse once. Bind once. Compile once. Reuse across the lifetime of the engine.

`AlderExpression` preserves parsed syntax across evaluations and engines. The engine caches bound and compiled state across calls against the same context type surface. `Compile<TDelegate>(...)` produces a typed synchronous delegate for hot paths. `DynamicQueryPlan` reuses parsed query fragments across operators, expression-tree export, and delegate execution.

```csharp
var expression = engine.Parse("price * (1 - discount)");

var first  = engine.Evaluate<double>(expression, new { price = 100.0, discount = 0.10 });
var second = engine.Evaluate<double>(expression, new { price = 250.0, discount = 0.10 });

var isVisible = engine.Compile<Func<decimal, decimal, bool>>(
    "total >= minimum", "total", "minimum");
```

Cache invalidation is conservative. A value-only change keeps prior work. A declared-type change rebinds because overload resolution, conversion legality, and the resolved-versus-dynamic boundary may shift.

Benchmarks for the parser, binder, interpreter, compiled backend, and Dynamic LINQ live under [`benchmarks/Alder.Benchmarks`](benchmarks/Alder.Benchmarks). Run them locally to compare against your workload:

```bash
dotnet run -c Release --project benchmarks/Alder.Benchmarks
```

See [Execution and reuse](docs/operations/execution-and-reuse.md).

## Host integration

Hosts assemble Alder's expression-facing world through `AlderOptions`. Variables come from typed values, anonymous objects, dictionaries, positional `@0` placeholders, or runtime-type-preserving inputs. Host APIs reach expressions through global functions, named modules, attributed registration (`[AlderModule]`, `[AlderFunction]`), registered assemblies, imported namespaces, and extension-method containers. Modules resolve through `IServiceProvider` so module-backed expressions obtain instance targets from the host container. Child engines inherit configuration with isolated local variable state.

```csharp
var engine = new AlderEngine(options =>
{
    options.Modules.Register<PricingModule>("pricing");
    options.Functions.Register("hash", args => Sha256((string)args[0]!));
    options.Types.AddNamespace("Acme.Domain");
    options.Types.AddExtensionMethods<MoneyExtensions>();
});
```

See [Configuration](docs/reference/configuration.md), [Register types and extension methods](docs/guides/type-registration.md), [Expose functions and modules](docs/guides/functions-and-modules.md), and [Choose variables and child engines](docs/guides/variables-context-and-child-engines.md).

## Diagnostics and tracing

Parse, bind, validation, compilation, export, and runtime failures surface as `AlderException` with structured `AlderDiagnostic` values: codes (Roslyn `CS####` where applicable, `ALDR####` otherwise), human-readable messages, and source spans.

```csharp
if (!engine.TryValidate(source, out var diagnostics))
{
    foreach (var d in diagnostics)
        log.Warn("{Code} at {Span}: {Message}", d.Code, d.Span, d.Message);
}
```

`EvaluateWithTrace(...)` returns a tree showing each evaluated node, its inputs, its output, and the execution path it took.

See [Diagnostics and debugging](docs/operations/diagnostics-and-debugging.md).

## Documentation

The full set lives in [`docs/`](docs/README.md): architecture, binding system, async execution, compiled backend, Dynamic LINQ, security model, AOT and generated dispatch, configuration, execution model, the language support matrix, and integration guides.

## Build from source

```bash
dotnet restore
dotnet build
dotnet test
```

Repository layout, test-suite organization, and the AOT-matrix harness are in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
