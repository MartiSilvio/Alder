---
title: Dynamic LINQ
description: How Alder composes runtime-defined LINQ queries across in-process sequences, query providers, async streams, and reusable expression plans.
---

# Dynamic LINQ

Alder's Dynamic LINQ system turns runtime text into typed components for LINQ pipelines. A filter expression becomes a `Where` predicate, a projection becomes a `Select` selector, an ordering expression becomes a key, and relationship fragments become joins, group joins, aggregate selectors, or reusable plans.

Use [Use Dynamic LINQ](/guides/use-dynamic-linq/) for workflow examples and the supported operator surface.

## Runtime query composition

Dynamic LINQ is Alder's runtime query-composition layer. It fits applications where the host owns the source and surrounding LINQ pipeline while predicates, selectors, keys, joins, projections, or aggregate selectors come from stored filters, configurable views, report definitions, policy-controlled search screens, or user-authored query fragments.

Each fragment passes through Alder's expression pipeline: parsing, binding, diagnostics, sandbox validation, type resolution, and conversion rules. After binding, the LINQ layer adapts the result into delegates for `IEnumerable<T>` and async streams, expression trees for `IQueryable<T>`, or reusable `DynamicQueryPlan` instances. Dynamic LINQ is the query adapter over Alder's core semantics.

The surrounding execution remains LINQ: `Enumerable`, `Queryable`, or an in-process async-stream pipeline.

## Query fragment model

Dynamic LINQ accepts body-only expressions and full lambda syntax. Body-only expressions use the current element as `it` and also support implicit member access. Full lambdas carry their own parameter names, which is useful when fragments are stored or shared across host contexts.

Runtime values are passed separately from expression text through positional placeholders such as `@0` or through named values. They participate in binding as values with real runtime types, but they do not become string-concatenated source code.

## Typed binding

Every Dynamic LINQ fragment is bound against a CLR type surface. Predicates, selectors, joins, and projections use the same member lookup, overload resolution, conversions, nullable behavior, object construction, indexers, extension methods, and sandbox policy as ordinary Alder expressions.

Typed overloads make the result contract explicit. Non-generic overloads preserve runtime flexibility when a view, report, or stored query determines the shape. Both routes share the same parser and binder; they differ in the result contract the host asks Alder to produce.

## Execution surfaces

Dynamic LINQ has one front end and three execution surfaces.

`IEnumerable<T>` executes in process. Alder compiles predicates and selectors to delegates and calls `Enumerable` operators. This is the broadest path for materialized data, imported datasets, in-memory search results, and application-owned collections.

`IQueryable<T>` exports expression trees and calls `Queryable` operators. The provider receives an ordinary LINQ tree and decides whether it can translate or execute that shape.

`IAsyncEnumerable<T>` supports a selected in-process async-stream surface. Filtering, projection, flattening, `Skip`, `Take`, `SkipWhile`, and `TakeWhile` stream through compiled delegates during asynchronous enumeration. Buffering and terminal operators, including `Distinct`, `Reverse`, quantifiers, counts, aggregates, and element operators, materialize the stream before applying LINQ behavior. Provider translation, remote planning, and database pushdown remain `IQueryable<T>` concerns.

## Operator breadth

Dynamic LINQ covers the operator families used by runtime filter builders, configurable views, reports, and policy-controlled search screens: filtering, ordering, projection, flattening, grouping, joins, group joins, paging, set operations, element operations, quantifiers, and aggregates.

Coverage differs by execution surface. Some operators exist for `IEnumerable<T>` and `IQueryable<T>` but not async streams. Some exported `IQueryable<T>` shapes are valid Alder output but still provider-limited. The practical support boundary is three-dimensional: source type, execution surface, and provider behavior all affect the final query shape.

## Reusable plans

Prepared plans are the reusable form of Dynamic LINQ fragments. `ParsePredicate`, `ParseSelector`, and `ParseLambda` return a `DynamicQueryPlan` with the inferred result type, exported expression-tree view, and compiled delegate view.

Plans let a host parse a stored filter once and reuse the same fragment across in-process execution, provider-backed query assembly, validation, or custom query composition. Reusing a plan avoids repeated Alder parsing and binding. Provider-side query compilation, parameterization, caching, and execution strategy remain owned by the provider.

## Provider export

Provider interop has two contracts.

- Alder-valid means a fragment can be parsed, bound, and exported as an expression tree.
- Provider-valid means a specific `IQueryable` provider can translate or execute that tree.

A successful export does not guarantee provider translation. EF Core can translate many verified shapes, including filtering, ordering, projection, grouping, flattening, joins, group joins, paging, null-coalescing predicates, string methods, and `EF.Property<T>(...)`. Other providers may accept a different subset of the same Alder-exported trees.

Export also has a narrower node surface than Alder runtime evaluation. Statement-bodied lambdas, assignments, variable declarations, dynamic call shapes, collection expressions, spread, slices, ranges, multidimensional indexing, and reflection-leaking members are rejected before provider translation begins.

## Schema-shaped data

Dynamic LINQ supports schema-shaped row data where the CLR row type is stable but the selected columns vary. `DataRow` indexer expressions work over `IEnumerable<DataRow>` and `IQueryable<DataRow>`.

The indexer route keeps access explicit. `DataRowExtensions.Field<T>(...)` is available only when the host deliberately trusts the required `System.Data` surface, registers the assembly for type resolution, and registers the extension-method container.

## Engine policy

String-based Dynamic LINQ extension operators such as `WhereDynamic("...")`, `SelectDynamic("...")`, and `OrderByDynamic("...")` require `UseCompiler()` on a JIT-capable runtime. In-process sequence operators compile predicates and selectors to delegates. `IQueryable<T>` operators export expression trees and call the matching `Queryable` operators; provider translation remains downstream.

Expression-tree export is a separate surface. `ParseAsExpression<TDelegate>(...)`, `ParsePredicate(...)`, `ParseSelector(...)`, and `ParseLambda(...)` can prepare expression trees without calling `UseCompiler()`. Compiling those trees to delegates still requires dynamic code support.

Use the global engine when one query policy applies across the process. Pass an explicit `AlderEngine` when tenant boundaries, sandbox settings, type visibility, extension methods, or query validation policy belong to a specific application boundary. NativeAOT and IL2CPP-style deployments should keep runtime expression evaluation on Alder's interpreter and generated dispatch path outside the Dynamic LINQ operator surface.

## Dynamic LINQ and `Evaluate(...)`

`Evaluate(...)` executes an expression as the unit of work. Dynamic LINQ prepares expression fragments for a larger LINQ pipeline.

Use `Evaluate(...)` when the expression produces the final result. Use Dynamic LINQ when the expression is a predicate, selector, key, projection, join component, aggregate selector, or paging component inside a sequence query.

## Related pages

- [Use Dynamic LINQ](/guides/use-dynamic-linq/)
- [Compiled backend](/concepts/compiled-backend/)
- [Configuration](/reference/configuration/)
