# AlderOptions

`AlderOptions` is the configuration surface for `AlderEngine`. Language mode, security boundaries, execution limits, type access, extensibility — everything is set here before the engine is constructed. Once the constructor returns, the configuration is frozen. Nothing can be changed afterward.

```csharp
var engine = new AlderEngine(o =>
{
    o.LanguageMode = LanguageMode.Standard;
    o.UseCompiler();
    o.Sandbox = SandboxOptions.Safe();
    o.Constraints = new ExecutionConstraints { MaxTimeout = TimeSpan.FromSeconds(5) };
    o.Types.AddNamespace("System.Text");
    o.Modules.Register<MyCalculator>("Calc");
    o.Functions.Register("lookup", args => db.Find((string)args[0]!));
});
```

## Language Mode

| Mode | Behavior |
|------|----------|
| `LanguageMode.Standard` | C# expression semantics per ECMA-334 (default) |
| `LanguageMode.Extended` | Superset — adds comparison chaining, pipeline operators, built-in aggregates, ranges, slicing, `let..in`, `it` iterator, date/time sugar |

```csharp
// Standard: strict C# semantics
var standard = new AlderEngine();
standard.Evaluate<int>("Math.Max(10, 20)"); // 20

// Extended: additional operators and syntax
var extended = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);
extended.Evaluate<bool>("1 < x < 10", new { x = 5 }); // true — comparison chaining
```
<!-- test: Options_LanguageMode.csx -->

Standard mode is the default. Extended mode is a strict superset — every valid Standard expression is also valid in Extended mode. If you're building a rule engine or data pipeline where users expect familiar shortcuts, Extended mode provides them. If you're evaluating C# code and want spec-exact behavior, stay in Standard.

## Sandbox

The sandbox controls which runtime operations expressions can perform. Three presets cover the common security postures:

| Preset | Method calls | Property read | Static access | Assignment | Construction | Best for |
|--------|-------------|---------------|--------------|------------|-------------|----------|
| `Trusted()` | Yes | Yes | Yes | Yes | Yes | Internal tools, trusted code |
| `Safe()` | No | Yes | No | Yes | No | User-facing formulas, rule engines |
| `Strict()` | No | Yes | No | No | No | Read-only data access, templates |

```csharp
// Trusted: full access (default)
var engine = new AlderEngine();

// Safe: property access and assignment, no method calls or construction
var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Safe());

// Strict: read-only — property reads only
var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Strict());
```
<!-- test: Options_Sandbox_Trusted.csx -->

### What the sandbox does NOT restrict

Regardless of sandbox mode, expressions can always use:

- **Registered modules** — `Math.Abs(-5)`, `Convert.ToInt32(3.7)`, and any custom module you register. Module methods and properties bypass sandbox checks entirely because you explicitly chose to expose them.
- **LINQ extension methods** — `.Where()`, `.Select()`, `.Sum()`, `.OrderBy()` and all other `Enumerable` extensions. Extension method dispatch is always allowed.
- **Registered functions** — delegate-based functions registered via `o.Functions.Register()`.
- **Lambda definitions and invocation** — `var fn = (x) => x * 2; fn(5)`.
- **Arithmetic, string operations, comparisons** — operators and literal manipulation don't trigger sandbox checks.

This design means `Safe()` mode doesn't cripple the evaluation — users can still do meaningful computation through LINQ, modules, and registered functions. What they *can't* do is call arbitrary instance methods (`.ToString()`, `.Add()`, `.Clear()`), construct objects (`new Process()`), or access static members on arbitrary types (`Environment.Exit()`).

### `Safe()` in detail

`Safe()` is the right starting point for evaluating user-supplied expressions. It allows the operations needed for data access and computation while blocking the operations that could mutate external state or escape the sandbox:

```csharp
var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Safe());
engine.SetVariable<List<int>>("items", new List<int> { 1, 2, 3, 4, 5 });

// These all work in Safe mode:
engine.Evaluate("items.Where(x => x > 2).Sum()");    // LINQ — always allowed
engine.Evaluate("items[0]");                           // index read — allowed
engine.Evaluate("items.Count");                        // property read — allowed
engine.Evaluate("Math.Abs(-5)");                       // module method — always allowed

// These are blocked:
engine.Evaluate("items.Add(6)");       // ALDR0100 — method call blocked
engine.Evaluate("items.Clear()");      // ALDR0100 — method call blocked
engine.Evaluate("items.ToString()");   // ALDR0100 — method call blocked
engine.Evaluate("int.MaxValue");       // ALDR0104 — static property blocked
engine.Evaluate("new List<int>()");    // ALDR0106 — construction blocked
```

### `Strict()` in detail

`Strict()` is for pure read-only access — template rendering, data display, read-only formulas. It allows property reads but blocks assignment, method calls, and mutation of any kind:

```csharp
var engine = new AlderEngine(o => o.Sandbox = SandboxOptions.Strict());
engine.SetVariable("order", new { Total = 150.0, Status = "Shipped" });

// Works:
engine.Evaluate("order.Total");                       // property read
engine.Evaluate("order.Status");                      // property read
engine.Evaluate("Math.Max(order.Total, 100)");        // module method

// Blocked:
engine.Evaluate("order.Total = 0");                   // ALDR0101 — assignment blocked
engine.Evaluate("order.Status.ToUpper()");            // ALDR0100 — method call blocked
```

### Customizing sandbox presets

All presets are `record` types — use `with` to adjust individual flags:

```csharp
// Safe mode + allow construction (for computed collections)
o.Sandbox = SandboxOptions.Safe() with { AllowConstruction = true };

// Strict mode + allow method calls (for string operations)
o.Sandbox = SandboxOptions.Strict() with { AllowMethodCalls = true };

// Deny everything (only modules, LINQ, and registered functions work)
o.Sandbox = new SandboxOptions();
```

### Permission flags

| Flag | Controls | Diagnostic on violation |
|------|----------|------------------------|
| `AllowMethodCalls` | Instance method calls (`.ToUpper()`, `.Add()`) | ALDR0100 |
| `AllowPropertyRead` | Instance property reads (`.Length`, `.Count`) | ALDR0103 |
| `AllowStaticPropertyRead` | Static property reads (`int.MaxValue`) | ALDR0104 |
| `AllowStaticFieldRead` | Static field reads (`double.NaN`) | ALDR0104 |
| `AllowAssignment` | Variable assignment (`=`, `+=`, `++`, `--`, `??=`) | ALDR0101 |
| `AllowPropertySet` | Property setters (`obj.Name = "x"`) | ALDR0105 |
| `AllowIndexSet` | Index setters (`list[0] = x`) | ALDR0102 |
| `AllowConstruction` | Object creation via `new` | ALDR0106 |

### Type allow/deny lists

For fine-grained control beyond the presets, restrict which types are accessible:

```csharp
o.Sandbox = SandboxOptions.Trusted() with
{
    // Only these types can be used (overrides default deny list)
    TrustedTypes = new HashSet<Type> { typeof(Console), typeof(MemoryStream) },

    // These types are blocked (overrides permissions)
    DeniedTypes = new HashSet<Type> { typeof(Environment) },

    // Entire namespaces can be allowed or denied
    TrustedNamespaces = new HashSet<string> { "System.Text" },
    DeniedNamespaces = new HashSet<string> { "System.Net" },
};
```

**Resolution order:** Hard-denied (Alder internals) → TrustedTypes/TrustedNamespaces → DeniedTypes/DeniedNamespaces → allow by default.

`TrustedTypes` overrides the default deny list. If `MemoryStream` is in your trusted set, it's accessible even though `System.IO` is denied by default. Conversely, `DeniedTypes` overrides permissions — a type in both `TrustedTypes` and `DeniedTypes` is denied.

**Hard-denied types** (always blocked, regardless of configuration):
- `AlderEngine`, `AlderOptions`, `AlderExpression` — prevents sandbox escape.

**Default denied types** (active unless overridden by TrustedTypes):
- `Activator`, `AppDomain`, `Console`, `Delegate`, `Environment`, `GC`, `WeakReference`
- `Thread`, `ThreadPool`, `Process`, `ProcessStartInfo`, `Marshal`

**Default denied namespaces:**
- `System.IO`, `System.Net`, `System.Threading`, `System.Diagnostics`
- `System.Reflection`, `System.Runtime.InteropServices`, `System.Security`
- `System.Data`, `System.CodeDom`, `System.Linq.Expressions`
- And others — see `SecurityPolicy.DefaultDeniedNamespaces` for the full list.

## Execution Constraints

`ExecutionConstraints` prevents runaway expressions — infinite loops, combinatorial explosions, or expressions that simply take too long. In any system evaluating untrusted input, these limits are non-negotiable.

```csharp
o.Constraints = new ExecutionConstraints
{
    MaxStatements = 10_000,         // null = unlimited
    MaxLoopIterations = 1_000,      // null = unlimited
    MaxTimeout = TimeSpan.FromSeconds(5),  // null = unlimited
};
```
<!-- test: Options_Constraints.csx -->

| Property | Type | Default | Diagnostic on violation |
|----------|------|---------|------------------------|
| `MaxStatements` | `long?` | `null` (unlimited) | ALDR0200 |
| `MaxLoopIterations` | `long?` | `null` (unlimited) | ALDR0201 |
| `MaxTimeout` | `TimeSpan?` | `null` (unlimited) | ALDR0203 |

When a limit is hit, `Evaluate` throws `AlderExecutionLimitException` (a subclass of `AlderException`) with the corresponding diagnostic code. `TryEvaluate` returns `false`.

`MaxStatements` counts every statement executed — each iteration of a loop body, each branch of an `if`, each expression statement. A `for` loop with 100 iterations running 3 statements per iteration consumes 300 statements.

`MaxLoopIterations` caps a single loop construct. A `while(true)` that runs 1,001 times when the limit is 1,000 triggers immediately — it doesn't wait for the outer statement limit.

## Types

By default, expressions can use:
- **C# keywords**: `int`, `string`, `bool`, `double`, `object`, `decimal`, `char`, and all other built-in type keywords including nullable forms (`int?`, `string?`).
- **System namespace**: `Math`, `Convert`, `DateTime`, `Guid`, `Random`, `TimeSpan`, `Array`, `Tuple`, etc.
- **System.Collections.Generic**: `List<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`, etc.
- **System.Linq**: `Enumerable` — all LINQ extension methods are available.
- **System.Text.RegularExpressions**: `Regex` — but only via fully qualified name (the namespace isn't imported by default).
- **Fully qualified names**: any type from loaded assemblies can be used by its full name, e.g. `new System.Text.StringBuilder()`.

```csharp
var engine = new AlderEngine();

// Works without configuration:
engine.Evaluate("new List<int> { 1, 2, 3 }");
engine.Evaluate("new Dictionary<string, int> { [\"a\"] = 1 }");
engine.Evaluate("Math.PI");
engine.Evaluate("new System.Text.StringBuilder(\"hello\")");

// Fails without AddNamespace — StringBuilder isn't in a default namespace:
// engine.Evaluate("new StringBuilder()"); // CS0246
```

### `Types.AddNamespace`

Import a namespace so its types can be used without qualification:

```csharp
var engine = new AlderEngine(o => o.Types.AddNamespace("System.Text"));

// Now works without fully qualified name:
engine.Evaluate("new StringBuilder(\"hello\").Append(\" world\").ToString()");
// "hello world"
```
<!-- test: Options_TypeBuilder_Namespace.csx -->

### `Types.AddAssembly`

Make types from an external assembly available. Types can then be used via fully qualified name, or unqualified if their namespace is also imported:

```csharp
o.Types.AddAssembly(typeof(MyDomain.Order).Assembly);
o.Types.AddNamespace("MyDomain");
// Now: engine.Evaluate("new Order()")
```
<!-- test: Types_Assembly.csx -->

### `Types.AddExtensionMethods`

Register additional static types whose extension methods become available on matching types. `System.Linq.Enumerable` is included by default — you don't need to register it.

```csharp
o.Types.AddExtensionMethods<MyStringExtensions>();
// Now: engine.Evaluate("name.Reverse()") — if MyStringExtensions has a Reverse(this string) method
```
<!-- test: Types_ExtensionMethods.csx -->

## Modules

Modules expose .NET classes to expressions as named objects with methods and properties. `Math` and `Convert` are registered as built-in modules by default — `Math.Abs()`, `Math.PI`, `Convert.ToInt32()` all work without configuration.

Module methods and properties are always accessible regardless of sandbox mode. This is by design — you register a module because you want expressions to use it.

### Registering a module

```csharp
var engine = new AlderEngine(o =>
    o.Modules.Register<PricingEngine>("Pricing"));

// Expressions access it as Pricing.MethodName(args)
engine.Evaluate("Pricing.CalculateDiscount(100.0, 0.15)");
```
<!-- test: Modules_MemberAccess.csx -->

All public methods, properties, and fields are exposed by default. For tighter control, use `explicitOnly`:

```csharp
o.Modules.Register<PricingEngine>("Pricing", explicitOnly: true);
// Only methods marked with [AlderFunction] are accessible
// Unmarked methods throw CS0117 (member not found)
```
<!-- test: Modules_ExplicitOnly.csx -->

### Attribute-based registration

Decorate classes with `[AlderModule]` and methods with `[AlderFunction]` for self-describing registration:

```csharp
[AlderModule("Calc")]
public class CalculatorModule
{
    public double Add(double a, double b) => a + b;

    [AlderFunction("mul")]  // custom name in expressions
    public double Multiply(double a, double b) => a * b;
}

// Register via attribute scanning:
var engine = new AlderEngine(o => o.Modules.RegisterFromType<CalculatorModule>());
engine.Evaluate("Calc.Add(1.5, 2.5)");   // 4.0
engine.Evaluate("Calc.mul(3.0, 4.0)");   // 12.0
```
<!-- test: Modules_AttributeBased.csx -->

When `[AlderModule(ExplicitOnly = true)]` is set on the class, only methods with `[AlderFunction]` are exposed — others throw CS0117.

### Global functions from attributes

Types without `[AlderModule]` that contain `[AlderFunction]` methods are registered as global functions — callable directly by name without a module prefix:

```csharp
public class Utilities
{
    [AlderFunction("triple")]
    public long Triple(long value) => value * 3;
}

var engine = new AlderEngine(o => o.Modules.RegisterFromType<Utilities>());
engine.Evaluate("triple(4)"); // 12
```

### Assembly scanning

Register all attributed types from an assembly in one call:

```csharp
o.Modules.RegisterFromAssembly(typeof(MyModule).Assembly);
// Scans for [AlderModule] and [AlderFunction] attributes
```

### Module instance resolution

When a module method is called, the engine resolves an instance in this order:

1. **Pre-created instance** — if you passed one during registration (`Register<T>("Name", instance: myInstance)`).
2. **Service provider** — if `o.ServiceProvider` is set, the engine calls `GetService(typeof(T))`.
3. **Parameterless constructor** — the engine creates one via `Activator.CreateInstance`.

This means modules integrate naturally with dependency injection:

```csharp
o.ServiceProvider = myServiceProvider;
o.Modules.RegisterFromType<MyDatabaseModule>();
// Engine resolves MyDatabaseModule from DI on each call
```

## Functions

Functions are the simplest extensibility point — register a delegate that receives arguments as `object?[]`:

```csharp
var engine = new AlderEngine(o =>
{
    o.Functions.Register("clamp", args =>
    {
        var value = Convert.ToDouble(args[0]);
        var min = Convert.ToDouble(args[1]);
        var max = Convert.ToDouble(args[2]);
        return Math.Max(min, Math.Min(max, value));
    });
});

engine.Evaluate<double>("clamp(150, 0, 100)"); // 100.0
```
<!-- test: Options_FunctionBuilder.csx -->

Registered functions are always callable regardless of sandbox mode — like modules, they're explicitly trusted by the host application.

The trade-off vs modules: functions are simpler to register (just a lambda) but don't support overloading, property access, or member discovery. For anything beyond a single callable, use a module.

## AOT

The `Aot` builder configures ahead-of-time compiled type metadata generated by the Alder source generator. This metadata enables reflection-free member access and method dispatch — critical for NativeAOT, IL2CPP (Unity), and performance-sensitive paths.

```csharp
// Built-in context is registered by default — covers BCL types
// Add your own generated context for custom types:
o.Aot.UseGeneratedContext(MyAppTypeContext.Default);
```

The built-in context covers common BCL types (`string`, `int`, `List<T>`, `Dictionary<TKey, TValue>`, etc.). For your own domain types, the source generator creates type-specific dispatch code at compile time.

To disable AOT dispatch entirely and fall back to pure reflection:

```csharp
o.Aot.ClearBuiltInContext();
```

## Case Sensitivity

```csharp
o.IsCaseSensitive = false; // default: true
```

When disabled, identifier resolution (`variable`, `Variable`, `VARIABLE`) is case-insensitive. This applies to variable names, module names, function names, and type resolution. Operators and keywords are always case-insensitive regardless of this setting.

## Service Provider

```csharp
o.ServiceProvider = myServiceProvider;
```

An `IServiceProvider` used for dependency injection when resolving module instances. When a module method is called and no pre-created instance was provided during registration, the engine calls `ServiceProvider.GetService(typeof(T))` before falling back to parameterless construction.

## Compiled Evaluation

```csharp
o.UseCompiler();
```

Enables IL compilation. When configured, `Evaluate` automatically compiles expressions to IL on first execution and caches the delegate. See [AlderEngine — Compiled Evaluation](alder-engine.md#compiled-evaluation) for the full compilation API.

`UseCompiler()` requires a JIT compiler — it throws `PlatformNotSupportedException` on NativeAOT and IL2CPP platforms. On those platforms, the interpreter with AOT metadata provides the best performance.

## Full Property Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LanguageMode` | `LanguageMode` | `Standard` | C# expression semantics or Extended superset |
| `IsCaseSensitive` | `bool` | `true` | Whether identifiers are case-sensitive |
| `Sandbox` | `SandboxOptions` | `Trusted()` | Runtime security policy |
| `Constraints` | `ExecutionConstraints` | Unlimited | Statement, loop, and timeout limits |
| `ServiceProvider` | `IServiceProvider?` | `null` | DI container for module instance resolution |
| `Modules` | `ModuleBuilder` | — | Register module types |
| `Functions` | `FunctionBuilder` | — | Register standalone functions |
| `Types` | `TypeBuilder` | — | Configure assemblies, namespaces, extension methods |
| `Aot` | `AotBuilder` | Built-in context | Configure AOT type metadata |
