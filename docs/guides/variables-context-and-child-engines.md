---
title: Choose variables and child engines
description: Register variables, choose the right context shape, isolate per-call values, and use child engines without changing the parent surface.
---

# Choose variables and child engines

Alder has more than one way to supply values to an expression, and the choice changes real behavior. It affects the visible variable set, the static types available to binding, whether a value survives after the call, and whether work stays isolated from the parent engine. Use engine-level variables for durable shared context, typed object-backed inputs when binding precision matters, per-call values for temporary inputs, and child engines when you need inheritance plus isolation.

For the broader runtime model behind engine lifetime, parsed-expression reuse, compiled reuse, and concurrency, see [Execution and reuse](/operations/execution-and-reuse/).

## Register values on the engine

Use `SetVariable<T>` when you know the variable's intended static type and want Alder to bind against that type:

<!-- test: FluentChaining -->
```csharp
using Alder;

var engine = new AlderEngine();

engine
    .SetVariable<double>("rate", 0.05)
    .SetVariable<int>("years", 10)
    .SetVariable<double>("principal", 1000.0);

var result = engine.Evaluate<double>(
    "principal * Math.Pow(1 + rate, years)");
```

The generic overload stores the declared type from `T` together with the runtime value.

The non-generic overload is different:

```csharp
engine.SetVariable("x", 42);
```

That path stores `x` as `object` for binding purposes. The runtime value is still `42`, but the binding surface is looser.

## Choose the right value shape

Choose variables by lifetime and binding surface. Engine variables and child-engine variables persist for later evaluations; per-call values disappear after one call. Typed inputs give the binder concrete static types; object-shaped inputs keep the source flexible and defer more work to runtime dispatch.

### Typed surface

These paths preserve useful static type information for binding:

- `SetVariable<T>(...)`
- `Evaluate(..., new { ... })`
- `Evaluate(..., expression, new { ... })`

Anonymous-object properties are projected into named variables together with their property types:

```csharp
var eligible = engine.Evaluate<bool>(
    "age >= 18 && country != null",
    new { age = 25, country = "US" });
```

### Object-shaped surface

These paths expose values by name, but bind them as `object`:

- `SetVariable(string, object?)`
- `SetVariables(IDictionary<string, object?>)`
- `Evaluate(..., IDictionary<string, object?>)`
- positional variables such as `Evaluate("...", 1, 2, 3)`

For example:

<!-- test: Dictionary -->
```csharp
var vars = new Dictionary<string, object?>
{
    ["threshold"] = 100,
    ["multiplier"] = 1.5
};

var result = engine.Evaluate<double>(
    "threshold * multiplier",
    vars);
```

That works, but the binding surface is less precise than the anonymous-object form.

### Runtime-type-preserving dictionaries

Use `SetVariablesPreservingRuntimeTypes(...)` when input arrives as a dictionary but expressions need the concrete runtime types for member access, overload selection, or numeric binding:

<!-- test: SetVariablesPreservingRuntimeTypes_UsesConcreteDictionaryValueTypes -->
```csharp
public sealed record CustomerInfo(string Name);
public sealed record OrderRow(decimal Total, CustomerInfo Customer);

var order = new OrderRow(125m, new CustomerInfo("Ada"));
var inputs = new Dictionary<string, object?>
{
    ["order"] = order,
    ["minimum"] = 100m
};

var child = engine.CreateChild()
    .SetVariablesPreservingRuntimeTypes(inputs);

var accepted = child.Evaluate<bool>(
    "order.Total >= minimum && order.Customer.Name.StartsWith(\"A\")");
```

Dictionaries populated by request binding, form input, tool calls, or deserialized configuration often arrive as `IDictionary<string, object?>`. Runtime-type preservation keeps the dictionary shape while binding `order` as `OrderRow` and `minimum` as `decimal`. Null values still bind as `object` because there is no runtime type to preserve.

## Use per-call values for temporary input

Per-call values live in a child binding context created for that evaluation. They do not modify the engine's durable variable set.

<!-- test: PerCallVariables_DoNotPersist -->
```csharp
var engine = new AlderEngine();
engine.SetVariable<int>("x", 10);

var total = engine.Evaluate<int>("x + y", new { y = 20 });
```

After that call, `y` is gone. If the next evaluation must see the same value, register it on the engine or use a child engine that keeps that local state.

The same isolation rule applies to temporary dictionary values:

```csharp
var result = engine.Evaluate<long>(
    "item * multiplier",
    new Dictionary<string, object?>
    {
        ["item"] = 5L,
        ["multiplier"] = 2L
    });
```

## Use positional values for short-lived argument lists

Alder also supports positional variables:

```csharp
var sum = engine.Evaluate<int>("@0 + @1 + @2", 1, 2, 3);
```

Positional variables are convenient for short argument lists. Stable named inputs are a better fit when the expression needs a durable, type-rich binding surface.

## Use child engines for inherited state with local isolation

`CreateChild()` clones the parent's configuration and creates a child context that can see parent variables without writing back into the parent.

<!-- test: ChildEngines -->
```csharp
var parent = new AlderEngine();
parent.SetVariable<double>("baseFee", 50.0);

var tenantA = parent.CreateChild();
tenantA.SetVariable<double>("discount", 0.1);

var tenantB = parent.CreateChild();
tenantB.SetVariable<double>("discount", 0.25);

var a = tenantA.Evaluate<double>("baseFee * (1 - discount)");
var b = tenantB.Evaluate<double>("baseFee * (1 - discount)");
```

Each child sees `baseFee` from the parent, each child gets its own `discount`, and the parent remains unchanged. This is the main isolation tool when multiple evaluations should share a baseline context but not each other's local values.

## Use child engines in concurrent work

Child engines are the supported way to run many related evaluations against shared parent state. The root engine supports concurrent evaluation, but it does not provide transactional updates across shared mutable variables.

Compound parent-scope updates such as `x = x + 1` are not atomic, and evaluation does not provide snapshot isolation against concurrent writes.

The tested pattern is to keep shared inputs on the parent and put evaluation-specific values on children:

```csharp
var engine = new AlderEngine();
engine.SetVariable("taxRate", 0.08);

Parallel.ForEach(amounts, amount =>
{
    var child = engine.CreateChild();
    child.SetVariable("amount", amount);
    var tax = child.Evaluate<double>("amount * taxRate");
});
```

A variable introduced only on a child does not appear on the parent later.

## Understand the binding consequences

The binder caches work against the visible context, its type surface, and the expression text. Stable declared types are reusable; declared-type changes force rebinding because overload resolution, conversion legality, and dispatch strategy can change.

```csharp
var expression = engine.Parse("(long)x");

engine.SetVariable<int>("x", 42);
engine.Evaluate(expression);   // succeeds

engine.SetVariable<object>("x", 42);
engine.Evaluate(expression);   // fails under object unboxing rules
```

That is the practical reason to prefer `SetVariable<T>` or typed object-backed inputs when the type is known and semantically important. For the full reuse and invalidation model, see [Execution and reuse](/operations/execution-and-reuse/).

## Pick the narrowest tool that matches the job

- Use `SetVariable<T>` for durable engine state with a meaningful static type.
- Use `Evaluate(..., new { ... })` for temporary typed inputs.
- Use `Evaluate(..., IDictionary<string, object?>)` when the input is naturally object-shaped and temporary.
- Use `SetVariablesPreservingRuntimeTypes(...)` when the input arrives as a dictionary but the runtime types still matter for binding.
- Use `CreateChild()` when the evaluation needs inherited baseline state plus local isolation.
- Use positional arguments for short, one-off parameter lists.

## Common mistakes

### Using the non-generic registration path when the type matters

The non-generic path weakens binding when the static type matters:

```csharp
engine.SetVariable("x", 42);          // object-shaped binding surface
engine.SetVariable<int>("x", 42);     // typed binding surface
```

If overload selection, casts, member access, or numeric behavior depend on the static type, use the generic overload or a typed object-backed call surface.

### Expecting per-call values to persist

Per-call values are scoped to that evaluation. They do not become part of the engine's long-lived context.

### Sharing one mutable parent context across concurrent workflows

The root engine supports concurrent evaluation. It does not turn parent-scoped mutation into a synchronization primitive.

If each worker needs its own local values, create a child per worker and keep the parent read-mostly.

### Reusing a parsed expression after changing the variable type surface without expecting rebinding

Parsed-expression reuse preserves syntax. When the visible variable type changes, Alder rebinds because the semantic plan may need different overloads, conversions, member targets, or dispatch strategy.

## Practical patterns

### Durable engine state

```csharp
var engine = new AlderEngine();
engine.SetVariable<double>("interestRate", 0.05);
engine.SetVariable<int>("years", 30);

var payment = engine.Evaluate<double>(
    "principal * Math.Pow(1 + interestRate, years)",
    new { principal = 1000.0 });
```

### Temporary typed request values

```csharp
var engine = new AlderEngine();

var total = engine.Evaluate<decimal>(
    "price * quantity",
    new { price = 19.99m, quantity = 3m });
```

### Multi-tenant or per-request isolation

```csharp
var root = new AlderEngine();
root.SetVariable<double>("baseFee", 50.0);

var request = root.CreateChild();
request.SetVariable<double>("discount", 0.2);

var total = request.Evaluate<double>("baseFee * (1 - discount)");
```

## Related pages

- [Execution and reuse](/operations/execution-and-reuse/)
- [Binding system](/concepts/binding-system/)
- [Configuration](/reference/configuration/)
