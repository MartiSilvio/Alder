---
title: Dynamic LINQ
description: How Alder composes runtime-defined LINQ queries across in-process sequences, query providers, async streams, and reusable expression plans.
---

# Dynamic LINQ

Alder's Dynamic LINQ system builds typed LINQ pipelines from expressions supplied at runtime. It parses C#-shaped predicates, selectors, keys, and projections; binds them against CLR model types; exports delegates or expression trees; and composes those fragments into ordinary LINQ operators over `IEnumerable<T>`, `IQueryable<T>`, and selected `IAsyncEnumerable<T>` flows.

This is a runtime query composition layer, backed by Alder's compiler and engine configuration model. It is designed for applications where query shape is data: grids, dashboards, reporting tools, stored filters, policy-controlled search surfaces, configurable exports, and provider-backed systems that need late-bound query logic without leaving the LINQ execution model.

## The shape of the system

Dynamic LINQ accepts either full lambda syntax or body-only expressions. For a single input element, Alder exposes that element as `it` and also enables implicit member access:

```csharp
var openOrders = orders.WhereDynamic("""Status == "Open" && Total >= @0""", 100m);
var newestFirst = openOrders.OrderByDescendingDynamic<OrderRow, DateTime>("CreatedAt");
var page = newestFirst
    .SelectDynamic<OrderRow, OrderSummary>("new { Id, Customer, Total, CreatedAt }")
    .SkipDynamic(50)
    .TakeDynamic(25)
    .ToList();
```

The expression is bound against `OrderRow`, so `Status`, `Total`, `CreatedAt`, and the projection members resolve as CLR members before execution. Inline variables such as `@0` and named values flow through the same binding phase, giving the generated predicate or selector a real type surface while keeping runtime values separate from expression text.

The operator surface covers the shapes a runtime query system needs: filtering, ordering, projection, flattening, grouping, joins, group joins, paging, set operations, element operations, predicates such as `Any` and `All`, and aggregates including `Count`, `LongCount`, `Sum`, `Average`, `Min`, and `Max`. The full inventory belongs in the operator reference; the architectural point is that Dynamic LINQ composes complete query pipelines.

The public surface has three layers:

- string-based dynamic operators such as `WhereDynamic("Status == @0", status)`
- typed overloads such as `SelectDynamic<OrderRow, OrderSummary>("new { Id, Total }")`
- prepared plans from `ParsePredicate`, `ParseSelector`, and `ParseLambda`

Those layers share the same binding machinery. The choice is about integration shape: call an operator directly, ask Alder to materialize a typed result, or prepare a reusable expression fragment for host-side query assembly.

## Typed binding against CLR models

Alder binds Dynamic LINQ expressions against real .NET types. That binding model lets the same expression language reach properties, fields, methods, indexers, nullable operations, conversions, object creation, and structural projections with the same semantics Alder uses elsewhere.

```csharp
var summaries = products.SelectDynamic<Product, ProductSummary>(
    "new { Name, Price, Category }");

var electronics = products.WhereDynamic<Product>(
    """Category == "Electronics" && Price > minPrice""",
    new { minPrice = 50m });
```

Structural projections can materialize to object-shaped results or typed DTOs when the requested result type matches the projection shape. Joins bind multiple typed parameters, infer one key type from both key selectors, and reject mismatched keys before invoking LINQ.

## Generic and non-generic APIs

Most Dynamic LINQ operators have a generic form and a non-generic form. The generic form tells Alder the result shape the host wants. The non-generic form preserves runtime flexibility and usually returns an untyped sequence or scalar for the host to cast, inspect, or pass onward.

```csharp
var typedNames = products
    .SelectDynamic<Product, string>("Name")
    .ToList();

var runtimeNames = products
    .SelectDynamic("Name")
    .Cast<string>()
    .ToList();
```

Typed overloads are the right default when the host knows the result type: DTO projections, numeric keys, aggregate result types, or a specific selector return type. Non-generic overloads fit configurable views, report builders, and stored query definitions where the result shape is discovered at runtime.

The expression binding is the same in both cases. The difference is the public result contract the host asks Alder to produce.

## Filtering

Filtering turns a runtime predicate into a typed predicate for the source element.

```csharp
var openHighValue = orders.WhereDynamic(
    """Status == @0 && Total >= @1""",
    "Open",
    500m);
```

Body-only expressions are compact when the source element is obvious. Full lambda syntax fits stored expressions that already include parameter names:

```csharp
var assignedTickets = tickets.WhereDynamic(
    """ticket => ticket.AssigneeId == userId && !ticket.IsClosed""",
    new { userId });
```

Both forms bind against the same CLR model type. The body-only form resolves `Status` or `Total` through the implicit receiver; the lambda form resolves members through the explicit parameter.

## Ordering

Ordering operators turn runtime key selectors into typed ordering keys, with primary, secondary, and descending variants.

```csharp
var queue = tickets
    .WhereDynamic("!IsClosed")
    .OrderByDescendingDynamic<TicketRow, int>("Priority")
    .ThenByDynamic<TicketRow, DateTime>("CreatedAt");
```

For non-generic queries, the key type is inferred from the selector and the ordered sequence stays composable:

```csharp
var ordered = products
    .OrderByDynamic("Category")
    .ThenByDescendingDynamic("Price");
```

Those selectors follow the source surface: delegates for `IEnumerable<T>`, expression-tree keys for `IQueryable<T>`.

## Projection

Projection turns runtime selectors into scalar values, DTOs, or structural objects with named members.

```csharp
var reportRows = sales.SelectDynamic<SaleRow, SalesReportRow>(
    "new { Region, Rep, Revenue, ClosedAt }");
```

Structural objects are useful when the selected columns are part of report configuration:

```csharp
var selectedColumns = users.SelectDynamic(
    "new { Id, DisplayName = Name, Email, LastLoginUtc }");
```

Projection also supports flattening through `SelectManyDynamic`:

```csharp
var lineItems = orders.SelectManyDynamic<OrderRow, OrderLineRow>(
    "Lines");
```

The selector is bound against the source element. For flattening, Alder infers the collection element type and composes the corresponding LINQ operator.

## Grouping

Grouping turns runtime keys into `IGrouping<TKey, TElement>` sequences. That makes report-style partitioning possible without leaving LINQ.

```csharp
var byRegion = sales
    .GroupByDynamic<SaleRow, string>("Region")
    .Select(group => new
    {
        Region = group.Key,
        Count = group.Count(),
        Revenue = group.Sum(row => row.Revenue)
    });
```

Grouping is often paired with host-side LINQ after the dynamic key has been selected. Alder owns the runtime key binding; ordinary LINQ remains responsible for the surrounding summary shape. On `IQueryable<T>`, the grouping key is exported as an expression tree for the provider.

## Aggregates

Dynamic aggregates bind a selector and delegate the aggregate operation to LINQ. That preserves .NET's numeric behavior instead of inventing a separate arithmetic model.

```csharp
var openRevenue = orders
    .WhereDynamic("""Status == "Open" """)
    .SumDynamic<OrderRow, decimal>("Total");

var hasEscalations = tickets.AnyDynamic(
    """Priority >= @0 && Status != "Closed" """,
    4);
```

The typed aggregate overload is useful when the host expects a specific numeric result. The non-generic aggregate overload returns the runtime aggregate value as an object, preserving the selector's inferred result type.

## Joins

Dynamic joins bind three expression fragments: the outer key, the inner key, and the result selector. The key selectors are checked together so the join has one key type.

```csharp
var rows = orders.JoinDynamic<OrderRow, CustomerRow, int, OrderCustomerRow>(
    customers,
    "order => order.CustomerId",
    "customer => customer.Id",
    """(outer, inner) => new { outer.Id, Customer = inner.Name, outer.Total }""");
```

Group joins expose the grouped inner sequence to the result selector:

```csharp
var customerSummaries = customers.GroupJoinDynamic<CustomerRow, OrderRow, int, CustomerOrderSummary>(
    orders,
    "customer => customer.Id",
    "order => order.CustomerId",
    """(outer, group) => new { outer.Name, OrderCount = group.Count() }""");
```

On `IQueryable<T>`, the same structure is exported through `Queryable.Join` or `Queryable.GroupJoin`.

## Paging and element access

Paging operators compose with dynamic filtering and ordering. The usual pattern is to filter first, impose a stable order, then page:

```csharp
var page = tickets
    .WhereDynamic("""Status == @0""", "Open")
    .OrderByDescendingDynamic<TicketRow, DateTime>("UpdatedAt")
    .SkipDynamic(pageIndex * pageSize)
    .TakeDynamic(pageSize)
    .ToList();
```

Element operators fit lookup and preview workflows:

```csharp
var firstMatch = products
    .OrderByDynamic<Product, string>("Name")
    .FirstOrDefaultDynamic("""Category == "Hardware" """);
```

`SkipDynamic`, `TakeDynamic`, `ElementAtDynamic`, `ElementAtOrDefaultDynamic`, `FirstDynamic`, `LastDynamic`, `SingleDynamic`, and their default-returning variants follow LINQ's execution and exception behavior for the underlying source.

## Set operations

Set operations keep runtime-composed sequences inside LINQ after filtering or projection selects each side.

```csharp
var internalUsers = users.WhereDynamic("""Email.EndsWith("@example.com")""");
var activeUsers = users.WhereDynamic("IsActive");

var activeInternalUsers = internalUsers.IntersectDynamic(activeUsers);
```

`ConcatDynamic`, `UnionDynamic`, `IntersectDynamic`, `ExceptDynamic`, `DistinctDynamic`, `ReverseDynamic`, `CastDynamic`, `OfTypeDynamic`, `AppendDynamic`, `PrependDynamic`, `ContainsDynamic`, and `SequenceEqualDynamic` follow the source's LINQ surface. Provider-backed set operations remain subject to provider translation.

## Execution surfaces

Dynamic LINQ has one expression front end and distinct execution boundaries for each LINQ surface.

`IEnumerable<T>` executes in process. Alder prepares the expression, compiles it to a delegate, and calls the corresponding `Enumerable` operator. This is the broadest path for materialized data, imported datasets, in-memory search results, and application-owned collections.

`IQueryable<T>` exports expression trees and calls the matching `Queryable` operator. The provider receives the composed query and decides how much of it can translate to its backend.

`IAsyncEnumerable<T>` supports a selected in-process async-stream surface. Alder compiles predicates and selectors, then applies them during asynchronous enumeration. Aggregate and some element operations materialize the stream internally before delegating to LINQ semantics.

## IQueryable and provider boundaries

On `IQueryable<T>`, Alder emits expression trees that preserve the typed shape of the dynamic fragment:

```csharp
var query = db.Orders
    .WhereDynamic("""Customer.StartsWith(@0) && Total >= @1""", "A", 50m)
    .OrderByDynamic<EfOrder, decimal>("Total")
    .SelectDynamic<EfOrder, OrderSummary>("new { Customer, Total }");
```

The resulting query stays in provider space until the host materializes it. EF Core can translate verified shapes such as filtering, ordering, projection, grouping, flattening, joins, group joins, paging, `CastDynamic`, `DistinctDynamic`, `ReverseDynamic`, null-coalescing predicates, and provider-visible calls such as `EF.Property<T>(...)`.

Provider translation remains a real boundary. A valid Alder expression tree can still be rejected by a specific provider. In the EF Core SQLite verification path, provider limits include shapes such as `OfTypeDynamic`, `SequenceEqualDynamic`, `SkipWhileDynamic`, `TakeWhileDynamic`, `AppendDynamic`, `PrependDynamic`, and `DefaultIfEmptyDynamic(value)` with a custom default. Those outcomes come from the provider translation contract.

The practical rule is direct: Alder owns parsing, binding, expression-tree construction, and LINQ composition. The provider owns final translation. When translation fails, revise the query shape for the provider or move that portion to an in-process `IEnumerable<T>` boundary intentionally.

## Prepared plans and reusable lambdas

Dynamic LINQ also exposes the prepared query fragment itself. `ParsePredicate`, `ParseSelector`, and `ParseLambda` return a `DynamicQueryPlan` with the inferred result type and expression export APIs:

```csharp
var plan = engine.ParsePredicate<OrderRow>(
    """Status == "Open" && Total >= 100m""");

Expression<Func<OrderRow, bool>> expression = plan.ToExpression<Func<OrderRow, bool>>();
Func<OrderRow, bool> predicate = plan.Compile<Func<OrderRow, bool>>();
```

This gives host code a reusable boundary. A product can parse a stored filter once, cache the expression or delegate under its own invalidation policy, and feed the same fragment into custom query assembly. The plan API is also the bridge for hosts that want Alder's binding and expression export as a direct integration surface.

Selectors and custom lambdas use the same plan model:

```csharp
var selector = engine.ParseSelector<ProductRow, ProductExportRow>(
    "new { Id, Name, Category, Price }")
    .ToExpression<Func<ProductRow, ProductExportRow>>();

var plan = engine.ParseLambda(
    [typeof(ProductRow), typeof(decimal)],
    ["product", "threshold"],
    typeof(bool),
    "product.Price >= threshold");

var predicate = plan.Compile<Func<ProductRow, decimal, bool>>();
```

Expression export is the provider-facing shape. Delegate compilation is the in-process shape. Prepared plans let the host decide when to bind, cache, and apply the lambda.

## Global engine and custom engine

Dynamic LINQ belongs to the compiled backend. The extension methods use the configured `AlderEval` engine by default and require that engine to have a compiler:

```csharp
using Alder.Compiled;

AlderEval.Configure(o => o.UseCompiler());

var rows = products.WhereDynamic("Price > @0", 50m).ToList();
```

Applications with one shared query policy can configure `AlderEval` once and use the global extension-method path. Applications with tenant boundaries, restricted sandboxes, custom type visibility, or subsystem-specific extension methods should pass an explicit `AlderEngine`:

```csharp
using var engine = new AlderEngine(o => o.UseCompiler());

var visible = orders.WhereDynamic(engine, "CanShip && Total <= maxTotal", maxTotal);
```

The engine determines the language mode, sandbox policy, visible types, registered extensions, and compiler availability. Dynamic LINQ inherits that policy from the configured engine.

## Async streams

The async surface exists for runtime-defined operations over streams already represented as `IAsyncEnumerable<T>`. It is an in-process stream pipeline over compiled delegates:

```csharp
await foreach (var item in source
    .WhereDynamic("Price > @0", 50m)
    .SelectDynamic<Product, string>("Name")
    .TakeDynamic(10))
{
    Console.WriteLine(item);
}
```

This path executes as the stream is consumed. Provider translation, remote query planning, and database pushdown belong to `IQueryable<T>`. Aggregates such as `SumDynamic`, `AverageDynamic`, `MinDynamic`, and `MaxDynamic` return `ValueTask` results and preserve LINQ aggregate behavior over the selected value type.

Async operators also accept prepared expression trees and delegates where the generated surface exposes them:

```csharp
var predicate = engine.ParsePredicate<ProductRow>("Price > 50m")
    .ToExpression<Func<ProductRow, bool>>();

await foreach (var product in stream.WhereDynamic(predicate))
{
    Console.WriteLine(product.Name);
}
```

The async surface is narrower than the synchronous surfaces. It covers filtering, projection, flattening, paging, distinct, reverse, element predicates, counts, long counts, common element operators, and scalar aggregates. Joins, group joins, provider translation, and remote execution remain outside the async-stream contract.

## Dynamic LINQ and `Evaluate(...)`

`Evaluate(...)` executes an expression as the unit of work:

```csharp
var total = engine.Evaluate<decimal>("return subtotal + tax;");
```

Dynamic LINQ prepares expression fragments for a sequence pipeline:

```csharp
var filtered = orders.WhereDynamic("Total > 100m");
```

Use `Evaluate(...)` when the expression produces the final result. Use Dynamic LINQ when the expression is a predicate, selector, key, projection, join component, aggregate selector, or paging component inside a larger LINQ query.

## Related pages

- [Use Dynamic LINQ](/how-to/use-dynamic-linq/)
- [Dynamic LINQ operator status](/reference/language/operator-status/)
- [Compiled backend](/explanation/compiled-backend/)
- [Configuration](/reference/configuration/)
