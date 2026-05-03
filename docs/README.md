# Alder documentation

Alder is an embeddable C# runtime engine: parser, binder, interpreter, optional compiled backend, Dynamic LINQ, expression-tree export, AOT-friendly generated dispatch, sandbox policy, and execution constraints, all in one library. These docs describe the engineering surface: what Alder does, how it behaves, and where its boundaries lie.

Concepts establish the mental model. Guides walk through concrete integration tasks. Reference documents exact contracts. Operations covers production behavior, security, and reuse.

## Concepts

- [Architecture](./concepts/architecture.md): parse-bind-execute pipeline, backend split, integration surfaces.
- [Binding system](./concepts/binding-system.md): where Alder resolves operations statically and where it defers to runtime dispatch.
- [Compiled backend](./concepts/compiled-backend.md): synchronous delegate compilation and LINQ expression export.
- [Async execution](./concepts/async-execution.md): interpreter-backed asynchronous evaluation, await semantics, cancellation.
- [Dynamic LINQ](./concepts/dynamic-linq.md): runtime query composition over `IEnumerable<T>`, `IQueryable<T>`, and async streams.
- [Extended language mode](./concepts/extended-language-mode.md): scripting forms and expression ergonomics layered over C# syntax.

## Guides

- [Register types and extension methods](./guides/type-registration.md): assemblies, namespaces, and extension-method containers.
- [Expose functions and modules](./guides/functions-and-modules.md): host-owned APIs as global functions or named modules.
- [Choose variables and child engines](./guides/variables-context-and-child-engines.md): variable scopes, per-call values, and isolated child contexts.
- [Use Dynamic LINQ](./guides/use-dynamic-linq.md): predicates, selectors, joins, projections, plans, and provider export.
- [Deploy with NativeAOT](./guides/nativeaot-deployment.md): generated contexts, type rooting, and the AOT publish checklist.

## Reference

- [Configuration](./reference/configuration.md): `AlderOptions`, `AlderConfig`, registration entry points, precedence rules.
- [Execution model](./reference/execution-model.md): evaluation lifecycle, cache boundaries, control flow, error propagation.
- [Standard mode language support](./reference/language/standard-mode-language-support.md): the C# syntax Alder accepts in `LanguageMode.Standard`.

## Operations

- [Execution and reuse](./operations/execution-and-reuse.md): engine lifetime, parsed-expression reuse, compiled artifacts, query plans.
- [Security model](./operations/security-model.md): sandbox policy, trust and deny rules, execution limits, reflection boundary.
- [AOT and generated dispatch](./operations/aot-and-generated-dispatch.md): typed dispatch, generated contexts, reflection fallback.
- [Diagnostics and debugging](./operations/diagnostics-and-debugging.md): parse, bind, validation, compilation, export, and runtime diagnostics.

## Reading order

Readers evaluating Alder should start with [Architecture](./concepts/architecture.md) and [Execution and reuse](./operations/execution-and-reuse.md). Hosts integrating Alder should read [Configuration](./reference/configuration.md), then the guide that matches the integration target. Hosts shipping AOT or trimmed binaries should read [Deploy with NativeAOT](./guides/nativeaot-deployment.md) alongside [AOT and generated dispatch](./operations/aot-and-generated-dispatch.md). Hosts evaluating untrusted expressions should read [Security model](./operations/security-model.md) before exposing any registration surface.
