---
title: Use Dynamic LINQ
description: Compose runtime-defined LINQ queries over IEnumerable and IQueryable sources with Alder Dynamic LINQ.
---

# Use Dynamic LINQ

Use Dynamic LINQ when a query pipeline is known by the host but some predicates, selectors, keys, or aggregates arrive at runtime. Alder binds those fragments against the source element type and composes them through ordinary LINQ operators.

Dynamic LINQ requires the compiled backend:

```csharp
using Alder;
using Alder.Compiled;

var engine = new AlderEngine(options => options.UseCompiler());
```

The extension methods use `AlderEval` by default. Pass an explicit `AlderEngine` when query policy, visible types, or sandbox settings belong to a specific application boundary.

## Define a source model

```csharp
public sealed class Product
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Price { get; init; }
    public bool InStock { get; init; }
}

var products = new List<Product>
{
    new() { Name = "Widget", Category = "Tools", Price = 9.99m, InStock = true },
    new() { Name = "Gadget", Category = "Electronics", Price = 49.99m, InStock = true },
    new() { Name = "Doohickey", Category = "Electronics", Price = 299.99m, InStock = false }
};
```

## Filter with runtime predicates

`WhereDynamic(...)` accepts full lambda syntax or body-only syntax. Body-only expressions use the source element as the implicit receiver.

```csharp
var electronics = products
    .WhereDynamic(engine, """Category == "Electronics" && Price >= @0""", 50m)
    .ToList();
```

Use `@0`, `@1`, and later placeholders for positional runtime values. Use named values when the expression should read like a stored rule:

```csharp
var visible = products
    .WhereDynamic(engine, "p => p.InStock && p.Price <= maxPrice", new { maxPrice = 100m })
    .ToList();
```

## Order and page

Ordering keys can be supplied dynamically and then followed by ordinary paging:

```csharp
var page = products
    .WhereDynamic(engine, "InStock")
    .OrderByDynamic<Product, string>(engine, "Category")
    .ThenByDescendingDynamic<Product, decimal>(engine, "Price")
    .SkipDynamic(0)
    .TakeDynamic(20)
    .ToList();
```

## Project known and runtime shapes

Use typed `SelectDynamic<TSource, TResult>(...)` when the result type is known:

```csharp
var names = products
    .SelectDynamic<Product, string>(engine, "Name")
    .ToList();
```

Structural projections can materialize configured result shapes:

```csharp
var rows = products
    .SelectDynamic(engine, "new { ProductName = Name, Category, Price }")
    .ToList();
```

For DTO projection, ask for the DTO result type and match the projection members to that type.

## Aggregate

Dynamic aggregate selectors keep report calculations in the query pipeline:

```csharp
var electronicsRevenue = products
    .WhereDynamic(engine, """Category == "Electronics" """)
    .SumDynamic<Product, decimal>(engine, "Price");

var stockedCount = products.CountDynamic(engine, "InStock");
```

## Compose over IQueryable

The same operators work over `IQueryable<T>`. Alder exports expression trees and the provider translates the resulting query:

```csharp
var query = db.Products
    .WhereDynamic(engine, "p => p.Price >= @0", 10m)
    .OrderByDynamic<Product, decimal>(engine, "Price")
    .SelectDynamic<Product, string>(engine, "Name");
```

Provider translation remains a separate boundary. A dynamic expression can be valid in Alder and still be rejected by EF Core or another provider if the generated expression tree uses a shape that provider cannot translate.

## Troubleshooting

- `InvalidOperationException` mentioning `UseCompiler`: configure the engine with `o.UseCompiler()` or configure `AlderEval` with `AlderEval.Configure(o => o.UseCompiler())`.
- Missing extension methods: add `using Alder.Compiled;`.
- Unknown member or identifier: check the expression against the source element type.
- Wrong typed projection result: make `TResult` match the selector result or use the non-generic overload and cast later.
- Provider rejection on `IQueryable<T>`: revise the query for the provider or materialize intentionally before applying an in-process operator.

## Related pages

- [Dynamic LINQ](/explanation/dynamic-linq/)
- [Dynamic LINQ operator status](/reference/language/operator-status/)
- [Configuration](/reference/configuration/)
