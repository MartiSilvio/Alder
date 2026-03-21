---
title: "Nullable Types and Conversions"
description: "Nullable value types, implicit and explicit conversions, lifted operators, and user-defined conversions in Alder."
sidebar:
  order: 7
---

Alder implements the complete C# nullable value type system and type conversion rules per ECMA-334. This page covers nullable semantics, the implicit and explicit conversion tables, and user-defined conversion support.

## Nullable Value Types

Append `?` to any value type keyword to create its nullable variant. A `Nullable<T>` can hold a value of type `T` or `null`.

```csharp
int? x = 42;
x.HasValue
// output: true
```

```csharp
int? x = 42;
x.Value
// output: 42
```

```csharp
int? x = null;
x.HasValue
// output: false
```

All 13 value type keywords support nullable variants: `sbyte?`, `byte?`, `short?`, `ushort?`, `int?`, `uint?`, `long?`, `ulong?`, `float?`, `double?`, `decimal?`, `bool?`, `char?`.

### Nullable Reference Types

`string?` and `object?` are accepted syntactically but map to `string` and `object` at runtime. Alder does not enforce nullable reference type annotations (no NRT analysis).

## Lifted Operators

When a nullable type participates in an arithmetic or comparison operation, the operator is "lifted" to handle null operands.

### Lifted Arithmetic

If either operand is null, the result is null.

```csharp
(int?)3 + (int?)4
// output: 7
```

```csharp
(int?)3 + (int?)null
// output: null
```

```csharp
(int?)null * (int?)5
// output: null
```

This applies to `+`, `-`, `*`, `/`, `%`, and the bitwise operators `&`, `|`, `^`, `~`, `<<`, `>>`.

### Lifted Comparison

If either operand is null, relational comparisons (`<`, `>`, `<=`, `>=`) return `false`.

```csharp
(int?)3 > (int?)2
// output: true
```

```csharp
(int?)3 > (int?)null
// output: false
```

```csharp
(int?)null < (int?)null
// output: false
```

### Lifted Equality

For `==` and `!=`, two null values are considered equal.

```csharp
(int?)null == (int?)null
// output: true
```

```csharp
(int?)3 == (int?)null
// output: false
```

```csharp
(int?)3 != (int?)null
// output: true
```

## Three-Value Boolean Logic

`bool?` with `&&` and `||` follows SQL-style three-value logic, per ECMA-334 sections 12.13.5 and 12.14.2.

### `&&` Truth Table

| Left    | Right   | Result  |
| ------- | ------- | ------- |
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
(bool?)true && (bool?)null
// output: null
```

```csharp
(bool?)false && (bool?)null
// output: false
```

### `||` Truth Table

| Left    | Right   | Result  |
| ------- | ------- | ------- |
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
// output: true
```

```csharp
(bool?)false || (bool?)null
// output: null
```

### Null-Coalescing Operator

The `??` operator returns the left operand if it has a value, otherwise the right operand.

```csharp
(int?)42 ?? 0
// output: 42
```

```csharp
(int?)null ?? 0
// output: 0
```

## Default Values

The `default` expression returns the zero/null value for a type.

| Type      | `default` Value |
| --------- | --------------- |
| `int`     | `0`             |
| `long`    | `0`             |
| `double`  | `0.0`           |
| `decimal` | `0`             |
| `bool`    | `false`         |
| `char`    | `'\0'`          |
| `int?`    | `null`          |
| `string`  | `null`          |
| `object`  | `null`          |

```csharp
default(int)
// output: 0
```

```csharp
default(bool)
// output: false
```

```csharp
default(int?)
// output: null
```

## Implicit Numeric Conversions

Alder implements the implicit numeric conversion table from ECMA-334 section 10.2.3. A conversion listed below happens automatically without a cast.

| From     | Converts To                                                                     |
| -------- | ------------------------------------------------------------------------------- |
| `sbyte`  | `short`, `int`, `long`, `float`, `double`, `decimal`                            |
| `byte`   | `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal` |
| `short`  | `int`, `long`, `float`, `double`, `decimal`                                     |
| `ushort` | `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`                    |
| `int`    | `long`, `float`, `double`, `decimal`                                            |
| `uint`   | `long`, `ulong`, `float`, `double`, `decimal`                                   |
| `long`   | `float`, `double`, `decimal`                                                    |
| `ulong`  | `float`, `double`, `decimal`                                                    |
| `float`  | `double`                                                                        |
| `char`   | `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`          |

There are no predefined implicit conversions **to** `char`. Values of other integral types do not automatically convert to `char` (ECMA-334 section 10.2.3).

```csharp
int x = 42;
long y = x;
y
// output: 42
```

```csharp
char c = 'A';
int code = c;
code
// output: 65
```

:::note
Implicit conversions from `int`/`long` to `float` and from `long` to `double` may lose precision but never lose magnitude. These are widening conversions per the spec.
:::

## Implicit Nullable Conversions

Nullable types extend the implicit conversion rules.

- **`T` to `T?`** is always implicit (identity lift).
- **`S` to `T?`** is implicit when `S` to `T` is an implicit numeric conversion.
- **`S?` to `T?`** is implicit when `S` to `T` is an implicit numeric conversion.

Per ECMA-334 section 10.6.1.

```csharp
int x = 42;
int? y = x;
y.Value
// output: 42
```

```csharp
int x = 42;
long? y = x;
y.Value
// output: 42
```

## Implicit Tuple Conversions

Tuples convert implicitly when they have the same arity and each element converts implicitly. Per ECMA-334 section 10.2.13.

```csharp
(int, int) a = (1, 2);
(long, double) b = a;
b
// output: (1, 2)
```

## Explicit Casts

Use `(Type)expression` to perform an explicit cast. Explicit casts cover narrowing numeric conversions, unboxing, and enum conversions.

### Numeric Narrowing

```csharp
(byte)256
// output: 0
```

In an unchecked context (the default), numeric overflow wraps silently. In a checked context, it throws `OverflowException`.

```csharp
checked((byte)256)
// output: OverflowException
```

### Unboxing

When a value type is boxed as `object`, unboxing requires an exact type match. This is a common source of errors.

```csharp
(int)(object)42
// output: 42
```

```csharp
// This fails: the boxed value is int, not long
(long)(object)42
// output: InvalidCastException
```

The correct way to convert a boxed `int` to `long` is to unbox to `int` first, then widen:

```csharp
(long)(int)(object)42
// output: 42
```

### Enum Conversions

Enums can be cast to and from their underlying numeric type.

```csharp
(int)System.DayOfWeek.Wednesday
// output: 3
```

```csharp
(System.DayOfWeek)3
// output: Wednesday
```

### Reference Type Casts

Upcasting (derived to base) is implicit. Downcasting (base to derived) requires an explicit cast and throws `InvalidCastException` if the runtime type does not match.

### Casting Null to Value Types

Casting `null` to a non-nullable value type throws a `AlderException` with diagnostic code CS0037 ("Cannot convert null to 'Int32' because it is a non-nullable value type").

```csharp
(int)(object)null
// output: AlderException (CS0037)
```

Casting `null` to a nullable value type succeeds.

```csharp
(int?)(object)null
// output: null
```

## User-Defined Conversions

Alder resolves `op_Implicit` and `op_Explicit` operators on registered types per ECMA-334 sections 10.5.3 through 10.5.5. When a type registered through the Engine API defines implicit or explicit conversion operators, Alder applies them during overload resolution and cast evaluation.

## See Also

- [Built-in Types](./built-in-types)
- [Numeric Types](./numeric-types)
