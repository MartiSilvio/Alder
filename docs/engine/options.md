---
title: "CsEvalOptions"
description: "Configure engine behavior: case sensitivity, execution constraints, sandbox, expression depth, and language mode."
sidebar:
  order: 1
---

## Overview

`CsEvalOptions` is a sealed C# record that controls engine behavior. It is configured at engine creation time and cannot be changed after the engine is constructed (the record is immutable via `init`-only properties).

```csharp
// Default options
var engine = new CsEvalEngine();

// Custom options
var engine = new CsEvalEngine(new CsEvalOptions
{
    IsCaseSensitive = false
});
```

The `CsEvalOptions.Default` static property returns a new instance with all default values.

## Properties

### IsCaseSensitive

| Type | Default |
|------|---------|
| `bool` | `true` |

Controls whether member lookup is case-sensitive. When `false`, identifier resolution uses ordinal case-insensitive comparison.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions { IsCaseSensitive = false });
engine.SetVariable("UserName", "Alice");

var result = engine.Evaluate<string>("username");
// result: "Alice"
```

### Constraints

| Type | Default |
|------|---------|
| `ExecutionConstraints?` | `null` (unlimited) |

Execution resource limits enforced at statement boundaries. When `null`, no limits are applied. `ExecutionConstraints` is a mutable class — limits can be changed between evaluations.

**ExecutionConstraints properties:**

| Property | Type | Effect |
|----------|------|--------|
| `MaxStatements` | `long?` | Maximum statements per `Evaluate()` call. Each loop iteration, block statement, and top-level expression counts as one. Exceeding throws `CsEvalExecutionLimitException`. |
| `MaxTimeout` | `TimeSpan?` | Maximum wall-clock time per `Evaluate()` call. Uses `Stopwatch` for low-overhead monotonic timing, checked at statement boundaries. Exceeding throws `CsEvalExecutionLimitException`. |

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Constraints = new ExecutionConstraints { MaxStatements = 100 }
});

// Runs fine — well under 100 statements
var result = engine.Evaluate<int>("{ var x = 0; for (int i = 0; i < 10; i++) x += i; return x; }");
// result: 45

// Exceeds limit — throws CsEvalExecutionLimitException
engine.Evaluate("{ while (true) {} }");
```

### MaxExpressionDepth

| Type | Default |
|------|---------|
| `int` | `512` |

Maximum nesting depth for expression evaluation and compilation. The evaluator and IL compiler enforce this cap independently. When exceeded, a catchable `CsEvalException` is thrown instead of risking an uncatchable `StackOverflowException`.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions { MaxExpressionDepth = 100 });
```

### Sandbox

| Type | Default |
|------|---------|
| `SandboxOptions` | `SandboxOptions.Trusted()` |

Controls which operations expressions can perform. `SandboxOptions` is a sealed record with boolean flags and three factory presets.

**Flags:**

| Flag | Effect |
|------|--------|
| `AllowMethodCalls` | Method calls on variable objects (e.g., `str.ToUpper()`) |
| `AllowPropertyRead` | Property/field reads on variable objects (e.g., `str.Length`) |
| `AllowStaticPropertyRead` | Static property reads from types (e.g., `int.MaxValue`) |
| `AllowStaticFieldRead` | Static field reads from types |
| `AllowAssignment` | Variable reassignment (e.g., `x = 5`, `x++`). Declarations are always allowed. |
| `AllowPropertySet` | Property/field assignment on objects |
| `AllowIndexSet` | Index assignment (e.g., `arr[0] = 5`) |
| `AllowConstruction` | Object construction via `new` expressions |
| `AllowedTypes` | `HashSet<Type>?` — when set, only listed types may be resolved or constructed |

**Factory presets:**

| Preset | Description | Flags enabled |
|--------|-------------|---------------|
| `Trusted()` | Full access. All flags enabled. | All |
| `Safe()` | No method calls or construction. Property reads, assignments, and indexing allowed. | `AllowPropertyRead`, `AllowAssignment`, `AllowPropertySet`, `AllowIndexSet` |
| `Strict()` | Read-only. No mutations, no method calls, no construction. | `AllowPropertyRead` |

```csharp
// Safe mode: blocks method calls
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe()
});

engine.SetVariable("name", "Alice");

// Property reads work
var len = engine.Evaluate<int>("name.Length");
// result: 5

// Method calls throw CsEvalSandboxException
engine.Evaluate("name.ToUpper()");
// throws CsEvalSandboxException
```

:::note
Sandbox configuration is covered in depth in the Security section. Modules, registered functions, lambdas, and LINQ extension methods are always allowed regardless of `AllowMethodCalls`.
:::

### LanguageMode

| Type | Default |
|------|---------|
| `LanguageMode` (enum) | `Standard` |

Controls which syntax features are available.

| Value | Behavior |
|-------|----------|
| `Standard` | Strict ECMA-334 compliance. Non-standard extensions are rejected. |
| `Extended` | Enables non-standard syntax sugar (spread, object merge, `===`, `!==`, etc.) |

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    LanguageMode = LanguageMode.Standard
});
```

:::note
Extended mode is documented separately. All examples in this documentation use Standard mode.
:::

### UseCompiler()

The `UseCompiler()` extension method from the **CsEval.Compiled** package enables compiled execution (IL emission via LINQ expression trees). Without it, the engine uses tree-walking interpretation.

```csharp
using CsEval.Compiled;

// Interpreted (default) — always tree-walks
var engine = new CsEvalEngine();

// Compiled — emits IL
var engine = new CsEvalEngine(CsEvalOptions.Default.UseCompiler());

// Compiled with custom options — chains on any options value
var options = new CsEvalOptions
{
    LanguageMode = LanguageMode.Extended,
    Sandbox = SandboxOptions.Safe()
}.UseCompiler();
var engine = new CsEvalEngine(options);
```

See [Compilation Modes](../engine/compilation-modes/) for full details.

### ExpressionCompiler

| Type | Default |
|------|---------|
| `IExpressionCompiler` | `DefaultExpressionCompiler.Instance` |

Strategy used to compile LINQ expression trees to delegates when the compiled backend is active. The default uses `System.Linq.Expressions`. Supply an alternative implementation (e.g., FastExpressionCompiler) to override.

## Engine Lifecycle

### Freeze-on-First-Use

The engine follows a two-phase lifecycle:

1. **Configuration phase** — Register modules, functions, assemblies, namespaces, and extension methods. Set variables via `SetVariable`.
2. **Evaluation phase** — After the first `Evaluate()` call, the engine configuration is frozen. Registration methods (`RegisterModule`, `RegisterFunction`, `RegisterAssembly`, `RegisterNamespace`, `RegisterExtensionMethods`) throw `InvalidOperationException` if called.

`SetVariable` is the exception — it works in both phases. Before freeze, variables are staged internally. After freeze, they are defined directly in the evaluation context (thread-safe).

### Dispose

`CsEvalEngine` implements `IDisposable`. Disposing clears internal caches and marks the engine as disposed. Subsequent calls throw `ObjectDisposedException`.

```csharp
using var engine = new CsEvalEngine();
var result = engine.Evaluate<int>("1 + 2");
// result: 3
// engine is disposed at end of scope
```

## See Also

- [Expressions](../engine/expressions/) — Parse, evaluate, and reuse expressions
- [Variables](../engine/variables/) — Inject host values into expressions
