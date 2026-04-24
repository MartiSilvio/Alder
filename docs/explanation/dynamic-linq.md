---
title: Dynamic LINQ
description: How Alder keeps runtime-defined queries inside ordinary LINQ pipelines across in-process sequences, query providers, and async streams.
---

# Dynamic LINQ

Alder includes a Dynamic LINQ capability for applications that cannot know the whole query up front. Filters come from UI state, sort orders from user preference, projections from configurable views, grouping keys from reports, joins from stored query definitions. The useful part is not that these pieces are strings. The useful part is that the resulting code still reads and behaves like a LINQ pipeline.

## Why it exists

Most applications do not need a second query language. They need a way to keep using the first one when parts of the query are chosen at runtime.

Dynamic LINQ serves that need. It binds C#-shaped expressions against real .NET types and turns them into the same kinds of fragments a developer would otherwise write as static lambdas: predicates, selectors, ordering keys, grouping keys, and join projections. That keeps the host application in familiar territory. A grid still filters, sorts, projects, and pages through LINQ. A report still groups and aggregates through LINQ. A provider-backed query still composes through LINQ. Alder supplies the runtime-defined pieces without forcing the application into a separate DSL.

## Query composition at runtime

The surface is broad, but the shape is familiar:

```csharp
var filtered = orders.WhereDynamic("""Status == "Open" && Total >= 100m""");
var ordered = orders.OrderByDescendingDynamic<OrderRow, DateTime>("CreatedAt");
var projected = orders.SelectDynamic<OrderRow, object>("new { Id, Customer, Total }");
var grouped = orders.GroupByDynamic<OrderRow, string>("Region");
var revenue = orders.SumDynamic("Total");
var joined = orders.JoinDynamic<OrderRow, CustomerRow, int, object>(
    customers,
    "order => order.CustomerId",
    "customer => customer.Id",
    """(outer, inner) => new { outer.Id, Customer = inner.Name, outer.Total }""");
```

These are ordinary query building blocks. Dynamic LINQ makes them available when the expression is supplied at runtime instead of written inline at compile time.

That matters most in the places where queries are inherently late-bound: admin tables, search interfaces, dashboards, export definitions, reporting views, and provider-backed applications where query behavior is stored as data or assembled from user intent. The host still builds one query. It simply does so with a runtime-defined predicate here, a runtime-defined selector there, and a runtime-defined ordering or grouping key further down the chain.

## One language, three execution paths

The central idea of the feature is not any single operator. It is the way one expression surface maps onto three different sequence types.

For `IEnumerable<T>`, Alder parses the expression, binds it against `T`, compiles a delegate, and applies that delegate in process. This is the broadest surface. It fits local collections, in-memory search results, imported datasets, and other flows where the data is already materialized and runtime composition needs to stay fast and direct.

For `IQueryable<T>`, Alder exports expression trees. The query stays in provider space, so runtime-defined filters, selectors, grouping keys, and joins can still participate in the provider's execution plan. This is the path that matters for EF Core and similar systems. Alder produces the expression tree; the provider decides whether and how that tree translates.

For `IAsyncEnumerable<T>`, Alder compiles selected predicates and selectors and applies them during asynchronous enumeration. This is an in-process execution path over async streams. It is useful when data arrives incrementally and query behavior still has to be chosen at runtime, but it does not serve the same role as `IQueryable<T>`.

These three paths share one expression model and three different execution boundaries. Once that distinction is clear, most of Dynamic LINQ's behavior becomes easy to reason about.

## Where the surface shows up

In practice, the feature is less about isolated operators than about the shape of a whole pipeline:

```csharp
var page = orders
    .WhereDynamic("""Status == status && Total >= minTotal""", new { status = "Open", minTotal = 100m })
    .OrderByDescendingDynamic<OrderRow, DateTime>("CreatedAt")
    .SelectDynamic<OrderRow, object>("new { Id, Customer, Total, CreatedAt }")
    .TakeDynamic(50)
    .ToList();
```

That one query captures the common rhythm: narrow the rows, impose a stable order, shape the result, then page or materialize it. Dynamic LINQ extends naturally from there. Projection can produce known types or structural objects with named members. Grouping can partition data on a runtime-defined key and hand the resulting groups back to ordinary LINQ for summarization. Joins and group joins can relate one sequence to another without changing the overall style of the query.

The feature also reaches into the surrounding families that make a runtime query system usable in practice: element access, common set operations, flattening, counts, sums, averages, minima, maxima, and paging operators. The full operator inventory belongs in reference material. The important point here is that the surface is wide enough to keep a real application inside one consistent query model.

## Engine boundaries

Dynamic LINQ requires the compiled backend. The extension methods validate that a compiler is configured and throw `InvalidOperationException` when it is not.

`AlderEval` is the shared-engine path:

```csharp
using Alder.Compiled;

AlderEval.Configure(o => o.UseCompiler());

var rows = products.WhereDynamic("Price > @0", 50m).ToList();
```

This is a good fit when the process has one common Dynamic LINQ environment: one language mode, one sandbox policy, one set of visible types and extensions. `AlderEval.Configure(...)` must run before the first `AlderEval.GetEngine()` call, and that configuration is single-assignment for the lifetime of the global engine.

A custom `AlderEngine` is the better fit when query behavior belongs to a specific boundary: a tenant, a subsystem, a restricted sandbox, or a custom type and extension surface. In those cases, query policy belongs with the component that owns it.

The same boundary shapes the reusable lambda APIs exposed through `Alder.Compiled`, including `ParsePredicateExpression(...)`, `ParseSelectorExpression(...)`, `ParseLambdaExpression(...)`, and `CreateDynamicLambdaFactory()`. These APIs return `LambdaExpression` values that can be cached, compiled, or integrated into host-side query composition without going through the extension methods each time.

## Limits worth knowing

The most important limit is provider translation. On `IQueryable<T>`, a valid Alder expression tree still has to pass through the provider's translator. In the verified EF Core SQLite path, filtering, ordering, structural projection, grouping, flattening, joins, group joins, paging, `CastDynamic`, `DistinctDynamic`, `ReverseDynamic`, null-coalescing predicates, and `EF.Property<T>(...)` all compose successfully. Other shapes remain provider-limited: `OfTypeDynamic`, `SequenceEqualDynamic`, `SkipWhileDynamic`, `TakeWhileDynamic`, `AppendDynamic`, `PrependDynamic`, and `DefaultIfEmptyDynamic(value)` with a custom default are rejected by that provider.

The second limit is surface breadth across sequence types. `IEnumerable<T>` is the broadest path. `IAsyncEnumerable<T>` is narrower because it only covers the async operators Alder implements over compiled in-process delegates. It supports filtering, projection, flattening, common element operators, counts, long counts, sums, averages, minima, maxima, paging, distinct, and reverse. It is useful, but it is a different execution model from `IQueryable<T>`.

There are narrower domain-specific limits as well. `DataRow` works through indexer-based expressions such as `(string)it["City"] == "Seattle"` over `IEnumerable<DataRow>` and `IQueryable<DataRow>`. The `DataRowExtensions.Field<T>(...)` path has a stricter boundary because it depends on type registration and sandbox allowance for `System.Data`.

## Dynamic LINQ and `Evaluate(...)`

`Evaluate(...)` runs an expression and returns its result. Dynamic LINQ builds query fragments against typed sequence elements and composes those fragments into a larger LINQ pipeline.

```csharp
var total = engine.Evaluate<decimal>("return subtotal + tax;");
var filtered = orders.WhereDynamic("Total > 100m");
```

Use `Evaluate(...)` when the expression itself is the unit of work. Use Dynamic LINQ when the expression has to participate in filtering, ordering, projection, grouping, joins, paging, or related query composition over a sequence.

## Related pages

- [Use Dynamic LINQ](/how-to/use-dynamic-linq/)
- [Dynamic LINQ operator status](/reference/language/operator-status/)
- [Compiled backend](/explanation/compiled-backend/)
- [Configuration](/reference/configuration/)
