---
title: "Iteration Statements"
description: "for, foreach, while, and do-while loops with execution limit behavior in CsEval."
sidebar:
  order: 3
---

## Overview

CsEval supports `for`, `foreach`, `while`, and `do-while` loops. All loops are subject to the `MaxStatements` execution limit, which protects against infinite loops. The loop statements themselves require no sandbox flags, but operations inside them may require flags like `AllowMethodCalls` or `AllowConstruction`.

## for Loop

### Basic for Loop

```csharp
{ var sum = 0; for (var i = 1; i <= 5; i++) { sum += i; } return sum; }
// output: 15
```

### Multiple Initializers and Iterators

The `for` statement supports multiple initializers and iterators separated by commas:

```csharp
{ var sum = 0; for (int i = 0, j = 10; i < 3; i++, j--) { sum += j; } return sum; }
// output: 27
```

### Empty Parts (Infinite Loop with Break)

All parts of the `for` header can be omitted. Use `break` to exit:

```csharp
{ var i = 0; for (;;) { i++; if (i >= 3) break; } return i; }
// output: 3
```

## foreach Loop

The `foreach` loop iterates over arrays, `List<T>`, and any `IEnumerable`.

### Array

```csharp
{ var sum = 0; foreach (var x in new int[] {1, 2, 3}) { sum += x; } return sum; }
// output: 6
```

### List

```csharp
{
    var list = new List<int>();
    list.Add(10);
    list.Add(20);
    list.Add(30);
    var sum = 0;
    foreach (var x in list) { sum += x; }
    return sum;
}
// output: 60
```

### String Characters

```csharp
{ var count = 0; foreach (var c in "hello") { count++; } return count; }
// output: 5
```

## while Loop

```csharp
{ var i = 0; while (i < 3) { i++; } return i; }
// output: 3
```

### Counting Pattern

```csharp
{
    var sum = 0;
    var n = 1;
    while (n <= 100)
    {
        sum += n;
        n++;
    }
    return sum;
}
// output: 5050
```

## do-while Loop

The `do-while` loop always executes the body at least once before checking the condition.

```csharp
{ var i = 0; do { i++; } while (i < 3); return i; }
// output: 3
```

### Executes At Least Once

```csharp
{ var i = 0; do { i++; } while (false); return i; }
// output: 1
```

## Nested Loops

Loops can be nested to any depth. Each loop body creates its own child scope.

```csharp
{
    var count = 0;
    for (var i = 0; i < 3; i++)
    {
        for (var j = 0; j < 3; j++)
        {
            count++;
        }
    }
    return count;
}
// output: 9
```

### break and continue

`break` exits the innermost loop. `continue` skips to the next iteration.

```csharp
{
    var sum = 0;
    for (var i = 0; i < 10; i++)
    {
        if (i % 2 == 0) continue;
        if (i > 7) break;
        sum += i;
    }
    return sum;
}
// output: 16
```

## Execution Limits

All loops are subject to the `MaxStatements` execution limit configured via `ExecutionConstraints`. When the statement count exceeds the limit, CsEval throws `CsEvalExecutionLimitException`. This protects against accidental infinite loops.

```csharp
// With MaxStatements = 10, an infinite loop is terminated:
{ var i = 0; while (true) { i++; } return i; }
// output: CsEvalExecutionLimitException: Execution exceeded maximum statement count
```

The default `CsEvalOptions.Default` has no statement limit. Set `Constraints` to enable it:

```csharp
var options = CsEvalOptions.Default with
{
    Constraints = new ExecutionConstraints { MaxStatements = 1000 }
};
```

## See Also

- [Declaration Statements](./declaration-statements) -- variable declarations and scoping
- [Selection Statements](./selection-statements) -- if/else and switch
