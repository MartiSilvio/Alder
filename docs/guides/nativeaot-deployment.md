---
title: Deploy with NativeAOT
description: Checklist for using Alder in NativeAOT and trimming-sensitive deployments with generated dispatch metadata.
---

# Deploy with NativeAOT

NativeAOT deployment is an inventory exercise. Alder still parses, binds, validates, and interprets expressions at runtime, but the operations that depend on runtime metadata need generated dispatch coverage. The host decides which CLR types, generic shapes, delegates, modules, functions, and type-resolution paths belong in the expression-facing surface.

Use this guide as the deployment checklist. For the underlying runtime model, see [AOT and generated dispatch](../operations/aot-and-generated-dispatch.md). For type-name resolution, see [Register types and extension methods](./type-registration.md).

## Start with the interpreter

Use the interpreter for NativeAOT and trimming-sensitive deployments. Do not configure `UseCompiler()` in the AOT engine:

<!-- test: NativeAotChecklist_RegisterGeneratedContext -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Security = SecurityOptions.Trusted();
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});
```

`UseCompiler()` enables Alder's compiled backend, which lowers bound trees to `System.Linq.Expressions` and requires runtime dynamic code generation. NativeAOT deployments should keep evaluation on the interpreter and use generated dispatch metadata for reflection-sensitive runtime operations.

## Create generated contexts

A generated context is a partial class derived from `AlderTypeContext`. Add `[AlderRegistered(typeof(...))]` for each concrete CLR type expressions need to read, write, construct, or call:

<!-- test: GeneratedContext_ProvidesReflectionFreeMemberAndMethodDispatch -->
```csharp
using Alder.Aot;

namespace Rules;

public sealed class OrderRow
{
    public decimal Total { get; set; }
    public CustomerInfo Customer { get; set; } = new();

    public bool IsOpen() => Total > 0m;
}

public sealed class CustomerInfo
{
    public string Name { get; set; } = "";
}

[AlderRegistered(typeof(OrderRow))]
[AlderRegistered(typeof(CustomerInfo))]
public partial class RulesAotContext : AlderTypeContext
{
}
```

Generated dispatch is based on concrete runtime types. If an expression reaches `order.Customer.Name`, the generated context needs coverage for both `OrderRow` and `CustomerInfo`. Registering only the root object type is enough only when expressions never navigate into additional application-defined runtime types.

## Register the context

Register generated contexts during engine construction:

<!-- test: NativeAotChecklist_RegisterGeneratedContext -->
```csharp
using Alder;
using Rules;

var engine = new AlderEngine(options =>
{
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
});

engine.SetVariable("order", new OrderRow
{
    Total = 125m,
    Customer = new CustomerInfo { Name = "Ada" }
});

var accepted = engine.Evaluate<bool>(
    """order.IsOpen() && order.Customer.Name == "Ada" """);
```

JIT deployments can use generated contexts incrementally because reflection fallback remains available. NativeAOT deployments use generated dispatch as the authoritative route for operations that cannot depend on open-ended reflection.

## Separate type resolution from dispatch

Type registration and generated dispatch solve different jobs.

`Types.AddAssembly(...)` and `Types.AddNamespace(...)` let the binder resolve type names. `Aot.UseGeneratedContext(...)` supplies reflection-free runtime operations after a type is reached:

<!-- test: NativeAotChecklist_TypeResolutionAndGeneratedDispatch -->
```csharp
using Alder;
using Rules;

var engine = new AlderEngine(options =>
{
    options.Aot.UseGeneratedContext(RulesAotContext.Default);
    options.Types.AddAssembly(typeof(OrderRow).Assembly);
    options.Types.AddNamespace("Rules");
});

var value = engine.Evaluate<decimal>(
    """new OrderRow { Total = 42m }.Total""");
```

In trimmed applications, avoid treating assembly and namespace registration as the deployment inventory. They help binding. Generated contexts root the runtime members and constructors that evaluation needs.

## Root async and delegate shapes

Some AOT-sensitive operations are not ordinary member access.

Register closed `Task<T>` roots when expressions call `Task.FromResult<T>` for application-specific result types:

<!-- test: GeneratedContext_ProvidesReflectionFreeMemberAndMethodDispatch -->
```csharp
using Alder.Aot;

[AlderRegistered(typeof(Task<OrderResult>))]
public partial class AsyncRulesAotContext : AlderTypeContext
{
}
```

That roots the exact `Task.FromResult<OrderResult>` shape. Other result types need their own roots.

Closed delegate conversion also needs an explicit route when runtime generic closure is unavailable. Application-specific delegate types can be supplied by overriding `GetDelegateFactories()` on an `AlderTypeContext` with `RootedType` keys.

## Prefer explicit host APIs

Reflection-heavy registration is convenient under JIT and fragile under trimming. In NativeAOT-oriented code, prefer explicit expression-facing surfaces:

- register generated contexts for domain models, DTOs, and helper types
- register modules with `Modules.RegisterFromType<T>()` or `Modules.Register<T>(...)`
- register small global functions for narrow host operations
- avoid `Modules.RegisterFromAssembly(...)` for production AOT surfaces
- avoid relying on broad namespace imports to define what a published binary keeps

Functions and modules are often better than exposing broad application assemblies. They make host authority visible and keep the expression-facing API small enough to audit.

## Check generated-mode diagnostics

Authoritative generated mode reports missing runtime coverage with specific diagnostics:

- `ALDR0316`: member unavailable in authoritative generated mode
- `ALDR0317`: method unavailable in authoritative generated mode
- `ALDR0318`: constructor unavailable in authoritative generated mode

When one of those appears, inspect the expression's runtime path:

1. Identify the concrete runtime type reached by the expression.
2. Add `[AlderRegistered(typeof(...))]` for that type if it is missing.
3. Confirm the generated surface covers the operation shape: property, field, indexer, constructor, instance method, or static method.
4. Add closed generic, `Task<T>`, or delegate roots when the missing operation is not ordinary member dispatch.
5. Rebuild and test the published artifact.

The error can appear several hops after the root variable. A failure on `order.Customer.Name` may point to the runtime type of `Customer`, not the root order type.

## Publish and verify

Run ordinary tests under JIT, then test the published NativeAOT artifact. JIT tests can prove expression semantics and generated-context integration. The published binary proves metadata retention, dynamic-code restrictions, and generated-mode behavior under the runtime that will ship.

For this repository, the AOT matrix harness lives under `tests/Alder.AotMatrix`, and the helper script is:

```bash
./scripts/aot-matrix.sh
```

Application test suites should use the same pattern: run a representative expression corpus through the normal test host and through the published AOT binary.

## Checklist

Before shipping a NativeAOT build:

- The engine does not call `UseCompiler()`.
- Every expression-facing domain type is registered in an `AlderTypeContext`.
- Nested runtime types reached through members are registered.
- Constructed types have generated constructor coverage.
- Called methods and static members are covered by generated dispatch.
- `Task<T>`, delegate, and generic static shapes are rooted explicitly when needed.
- Assembly scanning is removed from production AOT configuration or intentionally justified.
- Security policy is configured independently from type visibility.
- Stored expressions are validated under the same engine policy used in production.
- The published NativeAOT artifact runs the representative expression corpus.

## Related pages

- [AOT and generated dispatch](../operations/aot-and-generated-dispatch.md)
- [Register types and extension methods](./type-registration.md)
- [Expose functions and modules](./functions-and-modules.md)
- [Security model](../operations/security-model.md)
- [Compiled backend](../concepts/compiled-backend.md)
- [Configuration](../reference/configuration.md)
