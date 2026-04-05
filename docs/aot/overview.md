Alder runs on NativeAOT, Unity IL2CPP, and every other .NET platform, including environments where reflection is restricted and runtime code generation is unavailable. This is enabled by a two-tier dispatch model backed by an incremental source generator.

The source generator runs at compile time and emits typed property access, method dispatch, constructor invocation, and pre-instantiated delegate factories for each registered type. At runtime, `TypedDispatchHelper` checks this AOT-generated dispatch before falling back to reflection. The same expression that evaluates on full .NET with JIT compilation evaluates on NativeAOT and IL2CPP through the interpreter with AOT dispatch: same API, same behavior, single NuGet package, no conditional compilation in user code.

## Two-Tier Model

```mermaid
graph TD
    A["Member access or<br/>method call"] --> B{"AOT dispatch<br/>registered for type?"}
    B -->|"Yes"| C["TypedDispatch<br/>(no reflection)"]
    B -->|"No"| D["Reflection fallback<br/>(PropertyInfo, MethodInfo)"]
    C -->|"TryGet/TryInvoke<br/>returns true"| E["Result"]
    C -->|"returns false"| D
    D --> E
```

The AOT check happens at every member access and method invocation via `TypedDispatchHelper`: the centralized entry point for all typed dispatch. The check walks the type hierarchy: if dispatch is registered for `List<int>`, accessing members inherited from `IList<int>` or `ICollection<int>` will also use the AOT path.

## When AOT Matters

- **NativeAOT (.NET 7+)**: `MakeGenericMethod` with value-type arguments is restricted. The source generator pre-instantiates generic methods and delegate factories to avoid this.
- **Unity IL2CPP**: Same restrictions as NativeAOT: no runtime code generation. The interpreter with AOT dispatch is the only execution path.
- **Standard .NET**: AOT dispatch provides a performance benefit by skipping reflection, but reflection works fine as a fallback.

On NativeAOT, `UseCompiler()` throws `PlatformNotSupportedException` because `Expression.Compile()` requires a JIT. The interpreter with AOT dispatch is the recommended path.

## Source Generator

Alder ships an incremental source generator that produces `TypedDispatch` implementations for registered types. To use it:

### 1. Define a type context

```csharp
using Alder.Aot;

[AlderRegistered(typeof(List<int>))]
[AlderRegistered(typeof(Dictionary<string, int>))]
[AlderRegistered(typeof(DateTime))]
public partial class MyTypeContext : AlderTypeContext
{
}
```

### 2. Register it with the engine

```csharp
var engine = new AlderEngine(o =>
{
    o.Aot.UseGeneratedContext(new MyTypeContext());
});
```

### 3. The generator emits

For each `[AlderRegistered]` type, the generator produces a `file sealed class` implementing `TypedDispatch` with:

| Method | Generated code |
|--------|---------------|
| `TryGet(name, instance, out value)` | `switch` on member name → typed property or field read |
| `TrySet(name, instance, value)` | `switch` on member name → typed property or field write |
| `TryGetStatic(name, out value)` | `switch` on member name → static property or field read |
| `TryGetIndex(instance, key, out value)` | Typed indexer get |
| `TrySetIndex(instance, key, value)` | Typed indexer set |
| `TryCreate(args, out instance)` | Constructor dispatch |
| `TryInvoke(name, instance, args, out result)` | Instance method dispatch with `is` type checks per overload |
| `TryInvokeStatic(name, args, out result)` | Static method dispatch |

All dispatch uses `is` type checks for same-arity overloads, never blind casts that rely on exception fallback. If the AOT path can't handle an argument shape (named args, null values, special markers), it returns `false` and the reflection path takes over.

### Extension method dispatch

The generator also emits `EnumerableDispatch`: an `TypedDispatch` implementation for LINQ extension methods. This is driven by a data-driven `LinqMethodDescriptor` table and emitted by `ExtensionMethodEmitter`. It provides typed dispatch for common LINQ operations (`Where`, `Select`, `OrderBy`, etc.) without reflection.

## `TypedDispatch` Interface

```csharp
public interface TypedDispatch
{
    Type Type { get; }
    bool TryGet(string name, object instance, out object? value);
    bool TrySet(string name, object instance, object? value);
    bool TryGetStatic(string name, out object? value);
    bool TryGetIndex(object instance, object key, out object? value);
    bool TrySetIndex(object instance, object key, object? value);
    bool TryCreate(object?[] args, out object? instance);
    bool TryInvoke(string name, object instance, object?[] args, out object? result);
    bool TryInvokeStatic(string name, object?[] args, out object? result);
}
```

Every method returns `bool`. `true` means the operation was handled, `false` signals fallback to reflection. No exceptions for control flow.

The interface is simplified compared to the old `IAotTypeMetadata`: property and field access are unified into `TryGet`/`TrySet`/`TryGetStatic`, eliminating the need for separate property and field methods.

## TypedDispatchHelper

`TypedDispatchHelper` is the centralized entry point for all typed dispatch. All member access and method invocation in both the interpreter and compiler goes through it. It handles:

- Looking up the `TypedDispatch` for a type via `AlderConfig.TryGetDispatch(type, out TypedDispatch?)`
- Walking the type hierarchy to find dispatch for base types
- Routing member reads, writes, method calls, and constructor invocations to the appropriate `TypedDispatch` method

## Delegate Factories

On NativeAOT, `MakeGenericMethod` with value-type generic arguments is restricted. Alder uses lambda-to-delegate conversion extensively (every LINQ lambda needs to be converted to `Func<T, bool>`, `Func<T, TResult>`, etc.). When `T` is a value type, this conversion requires `MakeGenericMethod` to create the delegate factory.

The source generator pre-instantiates delegate factories for common shapes:

| Delegate shape | Purpose |
|---------------|---------|
| `Func<T, bool>` | LINQ predicates (`Where`, `Any`, `All`, `First`) |
| `Func<T, T>` | Identity transforms (`Select`) |
| `Func<T, int>` | Integer projections |
| `Func<T, string>` | String projections |
| `Func<T, object>` | Boxing projections |
| `Action<T>` | Side effects |
| `Func<T, T, T>` | `Aggregate` |
| `Func<T, T, bool>` | Comparison predicates |
| `Comparison<T>` | `Sort` |

Each factory is a `Func<object, Delegate>` that wraps a `LambdaValue` in the target delegate type. These are registered via `AlderTypeContext.GetDelegateFactories()` and stored in `AlderConfig.DelegateFactories`.

## Generic Instantiation Rooting

The generator also emits static field roots that force the AOT compiler to compile specific generic instantiations:

- `Nullable<T>` for each registered value type
- `EqualityComparer<T>`, `Comparer<T>` for comparisons
- `List<T>`, `IEnumerable<T>` for collection operations
- LINQ methods: `ToList<T>`, `ToArray<T>`, `Count<T>` via method-group-to-delegate conversion

This ensures that when an expression calls `items.ToList()` where `items` is `IEnumerable<int>`, the AOT-compiled binary contains the `Enumerable.ToList<int>` instantiation.

## Built-In Context

Alder ships with a built-in AOT context (`AlderBuiltInContext`) that provides dispatch for common BCL types. This is registered by default: no user action needed for standard types like `string`, `int`, `DateTime`, `List<T>`, etc.

To disable the built-in context (e.g., to reduce binary size in AOT scenarios where only specific types are needed):

```csharp
o.Aot.ClearBuiltInContext();
o.Aot.UseGeneratedContext(new MyMinimalContext());
```

## LINQ Extension Method Dispatch

The generator emits `EnumerableDispatch`: a dedicated `TypedDispatch` implementation for LINQ operations. For each registered value type, it generates type-specialized dispatch for common LINQ methods (`Where`, `Select`, `OrderBy`, `GroupBy`, `Sum`, `Average`, `ToList`, `ToArray`, etc.). Lambda arguments are converted via generated helper methods (`AsPredicate<T>`, `AsProjection<T>`) that wrap `LambdaValue` objects in strongly-typed delegates without reflection.

The LINQ dispatch is driven by a `LinqMethodDescriptor` table that categorizes methods by shape. `Filter` (predicate), `Projection` (selector), `ScalarAggregate` (numeric reduction), `IntArg` (skip/take), `ValueArg` (contains), etc. Each shape has its own emission template in `ExtensionMethodEmitter`.

## What the Generator Does NOT Handle

The generator skips:
- **Generic methods**: Only non-generic methods are emitted in the dispatch code. Generic method calls (like `Enumerable.Select<T, TResult>`) go through the reflection path with pre-rooted instantiations.
- **`ref`/`in`/`params` parameters**: Methods with ref-kind parameters are skipped.
- **`ref` return types**: Methods that return by ref are skipped.
- **Pointer types and ref-like types** (`Span<T>`, `ReadOnlySpan<T>`): Parameters or returns with these types are skipped.

These limitations are by design: the AOT path handles the common case (property access, simple method calls), and the reflection path handles everything else. The two-tier model means no user-visible functionality is lost.
