---
title: "Range and Index Operators"
description: "Range (..) and index-from-end (^) operators in Alder."
sidebar:
  order: 11
---

## Overview

Alder supports the C# range and index-from-end operators for creating `System.Range` and `System.Index` values. These operators work without sandbox restrictions to create the values themselves, though using them to index into collections requires the appropriate member access sandbox flags.

## Index From End (`^`)

The `^` operator creates a `System.Index` that counts from the end of a sequence. `^1` refers to the last element, `^2` to the second-to-last, and so on.

```csharp
"hello"[^1]
// output: o

"hello"[^2]
// output: l
```

`^0` refers to the position past the last element (the length), which is useful as the end of a range but would throw `IndexOutOfRangeException` if used directly as an index.

## Range Operator (`..`)

The `..` operator creates a `System.Range` value representing a slice of a sequence. The start is inclusive and the end is exclusive. Both bounds must be specified when used in a subscript expression.

```csharp
"hello"[1..4]
// output: ell

"hello"[0..3]
// output: hel
```

### Combining with Index From End

Ranges can use `^` for either or both bounds.

```csharp
"hello"[1..^1]
// output: ell
```

## Sandbox Requirements

Creating `System.Index` and `System.Range` values does not require any sandbox flags. However, using them to slice into strings or arrays uses member access and requires the appropriate sandbox permissions (such as `AllowMethodCalls` for string slicing).

:::note
The inclusive range (`..=`) and exclusive range (`..<`) operators are Extended-mode-only and are not available in Standard mode.
:::

## See Also

- [Member access](./member-access) -- `[]` element access and sandbox requirements
- [Operators overview](./index) -- full precedence table
