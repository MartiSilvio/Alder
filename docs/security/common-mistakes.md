---
title: "Common Mistakes"
description: "Security anti-patterns with wrong/right code pairs -- avoid these pitfalls when configuring the Alder sandbox."
sidebar:
  order: 3
---

## 1. Engine-per-Request Anti-Pattern

Creating a new `AlderEngine` for every evaluation wastes resources. The engine caches parsed expressions and compilation artifacts. Creating a new instance discards all caches and forces re-initialization.

**Wrong** -- new engine per evaluation:

```csharp
foreach (var expr in expressions)
{
    var engine = new AlderEngine(new AlderOptions
    {
        Sandbox = SandboxOptions.Safe()
    });
    engine.SetVariable("input", expr);
    var result = engine.Evaluate("input.Length");
}
```

**Right** -- create once, reuse:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Safe()
});

foreach (var expr in expressions)
{
    engine.SetVariable("input", expr);
    var result = engine.Evaluate("input.Length");
}
```

`SetVariable` works both before and after the engine freezes on first evaluation, so you can safely update variables between evaluations on the same engine.

## 2. Safe() is Not Fully Sandboxed

`Safe()` blocks method calls and construction, but it **still allows** property reads, variable assignment, property set, and index set. If you need a truly read-only sandbox, use `Strict()` or a custom preset.

**Wrong** -- assuming Safe() prevents all mutation:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Safe()
});
engine.SetVariable("x", 0);

// This succeeds -- Safe() allows assignment
{ x = 999; return x; }
// output: 999
```

**Right** -- use Strict() for read-only access:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Strict()
});
engine.SetVariable("x", 0);

// Strict blocks assignment
{ x = 999; return x; }
// throws AlderSandboxException

// Property reads still work
x
// output: 0
```

**Safe() flag summary:** AllowPropertyRead, AllowAssignment, AllowPropertySet, AllowIndexSet are all `true`. AllowMethodCalls, AllowStaticPropertyRead, AllowStaticFieldRead, AllowConstruction are `false`.

## 3. AllowedTypes with Open Generic

`AllowedTypes` checks the **exact constructed type**. Registering an open generic (`typeof(List<>)`) does not cover any constructed form.

**Wrong** -- open generic does not match constructed types:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Trusted() with
    {
        AllowedTypes = new HashSet<Type> { typeof(List<>) }
    }
});

// Fails -- List<int> is not List<>
new List<int> { 1, 2, 3 }
// throws AlderSandboxException
```

**Right** -- register the exact constructed type:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Trusted() with
    {
        AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
    }
});

new List<int> { 1, 2, 3 }
// succeeds
```

## 4. Post-Freeze Configuration

The engine freezes its configuration on the first `Evaluate()` call. After that, registration methods (`RegisterFunction`, `RegisterModule`, `RegisterFromAssembly`, `RegisterNamespace`, `RegisterExtensionMethods`) throw `InvalidOperationException`.

**Wrong** -- registering after evaluation:

```csharp
var engine = new AlderEngine();
engine.Evaluate("1 + 1");

// Throws -- engine is frozen
engine.RegisterFunction("double", args => (int)args[0]! * 2);
// throws InvalidOperationException
```

**Right** -- register everything before the first evaluation:

```csharp
var engine = new AlderEngine();
engine.RegisterFunction("double", args => (int)args[0]! * 2);

engine.Evaluate("double(5)");
// output: 10
```

`SetVariable` is the exception -- it works both before and after freeze:

```csharp
var engine = new AlderEngine();
engine.Evaluate("1 + 1");

// SetVariable works after freeze
engine.SetVariable("x", 42);
engine.Evaluate("x");
// output: 42
```

## 5. Reflection Leak False Security

The reflection leak guard is **always active**, regardless of sandbox mode. Even in `Trusted()` mode with all flags enabled, expressions cannot obtain Type objects through member access.

**Wrong** -- expecting `.GetType()` to work in Trusted mode:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Trusted()
});
engine.SetVariable("text", "hello");

// Throws even in Trusted mode -- reflection guard is always on
text.GetType()
// throws AlderSandboxException
```

**Right** -- use `typeof()` for type information:

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Trusted()
});

typeof(int)
// output: System.Int32

typeof(string)
// output: System.String
```

`typeof()` works because it is a compile-time type literal. The reflection guard only intercepts runtime returns of reflection types through member access and method calls.

## See Also

- [Sandbox Overview](../security/sandbox-overview/) -- Threat model, permission flags, presets
- [Execution Limits](../security/execution-limits/) -- MaxStatements, MaxTimeout, MaxExpressionDepth
