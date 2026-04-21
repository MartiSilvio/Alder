---
title: Dynamic LINQ Operator Status
description: Support matrix for Dynamic LINQ operators in Alder compared to System.Linq.Dynamic.Core.
---

# Dynamic LINQ Operator Status

Status snapshot based on current `AlderLinqExtensions` and benchmark usage.

Legend:

- `Supported`: implemented with parity-oriented API coverage.
- `Partial`: implemented, but not across all common query shapes (`IEnumerable`, `IQueryable`, `IAsyncEnumerable`) or missing a common variant.
- `Not Yet`: no direct dynamic operator yet.

## Core Query Operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Where` | Supported | `WhereDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Select` | Supported | `SelectDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `OrderBy` | Supported | `OrderByDynamic` + `OrderByDescendingDynamic` for `IEnumerable` and `IQueryable`. |
| `ThenBy` | Supported | `ThenByDynamic` + `ThenByDescendingDynamic` for `IEnumerable` and `IQueryable`. |
| `GroupBy` | Supported | `GroupByDynamic` for `IEnumerable` and `IQueryable`. |
| `SelectMany` | Not Yet | No `SelectManyDynamic` API yet. |
| `Join` | Not Yet | No dynamic join operator yet. |
| `GroupJoin` | Not Yet | No dynamic group join operator yet. |

## Set / Type Operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Distinct` | Not Yet | No direct `DistinctDynamic`. |
| `DistinctBy` | Partial | `DistinctByDynamic` exists for `IEnumerable` only. |
| `OfType` | Not Yet | Not implemented. |
| `Cast` | Not Yet | Not implemented. |
| `DefaultIfEmpty` | Not Yet | Not implemented. |

## Quantifiers / Element Operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Any` | Supported | `AnyDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `All` | Supported | `AllDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `First` | Supported | `FirstDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `FirstOrDefault` | Supported | `FirstOrDefaultDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Single` | Partial | `SingleDynamic` for `IEnumerable` only. |
| `SingleOrDefault` | Partial | `SingleOrDefaultDynamic` for `IEnumerable` only. |
| `Last` | Partial | `LastDynamic` for `IEnumerable` only. |
| `LastOrDefault` | Partial | `LastOrDefaultDynamic` for `IEnumerable` only. |

## Aggregation / Windowing

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Count` | Supported | `CountDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `LongCount` | Not Yet | Not implemented. |
| `Sum` | Supported | `SumDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Average` | Supported | `AverageDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Min` | Supported | `MinDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Max` | Supported | `MaxDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Aggregate` | Not Yet | Not implemented. |

## Paging / Sequence Control

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Skip` | Not Yet | Not implemented. |
| `Take` | Not Yet | Not implemented. |
| `SkipWhile` | Not Yet | Not implemented. |
| `TakeWhile` | Not Yet | Not implemented. |
| `Reverse` | Not Yet | Not implemented. |
| `Page` / `PageResult` | Not Yet | Not implemented. |

## Dynamic-Core Specific Helpers

| Feature | Alder status | Notes |
| --- | --- | --- |
| `AsDynamicEnumerable` | Not Yet | No direct equivalent. |
| `GroupByMany` | Not Yet | No direct equivalent. |

