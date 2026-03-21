---
title: "Exceptions and Diagnostics"
description: "Exception hierarchy, non-throwing APIs, TryValidate pattern, and ControlFlowSignal internals."
sidebar:
  order: 1
---

## Overview

Alder uses structured exceptions with Roslyn-style diagnostic codes. Every exception carries an optional `ErrorCode` and location information, making it straightforward to programmatically identify error types and display precise source locations.

All Alder exceptions inherit from `AlderException`. Non-throwing alternatives (`TryEvaluate`, `TryParse`, `TryValidate`) are available for scenarios where exceptions are undesirable.

## Base Class: AlderException

`AlderException` extends `System.Exception` and adds diagnostic metadata.

**Properties:**

| Property        | Type              | Description                                                            |
| --------------- | ----------------- | ---------------------------------------------------------------------- |
| `ErrorCode`     | `DiagnosticCode?` | The diagnostic code enum value, or `null` for unstructured errors      |
| `FormattedCode` | `string?`         | Human-readable code string (e.g., `"CS0103"`, `"CSEV0010"`), or `null` |
| `Line`          | `int?`            | 1-based line number where the error occurred                           |
| `Column`        | `int?`            | 1-based column number                                                  |
| `SpanStart`     | `int?`            | 0-based source span start index                                        |
| `SpanLength`    | `int?`            | Source span length                                                     |

```csharp
var engine = new AlderEngine();

try
{
    engine.Evaluate("undeclaredVar");
}
catch (AlderException ex)
{
    // ex.FormattedCode  --> "CS0103"
    // ex.ErrorCode      --> DiagnosticCode.CS0103
    // ex.Message        --> "CS0103: The name 'undeclaredVar' does not exist in the current context"
}
```

## Exception Subclasses

### AlderParserException

Thrown when the parser encounters a syntax error. Inherits location properties from `AlderException`.

```csharp
var engine = new AlderEngine();

try
{
    engine.Evaluate("x +");
}
catch (AlderParserException ex)
{
    // ex.Message contains syntax error details
    // ex.Line, ex.Column point to the error location
}
```

### AlderLexerException

Thrown when the lexer encounters a tokenization error (e.g., unterminated strings, invalid escape sequences, invalid numeric literals).

```csharp
var engine = new AlderEngine();

try
{
    engine.Evaluate("\"unterminated");
}
catch (AlderLexerException ex)
{
    // ex.Message --> "Unterminated string at 1:14"
    // ex.Line, ex.Column point to the error location
}
```

### AlderDepthException

Thrown when expression nesting exceeds `AlderOptions.MaxExpressionDepth` (default: 512). Unlike `StackOverflowException`, this is catchable.

**Additional properties:**

| Property   | Type  | Description                                    |
| ---------- | ----- | ---------------------------------------------- |
| `MaxDepth` | `int` | The configured maximum depth that was exceeded |

```csharp
var engine = new AlderEngine(new AlderOptions { MaxExpressionDepth = 3 });

try
{
    engine.Evaluate("true ? (true ? (true ? (true ? 1 : 2) : 2) : 2) : 2");
}
catch (AlderDepthException ex)
{
    // ex.MaxDepth --> 3
}
```

### AlderLanguageModeException

Thrown when an Extended-mode syntax feature is used with `LanguageMode.Standard` (the default).

**Additional properties:**

| Property      | Type     | Description                                                             |
| ------------- | -------- | ----------------------------------------------------------------------- |
| `FeatureName` | `string` | The feature that requires Extended mode (e.g., `"**"`, `"in"`, `"[:]"`) |

```csharp
var engine = new AlderEngine(); // Standard mode

try
{
    engine.Evaluate("2 ** 3");
}
catch (AlderLanguageModeException ex)
{
    // ex.FeatureName   --> "**"
    // ex.FormattedCode --> "CSEV0009"
}
```

### AlderExecutionLimitException

Thrown when `MaxStatements` or `MaxTimeout` is exceeded. The engine remains healthy after this exception -- subsequent evaluations work normally.

**Additional properties:**

| Property             | Type                 | Description                                          |
| -------------------- | -------------------- | ---------------------------------------------------- |
| `LimitType`          | `ExecutionLimitType` | `Statements` or `Timeout`                            |
| `LimitValue`         | `long`               | The configured limit (statement count or timeout ms) |
| `ActualValue`        | `long`               | The actual value when the limit was hit              |
| `StatementsExecuted` | `long`               | Total statements executed when thrown                |
| `ElapsedTime`        | `TimeSpan`           | Wall-clock time elapsed when thrown                  |

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Constraints = new ExecutionConstraints { MaxStatements = 10 }
});

try
{
    engine.Evaluate("{ while (true) {} }");
}
catch (AlderExecutionLimitException ex)
{
    // ex.LimitType          --> ExecutionLimitType.Statements
    // ex.LimitValue         --> 10
    // ex.StatementsExecuted --> >= 10
}
```

### AlderSandboxException

Thrown when an expression is blocked by the sandbox. Covers all CSEV03xx diagnostics: method call, property access, assignment, construction, and type allowlist violations.

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Sandbox = SandboxOptions.Strict()
});
engine.SetVariable("s", "hello");

try
{
    engine.Evaluate("s.ToUpper()");
}
catch (AlderSandboxException ex)
{
    // ex.FormattedCode --> "CSEV0011"
    // ex.Message       --> "CSEV0011: Method calls blocked by sandbox: ToUpper"
}
```

## Recommended Catch Order

Catch from most specific to least specific:

```csharp
try
{
    engine.Evaluate(userExpression);
}
catch (AlderSandboxException ex)
{
    // Sandbox violation -- user tried a blocked operation
}
catch (AlderExecutionLimitException ex)
{
    // Resource limit exceeded (infinite loop, timeout)
}
catch (AlderLanguageModeException ex)
{
    // Extended-mode feature in Standard mode
}
catch (AlderDepthException ex)
{
    // Expression too deeply nested
}
catch (AlderLexerException ex)
{
    // Tokenization error (bad string literals, etc.)
}
catch (AlderParserException ex)
{
    // Syntax error
}
catch (AlderException ex)
{
    // Any other Alder error (type errors, null access, etc.)
}
```

## Non-Throwing APIs

### TryEvaluate

Returns `true` if evaluation succeeds. The result is returned via an `out` parameter. No exception is thrown on failure.

```csharp
var engine = new AlderEngine();

if (engine.TryEvaluate("1 + 2", out object? result))
{
    // result --> 3
}
else
{
    // Expression failed -- result is null
}
```

### TryParse

Returns `true` if parsing succeeds. The parsed expression is returned via an `out` parameter.

```csharp
var engine = new AlderEngine();

if (engine.TryParse("1 + 2", out var expression))
{
    // expression is a valid AlderExpression
}
else
{
    // Syntax error -- expression is null
}
```

### TryValidate

Returns `true` if the expression is valid. When `false`, the diagnostics list contains structured error information including severity, code, message, and location.

```csharp
var engine = new AlderEngine();

if (!engine.TryValidate("undeclaredVar", out var diagnostics))
{
    foreach (var diag in diagnostics)
    {
        // diag.Severity  --> DiagnosticSeverity.Error
        // diag.Code      --> DiagnosticCode.CS0103
        // diag.Message   --> "CS0103: The name 'undeclaredVar' does not exist in the current context"
        // diag.Line       --> int? (line number)
        // diag.Column     --> int? (column number)
        // diag.SpanStart  --> int? (span start)
        // diag.SpanLength --> int? (span length)
    }
}
```

### AlderDiagnostic

The `AlderDiagnostic` record carries structured error information:

| Property     | Type                 | Description                    |
| ------------ | -------------------- | ------------------------------ |
| `Severity`   | `DiagnosticSeverity` | `Error`, `Warning`, or `Info`  |
| `Message`    | `string`             | The formatted error message    |
| `Code`       | `DiagnosticCode?`    | The diagnostic code, or `null` |
| `Line`       | `int?`               | 1-based line number            |
| `Column`     | `int?`               | 1-based column number          |
| `SpanStart`  | `int?`               | 0-based span start             |
| `SpanLength` | `int?`               | Span length                    |

## ControlFlowSignal (Not an Exception)

`return`, `break`, `continue`, `goto case`, `goto default`, and `goto` are implemented internally as `ControlFlowSignal` value objects, **not** as exceptions. This means:

- User `catch` blocks **cannot** intercept control flow signals
- No SEH overhead or stack trace capture for control flow
- The engine handles these signals internally and they never escape to the caller

This is by design for performance and correctness.

## See Also

- [Error Codes](../diagnostics/error-codes/) -- complete catalog of all CS and CSEV diagnostic codes
