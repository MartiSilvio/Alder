---
title: "Method Invocation"
description: "Method invocation in CsEval expressions: overload resolution, generic inference, out var, named arguments, params, and extension methods."
sidebar:
  order: 10
---

## Overview

CsEval resolves and invokes methods using a multi-stage pipeline. Instance and static method calls on types are gated by the `AllowMethodCalls` sandbox flag. Host-registered functions, modules, lambdas, and extension methods (including LINQ) are always allowed regardless of sandbox settings.

## Sandbox Gate

The `AllowMethodCalls` flag controls whether instance and static method calls are permitted on runtime types. It is enabled in `Trusted()` mode but disabled in `Safe()` and `Strict()` modes.

```csharp
// Trusted mode (default) — method calls allowed
var engine = new CsEvalEngine();
engine.SetVariable("name", "hello");
var result = engine.Evaluate<string>("name.ToUpper()");
// result: "HELLO"

// Safe mode — method calls blocked
var safeEngine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe()
});
safeEngine.SetVariable("name", "hello");
safeEngine.Evaluate("name.ToUpper()");
// throws CsEvalSandboxException
```

:::note
Modules, registered functions, lambdas, and LINQ extension methods are Tier 1 callees and bypass the `AllowMethodCalls` gate entirely. Only instance/static method calls on runtime types are gated.
:::

## Instance Methods

Instance methods are called on variable objects or expression results.

```csharp
var engine = new CsEvalEngine();

engine.Evaluate<string>("\"hello\".ToUpper()");
// "HELLO"

engine.Evaluate<bool>("\"hello world\".Contains(\"world\")");
// true
```

## Static Methods

Static method calls use the type name followed by the method name.

```csharp
var engine = new CsEvalEngine();

engine.Evaluate<int>("Math.Max(3, 7)");
// 7

engine.Evaluate<int>("int.Parse(\"42\")");
// 42
```

`Math` and `Convert` are built-in modules, so they work without type registration. For other types, register the assembly and namespace first.

## Overload Resolution

CsEval implements overload resolution per ECMA-334 section 12.6.4 using a scoring algorithm. When multiple method overloads match the provided arguments, the engine selects the best match.

### Scoring Algorithm

Each argument is scored against its corresponding parameter:

| Match Quality | Score |
|--------------|-------|
| Exact type match | 100 |
| Assignable (base class, interface) | 10 |
| Lambda to delegate | 5 |
| Implicit conversion (numeric widening, user-defined) | 1 |
| Null to nullable | 1 |
| No match | -1 (method rejected) |

The total score for a method is the sum of all argument scores plus a form bonus:

| Form | Base Score |
|------|-----------|
| Normal form (exact parameter count) | 1000 |
| Expanded params form | 500 |

Normal form is always preferred over expanded params form per ECMA-334 section 12.6.4.3. Methods with default parameters incur a -10 penalty per default used.

### Tie-Breaking

When two methods have equal scores, specificity rules apply:

1. Non-params methods preferred over params methods
2. Non-generic methods preferred over generic methods
3. Methods requiring fewer implicit arguments preferred
4. Parameter-by-parameter comparison using ECMA-334 section 12.6.4.7 "better conversion target"

```csharp
var engine = new CsEvalEngine();

// Resolves to Convert.ToInt32(double) — exact match for double arg
engine.Evaluate<int>("Convert.ToInt32(42.9)");
// 43

// Resolves to Convert.ToInt32(string) — exact match for string arg
engine.Evaluate<int>("Convert.ToInt32(\"42\")");
// 42
```

## Generic Type Inference

CsEval supports both explicit and implicit generic type arguments.

### Explicit Type Arguments

Specify type arguments directly in the method call.

```csharp
var engine = new CsEvalEngine();

engine.Evaluate("Array.Empty<int>()");
// int[] (empty)
```

### Implicit Inference

Type arguments are inferred from the runtime types of the provided arguments. The engine walks parameter types, matches generic parameters against argument types (including through interfaces), and fills in the type argument array.

```csharp
var engine = new CsEvalEngine();

// T inferred as int from the List<int> argument
engine.SetVariable("numbers", new List<int> { 3, 1, 2 });
engine.Evaluate("numbers.ConvertAll(x => x * 2)");
// List<int>: [6, 2, 4]
```

For extension methods with lambda arguments, the engine can also infer return types by test-invoking the lambda with default arguments, or by static analysis of the lambda body AST.

## Named Arguments

Named arguments use `name: value` syntax and are reordered to match parameter positions at invocation time.

```csharp
var engine = new CsEvalEngine();

engine.Evaluate<string>("string.Format(format: \"{0} {1}\", arg0: \"hello\", arg1: \"world\")");
// "hello world"
```

Named arguments can be mixed with positional arguments. Positional arguments fill parameters left-to-right, skipping positions claimed by named arguments.

## Out Variables

The `out var` syntax creates a new variable in the evaluation context after the method completes. The engine uses an `OutArgMarker` internally that matches `ByRef` parameters as an exact-match score during overload resolution.

```csharp
var engine = new CsEvalEngine();

engine.Evaluate<int>("{ int.TryParse(\"42\", out var x); return x; }");
// 42
```

After invocation, the out parameter's value is copied back and defined as a variable in the current scope.

## Params Arrays

Methods with `params` parameters accept variable-length argument lists. CsEval expands the arguments into an array when the expanded form is selected during overload resolution.

```csharp
var engine = new CsEvalEngine();

engine.Evaluate<string>("string.Format(\"{0} {1}\", \"a\", \"b\")");
// "a b"
```

The expanded form scores 500 points lower than the normal form, ensuring that if a method has an overload accepting the exact number of arguments, it is preferred.

## Extension Methods

When no instance method matches, CsEval falls back to registered extension methods. This follows ECMA-334 section 12.8.9.2: instance methods take precedence over extension methods.

Extension method classes must be registered explicitly via `RegisterExtensionMethods`.

```csharp
public static class StringExtensions
{
    public static string Shout(this string s) => s.ToUpper() + "!";
}

var engine = new CsEvalEngine()
    .RegisterExtensionMethods(typeof(StringExtensions))
    .SetVariable("name", "hello");

engine.Evaluate<string>("name.Shout()");
// "HELLO!"
```

LINQ methods are available automatically since `System.Linq.Enumerable` is registered as a built-in extension type.

## Resolution Pipeline

The full method resolution pipeline for instance method calls:

1. **AOT metadata** -- if a generated type context provides method dispatch for the type, attempts fast path
2. **Type-based fast path** -- extracts argument types and uses `MethodResolver` for score-based resolution (no named args, no out params, no nulls)
3. **Full candidate scoring** -- builds candidate list including generic methods with explicit type args, scores with `FindBestMethod`
4. **Extension method fallback** -- if no instance method found, tries registered extension types
5. **Sandbox gate** -- if `AllowMethodCalls` is `false` and no extension method matched, throws `CsEvalSandboxException`

For static method calls, the pipeline is similar but starts from the resolved type directly.

## See Also

- [New Operator](../engine/new-operator/) -- Object construction in expressions
- [Functions and Modules](../engine/functions-and-modules/) -- Host-registered functions and modules
- [Type Registration](../engine/type-registration/) -- Register assemblies for type resolution
