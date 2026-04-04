Two mechanisms for extending what expressions can call: **standalone functions** (delegates) and **modules** (class-backed objects with methods, properties, fields).

## Standalone Functions

Register via `AlderOptions.Functions`:

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

Arguments arrive as `object?[]`. Functions are resolved before variables: if a function and variable share the same name, the function wins.

## Modules

A module exposes a class's public members to expressions via `moduleName.Member()`:

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

All public methods, properties, and fields declared directly on the type are exposed. Inherited members are excluded.

### Built-in modules

`Math` and `Convert` are registered by default.

### Explicit-only mode

With `explicitOnly: true`, only methods marked `[AlderFunction]` are exposed:

```csharp
[AlderModule("secure", ExplicitOnly = true)]
public class SecureModule
{
    [AlderFunction]
    public string Hash(string input) => Convert.ToBase64String(
        System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(input)));

    // Not exposed (no [AlderFunction])
    public string InternalMethod() => "hidden";
}

var engine = new AlderEngine(o =>
{
    o.Modules.RegisterFromType<SecureModule>();
});

// secure.Hash("hello") works
// secure.InternalMethod() produces CS0117
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

Expressions call `module.fmt(100)` instead of `module.FormatCurrency(100)`.

### Global functions from types

When a type registered via `RegisterFromType` has no `[AlderModule]` attribute, its `[AlderFunction]` methods become global functions (callable without a module prefix):

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

For instance methods, the engine resolves an instance in order:

1. **Explicit instance** provided at registration: `Register<T>("name", instance: myInstance)`
2. **Service provider**: `serviceProvider.GetService(moduleType)` if `AlderOptions.ServiceProvider` is configured
3. **Activator**: `Activator.CreateInstance(moduleType)` (requires public parameterless constructor)

If none succeeds: `ALDR0315`.

Static modules (`static class` in C#) skip instance resolution entirely.

### Assembly scanning

```csharp
o.Modules.RegisterFromAssembly(typeof(MyModule).Assembly);
```

For each type found:
- `[AlderModule]` present: registered as a named module
- `[AlderFunction]` methods without `[AlderModule]`: registered as global functions
- Abstract types, interfaces, and types without parameterless constructors are skipped

### Module methods and security

Module methods bypass the `AllowMethodCalls` sandbox check. Modules are registered by the host application and treated as trusted. Extension methods (LINQ) also bypass this check. Both are still guarded against reflection leaks.

### Method resolution

Module methods use overload resolution per call. Both instance and static methods on the module type are available simultaneously.
