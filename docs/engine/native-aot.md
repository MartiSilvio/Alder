---
title: "Native AOT"
description: "Source generator approach for AOT-compatible type metadata, AlderTypeContext, AlderBuiltInContext, and trimming limitations."
sidebar:
  order: 8
---

## Overview

Alder supports Native AOT deployment via source generators that pre-compute type metadata at compile time, eliminating the need for runtime reflection.

**Honest assessment:** Alder's interpreted mode (the default) works well under AOT. The compiled backend from the **Alder.Compiled** package (IL emission via `System.Linq.Expressions`) may encounter limitations depending on your trimming configuration, since expression tree compilation relies on reflection emit. If you target AOT, test your specific expressions under your trimming settings.

## The Problem

In a standard .NET deployment, Alder uses reflection to discover type members (properties, methods, fields, constructors) at runtime. Native AOT trims unused code paths, which can remove type metadata that Alder needs to resolve member access in expressions.

Methods that scan assemblies via reflection are marked `[RequiresUnreferencedCode]` and are unavailable in AOT-safe code:

```csharp
// This method is [RequiresUnreferencedCode] — not AOT-safe
engine.RegisterFromAssembly(typeof(MyType).Assembly);
```

## Source Generator Approach

Alder provides a source generator that pre-computes type metadata at compile time. You annotate a partial class extending `AlderTypeContext` with `[AlderRegistered]` attributes:

```csharp
using Alder.Aot;

[AlderRegistered(typeof(MyType))]
[AlderRegistered(typeof(AnotherType))]
public partial class MyAppContext : AlderTypeContext;
```

The source generator produces an implementation of `GetTypeMetadata()` that returns pre-computed `IAotTypeMetadata` instances for each registered type.

### Registering the Context

Use `UseGeneratedContext()` to register your context with the engine:

```csharp
var engine = new AlderEngine();
engine.UseGeneratedContext(new MyAppContext());
```

Multiple contexts can be registered. Metadata from later contexts overwrites earlier entries for the same type.

## AlderBuiltInContext

Alder ships with a built-in context that covers common BCL types. It is registered by default on every engine -- you do not need to add it manually.

The built-in context covers:

- Primitives: `int`, `long`, `double`, `float`, `decimal`, `bool`, `char`, `byte`, `string`, `object`
- Date/time: `DateTime`, `TimeSpan`
- Utility: `Guid`, `Math`, `Convert`, `Environment`, `Exception`
- Collections: `List<int>`, `List<string>`, `Dictionary<string, object>`, `HashSet<int>`, `Queue<int>`, `Stack<int>`, and more
- Tuples: `(int, int)`, `(string, int)`, `(string, string)`, `(object, object)`
- Nullable: `int?`, `double?`, `bool?`, `long?`

For types not in the built-in context, create your own context with `[AlderRegistered]` attributes.

## ClearGeneratedContexts

Removes all registered contexts (including the built-in context). Must be called before freeze.

```csharp
var engine = new AlderEngine();
engine.ClearGeneratedContexts(); // removes built-in context
engine.UseGeneratedContext(new MyCustomContext()); // add only your types
```

## IAotTypeMetadata Interface

Generated contexts implement this interface for each registered type:

```csharp
public interface IAotTypeMetadata
{
    Type Type { get; }
    bool TryGetProperty(string name, object instance, out object? value);
    bool TrySetProperty(string name, object instance, object? value);
    bool TryGetField(string name, object instance, out object? value);
    bool TrySetField(string name, object instance, object? value);
    bool TryGetIndex(object instance, object key, out object? value);
    bool TrySetIndex(object instance, object key, object? value);
    bool TryGetStaticProperty(string name, out object? value);
    bool TryGetStaticField(string name, out object? value);
    bool TryCreateInstance(object?[] args, out object? instance);
    bool TryInvokeMethod(string name, object instance, object?[] args, out object? result);
    bool TryInvokeStaticMethod(string name, object?[] args, out object? result);
}
```

Each `Try*` method returns `true` if the operation was handled by the pre-computed metadata, `false` if the runtime should fall back to reflection.

## Limitations

### RegisterFromAssembly

`RegisterFromAssembly` is marked `[RequiresUnreferencedCode]` and cannot be used in AOT-safe code. Use `[AlderRegistered]` source generation instead.

### Anonymous Object Variables

Passing anonymous objects to `Evaluate()` uses reflection (`GetProperties`) to extract variable values. This is not AOT-compatible. Use explicit `Dictionary<string, object?>` instead:

```csharp
// Not AOT-safe:
engine.Evaluate("x + y", new { x = 1, y = 2 });

// AOT-safe:
engine.Evaluate("x + y", new Dictionary<string, object?> { ["x"] = 1, ["y"] = 2 });
```

### Compiled Mode Under Trimming

The compiled backend (enabled via `UseCompiler()`) uses `System.Linq.Expressions` which relies on reflection emit. Under aggressive trimming, some expression patterns may fail to compile. If you encounter issues:

1. Use the default interpreted mode (do not call `UseCompiler()`)
2. Or adjust your trimming configuration to preserve `System.Linq.Expressions`

The interpreted mode has no dependency on reflection emit and works reliably under AOT.

## See Also

- [Type Registration](/engine/type-registration/) -- `RegisterAssembly`, `RegisterNamespace`, `UseGeneratedContext`
- [Compilation Modes](/engine/compilation-modes/) -- interpreted vs compiled tradeoffs
