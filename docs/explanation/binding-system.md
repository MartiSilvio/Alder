---
title: Binding system
description: How Alder transforms parsed syntax into executable bound trees with resolved and dynamic binding paths.
---

# Binding system

## Context
Alder’s binder is the semantic boundary between syntax and execution. The parser produces `Expr` nodes. The binder produces `BoundExpr` nodes that encode executable meaning: static type shape, resolved members and calls, deferred dynamic operations, control-flow legality, and diagnostics metadata.

Interpreter and compiled backends execute bound trees. `AlderExpression` owns binding orchestration, cache reuse, and bind-failure normalization.

## Binding lifecycle summary
1. Parsing produces an AST (`Expr`) stored in `AlderExpression`.
2. `AlderExpression.GetOrCreateBoundExpression` invokes `Binder.Bind`, which routes each AST node through generated dispatch.
3. Per-node binders recursively bind children via `BinderContext.Bind` and construct `BoundExpr` nodes.
4. Binders resolve types and symbols through `BindingContext`, `TypeResolver`, `TypeMetadataProvider`, and overload-resolution services.
5. For each bind site, the binder emits a resolved node when selection is deterministic, otherwise it emits a dynamic node.
6. The bound tree is cached per `AlderContext` identity and context type-inference version.
7. On semantic bind errors, recovery binding collects diagnostics and `AlderExpression` raises `BindingFailed`; on unsupported bind paths, `BindingNotSupportedException` is recorded as sticky unavailability.

## Role in execution pipeline
Binding participates in all semantic execution paths:

- Evaluation path obtains a bound tree before interpreter execution.
- Compilation path obtains the same bound tree before compilation pipeline passes.
- Validation path binds for diagnostics without executing.

Pipeline responsibilities are explicit:

- Parsing defines syntax.
- Binding defines executable semantic shape.
- Runtime executes bound semantics and does not repeat bind-time structural checks.

## Core design
The binding system uses four design decisions:

- Dispatch is generated from `[BindsNode]`; generator diagnostics validate binder coverage.
- Binding is scoped: `BindingContext` tracks lexical locals, read-only local reasons, language mode, and runtime metadata access.
- The type model is total: every bound node has `BoundType`; unknown static type is `BoundUnknownType`.
- Output is dual-path by design: resolved nodes encode deterministic static selection; dynamic nodes encode runtime resolution.

Resolved and dynamic outputs are both first-class executable results.

## Binding flow
`AlderExpression.GetOrCreateBoundExpression` performs version-gated bind retrieval or bind creation.

When no valid cache entry exists, `Binder.Bind` runs generated dispatch and binders recursively construct bound children. Bound nodes carry normalized source span metadata.

If normal binding throws `AlderException`, `AlderExpression` reruns with `BindRecovering`. Recovery returns an error-marked bound surrogate tree, accumulates diagnostics, and surfaces `AlderException(BindingFailed)` with collected diagnostics.

`BindingNotSupportedException` follows a separate path: `TryGetOrCreateBoundExpression` stores `_bindingUnavailable` and `_bindingUnavailableReason` on the expression. Future bind attempts for that expression return failure immediately.

## Type resolution
Type resolution is delegated to `TypeResolver` and is used wherever binders require concrete CLR types: type references, casts, declarations, object creation, foreach declarations, and catch typing.

`TypeResolver` uses fixed precedence: built-in keywords, implicit imports, explicit imports with ambiguity handling, fully qualified names, then generic/array specialization.

`BindingContext` contributes local and runtime type information:

- Locals carry explicit `BoundType` and stable local IDs.
- Runtime declared variable type takes precedence.
- Runtime value type is used when declaration is absent and value is non-null.
- Missing information yields `BoundType.Unknown`.

Identifier binding follows that model: type identifiers become `BoundTypeRefExpr`; value identifiers become `BoundIdentifierExpr` with known or unknown static type.

## Call and member resolution
Member resolution (`MemberAccessBinder` + `MemberBinderService`) selects static or instance target, then resolves property, field, and method-group in that order. It emits resolved member nodes on success and `BoundDynamicMemberAccessExpr` on unresolved access. Structural member metadata (`BoundStructuralType`) participates in selection.

Call resolution (`CallBinder` + `CallBinderService` + `OverloadResolver`) handles module static calls, instance/static method groups, extension methods, named/default/params mapping, and lambda/method-group argument descriptors. If call planning succeeds, binder emits `BoundResolvedCallExpr` with a concrete `ResolvedCall` plan. If planning is not deterministic, binder emits `BoundDynamicCallExpr`.

Index resolution follows the same pattern. `IndexAccessBinder` and `MultiDimIndexAccessBinder` emit resolved index nodes when bind services produce a concrete plan; otherwise they emit dynamic index nodes.

## Dynamic fallback behavior
Dynamic fallback is a deliberate bind result.

Decision boundary:

- Binder emits resolved nodes only when member/call/index/type selection is deterministic with available static information.
- Binder emits dynamic nodes when deterministic selection is not possible at bind time.

This boundary is enforced in code. Example: `CallBinderService.TryBindFromTypes` returns `false` for multi-overload sets when any argument type is `object`, and binder emits a dynamic call node.

Dynamic fallback is not bind failure. Bind failure is represented by exceptions (`AlderException` or `BindingNotSupportedException`). Dynamic nodes remain valid executable output.

## Binder dispatch model
Binder dispatch is generated by `BinderDispatchGenerator` from classes annotated with `[BindsNode(typeof(...))]`.

Generator contract:

- Binder class is static.
- Binder class exposes `public static BoundExpr Bind(TExpr, BindingContext, BinderContext)`.
- Violations emit generator errors (`ALDR9001`, `ALDR9002`).

Generated output builds a `switch` over runtime AST type and routes each node to its binder. If no binder exists for a node type, generated dispatch throws `BindingNotSupportedException` with the unsupported type name.

Dispatch ordering is inheritance-depth-aware so specific expression types are matched before base types.

## Error handling and recovery
The binder has two semantic error modes:

- Semantic validation failures (`AlderException`) are recoverable for diagnostics collection.
- Unsupported binding shape (`BindingNotSupportedException`) marks expression-level bind unavailability.

Recovery behavior:

- `BindRecovering` catches `AlderException`, records normalized diagnostics, and returns error-marked placeholders.
- Diagnostic normalization assigns missing span/line/column from source text and applies fallback diagnostic code when missing.
- `AlderExpression` converts recovery output into `BindingFailed` exceptions with the diagnostic set.

This prevents partially invalid trees from entering normal evaluation paths.

## Context interaction and versioning
Binding cache validity is versioned against context type surface:

- Cache key is `AlderContext` object identity.
- Cache reuse requires exact `GetTypeInferenceVersion()` match.
- Version mismatch invalidates prior bound entries for that context.

`BindingContext` carries lexical state that influences bind output: local declarations, read-only local reasons, and legality flags (`InLoop`, `InSwitch`, `InLockBody`, `InFinally`, `InCatch`).

These flags enforce language constraints during binding, including break/continue placement, return in finally, and await inside lock body.

## Extension model
Binding extensibility is attribute-driven and dispatch-generated.

A contributor extends binding by adding an AST node binder that satisfies the `[BindsNode]` contract. The generator integrates that binder into `Binder.Dispatch` and validates signature correctness at build time.

Behavioral extension also depends on runtime-configured resolution services:

- type resolution through `TypeResolver`
- member/index metadata through `TypeMetadataProvider`
- function/module registries that affect identifier and call binding
- registered extension types that affect extension-method binding

## Constraints and invariants
The binder enforces these invariants:

- Every `BoundExpr` has a non-null `StaticType`.
- `BoundUnknownType` is the only representation of unknown static type information.
- Resolved nodes are emitted only when bind-time selection is deterministic.
- Dynamic nodes are emitted when bind-time selection is not deterministic.
- Resolved and dynamic nodes are both valid bind outputs.
- Binding does not expose partially invalid trees to evaluation paths; semantic errors surface as `BindingFailed`.
- Bound-cache reuse occurs only when context identity and type-inference version both match.
- Unsupported binding is sticky per `AlderExpression` once `BindingNotSupportedException` is recorded.
- Read-only local reasons are enforced at assignment bind sites.
- Flag-constrained control-flow rules are enforced during binding.

## Tradeoffs
The binder prioritizes semantic correctness under incomplete static information.

This choice has direct consequences:

- Dynamic fallback preserves executable semantics when static certainty is unavailable.
- Sticky unsupported-binding state prevents repeated unsupported bind attempts but keeps the expression unavailable until reparsed or recreated.
- Lambda argument rebinding improves overload-selection correctness but rejects capture patterns in that typed rebinding path.
- Context-version gating prevents stale semantic reuse while value-only state changes still reuse bound trees.

## Related pages
- [Architecture](/explanation/architecture/)
- [Execution model](/reference/execution-model/)
- [ECMA-334 conformance](/reference/language/ecma-conformance/)
