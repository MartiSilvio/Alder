---
title: "Compilation"
description: "IL compilation, UseCompiler, CompileToFunc, ParseAsExpression, swappable compiler backend"
sidebar:
  order: 5
---

Alder has two execution backends that share the same front-end (lexer → parser → binder → optimization passes). The **interpreter** walks the bound tree directly. The **compiler** translates the bound tree into LINQ expression trees and compiles them to native IL delegates.

Both backends produce identical results. The compiler exists for hot paths where the overhead of tree-walking interpretation is measurable — pricing engines evaluating the same formula millions of times, rule engines filtering every row in a dataset, real-time signal processing.

## Enabling Compilation

```csharp
var engine = new AlderEngine(o => o.UseCompiler());
```

<!-- test: Compilation_UseCompiler -->

`UseCompiler()` is an extension method from `Alder.Compiled` (shipped in the same NuGet package). On NativeAOT platforms where `RuntimeFeature.IsDynamicCodeSupported` is `false`, it throws `PlatformNotSupportedException` — the interpreter with AOT metadata provides the execution path on those platforms.

## Automatic Compilation

When `UseCompiler()` is configured, every `Evaluate` call automatically compiles the expression on first execution:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

// First call: parse → bind → pipeline passes → emit LINQ expression tree → compile to IL → execute
// Subsequent calls with same expression: execute cached delegate
string result = engine.Evaluate<string>("""
    string.Join(", ", new[] { 3, 1, 4, 1, 5 }.Distinct().OrderBy(x => x))
    """);
// "1, 3, 4, 5"
```

<!-- test: Compilation_AutoCompile -->

There is no manual compilation step for standard use. The compiled delegate is cached on the `AlderExpression` and reused for every subsequent evaluation. If compilation fails, `AlderException` with `ALDR0001` is thrown — there is no silent fallback to interpretation.

Compilation locks on the `AlderExpression` object (not the engine), so multiple threads compiling different expressions proceed in parallel. Exceptions from compiled code are enriched with source position information when the AST node has a span.

## Pre-compilation

For latency-sensitive applications where compilation should happen at startup:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

// ParseAndCompile: parse + bind + compile in one step
AlderExpression expr = engine.ParseAndCompile("Math.Sqrt(x * x + y * y)");

// Or explicitly compile a pre-parsed expression
var parsed = engine.Parse("items.Where(x => x > threshold).Count()");
engine.Compile(parsed);         // throws on failure
bool ok = engine.TryCompile(parsed);  // returns false on failure
```

<!-- test: Compilation_PreCompile -->

## `Compile<T>` — Hot-Path Wrapper

`Compile<T>` returns an `AlderCompiledExpression<T>` that bypasses engine dispatch entirely. The compiled delegate is invoked directly — no variable scoping overhead, no constraint checking per-call, no child context creation.

```csharp
var engine = new AlderEngine(o => o.UseCompiler());
engine.SetVariable<int>("n", 100);

var compiled = engine.Compile<int>("""
    Enumerable.Range(1, n).Where(x => x % 3 == 0 || x % 5 == 0).Sum()
    """);

int result = compiled.Invoke(); // 2418

// Variables set after compilation are visible — context is captured by reference
engine.SetVariable<int>("n", 10);
int result2 = compiled.Invoke(); // 33
```

<!-- test: Compilation_CompiledExpression -->

A second overload, `Invoke(IDictionary<string, object?> variables)`, accepts per-invocation variables via a child context.

The non-generic `Compile(string)` overload returns `AlderCompiledExpression<object?>` — useful when the return type varies.

## `CompileToFunc<T>` — Raw Delegate

When you want a bare `Func<T?>` with zero abstraction:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());
engine.SetVariable<double>("r", 5.0);

Func<double?> circleArea = engine.CompileToFunc<double>("Math.PI * r * r");
double? area = circleArea(); // ~78.54

engine.SetVariable<double>("r", 10.0);
double? area2 = circleArea(); // ~314.16
```

<!-- test: Compilation_CompileToFunc -->

## `ParseAsExpression<TDelegate>` — LINQ Expression Trees

For Entity Framework, IQueryable providers, or any system that consumes `Expression<TDelegate>`, Alder parses a lambda string into a typed expression tree:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

Expression<Func<int, bool>> predicate =
    engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");

// Pass to EF Core, IQueryable, or compile to a delegate
Func<int, bool> fn = predicate.Compile();
bool result = fn(25); // true
```

<!-- test: Compilation_ParseAsExpression -->

`ParseAsExpression` always parses in Standard mode regardless of the engine's `LanguageMode`. Extended syntax (`**`, `|>`, comprehensions, `in`, `like`) has no representation in standard LINQ expression trees — EF Core and other providers couldn't translate them to SQL. Forcing Standard mode ensures the output is provider-compatible. The expression tree is produced by a separate lightweight emitter that creates provider-transparent trees with no Alder runtime dependencies — any LINQ provider works.

Parameter types are inferred from the delegate's generic arguments: `Func<int, bool>` means one `int` parameter and a `bool` return.

`TryParseAsExpression<TDelegate>` is the non-throwing variant. `CompileExpression<TDelegate>` combines parsing and compilation in one call.

See [LINQ Dynamic](linq-dynamic.md) for string-based LINQ on any `IEnumerable<T>` or `IQueryable<T>`.

## Swapping the Expression Compiler

The `IExpressionCompiler` interface allows substituting an alternative LINQ expression tree compiler:

```csharp
public interface IExpressionCompiler
{
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}
```

Alder ships with a default implementation that calls `Expression<TDelegate>.Compile()`. To use an alternative like [FastExpressionCompiler](https://github.com/dadhi/FastExpressionCompiler):

```csharp
public class FastExpressionCompilerAdapter : IExpressionCompiler
{
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate
        => expression.CompileFast();  // FastExpressionCompiler extension method
}

var engine = new AlderEngine(o =>
{
    o.UseCompiler(new FastExpressionCompilerAdapter());
});
```

No third-party compilers are included — alternative backends are provided by the user. It is the user's responsibility to ensure the replacement supports all expression node types that Alder emits.

## Caching

Compiled delegates are cached at two levels:

1. **Per `AlderExpression`**: The compiled delegate is stored on the expression via a volatile field with double-checked locking.

2. **Per engine**: When compiling from a string, the delegate is cached with FIFO eviction. Shared between parent and child engines.

## Expression Tree Boundaries

The full emitter (`BoundExpressionEmitter`) handles all bound node kinds. The lightweight emitter (`ExpressionTreeEmitter`, used by `ParseAsExpression`) supports a smaller subset — it cannot emit loops, blocks, variable declarations, assignments, try/catch, collection expressions, or spread operators.

These limitations apply only to `ParseAsExpression`. The full `Evaluate` compilation path handles all of these.
