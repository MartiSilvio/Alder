---
title: Configuration
description: Reference for Alder engine configuration, option builders, AOT context registration, and precedence rules.
---

# Configuration

`AlderOptions` is the mutable configuration surface used before engine construction. `AlderEngine` captures those options as an immutable runtime model: language mode, sandbox policy, execution limits, registered modules and functions, type-resolution metadata, compiler settings, service-provider integration, and AOT dispatch metadata.

## Construction

`AlderEngine` accepts configuration through:

- `AlderEngine()`
- `AlderEngine(Action<AlderOptions> configure)`
- `AlderEngine(AlderOptions options)`

Configuration is materialized once, at engine construction time. Mutating the original `AlderOptions` instance afterward has no effect on the engine's runtime model.

## AlderOptions

`AlderOptions` exposes these top-level settings:

- `IsCaseSensitive`: controls name matching for registered functions, modules, and relevant runtime member lookup.
- `LanguageMode`: selects the accepted language surface, such as `Standard` or `Extended`.
- `Sandbox`: defines the security policy used during validation and execution.
- `Constraints`: sets runtime limits such as statement count, loop iterations, and timeout.
- `ExpressionCompiler`: selects the delegate compiler used by compiled execution.
- `ServiceProvider`: supplies module instances through dependency injection.
- `Modules`: registers named module surfaces.
- `Functions`: registers delegate-based global functions.
- `Types`: registers assemblies, namespaces, and extension-method containers for type and method resolution.
- `Aot`: registers generated type contexts used by typed dispatch.

## Compiler configuration

Compiled execution is enabled by calling `UseCompiler()` from the `Alder.Compiled` namespace during engine configuration:

```csharp
using Alder.Compiled;

var engine = new AlderEngine(options => options.UseCompiler());
```

That call installs Alder's compiled provider for synchronous `Evaluate(...)` and typed delegate compilation. It requires runtime dynamic-code support. NativeAOT, IL2CPP-style, and other dynamic-code-restricted deployments should use interpreted evaluation with generated dispatch metadata.

The `Alder.Compiled` namespace also exposes Dynamic LINQ and expression-tree export APIs. String-based Dynamic LINQ operators use compiled delegates for in-process sequence execution. `IQueryable<T>` operators export expression trees and call the matching `Queryable` operators; provider translation remains downstream. Direct `ParseAsExpression<TDelegate>(...)` export prepares LINQ expression trees without calling `UseCompiler()`, although compiling those trees to delegates still requires dynamic code support.

`ExpressionCompiler` controls only the final expression-tree-to-delegate compiler used after Alder has parsed, bound, validated, optimized, and lowered the expression. Setting an `ExpressionCompiler` without calling `UseCompiler()` does not make asynchronous evaluation, tracing, or AOT execution use compiled delegates.

## AlderConfig

`AlderConfig` contains the runtime form of that configuration:

- language and casing rules
- security policy
- execution constraints
- compiler settings
- service provider integration
- function and module registries
- type-resolution metadata
- extension-method registries
- AOT dispatch metadata

`AlderConfig` is shared by contexts created from the same engine instance.

## Modules

Module registration entry points:

- `Modules.Register<T>(moduleName, explicitOnly = false, instance = default)`
- `Modules.Register(moduleName, Type, explicitOnly = false, instance = null)`
- `Modules.Register(moduleName, Type, IReadOnlyDictionary<string, IReadOnlyCollection<MemberInfo>> members)`
- `Modules.RegisterFromType(Type|T, instance = null)`
- `Modules.RegisterFromAssembly(Assembly)`

### Naming

- `Register("name", ...)` uses the supplied name.
- `RegisterFromType` uses `[AlderModule("name")]` when present.
- If `RegisterFromType` is used on a type without `[AlderModule]`, methods marked with `[AlderFunction]` are registered as global functions.

### Exposure rules

- standard module registration exposes the default public module surface
- `explicitOnly = true` exposes only methods marked with `[AlderFunction]`
- the explicit member-map overload exposes only the members provided in that map

### Instance resolution

When an expression needs an instance member, Alder resolves the module instance in this order:

1. Use the instance supplied at registration time.
2. Resolve the module type from `IServiceProvider`.
3. Construct the module through a public parameterless constructor.
4. Fail if none of those paths succeeds.

Resolution occurs per access. Alder does not implicitly cache instances created through the service provider or parameterless-constructor path.

## Functions

Function registration entry point:

- `Functions.Register(string name, Func<object?[], object?> function)`

### Behavior

- function names are stored in a comparer derived from `IsCaseSensitive`
- registering the same name again overwrites the previous function
- methods discovered through `[AlderFunction]` merge into the same registry

### Delegate argument model

- expression arguments are evaluated before the delegate is called
- the delegate receives `object?[]`
- Alder does not perform automatic conversion on this surface
- validation and conversion are the delegate author's responsibility

## Types

Type registration entry points:

- `Types.AddAssembly(Assembly)`
- `Types.AddNamespace(string namespaceName)`
- `Types.AddExtensionMethods(Type|T)`

### Runtime effect

- registered assemblies and namespaces extend the type-resolution surface
- registered extension-method containers participate in method resolution
- duplicate extension-method registrations are ignored
- `Enumerable` is included by default

## AOT

AOT entry points:

- `Aot.UseGeneratedContext(AlderTypeContext context)`
- `Aot.ClearBuiltInContext()`

### Merge behavior

- Alder starts with its built-in generated context unless it is cleared
- additional contexts are merged in registration order
- later contexts override earlier typed-dispatch entries for the same runtime type
- delegate factory collisions are resolved by last registration wins
- closed delegate roots are merged by set union

`ClearBuiltInContext()` removes the built-in context and clears any previously queued additional contexts.

## Service provider

`AlderOptions.ServiceProvider` is used for module instance resolution. It is not a general-purpose expression dependency injection facility. Its role is to let module-backed expressions obtain instance targets from the host application's container.

Child contexts inherit the same service provider.

## Case sensitivity

- `IsCaseSensitive = true` requires exact matching
- `IsCaseSensitive = false` uses case-insensitive matching for registered names and relevant runtime lookups

This affects both runtime lookup and collision behavior at registration time.

## Precedence rules

- function collisions: last registration wins
- module collisions: last registration wins
- attribute-discovered global function collisions: last registration wins
- typed-dispatch collisions by runtime type: last merged context wins
- delegate factory collisions by delegate type: last merged context wins
- generic static dispatch metadata accumulates in registration order

## Runtime consequences

- parsing and validation use `LanguageMode`
- sandbox validation uses `Sandbox`
- runtime limits use `Constraints`
- type lookup uses the registered assemblies and namespaces
- extension method binding uses registered extension-method containers
- typed dispatch uses registered AOT contexts
- synchronous execution uses compiled evaluation only when a compiler is configured
- asynchronous execution uses the interpreter

## Guarantees

- engine configuration is fixed at construction time
- all contexts created by an engine share the same runtime snapshot
- module instance resolution order is deterministic
- AOT merge order is deterministic
- typed-dispatch lookup begins with exact runtime type matches; broader fallback behavior is handled by Alder's runtime dispatch layer

## Related pages

- [Architecture](/concepts/architecture/)
- [Register types and extension methods](/guides/type-registration/)
- [Deploy with NativeAOT](/guides/nativeaot-deployment/)
- [AOT and generated dispatch](/operations/aot-and-generated-dispatch/)
- [Execution model](/reference/execution-model/)
