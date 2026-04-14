---
title: Configuration
description: Reference for Alder engine configuration, option builders, runtime config materialization, registration surfaces, AOT context integration, and precedence rules.
---

# Configuration

## Purpose
This page defines Alder configuration surfaces and runtime configuration behavior for `AlderEngine`, `AlderOptions`, and `AlderConfig`.

## Entry points
`AlderEngine` accepts configuration through:

- `AlderEngine()`
- `AlderEngine(Action<AlderOptions> configure)`
- `AlderEngine(AlderOptions options)`

`AlderOptions` is the mutable configuration object used before engine construction.

## Configuration lifecycle
Configuration materialization sequence:

1. Caller mutates `AlderOptions` and nested builders (`Modules`, `Functions`, `Types`, `Aot`).
2. `AlderEngine` constructor calls `BuildConfig(options)`.
3. `BuildConfig` copies option state into runtime structures (`AlderConfig`) and merges AOT contexts.
4. `AlderEngine` stores one `_config` instance.
5. Runtime contexts (`AlderContext`) are created from `_config` and read configuration from that immutable snapshot.

Mutation boundary:

- `AlderOptions` is mutable before engine construction.
- `AlderConfig` is immutable after construction.
- Mutating the original `AlderOptions` after `AlderEngine` construction does not update `_config`.

## AlderOptions
`AlderOptions` defines:

- `IsCaseSensitive`: controls string comparers used for registered names and runtime string comparisons.
- `LanguageMode`: parser/validation language surface (`Standard` or `Extended`).
- `Sandbox`: converted to runtime `SecurityPolicy`.
- `Constraints`: execution limits (statement, loop, timeout).
- `ExpressionCompiler`: delegate compiler used by compiled backend.
- `ServiceProvider`: optional `IServiceProvider` for runtime module resolution.
- `Modules`: module/global-function registration builder.
- `Functions`: delegate function registration builder.
- `Types`: type-resolution and extension-method registration builder.
- `Aot`: generated context registration builder.

Internal `Compiler` is configured by compiled-backend extension methods and controls whether compiled execution is available.

## AlderConfig
`AlderConfig` stores the runtime snapshot consumed by parsing, binding, evaluation, and compilation.

Key fields:

- Language/security/casing: `LanguageMode`, `Security`, `IsCaseSensitive`, `Comparer`, `StringComparison`.
- Execution settings: `Constraints`, `Compiler`, `ExpressionCompiler`.
- Integration settings: `ServiceProvider`.
- Registrations: `Functions`, `Modules`, `ExtensionTypes`.
- Binding/runtime metadata: `TypeMetadata`, `TypeResolver`.
- AOT dispatch metadata: `TypeDispatch`, `DelegateFactories`, `ExtensionDispatches`.
- Compiled-runtime routing flag: `PreferResolvedRuntimeDispatch`.

`AlderConfig.TryGetDispatch(Type, out TypedDispatch)` resolves typed dispatch by runtime type, then base types up to but excluding `object`.

## Module registration
Module registration entry points:

- `Modules.Register<T>(moduleName, explicitOnly = false, instance = default)`
- `Modules.Register(moduleName, Type, explicitOnly = false, instance = null)`
- `Modules.Register(moduleName, Type, IReadOnlyDictionary<string, MemberInfo> members)`
- `Modules.RegisterFromType(Type|T, instance = null)`
- `Modules.RegisterFromAssembly(Assembly)`

Registration model:

- Each module registration produces one `RegisteredType`.
- `BuildConfig` converts module registrations into `ModuleInfo` entries keyed by module name.
- For `RegisterFromType*`, module name is resolved from `[AlderModule(Name=...)]`.
- If no module name is present, `[AlderFunction]` methods on that type are registered as global functions.

Member exposure rules:

- Standard registration uses `ModuleMemberMetadata.Build(...)`.
- `explicitOnly = true` exposes only methods marked with `[AlderFunction]`.
- Explicit member-map overload exposes only provided `MemberInfo` entries.
- Engine uses provided explicit member map as-is; comparer behavior depends on the supplied dictionary implementation.

Module instance resolution at runtime (`ModuleInfo.Resolve`):

1. Use pre-registered instance when non-null.
2. Else call `IServiceProvider.GetService(moduleType)` when provider is configured.
3. Else create instance through public parameterless constructor.
4. Else throw `CannotResolveModuleInstance`.

Resolution executes per runtime access; module instances are not cached by `ModuleInfo` when created through step 2 or step 3.

## Function registration
Function registration entry point:

- `Functions.Register(string name, Func<object?[], object?> function)`

Function model:

- Registered functions are stored by name in a comparer-aware dictionary.
- Function registration by the same name overwrites the previous delegate.
- Attribute-derived global functions (`[AlderFunction]` from `RegisterFromType*` and `RegisterFromAssembly`) are merged into the same function map.
- Name collisions between delegate functions and attribute-derived functions resolve by last assignment during `BuildConfig`.

## Type registration
Type registration entry points:

- `Types.AddAssembly(Assembly)`
- `Types.AddNamespace(string namespaceName)`
- `Types.AddExtensionMethods(Type|T)`

Runtime usage:

- `BuildConfig` creates `TypeResolver` from registered assemblies and namespaces with implicit BCL imports enabled.
- `ExtensionTypes` is copied into `AlderConfig` and consumed by runtime extension-method dispatch.

Ordering and uniqueness:

- Assemblies and namespaces preserve insertion order.
- `AddExtensionMethods` inserts new extension types at the front when not already present.
- `Enumerable` is present by default.
- Duplicate extension type inserts are ignored.

## AOT configuration
AOT entry points:

- `Aot.UseGeneratedContext(AlderTypeContext context)`
- `Aot.ClearBuiltInContext()`

AOT builder state:

- `BuiltInContext` defaults to `AlderBuiltInContext.Default`.
- `AdditionalContexts` stores user contexts in insertion order.

`ClearBuiltInContext()` behavior:

- sets `BuiltInContext` to `null`
- clears `AdditionalContexts`

AOT merge in `BuildConfig`:

1. Merge built-in context metadata when `BuiltInContext` is non-null.
2. Merge each additional context in insertion order.
3. Store merged results into `AlderConfig.TypeDispatch`, `AlderConfig.DelegateFactories`, and `AlderConfig.ExtensionDispatches`.

Merge rules:

- `TypeDispatch`: keyed by runtime `Type`; later context entries overwrite earlier entries.
- `DelegateFactories`: keyed by delegate `Type`; later context entries overwrite earlier entries.
- `ExtensionDispatches`: concatenated in merge order.

`PreferResolvedRuntimeDispatch` is `true` when dynamic code is unavailable or when at least one additional generated context is registered.

## Service provider integration
`IServiceProvider` integration points:

- `AlderOptions.ServiceProvider` is copied into `AlderConfig.ServiceProvider`.
- Root `AlderContext` is created with `_config.ServiceProvider`.
- Child contexts inherit service provider from parent when one is not explicitly supplied.
- Module instance resolution uses provider through `ModuleInfo.Resolve`.

The service provider is not used for arbitrary expression-level dependency injection; the runtime integration path is module instance resolution.

## Configuration merge and precedence rules
Precedence rules:

- Function name collisions: last registration wins.
- Module name collisions: last registration wins.
- Global function name collisions from attribute registrations: last assignment wins.
- AOT typed dispatch collisions by type: last merged context wins.
- AOT delegate factory collisions by delegate type: last merged context wins.
- AOT extension dispatches: merged by append; no overwrite phase.

Registration-order rules:

- `UseGeneratedContext` merge order is registration order.
- Built-in context is merged before additional contexts.
- `ClearBuiltInContext` removes built-in and all previously queued additional contexts.

Case-sensitivity rules:

- `IsCaseSensitive` selects option-level string comparer.
- Function/module dictionaries use that comparer.
- Type and namespace resolution comparers use that comparer.

## Runtime effects of configuration
Configuration effects by subsystem:

- Parsing/validation: `LanguageMode` controls accepted syntax surface.
- Security and limits: `Sandbox` is converted to `SecurityPolicy` used by validation/runtime checks.
- Security and limits: `Constraints` is applied to per-evaluation `ExecutionConstraintState` resets.
- Binding/type resolution: `TypeResolver` uses registered assemblies and namespaces.
- Binding/type resolution: `ExtensionTypes` contributes extension-method lookup.
- Dispatch/runtime invocation: `TypeDispatch` enables typed-dispatch lookup before reflection fallback.
- Dispatch/runtime invocation: `DelegateFactories` and `ExtensionDispatches` are consumed by runtime AOT helper paths.
- Dispatch/runtime invocation: `IsCaseSensitive` controls runtime name matching behavior.
- Backend selection: synchronous `Evaluate` uses compiled path only when `Compiler` is non-null.
- Backend selection: asynchronous evaluation runs interpreter path.
- Backend selection: `TryCompile` and `Compile` require a configured compiler.
- Compiled emission behavior: `PreferResolvedRuntimeDispatch` switches resolved member/call emission to runtime helper routing.

## Constraints and guarantees
- Engine configuration is captured at construction and remains stable for that engine instance.
- `AlderConfig` is shared across contexts created by the same engine.
- `AlderConfig` dictionaries are fixed snapshots created during `BuildConfig`.
- Module instance resolution order is deterministic: registered instance, service provider, parameterless constructor, failure.
- AOT merge order is deterministic: built-in context first, additional contexts in registration order.
- Dispatch lookup through `TryGetDispatch` traverses class inheritance only; interface traversal is not part of this lookup.

## Related pages
- [Execution model](/reference/execution-model/)
- [Architecture](/explanation/architecture/)
- [Binding system](/explanation/binding-system/)
- [Typed dispatch and AOT](/explanation/typed-dispatch/)
