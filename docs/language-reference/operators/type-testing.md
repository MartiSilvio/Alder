---
title: "Type Testing and Cast Operators"
description: "Type testing, safe cast, explicit cast, typeof, default, nameof, and sizeof operators in Alder."
sidebar:
  order: 10
---

## Overview

Alder supports type testing and cast operators for checking types at runtime, performing conversions, and querying type metadata.

## Type Test (`is`)

The `is` operator checks whether a value is compatible with a given type, returning `bool`. It works without any sandbox restrictions.

```csharp
42 is int
// output: True

"hello" is string
// output: True

42 is double
// output: False

"hello" is object
// output: True
```

### Null Checks

```csharp
null is string
// output: False

"hello" is not null
// output: True
```

The `is` operator also supports pattern matching with variable binding, relational patterns, and logical patterns. See [Pattern matching](./pattern-matching) for full coverage.

## Safe Cast (`as`)

The `as` operator performs a safe cast, returning `null` on failure instead of throwing. It only works with reference types and nullable value types.

```csharp
(object)"hello" as string
// output: hello

(object)42 as string
// output: null
```

## Explicit Cast

The cast operator `(Type)expr` performs explicit type conversions.

### Numeric Conversions

```csharp
(double)42
// output: 42

(int)3.14
// output: 3

(long)42
// output: 42

(byte)256
// output: 0
```

Numeric casts truncate toward zero for floating-point-to-integer conversions. Integer narrowing wraps in the default unchecked context.

### Checked and Unchecked Casts

Use `checked()` to throw `OverflowException` on narrowing overflow. See the statements reference for full checked/unchecked semantics.

```csharp
unchecked((byte)256)
// output: 0

checked((byte)256)
// output: System.OverflowException: Arithmetic operation resulted in an overflow.
```

### Unboxing

Unboxing requires the cast target to match the exact boxed type.

```csharp
(int)(object)42
// output: 42
```

## `typeof`

The `typeof` operator returns the `System.Type` object for a given type name. It can be used for type comparisons.

```csharp
typeof(int) == typeof(int)
// output: True

typeof(int) != typeof(string)
// output: True
```

:::note
While `typeof` returns a `Type` object, the reflection leak guard blocks `Type` values returned from member access and method calls (e.g., `.GetType()`). The `typeof` operator itself is not subject to this guard because it is resolved at bind time as a literal value.
:::

## `default`

The `default` operator returns the default value for a type: `0` for numeric types, `false` for `bool`, `null` for reference types.

```csharp
default(int)
// output: 0

default(bool)
// output: False

default(double)
// output: 0

default(string)
// output: null
```

## `nameof`

The `nameof` operator returns the string name of a variable, type, or member.

```csharp
{ var myVariable = 42; return nameof(myVariable); }
// output: myVariable
```

## `sizeof`

The `sizeof` operator returns the size in bytes of an unmanaged value type.

```csharp
sizeof(int)
// output: 4

sizeof(double)
// output: 8

sizeof(char)
// output: 2

sizeof(bool)
// output: 1
```

## See Also

- [Pattern matching](./pattern-matching) -- `is` patterns, `switch` expressions, relational and logical patterns
- [Member access](./member-access) -- `.`, `[]`, `()` with sandbox requirements
- [Numeric types](../types/numeric-types) -- conversion rules and type behavior
