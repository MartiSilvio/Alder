---
title: "Equality Operators"
description: "Equality and inequality operators with value semantics, reference semantics, NaN, nullable, and tuple equality in CsEval."
sidebar:
  order: 4
---

## Overview

The equality operators `==` and `!=` test whether two values are equal. Their behavior depends on the operand types: numeric types use value equality, strings use value equality (via the built-in `string ==` overload), and reference types use reference equality.

## Value Equality

Numeric types and `bool` are compared by value.

```csharp
1 == 1
// output: True

1 != 2
// output: True

3.14 == 3.14
// output: True

true == true
// output: True

true != false
// output: True
```

## String Equality

Strings use value equality -- two strings are equal if they contain the same character sequence.

```csharp
"abc" == "abc"
// output: True

"abc" != "def"
// output: True

"hello" == "HELLO"
// output: False
```

## Char Equality

Characters are compared by their underlying numeric value.

```csharp
'a' == 'a'
// output: True

'A' != 'a'
// output: True
```

## Nullable Equality

Nullable equality has special handling for `null`. Two `null` values are equal; a `null` compared to any non-null value is not equal.

```csharp
(int?)null == null
// output: True

(int?)null != null
// output: False

(int?)null == 5
// output: False

(int?)1 == 1
// output: True
```

## NaN Equality

Per IEEE 754, `NaN` is not equal to anything -- including itself. This applies to both `double.NaN` and `float.NaN`.

```csharp
double.NaN == double.NaN
// output: False

double.NaN != double.NaN
// output: True

double.NaN == 0.0
// output: False

double.NaN != 0.0
// output: True
```

## Tuple Equality

Tuples support element-wise equality. Two tuples are equal if all corresponding elements are equal.

```csharp
(1, 2) == (1, 2)
// output: True

(1, 2) != (1, 3)
// output: True

("a", 1) == ("a", 1)
// output: True
```

## Enum Equality

Enum values are compared by their underlying integral value.

```csharp
System.DayOfWeek.Monday == System.DayOfWeek.Monday
// output: True

System.DayOfWeek.Monday != System.DayOfWeek.Tuesday
// output: True
```

## See Also

- [Comparison operators](./comparison) -- `<`, `>`, `<=`, `>=` with nullable and NaN semantics
- [Arithmetic operators](./arithmetic) -- `+`, `-`, `*`, `/`, `%`, `++`, `--`
- [Numeric types](../types/numeric-types) -- numeric promotion rules
