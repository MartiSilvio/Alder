---
title: "Comparison Operators"
description: "Less than, greater than, and their or-equal variants with nullable and NaN semantics in Alder."
sidebar:
  order: 3
---

## Overview

The comparison operators `<`, `>`, `<=`, `>=` compare numeric values and return a `bool` result. They follow standard numeric promotion rules and support nullable lifted semantics.

## Basic Comparison

```csharp
3 < 5
// output: True

5 > 3
// output: True

3 <= 3
// output: True

5 >= 10
// output: False
```

## Numeric Promotion

Comparison operands undergo the same binary numeric promotion as arithmetic operators. See [Numeric types](../types/numeric-types) for the full promotion table.

```csharp
1 < 2L
// output: True

1 < 1.5
// output: True

100L > 50.0
// output: True
```

## Char Comparison

`char` values are compared by their underlying `ushort` numeric value.

```csharp
'a' < 'z'
// output: True

'A' < 'a'
// output: True

'B' > 'A'
// output: True
```

## Nullable Lifted Comparison

When either operand of a comparison is nullable, the operator uses **lifted** semantics (ECMA-334 section 12.4.8). If either operand is `null`, the comparison returns `false` -- regardless of the operator or the other operand's value.

```csharp
(int?)null < 5
// output: False

(int?)null > 5
// output: False

(int?)null <= 5
// output: False

(int?)null >= 5
// output: False

(int?)null < (int?)null
// output: False

(int?)null <= (int?)null
// output: False
```

This means that for nullable values, `!(a < b)` is **not** the same as `a >= b`. Both can return `false` when `null` is involved.

## NaN Comparison

IEEE 754 `NaN` (Not a Number) comparisons always return `false`, regardless of the operator or the other operand.

```csharp
double.NaN < 0.0
// output: False

double.NaN > 0.0
// output: False

double.NaN <= 0.0
// output: False

double.NaN >= 0.0
// output: False

double.NaN < double.NaN
// output: False
```

## See Also

- [Equality operators](./equality) -- `==` and `!=` with value, reference, and nullable semantics
- [Arithmetic operators](./arithmetic) -- `+`, `-`, `*`, `/`, `%`, `++`, `--`
- [Numeric types](../types/numeric-types) -- numeric promotion rules
