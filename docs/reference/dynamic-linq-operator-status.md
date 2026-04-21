---
title: Dynamic LINQ Operator Status
description: Support matrix for Dynamic LINQ operators in Alder compared to System.Linq.Dynamic.Core.
---

# Dynamic LINQ Operator Status

Status snapshot based on current `AlderLinqExtensions`, Dynamic LINQ tests, and EF/query-provider coverage.

Legend:

- `Supported`: implemented with deliberate API coverage.
- `Partial`: implemented, but not across all common front doors or missing an important variant.
- `Provider-Limited`: implemented for LINQ, but current EF Core SQLite coverage explicitly rejects some `IQueryable` shapes.
- `Not Yet`: no direct dynamic operator yet.

## Core Query Operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Where` | Supported | `WhereDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Select` | Supported | `SelectDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `OrderBy` | Supported | `OrderByDynamic` + `OrderByDescendingDynamic` for `IEnumerable` and `IQueryable`. |
| `ThenBy` | Supported | `ThenByDynamic` + `ThenByDescendingDynamic` for `IEnumerable` and `IQueryable`. |
| `GroupBy` | Supported | `GroupByDynamic` for `IEnumerable` and `IQueryable`. |
| `SelectMany` | Supported | `SelectManyDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`; EF-covered for relationship composition. |
| `Join` | Supported | `JoinDynamic` for `IEnumerable` and `IQueryable`; EF-covered. |
| `GroupJoin` | Supported | `GroupJoinDynamic` for `IEnumerable` and `IQueryable`; EF-covered for provider-safe projections. |

## Set / Type Operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Distinct` | Supported | `DistinctDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `DistinctBy` | Partial | `DistinctByDynamic` exists for `IEnumerable` only. |
| `OfType` | Provider-Limited | `OfTypeDynamic<TResult>` for `IEnumerable` and `IQueryable`; EF Core SQLite rejects the tested `IQueryable<object>` projection shape. |
| `Cast` | Partial | `CastDynamic<TResult>` for `IEnumerable` and `IQueryable`; EF-covered on a simple object projection, no async variant. |
| `DefaultIfEmpty` | Partial | Parameterless and custom-default overloads for `IEnumerable` and `IQueryable`; EF Core SQLite rejects the custom-default `IQueryable` shape. |

## Quantifiers / Element Operators

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Any` | Supported | `AnyDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `All` | Supported | `AllDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `First` | Supported | `FirstDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `FirstOrDefault` | Supported | `FirstOrDefaultDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Single` | Supported | `SingleDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `SingleOrDefault` | Supported | `SingleOrDefaultDynamic` for `IEnumerable` and `IQueryable`. |
| `Last` | Supported | `LastDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `LastOrDefault` | Supported | `LastOrDefaultDynamic` for `IEnumerable` and `IQueryable`. |
| `Contains` | Supported | `ContainsDynamic` for `IEnumerable` and `IQueryable`; EF-covered. |
| `ElementAt` | Supported | `ElementAtDynamic` for `IEnumerable` and `IQueryable`; EF-covered. |
| `ElementAtOrDefault` | Supported | `ElementAtOrDefaultDynamic` for `IEnumerable` and `IQueryable`; EF-covered. |
| `SequenceEqual` | Provider-Limited | `SequenceEqualDynamic` for `IEnumerable` and `IQueryable`; EF Core SQLite currently rejects the `IQueryable` shape. |

## Aggregation / Windowing

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Count` | Supported | `CountDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `LongCount` | Supported | `LongCountDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Sum` | Supported | `SumDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Average` | Supported | `AverageDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Min` | Supported | `MinDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Max` | Supported | `MaxDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Aggregate` | Not Yet | Not implemented. |

## Paging / Sequence Control

| Operator | Alder status | Notes |
| --- | --- | --- |
| `Skip` | Supported | `SkipDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Take` | Supported | `TakeDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `SkipWhile` | Provider-Limited | `SkipWhileDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`; EF Core SQLite rejects the `IQueryable` shape. |
| `TakeWhile` | Provider-Limited | `TakeWhileDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`; EF Core SQLite rejects the `IQueryable` shape. |
| `Reverse` | Supported | `ReverseDynamic` for `IEnumerable`, `IQueryable`, `IAsyncEnumerable`. |
| `Append` | Provider-Limited | `AppendDynamic` for `IEnumerable` and `IQueryable`; EF Core SQLite rejects the `IQueryable` shape. |
| `Prepend` | Provider-Limited | `PrependDynamic` for `IEnumerable` and `IQueryable`; EF Core SQLite rejects the `IQueryable` shape. |
| `Page` / `PageResult` | Not Yet | Not implemented. |

## Dynamic-Core Specific Helpers

| Feature | Alder status | Notes |
| --- | --- | --- |
| `AsDynamicEnumerable` | Not Yet | No direct equivalent. |
| `GroupByMany` | Not Yet | No direct equivalent. |

## Front-Door Notes

- `IEnumerable` is the broadest supported surface.
- `IQueryable` is supported where the generated expression tree is provider-safe and the provider can translate it.
- `IAsyncEnumerable` support exists only for operators that naturally execute in-process over compiled delegates.
- Alder intentionally prefers generic/C#-shaped APIs for type and sequence operators over string-based type-name conveniences.
- `EF.Property<T>(...)` is supported in query-tree export for `ParseAsExpression(...)` and Dynamic LINQ query predicates/selectors, including chained provider-safe member/method composition such as string predicates.
- Query-tree export rejects statically forbidden reflection-leaking members and calls such as `typeof(DateTime).Assembly`; those remain blocked rather than being emitted into provider-facing trees.

## DataTable / DataRow Notes

`DataRow` indexer expressions such as `row["City"]` are supported in Dynamic LINQ for `IEnumerable<DataRow>` and `IQueryable<DataRow>`.

`DataRowExtensions.Field<T>(...)` is blocked by the default sandbox because `System.Data` is denied by default. It can be enabled per-engine with an explicit trusted-namespace opt-in plus extension-method registration:

```xml
<DynamicLinqDataTableOptIn>
  <DefaultBehavior>System.Data is denied by the default sandbox.</DefaultBehavior>
  <SupportedByDefault>DataRow indexer access such as row["City"]</SupportedByDefault>
  <ManualOptIn>
    <TrustedNamespace>System.Data</TrustedNamespace>
    <RegisterAssembly>typeof(DataRowExtensions).Assembly</RegisterAssembly>
    <RegisterExtensionMethods>typeof(DataRowExtensions)</RegisterExtensionMethods>
  </ManualOptIn>
</DynamicLinqDataTableOptIn>
```
