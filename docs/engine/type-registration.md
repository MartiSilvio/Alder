---
title: "Type Registration"
description: "Assemblies, namespaces, extension methods, type resolution order"
sidebar:
  order: 7
---

Alder's `TypeResolver` controls which .NET types are available to expressions. It resolves type names through a five-step precedence chain modeled after Roslyn's resolution order.

## Resolution Order

When an expression references a type (via `new T()`, `typeof(T)`, cast `(T)x`, `is T`, or static access `T.Member`), the resolver tries each step in order:

1. **Built-in type keywords** — `int`, `string`, `bool`, `List<T>`, `Dictionary<K,V>`, etc.
2. **Implicit BCL imports** — common types from `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks` are available without qualification
3. **Explicit namespace imports** — types from namespaces registered via `Types.AddNamespace()`
4. **Fully qualified names** — any type from loaded assemblies via `Namespace.TypeName`
5. **Fail** — `CS0246: The type or namespace name 'X' could not be found`

If a type name matches in multiple imported namespaces, `CS0104: Ambiguous reference` is thrown — same as the C# compiler.

## Default Available Types

Without any configuration, these types are available by keyword or short name:

| Source | Examples |
|--------|---------|
| Type keywords | `int`, `long`, `double`, `float`, `decimal`, `string`, `bool`, `char`, `object`, `byte`, `sbyte`, `short`, `ushort`, `uint`, `ulong`, `nint`, `nuint`, `dynamic`, `void` |
| `System` | `Math`, `Convert`, `DateTime`, `Guid`, `Random`, `TimeSpan`, `Array`, `Tuple`, `StringComparison`, `ConsoleColor`, `DayOfWeek`, `DateTimeKind`, `TypeCode` |
| `System.Collections.Generic` | `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`, `KeyValuePair<K,V>`, `SortedList<K,V>`, `SortedDictionary<K,V>`, `LinkedList<T>` |
| `System.Linq` | All `Enumerable` extension methods (registered via `Types.ExtensionTypes`) |
| `System.Threading.Tasks` | `Task`, `Task<T>` |

## Adding Namespaces

```csharp
var engine = new AlderEngine(o =>
{
    o.Types.AddNamespace("System.IO");
    o.Types.AddNamespace("System.Text.RegularExpressions");
});

// Now available without FQN:
// new MemoryStream()
// Regex.IsMatch("hello", @"\w+")
```

<!-- test: Types_AddNamespace -->

Without `AddNamespace`, these types are still accessible via fully qualified names: `new System.IO.MemoryStream()`, `System.Text.RegularExpressions.Regex.IsMatch(...)`.

## Adding Assemblies

```csharp
o.Types.AddAssembly(typeof(MyDomainType).Assembly);
```

Makes all public types from the assembly available via fully qualified names. Without this, only types from already-loaded assemblies (the core runtime) are searchable.

## Extension Methods

```csharp
o.Types.AddExtensionMethods<MyLinqExtensions>();
```

Registers a static class's extension methods so they can be called on matching types in expressions. `System.Linq.Enumerable` is registered by default — this is why `.Where()`, `.Select()`, `.Sum()` work out of the box.

Extension methods are searched in registration order. If multiple extension types define a method with the same name, the first registered type wins during initial method discovery. Overload resolution then selects the best overload among all discovered candidates.

## Generic Type Resolution

The resolver handles generic types with nested generic arguments:

```csharp
typeof(Dictionary<string, List<int>>)   // resolved recursively
new Dictionary<string, List<int>>()      // construction works
```

Generic type arguments are resolved through the same five-step chain. If any argument fails to resolve, the entire generic type resolution fails.

## Nullable Types

`int?`, `string?`, `bool?` — the `?` suffix is handled by the parser, which produces a `Nullable<T>` type reference. The resolver resolves the inner type and wraps it.

## Array Types

`int[]`, `string[]`, `int[,]`, `int[][]` — array suffixes are parsed by `TryParseArraySuffix` in the resolver. The element type is resolved through the standard chain, then `RuntimeArrayFactory.GetArrayType` produces the array type.

## Caching

Resolved types are cached in a `ConcurrentDictionary<string, Type?>` on the `TypeResolver` instance. The cache is shared across evaluations on the same engine (and its children). Cache entries are never evicted — types don't change at runtime.

## Type Resolution vs Type Blocking

Type resolution (this page) determines whether a type *can be found*. Type blocking (see [Security](../security/sandbox.md#type-blocking)) determines whether a *found type* is *allowed*. They're separate concerns:

- A type in a denied namespace can still be resolved if it's also in `TrustedTypes`
- A type that's not found (not in any registered assembly/namespace) produces `CS0246`, not a security error
- A type that's found but blocked by security produces `ALDR0107`
