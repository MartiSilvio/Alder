---
title: "Checked and Unchecked"
description: "Checked and unchecked overflow control using expression and block syntax in CsEval."
sidebar:
  order: 6
---

## Overview

CsEval supports both the expression forms `checked(expr)` / `unchecked(expr)` and the block forms `checked { }` / `unchecked { }`. They control whether integer arithmetic overflow throws an `OverflowException` or wraps silently.

## Default Behavior

The default arithmetic context is **unchecked** (wrapping), matching C# runtime defaults. Integer overflow wraps silently without throwing.

```csharp
int.MaxValue + 1
// output: -2147483648
```

## checked(expr)

The `checked()` expression evaluates its operand in a checked context. If the operation overflows, it throws `System.OverflowException`.

### Integer Overflow

```csharp
checked(int.MaxValue + 1)
// output: System.OverflowException: Arithmetic operation resulted in an overflow.
```

### Long Overflow

```csharp
checked(long.MaxValue + 1L)
// output: System.OverflowException: Arithmetic operation resulted in an overflow.
```

### Cast Overflow

Narrowing casts that lose data also throw in a checked context.

```csharp
checked((byte)256)
// output: System.OverflowException: Arithmetic operation resulted in an overflow.
```

### Safe Arithmetic

When the result fits in the target type, `checked()` returns the value normally.

```csharp
checked(100 + 200)
// output: 300
```

## unchecked(expr)

The `unchecked()` expression evaluates its operand in an unchecked context. Overflow wraps around according to two's complement arithmetic.

### Integer Wrapping

```csharp
unchecked(int.MaxValue + 1)
// output: -2147483648
```

### Long Wrapping

```csharp
unchecked(long.MaxValue + 1L)
// output: -9223372036854775808
```

### Cast Truncation

Narrowing casts in an unchecked context truncate the value.

```csharp
unchecked((byte)256)
// output: 0
```

## Nesting

`checked()` and `unchecked()` can be nested. The innermost context applies.

```csharp
checked(unchecked(int.MaxValue + 1))
// output: -2147483648
```

The outer `checked` sets a checked context, but the inner `unchecked` overrides it for the addition. The result wraps.

```csharp
unchecked(checked(100 + 200))
// output: 300
```

The inner `checked` applies to `100 + 200`, which does not overflow, so the result is simply `300`.

## Block Forms

The block forms apply the checked/unchecked context to all statements within the block.

```csharp
{
    var threw = false;
    try { checked { var x = int.MaxValue + 1; } }
    catch (System.OverflowException) { threw = true; }
    return threw;
}
// output: True
```

```csharp
{
    var result = 0;
    unchecked { result = int.MaxValue + 1; }
    return result;
}
// output: -2147483648
```

## Affected Operations

The checked/unchecked context affects:

- **Arithmetic:** `+`, `-`, `*`, unary `-` on integer types
- **Casts:** Narrowing conversions between integer types (e.g., `(byte)int_value`)
- **Increment/Decrement:** `++` and `--` on integer variables

Floating-point arithmetic (`float`, `double`, `decimal`) is not affected by checked/unchecked context. Floating-point overflow follows IEEE 754 rules regardless.

## See Also

- [Numeric Types](../types/numeric-types) -- integer overflow behavior and literal formats
- [Arithmetic Operators](../operators/arithmetic) -- overflow examples with `checked`/`unchecked`
- [Exception Handling](./exception-handling) -- catching `OverflowException`
