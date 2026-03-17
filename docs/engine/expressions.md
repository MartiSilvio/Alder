---
title: "Expressions"
description: "Expression lifecycle: parse, evaluate, reuse, validate, trace. All Evaluate, Parse, TryValidate, and EvaluateWithTrace overloads."
sidebar:
  order: 2
---

## Overview

Expressions follow a **parse-evaluate-reuse** lifecycle. You can evaluate a string directly, or parse it into a `CsEvalExpression` object for repeated evaluation without re-parsing.

```csharp
var engine = new CsEvalEngine();

// Direct evaluation — parse + evaluate in one call
var result = engine.Evaluate<int>("1 + 2");
// result: 3

// Parse once, evaluate many times
var expr = engine.Parse("x * 2");
var a = engine.Evaluate<int>(expr, new Dictionary<string, object?> { ["x"] = 5 });
// a: 10
var b = engine.Evaluate<int>(expr, new Dictionary<string, object?> { ["x"] = 10 });
// b: 20
```

## Parse

### Parse(string)

Parses an expression string into a reusable `CsEvalExpression`. Throws on syntax errors.

```csharp
var engine = new CsEvalEngine();
CsEvalExpression expr = engine.Parse("Math.Max(a, b)");
```

### TryParse(string, out CsEvalExpression?, out string?)

Non-throwing variant. Returns `false` on parse failure with an error message.

```csharp
var engine = new CsEvalEngine();

if (engine.TryParse("1 +", out var expr, out var error))
{
    // use expr
}
else
{
    // error contains the parse failure message
}
```

### TryParse(string, out CsEvalExpression?)

Non-throwing variant without error message.

```csharp
var engine = new CsEvalEngine();
bool valid = engine.TryParse("1 + 2", out var expr);
```

## Evaluate

### Evaluate(string, ...)

Parses and evaluates a string expression. Returns `object?`.

```csharp
var engine = new CsEvalEngine();
object? result = engine.Evaluate("1 + 2");
// result: 3
```

### Evaluate(CsEvalExpression, ...)

Evaluates a pre-parsed expression.

```csharp
var engine = new CsEvalEngine();
var expr = engine.Parse("42");
object? result = engine.Evaluate(expr);
// result: 42
```

### Evaluate&lt;T&gt;(string, ...) / Evaluate&lt;T&gt;(CsEvalExpression, ...)

Generic overloads that convert the result to type `T`.

```csharp
var engine = new CsEvalEngine();
int result = engine.Evaluate<int>("10 + 20");
// result: 30
```

### Variable overloads

All `Evaluate` methods accept optional parameters for per-invocation variables, an `IServiceProvider`, and a `CancellationToken`:

```csharp
var engine = new CsEvalEngine();

// Dictionary variables
var vars = new Dictionary<string, object?> { ["x"] = 10, ["y"] = 20 };
var result = engine.Evaluate<int>("x + y", vars);
// result: 30

// Anonymous object variables (uses reflection — not AOT-compatible)
var result2 = engine.Evaluate<int>("x + y", new { x = 10, y = 20 });
// result2: 30
```

Per-invocation variables are scoped to that call only. See [Variables](../engine/variables/) for details.

## TryEvaluate

Non-throwing evaluation. Returns `false` on any error (parse, bind, or runtime).

### TryEvaluate(string, out object?, ...)

```csharp
var engine = new CsEvalEngine();
if (engine.TryEvaluate("1 + 2", out var result))
{
    // result: 3
}
```

### TryEvaluate&lt;T&gt;(string, out T?, ...)

```csharp
var engine = new CsEvalEngine();
if (engine.TryEvaluate<int>("1 + 2", out var result))
{
    // result: 3
}
```

Both overloads accept optional `variables`, `serviceProvider`, and `cancellationToken` parameters.

## TryValidate

Validates an expression for syntax and semantic correctness without evaluating it. Returns structured diagnostics.

```csharp
var engine = new CsEvalEngine();

// Valid expression
bool valid = engine.TryValidate("1 + 2", out var diagnostics);
// valid: true, diagnostics is empty

// Invalid — unresolved identifier
bool valid2 = engine.TryValidate("unknownVar + 1", out var diagnostics2);
// valid2: false
// diagnostics2 contains CsEvalDiagnostic with error info
```

`CsEvalDiagnostic` contains:

| Property | Type | Description |
|----------|------|-------------|
| `Severity` | `DiagnosticSeverity` | `Error`, `Warning`, or `Info` |
| `Message` | `string` | Human-readable error message |
| `Code` | `DiagnosticCode?` | Structured error code |
| `Line` | `int?` | Line number |
| `Column` | `int?` | Column number |

## EvaluateWithTrace

Returns an `EvaluationTraceResult` with a step-by-step execution trace alongside the result. Useful for debugging and understanding how an expression is evaluated.

```csharp
var engine = new CsEvalEngine();

var trace = engine.EvaluateWithTrace("1 + 2");
// trace.Result: 3
// trace.Steps: list of EvaluationTraceStep records
```

:::note
`EvaluateWithTrace` always uses the interpreted pipeline internally for tracing, regardless of whether the compiled backend is active.
:::

`EvaluationTraceResult` contains:

| Property | Type | Description |
|----------|------|-------------|
| `Result` | `object?` | The evaluation result |
| `Steps` | `IReadOnlyList<EvaluationTraceStep>` | Ordered execution steps |

`EvaluationTraceStep` contains:

| Property | Type | Description |
|----------|------|-------------|
| `NodeKind` | `string` | AST node type (e.g., `"BinaryExpr"`, `"LiteralExpr"`) |
| `Value` | `object?` | The value produced at this step |
| `Display` | `string?` | Human-readable display of the step |

Both string and `CsEvalExpression` overloads are available, with optional `variables`, `serviceProvider`, and `cancellationToken` parameters.

## CsEvalExpression

A pre-parsed expression object returned by `Parse()`. Holds the AST and compilation state.

### Source

```csharp
var engine = new CsEvalEngine();
var expr = engine.Parse("1 + 2");
string source = expr.Source;
// source: "1 + 2"
```

### IsCompiled

Returns `true` if this expression has been successfully compiled to IL.

### IsCompilable

Returns `true` if compilable, `false` if not, or `null` if compilation has not been attempted.

### CompilationFailureReason

Returns the reason compilation failed, or `null` if it succeeded or hasn't been attempted.

### TryCompile() / Compile()

Compilation is owned by the engine. The `TryCompile` and `Compile` methods from the **CsEval.Compiled** package attempt to compile a parsed expression to IL.

```csharp
using CsEval.Compiled;

var engine = new CsEvalEngine(CsEvalOptions.Default.UseCompiler());
var expr = engine.Parse("1 + 2");

if (engine.TryCompile(expr))
{
    // expr.IsCompiled is now true
}

// Or throw on failure:
engine.Compile(expr);
```

### GetVariables()

Returns the distinct names of unbound identifiers found in the expression. Useful for detecting which variables an expression references before evaluation.

```csharp
var engine = new CsEvalEngine();
var expr = engine.Parse("x + y * z");

IReadOnlyList<string> vars = expr.GetVariables();
// vars: ["x", "y", "z"]
```

## Expression Caching

Parsed expressions are cached in a FIFO-bounded cache (10,000 entries). When an engine calls `Parse()` with a string that was previously parsed, the cached `CsEvalExpression` is returned. The cache is shared between parent and child engines created via `CreateChild()`.

## See Also

- [CsEvalOptions](../engine/options/) — Configure engine behavior
- [Variables](../engine/variables/) — Inject host values into expressions
