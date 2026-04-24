---
title: Architecture
description: Architectural explanation of Alder's parse-bind-execute pipeline, backend split, and integration surfaces.
---

# Architecture

Alder is a runtime C# expression engine with a single semantic pipeline and two execution backends. The architectural constraint is simple and strict: backend choice must not alter language semantics.

An expression is parsed once, bound against the active context, passed through validation and optimization, then executed by either the interpreter or the compiled backend. Both backends consume the same semantic form. Security policy, execution limits, and runtime dispatch rules apply uniformly.

## Pipeline

Alder evaluation has four stages:

1. Parse source text into syntax.
2. Bind syntax into a semantic form with resolved types, conversions, and operations where possible.
3. Apply validation and optimization passes.
4. Execute through the interpreter or compiled backend.

The binder is the semantic center of the system. When static information is sufficient, Alder resolves calls, members, indexes, and conversions during the binding phase. When it is not, Alder preserves a dynamic operation and resolves it at runtime. That boundary keeps Alder aligned with C# semantics without requiring every expression to be statically closed.

## Backend split

The interpreter executes Alder's bound form directly. It is the default execution path, the broadest compatibility surface, and the path used by asynchronous evaluation.

The compiled backend lowers that same bound form into a compiled delegate and reuses the delegate while the relevant context type surface remains stable. Its purpose is operational efficiency for repeated synchronous evaluation, not a different language contract.

## Semantic consistency

The parser, binder, sandbox validation, and execution constraints are shared across backends. Differences between interpreter and compiler are therefore engineering gaps, not separate contracts. Alder assumes semantic parity and treats backend divergence as a defect.

## Binding and runtime resolution

Alder is resolved-first, not resolved-only.

Operations with a deterministic static meaning are fixed during binding. That includes overload selection, member selection, conversion classification, and control-flow legality. Where static information is inconclusive, Alder keeps the operation dynamic and defers resolution to runtime dispatch.

That division has direct operational consequences:

- statically known expressions surface semantic errors earlier and produce more reusable artifacts
- expressions shaped by `object`, open-ended values, or late-bound members shift more work to runtime resolution

## Dispatch and AOT

Runtime operations use typed dispatch when generated metadata is available and fall back to reflection-based resolution when it is not. The typed path exists to support AOT-oriented deployments and reduce dependence on reflection metadata. It is an execution strategy, not a separate semantic mode.

Generated contexts can cover the types that matter in a trimmed or ahead-of-time build while reflection fallback preserves coverage for shapes outside that metadata set. Alder therefore supports mixed deployments: generated where deliberate, reflective where necessary.

## Configuration model

Most integration points are configuration-driven:

- modules expose named member surfaces
- functions expose global call sites
- type registration controls type lookup and extension-method discovery
- AOT registration adds generated type metadata
- a service provider resolves module instances
- compiler configuration enables compiled execution

These are the public extension surfaces that shape Alder's runtime model inside an application. They are sufficient for most integrations without requiring knowledge of the internal machinery behind them.

## Reuse and invalidation

Alder caches semantic and compiled artifacts, but cache reuse is gated by changes to the context's type surface. Value changes can often reuse prior work. Declared-type changes cannot, because they may alter overload resolution, conversion legality, or the choice between resolved and dynamic execution.

The cache policy is intentionally conservative. Alder prefers rebinding or recompilation to executing against stale semantic assumptions.

## Security and constraints

Sandbox enforcement and execution limits are separate concerns.

Sandbox validation determines whether an expression is allowed to perform a given operation. Execution constraints bound the amount of work the expression may perform at runtime through statement counts, loop limits, timeouts, and cancellation. Both apply regardless of backend.

## Tradeoffs

Alder's architecture makes a few explicit tradeoffs:

- a single semantic pipeline reduces behavioral drift, but requires rigorous parity across execution backends
- resolved-first binding improves diagnostics and cache reuse, but dynamic scenarios still require a capable runtime dispatch layer
- typed dispatch improves AOT viability, but reflection fallback must remain semantically aligned
- the convenience of a global facade coexists with an explicit engine API, but the facade is necessarily more stateful

## Related pages

- [Binding system](/explanation/binding-system/)
- [Typed dispatch and AOT](/explanation/typed-dispatch/)
- [Execution model](/reference/execution-model/)
- [ECMA-334 conformance](/reference/language/ecma-conformance/)
