---
title: "Member Access Operators"
description: "Member access, element access, and invocation operators in Alder with sandbox requirements."
sidebar:
  order: 9
---

## Overview

Member access operators are the most sandbox-sensitive operators in Alder. Almost every form of member access requires a specific sandbox flag to be enabled. The examples on this page use `SandboxOptions.Trusted()`, which enables all sandbox flags.

## Member Access (`.`)

The `.` operator accesses members of a value: properties, fields, and methods.

### Instance Property Access

Requires the `AllowPropertyRead` sandbox flag.

```csharp
"hello".Length
// output: 5
```

### Static Property Access

Requires the `AllowStaticPropertyRead` sandbox flag.

```csharp
int.MaxValue
// output: 2147483647

string.Empty
// output:
```

### Static Field Access

Requires the `AllowStaticFieldRead` sandbox flag. Static fields on well-known types (like `int.MaxValue`, `double.NaN`) are accessed through this mechanism when the member is a field rather than a property.

```csharp
double.NaN
// output: NaN
```

## Element Access (`[]`)

The `[]` operator accesses elements by index or key.

### String Indexing

Strings support character access by index. This uses element access and requires appropriate sandbox permissions.

```csharp
"hello"[0]
// output: h

"hello"[4]
// output: o
```

### Array Indexing

Arrays support element access by integer index.

```csharp
new int[] { 10, 20, 30 }[1]
// output: 20
```

### Index Set

Writing to an index position (e.g., `arr[i] = value`) requires the `AllowIndexSet` sandbox flag.

## Invocation (`()`)

The `()` operator invokes methods. Requires the `AllowMethodCalls` sandbox flag.

```csharp
"hello".ToUpper()
// output: HELLO

"hello world".Contains("world")
// output: True

"  hello  ".Trim()
// output: hello
```

### Method Chaining

Multiple invocations can be chained, each requiring `AllowMethodCalls`.

```csharp
"Hello World".ToLower().Trim()
// output: hello world
```

## Property Set

Writing to a property (e.g., `obj.Prop = value`) requires the `AllowPropertySet` sandbox flag.

## Construction (`new`)

Object creation with `new` requires the `AllowConstruction` sandbox flag. See the Engine API documentation for details on allowed types and construction rules.

## Sandbox Requirements Summary

| Operation              | Example             | Required Sandbox Flag     |
| ---------------------- | ------------------- | ------------------------- |
| Instance property read | `"hello".Length`    | `AllowPropertyRead`       |
| Static property read   | `int.MaxValue`      | `AllowStaticPropertyRead` |
| Static field read      | `double.NaN`        | `AllowStaticFieldRead`    |
| Method call            | `"hello".ToUpper()` | `AllowMethodCalls`        |
| Property write         | `obj.Prop = value`  | `AllowPropertySet`        |
| Index write            | `arr[i] = value`    | `AllowIndexSet`           |
| Construction           | `new List<int>()`   | `AllowConstruction`       |

:::note
The `SandboxOptions.Trusted()` preset enables all of these flags. For restricted sandboxes, each flag must be enabled individually.
:::

## Null-Conditional Variants

The null-conditional operators `?.` and `?[]` are covered on the [Null operators](./null-operators) page.

## See Also

- [Null operators](./null-operators) -- `?.`, `?[]`, `??`, `??=`
- [Type testing](./type-testing) -- `is`, `as`, casts, `typeof`
- [Assignment operators](./assignment) -- `=`, `+=`, and compound assignment
