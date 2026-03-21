---
title: "Variables"
description: "Inject host values into expressions with SetVariable, SetVariable<T>, SetVariables, anonymous objects, and per-invocation dictionaries."
sidebar:
  order: 3
---

## Overview

Variables are the primary way to pass host data into Alder expressions. Values set on the engine are available to all subsequent evaluations.

```csharp
var engine = new AlderEngine();
engine.SetVariable("name", "Alice");
engine.SetVariable("age", 30);

var greeting = engine.Evaluate<string>(@"""Hello, "" + name");
// greeting: "Hello, Alice"
```

## SetVariable(string, object?)

Sets a variable with the compile-time type stored as `object`. Returns the engine for fluent chaining.

```csharp
var engine = new AlderEngine();
engine.SetVariable("x", 42);

var result = engine.Evaluate<int>("x + 8");
// result: 50
```

## SetVariable&lt;T&gt;(string, T)

Sets a variable with precise generic type information. This enables the compiler to generate optimized IL because the exact type is known at definition time, rather than boxing through `object`.

```csharp
var engine = new AlderEngine();
engine.SetVariable<int>("x", 42);

var result = engine.Evaluate<int>("x + 8");
// result: 50
```

**When to use which:**

- `SetVariable("x", 42)` — stores as `object`, value is boxed. Works for all scenarios.
- `SetVariable<int>("x", 42)` — stores with `int` type metadata. Enables optimized compilation paths.

Use `SetVariable<T>` when performance matters and the type is known at call time.

## SetVariables(IDictionary&lt;string, object?&gt;)

Bulk-sets multiple variables from a dictionary. All values are stored with `object` type semantics.

```csharp
var engine = new AlderEngine();
engine.SetVariables(new Dictionary<string, object?>
{
    ["x"] = 10,
    ["y"] = 20,
    ["label"] = "sum"
});

var result = engine.Evaluate<int>("x + y");
// result: 30
```

## Anonymous Object Variables

The `Evaluate` overloads accept an anonymous object whose properties become per-invocation variables. This uses reflection internally and is not AOT-compatible.

```csharp
var engine = new AlderEngine();
var result = engine.Evaluate<int>("x + y", new { x = 1, y = 2 });
// result: 3
```

Each public property on the anonymous object is extracted via reflection and injected as a variable for that evaluation call only.

## Per-Invocation Dictionary Variables

Pass an `IDictionary<string, object?>` to any `Evaluate` overload. These variables are scoped to that single call — they do not persist on the engine.

```csharp
var engine = new AlderEngine();

var vars = new Dictionary<string, object?> { ["x"] = 100 };
var result = engine.Evaluate<int>("x * 2", vars);
// result: 200

// x is not available in the next call without the dictionary
engine.TryEvaluate("x", out var result2);
// result2: null (evaluation failed — x is not defined)
```

Internally, per-invocation variables create a temporary child engine with its own context. The child inherits the parent's configuration and variables but adds the per-invocation values in its own scope.

## Scoping

Variables set via `SetVariable` persist across evaluations — they are part of the engine's state.

```csharp
var engine = new AlderEngine();
engine.SetVariable("counter", 0);

engine.Evaluate<int>("counter");
// result: 0

engine.SetVariable("counter", 10);
engine.Evaluate<int>("counter");
// result: 10
```

Per-invocation variables (dictionary or anonymous object) are scoped to that call only and do not affect engine state.

## Fluent Chaining

All `SetVariable` methods return the engine, enabling fluent configuration:

```csharp
var engine = new AlderEngine();
var result = engine
    .SetVariable("a", 1)
    .SetVariable("b", 2)
    .SetVariable("c", 3)
    .Evaluate<int>("a + b + c");
// result: 6
```

## Freeze Behavior

`SetVariable` is special among engine mutation methods. While registration methods (`RegisterModule`, `RegisterFunction`, etc.) throw `InvalidOperationException` after the first `Evaluate()`, `SetVariable` works in both phases:

- **Before first evaluation:** Variables are staged in an internal pending dictionary. They are materialized into the evaluation context when the engine freezes.
- **After first evaluation:** Variables are defined directly in the evaluation context via `context.Define()`. This is thread-safe.

```csharp
var engine = new AlderEngine();

// Before freeze
engine.SetVariable("x", 1);
engine.Evaluate<int>("x");
// result: 1

// After freeze — still works
engine.SetVariable("x", 99);
engine.Evaluate<int>("x");
// result: 99
```

## See Also

- [AlderOptions](../engine/options/) — Configure engine behavior
- [Expressions](../engine/expressions/) — Parse, evaluate, and reuse expressions
