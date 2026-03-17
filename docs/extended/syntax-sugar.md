---
title: "Extended Mode: Syntax Sugar"
description: "Unless, until, let-in, comprehensions, array literals, spread, if-expression, string repetition, and slicing."
sidebar:
  order: 3
---

Extended mode adds syntax sugar inspired by Python, Ruby, and Rust. All features on this page require `LanguageMode.Extended` and throw `CsEvalLanguageModeException` in Standard mode.

## unless Statement

`unless` inverts a condition check. It desugars to `if (!cond) { body }`.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions { LanguageMode = LanguageMode.Extended });

engine.Evaluate("var x = 0; unless (false) { x = 1; } x");
// output: 1
```

There is no `else` clause on `unless` -- use a normal `if/else` when you need both branches.

## until Loop

`until` inverts a loop condition. It desugars to `while (!cond) { body }`.

```csharp
engine.Evaluate("var i = 0; until (i >= 5) { i++; } i");
// output: 5
```

## let-in Expression

Scoped variable binding that introduces a variable visible only within the body expression.

### Simple binding

```csharp
engine.Evaluate("let x = 5 in x * x");
// output: 25
```

The variable `x` exists only for the `in` body -- it does not leak into the surrounding scope.

### Destructuring binding

```csharp
engine.SetVariable("person", new { Name = "Alice", Age = 30 });
engine.Evaluate("let {Name, Age} = person in Name");
// output: Alice
```

Destructuring creates a temporary variable, then accesses each named property. Works with anonymous objects and registered types that expose matching properties.

## List Comprehensions

Python-style comprehensions produce arrays.

### Basic comprehension

```csharp
engine.Evaluate("[x * x for x in new[] {1, 2, 3}]");
// output: int[] {1, 4, 9}
```

Desugars to `source.Select(x => x * x).ToArray()`.

### Filtered comprehension

```csharp
engine.Evaluate("[x for x in new[] {1, 2, 3, 4, 5} if x > 3]");
// output: int[] {4, 5}
```

Desugars to `source.Where(x => x > 3).Select(x => x).ToArray()`.

:::note
Comprehensions call `.ToArray()` and return a materialized array, not a lazy `IEnumerable`.
:::

## Array Literals

`[1, 2, 3]` creates an array. When all elements share the same type, the result is a typed array (e.g., `int[]`); mixed-type literals produce `object[]`. Standard mode rejects this syntax with `CsEvalLanguageModeException`.

```csharp
engine.Evaluate("[1, 2, 3]");
// output: int[] {1, 2, 3}

engine.Evaluate("[]");
// output: object[] {}
```

## Spread Operator

### Array spread

`..` inside array literals concatenates collections.

```csharp
engine.Evaluate("var a = new[] {1, 2}; var b = new[] {3, 4}; [..a, ..b]");
// output: int[] {1, 2, 3, 4}
```

### Object spread

`..` inside anonymous object expressions copies properties, with later values overriding earlier ones.

```csharp
engine.SetVariable("existing", new { Name = "Alice", Age = 30 });
engine.Evaluate("new { ..existing, Name = \"Bob\" }");
// output: { Name = Bob, Age = 30 }
```

## If-Expression

A ternary alternative using `if`/`else` keywords. Both branches are required.

```csharp
engine.Evaluate("if (true) 1 else 2");
// output: 1

engine.Evaluate("if (false) \"yes\" else \"no\"");
// output: no
```

Equivalent to `condition ? thenValue : elseValue` but reads more naturally in longer expressions.

## String Repetition

The `*` operator repeats a string when one operand is a string and the other is an integer.

```csharp
engine.Evaluate("\"ab\" * 3");
// output: ababab

engine.Evaluate("3 * \"ab\"");
// output: ababab
```

Works in both `string * int` and `int * string` order.

## Slicing

Python-style slice syntax on arrays, lists, and strings. The slice returns the same type as the input (`T[]` for arrays, `List<T>` for lists, `string` for strings).

### Basic slice

```csharp
engine.Evaluate("new[] {1, 2, 3, 4, 5}[1:3]");
// output: int[] {2, 3}
```

The end index is exclusive: `[start:end]` takes elements from index `start` up to but not including `end`.

### Open-ended slices

```csharp
engine.Evaluate("new[] {1, 2, 3, 4, 5}[:3]");
// output: int[] {1, 2, 3}

engine.Evaluate("new[] {1, 2, 3, 4, 5}[2:]");
// output: int[] {3, 4, 5}
```

### Stepped slice

```csharp
engine.Evaluate("new[] {1, 2, 3, 4, 5}[::2]");
// output: int[] {1, 3, 5}
```

`[::step]` takes all elements with the given stride. A step of zero throws an error.

### Out-of-bounds clamping

Slice indices that exceed the collection bounds are clamped to the valid range (Python behavior), rather than throwing an exception.
