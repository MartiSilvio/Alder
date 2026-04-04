Variables connect your application to the expressions it evaluates. The injection method determines what the binder knows at semantic analysis time, which affects resolution strategy, performance, AOT compatibility, and diagnostic precision.

## Injection Patterns

| Pattern | Scope | Binder knows type | Performance | Best for |
|---------|-------|-------------------|-------------|----------|
| `SetVariable<T>` | Persistent | Yes: `typeof(T)` | Best: bind-time resolution | Server apps, reused engines |
| `SetVariable` (untyped) | Persistent | No: `typeof(object)` | Slower: runtime reflection | Dynamic values, unknown types |
| Anonymous object | Single `Evaluate` call | Via reflection per call | Moderate | Quick one-off evaluations |
| `IDictionary<string, object?>` | Single `Evaluate` call | No: `typeof(object)` | Slower: runtime reflection | Dynamic keys from config/user input |

### `SetVariable<T>`: typed, persistent

```csharp
var engine = new AlderEngine();
engine.SetVariable<List<int>>("scores", new List<int> { 88, 92, 76, 95, 61 });

double avg = engine.Evaluate<double>("scores.Where(s => s >= 70).Average()");
// 87.75
```

<!-- test: Variables_SetVariableTyped -->

The generic type parameter `T` is stored alongside the value. During semantic analysis, when the binder encounters the identifier `scores`, it calls `BindingContext.TryGetVariableType("scores")`, which returns `typeof(List<int>)`. The binder then produces resolved nodes (`BoundPropertyAccessExpr` for `.Count`, `BoundResolvedCallExpr` for `.Where()` and `.Average()`) with the method already selected. The interpreter and compiler execute these without runtime method lookup.

With untyped `SetVariable(string, object?)`, the binder gets `typeof(object)` and produces dynamic nodes instead. Everything still works, but member resolution defers to runtime reflection on every evaluation.

`SetVariable<T>` returns the engine for fluent chaining:

```csharp
engine
    .SetVariable<double>("rate", 0.05)
    .SetVariable<int>("years", 10)
    .SetVariable<double>("principal", 1000.0);
```

<!-- test: Variables_FluentChaining -->

Variables persist across evaluations. Updating a variable's value is visible to the next `Evaluate` call on any thread.

The engine uses a two-phase variable lifecycle. Variables set before the first evaluation are stored as `PendingVariable` structs (value + inferred type) in a `Dictionary` protected by a lock (`_contextInitLock`). On the first evaluation, `GetOrCreateContext()` bulk-defines all pending variables into the `AlderContext`, then clears the pending dictionary. Variables set after context initialization bypass the pending state entirely. They are defined directly into the `AlderContext` using a double-check lock pattern (check outside lock, re-check inside lock) to avoid contention in the hot path. The `AlderContext` itself uses `ConcurrentDictionary` for thread-safe reads during evaluation.

### Anonymous object: inline, scoped

```csharp
bool eligible = engine.Evaluate<bool>(
    "age >= 18 && country != null",
    new { age = 25, country = "US" }); // true
```

<!-- test: Variables_AnonymousObject -->

Internally, `ToVariableDictionary` reads the object's public properties via reflection and builds an `IDictionary<string, object?>`. A child engine is created, the dictionary is loaded into it via `SetVariables`, and evaluation runs against that child. The parent engine's variable store is untouched.

### `IDictionary<string, object?>`: dynamic keys, scoped

```csharp
var vars = new Dictionary<string, object?>
{
    ["threshold"] = 100,
    ["multiplier"] = 1.5
};
double result = engine.Evaluate<double>("threshold * multiplier", vars); // 150.0
```

<!-- test: Variables_Dictionary -->

Same scoping as anonymous objects. A child engine is created, variables loaded, evaluation runs, parent unaffected. Values are typed as `object`, so the binder produces dynamic nodes.

## Bound Tree Caching and Type Versioning

When you call `Evaluate(AlderExpression)`, the engine checks whether a bound tree already exists for the current variable type state. The `AlderExpression` maintains a bound tree cache keyed by `AlderContext` using a `ConditionalWeakTable`. Each cache entry carries a version number derived from `AlderContext.GetTypeInferenceVersion()`.

When you call `SetVariable<T>` and the type changes, `_variableTypeVersion` is atomically incremented. The next evaluation detects the version mismatch and re-binds the expression with the new type information. When types have not changed, binding is skipped entirely and only execution runs.

This means:

- **Same expression, same variable types**: parse once, bind once, execute many times.
- **Same expression, changed variable types**: parse once, re-bind, execute.
- **Same expression, changed variable values (same types)**: parse once, bind once, execute. No re-binding needed because the binder only cares about types, not values.

## Child Engines

`CreateChild()` creates a new engine that inherits the parent's configuration, expression cache, and variables, but maintains its own variable scope.

```csharp
var parent = new AlderEngine();
parent.SetVariable<double>("baseFee", 50.0);

var tenantA = parent.CreateChild();
tenantA.SetVariable<double>("discount", 0.1);

var tenantB = parent.CreateChild();
tenantB.SetVariable<double>("discount", 0.25);

double a = tenantA.Evaluate<double>("baseFee * (1 - discount)"); // 45.0
double b = tenantB.Evaluate<double>("baseFee * (1 - discount)"); // 37.5

// Parent is unaffected: no discount variable exists here
double base_ = parent.Evaluate<double>("baseFee"); // 50.0
```

<!-- test: Variables_ChildEngines -->

Child engines share the parent's `AlderConfig`, `ExpressionCache`, and `DisposalToken`. The child's `AlderContext` is created via `parentContext.CreateChild()`, which uses local `Dictionary` storage instead of `ConcurrentDictionary` (faster for evaluation-scoped contexts that are not shared across threads).

Variable lookup walks the context chain: child first, then parent. Assignment targets the context that owns the variable. If `baseFee` is defined in the parent, `baseFee = 100` in a child expression modifies the parent's value. If the variable does not exist in any scope, `CS0103` is raised.

Disposing the parent disposes all children (they share the same `DisposalToken`).

### Concurrency Pattern

The typical server pattern: one parent engine configured at startup, one child per request or tenant.

```csharp
// Startup
var engine = new AlderEngine(o => { /* configure once */ });
engine.SetVariable<double>("taxRate", 0.08);

// Per-request (concurrent, thread-safe)
Parallel.ForEach(orders, order =>
{
    var child = engine.CreateChild();
    child.SetVariable<double>("amount", order.Amount);
    var tax = child.Evaluate<double>("amount * taxRate");
});
```

<!-- test: Variables_ConcurrentChildEngines -->

Each child sees `taxRate` from the parent and its own `amount`. No locking required. `CreateChild()` and `SetVariable` are thread-safe, and child contexts use isolated local storage.

## Scoping During Evaluation

When `Evaluate` is called, the engine creates additional context layers:

1. **Engine context**: persistent variables from `SetVariable`, `ConcurrentDictionary`-backed.
2. **Per-call child engine** (if `variables` parameter is passed): scoped dictionary/anonymous object variables.
3. **Execution context**: a child of the above, created for each `Evaluate` call in interpreted mode. Expression-internal `var` declarations live here and are discarded after evaluation.

This three-layer design ensures that expression-internal variables (`var x = 5;`) never leak into the engine's persistent state, and per-call variables never modify the parent engine.

## `GetVariables`: Discovering Referenced Names

```csharp
var expr = engine.Parse("orders.Where(o => o.Total > minAmount).Count()");
var vars = expr.GetVariables(); // ["orders", "minAmount"]
```

<!-- test: Variables_GetVariables -->

`GetVariables()` walks the AST via `VariableCollector` and returns the distinct names of unbound identifiers (names that the expression expects the engine to provide). LINQ range variables (`o` in the example) are excluded because they are bound by the lambda.

This is useful for building UIs that prompt for missing inputs, or for validating that user-supplied code only references permitted names.

## Case Sensitivity

Variable names are case-sensitive by default (`StringComparer.Ordinal`). Set `IsCaseSensitive = false` on `AlderOptions` to use `StringComparer.OrdinalIgnoreCase`. This affects all variable lookups, function names, module names, and member resolution.

```csharp
var engine = new AlderEngine(o => o.IsCaseSensitive = false);
engine.SetVariable<int>("count", 42);
int result = engine.Evaluate<int>("COUNT"); // 42, case-insensitive match
```

<!-- test: Variables_CaseInsensitive -->
