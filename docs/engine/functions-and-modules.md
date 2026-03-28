---
title: "Functions and Modules"
description: "Delegate functions, class-backed modules, attributes, assembly scanning"
sidebar:
  order: 6
---

Alder provides two mechanisms for extending what expressions can call: **standalone functions** (simple delegates) and **modules** (class-backed objects with methods, properties, and fields).

## Standalone Functions

Register a delegate-based function via `AlderOptions.Functions`:

```csharp
var engine = new AlderEngine(o =>
{
    o.Functions.Register("clamp", args =>
    {
        var value = Convert.ToDouble(args![0]);
        var min = Convert.ToDouble(args[1]);
        var max = Convert.ToDouble(args[2]);
        return Math.Min(Math.Max(value, min), max);
    });
});

double result = engine.Evaluate<double>("clamp(150, 0, 100)"); // 100.0
```

<!-- test: Functions_Register -->

Functions receive arguments as `object?[]`. The engine handles numeric coercion for arguments that match a parameter's type but need conversion (e.g., passing `int` where `double` is expected). If fewer arguments are provided than the function expects and the remaining parameters have default values, defaults are filled in.

Functions are resolved before variables in `IdentifierRuntime` — if a function and a variable share the same name, the function wins.

## Modules

A module exposes a class's public members to expressions via `moduleName.Member()` syntax:

```csharp
public class MathUtils
{
    public double CircleArea(double radius) => Math.PI * radius * radius;
    public double Hypotenuse(double a, double b) => Math.Sqrt(a * a + b * b);
    public double Pi => Math.PI;
}

var engine = new AlderEngine(o =>
{
    o.Modules.Register<MathUtils>("utils");
});

double area = engine.Evaluate<double>("utils.CircleArea(5)");   // ~78.54
double pi = engine.Evaluate<double>("utils.Pi");                // ~3.14159
```

<!-- test: Modules_Register -->

By default, all public methods, properties, and fields (excluding special-name methods like property getters and async methods) are exposed. Properties and fields are read-only through module access.

### Built-in modules

`Math` and `Convert` are registered as modules by default — this is why `Math.Round(3.14)` and `Convert.ToInt32("42")` work without any configuration.

### Explicit-only mode

When `explicitOnly: true`, only methods marked with `[AlderFunction]` are exposed:

```csharp
[AlderModule("secure", ExplicitOnly = true)]
public class SecureModule
{
    [AlderFunction]
    public string Hash(string input) => Convert.ToBase64String(
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input)));

    // Not exposed — no [AlderFunction] attribute
    public string InternalMethod() => "hidden";
}

var engine = new AlderEngine(o =>
{
    o.Modules.RegisterFromType<SecureModule>();
});

// secure.Hash("hello") — works
// secure.InternalMethod() — CS0117: 'SecureModule' does not contain a definition for 'InternalMethod'
```

<!-- test: Modules_ExplicitOnly -->

`ExplicitOnly` can be set on the `[AlderModule]` attribute or as a parameter to `Register<T>()`.

### `[AlderFunction]` attribute

Controls the name exposed to expressions:

```csharp
public class Formatting
{
    [AlderFunction("fmt")]
    public string FormatCurrency(double amount) => $"${amount:N2}";
}
```

When `[AlderFunction("fmt")]` is used, the expression calls `module.fmt(100)`, not `module.FormatCurrency(100)`.

### Global functions from types

When a type is registered via `RegisterFromType` or `RegisterFromAssembly` and has no `[AlderModule]` attribute, methods marked with `[AlderFunction]` are registered as global functions (callable without a module prefix):

```csharp
public class GlobalHelpers
{
    [AlderFunction("greet")]
    public string Greet(string name) => $"Hello, {name}!";
}

var engine = new AlderEngine(o =>
{
    o.Modules.RegisterFromType<GlobalHelpers>();
});

string result = engine.Evaluate<string>("""greet("Alice")"""); // "Hello, Alice!"
```

<!-- test: Functions_GlobalFromType -->

### Module instance resolution

When a module method is called, the engine needs an instance (for instance methods). Resolution order:

1. **Explicit instance** — if provided during registration: `Register<T>("name", instance: myInstance)`
2. **Service provider** — if `AlderOptions.ServiceProvider` is configured: `serviceProvider.GetService(moduleType)`
3. **Activator** — `Activator.CreateInstance(moduleType)` (requires a public parameterless constructor)

If none succeeds, `ALDR0315: Cannot resolve module instance` is thrown.

For static modules (abstract sealed classes — i.e., `static class` in C#), no instance is needed. The binder detects this and routes calls through `TryBindStaticModuleCall`, which uses `MethodInfo.Invoke(null, args)` directly.

### Assembly scanning

`RegisterFromAssembly` scans an assembly for types with `[AlderModule]` or methods with `[AlderFunction]`:

```csharp
o.Modules.RegisterFromAssembly(typeof(MyModule).Assembly);
```

For each type found:
- If it has `[AlderModule]`, it's registered as a named module
- If it has methods with `[AlderFunction]` but no `[AlderModule]`, those methods become global functions
- Types that are abstract, interfaces, or lack a parameterless constructor (and have no static-only functions) are skipped

### Module methods bypass sandbox

Module method calls bypass the `AllowMethodCalls` permission check in `SecurityValidationPass`. This is by design — modules are registered by the host application, so they're trusted. The `BoundResolvedCallExpr.IsModuleCall` flag signals this to the security pass.

Extension methods (LINQ) also bypass the method call check for the same reason — they're registered via `Types.AddExtensionMethods`.
