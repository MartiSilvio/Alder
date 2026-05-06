---
title: Use Dynamic LINQ
description: Compose runtime-defined LINQ queries over IEnumerable, IQueryable, and async streams with Alder Dynamic LINQ.
---

# Use Dynamic LINQ

Dynamic LINQ is for product features where query behavior is data: saved filters, configurable grids, report columns, search screens, policy rules, sort definitions, aggregate selectors, and provider-backed queries. Alder binds each runtime fragment against the CLR source type and composes it into ordinary LINQ over `IEnumerable<T>`, `IQueryable<T>`, or `IAsyncEnumerable<T>`.

Those fragments use Alder's normal expression pipeline: parsing, binding, diagnostics, security policy validation, type resolution, and conversions. The LINQ layer adapts the bound result into predicates, selectors, ordering keys, grouping keys, join components, projections, aggregate selectors, expression trees, delegates, and reusable query plans.

The host owns the source, the surrounding LINQ pipeline, the engine policy, and the execution boundary. Alder owns parsing, binding, validation, expression export, and delegate generation for the dynamic fragments.

## Supported query surface

Dynamic LINQ is organized around the LINQ families runtime query builders need. `Provider-bound` means Alder can build the `IQueryable<T>` expression shape, while translation still depends on the provider.

| Query family | Dynamic fragment | `IEnumerable<T>` | `IQueryable<T>` | `IAsyncEnumerable<T>` | Boundary |
| --- | --- | --- | --- | --- | --- |
| Filtering: `Where` | Predicate | Yes | Yes | Yes | Binds against the source element type. |
| Projection: `Select` | Selector or materializer | Yes | Yes | Yes | Supports scalar, DTO, structural, and runtime-shaped results. |
| Ordering: `OrderBy`, `ThenBy` variants | Key selector | Yes | Yes | No | Async streams preserve enumeration order. |
| Paging and sequence control: `Skip`, `Take`, `SkipWhile`, `TakeWhile`, `Reverse` | Count or predicate | Yes | Provider-bound | Yes | Provider translation can reject some sequence-control shapes. |
| Flattening: `SelectMany` | Collection selector | Yes | Yes | Yes | Result-selector overloads are synchronous/provider only. |
| Grouping: `GroupBy` | Key selector | Yes | Yes | No | Shape grouped summaries with normal LINQ after key selection. |
| Relationships: `Join`, `GroupJoin` | Outer key, inner key, result selector | Yes | Yes | No | Key selectors are checked together before LINQ invocation. |
| Quantifiers: `Any`, `All` | Predicate | Yes | Yes | Yes | Predicate semantics follow LINQ quantifier behavior. |
| Element and value access: `First`, `Single`, `ElementAt`, `Contains` variants | Predicate, index, or value | Yes | Yes | Predicate-based | Async streams expose `First`, `Last`, and `Single` variants, but not `Contains` or `ElementAt`. |
| Aggregates: `Count`, `LongCount`, `Sum`, `Average`, `Min`, `Max` | Predicate or selector | Yes | Yes | Yes | `Aggregate` has no direct dynamic operator. |
| Set operations: `Distinct`, `Concat`, `Union`, `Intersect`, `Except` | Sequence value or none | Yes | Yes | `Distinct` only | Async streams expose `Distinct`; binary set operations are synchronous/provider only. |
| Keyed distinct: `DistinctBy` | Key selector | Yes | No | No | In-memory keyed distinct over `IEnumerable<T>`. |
| Type and default operators: `Cast`, `OfType`, `DefaultIfEmpty`, `Append`, `Prepend`, `SequenceEqual` | Type, value, or sequence | Yes | Provider-bound | No | Translation support varies by provider and query shape. |
| Prepared fragments: `ParsePredicate`, `ParseSelector`, `ParseLambda`, `DynamicQueryPlan` | Bound plan | Yes | Yes | Yes | Plans feed operators, expression export, and compiled delegate execution. |

Compose paging with `SkipDynamic(...)` and `TakeDynamic(...)`, then shape totals or page metadata in host code.

## Query setup and binding

String-based Dynamic LINQ extension operators require `UseCompiler()` on a JIT-capable runtime. The API is available from Alder through the `Alder.Compiled` namespace. In-process sequence operators compile predicates and selectors to delegates. `IQueryable<T>` operators export expression trees and call the matching `Queryable` operators; provider translation remains downstream.

<!-- test: FilteringOrderingPagingAndProjection_ComposeInOneRuntimeQuery -->
```csharp
using Alder;
using Alder.Compiled;

using var engine = new AlderEngine(options => options.UseCompiler());
```

The extension methods can use the global `AlderEval` engine:

<!-- test: GlobalAlderEvalConfiguration_EnablesStringBasedQueryExtensions -->
```csharp
AlderEval.Configure(options => options.UseCompiler());

var expensive = products.WhereDynamic("Price >= @0", 100m).ToList();
```

Pass an explicit `AlderEngine` when query policy belongs to a specific boundary: tenant scope, security settings, registered extension methods, visible types, or validation rules.

The prepared-plan and direct export APIs are separate from string-based operator execution. `ParsePredicate(...)`, `ParseSelector(...)`, `ParseLambda(...)`, and `ParseAsExpression<TDelegate>(...)` can prepare expression trees without calling `UseCompiler()`. Compiling a plan or expression tree to a delegate still requires dynamic code support.

String-based Dynamic LINQ operator execution depends on `UseCompiler()`. NativeAOT and IL2CPP-style deployments should keep runtime expression evaluation on Alder's interpreter and generated dispatch path, then compose query behavior through host-owned code outside the Dynamic LINQ operator surface.

### Model binding

Dynamic LINQ binds against CLR types. The source type is the expression-facing surface for member lookup, overload resolution, conversions, nullable behavior, object construction, indexers, extension methods, and security policy validation.

Alder applies its normal preparation pipeline before LINQ composition: parsing, binding, diagnostics, security policy validation, type resolution, and conversions. The LINQ layer then adapts the bound result into query operators. Execution constraints apply where Alder executes in-process work; provider execution remains under the provider's runtime.

<!-- test: FilteringOrderingPagingAndProjection_ComposeInOneRuntimeQuery -->
```csharp
public sealed record Product(
    string Name,
    decimal Price,
    string Category,
    bool InStock);

public sealed record ProductSummaryDto
{
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
}

var products = new List<Product>
{
    new("Widget", 9.99m, "Tools", true),
    new("Gadget", 49.99m, "Electronics", true),
    new("Doohickey", 149.99m, "Electronics", false),
    new("Thingamajig", 4.99m, "Tools", true),
    new("Whatchamacallit", 299.99m, "Specialty", true)
};
```

### Expression forms

Body-only fragments use the current element as `it` and also allow implicit member access:

<!-- test: ExpressionForms_SupportImplicitAndExplicitLambdaSelectors -->
```csharp
var names = products
    .SelectDynamic<Product, string>(engine, "Name")
    .ToList();
```

Full lambda syntax is useful when stored expressions carry their own parameter names:

<!-- test: ExpressionForms_SupportImplicitAndExplicitLambdaSelectors -->
```csharp
var names = products
    .SelectDynamic<Product, string>(engine, "product => product.Name")
    .ToList();
```

Both forms bind to the same `Product` type surface. Choose body-only syntax for compact host-controlled fragments. Choose lambda syntax when expression text should be portable across contexts.

### Runtime values

Pass runtime values separately from expression text. Positional values use `@0`, `@1`, and later placeholders:

<!-- test: RuntimeValues_SupportPositionalNamedAndMixedBinding -->
```csharp
var visible = products
    .WhereDynamic(
        engine,
        """Category == @0 && Price >= @1""",
        "Electronics",
        50m)
    .ToList();
```

Named values make stored rules easier to read:

<!-- test: RuntimeValues_SupportPositionalNamedAndMixedBinding -->
```csharp
var visible = products
    .WhereDynamic(
        engine,
        "product => product.Price <= maxPrice && product.Category == category",
        new { maxPrice = 100m, category = "Electronics" })
    .ToList();
```

You can mix both forms when the host naturally has both positional values and named context:

<!-- test: RuntimeValues_SupportPositionalNamedAndMixedBinding -->
```csharp
var filtered = products
    .WhereDynamic(
        engine,
        "product => product.Price > @0 && product.Category == category",
        10m,
        new { category = "Electronics" })
    .ToList();
```

Runtime values participate in binding with their actual runtime types. They are not concatenated into source text.

## Filtering

Filtering is the most common Dynamic LINQ entry point. `WhereDynamic(...)` turns a runtime predicate into a normal LINQ predicate or provider expression.

<!-- test: FilteringExamples_UseImplicitAndExplicitRuntimeValues -->
```csharp
var inStockElectronics = products
    .WhereDynamic(engine, """Category == "Electronics" && InStock""")
    .ToList();
```

Predicates support the same expression-level language surface Alder exposes for bound fragments: member access, comparisons, logical operators, method calls allowed by policy, nullable operations, conversions, indexers, and registered extension methods.

<!-- test: FilteringExamples_UseImplicitAndExplicitRuntimeValues -->
```csharp
var searchResults = products
    .WhereDynamic(
        engine,
        "product => product.Name.StartsWith(prefix) && product.Price <= maxPrice",
        new { prefix = "W", maxPrice = 300m })
    .ToList();
```

On `IEnumerable<T>`, Alder compiles the predicate to a delegate and calls `Enumerable.Where`. On `IQueryable<T>`, Alder exports an expression tree and calls `Queryable.Where`; the provider then decides whether the tree can translate.

## Ordering and paging

Ordering operators bind runtime key selectors. They are available for `IEnumerable<T>` and `IQueryable<T>`.

<!-- test: FilteringOrderingPagingAndProjection_ComposeInOneRuntimeQuery -->
```csharp
var ordered = products
    .OrderByDynamic<Product, string>(engine, "Category")
    .ThenByDescendingDynamic<Product, decimal>(engine, "Price")
    .ToList();
```

A typical result-set pipeline filters first, imposes a stable order, and then pages:

<!-- test: FilteringOrderingPagingAndProjection_ComposeInOneRuntimeQuery -->
```csharp
var page = products
    .WhereDynamic(engine, "InStock")
    .OrderByDynamic<Product, string>(engine, "Category")
    .ThenByDynamic<Product, string>(engine, "Name")
    .SkipDynamic(20)
    .TakeDynamic(20)
    .ToList();
```

`SkipDynamic(...)`, `TakeDynamic(...)`, `SkipWhileDynamic(...)`, `TakeWhileDynamic(...)`, and `ReverseDynamic(...)` compose with `IEnumerable<T>`, `IQueryable<T>`, and the supported async-stream surface. Provider translation still belongs to the provider; some exported paging shapes are provider-limited even when Alder can produce the expression tree.

## Projections

Projection is the center of Dynamic LINQ for grids, reports, exports, and configurable views. It controls what leaves the query pipeline. Alder supports scalar selectors, typed DTO materialization, structural rows, aliases, and runtime-shaped projections.

### Scalar selectors

Use typed scalar selectors when the result type is known:

<!-- test: ProjectionSupportsScalarDtoStructuralAndRuntimeShapedRows -->
```csharp
var prices = products
    .SelectDynamic<Product, decimal>(engine, "Price")
    .ToList();
```

The generic result type is part of the contract. If the selector cannot produce that type, binding or materialization fails; there is no loosely shaped fallback on a typed projection.

### DTO projection

Use DTO projection when the host owns the result contract and the selected members should materialize into that contract:

<!-- test: ProjectionSupportsScalarDtoStructuralAndRuntimeShapedRows -->
```csharp
var summaries = products
    .SelectDynamic<Product, ProductSummaryDto>(
        engine,
        "new { Name, Price }")
    .ToList();
```

DTO projection fits application-owned result models, export rows, API response shapes, and strongly typed report rows. The projection members must match what the target type can accept.

### Structural rows

Use structural projection when the selected columns are configured at runtime:

<!-- test: ProjectionSupportsScalarDtoStructuralAndRuntimeShapedRows -->
```csharp
var rows = products
    .SelectDynamic<Product, IReadOnlyDictionary<string, object?>>(
        engine,
        "new { ProductName = Name, Category, Price }")
    .ToList();

var firstName = rows[0]["ProductName"];
```

The non-generic projection route keeps the shape runtime-defined:

<!-- test: ProjectionSupportsScalarDtoStructuralAndRuntimeShapedRows -->
```csharp
var configuredRows = products
    .SelectDynamic(engine, "new { Name, Category, Price }")
    .Cast<IReadOnlyDictionary<string, object?>>()
    .ToList();
```

Structural rows are useful when the column list is data. Typed DTO projection is better when the result contract is stable.

### Provider projection

On `IQueryable<T>`, projection is exported as an expression tree. Simple member, scalar, DTO, and structural projection shapes can be valid Alder output, but a provider may reject a tree it cannot translate. Keep provider-facing projections expression-shaped: avoid statement bodies, assignments, dynamic call shapes, collection expressions, and reflection-oriented members.

## Flattening and grouping

Grouping and flattening cover query shapes where each source row expands, partitions, or contributes to a grouped result.

### Flatten nested collections

`SelectManyDynamic(...)` flattens nested collections selected at runtime:

<!-- test: GroupingFlatteningJoinsAndGroupJoins_UseDynamicKeysAndSelectors -->
```csharp
public sealed record Customer(string Name, List<Order> Orders);
public sealed record Order(string Product, int Quantity, decimal UnitPrice);

var orderedProducts = customers
    .SelectManyDynamic<Customer, Order>(engine, "Orders")
    .Select(order => order.Product)
    .ToList();
```

Use a result selector when the output needs both the outer and inner values:

<!-- test: SelectManyResultSelectors_ProjectOuterAndInnerRows -->
```csharp
var orderLabels = customers
    .SelectManyDynamic<Customer, Order, string>(
        engine,
        "customer => customer.Orders",
        """(outer, inner) => outer.Name + ":" + inner.Product""")
    .ToList();
```

`SelectManyDynamic(...)` is available for `IEnumerable<T>`, `IQueryable<T>`, and `IAsyncEnumerable<T>`. The overload with a result selector is available for `IEnumerable<T>` and `IQueryable<T>`.

### Group rows

`GroupByDynamic(...)` selects grouping keys at runtime and returns ordinary `IGrouping<TKey, T>` results:

<!-- test: GroupingFlatteningJoinsAndGroupJoins_UseDynamicKeysAndSelectors -->
```csharp
var byCategory = products
    .GroupByDynamic<Product, string>(engine, "Category")
    .Select(group => new
    {
        Category = group.Key,
        Count = group.Count(),
        Total = group.Sum(product => product.Price)
    })
    .ToList();
```

Grouping is available for `IEnumerable<T>` and `IQueryable<T>`. The dynamic portion is the key selector. The host can continue with normal LINQ to shape summaries after grouping.

## Joins

Dynamic joins bind multiple typed fragments as one operation: the outer key, the inner key, and the result selector. Alder checks the key selectors together so the join has one key type.

<!-- test: GroupingFlatteningJoinsAndGroupJoins_UseDynamicKeysAndSelectors -->
```csharp
public sealed record WarehouseStock(string Category, int Count);

var stock = new List<WarehouseStock>
{
    new("Tools", 12),
    new("Electronics", 5),
    new("Specialty", 1)
};

var joined = products
    .JoinDynamic<Product, WarehouseStock, string, string>(
        stock,
        engine,
        "product => product.Category",
        "stock => stock.Category",
        """(outer, inner) => outer.Name + ":" + inner.Count""")
    .ToList();
```

Use `GroupJoinDynamic(...)` when each outer row needs the grouped inner sequence:

<!-- test: GroupingFlatteningJoinsAndGroupJoins_UseDynamicKeysAndSelectors -->
```csharp
var grouped = products
    .GroupJoinDynamic<Product, WarehouseStock, string, string>(
        stock,
        engine,
        "product => product.Category",
        "stock => stock.Category",
        """(outer, group) => outer.Name + ":" + group.Count()""")
    .ToList();
```

`JoinDynamic(...)` and `GroupJoinDynamic(...)` are available for `IEnumerable<T>` and `IQueryable<T>`. They are not async-stream operators. Provider-backed joins also depend on provider translation.

## Aggregates and terminal operations

Terminal operators turn a sequence into a scalar, a boolean, or a selected element. Dynamic LINQ keeps the dynamic part in the predicate or selector and delegates terminal behavior to LINQ.

### Aggregates

Alder supports dynamic `Count`, `LongCount`, `Sum`, `Average`, `Min`, and `Max`:

<!-- test: AggregatesElementAndSequenceOperators_FollowLinqBehavior -->
```csharp
var stockedCount = products.CountDynamic(engine, "InStock");

var totalElectronicsRevenue = products
    .WhereDynamic(engine, """Category == "Electronics" """)
    .SumDynamic<Product, decimal>(engine, "Price");

var averagePrice = products.AverageDynamic(engine, "product => (double)product.Price");
```

Typed aggregate overloads make the expected result type explicit. Non-generic aggregate overloads preserve the selector's inferred runtime result type. `Aggregate` does not have a direct dynamic operator.

### Quantifiers

`AnyDynamic(...)` and `AllDynamic(...)` bind runtime predicates:

<!-- test: AggregatesElementAndSequenceOperators_FollowLinqBehavior -->
```csharp
var hasSpecialty = products.AnyDynamic(engine, """Category == "Specialty" """);
var allPriced = products.AllDynamic(engine, "Price > 0m");
```

### Element access

Element operators follow LINQ's exception and default-value behavior:

<!-- test: ElementSetAndTypeOperators_FollowLinqBehavior -->
```csharp
var firstSpecialty = products.FirstDynamic(engine, """Category == "Specialty" """);
var maybeMissing = products.FirstOrDefaultDynamic(engine, """Category == "Office" """);
var onlySpecialty = products.SingleDynamic(engine, """Category == "Specialty" """);
var thirdName = products.Select(product => product.Name).ElementAtDynamic(2);
```

`FirstDynamic(...)`, `LastDynamic(...)`, `SingleDynamic(...)`, and `ElementAtDynamic(...)` throw in the same cases as LINQ. The `OrDefault` variants return the default value when LINQ would.

## Set, sequence, and value operations

Some Dynamic LINQ operators do not take expression strings. They exist to keep runtime-composed pipelines inside the same extension-method surface.

### Set and distinct operations

<!-- test: ElementSetAndTypeOperators_FollowLinqBehavior -->
```csharp
var categories = products
    .Select(product => product.Category)
    .DistinctDynamic()
    .ToList();

var distinctProductsByCategory = products
    .DistinctByDynamic<Product, string>(engine, "Category")
    .ToList();
```

Set operations compose two sequences:

<!-- test: ElementSetAndTypeOperators_FollowLinqBehavior -->
```csharp
var visibleCategories = new[] { "Tools", "Electronics" };
var allowedCategories = new[] { "Electronics", "Specialty" };

var shared = visibleCategories
    .IntersectDynamic(allowedCategories)
    .ToList();
```

`ConcatDynamic`, `UnionDynamic`, `IntersectDynamic`, and `ExceptDynamic` are available for `IEnumerable<T>` and `IQueryable<T>`. `DistinctDynamic` is available for `IEnumerable<T>`, `IQueryable<T>`, and `IAsyncEnumerable<T>`. `DistinctByDynamic(...)` is `IEnumerable<T>` only.

### Type and value operators

`CastDynamic<TResult>()` and `OfTypeDynamic<TResult>()` work over non-generic or weakly typed sequences:

<!-- test: ElementSetAndTypeOperators_FollowLinqBehavior -->
```csharp
IEnumerable values = new object?[] { 1, "two", null, 3, "four" };

var numbers = values.OfTypeDynamic<int>().ToList();
```

`DefaultIfEmptyDynamic`, `AppendDynamic`, `PrependDynamic`, `ContainsDynamic`, and `SequenceEqualDynamic` follow LINQ's source and result behavior. Some `IQueryable<T>` shapes are provider-limited; for example, tested EF Core SQLite translation rejects several sequence-control shapes even though Alder can build the query tree.

## Reusable plans

Use `DynamicQueryPlan` when a stored filter, selector, or lambda will be applied repeatedly. A plan preserves the bound fragment, inferred result type, expression-tree view, and compiled delegate view.

<!-- test: ReusablePlansExposeExpressionAndCompiledDelegateViews -->
```csharp
var filter = engine.ParsePredicate<Product>("Price > 50m");
var price = engine.ParseSelector<Product, decimal>("Price");

var inMemory = products
    .WhereDynamic(filter)
    .OrderByDescendingDynamic<Product, decimal>(price)
    .ToList();

var total = products.SumDynamic(price);
```

The same plan can feed a provider-backed query:

<!-- test: ReusablePlansExposeExpressionAndCompiledDelegateViews -->
```csharp
var query = db.Products
    .WhereDynamic(filter)
    .SelectDynamic<Product, decimal>(price);
```

Plans avoid repeated Alder parsing and binding. They do not replace provider query compilation, SQL parameterization, caching, or execution strategy; those remain provider responsibilities.

Plans also expose both integration views directly:

<!-- test: ReusablePlansExposeExpressionAndCompiledDelegateViews -->
```csharp
using System.Linq.Expressions;

Expression<Func<Product, bool>> expression =
    filter.ToExpression<Func<Product, bool>>();

Func<Product, bool> localPredicate =
    filter.Compile<Func<Product, bool>>();
```

Use the expression view when another component needs a LINQ tree. Use the delegate view for local in-process execution.

## Provider-backed queries

`IQueryable<T>` operators export expression trees and call the matching `Queryable` operator. The provider receives ordinary LINQ nodes, not Alder source text.

<!-- test: ProviderExport_ProducesExpressionTrees_ButProviderTranslationIsSeparate -->
```csharp
var query = db.Products
    .WhereDynamic(engine, "product => product.Price >= @0", 50m)
    .OrderByDynamic<Product, decimal>(engine, "Price")
    .SelectDynamic<Product, ProductSummaryDto>(
        engine,
        "new { Name, Price }");
```

Provider integration has two gates:

- Alder-valid: the fragment parses, binds, and exports as an expression tree.
- Provider-valid: the specific provider can translate or execute that tree.

EF Core can translate many verified shapes, including filtering, ordering, projection, grouping, flattening, joins, group joins, paging, null-coalescing predicates, string methods, and `EF.Property<T>(...)`.

<!-- test: ProviderExport_ProducesExpressionTrees_ButProviderTranslationIsSeparate -->
```csharp
var query = db.Products
    .WhereDynamic(
        engine,
        """product => EF.Property<string>(product, "Category") == @0""",
        "Electronics")
    .SelectDynamic<Product, string>(engine, "product => product.Name");
```

Export has a narrower node surface than runtime evaluation. It is for expression-shaped fragments such as member access, calls, operators, conditionals, casts, indexers, and pure constructor calls used by projection shapes. Statement-bodied lambdas, assignments, variable declarations, object initializers, dynamic call shapes, collection expressions, spread, slices, ranges, multidimensional indexing, and reflection-leaking members are rejected before provider translation begins.

Provider-limited shapes include cases such as `OfTypeDynamic`, `SequenceEqualDynamic`, `SkipWhileDynamic`, `TakeWhileDynamic`, `AppendDynamic`, `PrependDynamic`, and `DefaultIfEmptyDynamic(value)` with a custom default in the tested EF Core SQLite path. When provider translation fails, revise the exported query shape or intentionally move that portion to an in-process `IEnumerable<T>` boundary.

You can export an expression directly when the host needs the tree outside the Dynamic LINQ operator surface:

<!-- test: ProviderExport_ProducesExpressionTrees_ButProviderTranslationIsSeparate -->
```csharp
Expression<Func<Product, bool>> directExpression =
    engine.ParseAsExpression<Func<Product, bool>>(
        "product => product.Price >= 50m && product.InStock");
```

## DataRow and DataTable

Dynamic LINQ supports schema-shaped row data where the CLR row type is stable but selected columns vary. Bind against `DataRow` and use explicit indexer access.

<!-- test: DataRowIndexerQueries_WorkForSchemaShapedData -->
```csharp
using System.Data;

var rows = table.AsEnumerable()
    .WhereDynamic<DataRow>(engine, """(string)it["City"] == @0""", "Seattle")
    .OrderByDynamic<DataRow, int>(engine, """(int)it["Size"]""")
    .SelectDynamic<DataRow, IReadOnlyDictionary<string, object?>>(
        engine,
        """new { City = (string)it["City"], Size = (int)it["Size"] }""")
    .ToList();
```

`DataRow` indexer expressions work over `IEnumerable<DataRow>` and `IQueryable<DataRow>`. The indexer route keeps column access explicit.

`DataRowExtensions.Field<T>(...)` is blocked by the default security policy because `System.Data` is denied by default. To use it, trust the required `System.Data` namespace, register the `System.Data` assembly for type resolution, and register `DataRowExtensions` as an extension-method container.

## Async streams

`IAsyncEnumerable<T>` Dynamic LINQ operators execute in process. Filtering, projection, flattening, `Skip`, `Take`, `SkipWhile`, and `TakeWhile` stream as the source is consumed. `Distinct`, `Reverse`, quantifiers, counts, aggregates, and `First`/`Last`/`Single` variants materialize the stream before applying LINQ behavior. Provider translation and database pushdown belong to `IQueryable<T>`.

<!-- test: AsyncStreams_SupportFilteringProjectionPagingAndAggregates -->
```csharp
await foreach (var name in source
    .WhereDynamic(engine, "product => product.InStock && product.Price >= @0", 50m)
    .SelectDynamic<Product, string>(engine, "product => product.Name")
    .TakeDynamic(10))
{
    Console.WriteLine(name);
}
```

Async streams support filtering, projection, flattening, paging, `Distinct`, `Reverse`, quantifiers, `First`/`Last`/`Single` variants, counts, and supported aggregates. They do not cover every synchronous or provider operator.

<!-- test: AsyncStreams_SupportFilteringProjectionPagingAndAggregates -->
```csharp
var count = await source.CountDynamic(engine, "product => product.InStock");
var total = await source.SumDynamic(engine, "product => product.Price");
var first = await source.FirstOrDefaultDynamic(engine, "product => product.Price > 100m");
```

Joins, group joins, ordering, grouping, `Concat`, `Union`, `Intersect`, `Except`, `Cast`, `OfType`, `Contains`, `ElementAt`, and `SequenceEqual` are outside the async-stream Dynamic LINQ surface.

## Troubleshooting

- `InvalidOperationException` mentioning `UseCompiler`: configure the engine with `o.UseCompiler()` or configure `AlderEval` with `AlderEval.Configure(o => o.UseCompiler())`.
- `PlatformNotSupportedException` from `UseCompiler()`: the runtime does not support dynamic code. String-based Dynamic LINQ operators depend on `UseCompiler()`; use interpreter and generated dispatch paths for AOT expression evaluation.
- Missing extension methods: add `using Alder.Compiled;`.
- Unknown member or identifier: check whether the fragment is body-only or a full lambda, then check the source element type.
- Positional value not found: make sure `@0`, `@1`, and later placeholders line up with the values passed after the expression.
- Wrong typed projection result: make `TResult` match the selector result, project to a DTO with matching members, or use a structural row.
- Provider rejection on `IQueryable<T>`: separate Alder export success from provider translation success; revise the query shape or materialize intentionally before applying an in-process operator.
- Async operator missing: async streams support filtering, projection, flattening, `Skip`, `Take`, `SkipWhile`, `TakeWhile`, `Reverse`, quantifiers, `First`/`Last`/`Single` variants, aggregates, and `Distinct`; ordering, grouping, joins, binary set operations, type filters, `Contains`, `ElementAt`, and `SequenceEqual` stay on synchronous or provider-backed surfaces.
- `DataRowExtensions.Field<T>(...)` blocked by the security policy: trust only the required `System.Data` surface and register the extension-method container explicitly.

## Related pages

- [Dynamic LINQ](../concepts/dynamic-linq.md)
- [Compiled backend](../concepts/compiled-backend.md)
- [Configuration](../reference/configuration.md)
