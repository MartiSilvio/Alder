---
title: "Bitwise and Shift Operators"
description: "Bitwise AND, OR, XOR, complement, left shift, right shift, and unsigned right shift operators in CsEval."
sidebar:
  order: 6
---

## Overview

CsEval supports all standard C# bitwise and shift operators on integer types. The `&`, `|`, and `^` operators are **dual-purpose** -- they perform bitwise operations when operands are integers and boolean logic when operands are `bool`. This page documents the integer/bitwise behavior; see [Boolean logical operators](./boolean-logical) for `bool` behavior.

## Bitwise AND (`&`)

Produces a result where each bit is 1 only if both corresponding input bits are 1.

```csharp
0xFF & 0x0F
// output: 15

0b1100 & 0b1010
// output: 8
```

## Bitwise OR (`|`)

Produces a result where each bit is 1 if either corresponding input bit is 1.

```csharp
0xFF | 0x0F
// output: 255

0b1100 | 0b1010
// output: 14
```

## Bitwise XOR (`^`)

Produces a result where each bit is 1 if the corresponding input bits differ.

```csharp
0b1100 ^ 0b1010
// output: 6

0xFF ^ 0xFF
// output: 0
```

## Bitwise Complement (`~`)

Inverts every bit in the operand.

```csharp
~0
// output: -1

~-1
// output: 0
```

## Left Shift (`<<`)

Shifts bits left by the specified count. Vacated low-order bits are set to zero.

```csharp
1 << 3
// output: 8

0xFF << 4
// output: 4080
```

## Right Shift (`>>`)

Shifts bits right by the specified count. For signed types, the sign bit is propagated (arithmetic shift). For unsigned types, vacated high-order bits are set to zero.

```csharp
8 >> 2
// output: 2

-8 >> 2
// output: -2

0xFFu >> 4
// output: 15
```

## Unsigned Right Shift (`>>>`)

Shifts bits right by the specified count, always filling vacated high-order bits with zero regardless of the sign of the left operand. This is a C# 11 feature supported in CsEval.

```csharp
-1 >>> 31
// output: 1

-8 >>> 2
// output: 1073741822
```

## Shift Count Masking

The shift count is automatically masked to prevent shifting by more bits than the type contains:

- For `int` and smaller types: count is masked with `0x1F` (5 low-order bits), so the effective shift is 0-31.
- For `long`: count is masked with `0x3F` (6 low-order bits), so the effective shift is 0-63.

```csharp
1 << 33
// output: 2

1L << 65
// output: 2
```

## Numeric Promotion

Bitwise and shift operators follow standard numeric promotion rules:

- `byte`, `sbyte`, `short`, `ushort`, and `char` are promoted to `int` before the operation.
- Mixed-type operations promote to the wider type per ECMA-334 binary numeric promotion rules.

```csharp
(byte)0xFF & (byte)0x0F
// output: 15

(byte)0x0F | 0x100
// output: 271
```

## Enum Bitwise Operations

The `&`, `|`, `^`, and `~` operators work on enum types, which is useful for working with flags enums.

```csharp
System.IO.FileAccess.Read | System.IO.FileAccess.Write
// output: ReadWrite

System.IO.FileAccess.ReadWrite & System.IO.FileAccess.Read
// output: Read
```

## Lifted Operators (`T?`)

When operands are nullable integer types, bitwise and shift operators return `null` if either operand is `null`.

```csharp
(int?)null & 0xFF
// output:

(int?)5 | (int?)3
// output: 7
```

## See Also

- [Boolean logical operators](./boolean-logical) -- `&`, `|`, `^` on `bool` types
- [Arithmetic operators](./arithmetic) -- `+`, `-`, `*`, `/`, `%`
- [Assignment operators](./assignment) -- compound bitwise assignment `&=`, `|=`, `^=`, `<<=`, `>>=`, `>>>=`
