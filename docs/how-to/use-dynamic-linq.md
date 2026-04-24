---
title: Use Dynamic LINQ
description: Query IEnumerable and IQueryable sources with Alder Dynamic LINQ string expressions.
---

# Use Dynamic LINQ

Use Alder Dynamic LINQ when you need string-defined predicates, selectors, ordering, or aggregates over `IEnumerable<T>` or `IQueryable<T>`.

```csharp
using Alder;
using Alder.Compiled;

public sealed class Product
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Price { get; init; }
    public bool InStock { get; init; }
}

var engine = new AlderEngine(o => o.UseCompiler());

var products = new List<Product>
{
    new() { Name = "Widget", Category = "Tools", Price = 9.99m, InStock = true },
    new() { Name = "Gadget", Category = "Electronics", Price = 49.99m, InStock = true },
    new() { Name = "Doohickey", Category = "Electronics", Price = 299.99m, InStock = false },
    new() { Name = "Thingamajig", Category = "Tools", Price = 4.99m, InStock = true }
};
```

## Prerequisites

Dynamic LINQ requires the compiled backend. Configure the engine with `UseCompiler()` and import `Alder.Compiled`.

## Filter

Use `WhereDynamic(...)` with either a lambda-style predicate or a body-only predicate:

```csharp
var expensive = products
    .WhereDynamic(engine, "p => p.Price > 50m")
    .ToList();

var tools = products
    .WhereDynamic(engine, """Category == "Tools" """)
    .ToList();
```

Use `@0`, `@1`, and so on for external parameters:

```csharp
var threshold = 50m;

var result = products
    .WhereDynamic(engine, "p => p.Price > @0", threshold)
    .ToList();
```

## Order

Use `OrderByDynamic`, `OrderByDescendingDynamic`, `ThenByDynamic`, and `ThenByDescendingDynamic`:

```csharp
var byCategoryThenPrice = products
    .OrderByDynamic<Product, string>(engine, "p => p.Category")
    .ThenByDynamic<Product, decimal>(engine, "p => p.Price")
    .ToList();
```

## Project

Use `SelectDynamic<TSource, TResult>` when the result type is known:

```csharp
var names = products
    .SelectDynamic<Product, string>(engine, "Name")
    .ToList();

var pricesWithTax = products
    .SelectDynamic<Product, decimal>(engine, "p => p.Price * 1.1m")
    .ToList();
```

Structural projections are also supported:

```csharp
var projected = products
    .SelectDynamic<Product, object>(engine, "new { ProductName = Name, Price }")
    .ToList();
```

Projected members are surfaced as public properties on the returned objects:

```csharp
var first = projected[0];
var name = first.GetType().GetProperty("ProductName")!.GetValue(first);
var price = first.GetType().GetProperty("Price")!.GetValue(first);
```

## Compose

Filtering, ordering, and projection compose in the expected LINQ order:

```csharp
var visibleNames = products
    .WhereDynamic(engine, "p => p.InStock && p.Price < 100m")
    .OrderByDynamic<Product, decimal>(engine, "Price")
    .SelectDynamic<Product, string>(engine, "Name")
    .ToList();
```

## Query IQueryable

The same operators work on `IQueryable<T>`:

```csharp
var result = products
    .AsQueryable()
    .WhereDynamic(engine, "p => p.Price >= 10m")
    .OrderByDynamic<Product, decimal>(engine, "Price")
    .SelectDynamic<Product, string>(engine, "Name")
    .ToList();
```

Provider translation still applies. An expression can be valid in Alder yet still be rejected by the underlying query provider.

## Verify

```csharp
var names = products
    .WhereDynamic(engine, "p => p.InStock")
    .SelectDynamic<Product, string>(engine, "Name")
    .ToList();

if (names.Count != 3)
    throw new Exception("Dynamic LINQ filter returned the wrong number of rows.");
```

Terminal operators are available as well:

```csharp
var count = products.AsQueryable().CountDynamic(engine, "p => p.InStock");
var first = products.FirstOrDefaultDynamic(engine, """p => p.Category == "Tools" """);
```

## Troubleshooting

- `InvalidOperationException` mentioning `UseCompiler`: configure the engine with `o.UseCompiler()` before using Dynamic LINQ.
- Missing extension methods: add `using Alder.Compiled;`.
- Unknown member or identifier: validate the expression against the source element type.
- Wrong result type from `SelectDynamic<TSource, TResult>`: make sure `TResult` matches the expression result.
- `IQueryable<T>` query rejected after composition: the query provider could not translate the resulting expression tree.

## Related pages

- [Dynamic LINQ operator status](/reference/language/operator-status/)
- [Configuration](/reference/configuration/)
