---
title: "Lambda Expressions"
description: "Lambda expression syntax, closures, and invocation in CsEval."
sidebar:
  order: 13
---

## Overview

Lambda expressions create anonymous functions using the `=>` operator. CsEval supports all standard C# lambda syntax variants including expression bodies, block bodies, typed and untyped parameters, and closures.

Lambda **creation** does not require any sandbox flags. Lambda **invocation** (calling the lambda) requires `AllowMethodCalls`.

## Expression Body

An expression body lambda evaluates a single expression and returns the result.

### Single parameter (untyped)

```csharp
{ var f = x => x + 1; return f(5); }
// output: 6
```

When there is a single untyped parameter, parentheses are optional.

### Single parameter (typed)

```csharp
{ var f = (int x) => x * 2; return f(5); }
// output: 10
```

### Multiple parameters

```csharp
{ var f = (int x, int y) => x + y; return f(3, 4); }
// output: 7
```

### No parameters

```csharp
{ var f = () => 42; return f(); }
// output: 42
```

## Block Body

A block body lambda uses braces and explicit `return` statements, allowing multiple statements.

```csharp
{ var f = (int x) => { var y = x * 2; return y + 1; }; return f(3); }
// output: 7
```

Block bodies follow the same rules as any block scope -- you can declare local variables, use control flow, and must use `return` to produce a value.

```csharp
{
    var classify = (int x) => {
        if (x > 0) return "positive";
        if (x < 0) return "negative";
        return "zero";
    };
    return classify(-5);
}
// output: negative
```

## Closures

Lambdas capture outer variables by reference, following standard C# closure semantics. Changes to captured variables are visible inside the lambda, and the lambda can modify captured variables.

### Reading a captured variable

```csharp
{ var n = 10; var f = (int x) => x + n; return f(5); }
// output: 15
```

### Mutating a captured variable

```csharp
{
    var count = 0;
    var inc = () => { count = count + 1; return count; };
    inc();
    inc();
    return inc();
}
// output: 3
```

Each call to `inc()` increments the shared `count` variable. The lambda and the outer scope share the same variable.

## Variable Assignment

Lambdas are typically assigned to a `var`-declared variable, then invoked by name.

```csharp
{ var greet = (string name) => $"Hello {name}!"; return greet("World"); }
// output: Hello World!
```

You can also assign lambdas with explicit `Func<>` type annotations when the parameter types cannot be inferred:

```csharp
{ Func<int, int> doubler = x => x * 2; return doubler(5); }
// output: 10
```

## Lambdas as Arguments

Lambdas are commonly passed as arguments to methods like LINQ operators:

```csharp
{
    var numbers = new int[] { 1, 2, 3, 4, 5 };
    return numbers.Where(x => x > 3).Count();
}
// output: 2
```

:::note
Passing lambdas to methods requires `AllowMethodCalls`. The default `Trusted()` sandbox preset enables this.
:::

## Sandbox Interaction

| Operation | Sandbox Flag |
|---|---|
| Creating a lambda (`x => x + 1`) | None required |
| Invoking a lambda (`f(5)`) | `AllowMethodCalls` |
| Assigning a lambda to a variable (`var f = ...`) | `AllowAssignment` (for reassignment; `var` declarations are always allowed) |

## See Also

- [Member access](./member-access) -- method invocation via `()`
- [Assignment operators](./assignment) -- variable declaration and assignment
- [Operators overview](./index) -- full precedence table
