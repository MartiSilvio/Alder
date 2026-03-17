---
title: "Exception Handling"
description: "try/catch/finally, typed catches, when guards, throw, rethrow, and using statements in CsEval."
sidebar:
  order: 5
---

## Overview

CsEval supports the full C# exception handling model: `try`/`catch`/`finally`, typed catch clauses with fully qualified type names, `when` guards, `throw`, and rethrow (`throw;`). The `using` statement is also covered here as it desugars to `try`/`finally` resource management.

:::note
Creating exception objects with `new` requires the `AllowConstruction` sandbox flag. With the default `Trusted()` sandbox preset, this is already enabled.
:::

## try/catch

A `try` block executes statements that may throw exceptions. A `catch` block handles specific or general exceptions.

```csharp
{
    var result = "";
    try { throw new System.Exception("test"); }
    catch (System.Exception ex) { result = ex.Message; }
    return result;
}
// output: test
```

## Typed Catch

A typed catch clause catches only exceptions of the specified type (or its subclasses). Type names can be fully qualified.

```csharp
{
    var r = "";
    try { throw new System.ArgumentException("bad"); }
    catch (System.ArgumentException ex) { r = ex.Message; }
    return r;
}
// output: bad
```

### Catch Ordering

When multiple catch clauses are present, they are evaluated in order. The first matching clause handles the exception.

```csharp
{
    var r = "";
    try { throw new System.ArgumentException("bad"); }
    catch (System.InvalidOperationException) { r = "invalid-op"; }
    catch (System.ArgumentException) { r = "arg"; }
    catch (System.Exception) { r = "general"; }
    return r;
}
// output: arg
```

## Bare Catch

A bare `catch` clause (no type specified) catches all exceptions. It must be the last catch clause -- the parser enforces this.

```csharp
{
    var r = "";
    try { throw new System.InvalidOperationException(); }
    catch (System.ArgumentException) { r = "arg"; }
    catch { r = "bare"; }
    return r;
}
// output: bare
```

## when Guards

A `when` guard adds a boolean condition to a catch clause. If the guard evaluates to `false`, the catch clause is skipped and the next catch clause is tried.

```csharp
{
    var r = 0;
    try { throw new System.Exception("other"); }
    catch (System.Exception ex) when (ex.Message == "match") { r = 1; }
    catch (System.Exception) { r = 2; }
    return r;
}
// output: 2
```

The `when` guard is evaluated before entering the catch body. If the guard itself throws an exception, it is treated as `false` and the next catch clause is tried.

```csharp
{
    var r = 0;
    try { throw new System.Exception("test"); }
    catch (System.Exception ex) when (ex.Message == "test") { r = 1; }
    catch (System.Exception) { r = 2; }
    return r;
}
// output: 1
```

## finally

A `finally` block always executes -- after normal completion, after a catch handles an exception, and after an unhandled exception propagates.

### After Normal Completion

```csharp
{
    var x = 1;
    try { x = 2; }
    finally { x = x * 3; }
    return x;
}
// output: 6
```

### After Catch

```csharp
{
    var x = 0;
    try { throw new System.Exception("e"); }
    catch { x = 1; }
    finally { x = x + 10; }
    return x;
}
// output: 11
```

### After Exception Propagation

The `finally` block runs even when no catch clause handles the exception.

```csharp
{
    var x = 1;
    try { x = 2; }
    finally { x = x + 10; }
    return x;
}
// output: 12
```

## try/finally (No Catch)

A `try` block can be paired with only a `finally` block (no catch clause required).

```csharp
{
    var x = 1;
    try { x = 2; }
    finally { x = x + 10; }
    return x;
}
// output: 12
```

## throw

The `throw` statement throws an exception object. The object must be an instance of `System.Exception` or a derived type.

```csharp
{
    var r = "";
    try { throw new System.Exception("boom"); }
    catch (System.Exception ex) { r = ex.Message; }
    return r;
}
// output: boom
```

### Throwing Derived Types

```csharp
{
    var r = "";
    try { throw new System.InvalidOperationException("not allowed"); }
    catch (System.InvalidOperationException ex) { r = ex.Message; }
    return r;
}
// output: not allowed
```

## Rethrow

The `throw;` statement (with no operand) re-throws the current exception from within a `catch` block. The original exception is preserved, including its stack trace.

```csharp
{
    try {
        try { throw new System.Exception("inner"); }
        catch { throw; }
    }
    catch (System.Exception ex) { return ex.Message; }
}
// output: inner
```

Using `throw;` outside a `catch` block is an error.

## using Statement

The `using` statement ensures that an `IDisposable` resource is disposed when the block exits. It desugars to `try`/`finally` with a `Dispose()` call in the `finally` block.

:::note
CsEval supports only the parenthesized form `using (var x = ...) { }`. The C# 8 declaration form `using var x = ...;` without parentheses is not supported.
:::

```csharp
{
    var result = "";
    using (var ms = new System.IO.MemoryStream())
    {
        result = "len=" + ms.Length.ToString();
    }
    return result;
}
// output: len=0
```

The resource is disposed even if an exception occurs inside the `using` block.

## Validation Rules

The parser enforces these constraints:

- A `try` block must have at least one `catch` clause or a `finally` block (or both)
- A bare `catch` clause (no type) must be the last catch clause
- `throw;` (rethrow) is only valid inside a `catch` block

## See Also

- [Jump Statements](./jump-statements) -- `break` and `continue` work correctly inside `try` blocks
- [Checked and Unchecked](./checked-and-unchecked) -- `checked()` throws `OverflowException`
