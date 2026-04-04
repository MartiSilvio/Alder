`TypeResolver` controls which .NET types are available to expressions. Resolution follows a five-step precedence chain.

## Resolution Order

When an expression references a type (`new T()`, `typeof(T)`, `(T)x`, `is T`, `T.Member`):

1. **Built-in keywords**: `int`, `string`, `bool`, `List<T>`, `Dictionary<K,V>`, etc.
2. **Implicit BCL imports**: `System`, `System.Collections.Generic`, `System.Linq`, `System.Threading.Tasks`
3. **Explicit imports**: namespaces registered via `Types.AddNamespace()`
4. **Fully qualified names**: any type from loaded assemblies
5. **Fail**: `CS0246`

Ambiguous matches across imported namespaces produce `CS0104`.

## Default Available Types

| Source | Examples |
|--------|---------|
| Type keywords | `int`, `long`, `double`, `float`, `decimal`, `string`, `bool`, `char`, `object`, `byte`, `sbyte`, `short`, `ushort`, `uint`, `ulong`, `nint`, `nuint`, `dynamic`, `void` |
| `System` | `Math`, `Convert`, `DateTime`, `Guid`, `Random`, `TimeSpan`, `Array`, `Tuple`, `StringComparison`, `DayOfWeek`, `TypeCode` |
| `System.Collections.Generic` | `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`, `KeyValuePair<K,V>`, `SortedList<K,V>`, `LinkedList<T>` |
| `System.Linq` | All `Enumerable` extension methods |
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

Without `AddNamespace`, these types are still accessible via fully qualified names: `new System.IO.MemoryStream()`.

## Adding Assemblies

```csharp
o.Types.AddAssembly(typeof(MyDomainType).Assembly);
```

Makes all public types from the assembly available via fully qualified names.

## Extension Methods

```csharp
o.Types.AddExtensionMethods<MyLinqExtensions>();
```

Registers a static class's extension methods. `System.Linq.Enumerable` is registered by default. User-registered types are inserted at index 0, giving them priority. Overload resolution selects the best overload among all discovered candidates.

## Generic Types

Nested generics resolve recursively:

```csharp
typeof(Dictionary<string, List<int>>)
new Dictionary<string, List<int>>()
```

Short names like `List` automatically probe arities 1-8 (`` List`1 ``, `` List`2 ``, etc.). The resolver uses CLR backtick notation internally.

Nested types (`OuterClass.InnerType`) split right-to-left, converting dots to the CLR `+` separator.

`System.Reflection` types are excluded from implicit imports and only resolvable via FQN (where they are blocked by default denied namespaces).

## Nullable Types

`int?`, `bool?` etc. The parser produces `Nullable<T>`, the resolver resolves the inner type and wraps.

## Array Types

`int[]`, `int[,]`, `int[][]`. The element type resolves through the standard chain.

## Caching

Resolved types are cached in a `ConcurrentDictionary` per engine. Shared across parent and child engines. Never evicted.

## Resolution vs Blocking

Type resolution determines whether a type *can be found*. Type blocking ([Security](../security/sandbox.md#type-blocking)) determines whether a found type is *allowed*. Separate concerns:

- A type in a denied namespace can still be resolved if also in `TrustedTypes`
- A type not found produces `CS0246` (resolution error)
- A type found but blocked produces `ALDR0107` (security error)
