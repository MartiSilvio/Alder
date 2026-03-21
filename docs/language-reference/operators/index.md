---
title: "Operators"
description: "Complete operator precedence table and overview of all operators supported in Alder Standard mode."
sidebar:
  order: 1
---

## Overview

Alder implements a precedence-climbing expression parser with 18 precedence levels. This page lists every operator available in Standard mode, ordered from lowest to highest precedence.

Operator overloading is not supported in Alder -- user-defined operators cannot be defined. However, built-in operator overloads for types like `string +` and `Delegate +/-` work as expected.

## Precedence Table

Higher precedence means tighter binding. When two operators compete for the same operand, the higher-precedence operator wins.

| Precedence | Category                                   | Operators                                                                        | Associativity |
| :--------: | ------------------------------------------ | -------------------------------------------------------------------------------- | ------------- |
|     1      | [Assignment](./assignment)                 | `=` `+=` `-=` `*=` `/=` `%=` `&=` `\|=` `^=` `<<=` `>>=` `>>>=` `??=`            | Right         |
|     2      | [Conditional](./conditional)               | `? :`                                                                            | Right         |
|     3      | [Null-coalescing](./null-operators)        | `??`                                                                             | Right         |
|     4      | [Range](./range-and-index)                 | `..`                                                                             | N/A           |
|     5      | [Logical OR](./boolean-logical)            | `\|\|`                                                                           | Left          |
|     6      | [Logical AND](./boolean-logical)           | `&&`                                                                             | Left          |
|     7      | [Bitwise OR](./bitwise-and-shift)          | `\|`                                                                             | Left          |
|     8      | [Bitwise XOR](./bitwise-and-shift)         | `^`                                                                              | Left          |
|     9      | [Bitwise AND](./bitwise-and-shift)         | `&`                                                                              | Left          |
|     10     | [Equality](./equality)                     | `==` `!=`                                                                        | Left          |
|     11     | [Relational / Type testing](./comparison)  | `<` `>` `<=` `>=` `is` `as` `switch`                                             | Left          |
|     12     | [Shift](./bitwise-and-shift)               | `<<` `>>` `>>>`                                                                  | Left          |
|     13     | [Additive](./arithmetic)                   | `+` `-`                                                                          | Left          |
|     14     | [Multiplicative](./arithmetic)             | `*` `/` `%`                                                                      | Left          |
|     15     | Unary                                      | `-` `+` `!` `~` `^` (index-from-end) `(cast)` `++x` `--x`                        | Right         |
|     16     | Postfix / [Member access](./member-access) | `.` `?.` `[]` `?[]` `()` `x++` `x--`                                             | Left          |
|     17     | Primary                                    | literals, identifiers, `typeof` `default` `nameof` `sizeof` lambdas tuples `new` | N/A           |

## Category Descriptions

### Assignment (precedence 1)

Simple and compound assignment. Requires `AllowAssignment` sandbox flag for variable reassignment; variable declarations (`var x = 5`) are always allowed. See [Assignment operators](./assignment).

### Conditional (precedence 2)

The ternary conditional operator `condition ? consequent : alternative`. See [Conditional operator](./conditional).

### Null-coalescing (precedence 3)

Returns the left operand if non-null, otherwise the right. `??=` assigns only when the left side is null. See [Null operators](./null-operators).

### Range (precedence 4)

Creates `System.Range` values for slicing. See [Range and index](./range-and-index).

### Logical OR / AND (precedence 5-6)

Short-circuit boolean operators. See [Boolean logical operators](./boolean-logical).

### Bitwise OR / XOR / AND (precedence 7-9)

Bitwise operations on integers and non-short-circuit boolean logic. See [Bitwise and shift operators](./bitwise-and-shift).

### Equality (precedence 10)

Value and reference equality testing. See [Equality operators](./equality).

### Relational / Type testing (precedence 11)

Numeric comparisons and type testing with `is`, `as`, and `switch` expressions. See [Comparison operators](./comparison), [Type testing](./type-testing), and [Pattern matching](./pattern-matching).

### Shift (precedence 12)

Bit shift operations including unsigned right shift (`>>>`). See [Bitwise and shift operators](./bitwise-and-shift).

### Additive (precedence 13)

Addition, subtraction, and string concatenation. See [Arithmetic operators](./arithmetic).

### Multiplicative (precedence 14)

Multiplication, division, and remainder. See [Arithmetic operators](./arithmetic).

### Unary (precedence 15)

Prefix operators: numeric negation, logical NOT, bitwise complement, index-from-end (`^`), cast expressions, and prefix increment/decrement.

### Postfix / Member access (precedence 16)

Member access (`.`), null-conditional access (`?.`), indexing (`[]`, `?[]`), invocation (`()`), and postfix increment/decrement. See [Member access](./member-access).

### Primary (precedence 17)

Literals, identifiers, parenthesized expressions, `typeof`, `default`, `nameof`, `sizeof`, lambda expressions, tuple expressions, and `new` expressions.

## See Also

- [Arithmetic operators](./arithmetic) -- `+`, `-`, `*`, `/`, `%`, `++`, `--`
- [Comparison operators](./comparison) -- `<`, `>`, `<=`, `>=`
- [Equality operators](./equality) -- `==`, `!=`
- [Numeric types](../types/numeric-types) -- numeric promotion rules and type behavior
