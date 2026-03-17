---
title: "Assignment Operators"
description: "Simple and compound assignment operators in CsEval, including sandbox requirements."
sidebar:
  order: 7
---

## Overview

CsEval supports simple assignment and all standard C# compound assignment operators.

:::caution
**All assignment operators require the `AllowAssignment` sandbox flag.** With the default `SandboxOptions.Trusted()` preset, this is already enabled. Variable *declarations* (`var x = 5`) are always allowed regardless of sandbox settings -- only reassignment is gated.
:::

## Simple Assignment (`=`)

Assigns the right operand's value to the left operand.

```csharp
{ var x = 5; x = 10; return x; }
// output: 10
```

## Compound Assignment Operators

Compound assignment operators combine an operation with assignment. The expression `x op= y` is equivalent to `x = x op y`, except that `x` is evaluated only once.

| Operator | Equivalent | Base Operator |
|:---:|:---:|:---:|
| `+=` | `x = x + y` | `+` |
| `-=` | `x = x - y` | `-` |
| `*=` | `x = x * y` | `*` |
| `/=` | `x = x / y` | `/` |
| `%=` | `x = x % y` | `%` |
| `&=` | `x = x & y` | `&` |
| `\|=` | `x = x \| y` | `\|` |
| `^=` | `x = x ^ y` | `^` |
| `<<=` | `x = x << y` | `<<` |
| `>>=` | `x = x >> y` | `>>` |
| `>>>=` | `x = x >>> y` | `>>>` |

### Arithmetic Compound Assignment

```csharp
{ var x = 10; x += 5; return x; }
// output: 15

{ var x = 10; x -= 3; return x; }
// output: 7

{ var x = 4; x *= 3; return x; }
// output: 12

{ var x = 15; x /= 4; return x; }
// output: 3

{ var x = 17; x %= 5; return x; }
// output: 2
```

### Bitwise Compound Assignment

```csharp
{ var x = 0xFF; x &= 0x0F; return x; }
// output: 15

{ var x = 0x0F; x |= 0xF0; return x; }
// output: 255

{ var x = 0xFF; x ^= 0x0F; return x; }
// output: 240
```

### Shift Compound Assignment

```csharp
{ var x = 1; x <<= 3; return x; }
// output: 8

{ var x = 16; x >>= 2; return x; }
// output: 4

{ var x = -1; x >>>= 31; return x; }
// output: 1
```

## Null-Coalescing Assignment (`??=`)

The `??=` operator assigns the right operand to the left operand only if the left operand is `null`. See [Null operators](./null-operators) for full details.

:::caution
`??=` also requires the `AllowAssignment` sandbox flag.
:::

```csharp
{ int? x = null; x ??= 42; return x; }
// output: 42

{ int? x = 10; x ??= 42; return x; }
// output: 10
```

## Sandbox Requirements

### Variable Assignment

All assignment operators (`=`, `+=`, `-=`, etc.) on variables require the `AllowAssignment` sandbox flag. This flag is enabled in the `Trusted()`, `Safe()`, and `Strict()` presets.

### Member Assignment

Compound assignment on a member (e.g., `obj.Prop += 1`) requires **both** `AllowPropertyRead` (to read the current value) and `AllowPropertySet` (to write the new value), in addition to `AllowAssignment`.

### Index Assignment

Compound assignment on an indexed element (e.g., `arr[i] += 1`) requires `AllowIndexSet` in addition to `AllowAssignment`.

## See Also

- [Null operators](./null-operators) -- `??=` null-coalescing assignment
- [Arithmetic operators](./arithmetic) -- `++`, `--` increment/decrement (also require `AllowAssignment`)
- [Bitwise and shift operators](./bitwise-and-shift) -- base operators for `&=`, `|=`, `^=`, `<<=`, `>>=`, `>>>=`
