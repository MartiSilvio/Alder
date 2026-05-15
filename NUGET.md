# Alder: C# Expression Engine for .NET

[![.NET CI](https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Alder?logo=nuget&logoColor=white)](https://www.nuget.org/packages/Alder)
![.NET 8+](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet&logoColor=white)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
![NativeAOT](https://img.shields.io/badge/NativeAOT-generated%20dispatch-brightgreen)
![No third-party dependencies](https://img.shields.io/badge/dependencies-none-brightgreen)
[![MIT License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/MartiSilvio/Alder/blob/master/LICENSE)

**An embeddable C# expression evaluator with compiler-style binding for CLR types.**

Interpreter-first execution with optional compiled delegates, Dynamic LINQ, expression-tree export, host-controlled security, and NativeAOT generated dispatch.

C# semantics · Native AOT · Async · Dynamic LINQ · Zero dependencies

Alder evaluates C# expressions and statement blocks at runtime against CLR objects supplied by the host. Before execution, the parser and binder build a semantic model. That model decides type resolution, overload resolution, conversions, and control flow. The same pipeline applies security policy and execution limits.

The interpreter is the baseline execution path. JIT-capable hosts can opt into compiled delegates. Query providers can receive `Expression<TDelegate>` trees. NativeAOT hosts can route registered member access through generated dispatch metadata.

Standard mode follows ECMA-334 7th edition semantics. It covers lambdas and query syntax, pattern matching, async code, iterators, and user-defined conversions and operators. The interpreter and compiled backend share parser and binder. They also share validation, security, and limits. They produce identical results; divergence is a defect.

## At a glance

- **C# expressions and statements at runtime.** Standard mode follows ECMA-334 7th edition for runtime expressions and statement blocks. It includes lambdas and queries, pattern matching, async code, iterators, and user-defined conversions and operators. [Support matrix](https://github.com/MartiSilvio/Alder/blob/master/docs/reference/language/standard-mode-language-support.md).
- **Native AOT through generated dispatch.** A source generator emits reflection-free dispatch from `[AlderRegistered]` declarations. The interpreter runs under AOT without trim warnings.
- **Async inside expressions.** `EvaluateAsync` awaits inside the bound tree. `IAsyncEnumerable<T>`, `await foreach`, and iterators are first-class through the interpreter.
- **Shared semantics across surfaces.** Expression evaluation, Dynamic LINQ (`WhereDynamic`, `OrderByDynamic`), and `Expression<TDelegate>` export go through one parser and binder. They use the same security policy and execution limits.

Targets `net8.0` and `netstandard2.0`. Zero third-party runtime dependencies.

## A first look

`AlderEval` is the static entry point. Calls run against a default engine and need no setup:

```csharp
using Alder;

AlderEval.Evaluate<int>("1 + 2");                                   // 3
AlderEval.Evaluate<decimal>("price * 1.2m", new { price = 100m });  // 120m
```

`AlderEngine` gives you the same evaluation surface with owned lifecycle and configuration:

```csharp
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

## End-to-end integration

A configured `AlderEngine` carries compiler settings, security policy, and generated AOT dispatch into every call it serves:

```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options =>
{
    options.UseCompiler();
    options.Security = SecurityOptions.Trusted();
    options.Modules.Register<PricingModule>("pricing");
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

Use `TryValidate` to surface parser and binder diagnostics before execution:

```csharp
if (!engine.TryValidate(rule, out var diagnostics))
    return diagnostics;
```

Synchronous evaluation dispatches through the compiled backend against host-shaped types:

```csharp
var accepted = engine.Evaluate<bool>(rule, new { order, minimum = 500m });
```

Awaitable expression bodies cooperate with cancellation and constraints:

```csharp
var quote = await engine.EvaluateAsync<decimal>(
    "await pricing.QuoteAsync(order)",
    new { order });
```

Runtime fragments export as `Expression` trees so EF Core can translate them to SQL:

```csharp
var report = await db.Orders
    .WhereDynamic(engine, """Status == "Open" && Total >= @0""", 250m)
    .OrderByDynamic<Order, decimal>(engine, "Total")
    .SelectDynamic<Order, OrderSummary>(engine, "new { Id, Total }")
    .ToListAsync();
```

## Install

```bash
dotnet add package Alder
```

The `Alder` package is the single public package. It includes the runtime, optional `Alder.Compiled` APIs for JIT-capable consumers, and the source generator for AOT generated dispatch metadata.

## Language surface

Standard mode evaluates C# at the expression and statement-block level under ECMA-334 7th edition semantics. Type and member declarations, namespaces, attributes, preprocessor directives, and unsafe code are out of scope. The full support matrix lives in [Standard mode language support](https://github.com/MartiSilvio/Alder/blob/master/docs/reference/language/standard-mode-language-support.md).

[Extended mode](https://github.com/MartiSilvio/Alder/blob/master/docs/concepts/extended-language-mode.md) adds scripting syntax on the same parser. The additional surface includes pipelines and regex predicates, SQL-style comparisons, ranges, date arithmetic, and aggregate helpers. A valid C# expression produces the same result in either mode.

## The expression engine

Alder's binder is the semantic boundary between syntax and execution. It resolves type relationships, overloads, member targets, assignment legality, and control-flow shape. It also records where runtime dispatch is still required.

Execution paths consume that bound model while preserving the same security policy and execution limits.

The **interpreter** evaluates the bound tree directly. It is the default synchronous path, the engine for `EvaluateAsync(...)`, and the path used under NativeAOT and trimming-sensitive deployments.

The **compiled backend** lowers the same bound tree to a reusable delegate through `System.Linq.Expressions`. With `UseCompiler()` configured, synchronous `Evaluate(...)` uses that delegate path and recompiles when the relevant type surface changes.

Both backends share parser and binder. They also share validation rules, security policy, execution limits, and language semantics. They produce identical results. Divergence is a defect.

Architecture: [Architecture](https://github.com/MartiSilvio/Alder/blob/master/docs/concepts/architecture.md), [Binding system](https://github.com/MartiSilvio/Alder/blob/master/docs/concepts/binding-system.md), [Execution model](https://github.com/MartiSilvio/Alder/blob/master/docs/reference/execution-model.md).

## Async expressions

`EvaluateAsync(...)` runs through the interpreter and awaits expression-level asynchronous work directly inside the bound tree.

```csharp
using var engine = new AlderEngine(options =>
{
    options.Modules.Register<PricingModule>("pricing");
});

var prices = await engine.EvaluateAsync<decimal[]>(
    """
    var quotes = await pricing.FetchAsync(symbols);
    return quotes.Select(q => q.Bid).ToArray();
    """,
    new { symbols });
```

`await` cooperates with `CancellationToken` and execution constraints. Long-running expressions surface `OperationCanceledException` or `AlderExecutionLimitException` at expression-level checkpoints.

Iterators, `await foreach`, and `IAsyncEnumerable<T>` are first-class inside the same evaluation tree.

See [Async execution](https://github.com/MartiSilvio/Alder/blob/master/docs/concepts/async-execution.md).

## Dynamic LINQ

Dynamic LINQ adapts runtime fragments into LINQ pipelines for in-memory collections, query providers, and async streams.

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

`IEnumerable<T>` executes in process through compiled delegates. `IQueryable<T>` exports expression trees and calls the matching `Queryable` operators. Provider translation belongs to the provider. `IAsyncEnumerable<T>` streams in process through compiled delegates during asynchronous enumeration.

Operator coverage spans filters and ordering; projection and flattening; grouping, joins, and group joins; paging and set operations; element operators, quantifiers, and aggregates.

`DynamicQueryPlan` captures a parsed fragment for reuse across operators, provider-backed query assembly, validation, delegate execution, and expression-tree export.

The full operator matrix is in [Use Dynamic LINQ](https://github.com/MartiSilvio/Alder/blob/master/docs/guides/use-dynamic-linq.md).

## LINQ expression-tree export

Alder produces `Expression<TDelegate>` trees that LINQ providers translate.

```csharp
using System.Linq.Expressions;

Expression<Func<Order, bool>> predicate =
    engine.ParseAsExpression<Func<Order, bool>>(
        """order => order.Total >= 500m && order.Status == "Open" """);
```

EF Core can translate the verified shapes Alder emits for filters and ordering; projections and grouping; flattening, joins, and group joins; paging; null-coalescing predicates; string methods; and `EF.Property<T>(...)`.

The export surface is narrower than runtime evaluation. Alder rejects statement-bodied lambdas, assignments, dynamic call shapes, collection expressions, and reflection-leaking members before provider translation begins.

Details in [Compiled backend](https://github.com/MartiSilvio/Alder/blob/master/docs/concepts/compiled-backend.md).

## Security policy

`SecurityOptions` controls expression authority. Alder defaults to trusted execution for ease of adoption. `Trusted()` enables every gated operation for trusted expressions.

Hosts that evaluate user-authored or tenant-authored expressions should choose an explicit `new SecurityOptions { ... }` policy and name each allowed operation directly.

Allow and deny lists cover concrete CLR types and namespaces. Reflection metadata is blocked at evaluation boundaries so expressions can compare types and read names without escaping into reflective discovery or invocation.

```csharp
options.Security = new SecurityOptions
{
    AllowPropertyRead = true,
    AllowStaticPropertyRead = true,
    AllowStaticFieldRead = true,
    AllowConstruction = true,
    TrustedTypes = [typeof(StringBuilder)],
};
```

The default deny surface covers reflection and dynamic code generation; file and process access; networking and interop; security-sensitive runtime services; and data access. The boundary is in-process. Alder constrains expression behavior inside the host runtime; it does not provide process or operating-system isolation.

See [Security model](https://github.com/MartiSilvio/Alder/blob/master/docs/operations/security-model.md).

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

When a limit is exceeded, Alder throws `AlderExecutionLimitException`. The exception reports the limit type and configured value, the observed value, executed statement count, and elapsed time. `SecurityOptions.MaxCollectionSize` bounds collection-producing results separately.

## NativeAOT

Alder supports NativeAOT through interpreted evaluation backed by generated dispatch metadata. The source generator reads `[AlderRegistered]` declarations on a partial `AlderTypeContext` and emits reflection-free dispatch code.

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

See [Deploy with NativeAOT](https://github.com/MartiSilvio/Alder/blob/master/docs/guides/nativeaot-deployment.md) and [AOT and generated dispatch](https://github.com/MartiSilvio/Alder/blob/master/docs/operations/aot-and-generated-dispatch.md).

## Reuse and performance

Parse once. Bind once. Compile once. Reuse across the lifetime of the engine.

`AlderExpression` preserves parsed syntax across evaluations and engines. The engine caches bound and compiled state for calls against the same context type surface.

`Compile<TDelegate>(...)` produces a typed synchronous delegate for hot paths. `DynamicQueryPlan` reuses parsed query fragments across operators, expression-tree export, and delegate execution.

```csharp
var expression = engine.Parse("price * (1 - discount)");

var first  = engine.Evaluate<double>(expression, new { price = 100.0, discount = 0.10 });
var second = engine.Evaluate<double>(expression, new { price = 250.0, discount = 0.10 });

var isVisible = engine.Compile<Func<decimal, decimal, bool>>(
    "total >= minimum", "total", "minimum");
```

Cache invalidation is conservative. Value-only changes keep prior work. Declared-type changes rebind because overload resolution, conversion legality, and the resolved-versus-dynamic boundary can shift.

See [Execution and reuse](https://github.com/MartiSilvio/Alder/blob/master/docs/operations/execution-and-reuse.md).

## Host integration

Hosts shape Alder's expression-facing world through `AlderOptions`. Variables can come from typed values, anonymous objects, dictionaries, positional `@0` placeholders, or inputs that preserve runtime type.

Host APIs reach expressions through global functions and named modules, attributed registration such as `[AlderModule]` and `[AlderFunction]`, registered assemblies or namespaces, and extension-method containers.

Modules resolve through `IServiceProvider`, so module-backed expressions obtain instance targets from the host container. Child engines inherit configuration with isolated local variable state.

```csharp
var engine = new AlderEngine(options =>
{
    options.Modules.Register<PricingModule>("pricing");
    options.Functions.Register("hash", args => Sha256((string)args[0]!));
    options.Types.AddNamespace("Acme.Domain");
    options.Types.AddExtensionMethods<MoneyExtensions>();
});
```

See [Configuration](https://github.com/MartiSilvio/Alder/blob/master/docs/reference/configuration.md), [Register types and extension methods](https://github.com/MartiSilvio/Alder/blob/master/docs/guides/type-registration.md), [Expose functions and modules](https://github.com/MartiSilvio/Alder/blob/master/docs/guides/functions-and-modules.md), and [Choose variables and child engines](https://github.com/MartiSilvio/Alder/blob/master/docs/guides/variables-context-and-child-engines.md).

## Diagnostics and tracing

Parsing and binding failures report as `AlderException`. Validation, compilation, export, and runtime failures use the same exception type. Diagnostics carry codes (Roslyn `CS####` where applicable, `ALDR####` otherwise), human-readable messages, and source spans.

```csharp
if (!engine.TryValidate(source, out var diagnostics))
{
    foreach (var d in diagnostics)
        log.Warn("{Code} at {Span}: {Message}", d.Code, d.Span, d.Message);
}
```

`EvaluateWithTrace(...)` returns a tree showing each evaluated node, its inputs, its output, and the execution path it took.

See [Diagnostics and debugging](https://github.com/MartiSilvio/Alder/blob/master/docs/operations/diagnostics-and-debugging.md).

## Documentation

Full documentation lives in the [GitHub repository](https://github.com/MartiSilvio/Alder/tree/master/docs), organized as concepts, guides, reference, and operations.

## License

[MIT](https://github.com/MartiSilvio/Alder/blob/master/LICENSE)
