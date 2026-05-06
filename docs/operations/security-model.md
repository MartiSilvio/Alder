---
title: Security model
description: How Alder controls runtime authority through security policy, trust rules, and execution limits.
---

# Security model

Alder's security model is a host-controlled policy over in-process evaluation. The host decides what expressions can see, which operations they may perform, which CLR surface remains available, and how much work evaluation may consume before Alder stops it. Those rules are enforced in the shared pipeline, so the same security policy and constraint model applies across Alder's execution backends.

The boundary is in-process. Alder validates and constrains expression behavior inside the host runtime; it does not provide process isolation or make host-exposed APIs harmless. Production security depends on treating registration, security policy, execution limits, and host API design as one boundary.

## Host-controlled authority

Security begins with `AlderOptions`. `SecurityOptions` controls which categories of runtime operations are allowed. `ExecutionConstraints` bounds statement count, loop iterations, and wall-clock time. Type, namespace, module, function, and extension-method registration determines what surface is visible to an expression.

`AlderOptions` defaults to `SecurityOptions.Trusted()`. Hosts that evaluate tenant-authored, user-authored, or otherwise untrusted expressions should choose a narrower security policy explicitly and register only the APIs the expression surface needs.

Those are separate concerns. Registration makes names resolvable. The security policy decides whether operations on those names are legal. An engine can therefore expose a type or module and still reject part of its use at evaluation time.

Registration is also a host authority decision. A registered function, module, delegate, extension method, or trusted type can carry whatever side effects its CLR implementation performs. Alder can gate expression operations around that surface, but it cannot infer the business authority or side effects behind a broad host API.

## Visible surface versus allowed operations

Alder separates visibility from permission. An expression can parse and bind successfully, then still fail security policy validation before execution begins. The security policy checks concrete operation categories: method calls, property reads, static property reads, static field reads, variable assignment, property assignment, index assignment, and object construction.

This distinction matters in production systems. A host can register assemblies, modules, or functions for convenience, but registration alone does not make every reachable operation legal.

Most security policy failures surface during validation, before evaluation starts. The validation pass checks resolved calls, construction, type references, casts, member reads, assignments, and mutations. Runtime checks still matter for dynamic receiver shapes, reflection-leak guards, collection-size enforcement, and execution limits. Treat both phases as part of the security contract.

The gates are specific. `AllowAssignment` controls variable reassignment, not local declaration. `AllowPropertySet` controls member mutation. `AllowIndexSet` controls indexed writes. `AllowPropertyRead`, `AllowStaticPropertyRead`, and `AllowStaticFieldRead` are separate read controls.

`AllowMethodCalls` gates ordinary method calls. Other callable surfaces follow separate trust paths:

- registered functions are callable even when ordinary method calls are disabled
- delegate invocation is not gated by `AllowMethodCalls`
- registered module calls are not treated as ordinary method calls for this check
- extension methods are exempt from the ordinary method-call block

Each callable form remains a distinct trust boundary with its own registration and invocation path.

Treat those callable surfaces as explicit trust boundaries. Prefer narrow functions and modules that expose the operation an expression needs. Broad services make the expression-facing authority harder to reason about, even when security policy flags remove some operation categories.

## Security policy presets

`SecurityOptions` exposes three presets. `Trusted()` enables method calls, reads, assignment, property and index writes, and object construction. `Safe()` allows reads, assignment, property and index writes, but disables ordinary method calls and object construction. `Strict()` is read-oriented and enables property and static member reads, but not ordinary method calls, assignment, mutation, or construction.

You can start from a preset and override individual flags:

<!-- test: SecurityPolicyPresetOverrides_ConfigureOperationPolicy -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Security = SecurityOptions.Safe() with
    {
        AllowConstruction = true,
        AllowPropertySet = false
    };
});
```

An empty `new SecurityOptions()` is more restrictive than `Strict()`. It does not enable property reads or static member reads unless you turn them on explicitly.

Preset names describe Alder operation categories, not isolation levels. `Safe()` disables ordinary method calls and construction, but expressions still run inside the host process and can still call registered functions, delegates, modules, and extension methods that the host made visible. Those callables execute with the authority and side effects of their CLR implementations.

Use the presets as starting points, then make the expression-facing API small. A strict security policy around a broad module can still expose more authority than a permissive security policy around a purpose-built function.

## Trust and deny rules

The security policy combines broad operation flags with type-level and namespace-level allow and deny rules. `DeniedTypes` and `DeniedNamespaces` block specific CLR surfaces. `TrustedTypes` and `TrustedNamespaces` carve exceptions back in.

Trust takes precedence over broader deny lists for normal type checks. A host can deny a namespace broadly, then allow a specific type or narrower namespace back in.

Trusted types and namespaces should be small. Adding a type to the trusted surface makes its allowed members part of the expression-facing API, including any side effects reachable through those members.

<!-- test: SecurityPolicyPresetOverrides_ConfigureOperationPolicy -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Security = SecurityOptions.Safe() with
    {
        AllowConstruction = true,
        TrustedTypes = [typeof(System.Text.StringBuilder)]
    };
});
```

Alder also carries a hard-denied internal set. `AlderEngine`, `AlderOptions`, and `AlderExpression` are never allowed through the type-allowance check.

The default deny surface is intentionally broad. It covers reflection and dynamic code generation, file and process access, networking, interop, security-sensitive runtime services, configuration infrastructure, and data access. For example, `System.Reflection`, `System.IO`, `System.Diagnostics`, `System.Net`, `System.Runtime.InteropServices`, `System.Security`, and `System.Data` are denied by default. Individual runtime types such as `Environment`, `Console`, `Process`, `GC`, `Activator`, and several threading primitives are also denied by default.

## Execution limits

Security policy controls authority. `ExecutionConstraints` controls work. It exposes three limits: `MaxStatements`, `MaxLoopIterations`, and `MaxTimeout`.

Statement and timeout checks run at Alder's statement boundaries. Loop iteration checks run as loops advance. When a limit is exceeded, Alder throws `AlderExecutionLimitException`, which carries the limit type, configured limit value, observed value, executed statement count, and elapsed time.

These limits are runtime guardrails, not permission rules. An expression can be semantically valid and fully allowed by the security policy, then still fail because it exceeded its resource budget.

Execution limits are cooperative. Alder checks them at statement, loop, timeout, and cancellation checkpoints inside its own evaluation path. A long-running registered function, module method, delegate, extension method, or ordinary CLR method executes as host code; Alder can enforce limits again when control returns to the expression runtime.

Collection growth is controlled separately through `SecurityOptions.MaxCollectionSize`. Alder enforces that limit for array allocation and for collection-producing results such as arrays and `ICollection` outputs returned from evaluation. Lazy `IEnumerable` values that do not expose a count are checked through statement and loop limits when Alder enumerates them inside an expression; enumeration performed later by the host is outside Alder's evaluation loop.

## Regex timeout

`SecurityOptions` includes `RegexTimeout`, with a default of one second.

Known limitation: that timeout is part of the public security options surface, but the built-in extended regex operators `=~` and `!~` use a one-second timeout directly in their runtime implementation. The operator path does not vary its timeout with the configured `RegexTimeout` value.

If you expose `System.Text.RegularExpressions.Regex` through the normal type system, those calls are ordinary .NET regex calls. They are governed by whatever overloads the expression invokes, not by Alder's `=~` operator helper.

## Reflection boundary

Reflection is restricted in two layers. First, the default deny lists block reflection namespaces and related types during security policy validation. Second, Alder guards evaluation results, method returns, delegate returns, and member returns against reflection metadata leaks.

That second boundary matters because some metadata is inert and some is operational.

- `Type` objects are allowed
- `GetType()` is allowed when the surrounding policy permits the call path
- `Type.Name` and type comparison remain usable
- `MemberInfo` and its subtypes are blocked
- `Assembly`, `Module`, runtime handles, `MethodBody`, and `System.Reflection.Emit` surfaces are blocked

An expression can therefore compare `text.GetType() == typeof(string)` but cannot continue into metadata that would enable reflective discovery or invocation.

## What Alder security covers

Alder security covers four host-configured boundaries: runtime operation policy, CLR type and namespace visibility, execution guardrails, and reflection metadata blocking. Those boundaries apply to operations Alder parses, validates, evaluates, and values returned through Alder's runtime.

## In-process boundary

Alder security is not process isolation, operating-system isolation, or a separate isolated runtime. Expressions execute inside the host process, and Alder constrains them through validation and runtime checks.

It is also not a substitute for careful host configuration. If the host registers broad type surfaces, permissive modules, or application services, Alder evaluates within that exposed world and then applies its policy to the operations it can observe. Production deployments should treat registration, security settings, and execution limits as one security boundary.
