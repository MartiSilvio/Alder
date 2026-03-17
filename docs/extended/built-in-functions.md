---
title: "Extended Mode: Built-in Functions"
description: "Math functions, constants, date utilities, aggregate operations, and the it placeholder."
sidebar:
  order: 4
---

Extended mode provides bare-name access to math functions, date utilities, and aggregate operations. No `Math.` prefix, no function registration needed. All built-in names are shadowed by user-registered functions and variables.

## Math Functions

All math functions are **always case-sensitive** (`StringComparer.Ordinal`). Calling `Sin(0)` will not resolve to the built-in `sin`.

### Trigonometric

| Function | Maps to | Example |
|----------|---------|---------|
| `sin(x)` | `Math.Sin` | `sin(0)` returns `0.0` |
| `cos(x)` | `Math.Cos` | `cos(0)` returns `1.0` |
| `tan(x)` | `Math.Tan` | `tan(0)` returns `0.0` |
| `asin(x)` | `Math.Asin` | `asin(0)` returns `0.0` |
| `acos(x)` | `Math.Acos` | `acos(1)` returns `0.0` |
| `atan(x)` | `Math.Atan` | `atan(0)` returns `0.0` |
| `sinh(x)` | `Math.Sinh` | `sinh(0)` returns `0.0` |
| `cosh(x)` | `Math.Cosh` | `cosh(0)` returns `1.0` |
| `tanh(x)` | `Math.Tanh` | `tanh(0)` returns `0.0` |
| `atan2(y, x)` | `Math.Atan2` | `atan2(1, 1)` returns `Math.PI / 4` |

### Roots and Powers

| Function | Maps to | Example |
|----------|---------|---------|
| `sqrt(x)` | `Math.Sqrt` | `sqrt(4)` returns `2.0` |
| `cbrt(x)` | `Math.Pow(x, 1/3)` | `cbrt(27)` returns `3.0` |
| `pow(x, y)` | `Math.Pow` | `pow(2, 10)` returns `1024.0` |
| `exp(x)` | `Math.Exp` | `exp(0)` returns `1.0` |

### Logarithmic

| Function | Maps to | Example |
|----------|---------|---------|
| `log(x)` | `Math.Log` (natural) | `log(1)` returns `0.0` |
| `log(x, base)` | `Math.Log(x, base)` | `log(8, 2)` returns `3.0` |
| `log2(x)` | `Math.Log(x, 2)` | `log2(8)` returns `3.0` |
| `log10(x)` | `Math.Log10` | `log10(100)` returns `2.0` |
| `ln(x)` | `Math.Log` (alias) | `ln(1)` returns `0.0` |

### Rounding

| Function | Maps to | Example |
|----------|---------|---------|
| `floor(x)` | `Math.Floor` | `floor(3.7)` returns `3.0` |
| `ceil(x)` | `Math.Ceiling` | `ceil(3.2)` returns `4.0` |
| `round(x)` | `Math.Round` | `round(3.5)` returns `4.0` |
| `round(x, digits)` | `Math.Round(x, digits)` | `round(3.14159, 2)` returns `3.14` |
| `truncate(x)` | `Math.Truncate` | `truncate(3.9)` returns `3.0` |

### Comparison and Sign

| Function | Maps to | Notes |
|----------|---------|-------|
| `min(a, b)` | `Math.Min` | Type-preserving: `min(3, 5)` returns `3` (int) |
| `max(a, b)` | `Math.Max` | Type-preserving: `max(3, 5)` returns `5` (int) |
| `clamp(val, min, max)` | `Math.Min(Math.Max(val, min), max)` | Type-preserving |
| `sign(x)` | `Math.Sign` | Returns `-1`, `0`, or `1` |
| `abs(x)` | `Math.Abs` | Type-preserving: `abs(-5)` returns `5` (int) |

**Type-preserving** means that `int` input produces `int` output, `long` produces `long`, etc. Functions like `sin` and `sqrt` always return `double`.

## Math Constants

| Name | Value | Type |
|------|-------|------|
| `pi` | `Math.PI` (~3.14159) | `double` |
| `e` | `Math.E` (~2.71828) | `double` |
| `tau` | `Math.PI * 2` (~6.28318) | `double` |
| `infinity` | `double.PositiveInfinity` | `double` |
| `nan` | `double.NaN` | `double` |

```csharp
var engine = new CsEvalEngine(new CsEvalOptions { LanguageMode = LanguageMode.Extended });

engine.Evaluate("pi");        // output: 3.141592653589793
engine.Evaluate("2 * pi");    // output: 6.283185307179586
engine.Evaluate("e");         // output: 2.718281828459045
```

Constants are always case-sensitive: `PI` and `Pi` do not resolve to `Math.PI`.

## Date/Time Functions

| Function | Returns | Example |
|----------|---------|---------|
| `now()` | `DateTime.Now` | Current date and time |
| `today()` | `DateTime.Today` | Current date at midnight |

Date functions respect `CsEvalOptions.IsCaseSensitive`. When case-insensitive, `Now()` and `TODAY()` also work.

## Date Arithmetic Sugar

Numeric values gain TimeSpan-creating member access in Extended mode:

| Sugar | Singular | Result |
|-------|----------|--------|
| `5.days` | `5.day` | `TimeSpan.FromDays(5)` |
| `2.hours` | `2.hour` | `TimeSpan.FromHours(2)` |
| `30.minutes` | `30.minute` | `TimeSpan.FromMinutes(30)` |
| `10.seconds` | `10.second` | `TimeSpan.FromSeconds(10)` |
| `500.milliseconds` | `500.millisecond` | `TimeSpan.FromMilliseconds(500)` |
| `1.weeks` | `1.week` | `TimeSpan.FromDays(7)` |

Works on any numeric type (int, long, float, double). The sugar is resolved via member access on the numeric value, so variables work too:

```csharp
engine.Evaluate("5.days");
// output: 5.00:00:00

engine.Evaluate("now() + 5.days");
// output: (DateTime five days from now)

engine.Evaluate("var n = 2; n.hours");
// output: 02:00:00
```

Date sugar member names are always case-sensitive (`StringComparer.Ordinal`).

## Aggregate Builtins

Collection operations available as bare functions:

| Function | Returns | Notes |
|----------|---------|-------|
| `sum(collection)` | Type-preserving sum | `int[]` input returns `int`; mixed types promote (int+double = double) |
| `avg(collection)` | `double` | Always returns double |
| `count(collection)` | `int` | Uses `ICollection.Count` when available |
| `min(collection)` | Minimum element | 1-arg overload is aggregate; 2-arg is `Math.Min` |
| `max(collection)` | Maximum element | 1-arg overload is aggregate; 2-arg is `Math.Max` |

```csharp
engine.Evaluate("sum(new[] {1, 2, 3})");
// output: 6

engine.Evaluate("avg(new[] {1.0, 2.0, 3.0})");
// output: 2.0

engine.Evaluate("count(new[] {1, 2, 3})");
// output: 3

engine.Evaluate("min(new[] {3, 1, 2})");
// output: 1

engine.Evaluate("max(new[] {3, 1, 2})");
// output: 3
```

Aggregate builtins respect `CsEvalOptions.IsCaseSensitive`. When case-insensitive, `SUM`, `AVG`, etc. also work.

:::note[min/max disambiguation]
With **1 argument**, `min` and `max` are aggregate builtins that find the minimum/maximum element in a collection. With **2 arguments**, they are `Math.Min`/`Math.Max` comparisons.
:::

## The `it` Placeholder

In Extended mode, the identifier `it` inside function call arguments is automatically lowered to a lambda parameter.

```csharp
engine.Evaluate("new[] {1, 2, 3, 4, 5}.Where(it > 3).ToArray()");
// output: int[] {4, 5}

engine.Evaluate("new[] {1, 2, 3}.Select(it * 10).ToArray()");
// output: int[] {10, 20, 30}

engine.Evaluate("new[] {1, 2, 3}.Any(it > 2)");
// output: True

engine.Evaluate("new[] {1, 2, 3}.All(it > 0)");
// output: True
```

`it > 3` is lowered to `it => it > 3` at parse time. This does **not** apply if the argument is already a lambda expression -- explicit lambdas are left as-is.

:::note
`it` placeholder requires `AllowMethodCalls` in the sandbox options, since it is used in method call arguments like `.Where()` and `.Select()`.
:::

## Resolution Order and Shadowing

When Extended mode encounters a bare identifier used as a function call, it resolves in this order:

1. **Registered functions** (`RegisterFunction`)
2. **User variables** (checked but skipped for call resolution if not callable)
3. **Bare math functions** (`sin`, `cos`, `sqrt`, etc.)
4. **Clock functions** (`now`, `today`)
5. **Aggregate builtins** (`sum`, `avg`, `count`, `min`, `max`)
6. **Modules**, then **type resolution**

For non-call identifiers (plain name resolution):

1. **Registered functions** (wrapped as `FunctionRef`)
2. **Modules**
3. **User variables**
4. **Type resolution**
5. **Namespace prefixes**
6. **Bare math constants** (`pi`, `e`, `tau`, `infinity`, `nan`)

User-defined names always win over built-ins:

```csharp
engine.Evaluate("var pi = 3; pi");
// output: 3   (not Math.PI)
```

## Case Sensitivity Summary

| Category | Case behavior |
|----------|---------------|
| Math functions (`sin`, `cos`, etc.) | Always case-sensitive |
| Math constants (`pi`, `e`, etc.) | Always case-sensitive |
| Date functions (`now`, `today`) | Respects `IsCaseSensitive` option |
| Date sugar (`.days`, `.hours`, etc.) | Always case-sensitive |
| Aggregate builtins (`sum`, `avg`, etc.) | Respects `IsCaseSensitive` option |
