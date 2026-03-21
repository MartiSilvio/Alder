---
title: "Extended Mode: Operators"
description: "Power, range, pipeline, spaceship, regex, strict equality, containment, pattern matching, chained comparisons, and word operators."
sidebar:
  order: 2
---

All operators on this page require `LanguageMode.Extended` and throw `AlderLanguageModeException` in Standard mode.

```csharp
var engine = new AlderEngine(new AlderOptions { LanguageMode = LanguageMode.Extended });
```

## Power Operator

`**` computes exponentiation via `Math.Pow`. Always returns `double`.

```csharp
engine.Evaluate("2 ** 10");    // output: 1024.0
engine.Evaluate("9 ** 0.5");   // output: 3.0
```

**Right-associative.** `2 ** 3 ** 2` evaluates as `2 ** (3 ** 2)` = `2 ** 9` = 512:

```csharp
engine.Evaluate("2 ** 3 ** 2"); // output: 512.0
```

**Unary minus binds looser** (Python convention). `-2 ** 2` is `-(2 ** 2)`, not `(-2) ** 2`:

```csharp
engine.Evaluate("-2 ** 2"); // output: -4.0
```

### Compound Assignment

`**=` applies power and assigns back:

```csharp
engine.Evaluate("{ var x = 3.0; x **= 2; return x; }"); // output: 9.0
```

## Range Operators

Alder has three range forms. The standard `..` operator is available in both modes; `..=` and `..<` are Extended-only.

| Operator | End behavior               | Example | Covers        |
| -------- | -------------------------- | ------- | ------------- |
| `..`     | Exclusive (C# spec)        | `1..5`  | 1, 2, 3, 4    |
| `..=`    | **Inclusive** (Rust-style) | `1..=5` | 1, 2, 3, 4, 5 |
| `..<`    | Explicit exclusive         | `1..<5` | 1, 2, 3, 4    |

`..` is **always** exclusive-end in both Standard and Extended mode, per the C# specification. `..=` is the inclusive form. `..<` is semantically identical to `..` but makes the exclusive intent explicit.

### Range Iteration

All three range forms can be iterated in Extended mode:

```csharp
// Inclusive range: 1 through 5
engine.Evaluate("{ var sum = 0; foreach (var i in 1..=5) sum += i; return sum; }");
// output: 15

// Explicit exclusive range: 1 through 4
engine.Evaluate("{ var sum = 0; foreach (var i in 1..<5) sum += i; return sum; }");
// output: 10

// Standard range (exclusive end) also iterable in Extended mode
engine.Evaluate("{ var sum = 0; foreach (var i in 1..5) sum += i; return sum; }");
// output: 10
```

When `start >= end` (after accounting for exclusivity), the range yields an empty sequence.

## Pipeline Operator

`|>` pipes the left value as the argument to the right-side function:

```csharp
engine.Evaluate("3.14 |> sin"); // output: 0.00159265291648683 (approximately)
engine.Evaluate("16 |> sqrt");  // output: 4.0
engine.Evaluate("-5 |> abs");   // output: 5
```

Pipeline chains left-to-right:

```csharp
engine.Evaluate("-3.14 |> abs |> sin");
// equivalent to: sin(abs(-3.14))
```

Pipeline also works with registered functions:

```csharp
engine.RegisterFunction("double_it", args => Convert.ToDouble(args[0]) * 2);
engine.Evaluate("5 |> double_it"); // output: 10.0
```

## Spaceship Operator

`<=>` performs three-way comparison, returning `-1`, `0`, or `1`:

```csharp
engine.Evaluate("5 <=> 3");  // output: 1
engine.Evaluate("3 <=> 5");  // output: -1
engine.Evaluate("3 <=> 3");  // output: 0
```

Null-safe: `null` is ordered before any non-null value:

```csharp
engine.Evaluate("null <=> 5");    // output: -1
engine.Evaluate("5 <=> null");    // output: 1
engine.Evaluate("null <=> null"); // output: 0
```

Uses `NumericDispatch` for mixed numeric types:

```csharp
engine.Evaluate("3 <=> 3.0"); // output: 0
```

## Chained Comparisons

Python/Julia-style comparison chaining. `a < b < c` desugars to `a < b && b < c`, with the middle operand evaluated only once:

```csharp
engine.Evaluate("{ var x = 5; return 0 < x < 10; }"); // output: True
engine.Evaluate("1 < 2 < 3");  // output: True
engine.Evaluate("1 < 2 > 3");  // output: False
```

Works with `<`, `<=`, `>`, `>=`, `==`, `!=`:

```csharp
engine.Evaluate("1 <= 2 <= 3"); // output: True
engine.Evaluate("1 == 1 == 1"); // output: True
```

## Regex Operators

`=~` tests if the left operand matches a regex pattern. `!~` is the negation.

```csharp
engine.Evaluate("\"hello\" =~ \"^h\"");    // output: True
engine.Evaluate("\"hello\" =~ \"^x\"");    // output: False
engine.Evaluate("\"hello\" !~ \"^x\"");    // output: True
engine.Evaluate("\"hello\" !~ \"^h\"");    // output: False
```

The left operand is converted to string via `.ToString()`. The right operand must be a string pattern:

```csharp
engine.Evaluate("42 =~ \"^4\""); // output: True
```

## Strict Equality

`===` requires both the same type **and** the same value. `!==` is the negation.

```csharp
engine.Evaluate("1 === 1");     // output: True
engine.Evaluate("1 === 1.0");   // output: False  (int vs double)
engine.Evaluate("\"a\" === \"a\""); // output: True
```

`null === null` is true. `NaN` is never strictly equal to anything, including itself:

```csharp
engine.Evaluate("null === null"); // output: True
```

Contrast with `==`, which performs numeric type promotion (`1 == 1.0` is `true`).

## Containment Operators

### `in` / `not in`

`in` checks whether a value exists in a collection or string:

```csharp
engine.Evaluate("2 in new[] { 1, 2, 3 }");  // output: True
engine.Evaluate("5 in new[] { 1, 2, 3 }");  // output: False
```

For strings, `in` checks substring containment:

```csharp
engine.Evaluate("\"lo\" in \"hello\""); // output: True
engine.Evaluate("\"xyz\" in \"hello\""); // output: False
```

`not in` is the negation:

```csharp
engine.Evaluate("5 not in new[] { 1, 2, 3 }"); // output: True
```

### `like` / `not like`

SQL-style pattern matching with `%` (any characters) and `_` (single character):

```csharp
engine.Evaluate("\"hello\" like \"h%\"");    // output: True
engine.Evaluate("\"hello\" like \"h_llo\"");  // output: True
engine.Evaluate("\"hello\" like \"x%\"");    // output: False
```

`not like` is the negation:

```csharp
engine.Evaluate("\"hello\" not like \"x%\""); // output: True
```

`like` respects `AlderOptions.StringComparison`. By default (`Ordinal`), matching is case-sensitive.

### `between ... and`

Checks whether a value falls within an inclusive range. Desugars to `expr >= low && expr <= high`:

```csharp
engine.Evaluate("5 between 1 and 10");   // output: True
engine.Evaluate("15 between 1 and 10");  // output: False
engine.Evaluate("1 between 1 and 10");   // output: True  (inclusive)
engine.Evaluate("10 between 1 and 10");  // output: True  (inclusive)
```

## Word Operators

`and`, `or`, and `not` are word aliases for `&&`, `||`, and `!`. In Extended mode they work as general logical operators:

```csharp
engine.Evaluate("true and false");  // output: False
engine.Evaluate("true or false");   // output: True
engine.Evaluate("not true");        // output: False
engine.Evaluate("not false");       // output: True
```

They short-circuit just like their symbolic equivalents:

```csharp
engine.Evaluate("false and (1 / 0 > 0)"); // output: False (right side not evaluated)
engine.Evaluate("true or (1 / 0 > 0)");   // output: True  (right side not evaluated)
```

In Standard mode, `and`, `or`, and `not` are only valid in pattern-matching contexts (e.g., `x is > 0 and < 100`). Using them as logical operators in Standard mode throws `AlderLanguageModeException`.

## See Also

- [Extended Mode overview](./index) -- how to enable, philosophy, feature registry
- [Syntax Sugar](./syntax-sugar) -- unless, until, let-in, comprehensions, array literals, spread, slicing
- [Standard mode operators](/language-reference/operators/) -- precedence table for Standard mode
