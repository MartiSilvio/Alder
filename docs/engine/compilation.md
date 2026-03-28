---
title: "Compilation"
description: "IL compilation, UseCompiler, CompileToFunc, ParseAsExpression, LINQ Dynamic"
sidebar:
  order: 5
---

Alder has two execution backends that share the same front-end (lexer → parser → binder → bound tree pipeline). The **interpreter** walks the bound tree directly. The **compiler** translates the bound tree into a `System.Linq.Expressions.Expression` tree, then compiles that to a native delegate via `Expression.Compile()`.

Both backends produce identical results. The compiler exists for hot paths where the overhead of tree-walking interpretation is measurable — pricing engines evaluating the same formula millions of times, rule engines filtering every row in a dataset, real-time signal processing.

## Enabling Compilation

```csharp
var engine = new AlderEngine(o => o.UseCompiler());
```

<!-- test: Compilation_UseCompiler -->

`UseCompiler()` is an extension method on `AlderOptions` provided by the `Alder.Compiled` assembly (shipped in the same NuGet package). It sets the internal `ICompiledProvider` to `CompiledProvider.Instance`, which routes compilation through `ILExpressionCompiler`.

On NativeAOT platforms where `RuntimeFeature.IsDynamicCodeSupported` is `false`, `UseCompiler()` throws `PlatformNotSupportedException`. The interpreter with AOT metadata provides the best performance on those platforms.

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

There is no manual compilation step for standard use. The compiled delegate is cached on the `AlderExpression` object and reused for every subsequent evaluation.

If compilation fails for a particular expression (not all bound node kinds are emittable — see [Compilation Boundaries](#compilation-boundaries)), `Evaluate` throws `AlderException` with code `ALDR0001`. There is no silent fallback to interpretation. When you opt into compiled mode, you get compiled execution or an explicit error.

## Pre-compilation

For latency-sensitive applications where you want compilation to happen at startup rather than on the first request:

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

`Compile<T>` returns an `AlderCompiledExpression<T>` that bypasses engine dispatch entirely. The compiled delegate is invoked directly without variable scoping overhead, constraint checking per-call, or child context creation.

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

`AlderCompiledExpression<T>.Invoke()` captures the engine's `AlderContext` by reference. Variables set via `SetVariable` after compilation are visible to subsequent invocations. The delegate signature is `(AlderContext, AlderConfig, ExecutionConstraintState, CancellationToken) → object?`.

## `CompileToFunc<T>` — Raw Delegate

When you want a bare `Func<T?>` with zero abstraction between your code and the compiled IL:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());
engine.SetVariable<double>("r", 5.0);

Func<double?> circleArea = engine.CompileToFunc<double>("Math.PI * r * r");
double? area = circleArea(); // ~78.54

engine.SetVariable<double>("r", 10.0);
double? area2 = circleArea(); // ~314.16
```

<!-- test: Compilation_CompileToFunc -->

Internally, `CompileToFunc<T>` calls `Compile<T>` and returns `() => compiled.Invoke()`.

## `ParseAsExpression<TDelegate>` — LINQ Expression Trees

For Entity Framework, IQueryable providers, or any system that consumes `Expression<TDelegate>`, Alder can parse a lambda string into a typed expression tree:

```csharp
var engine = new AlderEngine(o => o.UseCompiler());

Expression<Func<int, bool>> predicate =
    engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");

// Pass to EF Core, IQueryable, or compile to a delegate
Func<int, bool> fn = predicate.Compile();
bool result = fn(25); // true
```

<!-- test: Compilation_ParseAsExpression -->

`ParseAsExpression` always parses in Standard mode regardless of the engine's `LanguageMode`. The expression tree is emitted by `ExpressionTreeEmitter`, which is a separate emitter from `BoundExpressionEmitter` — it produces provider-transparent expression trees without Alder runtime dependencies.

Parameter types are inferred from the delegate's generic arguments: `Func<int, bool>` means one `int` parameter and a `bool` return. The lambda parameter count must match.

See [LINQ Dynamic](linq-dynamic.md) for string-based LINQ on any `IEnumerable<T>` or `IQueryable<T>`.

## The Compilation Pipeline

When compilation is requested, the bound tree passes through a different pipeline than interpretation:

```
Bound Tree
  → SecurityValidationPass     (same as interpreted)
  → ConstantFoldingPass         (same as interpreted)
  → DeadBranchEliminationPass   (same as interpreted)
  → ConversionInsertionPass     (compilation only — inserts explicit BoundCastExpr for type promotions)
  → BoundExpressionEmitter      (translates to System.Linq.Expressions)
  → IExpressionCompiler.Compile (Expression.Compile() by default)
```

The `ConversionInsertionPass` is compilation-only because the interpreter handles numeric promotion at runtime in `NumericDispatch.PromoteOperands()`. The compiler needs explicit cast nodes in the expression tree because `System.Linq.Expressions` requires exact type matching.

## Swapping the Expression Compiler

The `IExpressionCompiler` interface allows substituting an alternative LINQ expression tree compiler. Alder ships with `DefaultExpressionCompiler` which calls `Expression<TDelegate>.Compile()`. No third-party compilers are included; alternative backends are provided by the user as separate dependencies.

For example, [FastExpressionCompiler](https://github.com/dadhi/FastExpressionCompiler) is a popular alternative. To use it, implement the adapter and pass it to `UseCompiler()`:

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

It is the user's responsibility to ensure the replacement backend supports all expression node types that Alder emits and produces semantically correct delegates. If the alternative compiler doesn't handle a particular node type, the compiled expression may throw at runtime.

## Caching

Compiled delegates are cached at two levels:

1. **Per `AlderExpression`**: The `CompiledExpressionInfo` (containing the delegate) is stored on the `AlderExpression` via a `volatile` field. Thread-safe via double-checked locking.

2. **Per engine `ExpressionCache`**: When compiling from a string (not a pre-parsed `AlderExpression`), the delegate is cached in a `ConcurrentDictionary<string, CompiledExpressionInfo>` with FIFO eviction at 10,000 entries. Shared between parent and child engines.

## Compilation Boundaries

The `BoundExpressionEmitter` handles all 63 `BoundNodeKind` values. However, `ExpressionTreeEmitter` (used by `ParseAsExpression`) supports a smaller subset — it cannot emit:

- Switch expressions and switch statements
- Blocks, variable declarations
- Loops (`for`, `while`, `do`, `foreach`)
- Assignment operators
- `try`/`catch`/`finally`
- Collection expressions and array literals
- Spread operators

These limitations apply only to `ParseAsExpression` — the full `Evaluate` compilation path through `BoundExpressionEmitter` handles all of these.
