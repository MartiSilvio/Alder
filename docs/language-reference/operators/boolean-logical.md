---
title: "Boolean Logical Operators"
description: "Logical negation, AND, OR, XOR, and short-circuit conditional operators in Alder."
sidebar:
  order: 5
---

## Overview

Alder supports the standard C# boolean logical operators. The `&`, `|`, and `^` operators are **dual-purpose** -- they perform boolean logic when operands are `bool` and bitwise operations when operands are integers. This page documents the boolean behavior; see [Bitwise and shift operators](./bitwise-and-shift) for integer behavior.

## Logical Negation (`!`)

Returns the opposite of its `bool` operand.

```csharp
!true
// output: False

!false
// output: True
```

## Logical AND (`&`) -- Non-Short-Circuit

Evaluates **both** operands and returns `true` only if both are `true`. Unlike `&&`, the right operand is always evaluated.

```csharp
true & true
// output: True

true & false
// output: False

false & true
// output: False
```

## Logical OR (`|`) -- Non-Short-Circuit

Evaluates **both** operands and returns `true` if either is `true`.

```csharp
true | false
// output: True

false | false
// output: False
```

## Logical XOR (`^`)

Returns `true` if the operands differ.

```csharp
true ^ false
// output: True

true ^ true
// output: False

false ^ false
// output: False
```

## Conditional AND (`&&`) -- Short-Circuit

Returns `true` if both operands are `true`. The right operand is **not evaluated** if the left is `false`.

```csharp
true && true
// output: True

true && false
// output: False

false && true
// output: False
```

### Short-Circuit Behavior

When the left operand is `false`, the right operand is never evaluated. This is useful for guarding expressions that would fail on null or invalid input.

```csharp
{ var s = (string)null; return s != null && s == "hello"; }
// output: False
```

## Conditional OR (`||`) -- Short-Circuit

Returns `true` if either operand is `true`. The right operand is **not evaluated** if the left is `true`.

```csharp
true || false
// output: True

false || true
// output: True

false || false
// output: False
```

### Short-Circuit Behavior

When the left operand is `true`, the right operand is never evaluated.

```csharp
{ var s = (string)null; return s == null || s == "hello"; }
// output: True
```

## Three-Value Logic (`bool?`)

When operands are nullable booleans (`bool?`), the logical operators follow ECMA-334 section 12.13.5 three-value logic. A `null` value represents an unknown truth value.

### `&&` with `bool?`

|  Left   |  Right  | Result  |
| :-----: | :-----: | :-----: |
| `true`  | `true`  | `true`  |
| `true`  | `false` | `false` |
| `true`  | `null`  | `null`  |
| `false` | `true`  | `false` |
| `false` | `false` | `false` |
| `false` | `null`  | `false` |
| `null`  | `true`  | `null`  |
| `null`  | `false` | `false` |
| `null`  | `null`  | `null`  |

```csharp
(bool?)false && (bool?)null
// output:

(bool?)true && (bool?)null
// output:

(bool?)null && (bool?)null
// output:
```

:::note
`false && null` returns `false` because if one operand is definitely false, the conjunction is false regardless of the unknown value. `true && null` returns `null` because the result depends on the unknown value.
:::

### `||` with `bool?`

|  Left   |  Right  | Result  |
| :-----: | :-----: | :-----: |
| `true`  | `true`  | `true`  |
| `true`  | `false` | `true`  |
| `true`  | `null`  | `true`  |
| `false` | `true`  | `true`  |
| `false` | `false` | `false` |
| `false` | `null`  | `null`  |
| `null`  | `true`  | `true`  |
| `null`  | `false` | `null`  |
| `null`  | `null`  | `null`  |

```csharp
(bool?)true || (bool?)null
// output: True

(bool?)false || (bool?)null
// output:

(bool?)null || (bool?)null
// output:
```

### `&` with `bool?`

The non-short-circuit `&` operator on `bool?` follows the same three-value truth table as `&&`:

```csharp
(bool?)false & (bool?)null
// output:

(bool?)true & (bool?)true
// output: True
```

### `|` with `bool?`

The non-short-circuit `|` operator on `bool?` follows the same three-value truth table as `||`:

```csharp
(bool?)true | (bool?)null
// output: True

(bool?)false | (bool?)null
// output:
```

### `^` with `bool?`

XOR on `bool?` returns `null` if either operand is `null`:

```csharp
(bool?)true ^ (bool?)null
// output:

(bool?)true ^ (bool?)false
// output: True
```

### `!` with `bool?`

Logical negation of `null` returns `null`:

```csharp
!(bool?)true
// output: False

!(bool?)null
// output:
```

## See Also

- [Bitwise and shift operators](./bitwise-and-shift) -- `&`, `|`, `^` on integer types
- [Equality operators](./equality) -- `==`, `!=`
- [Comparison operators](./comparison) -- `<`, `>`, `<=`, `>=`
