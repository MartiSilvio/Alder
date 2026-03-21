---
title: "Null Operators"
description: "Null-coalescing, null-coalescing assignment, and null-conditional operators in Alder."
sidebar:
  order: 8
---

## Overview

Alder supports the standard C# null operators for safely working with nullable values: null-coalescing (`??`), null-coalescing assignment (`??=`), null-conditional member access (`?.`), and null-conditional element access (`?[]`).

## Null-Coalescing (`??`)

Returns the left operand if it is non-null; otherwise returns the right operand. The right operand is only evaluated if the left operand is `null` (short-circuit evaluation).

```csharp
(int?)null ?? 42
// output: 42

(int?)5 ?? 42
// output: 5

(string)null ?? "default"
// output: default

"hello" ?? "default"
// output: hello
```

### Chaining

The `??` operator is right-associative, so chained expressions evaluate left-to-right, returning the first non-null value.

```csharp
(string)null ?? (string)null ?? "fallback"
// output: fallback
```

## Null-Coalescing Assignment (`??=`)

Assigns the right operand to the left operand only if the left operand is `null`.

:::caution
`??=` requires the `AllowAssignment` sandbox flag. With the default `SandboxOptions.Trusted()` preset, this is already enabled.
:::

```csharp
{ int? x = null; x ??= 42; return x; }
// output: 42

{ int? x = 10; x ??= 42; return x; }
// output: 10
```

## Null-Conditional Member Access (`?.`)

Accesses a member only if the operand is non-null. If the operand is `null`, the entire expression evaluates to `null` without throwing a `NullReferenceException`.

```csharp
"hello"?.Length
// output: 5

((string)null)?.Length
// output:
```

### Chaining

Null-conditional access can be chained. If any step in the chain is `null`, the entire chain short-circuits to `null`.

```csharp
"hello"?.ToUpper()?.Length
// output: 5

((string)null)?.ToUpper()?.Length
// output:
```

## Null-Conditional Element Access (`?[]`)

Accesses an element by index only if the operand is non-null. If the operand is `null`, the expression evaluates to `null`.

```csharp
{ var arr = new int[] { 10, 20, 30 }; return arr?[1]; }
// output: 20

{ var arr = (int[])null; return arr?[0]; }
// output:
```

## See Also

- [Assignment operators](./assignment) -- `=`, `+=`, and other assignment operators
- [Boolean logical operators](./boolean-logical) -- three-value `bool?` logic
- [Member access](./member-access) -- `.` and `[]` access operators
