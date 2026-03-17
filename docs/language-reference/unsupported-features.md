---
title: "Unsupported Features"
description: "Definitive list of C# constructs that CsEval does not support, organized by category."
sidebar:
  order: 50
---

## Overview

CsEval evaluates expressions and statement blocks, not compilation units. It cannot define types, use async/await, or access features requiring a full compiler pipeline. This page lists everything CsEval does **not** support. For each item, the behavior when attempted is noted as **Parser error** (the lexer/parser rejects the input), **Runtime error** (parsing succeeds but evaluation fails), or **Not applicable** (the concept does not map to an expression evaluator).

## Type Declarations

CsEval evaluates expressions, not compilation units. You cannot define new types.

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| Class | `class Foo { }` | Parser error |
| Struct | `struct Point { }` | Parser error |
| Interface | `interface IFoo { }` | Parser error |
| Enum | `enum Color { Red, Green }` | Parser error |
| Record | `record Person(string Name)` | Parser error |
| Delegate type | `delegate void Action()` | Parser error |
| Namespace | `namespace Foo { }` | Parser error |

All seven keywords are reserved in the lexer but have no parser handler. Any input beginning with one of these keywords produces an immediate parse error.

## Access and Member Modifiers

These modifiers are used to declare type members, which CsEval does not support.

| Feature | Keywords | Behavior |
|---------|----------|----------|
| Access modifiers | `public`, `private`, `protected`, `internal` | Parser error |
| Inheritance modifiers | `virtual`, `override`, `abstract`, `sealed` | Parser error |
| Static modifier | `static` (on members) | Parser error |
| Extern modifier | `extern` | Parser error |
| Partial modifier | `partial` | Parser error |
| Volatile modifier | `volatile` | Parser error |
| Readonly modifier | `readonly` (on fields) | Parser error |
| Event declaration | `event EventHandler Clicked;` | Parser error |
| Conversion operators | `implicit operator`, `explicit operator` | Parser error |

These keywords are reserved in the lexer but have no parser handler for member declaration contexts.

**Note:** `const` IS supported for local constant declarations (`const int x = 5;`). It is only unsupported in the type-member context (e.g., `const` fields on a class).

## Async and Await

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| Async lambda | `async () => await Task.Delay(1)` | Parser error |
| Await expression | `await SomeTask()` | Parser error |
| Async method declaration | `async Task<int> Foo() { }` | Not applicable |

The `async` and `await` keywords are reserved in the lexer but have no parser handler. There is no async state machine generation. Asynchronous code cannot be evaluated.

## Yield and Iterators

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| Yield return | `yield return 1` | Parser error |
| Yield break | `yield break` | Parser error |
| Iterator methods | `IEnumerable<int> GetItems() { yield return 1; }` | Not applicable |

The `yield` keyword is reserved but has no parser or evaluator support. Iterator generation is not available.

## Unsafe Code

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| Unsafe block | `unsafe { }` | Parser error |
| Fixed statement | `fixed (int* p = &x) { }` | Parser error |
| Pointer types | `int*`, `void*` | Parser error |
| Address-of operator | `&variable` (pointer context) | Parser error |
| Stackalloc | `stackalloc int[10]` | Parser error |
| Pointer dereference | `*ptr` | Not applicable |

These features are blocked for security. CsEval is designed for sandboxed evaluation where pointer manipulation is not permitted.

**Note:** `sizeof` IS supported for built-in value types (e.g., `sizeof(int)` returns `4`). It is only unsupported for user-defined types.

## ref, in, and Related Semantics

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| ref locals | `ref int x = ref arr[0]` | Parser error |
| ref returns | `ref int GetRef() { }` | Not applicable |
| in parameter modifier | `void Foo(in int x)` | Not applicable |
| ref struct | `ref struct Span { }` | Not applicable |
| params in lambda params | `(params int[] args) => ...` | Parser error |

**Partial support:** `out` arguments in method calls ARE supported. You can write:

```csharp
{
    var dict = new System.Collections.Generic.Dictionary<string, int> { ["a"] = 1 };
    dict.TryGetValue("a", out var val);
    return val;
}
// output: 1
```

## this and base Keywords

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| this reference | `this.Property` | Parser error |
| base reference | `base.Method()` | Parser error |
| this constructor | `this(args)` | Not applicable |
| base constructor | `base(args)` | Not applicable |

CsEval evaluates standalone expressions with no enclosing type instance. The `this` and `base` keywords have no meaning in this context.

## Modern C# Features (C# 8-12)

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| Using declarations (no parens) | `using var x = new MemoryStream();` | Parser error |
| Await using | `await using var x = ...` | Parser error |
| List patterns | `x is [1, 2, ..]` | Parser error |
| Required modifier | `required string Name { get; set; }` | Not applicable |
| Scoped modifier | `scoped Span<int> s` | Not applicable |
| File-scoped types | `file class Foo { }` | Not applicable |
| Generic attributes | `[GenericAttribute<int>]` | Not applicable |
| Static abstract interface members | `static abstract int Parse(string s)` | Not applicable |
| UTF-8 string literals | `"hello"u8` | Parser error |
| with expressions | `person with { Name = "Bob" }` | Parser error |
| Raw string interpolation | `$"""text {expr} text"""` | Parser error |
| checked/unchecked blocks | `checked { int x = int.MaxValue + 1; }` | Parser error |

**Note:** The parenthesized `using` statement IS supported: `using (var x = new MemoryStream()) { }`. Only the C# 8 declaration form without parentheses is unsupported.

**Note:** `checked(expr)` and `unchecked(expr)` expression forms ARE supported. Only the block statement form is unsupported.

## Preprocessor Directives and Attributes

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| Preprocessor directives | `#if`, `#else`, `#endif`, `#region`, `#define` | Parser error |
| Attributes | `[Serializable]`, `[Obsolete("msg")]` | Parser error |
| XML documentation | `/// <summary>` | Parser error |
| Global using | `global using System;` | Not applicable |

CsEval processes raw expression text. It has no preprocessor pipeline and no attribute resolution.

## Platform-Specific Type Keywords

| Feature | Typical Syntax | Behavior |
|---------|---------------|----------|
| nint | `nint x = 42` | Runtime error (type resolution fails) |
| nuint | `nuint x = 42` | Runtime error (type resolution fails) |
| dynamic | `dynamic x = 42` | Runtime error (type resolution fails) |

These three keywords are reserved in the lexer and have token entries, but they are NOT present in the type resolver's built-in type dictionary. They lex successfully but fail at type resolution time.

## NOT Unsupported (Common Misconceptions)

The following features **are fully supported** despite looking like they might not be. Do NOT assume these are missing.

### LINQ Query Syntax

CsEval includes a complete `QueryParser` that desugars query expressions into LINQ method call chains at parse time. All standard query clauses work:

```csharp
{
    var nums = new int[] { 1, 2, 3, 4, 5 };
    return (from n in nums where n > 2 select n * 10).Count();
}
// output: 3 (elements 3, 4, 5 pass the filter)
```

Supported clauses: `from`, `where`, `select`, `orderby` (ascending/descending), `group by`, `join`/`on`/`equals`, `let`, `into`.

### Pattern Matching

Full pattern matching support including `is` expressions and `switch` expressions:

```csharp
{
    var x = 42;
    return x is int n && n > 0 ? "positive" : "other";
}
// output: positive
```

Supported patterns: constant, type, relational (`> 5`, `< 10`), logical (`and`, `or`, `not`), var, discard (`_` in switch context), positional, property.

### Tuple Deconstruction

```csharp
{ var (a, b) = (10, 20); return a + b; }
// output: 30
```

Tuples of any arity (1 to 8+), named elements, element-wise comparison, and deconstruction are all supported.

### Null-Conditional Operators

```csharp
{
    var s = (string)null;
    return s?.Length ?? -1;
}
// output: -1
```

Both `?.` (null-conditional member access) and `?[]` (null-conditional element access) are supported.

### Range and Index Operators

```csharp
{
    var arr = new int[] { 10, 20, 30, 40, 50 };
    return arr[^1];
}
// output: 50
```

The hat operator (`^` for index-from-end) and both-bounds range operator (`..`) are supported. Open-ended ranges (`[..3]`, `[2..]`, `[..]`) are not supported in subscript expressions.

### Exception Handling

Complete try/catch/finally, typed catches with `when` guards, throw, and rethrow:

```csharp
{
    try { throw new System.InvalidOperationException("test"); }
    catch (System.InvalidOperationException ex) when (ex.Message == "test") { return "caught"; }
}
// output: caught
```

### Iteration Statements

All loop forms: `for`, `foreach`, `while`, `do-while` with `break` and `continue`.

### Switch Statements and Expressions

Both `switch` statements (with pattern cases and `when` guards) and `switch` expressions are supported.

### using and lock Statements

The parenthesized `using (resource) { }` and `lock (obj) { }` statements are supported.

### Local Functions

Local function declarations are desugared to lambda variable declarations:

```csharp
{
    int Add(int a, int b) { return a + b; }
    return Add(3, 4);
}
// output: 7
```
