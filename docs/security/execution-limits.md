---
title: "Execution Limits"
description: "MaxStatements, MaxTimeout, and MaxExpressionDepth -- preventing runaway expressions."
sidebar:
  order: 2
---

## Overview

Execution limits prevent runaway expressions from consuming unbounded resources. Alder provides three independent limits:

| Limit                | Location               | Default            | Mutable | Exception                      |
| -------------------- | ---------------------- | ------------------ | ------- | ------------------------------ |
| `MaxStatements`      | `ExecutionConstraints` | `null` (unlimited) | Yes     | `AlderExecutionLimitException` |
| `MaxTimeout`         | `ExecutionConstraints` | `null` (unlimited) | Yes     | `AlderExecutionLimitException` |
| `MaxExpressionDepth` | `AlderOptions`         | `512`              | No      | `AlderDepthException`          |

## MaxStatements

Caps the number of statements executed per `Evaluate()` call. Each loop iteration, block statement, and top-level expression counts as one statement. When exceeded, throws `AlderExecutionLimitException`.

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Constraints = new ExecutionConstraints { MaxStatements = 1000 }
});

// Under limit -- completes normally
{ var sum = 0; for (int i = 0; i < 10; i++) sum += i; return sum; }
// output: 45

// Exceeds limit -- throws
{ while (true) {} }
// throws AlderExecutionLimitException
```

## MaxTimeout

Caps wall-clock time per `Evaluate()` call. Uses `Stopwatch` for low-overhead monotonic timing, checked at statement boundaries. When exceeded, throws `AlderExecutionLimitException`.

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Constraints = new ExecutionConstraints { MaxTimeout = TimeSpan.FromSeconds(2) }
});

// Fast expression -- completes normally
1 + 2
// output: 3

// Slow expression -- throws after timeout
{ while (true) {} }
// throws AlderExecutionLimitException
```

:::note
The timeout is checked at statement boundaries, not continuously. A single long-running host method call can exceed the timeout before the next check point.
:::

## MaxExpressionDepth

Caps expression nesting depth. The evaluator and IL compiler enforce this cap independently. When exceeded, throws a catchable `AlderDepthException` instead of risking an uncatchable `StackOverflowException`.

`MaxExpressionDepth` lives on `AlderOptions`, not on `ExecutionConstraints`. It defaults to `512` and is set at engine creation time.

```csharp
var engine = new AlderEngine(new AlderOptions
{
    MaxExpressionDepth = 100
});

// Deeply nested expression exceeding the limit
// throws AlderDepthException
```

## Mutability Difference

`ExecutionConstraints` is a **mutable class**. Its properties (`MaxStatements`, `MaxTimeout`) can be changed between evaluations on the same engine. This lets you tighten or relax limits without creating a new engine.

`MaxExpressionDepth` is an **immutable property** on `AlderOptions` (a sealed record with `init`-only setters). It is fixed at engine creation and cannot change after.

```csharp
var constraints = new ExecutionConstraints { MaxStatements = 1000 };
var engine = new AlderEngine(new AlderOptions
{
    Constraints = constraints
});

// First evaluation: 1000-statement limit
{ var sum = 0; for (int i = 0; i < 10; i++) sum += i; return sum; }
// output: 45

// Relax limit for next evaluation
constraints.MaxStatements = 50_000;

// Second evaluation: 50,000-statement limit
{ var sum = 0; for (int i = 0; i < 1000; i++) sum += i; return sum; }
// output: 499500
```

## Exception Properties

`AlderExecutionLimitException` provides detailed information about the exceeded limit:

| Property             | Type                 | Description                                             |
| -------------------- | -------------------- | ------------------------------------------------------- |
| `LimitType`          | `ExecutionLimitType` | `Statements` or `Timeout`                               |
| `LimitValue`         | `long`               | The configured limit (statement count or timeout in ms) |
| `ActualValue`        | `long`               | The actual value when the limit was hit                 |
| `StatementsExecuted` | `long`               | Total statements executed when the exception was thrown |
| `ElapsedTime`        | `TimeSpan`           | Wall-clock time elapsed when the exception was thrown   |

```csharp
var engine = new AlderEngine(new AlderOptions
{
    Constraints = new ExecutionConstraints { MaxStatements = 100 }
});

try
{
    engine.Evaluate("{ while (true) {} }");
}
catch (AlderExecutionLimitException ex)
{
    // ex.LimitType == ExecutionLimitType.Statements
    // ex.LimitValue == 100
    // ex.StatementsExecuted >= 100
}
```

`AlderDepthException` provides:

| Property   | Type  | Description                                    |
| ---------- | ----- | ---------------------------------------------- |
| `MaxDepth` | `int` | The configured maximum depth that was exceeded |

## See Also

- [Sandbox Overview](../security/sandbox-overview/) -- Permission flags, presets, reflection guard
- [Common Mistakes](../security/common-mistakes/) -- Security anti-patterns with wrong/right code pairs
