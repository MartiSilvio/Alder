---
title: Binding system
description: How Alder assigns semantic meaning to parsed syntax, and where it resolves operations statically versus deferring them to runtime.
---

# Binding system

Alder's binder is the semantic boundary between syntax and execution. It determines types, conversions, overloads, member access, control-flow legality, and the boundary between statically resolved and dynamically resolved execution.

## Role in the pipeline

Both execution backends depend on the same bound representation. Binding is therefore the phase where most language behavior is fixed. If a rule belongs to semantic interpretation rather than raw syntax, it belongs here.

This includes:

- type resolution
- overload resolution
- conversion classification
- legality rules for constructs such as `break`, `continue`, `return`, `await`, and assignment targets
- the decision to emit a resolved operation or a dynamic one

## Output shape

Binding produces an executable semantic form that carries:

- the static type Alder can prove for each expression
- a concrete operation plan when selection is deterministic
- a deferred runtime operation when selection is not deterministic
- diagnostics for invalid constructs

Unknown static type is treated explicitly instead of being collapsed into `object` for convenience. That distinction matters because it governs whether an operation can be fixed during binding or must remain open until runtime.

## Resolved versus dynamic binding

Dynamic binding is a valid execution path, not a recovery mechanism.

If the target type, argument types, and available conversions make an operation deterministic, Alder binds it as resolved. If they do not, Alder preserves a dynamic form and lets runtime dispatch decide against the actual values involved. That usually occurs with `object`-typed values, ambiguous overload sets, or member access that depends on runtime shape rather than declared type.

This split is fundamental to the engine's binding contract. Alder does not guess when static information is inconclusive.

## Type resolution

Binding depends on a fully configured type environment. Type lookup draws from:

- built-in C# type names
- registered assemblies and namespaces
- generic and array type forms
- local declarations and context-provided variable types

The quality of type registration directly affects the quality of binding. Better type information yields earlier diagnostics, more resolved operations, and more stable reuse. Loosely typed integration surfaces push more work onto runtime dispatch.

## Calls, members, and indexes

The same binding policy applies across calls, member access, and index access:

- resolve the operation when the static information is sufficient
- defer the operation when it is not

In practice, strongly typed expressions usually fail earlier and more precisely, while dynamic expressions remain executable but pay for that flexibility in later resolution and narrower reuse.

## Error modes

Binding distinguishes between invalid input and unsupported engine behavior.

Semantically invalid expressions fail with diagnostics. Typical cases include impossible conversions, illegal control flow, or required members that cannot be resolved.

Unsupported binding is different. It means Alder has no semantic representation for that construct. That state is recorded as unavailable rather than retried on every execution attempt.

Dynamic fallback should not be confused with unsupported binding. A dynamic node is a successful bind result. Unsupported binding is not.

## Reuse model

Bound results are cached against the active context's type surface and source text. Reuse therefore depends on the semantic environment remaining equivalent. Value-only changes can usually reuse a bound result. Declared-type changes cannot, because they may alter overload selection, conversion legality, and dispatch strategy.

Cache reuse is gated by correctness, not by maximizing hit rate.

## Configuration influence

Several public configuration surfaces feed directly into binding:

- registered modules extend identifier and member resolution
- registered functions add global call targets
- registered assemblies and namespaces expand type lookup
- registered extension-method containers participate in method resolution
- language mode changes the accepted surface and therefore the semantic search space

These settings define the binder's world view. They are not incidental runtime options.

## Tradeoffs

The binding system is conservative by design:

- semantic correctness takes precedence over forcing early resolution
- dynamic execution remains available when static resolution is inconclusive
- bound reuse is aggressive only while the surrounding type information remains stable
- both backends inherit the same semantic decisions, which reduces drift but raises the cost of binding bugs

## Related pages

- [Architecture](/explanation/architecture/)
- [Typed dispatch and AOT](/explanation/typed-dispatch/)
- [Execution model](/reference/execution-model/)
- [ECMA-334 conformance](/reference/language/ecma-conformance/)
