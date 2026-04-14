---
title: Typed dispatch and AOT
description: Architectural explanation of Alder's typed dispatch metadata, generated context integration, and reflection fallback model.
---

# Typed dispatch and AOT

## Context

Alder runs two execution backends (interpreter and compiled). Both backends rely on the same runtime operations for member access, method invocation, index access, and construction. Typed dispatch is the reflection-free operation layer when metadata is available ahead of time.

The core abstraction is `Alder.Aot.TypedDispatch` (`src/Alder/Aot/TypedDispatch.cs`). It defines operation-specific `Try*` methods (`TryGet`, `TrySet`, `TryGetStatic`, `TryGetIndex`, `TrySetIndex`, `TryCreate`, `TryInvoke`, `TryInvokeStatic`) for one CLR `Type`. Each method is optional and defaults to `false`, so misses are explicit and drive reflection fallback.

`Alder.Aot.AlderTypeContext` (`src/Alder/Aot/AlderTypeContext.cs`) groups dispatch metadata into registrable contexts. A context also returns delegate factories and extension dispatch metadata for AOT closure cases.

## Role in execution pipeline

Typed dispatch is not a separate pipeline stage. It is a runtime dispatch tier inside evaluation-time operations:

- Interpreter path calls runtime helpers such as `MemberAccess`, `MethodInvoker`, and `ConstructionRuntime`.
- Compiled path emits direct reflection expressions or routes resolved nodes back through runtime helpers when `PreferResolvedRuntimeDispatch` is enabled (`src/Alder.Compiled/Compilation/Emission/Emitters/*`).

`AlderEngine` builds `AlderConfig` with type-dispatch dictionaries and optional extension/delegate maps (`src/Alder/AlderEngine.cs`). Runtime helpers query this config and try typed dispatch before reflection.

## Core design

The design has four layers:

1. Metadata contract:
   `TypedDispatch` defines a per-type, per-operation `Try*` contract.

2. Registration and storage:
   `AlderEngine` materializes context metadata into `Dictionary<Type, TypedDispatch>` and then into `FixedDictionary<Type, TypedDispatch>` in config. Lookup uses `AlderConfig.TryGetDispatch`, which walks runtime type and base types (`src/Alder/Runtime/AlderConfig.cs`).

3. Runtime gateway:
   `TypedDispatchHelper` centralizes typed-dispatch attempts and canonical-name retries for case-insensitive mode (`src/Alder/Runtime/TypedDispatchHelper.cs`).

4. Operation call sites:
   `MethodInvoker`, `MemberAccess`, and `ConstructionRuntime` call `TypedDispatchHelper` first, then run reflection paths when the typed operation returns `false`.

Typed dispatch never throws to signal “not handled”; it returns `false`.

## Dispatch lifecycle summary

Dispatch lifecycle sequence:

1. Operation request enters runtime helper (`MethodInvoker`, `MemberAccess`, `ConstructionRuntime`).
2. Helper calls `TypedDispatchHelper` with operation details.
3. `TypedDispatchHelper` resolves metadata via `AlderConfig.TryGetDispatch(type, out dispatch)`.
4. If metadata exists, helper calls operation-specific `dispatch.Try*`.
5. If call succeeds (`true`), runtime returns the result (with reflection-leak guarding on read paths).
6. If call misses (`false`), runtime executes reflection-based resolution/invocation.

Fallback decision points:

- No registered metadata for runtime type (or base chain) -> reflection path.
- Registered metadata exists but does not implement that operation or signature -> reflection path.
- Case-insensitive name mismatch -> typed dispatch retries with canonical member name; if retry misses, reflection path.

## Runtime dispatch flow

`TypedDispatchHelper` implements operation-specific entry points:

- Method calls: `TryInvokeInstance`, `TryInvokeStatic`.
- Member reads/writes: `TryGetMember`, `TryGetStaticMember`, `TrySetMember`.
- Indexer reads/writes: `TryGetIndex`, `TrySetIndex`.
- Construction: `TryCreate`.

Notable mechanisms:

- Case-insensitive canonicalization:
  When `IsCaseSensitive == false`, helper first tries the user-provided name, then resolves canonical member name through `TypeMetadataProvider` + reflection flags and retries dispatch (`ResolveCanonicalName`).

- Base-type metadata reuse:
  `AlderConfig.TryGetDispatch` walks `type -> baseType -> ...` until `object`, so metadata registered for a base class serves derived runtime instances.

- Guarded read values:
  Read operations pass returned values through `TypeHelpers.GuardReflectionLeak` to block forbidden reflection objects from escaping runtime/member APIs.

## Reflection fallback behavior

Typed dispatch miss does not fail the operation by itself. Each caller has explicit reflection fallback logic:

- Method calls (`MethodInvoker`):
  - Instance and static methods attempt typed dispatch first.
  - On miss, runtime performs overload resolution through reflection metadata and invokes selected `MethodInfo`.
  - Extension method invocation is interleaved per extension type: typed dispatch first, then reflection resolver.

- Member access (`MemberAccess`):
  - For ordinary object targets, runtime tries typed member dispatch first.
  - On miss, runtime resolves property/field by reflection; unresolved names become `MethodRef` for later invocation.
  - For `Type` targets, runtime attempts static-member dispatch first, then reflection over static members; if static lookup misses, the `Type` instance remains a valid instance target for dynamic calls.

- Index access (`MemberAccess.GetIndex` / `SetIndex`):
  - Built-in fast paths (string, `IList`, dictionaries) run first.
  - Then typed index dispatch.
  - Then reflection indexer matching (`FindMatchingIndexer`) and invocation.

- Constructor invocation (`ConstructionRuntime.InvokeConstructor`):
  - Runtime calls `TypedDispatchHelper.TryCreate` first.
  - On miss, runtime uses reflection constructor overload resolution and invokes the selected constructor.

`AotBuilder.ClearBuiltInContext()` states that reflection-based dispatch remains available where runtime permits it (`src/Alder/AlderOptions.cs`).

## Generated context integration

Generated and built-in contexts integrate in `AlderEngine` during config construction:

1. Start with built-in context (`AlderBuiltInContext.Default`) unless cleared.
2. Merge `GetTypeMetadata()` entries into `Dictionary<Type, TypedDispatch>`.
3. Merge delegate factories (`GetDelegateFactories()`) and extension dispatches (`GetExtensionDispatches()`).
4. Apply additional contexts in registration order (`o.Aot.UseGeneratedContext(...)`).

Conflict resolution is overwrite-based (`dictionary[type] = metadata`), so later contexts replace earlier metadata for the same type. `BuiltInContextTests.UseGeneratedContext_UserOverridesBuiltIn` verifies this rule.

Compiled backend integration detail:

- `preferResolvedRuntimeDispatch` is set when dynamic code is unsupported or when additional generated contexts are present (`src/Alder/AlderEngine.cs`).
- Compiled emitters then route resolved member/call assignments through runtime helpers (`GetResolvedMember`, `SetResolvedMember`, `InvokeResolvedMethod`), so compiled execution still applies typed-dispatch tiers.

## Method, member, and index dispatch

Method dispatch:

- `InvokeResolvedMethod` and `TryInvokeInstanceMethod` attempt typed dispatch before reflection resolution/invocation.
- Extension methods: `TryInvokeExtensionMethod` checks context-level generated extension dispatches (`ExtensionDispatches`), then typed dispatch on registered extension types, then reflection extension resolution.

Member dispatch:

- `GetMember` and `GetResolvedMember` use typed dispatch for instance/static property-field reads.
- `SetMember` and `SetResolvedMember` use typed dispatch for writes before reflection writes.

Index dispatch:

- `GetIndex` / `SetIndex` invoke typed index operations after direct native container fast paths and before reflection indexer fallback.
- Interpreted resolved index evaluator uses direct collection fast paths when binder marks the node as direct; otherwise it routes to `MemberAccess.GetIndex` and still uses typed dispatch.

Constructor dispatch:

- `ConstructionRuntime.InvokeConstructor` is the constructor dispatch hub and uses the same typed-first, reflection-second policy.

## Generator model

The incremental source generator (`AlderSourceGenerator`) builds dispatch metadata from two sources:

- Built-in context symbol (`AlderBuiltInContext`) plus built-in catalog types.
- User contexts deriving from `AlderTypeContext` and annotated with `[AlderRegistered(typeof(...))]`.

Model extraction (`TypeParser`) records:

- Public properties/fields (instance + static).
- Constructors.
- Single-parameter indexers.
- Methods that are dispatch-safe for generation.

Generation outputs:

- Per-type `TypedDispatch` subclasses (`TypeMetadataEmitter`) implementing operation-specific `Try*` methods.
- Context partial class with `Default` singleton, `s_metadata`, and `GetTypeMetadata()` override (`ContextEmitter`).
- Optional extension dispatch class (`LinqValueTypeDispatch`) for LINQ value-type cases.
- Optional delegate factory map for required delegate shapes.
- Type roots for closed generic preservation under trimming/AOT.

Key generator filtering/invariants:

- Value-type writable members/indexers are not emitted for `TrySet`/`TrySetIndex` (boxed-mutation correctness issue).
- Unsupported signatures (for example `ref`/`out` parameters, by-ref returns, unsafe parameter shapes) are skipped.
- Generic methods are expanded only for supported closed combinations; unresolved shapes stay on reflection paths.

## Constraints and invariants

- `TypedDispatch` is additive, not authoritative. Returning `false` preserves fallback behavior.
- For a given runtime type, operation, and configuration, dispatch behavior is deterministic.
- Typed and reflection paths must remain behaviorally equivalent for covered operations.
- Read-path reflection leak guards apply to typed and reflection results.
- Dispatch lookup is runtime type + base chain; interfaces are not part of `TryGetDispatch` traversal.
- Context merge order is deterministic and last-write-wins by type key.
- Generated dispatch is exact-name based; case-insensitive mode relies on runtime canonical-name retry.

## Tradeoffs

- Typed-first dispatch reduces runtime dependency on reflection metadata but requires explicit metadata coverage and merge management.
- Reflection fallback preserves behavior coverage and compatibility but keeps a second execution path that must stay semantically aligned.
- Case-insensitive canonical retry preserves correctness with generated exact-case metadata but adds reflection metadata lookup on case mismatches.
- Extension dispatch layering (generated extension dispatch -> typed extension type dispatch -> reflection resolver) preserves AOT behavior for generic-closure edge cases but increases dispatch complexity.

## Related pages

- [Architecture](/explanation/architecture/)
- [Binding system](/explanation/binding-system/)
- [Execution model](/reference/execution-model/)
