---
title: "Pattern Matching"
description: "Pattern matching with is expressions and switch expressions in CsEval."
sidebar:
  order: 14
---

## Overview

CsEval supports the full range of C# pattern matching: `is` expressions for inline pattern tests and `switch` expressions for multi-arm matching. Pattern matching works without sandbox restrictions, except for property patterns which require `AllowPropertyRead`.

## Constant Patterns

A constant pattern tests whether a value equals a specific constant.

```csharp
42 is 42
// output: True

42 is 99
// output: False

"hello" is "hello"
// output: True
```

### Null check

```csharp
(object)null is null
// output: True

"hello" is null
// output: False
```

## Type Patterns

A type pattern tests whether a value is of a given type.

```csharp
(object)42 is int
// output: True

(object)"hello" is int
// output: False
```

### Type pattern with variable binding

When the type matches, the pattern variable is assigned the value cast to the matched type.

```csharp
{ object x = "hello"; return x is string s ? s.Length : -1; }
// output: 5

{ object x = 42; return x is string s ? s.Length : -1; }
// output: -1
```

## Var Pattern

The `var` pattern always matches and binds the value to a new variable. This is useful for capturing an intermediate result.

```csharp
42 is var v
// output: True
```

```csharp
{ return 42 is var v ? v * 2 : 0; }
// output: 84
```

## Discard Pattern

The discard pattern `_` matches any value without binding. It is primarily used as a catch-all in switch expression arms.

```csharp
99 switch { 1 => "one", _ => "other" }
// output: other
```

## Relational Patterns

Relational patterns test a value against a constant using `<`, `>`, `<=`, or `>=`.

```csharp
42 is > 0
// output: True

42 is < 0
// output: False

42 is >= 10 and <= 100
// output: True
```

## Logical Patterns

Logical patterns combine other patterns using `and`, `or`, and `not`.

### not

```csharp
(object)"hello" is not null
// output: True

(object)null is not null
// output: False
```

### and

```csharp
42 is > 0 and < 100
// output: True

150 is > 0 and < 100
// output: False
```

### or

```csharp
(object)42 is int or string
// output: True

(object)"hi" is int or string
// output: True

(object)3.14 is int or string
// output: False
```

### Pattern Precedence

Logical patterns follow this precedence (lowest to highest):

| Precedence | Operator |
|:---:|---|
| 1 (lowest) | `or` |
| 2 | `and` |
| 3 | `not` |
| 4 | relational (`<`, `>`, `<=`, `>=`) |
| 5 (highest) | primary (constant, type, var, property, positional) |

This means `x is A or B and C` is parsed as `x is A or (B and C)`.

## Property Patterns

A property pattern tests properties of the matched value. Property patterns require `AllowPropertyRead` because they access object properties at runtime.

```csharp
"hello world" is { Length: > 5 }
// output: True

"hi" is { Length: > 5 }
// output: False
```

A null value never matches a property pattern:

```csharp
{ object x = null; return x is { Length: > 0 }; }
// output: False
```

:::note
Property patterns require `AllowPropertyRead` in the sandbox settings. The default `Trusted()` and `Safe()` presets enable this.
:::

## Positional (Tuple) Patterns

Positional patterns match against tuples by destructuring their elements.

```csharp
(1, "hello") switch { (1, "hello") => true, _ => false }
// output: True

(1, "hello") switch { (1, _) => true, _ => false }
// output: True
```

## Switch Expressions

A switch expression evaluates a value against multiple pattern arms, returning the result of the first matching arm.

```csharp
1 switch { 1 => "one", 2 => "two", _ => "other" }
// output: one
```

### Type-based switching

```csharp
(object)42 switch { int n => n * 2, string s => s.Length, _ => -1 }
// output: 84
```

### Relational arms

```csharp
75 switch { < 60 => "F", < 70 => "D", < 80 => "C", < 90 => "B", _ => "A" }
// output: C
```

### First-match semantics

Switch expressions use first-match semantics. If multiple arms could match, the first one wins.

### Non-exhaustive match

If no arm matches and there is no discard arm, a `SwitchExpressionException` is thrown at runtime.

## Combining Patterns

Patterns can be freely combined for expressive matching.

```csharp
{ object x = 42; return x is int n and > 0 ? n : -1; }
// output: 42
```

```csharp
{
    var classify = (int score) => score switch {
        < 0 or > 100 => "invalid",
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
    return classify(85);
}
// output: B
```

## Sandbox Interaction

| Pattern | Sandbox Flag |
|---|---|
| Constant, type, var, discard, relational, logical | None required |
| Property pattern (`{ Prop: value }`) | `AllowPropertyRead` |
| Switch expression | None (same as constituent patterns) |

## See Also

- [Type testing](./type-testing) -- `is`, `as`, `typeof`, cast operators
- [Conditional operator](./conditional) -- `?:` for simple two-branch selection
- [Operators overview](./index) -- full precedence table
