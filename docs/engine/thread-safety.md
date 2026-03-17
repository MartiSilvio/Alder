---
title: "Thread Safety"
description: "Freeze-on-first-use lifecycle, concurrent evaluation guarantees, child engine isolation, and per-invocation variables."
sidebar:
  order: 7
---

## Overview

CsEval is **thread-safe after freeze, not before**. The engine follows a two-phase lifecycle:

1. **Setup phase** (mutable, single-threaded) -- register modules, functions, types, namespaces
2. **Evaluation phase** (frozen, thread-safe) -- evaluate, parse, compile concurrently

The transition happens automatically on first use.

## Freeze-on-First-Use

The engine freezes its configuration the first time any of these methods is called:

- `Evaluate()` / `TryEvaluate()`
- `TryValidate()`
- `CreateChild()`
- `GetRegisteredModules()`

Freezing uses `Interlocked.CompareExchange` to atomically transition the config from mutable to immutable. If two threads race to freeze, only one wins -- the other sees the already-frozen config.

```csharp
var engine = new CsEvalEngine();

// Setup phase: mutable, single-threaded
engine.RegisterFunction("double", args => (int)args[0]! * 2);
engine.SetVariable("x", 10);

// First Evaluate() freezes the engine
var result = engine.Evaluate("double(x)"); // 20

// Evaluation phase: frozen, thread-safe
// Registration methods now throw:
Assert.Throws<InvalidOperationException>(() =>
    engine.RegisterFunction("triple", args => (int)args[0]! * 3));
```

## Before Freeze

During the setup phase, all configuration methods are available:

| Method | Before freeze | After freeze |
|--------|:---:|:---:|
| `RegisterFunction` | Yes | Throws `InvalidOperationException` |
| `RegisterModule` | Yes | Throws |
| `RegisterFromType` | Yes | Throws |
| `RegisterFromAssembly` | Yes | Throws |
| `RegisterAssembly` | Yes | Throws |
| `RegisterNamespace` | Yes | Throws |
| `RegisterExtensionMethods` | Yes | Throws |
| `UseGeneratedContext` | Yes | Throws |
| `ClearGeneratedContexts` | Yes | Throws |
| `SetVariable` | Yes | **Yes** |
| `Evaluate` / `Parse` / `Compile` | Triggers freeze | Yes |

## After Freeze

Once frozen, the engine's configuration (modules, functions, type registrations, namespaces) is immutable and shared safely across threads. These methods are all thread-safe:

- `Evaluate()` and all overloads
- `Evaluate<T>()`
- `TryEvaluate()` / `TryEvaluate<T>()`
- `Parse()` / `TryParse()`
- `TryValidate()`
- `EvaluateWithTrace()`
- `CreateChild()`
- `SetVariable()` / `SetVariable<T>()` / `SetVariables()`

## SetVariable Is Special

`SetVariable` is the only registration-like method that works both before and after freeze.

**Before freeze:** Variables are staged in a pending dictionary and materialized into the context when the engine freezes.

**After freeze:** Variables are written directly to the context's `ConcurrentDictionary`, making them immediately visible to concurrent evaluations.

```csharp
var engine = new CsEvalEngine();
engine.SetVariable("x", 1);       // staged (before freeze)
engine.Evaluate("x");             // freezes, returns 1
engine.SetVariable("x", 2);       // direct write (after freeze)
engine.Evaluate("x");             // returns 2
```

## Child Engine Pattern

`CreateChild()` returns a new engine that shares the parent's frozen configuration and expression cache, but has its own variable scope.

```csharp
var parent = new CsEvalEngine();
parent.SetVariable("shared", 100);

var child = parent.CreateChild();
child.SetVariable("local", 42);

// Child sees parent variables
var r1 = child.Evaluate<int>("shared + local"); // 142

// Parent does NOT see child variables
Assert.Throws<CsEvalException>(() => parent.Evaluate("local"));
```

### What Is Shared

| Resource | Shared? |
|----------|:---:|
| Frozen config (modules, functions, types) | Yes |
| Expression cache | Yes |
| Parent variables | Yes (read-only from child) |
| Child variables | No (isolated to child) |

### Use Case: Per-Request Isolation

In server scenarios, create a child engine per request to isolate user-specific variables while sharing the expensive frozen configuration:

```csharp
// At startup: configure once
var root = new CsEvalEngine();
root.RegisterModule("db", typeof(DbModule));
root.Evaluate("1"); // force freeze

// Per request: cheap child with isolated variables
var request = root.CreateChild();
request.SetVariable("userId", currentUser.Id);
request.SetVariable("input", requestBody.Expression);
var result = request.Evaluate("db.Query(userId)");
```

### Variable Isolation

Variables set on a child are not visible to the parent or to sibling children:

```csharp
var parent = new CsEvalEngine();
parent.SetVariable("x", 1);

var child1 = parent.CreateChild();
child1.SetVariable("y", 10);

var child2 = parent.CreateChild();
child2.SetVariable("y", 20);

// Each child has its own "y"
Assert.That(child1.Evaluate<int>("y"), Is.EqualTo(10));
Assert.That(child2.Evaluate<int>("y"), Is.EqualTo(20));

// Parent sees "x" but not "y"
Assert.That(parent.Evaluate<int>("x"), Is.EqualTo(1));
```

## Per-Invocation Variables

Passing a variables dictionary to `Evaluate()` creates a temporary child context. The variables are available only for that invocation and do not persist:

```csharp
var engine = new CsEvalEngine();
engine.SetVariable("price", 100);

// Per-invocation variable "bonus" exists only during this call
var result = engine.Evaluate<int>("price + bonus",
    new Dictionary<string, object?> { ["bonus"] = 50 }); // 150

// "bonus" is not visible in subsequent calls
Assert.Throws<CsEvalException>(() => engine.Evaluate("bonus"));
```

## Expression Cache Concurrency

The expression cache is backed by `ConcurrentDictionary` and supports concurrent reads and writes without locking. It is shared between parent and child engines.

The bound expression cache uses `ConditionalWeakTable` per context, which is GC-friendly -- cached bound expressions are collected when their context is no longer referenced.

## See Also

- [CsEvalOptions](/engine/options/) -- configuration properties
- [Expressions](/engine/expressions/) -- parse, evaluate, reuse
- [Compilation Modes](/engine/compilation-modes/) -- interpreted vs compiled
