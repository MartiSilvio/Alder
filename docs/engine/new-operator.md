---
title: "New Operator"
description: "Object creation in Alder expressions: construction, initializers, arrays, tuples, and sandbox gates."
sidebar:
  order: 9
---

## Overview

Alder expressions can create objects using the `new` operator, just like standard C#. Construction is gated by sandbox configuration -- the `AllowConstruction` flag must be enabled, and if an `AllowedTypes` allowlist is set, the type must appear in it.

Types must be available to the engine through one of: built-in type keywords (`int`, `string`, etc.), implicit BCL imports (`List<T>`, `Dictionary<TKey, TValue>`, etc.), or explicit registration via `RegisterAssembly`/`RegisterNamespace`. See [Type Registration](../engine/type-registration/) for details.

## Sandbox Gates

### AllowConstruction

The `AllowConstruction` flag controls whether `new` expressions are permitted. It is enabled in `Trusted()` mode but disabled in `Safe()` and `Strict()` modes.

```csharp
// Trusted mode (default) — construction allowed
var engine = new AlderEngine();
var list = engine.Evaluate("new List<int>()");
// list: List<int> (empty)

// Safe mode — construction blocked
var safeEngine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Safe()
});
safeEngine.Evaluate("new List<int>()");
// throws AlderSandboxException
```

### AllowedTypes

When `SandboxOptions.AllowedTypes` is set, only types in the allowlist may be constructed. The check uses exact type matching -- `typeof(List<int>)` must be in the set, not `typeof(List<>)`.

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Trusted() with
    {
        AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
    }
});

// Allowed — List<int> is in the allowlist
var list = engine.Evaluate("new List<int>()");

// Blocked — Dictionary<string, int> is not in the allowlist
engine.Evaluate("new Dictionary<string, int>()");
// throws AlderSandboxException
```

## Construction Patterns

### Basic Construction

Standard constructor invocation with arguments.

```csharp
var engine = new AlderEngine();

engine.Evaluate("new List<int>()");
// empty List<int>

engine.Evaluate("new DateTime(2024, 1, 1)");
// DateTime: 2024-01-01
```

### Object Initializers

Object initializers set properties after construction. The engine calls the default constructor, then assigns each property via `MemberAccess.SetMember`.

```csharp
var engine = new AlderEngine()
    .RegisterAssembly(typeof(MyType).Assembly)
    .RegisterNamespace("MyApp.Models");

engine.Evaluate("new MyType { Name = \"test\", Value = 42 }");
// MyType with Name="test", Value=42
```

The type must have a parameterless constructor and writable properties. The engine applies initializers in declaration order.

### Collection Initializers

Collection initializers call the `Add` method on the constructed object for each element. The type must implement a public `Add` method with one parameter.

```csharp
var engine = new AlderEngine();

engine.Evaluate("new List<int> { 1, 2, 3 }");
// List<int> with 3 elements: [1, 2, 3]
```

### Indexer Initializers

Indexer initializers use bracket syntax to set key-value pairs via the indexer.

```csharp
var engine = new AlderEngine();

engine.Evaluate("new Dictionary<string, int> { [\"a\"] = 1, [\"b\"] = 2 }");
// Dictionary with 2 entries: {"a": 1, "b": 2}
```

### Array Creation

Alder supports both sized arrays and arrays with initializers.

```csharp
var engine = new AlderEngine();

// Sized array (all elements default)
engine.Evaluate("new int[3]");
// int[3]: [0, 0, 0]

// Array with initializer
engine.Evaluate("new int[] { 10, 20, 30 }");
// int[3]: [10, 20, 30]

// Sized array with explicit type
engine.Evaluate("new string[5]");
// string[5]: [null, null, null, null, null]
```

### Tuple Creation

Tuple literals create `ValueTuple` instances. Alder supports tuples with up to 8+ elements -- tuples larger than 7 elements use `TRest` nesting automatically.

```csharp
var engine = new AlderEngine();

engine.Evaluate("(1, \"hello\")");
// ValueTuple<int, string>: (1, "hello")

engine.Evaluate("(1, 2, 3, 4, 5, 6, 7, 8)");
// ValueTuple<int, int, int, int, int, int, int, ValueTuple<int>>
// with TRest nesting for the 8th element
```

### Anonymous Objects

Anonymous objects are created through the host API by passing an anonymous object as a variables source. They cannot be constructed with `new` inside expressions. See [Variables](../engine/variables/) for details.

```csharp
var engine = new AlderEngine();
var result = engine.Evaluate("x + y", new { x = 1, y = 2 });
// result: 3
```

## Runtime Flow

When a `new` expression is evaluated, the engine follows this pipeline:

1. **Sandbox check** -- if `AllowConstruction` is `false`, throws `AlderSandboxException`
2. **Type allowlist** -- if `AllowedTypes` is set and the type is not in the set, throws `AlderSandboxException`
3. **AOT metadata** -- if a generated type context provides constructor metadata, attempts fast dispatch (matches on parameter count)
4. **Reflection fallback** -- uses `Activator.CreateInstance` with the provided arguments

If no matching constructor is found, throws `AlderException` with a `NoMatchingConstructor` diagnostic.

## See Also

- [Type Registration](../engine/type-registration/) -- Register assemblies and namespaces for type resolution
- [AlderOptions](../engine/options/) -- Sandbox configuration and AllowedTypes
- [Method Invocation](../engine/method-invocation/) -- Calling methods on constructed objects
