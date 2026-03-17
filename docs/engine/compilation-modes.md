---
title: "Compilation Modes"
description: "Interpreted vs compiled execution, CsEval.Compiled extension methods, expression caching, and custom compiler backends."
sidebar:
  order: 6
---

## Overview

CsEval has two execution backends:

| Backend | How it works | Package needed |
|---------|-------------|----------------|
| **Interpreted** | Tree-walks the bound AST at runtime | CsEval (core) |
| **Compiled** | Builds LINQ expression trees, emits IL, executes native delegates | CsEval.Compiled |

The `UseCompiler()` extension method from the **CsEval.Compiled** package switches the engine to the compiled backend. Without it, the engine uses interpretation.

```csharp
using CsEval.Compiled;

// Interpreted (default) — always tree-walks
var engine = new CsEvalEngine();

// Compiled — emits IL, throws if compilation fails
var engine = new CsEvalEngine(CsEvalOptions.Default.UseCompiler());
```

## Interpreted (Default)

Always evaluates via the tree-walking interpreter. No IL emission, no expression tree construction.

- Lower startup cost per expression (no compilation step)
- Required for `EvaluateWithTrace()` (tracing always uses the interpreted pipeline)
- Works in all environments including those that restrict dynamic code generation

## Compiled

Compiles expressions to IL via LINQ expression trees on first evaluation. If compilation fails, the engine throws `CsEvalException` rather than falling back to interpretation.

- Higher throughput for repeated evaluations (native delegate invocation)
- First evaluation incurs a one-time compilation cost
- Subsequent evaluations of the same expression skip compilation entirely (cached)

Requires the **CsEval.Compiled** package and `UseCompiler()` on the options.

## CsEval.Compiled Package

The **CsEval.Compiled** NuGet package provides extension methods on `CsEvalEngine` for explicit compilation workflows. These methods give you direct control over when and how expressions are compiled.

```
dotnet add package CsEval.Compiled
```

```csharp
using CsEval.Compiled;
```

### Compile&lt;T&gt;

Parses, compiles to IL, and returns a `CsEvalCompiledExpression<T>` for repeated invocation without engine dispatch overhead.

```csharp
var engine = new CsEvalEngine();
CsEvalCompiledExpression<int> compiled = engine.Compile<int>("1 + 2");
int result = compiled.Invoke(); // 3
```

Throws `CsEvalException` if the expression cannot be compiled.

### Compile (non-generic)

Same as `Compile<T>` but returns `CsEvalCompiledExpression<object?>`.

```csharp
var compiled = engine.Compile("1 + 2");
object? result = compiled.Invoke();
```

### CompileToFunc&lt;T&gt;

Compiles and returns a `Func<T?>` delegate for zero-overhead hot-path invocation.

```csharp
var add = engine.CompileToFunc<int>("1 + 2");
int? result = add(); // 3
```

The returned delegate captures the engine context by reference -- variables set via `SetVariable` after compilation are visible to subsequent invocations.

### ParseAndCompile

Parses and attempts compilation in one step. Returns a `CsEvalExpression` (compiled when possible).

```csharp
CsEvalExpression expr = engine.ParseAndCompile("x * 2");
```

### ParseAsExpression&lt;TDelegate&gt;

Parses a lambda expression string into a typed `System.Linq.Expressions.Expression<TDelegate>`, suitable for Entity Framework, IQueryable providers, and in-memory compilation.

```csharp
Expression<Func<int, bool>> expr = engine.ParseAsExpression<Func<int, bool>>("x => x > 5");
// Use with EF: dbContext.Users.Where(expr)
```

Always parses in Standard mode regardless of engine `LanguageMode`.

### TryParseAsExpression&lt;TDelegate&gt;

Non-throwing variant that returns `false` with diagnostics on failure.

```csharp
if (engine.TryParseAsExpression<Func<int, bool>>("x => x > 5", out var expr, out var diagnostics))
{
    // expr is ready
}
```

### CompileExpression&lt;TDelegate&gt;

Parses a lambda string into an expression tree and compiles it to a native delegate.

```csharp
Func<int, bool> isPositive = engine.CompileExpression<Func<int, bool>>("x => x > 0");
bool result = isPositive(42); // true
```

## CsEvalCompiledExpression&lt;T&gt;

The wrapper returned by `Compile<T>()`. Holds a compiled delegate for repeated invocation.

### Invoke()

Invokes using the engine's current context. Variables set after compilation are visible.

```csharp
var engine = new CsEvalEngine();
engine.SetVariable("x", 10);
var compiled = engine.Compile<int>("x * 2");

int result1 = compiled.Invoke(); // 20

engine.SetVariable("x", 50);
int result2 = compiled.Invoke(); // 100
```

### Invoke(variables)

Invokes with per-invocation variables. Creates a child context so the provided variables do not pollute the engine's shared state.

```csharp
var compiled = engine.Compile<int>("x + y");
int result = compiled.Invoke(new Dictionary<string, object?> { ["x"] = 3, ["y"] = 7 }); // 10
```

## CsEvalExpression Compilation Properties

After parsing, a `CsEvalExpression` exposes compilation state:

| Property | Type | Description |
|----------|------|-------------|
| `IsCompiled` | `bool` | `true` if successfully compiled |
| `IsCompilable` | `bool?` | `true`/`false` after attempt, `null` if never attempted |
| `CompilationFailureReason` | `string?` | Reason for failure, or `null` |

### TryCompile / Compile

Compilation is owned by the engine, not the expression. Use the engine's `TryCompile` and `Compile` methods from the **CsEval.Compiled** package:

```csharp
var expr = engine.Parse("1 + 2");
bool compiled = engine.TryCompile(expr); // true if compilation succeeded
// or:
engine.Compile(expr); // throws CsEvalException if compilation fails
```

### GetVariables

Returns the distinct unbound identifier names found in the expression AST.

```csharp
var expr = engine.Parse("x + y * 2");
IReadOnlyList<string> vars = expr.GetVariables(); // ["x", "y"]
```

## Expression Reuse

Parse once, evaluate multiple times with different variable values:

```csharp
var engine = new CsEvalEngine();
var expr = engine.Parse("x * 2");

engine.SetVariable("x", 5);
var r1 = engine.Evaluate(expr); // 10

engine.SetVariable("x", 10);
var r2 = engine.Evaluate(expr); // 20
```

## Expression Caching

The engine maintains a FIFO-bounded expression cache (default capacity: 10,000 entries) keyed by expression text. The cache is:

- **Shared** between parent and child engines (child engines created via `CreateChild()` inherit the parent's cache)
- **Thread-safe** (backed by `ConcurrentDictionary`)
- **Bounded** with FIFO eviction when capacity is exceeded

When the compiled backend is active (via `UseCompiler()`), the first evaluation of an expression compiles it and stores the delegate in the cache. Subsequent evaluations of the same expression text reuse the cached delegate.

## Custom Compiler Backend

The `IExpressionCompiler` interface allows substituting an alternative LINQ expression tree compiler (e.g., FastExpressionCompiler):

```csharp
public interface IExpressionCompiler
{
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}
```

Configure via `CsEvalOptions.ExpressionCompiler`:

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    ExpressionCompiler = new FastExpressionCompilerAdapter()
}.UseCompiler());
```

The default implementation delegates to `System.Linq.Expressions.LambdaExpression.Compile()`.

## When to Use Which Mode

| Scenario | Recommended Approach |
|----------|---------------------|
| One-shot evaluation | Default (interpreted) — avoids compilation overhead |
| Repeated evaluation of same expression | `UseCompiler()` — native delegate is faster |
| Hot path (millions of invocations) | `CompileToFunc<T>()` — minimal dispatch overhead |
| Debugging / tracing | Default (interpreted) — `EvaluateWithTrace()` always uses interpreter |
| Entity Framework / IQueryable | `ParseAsExpression<T>()` — produces LINQ expression trees |
| Environments restricting dynamic code | Default (interpreted) — no IL emission |

## See Also

- [Expressions](/engine/expressions/) -- parse, evaluate, reuse, trace
- [Thread Safety](/engine/thread-safety/) -- concurrency guarantees and child engines
- [Native AOT](/engine/native-aot/) -- AOT constraints on compilation
