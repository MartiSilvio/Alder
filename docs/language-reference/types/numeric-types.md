---
title: "Numeric Types"
description: "Integral and floating-point types, numeric promotions, literal formats, and arithmetic behavior in Alder."
sidebar:
  order: 2
---

## Overview

Alder supports all standard C# numeric literal formats, suffixes, and promotion rules. Numeric behavior follows the ECMA-334 specification, including binary numeric promotion (section 12.4.7.3) and implicit constant expression conversions (section 10.2.11).

## Integer Literals

Integer literals default to `int`. If the value does not fit in `int`, the type is automatically promoted.

### Decimal Integers

Promotion chain for unsuffixed decimal literals: `int` -> `long` -> `ulong` -> error.

```csharp
42
// output: 42

3000000000 > int.MaxValue
// output: True

10000000000000000000 > long.MaxValue
// output: True
```

### Hexadecimal Integers

Prefixed with `0x` or `0X`. Promotion chain: `int` -> `uint` -> `long` -> `ulong` -> error. Hex and binary literals include `uint` in the promotion chain because their bit patterns can represent unsigned values naturally.

```csharp
0xFF
// output: 255

0xFFFFFFFF > int.MaxValue
// output: True
```

### Binary Integers

Prefixed with `0b` or `0B`. Promotion chain: `int` -> `uint` -> `long` -> `ulong` -> error.

```csharp
0b1010
// output: 10
```

### Digit Separators

The `_` character can appear between digits for readability. It has no effect on the value or type.

```csharp
1_000_000
// output: 1000000

0xFF_FF
// output: 65535

0b1010_0101
// output: 165
```

## Floating-Point Literals

Numeric literals with a decimal point or exponent default to `double`.

### Decimal Point

```csharp
3.14
// output: 3.14
```

### Leading Decimal

A literal can begin with `.` (no leading zero required).

```csharp
.5
// output: 0.5
```

### Exponent Notation

Use `e` or `E` followed by an optional sign and exponent digits.

```csharp
1e10
// output: 10000000000

1.5E-3
// output: 0.0015

.5e2
// output: 50
```

## Numeric Suffixes

Suffixes force a specific numeric type. All suffixes are case-insensitive.

| Suffix                    | Type      | Example | Result           |
| ------------------------- | --------- | ------- | ---------------- |
| `L` / `l`                 | `long`    | `42L`   | `System.Int64`   |
| `U` / `u`                 | `uint`    | `42U`   | `System.UInt32`  |
| `UL` / `ul` / `LU` / `lu` | `ulong`   | `42UL`  | `System.UInt64`  |
| `F` / `f`                 | `float`   | `3.14F` | `System.Single`  |
| `D` / `d`                 | `double`  | `42D`   | `System.Double`  |
| `M` / `m`                 | `decimal` | `3.14M` | `System.Decimal` |

```csharp
42L
// output: 42

3.14f + 0f
// output: 3.14

100M + 0M
// output: 100
```

Floating-point suffixes (`F`, `D`, `M`) are valid on decimal and leading-decimal literals but not on hex or binary literals.

Integer suffixes (`L`, `U`, `UL`) are valid on decimal, hex, and binary integers.

## Binary Numeric Promotion

When a binary operator has two numeric operands of different types, both operands are promoted to a common type according to these rules from ECMA-334 section 12.4.7.3. The rules are evaluated in order; the first matching rule determines the result type.

| Rule | Condition                                                  | Result Type | Notes                                               |
| ---- | ---------------------------------------------------------- | ----------- | --------------------------------------------------- |
| 1    | Either operand is `decimal`                                | `decimal`   | Error if other is `float` or `double`               |
| 2    | Either operand is `double`                                 | `double`    |                                                     |
| 3    | Either operand is `float`                                  | `float`     |                                                     |
| 4    | Either operand is `ulong`                                  | `ulong`     | Error if other is a signed integer type             |
| 5    | Either operand is `long`                                   | `long`      |                                                     |
| 6    | One operand is `uint`, other is `sbyte`, `short`, or `int` | `long`      |                                                     |
| 7    | Either operand is `uint`                                   | `uint`      |                                                     |
| 8    | Otherwise                                                  | `int`       | Includes `byte`, `sbyte`, `short`, `ushort`, `char` |

### The `char` Exception

`char` is not treated as a signed integer. Per ECMA-334, `char` has implicit conversions to `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, and `decimal` -- but not to signed-only types like `sbyte` or `short`. This means:

- `uint + char` -> `uint` (Rule 7), **not** `long` (Rule 6)
- `byte + byte` -> `int` (Rule 8)
- `char + char` -> `int` (Rule 8)

```csharp
1u + 'A'
// output: 66

(byte)1 + (byte)2
// output: 3

1L + 1.0
// output: 2

1 + 1.5m
// output: 2.5
```

### Rule-by-Rule Examples

```csharp
// Rule 1: decimal wins
1m + 2
// output: 3

// Rule 2: double wins
1L + 2.0
// output: 3

// Rule 3: float wins
1 + 2.0f
// output: 3

// Rule 5: long wins
1L + 2
// output: 3

// Rule 6: uint + int -> long
1u + 2
// output: 3

// Rule 7: uint + char -> uint (char is not signed)
1u + 'A'
// output: 66

// Rule 8: small types promote to int
(short)1 + (short)2
// output: 3
```

## Implicit Constant Expression Conversions

When a compile-time constant expression of type `int` appears in a context requiring a narrower type, Alder permits implicit conversion if the value fits in the target type. This follows ECMA-334 section 10.2.11.

```csharp
byte b = 42
// output: 42
```

The value `42` is a constant of type `int`, but it fits in `byte` (0-255), so the assignment succeeds without an explicit cast.

Similarly, when one operand is `uint` and the other is a non-negative `int` constant, the constant is promoted to `uint` rather than widening both to `long`. The same applies for `ulong` with non-negative `int` constants.

## Checked and Unchecked Context

Arithmetic overflow behavior depends on the execution context. In a `checked` context, overflow throws `System.OverflowException`. In an `unchecked` context (the default), overflow wraps silently.

```csharp
unchecked(int.MaxValue + 1)
// output: -2147483648

checked(int.MaxValue + 1)
// output: System.OverflowException: Arithmetic operation resulted in an overflow.
```

For full checked/unchecked semantics, see the statements reference.

## See Also

- [Built-in Types](./built-in-types) -- complete type keyword list with CLR mappings
- [Nullable and Conversions](./nullable-and-conversions) -- implicit/explicit conversion rules and nullable semantics
