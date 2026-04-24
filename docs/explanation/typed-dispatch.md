---
title: Typed dispatch and AOT
description: How Alder uses generated type metadata for AOT-safe execution and when it falls back to reflection-based runtime dispatch.
---

# Typed dispatch and AOT

Alder supports ahead-of-time deployment by separating semantic binding from the mechanics of runtime invocation. When generated type metadata is available, Alder uses typed dispatch for common runtime operations. When it is not, or when the generated path does not cover a given shape, Alder falls back to reflection-based dispatch.

The contract is behavioral equivalence. Typed dispatch exists to improve deployment characteristics, not to define a separate runtime semantics.

## What typed dispatch covers

Typed dispatch is a pre-registered operation layer for runtime activities such as:

- member reads and writes
- method invocation
- index access
- construction

It allows Alder to execute known operations against known runtime types without performing reflective discovery at the point of execution.

## Why it exists

The primary use case is AOT-oriented deployment: NativeAOT, IL2CPP-style environments, aggressive trimming, and any deployment where reflective discovery is constrained or undesirable.

Typed dispatch is also useful outside strict AOT environments when predictable registration and explicit metadata are preferable to open-ended reflective lookup.

## Dispatch order

For operations that support typed dispatch, Alder follows a typed-first, reflective-second policy:

1. Attempt a typed dispatch entry for the runtime type.
2. Reuse compatible base-type metadata where applicable.
3. Fall back to the standard runtime dispatch path when the typed path declines the operation.

A miss on the typed path is not, by itself, an error. It means only that execution continues through the general runtime path.

## Fallback behavior

Reflection fallback occurs when:

- no generated metadata is registered for the runtime type
- the registered metadata does not cover the requested operation shape
- the call depends on runtime forms that the typed path intentionally does not encode
- case-insensitive lookup requires canonicalization and still does not produce a typed match

This additive model makes incremental AOT adoption practical. Generated contexts can cover the critical types in a deployment while reflection continues to service the long tail.

## Case sensitivity

Typed dispatch entries are exact. In case-insensitive mode, Alder preserves the external contract by retrying against the canonical member name before leaving the typed path. The user-visible rule remains stable:

- case-sensitive engines require exact casing
- case-insensitive engines accept equivalent casing when Alder can canonicalize the name
- unresolved requests continue through the normal runtime path

## Generated contexts

Typed dispatch is enabled by registering generated type contexts through engine configuration. Those contexts contribute operation metadata for the types they cover. Later registrations override earlier ones for the same runtime type, which makes user-supplied contexts a practical way to refine or replace built-in coverage.

That precedence rule matters in real integrations. AOT configuration is not merely additive metadata; it is part of the runtime dispatch policy.

## What it does not change

Typed dispatch does not change:

- parsing
- binding rules
- overload resolution
- sandbox policy
- execution limits

It changes only how runtime operations are carried out after the semantic decision has already been made.

## Limits by design

Typed dispatch is intentionally selective. Some shapes remain on the reflection path because they are awkward to encode safely, would require fragile special cases, or are not worth carrying in generated metadata. Conservative coverage is preferable to a broad but brittle typed layer.

## Tradeoffs

Typed dispatch trades generality for deployment control:

- it improves AOT viability and can reduce reflection pressure
- it requires explicit metadata registration and coverage management
- it preserves overall behavior through fallback, which means the typed and reflective paths must stay aligned
- it favors exactness over breadth, leaving some uncommon shapes to the general runtime path by design

## Related pages

- [Architecture](/explanation/architecture/)
- [Binding system](/explanation/binding-system/)
- [Configuration](/reference/configuration/)
- [Execution model](/reference/execution-model/)
