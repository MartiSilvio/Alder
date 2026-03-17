---
title: "Declaration Statements"
description: "Variable declarations, const, multi-var, deconstruction, local functions, using, lock, and block scoping in CsEval."
sidebar:
  order: 1
---

## Overview

CsEval supports variable declarations with `var`, typed declarations, `const`, multi-variable declarations, deconstruction, and local functions. Variables declared inside blocks, loops, or `if` bodies are scoped to that block via child `CsEvalContext` scopes.

## var Declarations

The `var` keyword infers the type from the initializer expression. An initializer is always required.

```csharp
{ var x = 42; return x; }
// output: 42

{ var name = "hello"; return name; }
// output: hello

{ var flag = true; return flag; }
// output: True
```

Assigning `null` to a `var` variable is a parser error because the type cannot be inferred:

```csharp
{ var x = null; return x; }
// output: CsEvalParserException: CS0815: Cannot assign null to an implicitly-typed variable
```

## Typed Declarations

Specify the type explicitly using any built-in type keyword.

```csharp
{ int x = 42; return x; }
// output: 42

{ string s = "world"; return s; }
// output: world

{ double pi = 3.14; return pi; }
// output: 3.14
```

### Generic Type Declarations

Generic types use the full generic syntax.

```csharp
{ List<int> nums = new List<int>(); nums.Add(1); nums.Add(2); return nums.Count; }
// output: 2
```

### Fully Qualified Type Declarations

Types can be declared with their full namespace path.

```csharp
{ System.Text.StringBuilder sb = new System.Text.StringBuilder("hello"); return sb.ToString(); }
// output: hello
```

## const Declarations

The `const` keyword declares a compile-time constant. The type must be specified explicitly.

```csharp
{ const int MAX = 100; return MAX; }
// output: 100

{ const string GREETING = "hello"; return GREETING; }
// output: hello
```

Attempting to reassign a `const` variable produces an error.

## Multi-Variable Declarations

Multiple variables of the same type can be declared in a single statement.

```csharp
{ int x = 1, y = 2; return x + y; }
// output: 3

{ int a = 10, b = 20, c = 30; return a + b + c; }
// output: 60
```

## Deconstruction Declarations

Tuple values can be deconstructed into individual variables.

```csharp
{ var (a, b) = (1, 2); return a + b; }
// output: 3

{ var (x, y, z) = (10, 20, 30); return x + y + z; }
// output: 60
```

## Local Functions

Local functions are declared with a return type keyword, name, and parameter list. They are desugared into lambda-assigned variables internally.

```csharp
{ int Square(int n) { return n * n; } return Square(5); }
// output: 25

{ int Add(int a, int b) { return a + b; } return Add(3, 4); }
// output: 7
```

Local functions can call each other and use variables from the enclosing scope:

```csharp
{ var factor = 10; int Scale(int n) { return n * factor; } return Scale(3); }
// output: 30
```

:::note
Local function parameters only support built-in type keywords (`int`, `string`, `bool`, `double`, etc.) as parameter types. Identifier-named types like `Exception` or `List<int>` are not supported as parameter types in local functions.
:::

## Block Statements

A block groups statements into a single scope. Variables declared inside a block are not visible outside it.

```csharp
{
    var x = 1;
    { var y = 2; x = x + y; }
    return x;
}
// output: 3
```

## using Statement

The `using` statement ensures that a disposable resource is disposed at the end of the block. Only the parenthesized form is supported.

```csharp
{
    var result = 0;
    using (var ms = new System.IO.MemoryStream())
    {
        result = 1;
    }
    return result;
}
// output: 1
```

:::note
The C# 8 `using` declaration form (`using var x = ...;` without parentheses) is **not supported**. Only the classic `using (var x = ...) { }` form works. The parenthesized form produces a parser error: `Expected '(' after 'using'`.
:::

## lock Statement

The `lock` statement acquires a mutual-exclusion lock on an object for the duration of the block.

```csharp
{
    var obj = new object();
    var x = 0;
    lock (obj) { x = 42; }
    return x;
}
// output: 42
```

## Scoping Rules

Variables are scoped to the block in which they are declared. Each block, loop body, and `if`/`else` body creates a child `CsEvalContext` scope. Variables declared in a child scope are accessible within that scope and any nested scopes, but not in the parent scope.

```csharp
{
    var x = 1;
    if (true) { var y = 2; x = x + y; }
    return x;
}
// output: 3
```

Loop variables are scoped to the loop body:

```csharp
{ var sum = 0; for (var i = 0; i < 3; i++) { sum += i; } return sum; }
// output: 3
```

## See Also

- [Selection Statements](./selection-statements) -- if/else and switch
- [Iteration Statements](./iteration-statements) -- for, foreach, while, do-while
