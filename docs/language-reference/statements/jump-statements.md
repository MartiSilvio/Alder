---
title: "Jump Statements"
description: "break, continue, return, goto, goto case, and goto default statements in Alder."
sidebar:
  order: 4
---

## Overview

Jump statements transfer control to a different point in the program. Alder supports `return`, `break`, `continue`, `goto` (with labels), `goto case`, and `goto default`.

Alder propagates jump semantics using `ControlFlowSignal` value objects rather than exceptions. This means user `catch` blocks cannot accidentally intercept control flow -- a `break` inside a `try` block will not be caught by a `catch` clause.

## return

The `return` statement exits the current block and optionally provides a value.

### Returning a Value

```csharp
{ return 42; }
// output: 42
```

```csharp
{ var x = 10; return x * 2; }
// output: 20
```

### Void Return

A `return;` with no value exits the block. The block evaluates to `null`.

```csharp
{ var x = 0; return; }
// output:
```

### Early Return

`return` exits immediately, skipping remaining statements.

```csharp
{
    var x = 5;
    if (x > 3) return "big";
    return "small";
}
// output: big
```

## break

The `break` statement exits the nearest enclosing loop or `switch` statement.

### In Loops

```csharp
{
    var i = 0;
    while (true) { if (i == 3) break; i++; }
    return i;
}
// output: 3
```

```csharp
{
    var sum = 0;
    for (var i = 1; i <= 10; i++)
    {
        if (i > 5) break;
        sum += i;
    }
    return sum;
}
// output: 15
```

### In switch

Every non-empty `case` in a `switch` statement must end with `break`, `return`, `goto case`, or `goto default`.

```csharp
{
    var result = "";
    switch (2)
    {
        case 1: result = "one"; break;
        case 2: result = "two"; break;
        default: result = "other"; break;
    }
    return result;
}
// output: two
```

## continue

The `continue` statement skips the rest of the current loop iteration and proceeds to the next iteration.

```csharp
{
    var sum = 0;
    for (var i = 1; i <= 5; i++)
    {
        if (i % 2 == 0) continue;
        sum += i;
    }
    return sum;
}
// output: 9
```

```csharp
{
    var count = 0;
    var i = 0;
    while (i < 10)
    {
        i++;
        if (i % 3 == 0) continue;
        count++;
    }
    return count;
}
// output: 7
```

## goto

The `goto` statement transfers control to a labeled statement within the same block.

### Forward Jump

```csharp
{
    var x = 0;
    goto skip;
    x = 99;
    skip:
    return x;
}
// output: 0
```

### Backward Jump

Alder supports backward `goto` jumps. The label scanner searches the entire block, not just forward.

```csharp
{
    var count = 0;
    start:
    count++;
    if (count < 3) goto start;
    return count;
}
// output: 3
```

:::note
Backward `goto` jumps are subject to the `MaxStatements` execution limit. An unbounded backward jump will be terminated by the statement counter, not run forever.
:::

### Labels

A label is an identifier followed by `:` that marks a target for `goto`. Labels are block-scoped -- you can only `goto` a label within the same block.

```csharp
{
    var result = 1;
    goto done;
    result = result * 100;
    done:
    return result;
}
// output: 1
```

## goto case and goto default

Inside a `switch` statement, `goto case` and `goto default` transfer control to a different case.

### goto case

```csharp
{
    var result = "";
    switch (1)
    {
        case 1: result = "one"; goto case 2;
        case 2: result = result + "+two"; break;
    }
    return result;
}
// output: one+two
```

### goto default

```csharp
{
    var result = "";
    switch (1)
    {
        case 1: result = "one"; goto default;
        default: result = result + "+def"; break;
    }
    return result;
}
// output: one+def
```

### Empty Case Fall-Through

Empty cases (with no statements) fall through to the next case naturally:

```csharp
{
    var result = "";
    switch (1)
    {
        case 1:
        case 2: result = "one or two"; break;
        default: result = "other"; break;
    }
    return result;
}
// output: one or two
```

## ControlFlowSignal Mechanism

Alder does not use .NET exceptions for control flow. Instead, `return`, `break`, `continue`, and `goto` produce `ControlFlowSignal` value objects that propagate up the call stack. This design:

- Avoids the performance overhead of SEH (structured exception handling)
- Prevents user `catch` blocks from intercepting control flow signals
- Allows `break` and `continue` to work correctly inside `try` blocks

```csharp
{
    var result = 0;
    for (var i = 0; i < 10; i++)
    {
        try { if (i == 3) break; }
        catch { result = -1; }
        result = i;
    }
    return result;
}
// output: 2
```

In this example, the `break` at `i == 3` exits the loop cleanly. The `catch` block does not intercept it.

## See Also

- [Iteration Statements](./iteration-statements) -- loops that use `break` and `continue`
- [Selection Statements](./selection-statements) -- `switch` with `break`, `goto case`, `goto default`
- [Exception Handling](./exception-handling) -- `try`/`catch`/`finally`
