---
title: Register types and extension methods
description: Configure Alder type resolution with assemblies, namespaces, extension methods, and deployment-aware alternatives.
---

# Register types and extension methods

Type registration defines the CLR type surface available to Alder's binder. It lets expressions name application types, construct public objects, call static members, and bind extension methods against host models. Use it when expressions need C# type resolution. Use functions or modules when the expression-facing API should be smaller than the underlying CLR surface.

Exact option behavior is covered in [Configuration](../reference/configuration.md). For method-call exposure through host-owned APIs, use [Expose functions and modules](./functions-and-modules.md).

## Choose the right exposure surface

The registration shape determines what the binder can see:

| Surface | Use when | Expression shape |
| --- | --- | --- |
| Assembly registration | Expressions need to resolve types from an assembly by full name or imported namespace. | `Acme.Rules.Money.FromDollars(125m)` |
| Namespace registration | Expressions should use unqualified type names from a registered assembly. | `Money.FromDollars(125m)` |
| Extension-method registration | Existing model types need additional instance-style methods. | `money.IsHighValue(100m)` |
| Function registration | The host wants one global operation with custom argument handling. | `isHighValue(money, 100m)` |
| Module registration | The host wants a named, curated API surface. | `pricing.IsHighValue(money, 100m)` |

Type registration expands C# resolution. Function and module registration expose host-owned operations. That distinction matters for security, AOT, trimming, and long-term API design.

The examples use this host model:

```csharp
namespace Billing;

public sealed record Money(decimal Amount)
{
    public static Money FromDollars(decimal amount) => new(amount);
}

public static class MoneyExtensions
{
    public static bool IsHighValue(this Money money, decimal threshold) =>
        money.Amount >= threshold;
}
```

## Register an assembly

`Types.AddAssembly(...)` adds an assembly to Alder's type-resolution search set. After registration, expressions can resolve public types from that assembly by fully qualified name:

<!-- test: TypeRegistration_AddAssembly_ResolvesQualifiedType -->
```csharp
using Alder;

var engine = new AlderEngine(options =>
{
    options.Types.AddAssembly(typeof(Money).Assembly);
});

var value = engine.Evaluate<decimal>(
    "Billing.Money.FromDollars(125m).Amount");
```

Assembly registration is the broadest type-resolution switch. It makes the assembly searchable; it does not automatically allow every operation on every type. Sandbox policy still validates construction, static access, method calls, property reads, assignment, and mutation before evaluation proceeds.

Use assembly registration for stable model or helper assemblies where expression authors genuinely need type names. For small host operations, prefer functions or modules.

## Import a namespace

`Types.AddNamespace(...)` imports a namespace for unqualified type resolution. The namespace works against types discovered from registered assemblies:

<!-- test: TypeRegistration_AddNamespace_ResolvesUnqualifiedType -->
```csharp
using Alder;

var engine = new AlderEngine(options =>
{
    options.Types.AddAssembly(typeof(Money).Assembly);
    options.Types.AddNamespace("Billing");
});

var value = engine.Evaluate<decimal>(
    "Money.FromDollars(125m).Amount");
```

Namespace registration improves expression readability, but it also makes collisions easier to create. Keep imported namespaces small and application-specific when expressions are user-authored or stored long term. Fully qualified names remain useful for rare or ambiguous types.

Alder includes implicit imports for common BCL namespaces such as `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`, `System.Text`, `System.Text.RegularExpressions`, `System.Text.Json`, `System.Numerics`, and `System.Globalization`. Application namespaces still need explicit registration.

## Register extension methods

`Types.AddExtensionMethods(...)` adds a static extension-method container to method resolution:

<!-- test: TypeRegistration_AddExtensionMethods_ResolvesExtensionMethod -->
```csharp
using Alder;

var engine = new AlderEngine(options =>
{
    options.Types.AddExtensionMethods(typeof(MoneyExtensions));
});

var accepted = engine.Evaluate<bool>(
    "money.IsHighValue(100m)",
    new { money = new Money(125m) });
```

`System.Linq.Enumerable` extension methods are included by default, which is why ordinary LINQ-shaped expressions such as `items.Where(x => x > 2)` work without custom registration. Custom extension containers are searched during binding and participate in overload resolution like other candidate methods.

Extension methods are callable host code. Register containers deliberately, especially when they include I/O, mutation, reflection, ambient state, or provider-specific behavior. For untrusted expressions, a small module or function can be easier to audit than a broad extension-method class.

## Prefer functions for narrow operations

Use a registered function when the expression should see one global operation and the host wants explicit control over argument conversion:

```csharp
var engine = new AlderEngine(options =>
{
    options.Functions.Register("isHighValue", args =>
    {
        var money = (Money)args[0]!;
        var threshold = Convert.ToDecimal(args[1]);
        return money.Amount >= threshold;
    });
});

var accepted = engine.Evaluate<bool>(
    "isHighValue(money, 100m)",
    new { money = new Money(125m) });
```

The delegate receives evaluated arguments as `object?[]`. That makes functions useful for host-defined conversion, validation, logging, or policy checks. It also means the function author owns argument handling.

## Prefer modules for grouped APIs

Use a module when a set of operations should live behind a named expression-facing owner:

```csharp
public sealed class PricingRules
{
    public bool IsHighValue(Money money, decimal threshold) =>
        money.Amount >= threshold;
}

var engine = new AlderEngine(options =>
{
    options.Modules.Register<PricingRules>("pricing");
});

var accepted = engine.Evaluate<bool>(
    "pricing.IsHighValue(money, 100m)",
    new { money = new Money(125m) });
```

Modules keep the expression surface organized and can use explicit-only registration for tighter exposure. They are often the better fit for tenant-authored rules, product policy, and host services because the expression-facing name signals ownership.

## Security boundaries

Registration controls visibility. Sandbox policy controls authority.

An assembly or namespace can make a type name resolvable while the sandbox still rejects construction, static access, method calls, property reads, or mutation. A function, module, or extension method can also carry side effects that Alder cannot infer from its signature. Treat every registered surface as part of the host's expression-facing API.

For user-authored expressions:

- register only the assemblies, namespaces, and extension containers the expression surface needs
- prefer small functions or explicit-only modules for business operations
- use `SandboxOptions.Safe()` or a stricter custom policy
- deny broad namespaces or types when application assemblies expose mixed-trust APIs
- validate expressions under the same engine policy used for execution

## Trimming and AOT consequences

Assembly and namespace registration are reflection-oriented type-resolution tools. They fit normal JIT deployments and development workflows well. In trimmed, NativeAOT, and IL2CPP-style deployments, broad discovery can lose metadata or fail to represent the runtime surface expressions need.

AOT-oriented applications should keep the expression surface explicit:

- register generated contexts for domain types reached by expressions
- prefer exact function, module, and generated-context registration over assembly-wide scanning
- avoid relying on namespace imports as the deployment inventory
- root closed generic, delegate, and `Task<T>` shapes that expressions need
- keep `UseCompiler()` out of NativeAOT deployments
- test the published binary; passing JIT tests do not prove published behavior

Type registration and generated dispatch solve different parts of the problem. `Types.AddAssembly(...)` and `Types.AddNamespace(...)` help the binder resolve type names. `Aot.UseGeneratedContext(...)` gives the runtime reflection-free dispatch metadata after a type is reached. In AOT-sensitive applications, use both only when both jobs are needed.

## Troubleshooting

- Type name not found: add the containing assembly, use the fully qualified name, or add the namespace for unqualified lookup.
- Wrong type selected: remove broad namespace imports or use a fully qualified type name.
- Extension method not found: register the static extension-method container with `Types.AddExtensionMethods(...)`.
- Extension overload mismatch: check the receiver type and argument types visible to binding.
- Sandbox failure after successful binding: adjust sandbox policy, or replace broad type access with a narrower function or module.
- Works under JIT but fails after publish: add generated contexts for reached runtime types and avoid assembly scanning as the only deployment inventory.

## Related pages

- [Expose functions and modules](./functions-and-modules.md)
- [Deploy with NativeAOT](./nativeaot-deployment.md)
- [Variables, context, and child engines](./variables-context-and-child-engines.md)
- [Binding system](../concepts/binding-system.md)
- [Security model](../operations/security-model.md)
- [AOT and generated dispatch](../operations/aot-and-generated-dispatch.md)
- [Configuration](../reference/configuration.md)
