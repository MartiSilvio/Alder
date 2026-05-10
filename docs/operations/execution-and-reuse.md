---
title: Execution and reuse
description: How to operate Alder engines, parsed expressions, compiled delegates, and query plans efficiently in production applications.
---

# Execution and reuse

Alder is designed to be operated as a reusable runtime component. An application configures an `AlderEngine`, gives it a stable type and policy surface, and evaluates parsed expressions, compiled artifacts, and query plans many times. The performance model follows from that lifecycle: configure once, parse where code enters the system, and keep high-throughput calls on reusable artifacts. The exact evaluation lifecycle and cache semantics belong in [Execution model](../reference/execution-model.md).

The core distinction is between source text, parsed syntax, bound semantics, and executable form. `AlderExpression` preserves parsed syntax. The engine caches bound and compiled state during evaluation. Compiled wrappers and Dynamic LINQ plans expose explicit artifacts for hot paths and query composition.

| Artifact | Reuse scope | Invalidated by |
| --- | --- | --- |
| `AlderEngine` | A stable language, security policy, type, module, function, AOT, and compiler policy | Rebuild when configuration changes. |
| `AlderExpression` | Parsed syntax shared across evaluations and engines | Source text changes. |
| Bound result | One context type surface | New variables, declared-type changes, or scope clearing. |
| Cached compiled output | One parsed expression and compatible context type surface | Declared-type changes or compiler/lowering failure. |
| `AlderCompiledExpression<T>` | Explicit wrapper over one compiled shape | Parent context type-surface changes. |
| Typed delegate from `Compile<TDelegate>(...)` | Host-owned synchronous delegate with parameter types from the delegate signature | Recompile when expression text, parameter contract, or relevant engine policy changes. |
| `DynamicQueryPlan` | Prepared query fragment for operators, expression export, or delegate execution | Recreate when expression text, captured constants, source model, or query policy changes. |

## Engine lifetime

`AlderEngine` is the runtime boundary for configuration, shared variables, type metadata, module registration, security policy, execution constraints, and optional compilation. The engine captures `AlderOptions` into an immutable `AlderConfig` at construction time. Mutating the original options object after construction has no effect.

<!-- test: EngineConfiguration_CanCombinePolicyAndCompilerSettings -->
```csharp
using Alder;
using Alder.Compiled;

var engine = new AlderEngine(options =>
{
    options.LanguageMode = LanguageMode.Standard;
    options.Security = new SecurityOptions
    {
        AllowPropertyRead = true,
        AllowStaticPropertyRead = true,
        AllowStaticFieldRead = true,
        AllowMethodCalls = true
    };
    options.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxLoopIterations = 1_000,
        MaxTimeout = TimeSpan.FromSeconds(5)
    };
    options.UseCompiler();
});
```

Use a long-lived engine when the application has a stable expression policy: the same security policy, language mode, type registrations, functions, modules, and compiler setting. Recreating an engine discards context state, type metadata caches, and runtime state attached to parsed expressions.

Rebuild an engine when the policy surface changes:

- language mode
- security policy
- execution constraints
- registered modules, functions, assemblies, namespaces, or extension methods
- service provider used for module instance resolution
- AOT generated contexts
- compiled backend or expression compiler selection

Variables are different. They are runtime state, not engine configuration. Changing values normally belongs on the existing engine, a child engine, or per-call variables.

## Global, tenant, and request scopes

A single shared engine fits applications with one expression policy and mostly read-only shared state:

<!-- test: EngineConfiguration_CanCombinePolicyAndCompilerSettings -->
```csharp
public static class RuleRuntime
{
    public static readonly AlderEngine Engine = new(options =>
    {
        options.Security = SecurityOptions.Trusted();
        options.UseCompiler();
    });
}
```

Per-tenant engines fit systems where tenants have different allowed functions, modules, type visibility, or security policies:

<!-- test: EngineConfiguration_CanCombinePolicyAndCompilerSettings -->
```csharp
var tenantEngine = new AlderEngine(options =>
{
    options.Security = tenant.BuildSecurityPolicy();
    options.Modules.Register("pricing", tenant.PricingModuleType);
    options.Types.AddAssembly(tenant.ModelAssembly);
    options.UseCompiler();
});
```

Per-request engines are the least reusable default. They fit requests that change engine policy. Requests that only change values should use per-call variables or child-local variables.

`AlderEval` provides a lazily initialized global engine. Its configuration can be supplied once through `AlderEval.Configure(...)` before the first `AlderEval.GetEngine()` call. After the global engine is created, `AlderEval.Configure(...)` throws. `AlderEval.Reset()` exists for test-style lifecycle control and is not safe while evaluations are in flight.

## Parse once, evaluate many

`Parse(...)` returns an `AlderExpression`, a reusable parsed representation of source text:

<!-- test: ParsedExpressions_EvaluateManyPerCallValueSets -->
```csharp
var expression = engine.Parse("price * (1 - discount)");

var first = engine.Evaluate<double>(
    expression,
    new { price = 100.0, discount = 0.10 });

var second = engine.Evaluate<double>(
    expression,
    new { price = 250.0, discount = 0.10 });
```

This is the default pattern for stored rule sets, scheduled policies, configurable calculations, and repeated validation logic. It removes repeated lexing and parsing. On evaluation, Alder binds against the active context and reuses the bound form while that context remains valid.

An `AlderExpression` can be evaluated by more than one engine. Each engine maintains its own runtime state for that expression, so sharing the parsed object does not share one engine's variables or compiled delegate with another engine.

## Binding and invalidation

Alder's binder resolves types, members, conversions, overloads, variable access, and semantic legality. Bound reuse is conservative: the cached form is reused only for the same context instance and matching type-inference version.

The type-inference version changes when the visible declared-type surface changes:

- a new variable is defined
- an existing variable is defined with a different declared type
- a scope is cleared

Value-only changes with the same declared type do not invalidate bound or compiled reuse:

<!-- test: ValueOnlyChanges_ReuseParsedExpressionBinding -->
```csharp
var expression = engine.Parse("x + 1");

engine.SetVariable<int>("x", 1);
var first = engine.Evaluate<int>(expression);

engine.SetVariable<int>("x", 10);
var second = engine.Evaluate<int>(expression);
```

Both calls can use the same semantic shape because `x` remains an `int`. If `x` changes from `string` to `int[]`, Alder rebinds so member access and overload resolution use the new static type.

## Shared variables and per-call variables

Engine variables live in the engine context and are visible to later evaluations:

<!-- test: PerCallVariables_DoNotPersist -->
```csharp
engine.SetVariable("threshold", 80);

var passed = engine.Evaluate<bool>("score >= threshold", new { score = 92 });
```

Prefer `SetVariable("name", value)` when the variable is part of the engine's stable type surface and the value expression already has the intended compile-time type. C# normally selects Alder's generic overload and infers that type. Use explicit `SetVariable<T>(...)` when you need to force an interface, base type, `object`, or typed `null`; values that are already statically typed as `object` bind as `object`. `SetVariablesPreservingRuntimeTypes(...)` uses each value's runtime type for dynamically sourced inputs that need concrete binding.

Per-call variables are applied in a child context for that evaluation. They do not mutate the engine's shared scope:

<!-- test: TypedAnonymousInputs_PreservePerCallBindingSurface -->
```csharp
var expression = engine.Parse("item.Price >= minimum");

var visible = engine.Evaluate<bool>(
    expression,
    new { item, minimum = 50m });
```

Anonymous-object variables preserve property types for binding. Dictionary and positional variables are convenient for dynamic values, but the normal dictionary path defines them as `object` for binding. Use typed anonymous objects or explicit engine variables when precise overload resolution and member binding matter.

When input naturally arrives as a dictionary but the values still need concrete binding, stage it with runtime-type preservation before evaluation:

<!-- test: SetVariablesPreservingRuntimeTypes_UsesConcreteDictionaryValueTypes -->
```csharp
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

That path binds `order` and `minimum` against their runtime types. Null values still bind as `object`.

## Child engines

`CreateChild()` creates an engine that inherits the parent configuration and visible variables through a child context:

<!-- test: ChildEngines_IsolateConcurrentLocalValues -->
```csharp
var parent = new AlderEngine();
parent.SetVariable<int>("taxRateBasisPoints", 825);

Parallel.ForEach(orders, order =>
{
    var child = parent.CreateChild();
    child.SetVariable<OrderRow>("order", order);

    var total = child.Evaluate<decimal>(
        "order.Subtotal * (1 + taxRateBasisPoints / 10000m)");
});
```

Child-local variables do not affect the parent or sibling child engines. Disposing a child does not dispose the parent. Disposing the parent makes dependent children unusable, because their lifecycle depends on the parent engine.

Child engines fit parallel workloads with read-only shared configuration and per-worker local state. They keep shared variables visible without mutating the parent scope.

## Concurrency contract

Concurrent evaluation is supported on the root engine and on child engines. The tests cover repeated parallel evaluation of the same parsed expression, parallel child creation, per-call dictionary variables, and child-local variables across interpreted and compiled modes.

The concurrency boundary is shared mutable state. Root context storage supports concurrent lookup and slot replacement, and concurrent `SetVariable(...)` calls are not expected to corrupt the engine. That does not make compound expression-level updates atomic. An expression such as `x = x + 1` is a read-modify-write sequence, not a synchronized operation. Evaluation also does not provide snapshot isolation when parent-scoped variables are being mutated while other evaluations read them.

Operationally:

- share engines for read-mostly policy and stable variables
- use per-call variables for request-specific values
- use child engines for parallel local mutation
- use external synchronization when multiple evaluations mutate shared parent variables
- avoid changing the parent type surface while high-throughput evaluations are in flight

## Compiled reuse

When a compiler is configured, synchronous `Evaluate(...)` uses the compiled backend. For a reused `AlderExpression`, Alder stores compiled output in the expression's runtime state while the relevant context remains current.

<!-- test: CompiledExpressionWrapper_SeesValueChanges_AndRejectsTypeSurfaceChanges -->
```csharp
using Alder.Compiled;

var engine = new AlderEngine(options => options.UseCompiler());
engine.SetVariable<int>("offset", 10);

var expression = engine.Parse("value + offset");

var first = engine.Evaluate<int>(expression, new { value = 5 });
var second = engine.Evaluate<int>(expression, new { value = 20 });
```

If visible variable types change, normal `Evaluate(AlderExpression)` recompiles before using the compiled backend again. Value changes with the same declared type remain visible through the execution context.

`Compile<T>(...)` returns an `AlderCompiledExpression<T>` wrapper:

<!-- test: CompiledExpressionWrapper_SeesValueChanges_AndRejectsTypeSurfaceChanges -->
```csharp
var compiled = engine.Compile<int>("value + offset");

var result = compiled.Invoke(
    new Dictionary<string, object?> { ["value"] = 5 });
```

`AlderCompiledExpression<T>` is stricter than normal `Evaluate(AlderExpression)`. It captures the parent context type version at compile time. If the visible type surface changes later, `Invoke(...)` throws the stale-compiled-expression diagnostic. Recompile the wrapper when the type surface changes.

`Compile<TDelegate>(code, parameterNames...)` produces a native delegate whose parameter types come from the delegate signature:

<!-- test: CompileTypedDelegate_UsesDelegateSignatureAsParameterContract -->
```csharp
var rule = engine.Compile<Func<decimal, decimal, bool>>(
    "total >= minimum",
    "total",
    "minimum");
```

Use this form for hot paths where the parameter shape is known in code. It avoids per-call variable binding for those parameters and presents the host with an ordinary delegate.

## Dynamic query plan reuse

Dynamic LINQ exposes reusable query fragments through `DynamicQueryPlan`. Plans are produced by `ParsePredicate`, `ParseSelector`, and `ParseLambda`:

<!-- test: DynamicQueryPlan_ReusesPredicateForEnumerableQueryableExpressionAndDelegate -->
```csharp
var plan = engine.ParsePredicate<OrderRow>(
    "Total >= 50m && IsActive");

var inMemory = orders.WhereDynamic(plan).ToList();
var providerQuery = db.Orders.WhereDynamic(plan);
```

A plan stores the prepared lambda shape, inferred result type, captured values, and exported expression tree. It can feed Dynamic LINQ operators directly, expose an `Expression<TDelegate>` for provider-facing query assembly, or compile to a delegate for in-process use:

<!-- test: DynamicQueryPlan_ReusesPredicateForEnumerableQueryableExpressionAndDelegate -->
```csharp
Expression<Func<OrderRow, bool>> expression =
    plan.ToExpression<Func<OrderRow, bool>>();

Func<OrderRow, bool> predicate =
    plan.Compile<Func<OrderRow, bool>>();
```

Use direct expression export when the host needs a LINQ tree but does not need a reusable plan object:

<!-- test: ProviderExport_ProducesExpressionTrees_ButProviderTranslationIsSeparate -->
```csharp
Expression<Func<OrderRow, bool>> exported =
    engine.ParseAsExpression<Func<OrderRow, bool>>(
        "order => order.Total >= 50m && order.IsActive");
```

Plan reuse is the query equivalent of parsed-expression reuse. Keep stored filters as plans under the host application's invalidation policy, and feed them into `IEnumerable<T>`, `IQueryable<T>`, or custom query assembly. Captured values belong to the plan that was created; create a new plan when those values are part of the query definition and need to change.

Provider-facing reuse has a second boundary after Alder export. Alder can reuse the prepared expression tree, but the provider still owns translation, SQL generation, parameterization, query compilation, and execution strategy. When a provider rejects a query, inspect the exported expression shape separately from the original Alder source.

## Hot-path patterns

### Singleton engine

Use one engine when all callers share the same language, security policy, type registrations, modules, functions, and compiler setting:

<!-- test: EngineConfiguration_CanCombinePolicyAndCompilerSettings -->
```csharp
var engine = new AlderEngine(options =>
{
    options.Security = SecurityOptions.Trusted();
    options.Types.AddAssembly(typeof(OrderRow).Assembly);
    options.UseCompiler();
});
```

Keep shared variables stable or read-only. Pass request values as typed per-call variables.

### Per-tenant engine

Use one engine per tenant when tenant policy affects visible types, modules, functions, or security policy rules. Cache the tenant engine for the tenant policy lifetime, and rebuild it when that policy changes.

### Cached stored rule set

Parse stored rules when they are loaded or updated:

<!-- test: ParsedExpressions_EvaluateManyPerCallValueSets -->
```csharp
var parsedRules = storedRules.ToDictionary(
    rule => rule.Id,
    rule => engine.Parse(rule.Expression));

bool EvaluateRule(string id, OrderRow order)
{
    return engine.Evaluate<bool>(
        parsedRules[id],
        new { order });
}
```

This keeps source text out of the request hot path and lets Alder reuse semantic and compiled state after the first evaluation.

### High-throughput delegate path

When the host knows the parameter shape, compile a delegate and call it directly:

<!-- test: CompileTypedDelegate_UsesDelegateSignatureAsParameterContract -->
```csharp
var isVisible = engine.Compile<Func<decimal, bool>>(
    "total >= 50m",
    "total");

foreach (var order in orders)
{
    if (isVisible(order.Total))
        Export(order);
}
```

This is the most direct in-process path for repeated synchronous work. Use expression export or Dynamic LINQ plans when the next stage must remain in provider space.

## Practical boundary

Run Alder as a long-lived configured runtime when policy is stable. Parse expressions at ingestion boundaries. Use compiled wrappers or typed delegates for synchronous hot paths, and `DynamicQueryPlan` for reusable query fragments. Keep request values out of shared parent mutation when concurrency matters, and rebuild the engine only when configuration changes.

## Related pages

- [Execution model](../reference/execution-model.md)
- [Compiled backend](../concepts/compiled-backend.md)
- [Async execution](../concepts/async-execution.md)
- [Dynamic LINQ](../concepts/dynamic-linq.md)
- [Configuration](../reference/configuration.md)
