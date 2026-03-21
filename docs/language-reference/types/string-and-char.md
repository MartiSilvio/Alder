---
title: "String and Char"
description: "String literals, verbatim strings, raw strings, char literals, and escape sequences in Alder."
sidebar:
  order: 3
---

## Overview

Alder supports all standard C# string and character literal types: regular strings, verbatim strings, raw string literals (C# 11), char literals, and their interpolated variants. Escape sequence processing follows the ECMA-334 specification exactly.

## Regular String Literals

A regular string literal is enclosed in double quotes. Backslash escape sequences are processed within the string.

```csharp
"hello, world"
// output: hello, world

"line 1\nline 2"
// output: line 1
// line 2

"tab\there"
// output: tab	here
```

Regular string literals cannot span multiple lines. A newline character in the source between the opening and closing quotes produces an error.

## Verbatim String Literals

A verbatim string literal is prefixed with `@`. Backslashes are treated as literal characters. The only escape is `""` for an embedded double quote. Verbatim strings can span multiple lines.

```csharp
@"C:\Users\file.txt"
// output: C:\Users\file.txt

@"She said ""hello"""
// output: She said "hello"
```

Verbatim strings are useful for file paths, regex patterns, and any content where backslashes should be preserved.

## Raw String Literals

Raw string literals (C# 11) are delimited by at least three double quotes (`"""`). No escape sequences are processed. The number of quotes in the delimiter can be increased to allow embedded sequences of double quotes.

```csharp
"""raw string"""
// output: raw string
```

### Multi-Line Raw Strings

When a raw string literal spans multiple lines, the content starts on the line after the opening quotes and ends on the line before the closing quotes. Alder preserves whitespace exactly as written; it does not strip leading whitespace based on closing-delimiter indentation.

```csharp
"""
    hello
    world
    """
// output: hello
// world
```

### Variable Quote Count

If the content itself contains `"""`, use more quotes in the delimiter:

```csharp
""""
She said """hello"""
""""
// output: She said """hello"""
```

## Char Literals

A char literal is a single character enclosed in single quotes. All escape sequences are supported in char literals.

```csharp
'A'
// output: A

'\n' == '\x0A'
// output: True

(int)'A'
// output: 65
```

A char literal must contain exactly one character (or one escape sequence that resolves to a single character). The `\U` escape with a code point above U+FFFF (which produces a surrogate pair) is not valid in char literals because it would require two UTF-16 code units.

## Escape Sequences

Escape sequences are processed in regular string literals and char literals. They are **not** processed in verbatim strings (except `""` for embedded quotes) or raw string literals.

| Sequence          | Character               | Unicode Value            |
| ----------------- | ----------------------- | ------------------------ |
| `\n`              | Newline (LF)            | U+000A                   |
| `\r`              | Carriage return (CR)    | U+000D                   |
| `\t`              | Horizontal tab          | U+0009                   |
| `\0`              | Null                    | U+0000                   |
| `\a`              | Alert (bell)            | U+0007                   |
| `\b`              | Backspace               | U+0008                   |
| `\f`              | Form feed               | U+000C                   |
| `\v`              | Vertical tab            | U+000B                   |
| `\\`              | Backslash               | U+005C                   |
| `\"`              | Double quote            | U+0022                   |
| `\'`              | Single quote            | U+0027                   |
| `\uHHHH`          | Unicode (4 hex digits)  | U+0000 to U+FFFF         |
| `\UHHHHHHHH`      | Unicode (8 hex digits)  | U+00000000 to U+0010FFFF |
| `\xH` to `\xHHHH` | Hex escape (1-4 digits) | U+0000 to U+FFFF         |

### Unicode Escapes

The `\u` escape requires exactly 4 hexadecimal digits:

```csharp
"\u0041"
// output: A

"\u03B1"
// output: α
```

The `\U` escape requires exactly 8 hexadecimal digits and supports code points up to U+10FFFF. Code points above U+FFFF are encoded as surrogate pairs in strings, but are not valid in char literals.

```csharp
"\U0001F600"
// output: (grinning face emoji)
```

### Hex Escapes

The `\x` escape consumes 1 to 4 hexadecimal digits greedily:

```csharp
"\x41"
// output: A

"\x0041"
// output: A
```

### Escape Sequence Applicability

| Literal Type                      | Escape Processing                                      |
| --------------------------------- | ------------------------------------------------------ |
| Regular string (`"..."`)          | All backslash escapes                                  |
| Char literal (`'...'`)            | All backslash escapes                                  |
| Verbatim string (`@"..."`)        | `""` for embedded quote only; backslashes are literal  |
| Raw string (`"""..."""`)          | None; all characters are literal                       |
| Interpolated string (`$"..."`)    | All backslash escapes + `{{`/`}}` for literal braces   |
| Verbatim interpolated (`$@"..."`) | `""` for embedded quote + `{{`/`}}` for literal braces |

## See Also

- [String Interpolation](./string-interpolation) -- interpolated string syntax, alignment, and format specifiers
- [Built-in Types](./built-in-types) -- complete type keyword list
