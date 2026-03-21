---
title: "Selection Statements"
description: "if/else, switch statement with pattern matching and when guards, and switch expression syntax in Alder."
sidebar:
  order: 2
---

## Overview

Alder supports `if`/`else` chains, `switch` statements with full pattern matching support (constant, type, relational, logical patterns, and `when` guards), and `switch` expressions. The selection statements themselves require no sandbox flags, but operations inside them (method calls, property access, construction) may require the corresponding sandbox options.

## if/else

```csharp
{ var x = 5; if (x > 3) { return "big"; } else { return "small"; } }
// output: big
```

### if/else-if/else Chains

```csharp
{
    var x = 5;
    if (x > 10) { return "large"; }
    else if (x > 3) { return "medium"; }
    else { return "small"; }
}
// output: medium
```

### Single-Statement Body

Braces are optional for single-statement bodies:

```csharp
{ var x = 10; if (x > 5) return "yes"; return "no"; }
// output: yes
```

## switch Statement

### Constant Patterns

```csharp
{
    var x = 2;
    var result = "";
    switch (x)
    {
        case 1: result = "one"; break;
        case 2: result = "two"; break;
        case 3: result = "three"; break;
        default: result = "other"; break;
    }
    return result;
}
// output: two
```

### Type Patterns

```csharp
{
    object x = 42;
    var result = "";
    switch (x)
    {
        case int n: result = "int:" + n; break;
        case string s: result = "string:" + s; break;
        default: result = "other"; break;
    }
    return result;
}
// output: int:42
```

### when Guards

```csharp
{
    object x = 42;
    var result = "";
    switch (x)
    {
        case int n when n > 10: result = "big int"; break;
        case int n: result = "small int"; break;
        default: result = "other"; break;
    }
    return result;
}
// output: big int
```

### goto case and goto default

Use `goto case` or `goto default` for explicit control flow between cases:

```csharp
{
    var x = 1;
    var result = "";
    switch (x)
    {
        case 1: result = "start"; goto case 2;
        case 2: result = result + "-end"; break;
        default: result = "default"; break;
    }
    return result;
}
// output: start-end
```

```csharp
{
    var x = 1;
    var result = "";
    switch (x)
    {
        case 1: result = "start"; goto default;
        case 2: result = "two"; break;
        default: result = result + "-default"; break;
    }
    return result;
}
// output: start-default
```

### Fall-Through Behavior

Alder enforces the C# rule that non-empty case blocks cannot fall through implicitly. Each non-empty case must end with `break`, `return`, `goto case`, or `goto default`.

```csharp
// This produces an error: CS0163
{
    var x = 1;
    var result = "";
    switch (x)
    {
        case 1: result = "one";
        case 2: result = "two"; break;
    }
    return result;
}
// output: AlderException: CS0163: Control cannot fall through from one case label to another
```

Empty cases are allowed to fall through to the next case:

```csharp
{
    var x = 1;
    var result = "";
    switch (x)
    {
        case 1:
        case 2: result = "one or two"; break;
        default: result = "other"; break;
    }
    return result;
}
// output: one or two
```

## switch Expression

The `switch` expression provides a compact form for value-producing matches:

```csharp
{
    var x = 2;
    return x switch { 1 => "one", 2 => "two", _ => "other" };
}
// output: two
```

Switch expressions support the same pattern types as switch statements:

```csharp
{
    object val = 42;
    return val switch
    {
        int n when n > 100 => "big",
        int n => "int:" + n,
        string s => "str:" + s,
        _ => "unknown"
    };
}
// output: int:42
```

For more complex pattern matching syntax (relational, logical, property, positional patterns), see the [Pattern Matching](../operators/pattern-matching) reference.

## See Also

- [Declaration Statements](./declaration-statements) -- variable declarations and scoping
- [Iteration Statements](./iteration-statements) -- loops
- [Pattern Matching](../operators/pattern-matching) -- full pattern matching reference
