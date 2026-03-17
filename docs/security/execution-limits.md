---
title: "Execution Limits"
description: "MaxStatements, MaxTimeout, and MaxExpressionDepth -- preventing runaway expressions."
sidebar:
  order: 2
---

## Overview

Execution limits prevent runaway expressions from consuming unbounded resources. CsEval provides three independent limits:

| Limit | Location | Default | Mutable | Exception |
|-------|----------|---------|---------|-----------|
| `MaxStatements` | `ExecutionConstraints` | `null` (unlimited) | Yes | `CsEvalExecutionLimitException` |
| `MaxTimeout` | `ExecutionConstraints` | `null` (unlimited) | Yes | `CsEvalExecutionLimitException` |
| `MaxExpressionDepth` | `CsEvalOptions` | `512` | No | `CsEvalDepthException` |

## MaxStatements

Caps the number of statements executed per `Evaluate()` call. Each loop iteration, block statement, and top-level expression counts as one statement. When exceeded, throws `CsEvalExecutionLimitException`.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Constraints = new ExecutionConstraints { MaxStatements = 1000 }
});

// Under limit -- completes normally
{ var sum = 0; for (int i = 0; i < 10; i++) sum += i; return sum; }
// output: 45

// Exceeds limit -- throws
{ while (true) {} }
// throws CsEvalExecutionLimitException
```

## MaxTimeout

Caps wall-clock time per `Evaluate()` call. Uses `Stopwatch` for low-overhead monotonic timing, checked at statement boundaries. When exceeded, throws `CsEvalExecutionLimitException`.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Constraints = new ExecutionConstraints { MaxTimeout = TimeSpan.FromSeconds(2) }
});

// Fast expression -- completes normally
1 + 2
// output: 3

// Slow expression -- throws after timeout
{ while (true) {} }
// throws CsEvalExecutionLimitException
```

:::note
The timeout is checked at statement boundaries, not continuously. A single long-running host method call can exceed the timeout before the next check point.
:::

## MaxExpressionDepth

Caps expression nesting depth. The evaluator and IL compiler enforce this cap independently. When exceeded, throws a catchable `CsEvalDepthException` instead of risking an uncatchable `StackOverflowException`.

`MaxExpressionDepth` lives on `CsEvalOptions`, not on `ExecutionConstraints`. It defaults to `512` and is set at engine creation time.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    MaxExpressionDepth = 100
});

// Deeply nested expression exceeding the limit
// throws CsEvalDepthException
```

## Mutability Difference

`ExecutionConstraints` is a **mutable class**. Its properties (`MaxStatements`, `MaxTimeout`) can be changed between evaluations on the same engine. This lets you tighten or relax limits without creating a new engine.

`MaxExpressionDepth` is an **immutable property** on `CsEvalOptions` (a sealed record with `init`-only setters). It is fixed at engine creation and cannot change after.

```csharp
var constraints = new ExecutionConstraints { MaxStatements = 1000 };
var engine = new CsEvalEngine(new CsEvalOptions
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

`CsEvalExecutionLimitException` provides detailed information about the exceeded limit:

| Property | Type | Description |
|----------|------|-------------|
| `LimitType` | `ExecutionLimitType` | `Statements` or `Timeout` |
| `LimitValue` | `long` | The configured limit (statement count or timeout in ms) |
| `ActualValue` | `long` | The actual value when the limit was hit |
| `StatementsExecuted` | `long` | Total statements executed when the exception was thrown |
| `ElapsedTime` | `TimeSpan` | Wall-clock time elapsed when the exception was thrown |

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Constraints = new ExecutionConstraints { MaxStatements = 100 }
});

try
{
    engine.Evaluate("{ while (true) {} }");
}
catch (CsEvalExecutionLimitException ex)
{
    // ex.LimitType == ExecutionLimitType.Statements
    // ex.LimitValue == 100
    // ex.StatementsExecuted >= 100
}
```

`CsEvalDepthException` provides:

| Property | Type | Description |
|----------|------|-------------|
| `MaxDepth` | `int` | The configured maximum depth that was exceeded |

## See Also

- [Sandbox Overview](../security/sandbox-overview/) -- Permission flags, presets, reflection guard
- [Common Mistakes](../security/common-mistakes/) -- Security anti-patterns with wrong/right code pairs
