---
title: Security model
description: How Alder controls runtime authority through sandbox policy, trust rules, and execution limits.
---

# Security model

Alder's security model is a host-controlled policy over in-process evaluation. The host decides what expressions can see, which operations they may perform, which CLR surface remains available, and how much work evaluation may consume before Alder stops it. Those rules are enforced in the shared pipeline, so the same sandbox and constraint model applies across Alder's execution backends.

## Host-controlled authority

Security begins with `AlderOptions`. `SandboxOptions` controls which categories of runtime operations are allowed. `ExecutionConstraints` bounds statement count, loop iterations, and wall-clock time. Type, namespace, module, and function registration determines what surface is visible to an expression.

Those are separate concerns. Registration makes names resolvable. The sandbox decides whether operations on those names are legal. An engine can therefore expose a type or module and still reject part of its use at evaluation time.

## Visible surface versus allowed operations

Alder does not treat visibility as permission. An expression can parse and bind successfully, then still fail sandbox validation before execution begins. The sandbox checks concrete operation categories: method calls, property reads, static property reads, static field reads, variable assignment, property assignment, index assignment, and object construction.

This distinction matters in production systems. A host can register assemblies, modules, or functions for convenience, but registration alone does not make every reachable operation legal.

The gates are intentionally specific. `AllowAssignment` controls variable reassignment, not local declaration. `AllowPropertySet` controls member mutation. `AllowIndexSet` controls indexed writes. `AllowPropertyRead`, `AllowStaticPropertyRead`, and `AllowStaticFieldRead` are separate read controls.

`AllowMethodCalls` is narrower than its name may suggest. It blocks ordinary method calls, but Alder treats some callable surfaces differently:

- registered functions are callable even when ordinary method calls are disabled
- delegate invocation is not gated by `AllowMethodCalls`
- registered module calls are not treated as ordinary method calls for this check
- extension methods are exempt from the ordinary method-call block

That split is deliberate. Alder does not flatten every callable form into one permission bit.

## Sandbox presets

`SandboxOptions` exposes three presets. `Trusted()` enables method calls, reads, assignment, property and index writes, and object construction. `Safe()` allows reads, assignment, property and index writes, but disables ordinary method calls and object construction. `Strict()` is read-oriented and enables property and static member reads, but not ordinary method calls, assignment, mutation, or construction.

You can start from a preset and override individual flags:

```csharp
var engine = new AlderEngine(options =>
{
    options.Sandbox = SandboxOptions.Safe() with
    {
        AllowConstruction = true,
        AllowPropertySet = false
    };
});
```

An empty `new SandboxOptions()` is more restrictive than `Strict()`. It does not enable property reads or static member reads unless you turn them on explicitly.

## Trust and deny rules

The sandbox combines broad operation flags with type-level and namespace-level allow and deny rules. `DeniedTypes` and `DeniedNamespaces` block specific CLR surfaces. `TrustedTypes` and `TrustedNamespaces` carve exceptions back in.

Trust takes precedence over the broader deny lists for normal type checks. A host can deny a namespace broadly, then allow a specific type or narrower namespace back in.

```csharp
var engine = new AlderEngine(options =>
{
    options.Sandbox = SandboxOptions.Safe() with
    {
        AllowConstruction = true,
        TrustedTypes = [typeof(System.Text.StringBuilder)]
    };
});
```

Alder also carries a hard-denied internal set. `AlderEngine`, `AlderOptions`, and `AlderExpression` are never allowed through the type-allowance check.

The default deny surface is intentionally broad. It covers reflection and dynamic code generation, file and process access, networking, interop, security-sensitive runtime services, configuration infrastructure, and data access. For example, `System.Reflection`, `System.IO`, `System.Diagnostics`, `System.Net`, `System.Runtime.InteropServices`, `System.Security`, and `System.Data` are denied by default. Individual runtime types such as `Environment`, `Console`, `Process`, `GC`, `Activator`, and several threading primitives are also denied by default.

## Execution limits

Sandbox policy controls authority. `ExecutionConstraints` controls work. It exposes three limits: `MaxStatements`, `MaxLoopIterations`, and `MaxTimeout`.

Statement and timeout checks run at Alder's statement boundaries. Loop iteration checks run as loops advance. When a limit is exceeded, Alder throws `AlderExecutionLimitException`, which carries the limit type, configured limit value, observed value, executed statement count, and elapsed time.

These limits are runtime guardrails, not permission rules. An expression can be semantically valid and fully allowed by the sandbox, then still fail because it exceeded its resource budget.

Collection growth is controlled separately through `SandboxOptions.MaxCollectionSize`. Alder enforces that limit for array allocation and for collection-producing results such as arrays and `ICollection` outputs returned from evaluation.

## Regex timeout

`SandboxOptions` includes `RegexTimeout`, with a default of one second.

That timeout is part of the public sandbox surface, but the built-in extended regex operators `=~` and `!~` use a one-second timeout directly in their runtime implementation. In practice, the current operator path does not vary its timeout with the configured `RegexTimeout` value.

If you expose `System.Text.RegularExpressions.Regex` through the normal type system, those calls are ordinary .NET regex calls. They are governed by whatever overloads the expression invokes, not by Alder's `=~` operator helper.

## Reflection boundary

Reflection is restricted in two layers. First, the default deny lists block reflection namespaces and related types during sandbox validation. Second, Alder guards evaluation results and member returns against reflection metadata leaks.

That second boundary matters because some metadata is inert and some is operational.

- `Type` objects are allowed
- `GetType()` is allowed when the surrounding policy permits the call path
- `Type.Name` and type comparison remain usable
- `MemberInfo` and its subtypes are blocked
- `Assembly`, `Module`, runtime handles, `MethodBody`, and `System.Reflection.Emit` surfaces are blocked

An expression can therefore compare `text.GetType() == typeof(string)` but cannot continue into metadata that would enable reflective discovery or invocation.

## What Alder security is

Alder security is a host-configured policy over runtime operations, a type and namespace filter over the CLR surface visible to evaluation, a set of execution guardrails that can stop runaway work, and a reflection boundary that blocks metadata objects which would widen authority at runtime.

## What Alder security is not

Alder security is not process isolation, operating-system isolation, or a separate sandboxed runtime. Expressions execute inside the host process, and Alder constrains them through validation and runtime checks.

It is also not a substitute for careful host configuration. If the host registers broad type surfaces, permissive modules, or application services, Alder evaluates within that exposed world and then applies its policy to the operations it can observe. Production deployments should treat registration, sandbox settings, and execution limits as one security boundary, not as unrelated options.
