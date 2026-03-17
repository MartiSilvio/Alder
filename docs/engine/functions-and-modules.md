---
title: "Functions and Modules"
description: "Register custom functions, organized modules, and assembly-scanned types with the CsEval engine."
sidebar:
  order: 4
---

## Overview

CsEval ships with built-in `Math` and `Convert` modules, but most applications need custom logic. The engine provides four registration paths:

| Method | What it does |
|--------|-------------|
| `RegisterFunction` | Adds a single callable function by name |
| `RegisterModule` | Groups a class's methods under a namespace prefix |
| `RegisterFromType` | Exposes a class's methods as top-level functions (no prefix) |
| `RegisterFromAssembly` | Scans an assembly for `[CsEvalModule]`-decorated classes |

All registration methods return `this` for fluent chaining and must be called **before the first `Evaluate()`**. After evaluation begins, the engine configuration freezes and registration throws `InvalidOperationException`.

## RegisterFunction

Registers a standalone function callable by name in expressions.

```csharp
public CsEvalEngine RegisterFunction(string name, Func<object?[], object?> function)
```

The delegate receives all arguments as an `object?[]` and returns `object?`.

```csharp
var engine = new CsEvalEngine()
    .RegisterFunction("add", args => (int)args[0]! + (int)args[1]!);

var result = engine.Evaluate<int>("add(3, 4)");
// result: 7
```

Functions registered this way are always callable regardless of sandbox settings -- they bypass the `AllowMethodCalls` flag.

## RegisterModule

Groups a class's public methods under a named prefix. Expressions call them as `moduleName.methodName()`.

### Generic overload

```csharp
public CsEvalEngine RegisterModule<T>(
    string moduleName,
    bool explicitOnly = false,
    T? instance = default) where T : class
```

```csharp
public class StringUtils
{
    public string Reverse(string s) => new(s.Reverse().ToArray());
    public string Upper(string s) => s.ToUpper();
}

var engine = new CsEvalEngine()
    .RegisterModule<StringUtils>("Str");

var result = engine.Evaluate<string>(@"Str.Upper(""hello"")");
// result: "HELLO"
```

When `instance` is `null` (the default), the engine creates an instance via the parameterless constructor on first use.

### Non-generic overload

```csharp
public CsEvalEngine RegisterModule(
    string moduleName,
    Type type,
    bool explicitOnly = false,
    object? instance = null)
```

Same behavior as the generic overload, accepting a `Type` directly.

### Explicit member map overload

```csharp
public CsEvalEngine RegisterModule(
    string moduleName,
    Type type,
    IReadOnlyDictionary<string, MemberInfo> members)
```

Registers a module with an explicit dictionary of exposed members. Useful when you need fine-grained control over which members are available and under what names.

### The `explicitOnly` parameter

When `explicitOnly` is `true`, only methods decorated with `[CsEvalFunction]` are exposed to expressions. All other public methods are hidden.

```csharp
public class Selective
{
    [CsEvalFunction]
    public int Allowed() => 1;

    public int Hidden() => 2;
}

var engine = new CsEvalEngine()
    .RegisterModule<Selective>("sel", explicitOnly: true);

var result = engine.Evaluate<int>("sel.Allowed()");
// result: 1

// sel.Hidden() would throw -- not exposed
```

The `ExplicitOnly` property can also be set on the `[CsEvalModule]` attribute itself, and `RegisterModule` respects it when `explicitOnly` is not explicitly passed as `true`.

## RegisterFromType

Registers methods decorated with `[CsEvalFunction]` from a type as top-level functions, callable without any module prefix.

```csharp
public CsEvalEngine RegisterFromType<T>(T? instance = default) where T : class
public CsEvalEngine RegisterFromType(Type type, object? instance = null)
```

```csharp
public class Helpers
{
    [CsEvalFunction]
    public int Double(int n) => n * 2;

    [CsEvalFunction]
    public int Triple(int n) => n * 3;
}

var engine = new CsEvalEngine()
    .RegisterFromType<Helpers>();

var result = engine.Evaluate<int>("Double(5)");
// result: 10
```

Only methods with the `[CsEvalFunction]` attribute are registered as global functions. Methods without the attribute are ignored. If the type has a `[CsEvalModule]` attribute with a name, it is registered as a named module instead (exposing all public methods or only attributed ones based on `ExplicitOnly`).

## RegisterFromAssembly

Scans an assembly for classes decorated with `[CsEvalModule]` or containing methods with `[CsEvalFunction]`, and registers them automatically.

```csharp
[RequiresUnreferencedCode("Registering from assembly scans all types and members via reflection.")]
public CsEvalEngine RegisterFromAssembly(Assembly assembly)
```

```csharp
engine.RegisterFromAssembly(typeof(MyModule).Assembly);
```

Classes with `[CsEvalModule]` are registered as named modules. Classes without the attribute but containing `[CsEvalFunction]` methods have those methods registered as global functions. Classes must have a parameterless constructor (unless all attributed methods are static).

:::note
This method carries `[RequiresUnreferencedCode]` because it uses reflection to scan assembly types. It is not compatible with Native AOT trimming. For AOT scenarios, use explicit registration or `UseGeneratedContext()`.
:::

## Attributes

### [CsEvalModule]

Marks a class as a module discoverable by `RegisterFromAssembly`.

```csharp
[CsEvalModule("fmt")]
public class Formatter
{
    public string Currency(double amount) => amount.ToString("C");
}
```

Properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string?` | `null` | Module name in expressions. When `null`, the name comes from the `RegisterModule` call. |
| `ExplicitOnly` | `bool` | `false` | When `true`, only `[CsEvalFunction]`-decorated methods are exposed. |

### [CsEvalFunction]

Marks a method for exposure. Used with `explicitOnly` modules or for global function registration via `RegisterFromAssembly`.

```csharp
[CsEvalFunction]           // uses method name
[CsEvalFunction("greet")]  // custom name in expressions
```

```csharp
[CsEvalModule("tools", ExplicitOnly = true)]
public class Tools
{
    [CsEvalFunction("hi")]
    public string Greet(string name) => $"Hello, {name}!";

    public string Secret() => "not visible";
}

var engine = new CsEvalEngine()
    .RegisterFromAssembly(typeof(Tools).Assembly);

var result = engine.Evaluate<string>(@"tools.hi(""World"")");
// result: "Hello, World!"
```

## Built-in Modules

Two modules are always registered and available without any setup:

| Module | Backing type | Examples |
|--------|-------------|----------|
| `Math` | `System.Math` | `Math.Abs(-5)`, `Math.Max(1, 2)`, `Math.Round(3.7)` |
| `Convert` | `System.Convert` | `Convert.ToInt32("42")`, `Convert.ToDouble("3.14")` |

```csharp
var engine = new CsEvalEngine();

var abs = engine.Evaluate<int>("Math.Abs(-5)");
// abs: 5

var num = engine.Evaluate<int>(@"Convert.ToInt32(""42"")");
// num: 42
```

## IServiceProvider Integration

All `Evaluate` overloads accept an optional `IServiceProvider?` parameter. When a module instance is `null` at registration time, the engine attempts to resolve it through the service provider before falling back to parameterless construction.

```csharp
var services = new ServiceCollection()
    .AddSingleton<MyService>()
    .BuildServiceProvider();

var engine = new CsEvalEngine()
    .RegisterModule<MyService>("svc");

var result = engine.Evaluate<string>(
    @"svc.GetName()",
    serviceProvider: services);
```

## GetRegisteredModules

Returns all registered modules for inspection or debugging.

```csharp
public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
```

```csharp
var engine = new CsEvalEngine()
    .RegisterModule<StringUtils>("Str");

var modules = engine.GetRegisteredModules();
// Contains "Math", "Convert", and "Str"
```

Calling `GetRegisteredModules()` triggers the configuration freeze, just like `Evaluate()`.

## Fluent Chaining

All registration methods return `this`, enabling concise setup:

```csharp
var engine = new CsEvalEngine()
    .RegisterFunction("add", args => (int)args[0]! + (int)args[1]!)
    .RegisterModule<StringUtils>("Str")
    .RegisterFromType<MathHelpers>()
    .SetVariable("x", 10);
```

## Pre-freeze Requirement

Registration methods must be called before the first `Evaluate()`. After evaluation, the engine freezes its configuration:

```csharp
var engine = new CsEvalEngine();
engine.Evaluate("1 + 1"); // freezes the engine

// This throws InvalidOperationException:
engine.RegisterFunction("late", args => 0);
```

## See Also

- [Type Registration](./type-registration.md) -- making host types available for construction and static access
