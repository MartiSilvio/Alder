---
title: "Conditional Operator"
description: "The ternary conditional operator (?:) in CsEval."
sidebar:
  order: 12
---

## Overview

The conditional operator `?:` evaluates one of two expressions based on a boolean condition. It is the only ternary operator in C# and works without any sandbox restrictions.

## Syntax

```
condition ? consequent : alternative
```

If `condition` is `true`, the `consequent` expression is evaluated and returned. If `false`, the `alternative` expression is evaluated and returned.

```csharp
true ? 1 : 2
// output: 1

false ? 1 : 2
// output: 2

5 > 3 ? "yes" : "no"
// output: yes
```

## Short-Circuit Evaluation

Only the selected branch is evaluated. The other branch is not executed.

```csharp
true ? 42 : 1 / 0
// output: 42
```

In this example, `1 / 0` is never evaluated because the condition is `true`.

## Right-Associativity

The conditional operator is right-associative. Nested ternaries chain naturally from right to left:

```
a ? b : c ? d : e
```

is parsed as:

```
a ? b : (c ? d : e)
```

### Nested Example

```csharp
{ var x = 15; return x > 20 ? "large" : x > 10 ? "medium" : "small"; }
// output: medium
```

## Type Rules

Both branches must produce values with a compatible type. The compiler determines a common type from the two branches:

- If both branches have the same type, the result has that type
- If one branch can be implicitly converted to the other's type, the wider type is used
- If neither branch can be converted to the other, it is a compilation error

```csharp
true ? 1 : 2.5
// output: 1

false ? 1 : 2.5
// output: 2.5
```

In this example, the `int` branch is promoted to `double` because both branches must share a common type.

## See Also

- [Boolean logical operators](./boolean-logical) -- `&&`, `||`, `!` for combining conditions
- [Null operators](./null-operators) -- `??` for null-coalescing, an alternative to `x != null ? x : default`
- [Operators overview](./index) -- full precedence table
