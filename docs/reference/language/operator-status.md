---
title: Dynamic LINQ operator status
description: Support matrix for Alder Dynamic LINQ operators across IEnumerable, IQueryable, and IAsyncEnumerable.
---

# Dynamic LINQ operator status

This matrix records Alder's Dynamic LINQ surface across `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. The conceptual explanation belongs in [Dynamic LINQ](/explanation/dynamic-linq/); this page is the lookup table for operator coverage and provider boundaries.

Legend:

- `Supported`: implemented with deliberate API coverage.
- `Partial`: implemented, but not across every major surface or variant.
- `Provider-limited`: available in Alder, but some `IQueryable` providers reject the generated query shape.
- `Not implemented`: no direct dynamic operator.

## Core query operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Where` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Select` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `OrderBy` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `ThenBy` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `GroupBy` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `SelectMany` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Join` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `GroupJoin` | Supported | Available for `IEnumerable` and `IQueryable`. |

## Set and type operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Distinct` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `DistinctBy` | Partial | Available for `IEnumerable` only. |
| `Concat` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `Union` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `Intersect` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `Except` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `OfType` | Provider-limited | Works in Alder, but the tested EF Core SQLite shape is rejected by the provider. |
| `Cast` | Partial | Available for `IEnumerable` and `IQueryable`; no async variant. |
| `DefaultIfEmpty` | Partial | Supported for `IEnumerable` and `IQueryable`, but provider support for custom-default query shapes is limited. |

## Quantifiers and element operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Any` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `All` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `First` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `FirstOrDefault` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Single` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `SingleOrDefault` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Last` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `LastOrDefault` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Contains` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `ElementAt` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `ElementAtOrDefault` | Supported | Available for `IEnumerable` and `IQueryable`. |
| `SequenceEqual` | Provider-limited | Works in Alder, but the tested EF Core SQLite query shape is rejected by the provider. |

## Aggregation and windowing

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Count` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `LongCount` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Sum` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Average` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Min` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Max` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Aggregate` | Not implemented | No direct dynamic operator. |

## Paging and sequence control

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Skip` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Take` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `SkipWhile` | Provider-limited | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`; the tested EF Core SQLite query shape is rejected by the provider. |
| `TakeWhile` | Provider-limited | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`; the tested EF Core SQLite query shape is rejected by the provider. |
| `Reverse` | Supported | Available for `IEnumerable`, `IQueryable`, and `IAsyncEnumerable`. |
| `Append` | Provider-limited | Available for `IEnumerable` and `IQueryable`; the tested EF Core SQLite query shape is rejected by the provider. |
| `Prepend` | Provider-limited | Available for `IEnumerable` and `IQueryable`; the tested EF Core SQLite query shape is rejected by the provider. |
| `Page` / `PageResult` | Not implemented | No direct dynamic operator. |

## Dynamic-LINQ-specific helpers

| Feature | Alder status | Notes |
| --- | --- | --- |
| `AsDynamicEnumerable` | Not implemented | No direct equivalent. |
| `GroupByMany` | Not implemented | No direct equivalent. |

## Surface notes

- `IEnumerable` operators execute in process over delegates.
- `IQueryable` operators export expression trees and remain subject to provider translation.
- `IAsyncEnumerable` operators execute in process during asynchronous enumeration.
- Alder prefers strongly typed, C#-shaped APIs over string-based type-name conveniences.
- `EF.Property<T>(...)` is supported in exported query trees and Dynamic LINQ predicates and selectors, including provider-safe chained member and method composition.
- Query-tree export still blocks statically forbidden reflection-leaking members and calls.

## DataTable and DataRow

`DataRow` indexer expressions such as `row["City"]` are supported in Dynamic LINQ for `IEnumerable<DataRow>` and `IQueryable<DataRow>`.

`DataRowExtensions.Field<T>(...)` is blocked by the default sandbox because `System.Data` is denied by default.

To enable it, opt in explicitly:

- trust the `System.Data` namespace in your sandbox policy
- register the `System.Data` assembly for type resolution
- register `DataRowExtensions` as an extension-method container
