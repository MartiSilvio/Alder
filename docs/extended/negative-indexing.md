---
title: "Extended Mode: Negative Indexing"
description: "Python-style negative indexing for arrays, lists, and strings."
sidebar:
  order: 5
---

In Extended mode, negative indices count from the end of a collection. `arr[-1]` returns the last element, `arr[-2]` the second-to-last, and so on.

## Normalization Formula

When the index is negative and the language mode is Extended:

```
normalizedIndex = length + index
```

For example, with a 5-element array and index `-1`: `normalizedIndex = 5 + (-1) = 4`, which is the last element.

This normalization is applied in `MemberAccess.NormalizeIndex`. In Standard mode, negative indices are passed directly to the underlying indexer, which throws an exception.

## Supported Collections

Negative indexing works on any type with an integer indexer:

- **Arrays** (`T[]`)
- **Lists** (`List<T>`)
- **Strings** (returns a `char`)
- Any type with an `int`-parameterized indexer

## Examples

```csharp
var engine = new AlderEngine(new AlderOptions { LanguageMode = LanguageMode.Extended });

engine.Evaluate("new[] {10, 20, 30}[-1]");
// output: 30

engine.Evaluate("new[] {10, 20, 30}[-2]");
// output: 20

engine.Evaluate("new[] {10, 20, 30}[-3]");
// output: 10

engine.Evaluate("\"hello\"[-1]");
// output: o
```

## Edge Cases

### First element via negative index

`arr[-length]` normalizes to index `0`, returning the first element:

```csharp
engine.Evaluate("new[] {10, 20, 30}[-3]");
// output: 10    (normalizedIndex = 3 + (-3) = 0)
```

### Out of bounds after normalization

If the normalized index is still negative (or >= length), an `ArgumentOutOfRangeException` is thrown:

```csharp
engine.Evaluate("new[] {10, 20, 30}[-4]");
// throws ArgumentOutOfRangeException (normalizedIndex = 3 + (-4) = -1)
```

### Standard mode behavior

In Standard mode, negative indices are not normalized. They fail the bounds check in `NormalizeIndex` and throw `ArgumentOutOfRangeException`:

```csharp
var standard = new AlderEngine(); // Standard mode (default)
standard.Evaluate("new[] {10, 20, 30}[-1]");
// throws ArgumentOutOfRangeException
```
