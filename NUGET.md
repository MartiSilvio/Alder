# Alder: C# Expression Runtime

[![.NET CI](https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MartiSilvio/Alder/actions/workflows/dotnet.yml)
![.NET 8+](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet&logoColor=white)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
![NativeAOT](https://img.shields.io/badge/NativeAOT-generated%20dispatch-brightgreen)
![No third-party dependencies](https://img.shields.io/badge/dependencies-none-brightgreen)
[![MIT License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/MartiSilvio/Alder/blob/master/LICENSE)

**Parse, bind, validate, and execute C# expressions and statement blocks against CLR types.**

Alder evaluates C# expressions and statement blocks at runtime against your host's CLR types. Lambdas, query syntax, pattern matching, async, and iterators bind with ECMA-334 semantics. The interpreter runs the bound tree directly. It is the default path, and the path used under Native AOT. An opt-in compiled backend lowers the same tree to a `System.Linq.Expressions` delegate for hot synchronous workloads. Both backends share the same parser, binder, security policy, and execution limits. Both produce identical results.

## Highlights

- **C# expressions and statements at runtime.** Lambdas, queries, pattern matching, async, iterators, user-defined operators and conversions, evaluated with ECMA-334 7th edition semantics.
- **Native AOT through generated dispatch.** A source generator emits reflection-free dispatch from `[AlderRegistered]` declarations. The interpreter runs under AOT without trim warnings.
- **Async inside expressions.** `EvaluateAsync` awaits inside the bound tree. `IAsyncEnumerable<T>`, `await foreach`, and iterators are first-class through the interpreter.
- **One grammar, three surfaces.** Expression evaluation, Dynamic LINQ (`WhereDynamic`, `OrderByDynamic`), and `Expression<TDelegate>` export for EF Core all parse through the same binder, validate against the same security policy, and answer to the same execution limits.

Targets `net8.0` and `netstandard2.0`. Zero third-party runtime dependencies.

## Install

```bash
dotnet add package Alder
```

## A first look

`AlderEval` is the static entry point. Calls run against a default engine and need no setup:

```csharp
using Alder;

AlderEval.Evaluate<int>("1 + 2");                                   // 3
AlderEval.Evaluate<decimal>("price * 1.2m", new { price = 100m });  // 120m
```

`AlderEngine` exposes the same evaluation surface as an instance you own and configure. The choice between the two is about lifecycle and configuration, not capability:

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

A configured `AlderEngine` carries compiler, security policy, and generated AOT dispatch into every call it serves:

```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options =>
{
    options.UseCompiler();
    options.Security = SecurityOptions.Safe();
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});

var accepted = engine.Evaluate<bool>(rule, new { order, minimum = 500m });

var quote = await engine.EvaluateAsync<decimal>(
    "await pricing.QuoteAsync(order)",
    new { order, pricing });

var report = await db.Orders
    .WhereDynamic(engine, """Status == "Open" && Total >= @0""", 250m)
    .OrderByDynamic<Order, decimal>(engine, "Total")
    .SelectDynamic<Order, OrderSummary>(engine, "new { Id, Total }")
    .ToListAsync();
```

## Documentation

Full documentation, architecture notes, the language support matrix, security model, and Dynamic LINQ operator coverage live in the [GitHub repository](https://github.com/MartiSilvio/Alder).

## License

[MIT](https://github.com/MartiSilvio/Alder/blob/master/LICENSE)
