---
title: "AlderOptions"
description: "Configuration — language mode, sandbox, constraints, compiler, sub-builders"
sidebar:
  order: 3
---

`AlderOptions` configures an `AlderEngine` instance. Configuration is captured at construction time and frozen — the engine is immutable after creation.

```csharp
var engine = new AlderEngine(o =>
{
    o.LanguageMode = LanguageMode.Extended;
    o.IsCaseSensitive = false;
    o.Sandbox = SandboxOptions.Safe();
    o.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxLoopIterations = 1_000,
        MaxTimeout = TimeSpan.FromSeconds(5)
    };
    o.Types.AddNamespace("System.IO");
    o.Functions.Register("double", args => Convert.ToDouble(args![0]) * 2);
    o.Modules.Register<MyModule>("myMod");
    o.UseCompiler();
});
```

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LanguageMode` | `LanguageMode` | `Standard` | `Standard` for ECMA-334 C# semantics. `Extended` adds operators, sugar, bare math, aggregates — see [Extended Mode](../language/extended.md). |
| `IsCaseSensitive` | `bool` | `true` | Controls variable names, function names, module names, and member resolution. When `false`, uses `StringComparer.OrdinalIgnoreCase`. |
| `Sandbox` | `SandboxOptions` | `Trusted()` | Security policy — permission flags and type/namespace blocking. See [Security](../security/sandbox.md). |
| `Constraints` | `ExecutionConstraints` | No limits | Statement count, loop iteration, and timeout limits. See [Security](../security/sandbox.md#execution-limits). |
| `ExpressionCompiler` | `IExpressionCompiler` | `DefaultExpressionCompiler` | Controls how LINQ expression trees are compiled to delegates. Implement `IExpressionCompiler` to substitute an alternative backend. The user is responsible for ensuring the replacement supports all required semantics. |
| `ServiceProvider` | `IServiceProvider?` | `null` | DI container for resolving module instances. When a module is registered without an explicit instance, the engine tries `ServiceProvider.GetService(moduleType)` before falling back to `Activator.CreateInstance`. |

## Sub-builders

### `Types` — Type Registration

Controls which types are available to expressions.

```csharp
o.Types.AddNamespace("System.IO");          // types in System.IO available without FQN
o.Types.AddAssembly(typeof(MyType).Assembly); // types from this assembly available via FQN
o.Types.AddExtensionMethods<MyExtensions>();  // extension methods from MyExtensions available
```

| Method | Description |
|--------|-------------|
| `AddNamespace(string)` | Makes types in this namespace available without fully qualified names |
| `AddAssembly(Assembly)` | Makes types from this assembly available via fully qualified names |
| `AddExtensionMethods(Type)` | Registers a static type's extension methods. `System.Linq.Enumerable` is registered by default. |
| `AddExtensionMethods<T>()` | Generic overload of the above |

See [Type Registration](type-registration.md) for details.

### `Functions` — Standalone Functions

Registers delegate-based functions callable by name from expressions.

```csharp
o.Functions.Register("clamp", args =>
{
    var value = Convert.ToDouble(args![0]);
    var min = Convert.ToDouble(args[1]);
    var max = Convert.ToDouble(args[2]);
    return Math.Min(Math.Max(value, min), max);
});
```

| Method | Description |
|--------|-------------|
| `Register(string name, Func<object?[], object?> function)` | Registers a function by name. Arguments arrive as `object?[]`. |

Functions are resolved before variables — if a function and a variable have the same name, the function wins.

### `Modules` — Class-Backed Modules

Registers classes whose public methods and properties are exposed to expressions via `moduleName.Method()`.

```csharp
o.Modules.Register<MathUtils>("utils");
// Expressions can now call: utils.Add(1, 2), utils.Pi, etc.
```

| Method | Description |
|--------|-------------|
| `Register<T>(string name, bool explicitOnly = false, T? instance = default)` | Register type `T` under the given name |
| `Register(string name, Type type, bool explicitOnly = false, object? instance = null)` | Non-generic overload |
| `RegisterFromType<T>(T? instance = default)` | Uses `[AlderModule]` attribute for the name, or registers methods as global functions |
| `RegisterFromType(Type, object?)` | Non-generic overload |
| `RegisterFromAssembly(Assembly)` | Scans assembly for `[AlderModule]` and `[AlderFunction]` decorated types |

When `explicitOnly = true`, only methods marked with `[AlderFunction]` are exposed. Otherwise, all public methods are available.

Module instances are resolved in order:
1. Explicit `instance` parameter (if provided)
2. `ServiceProvider.GetService(moduleType)` (if configured)
3. `Activator.CreateInstance(moduleType)` (requires parameterless constructor)

See [Functions and Modules](functions-and-modules.md) for details.

### `Aot` — AOT Configuration

Registers source-generator-produced type metadata for reflection-free dispatch.

```csharp
o.Aot.UseGeneratedContext(MyTypeContext.Instance);
```

| Method | Description |
|--------|-------------|
| `UseGeneratedContext(AlderTypeContext)` | Registers an AOT context produced by the source generator |
| `ClearBuiltInContext()` | Removes the default built-in AOT context, falling back to reflection only |

See [AOT Overview](../aot/overview.md) for details.

## `UseCompiler()`

Extension method from `Alder.Compiled` that enables IL compilation:

```csharp
o.UseCompiler();                                          // default Expression.Compile()
o.UseCompiler(new FastExpressionCompilerAdapter());       // third-party backend (user provides)
```

Alder does not ship third-party compiler backends. See [Compilation — Swapping the Expression Compiler](compilation.md#swapping-the-expression-compiler) for how to implement an adapter.

On NativeAOT platforms, `UseCompiler()` throws `PlatformNotSupportedException`. See [Compilation](compilation.md).

## `SandboxOptions`

A `record` with `init` properties. Use factory methods or `with` expressions:

```csharp
o.Sandbox = SandboxOptions.Trusted();  // all permissions
o.Sandbox = SandboxOptions.Safe();     // property reads, static field reads, assignment, property/index writes — no methods or construction
o.Sandbox = SandboxOptions.Strict();   // property reads (instance and static) and static field reads only

// Customize from a preset
o.Sandbox = SandboxOptions.Safe() with
{
    AllowMethodCalls = true,
    TrustedTypes = new HashSet<Type> { typeof(System.IO.MemoryStream) }
};
```

`SandboxOptions` also carries two resource limits:

| Property | Default | Description |
|----------|---------|-------------|
| `MaxCollectionSize` | 10,000,000 | Maximum size for arrays and collections |
| `RegexTimeout` | 1 second | Maximum duration for regex operations (`=~`, `!~`) |

See [Security](../security/sandbox.md) for the full permission matrix.

## `ExecutionConstraints`

A `record` with `init` properties:

```csharp
o.Constraints = new ExecutionConstraints
{
    MaxStatements = 10_000,          // total statements before ALDR0200
    MaxLoopIterations = 1_000,       // per-loop iteration cap before ALDR0203
    MaxTimeout = TimeSpan.FromSeconds(5) // wall-clock timeout before ALDR0201
};
```

All three are `null` by default (unlimited). When a limit is exceeded, `AlderExecutionLimitException` is thrown with the specific limit type, configured value, and actual value.

## `IExpressionCompiler`

```csharp
public interface IExpressionCompiler
{
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}
```

The default `DefaultExpressionCompiler` calls `expression.Compile()`. Implement this interface to use an alternative LINQ expression tree compiler. It is the user's responsibility to ensure the replacement backend supports all expression node types that Alder emits.

## `LanguageMode`

| Value | Description |
|-------|-------------|
| `Standard` | ECMA-334 C# expression and statement semantics |
| `Extended` | Superset — adds `**`, `\|>`, `<=>`, `===`, `in`, `like`, `between`, `..=`, `..<`, `[...]`, comprehensions, `let..in`, `if-else` expressions, `unless`/`until`, `and`/`or`/`not` word operators, bare math functions, aggregate built-ins, date/time sugar, implicit `it` |

Using Extended features in Standard mode throws `ALDR0020`.
