---
title: "Built-in Types"
description: "Complete list of C# built-in types supported by CsEval with their CLR mappings and support status."
sidebar:
  order: 1
---

## Overview

CsEval supports C# built-in type keywords as aliases for .NET CLR types. A built-in type keyword (such as `int` or `string`) resolves to the corresponding CLR type (such as `System.Int32` or `System.String`) at evaluation time. Both the keyword and the fully qualified CLR name can be used interchangeably in expressions.

## Integral Types

| Keyword | CLR Type | Size | Range |
|---------|----------|------|-------|
| `sbyte` | `System.SByte` | 8-bit signed | -128 to 127 |
| `byte` | `System.Byte` | 8-bit unsigned | 0 to 255 |
| `short` | `System.Int16` | 16-bit signed | -32,768 to 32,767 |
| `ushort` | `System.UInt16` | 16-bit unsigned | 0 to 65,535 |
| `int` | `System.Int32` | 32-bit signed | -2,147,483,648 to 2,147,483,647 |
| `uint` | `System.UInt32` | 32-bit unsigned | 0 to 4,294,967,295 |
| `long` | `System.Int64` | 64-bit signed | -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| `ulong` | `System.UInt64` | 64-bit unsigned | 0 to 18,446,744,073,709,551,615 |

```csharp
int.MaxValue
// output: 2147483647

long.MinValue
// output: -9223372036854775808
```

> **Note:** `nint` and `nuint` (platform-sized integers) are recognized as keywords but are not supported as types. Using them in a type position will produce an error.

## Floating-Point Types

| Keyword | CLR Type | Precision | Approximate Range |
|---------|----------|-----------|-------------------|
| `float` | `System.Single` | ~6-9 digits | +/-1.5e-45 to +/-3.4e38 |
| `double` | `System.Double` | ~15-17 digits | +/-5.0e-324 to +/-1.7e308 |
| `decimal` | `System.Decimal` | 28-29 digits | +/-1.0e-28 to +/-7.9e28 |

```csharp
3.14.GetType().Name
// output: Double

3.14m.GetType().Name
// output: Decimal
```

## Other Types

| Keyword | CLR Type | Description |
|---------|----------|-------------|
| `bool` | `System.Boolean` | `true` or `false` |
| `char` | `System.Char` | A single Unicode character (UTF-16 code unit) |
| `string` | `System.String` | An immutable sequence of Unicode characters |
| `object` | `System.Object` | Base type of all types |
| `void` | `System.Void` | Return type only; cannot be used as a variable type |

> **Note:** `dynamic` is recognized as a keyword but is not supported as a type.

```csharp
true.GetType().Name
// output: Boolean

'A'.GetType().Name
// output: Char
```

## Nullable Type Keywords

For value types, appending `?` to the keyword produces a `Nullable<T>` type. For reference types, the `?` suffix is accepted for syntactic convenience but has no runtime effect.

### Value Types

`int?` maps to `Nullable<int>`, `bool?` maps to `Nullable<bool>`, and so on. All 13 value type keywords have pre-registered nullable variants:

`sbyte?`, `byte?`, `short?`, `ushort?`, `int?`, `uint?`, `long?`, `ulong?`, `float?`, `double?`, `decimal?`, `bool?`, `char?`

```csharp
default(int?)
// output: null

((int?)42).GetType().Name
// output: Int32
```

### Reference Types

`string?` and `object?` are accepted but resolve to the same CLR type as `string` and `object`. CsEval does not enforce nullable reference type (NRT) annotations at runtime.

```csharp
typeof(string?) == typeof(string)
// output: True
```

## See Also

- [Numeric Types](./numeric-types) -- literal formats, suffixes, and promotion rules
- [String and Char](./string-and-char) -- string literal types and escape sequences
- [Nullable and Conversions](./nullable-and-conversions) -- nullable semantics and conversion rules
