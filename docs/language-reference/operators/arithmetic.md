---
title: "Arithmetic Operators"
description: "Addition, subtraction, multiplication, division, remainder, and increment/decrement operators in CsEval."
sidebar:
  order: 2
---

## Overview

CsEval supports the standard C# arithmetic operators with full numeric promotion rules per ECMA-334 section 12.4.7.3. Arithmetic on smaller types (`byte`, `short`, `char`) is automatically promoted to `int`.

## Addition (`+`)

The `+` operator performs numeric addition or string concatenation depending on the operand types.

### Numeric Addition

```csharp
3 + 4
// output: 7

1.5 + 2.5
// output: 4

1 + 2L
// output: 3
```

### String Concatenation

When either operand is a `string`, the `+` operator performs string concatenation. The non-string operand is converted to its string representation.

```csharp
"hello" + " world"
// output: hello world

"value: " + 42
// output: value: 42
```

#### Null String Behavior

A `null` operand in string concatenation is treated as the empty string (ECMA-334 section 12.10.5).

```csharp
"hello" + null
// output: hello

null + "world"
// output: world
```

## Subtraction (`-`)

```csharp
10 - 3
// output: 7

5.5 - 2.0
// output: 3.5
```

## Multiplication (`*`)

```csharp
4 * 5
// output: 20

2.5 * 4.0
// output: 10
```

## Division (`/`)

### Integer Division

Integer division truncates toward zero and returns an integer result.

```csharp
10 / 3
// output: 3

-7 / 2
// output: -3
```

Integer division by zero throws `DivideByZeroException`.

```csharp
1 / 0
// output: System.DivideByZeroException: Attempted to divide by zero.
```

### Floating-Point Division

Floating-point division follows IEEE 754 rules. Division by zero produces `Infinity` or `NaN`, not an exception.

```csharp
10.0 / 3.0
// output: 3.3333333333333335

1.0 / 0.0
// output: Infinity

-1.0 / 0.0
// output: -Infinity

0.0 / 0.0
// output: NaN
```

## Remainder (`%`)

Returns the remainder after integer or floating-point division.

```csharp
10 % 3
// output: 1

-10 % 3
// output: -1

10.5 % 3.0
// output: 1.5
```

Integer remainder by zero throws `DivideByZeroException`.

```csharp
1 % 0
// output: System.DivideByZeroException: Attempted to divide by zero.
```

## Increment and Decrement (`++`, `--`)

The `++` and `--` operators increment or decrement a variable by one. They come in prefix and postfix forms.

:::note
Increment and decrement modify a variable, which requires the `AllowAssignment` sandbox flag. With the default `SandboxOptions.Trusted()` preset, this is already enabled. If using a restricted sandbox, these operators will throw `CsEvalSandboxException`.
:::

### Prefix

Prefix increment/decrement modifies the variable and returns the **new** value.

```csharp
{ var x = 5; return ++x; }
// output: 6

{ var x = 5; return --x; }
// output: 4
```

### Postfix

Postfix increment/decrement modifies the variable and returns the **original** value.

```csharp
{ var x = 5; return x++; }
// output: 5

{ var x = 5; x++; return x; }
// output: 6
```

## Numeric Promotion

Arithmetic operators follow binary numeric promotion rules. When operands have different types, both are promoted to a common type. See [Numeric types](../types/numeric-types) for the full promotion table.

Key rules:
- `byte`, `sbyte`, `short`, `ushort`, and `char` promote to `int`
- Mixed `int`/`long` promotes to `long`
- Mixed integer/floating-point promotes to the floating-point type
- `decimal` cannot mix with `float` or `double`

```csharp
(byte)100 + (byte)200
// output: 300

1 + 2L
// output: 3

1 + 1.5
// output: 2.5
```

## Checked and Unchecked Overflow

By default, integer arithmetic wraps on overflow (unchecked context). Use `checked()` to throw `OverflowException` on overflow.

```csharp
unchecked(int.MaxValue + 1)
// output: -2147483648

checked(int.MaxValue + 1)
// output: System.OverflowException: Arithmetic operation resulted in an overflow.
```

For full checked/unchecked semantics, see the statements reference.

## See Also

- [Numeric types](../types/numeric-types) -- numeric promotion rules, literal formats, and type behavior
- [Comparison operators](./comparison) -- `<`, `>`, `<=`, `>=`
- [Equality operators](./equality) -- `==`, `!=`
