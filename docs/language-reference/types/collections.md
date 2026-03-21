---
title: "Collections"
description: "Arrays, List<T>, Dictionary<TKey, TValue>, and collection creation patterns in Alder Standard mode."
sidebar:
  order: 5
---

Alder Standard mode supports arrays, generic lists, and dictionaries using `new` keyword patterns. All collection types from `System.Collections.Generic` are implicitly available without a `using` directive.

## Array Creation

Create arrays with an explicit size or with an initializer list.

### Sized Arrays

```csharp
new int[3]
// output: System.Int32[] (length 3, all elements 0)
```

```csharp
new string[2]
// output: System.String[] (length 2, all elements null)
```

### Array Literals

```csharp
new int[] { 1, 2, 3 }
// output: System.Int32[] { 1, 2, 3 }
```

```csharp
new string[] { "hello", "world" }
// output: System.String[] { "hello", "world" }
```

### Multidimensional Arrays

Alder supports multidimensional array creation with comma-separated dimensions.

```csharp
new int[3, 4]
// output: System.Int32[,] (3 x 4, all elements 0)
```

### Jagged Arrays

Jagged arrays (arrays of arrays) use the standard C# syntax.

```csharp
new int[3][]
// output: System.Int32[][] (length 3, all elements null)
```

## List&lt;T&gt;

Create lists using `new List<T>` with a collection initializer. The `System.Collections.Generic` namespace is implicitly imported.

```csharp
new List<int> { 1, 2, 3 }
// output: System.Collections.Generic.List`1[System.Int32] { 1, 2, 3 }
```

Access elements by index and query the count.

```csharp
var list = new List<string> { "a", "b", "c" };
list[1]
// output: "b"
```

```csharp
var list = new List<int> { 10, 20, 30 };
list.Count
// output: 3
```

The `Add` method appends elements after creation.

```csharp
var list = new List<int> { 1, 2 };
list.Add(3);
list.Count
// output: 3
```

## Dictionary&lt;TKey, TValue&gt;

Create dictionaries using `new Dictionary<TKey, TValue>` with a collection initializer.

```csharp
new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }
// output: System.Collections.Generic.Dictionary`2[System.String,System.Int32] { ["a"] = 1, ["b"] = 2 }
```

Access values by key and check for key existence.

```csharp
var dict = new Dictionary<string, int> { ["x"] = 10, ["y"] = 20 };
dict["x"]
// output: 10
```

```csharp
var dict = new Dictionary<string, int> { ["x"] = 10, ["y"] = 20 };
dict.ContainsKey("y")
// output: true
```

```csharp
var dict = new Dictionary<string, int> { ["x"] = 10 };
dict.Count
// output: 1
```

:::note[Collection Expressions Are Extended Mode Only]
The `[1, 2, 3]` collection expression syntax and the `..` spread operator are **Extended-mode-only** features. In Standard mode, use `new T[] { ... }` or `new List<T> { ... }` to create collections.
:::

## See Also

- [Built-in Types](./built-in-types)
- [Tuples](./tuples)
- [Nullable Types and Conversions](./nullable-and-conversions)
