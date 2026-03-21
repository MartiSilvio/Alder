---
title: "Type Registration"
description: "Make host types available for construction, static access, and type references in Alder expressions."
sidebar:
  order: 5
---

## Overview

By default, Alder expressions can use built-in type keywords (`int`, `string`, `bool`, etc.) and common BCL types (`List`, `Dictionary`, `Task`, etc.) without any registration. To use your own types or types from other assemblies, you register them with the engine.

There is no method called `RegisterType`. Instead, type availability is controlled through these methods:

| Method                     | What it does                                                                                              |
| -------------------------- | --------------------------------------------------------------------------------------------------------- |
| `RegisterAssembly`         | Makes all public types in an assembly available for type resolution                                       |
| `RegisterNamespace`        | Adds a namespace to the implicit import list (enables short names)                                        |
| `RegisterExtensionMethods` | Registers a static class's extension methods                                                              |
| `RegisterFromType`         | Registers a type's methods as callable functions ([details](./functions-and-modules.md#registerfromtype)) |

All registration methods return `this` for fluent chaining and must be called **before the first `Evaluate()`**.

## RegisterAssembly

Makes all public types from an assembly available for type resolution. After registration, expressions can reference types by fully qualified name or by short name if the namespace is also imported.

```csharp
public AlderEngine RegisterAssembly(Assembly assembly)
```

```csharp
var engine = new AlderEngine()
    .RegisterAssembly(typeof(System.Text.Json.JsonSerializer).Assembly)
    .RegisterNamespace("System.Text.Json");

var result = engine.Evaluate("JsonSerializer.Serialize(42)");
// result: "42"
```

The engine always includes these assemblies without explicit registration:

- `System.Private.CoreLib` (core types)
- `System.Collections` (generic collections)
- `System.Threading.Tasks`
- `System.Linq`
- `System.Text.RegularExpressions`

## RegisterNamespace

Adds a namespace to the implicit import list, allowing expressions to use short type names instead of fully qualified names.

```csharp
public AlderEngine RegisterNamespace(string namespaceName)
```

```csharp
var engine = new AlderEngine()
    .RegisterAssembly(typeof(System.Net.IPAddress).Assembly)
    .RegisterNamespace("System.Net");

// Can now use "IPAddress" instead of "System.Net.IPAddress"
```

Without `RegisterNamespace`, types from registered assemblies can still be used by their fully qualified name.

## RegisterExtensionMethods

Registers a static class's extension methods so they can be called naturally in expressions.

```csharp
public AlderEngine RegisterExtensionMethods(Type type)
public AlderEngine RegisterExtensionMethods<T>()
```

LINQ extension methods from `System.Linq.Enumerable` are registered by default. Use this method for custom extension methods:

```csharp
public static class StringExtensions
{
    public static string Shout(this string s) => s.ToUpper() + "!";
}

var engine = new AlderEngine()
    .RegisterExtensionMethods<StringExtensions>()
    .SetVariable("name", "hello");

var result = engine.Evaluate<string>("name.Shout()");
// result: "HELLO!"
```

## Type Resolution Precedence

When an expression references a type name, Alder resolves it in this order:

1. **Built-in type keywords** -- `int`, `string`, `bool`, `double`, `decimal`, `char`, `object`, `long`, `float`, `byte`, `short`, and their nullable forms (`int?`, `string?`, etc.)
2. **Implicit BCL imports** -- Common types from `System`, `System.Collections.Generic`, `System.Threading.Tasks`, and `System.Linq` are available by short name (`List`, `Dictionary`, `Task`, `Regex`, etc.). Types from `System.Reflection` are excluded for security.
3. **Explicit namespace imports** -- Types from namespaces added via `RegisterNamespace()`. If two imported namespaces define the same type name, an ambiguous reference error is raised.
4. **Fully qualified name** -- Dotted names like `System.Text.StringBuilder` are resolved against all registered assemblies.
5. **Diagnostic** -- If no match is found, a `TypeNotFound` error is raised.

```csharp
var engine = new AlderEngine();

// Level 1: built-in keyword, no registration needed
var a = engine.Evaluate<int>("int.MaxValue");

// Level 2: implicit BCL import
var b = engine.Evaluate("new List<int>()");

// Level 4: fully qualified name against default assemblies
var c = engine.Evaluate("new System.Text.StringBuilder()");
```

## AllowedTypes

`SandboxOptions.AllowedTypes` provides a type-level allowlist. When set, only the listed types can be used for construction and static member access in expressions. Types not in the set trigger a `AlderSandboxException`.

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Trusted() with
    {
        AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
    }
});

// List<int> is allowed
var list = engine.Evaluate("new List<int>()");

// StringBuilder is NOT in AllowedTypes -- throws AlderSandboxException
// engine.Evaluate("new System.Text.StringBuilder()");
```

The allowlist checks the exact constructed type. For generic types, add the closed generic type (e.g., `typeof(List<int>)`) rather than the open generic definition.

When `AllowedTypes` is `null` (the default), all types in registered assemblies are available with no restriction.

For full sandbox configuration details, see the sandbox documentation in Phase 5.

## UseGeneratedContext

Registers an AOT-compatible type context generated by the Alder source generator. This enables type metadata to be available without runtime reflection, which is required for Native AOT deployment.

```csharp
public AlderEngine UseGeneratedContext(AlderTypeContext context)
```

```csharp
engine.UseGeneratedContext(new MyTypeContext());
```

The built-in context (`AlderBuiltInContext.Default`) is registered automatically. Use `UseGeneratedContext` to add metadata for your own types.

For details on the source generator and AOT workflow, see the Native AOT page.

## ClearGeneratedContexts

Removes all generated type contexts, including the built-in default context. Use this when you need complete control over which type metadata is available.

```csharp
public AlderEngine ClearGeneratedContexts()
```

## Pre-freeze Requirement

All type registration methods must be called before the first `Evaluate()`. After evaluation, the engine freezes and registration throws `InvalidOperationException`:

```csharp
var engine = new AlderEngine();
engine.Evaluate("1 + 1"); // freezes the engine

// This throws InvalidOperationException:
engine.RegisterNamespace("System.Net");
```

## See Also

- [Functions and Modules](./functions-and-modules.md) -- registering callable functions and module namespaces
