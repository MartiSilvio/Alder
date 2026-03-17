---
title: "Tuples"
description: "ValueTuple creation, named elements, deconstruction, and arity support in CsEval."
sidebar:
  order: 6
---

CsEval supports C# value tuples backed by the `System.ValueTuple<>` family of types. Tuples provide a lightweight way to group multiple values without defining a class or struct.

## Tuple Creation

Create tuples using parenthesized comma-separated expressions. The compiler infers element types.

```csharp
(1, "hello", true)
// output: (1, hello, True)
```

The resulting type is `System.ValueTuple<int, string, bool>`.

```csharp
(1, 2)
// output: (1, 2)
```

Single-element tuples are supported (minimum arity is 1). Zero-element tuples are not allowed.

```csharp
(42, )
// output: (42)
```

Tuple elements can be arbitrary expressions.

```csharp
(1 + 2, "hello".Length)
// output: (3, 5)
```

## Named Elements

Tuple elements can be given names. Names are metadata and do not change the underlying `ValueTuple` type.

```csharp
(Name: "Alice", Age: 30)
// output: (Alice, 30)
```

Named and unnamed elements can be mixed, though this is uncommon.

## Accessing Elements

Use `Item1`, `Item2`, etc. to access tuple elements positionally.

```csharp
var t = (10, "hello", true);
t.Item1
// output: 10
```

```csharp
var t = (10, "hello", true);
t.Item2
// output: "hello"
```

When names are provided, you can access elements by name.

```csharp
var t = (Name: "Alice", Age: 30);
t.Name
// output: "Alice"
```

## Tuple Deconstruction

Deconstruct tuples into individual variables using `var (x, y)` syntax.

```csharp
var (x, y) = (10, 20);
x + y
// output: 30
```

```csharp
var (name, age) = (Name: "Alice", Age: 30);
name
// output: "Alice"
```

## Tuple Comparison

Tuples support `==` and `!=` comparison, which compares elements pairwise.

```csharp
(1, 2) == (1, 2)
// output: true
```

```csharp
(1, 2) != (1, 3)
// output: true
```

## Implicit Tuple Conversions

Tuples with the same arity convert implicitly if each element converts implicitly. Per ECMA-334 section 10.2.13.

```csharp
(int, int) t = (1, 2);
(long, long) u = t;
u.Item1
// output: 1
```

## Arity Support

CsEval supports tuples with 1 through 7 elements directly using `ValueTuple<T1>` through `ValueTuple<T1, T2, T3, T4, T5, T6, T7>`.

For 8 or more elements, CsEval uses the nested `ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>` form, where the eighth type parameter holds the remaining elements as another `ValueTuple`.

```csharp
(1, 2, 3, 4, 5, 6, 7, 8)
// output: (1, 2, 3, 4, 5, 6, 7, 8)
```

Elements beyond 7 are accessed through the nested `Rest` field internally, but `Item8`, `Item9`, etc. still work as expected.

## See Also

- [Built-in Types](./built-in-types)
- [Collections](./collections)
- [Nullable Types and Conversions](./nullable-and-conversions)
