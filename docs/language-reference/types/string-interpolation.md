---
title: "String Interpolation"
description: "Interpolated string syntax, alignment and format specifiers, and verbatim interpolation in CsEval."
sidebar:
  order: 4
---

## Overview

CsEval supports C# interpolated strings, which embed expressions directly in string literals. Interpolation holes are evaluated at runtime and their results are converted to strings using `ToString()` or format specifiers.

## Basic Syntax

An interpolated string is prefixed with `$`. Expressions are enclosed in `{` and `}`.

```csharp
$"2 + 2 = {2 + 2}"
// output: 2 + 2 = 4

$"Hello, {"world".ToUpper()}"
// output: Hello, WORLD
```

Any valid CsEval expression can appear inside an interpolation hole, including method calls, property access, and arithmetic.

## Alignment Specifiers

The syntax `{expression,width}` pads the result to a minimum width. A positive width right-aligns; a negative width left-aligns.

```csharp
$"|{42,10}|"
// output: |        42|

$"|{42,-10}|"
// output: |42        |

$"|{"hi",8}|"
// output: |      hi|
```

If the formatted value is longer than the specified width, no truncation occurs -- the full value is output.

## Format Specifiers

The syntax `{expression:format}` applies a .NET format string to the value.

```csharp
$"{1234.5:N2}"
// output: 1,234.50

$"{42:D6}"
// output: 000042

$"{0.75:P0}"
// output: 75%

$"{255:X4}"
// output: 00FF
```

Common format specifiers:

| Specifier | Description | Example |
|-----------|-------------|---------|
| `N` / `Nn` | Number with digit grouping | `{1234.5:N2}` -> `1,234.50` |
| `D` / `Dn` | Decimal (integers), zero-padded | `{42:D6}` -> `000042` |
| `C` / `Cn` | Currency (culture-dependent) | `{9.99:C}` -> `$9.99` |
| `P` / `Pn` | Percentage | `{0.75:P0}` -> `75%` |
| `X` / `Xn` | Hexadecimal | `{255:X4}` -> `00FF` |
| `F` / `Fn` | Fixed-point | `{3.14159:F2}` -> `3.14` |
| `E` / `En` | Scientific notation | `{1234:E2}` -> `1.23E+003` |

## Combined Alignment and Format

Alignment and format specifiers can be used together with the syntax `{expression,width:format}`.

```csharp
$"|{42,10:D6}|"
// output: |    000042|

$"|{3.14,-12:F4}|"
// output: |3.1400      |
```

The alignment is applied after formatting: the format specifier produces the string representation, then the alignment pads it to the specified width.

## Nested Expressions

Interpolation holes can contain any expression, including ternary operators, method calls, and chained operations.

```csharp
$"Status: {(true ? "active" : "inactive")}"
// output: Status: active

$"Length: {"hello world".Split(' ').Length}"
// output: Length: 2
```

Parentheses are required around conditional expressions to distinguish the `:` in the ternary operator from a format specifier.

## Brace Escaping

To include a literal `{` or `}` in an interpolated string, double it.

```csharp
$"{{escaped braces}}"
// output: {escaped braces}

$"Value: {42}, Literal: {{"
// output: Value: 42, Literal: {
```

## Verbatim Interpolated Strings

Combining `$` and `@` prefixes (in either order) produces a verbatim interpolated string. This combines interpolation with verbatim string rules: backslashes are literal, `""` escapes a double quote, and the string can span multiple lines.

Both prefix orders are accepted:

```csharp
$@"Path: C:\Users\{System.Environment.MachineName}"
// output: Path: C:\Users\(machine name)

@$"Path: C:\Users\{System.Environment.MachineName}"
// output: Path: C:\Users\(machine name)
```

In verbatim interpolated strings:
- Backslashes are literal (no `\n`, `\t`, etc.)
- `""` produces a literal double quote
- `{{` and `}}` produce literal braces
- `{expression}` is evaluated as an interpolation hole

```csharp
$@"She said ""{""hello""}"" loudly"
// output: She said "hello" loudly
```

## See Also

- [String and Char](./string-and-char) -- string literal types and escape sequences
- [Built-in Types](./built-in-types) -- complete type keyword list
