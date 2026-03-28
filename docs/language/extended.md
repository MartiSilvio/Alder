---
title: "Extended Mode"
description: "Extended operators, collection features, bare math, aggregates, date/time sugar"
sidebar:
  order: 3
---

Extended mode is a strict superset of Standard C#. Every valid Standard expression works in Extended mode. Extended mode adds operators, control flow sugar, collection features, bare math functions, aggregate built-ins, and date/time arithmetic — all designed to make expressions more concise without sacrificing type safety.

```csharp
var engine = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);
```

For Standard mode C# features, see [Standard Mode](standard.md).

## Operators

### Power

`**` raises the left operand to the power of the right. Both operands are converted to `double`. Right-associative: `2 ** 3 ** 2` evaluates as `2 ** (3 ** 2)` = 512.

```csharp
2 ** 10             // 1024.0 (double)
var x = 3; x **= 2; return x;   // 9.0
```

<!-- test: ExtRef_Op_Power.csx -->

Nullable: `null ** x` and `x ** null` return `null`.

### Pipeline

`|>` passes the left value as the single argument to the right operand. The right operand must be callable: a lambda, registered function, module method, delegate, or method reference.

```csharp
5 |> (x => x * 2)    // 10
```

<!-- test: ExtRef_Op_Pipeline.csx -->

Arithmetic binds tighter than pipe: `2 + 3 |> (x => x * 2)` evaluates as `(2 + 3) |> (x => x * 2)` = 10.

The binder optimizes pipeline expressions: when the right operand is an identifier or a call expression, the pipeline is desugared into a direct call at bind time rather than going through runtime lambda invocation.

### Spaceship

`<=>` is three-way comparison. Returns `-1` if `a < b`, `0` if equal, `1` if `a > b`. `null` is ordered before any non-null value.

```csharp
1 <=> 2             // -1
"b" <=> "a"         // 1
null <=> 0          // -1
```

<!-- test: ExtRef_Op_Spaceship.csx -->

### Regex Match

`=~` tests whether the left string matches the right regex pattern. `!~` tests the inverse. Left operand is converted to string via `.ToString()`. Regex timeout is 1 second (enforced via `SecurityPolicy.RegexTimeout`).

```csharp
"hello123" =~ @"\d+"     // true
"hello" !~ @"\d+"        // true
```

<!-- test: ExtRef_Op_Regex.csx -->

### Strict Equality

`===` and `!==` compare without numeric widening. Types must match exactly.

```csharp
1 === 1       // true
1 === 1L      // false (int vs long)
1 === 1.0     // false (int vs double)
```

<!-- test: ExtRef_Op_StrictEquality.csx -->

### Membership

`in` tests whether a value exists in a collection. `not in` tests the inverse. `not in` desugars to `!(x in collection)` at parse time.

```csharp
3 in new[] { 1, 2, 3, 4 }        // true
5 not in new[] { 1, 2, 3, 4 }    // true
```

<!-- test: ExtRef_Op_Membership.csx -->

### Like (SQL-style)

`like` matches strings using SQL wildcard patterns: `%` matches any characters, `_` matches a single character. `not like` desugars to `!(x like pattern)` at parse time.

```csharp
"hello" like "hel%"      // true
"hello" like "h_llo"     // true
"hello" not like "xyz%"  // true
```

<!-- test: ExtRef_Op_Like.csx -->

### Between

`x between low and high` desugars to `x >= low && x <= high` at parse time.

```csharp
5 between 1 and 10    // true
15 between 1 and 10   // false
```

<!-- test: ExtRef_Op_Between.csx -->

### Chained Comparisons

Multiple comparison operators chain naturally. Each middle operand is evaluated exactly once. Short-circuits on first `false`. Chainable operators: `<`, `<=`, `>`, `>=`, `==`, `!=`.

```csharp
var x = 50;
return 0 <= x <= 100;     // true — same as 0 <= x && x <= 100
```

<!-- test: ExtRef_Op_ChainedComparison.csx -->

### Word Operators

`and`, `or`, `not` are contextual keywords that map to `&&`, `||`, `!` respectively. Same precedence and short-circuit behavior. These are only available in Extended mode — in Standard mode, they are parsed as identifiers.

```csharp
true and false    // false
true or false     // true
not true          // false
```

<!-- test: ExtRef_Op_WordOps.csx -->

### String Repetition

`string * count` or `count * string` repeats a string. Count must be a non-negative integer. Count of 0 returns `""`.

```csharp
"ab" * 3    // "ababab"
3 * "ab"    // "ababab"
```

<!-- test: ExtRef_Op_StringRepeat.csx -->

### Inclusive Range

Standard `..` produces a `System.Range` with an exclusive end. Extended adds `..=` (inclusive) and `..<` (explicit exclusive) which produce `IEnumerable<int>` sequences.

```csharp
// 1..=5  → IEnumerable<int> { 1, 2, 3, 4, 5 }
// 1..<5  → IEnumerable<int> { 1, 2, 3, 4 }
```

<!-- test: ExtRef_Op_InclusiveRange.csx -->

## Expressions

### If-Expression

When `if` is not followed by `{`, it's parsed as an expression — both branches required, no braces. The parser disambiguates based on the token following the condition: `{` triggers the statement form.

```csharp
var x = 5;
return if (x > 0) "positive" else "non-positive";
```

<!-- test: ExtRef_Expr_IfExpr.csx -->

### Let-In

`let` binds a scoped variable for a single expression. Desugars to a `BlockExpr` with variable declarations. Variables are scoped to the body — they don't leak.

```csharp
let price = 100m in let tax = price * 0.1m in price + tax    // 110.0m
```

<!-- test: ExtRef_Expr_LetIn.csx -->

Destructuring form:

```csharp
let { Name, Age } = new { Name = "Ada", Age = 20 } in Name + ":" + Age    // "Ada:20"
```

<!-- test: ExtRef_Expr_LetInDestructure.csx -->

### Implicit Iterator (`it`)

In LINQ method arguments, bare `it` auto-wraps into a lambda. This is handled at parse time in `ExpressionParser.CallArguments` — when the argument is not already a lambda and contains the identifier `it`, the parser wraps it in `it => (expression)`.

```csharp
new[] { 1, 2, 3, 4, 5 }.Where(it > 2).Select(it * 10).ToArray()
// [30, 40, 50] — same as .Where(it => it > 2).Select(it => it * 10)
```

<!-- test: ExtRef_Expr_ImplicitIt.csx -->

## Collection Features

### Array Literals

`[elements]` creates an array. Standard mode requires `new[] { 1, 2, 3 }`. Using `[...]` in Standard mode produces an `ExtendedModeRequired` diagnostic.

```csharp
[1, 2, 3]       // int[]
[]               // empty array
```

<!-- test: ExtRef_Collection_ArrayLiteral.csx -->

### Spread

`..` in array and object literals spreads a collection's elements.

```csharp
var a = new[] { 2, 3 };
return [1, ..a, 4];    // [1, 2, 3, 4]
```

<!-- test: ExtRef_Collection_Spread.csx -->

### Object Spread

```csharp
var obj = new { Name = "Alice", Age = 30 };
return new { ..obj, Age = 31 };    // { Name = "Alice", Age = 31 }
```

<!-- test: ExtRef_Collection_ObjectSpread.csx -->

### Comprehensions

Array comprehensions desugar to LINQ at parse time. `[expr for x in source]` becomes `source.Select(x => expr).ToArray()`. Add `if` for filtering.

```csharp
[x * x for x in Enumerable.Range(1, 10) if x % 2 == 0]    // [4, 16, 36, 64, 100]
```

<!-- test: ExtRef_Collection_Comprehension.csx -->

### Object Merge

`+` on two objects merges their properties into a `Dictionary<string, object?>`. Right-side properties override left-side on key collision. Implemented in `ObjectMergeOperator`.

```csharp
new { A = 1 } + new { B = 2 }    // { A: 1, B: 2 }
```

<!-- test: ExtRef_Collection_ObjectMerge.csx -->

### Slicing

Python-style slice syntax on arrays and strings. `[start:end]` is exclusive end. `[start:end:step]` adds a step. Implemented in `SliceExpr` → `BoundSliceExpr`.

```csharp
new[] { 10, 20, 30, 40, 50 }[1:4]    // [20, 30, 40]
"hello"[1:4]                           // "ell"
```

<!-- test: ExtRef_Collection_Slice.csx -->

## Bare Math Functions

Available without the `Math.` prefix. Resolved in `BareMathNames` at runtime. User variables with the same name shadow these built-in names.

### Constants

| Name | Value |
|------|-------|
| `pi` | `Math.PI` (3.14159...) |
| `e` | `Math.E` (2.71828...) |
| `tau` | `2 * Math.PI` (6.28318...) |
| `infinity` | `double.PositiveInfinity` |
| `nan` | `double.NaN` |

### Functions

| Function | Maps to | Notes |
|----------|---------|-------|
| `sin`, `cos`, `tan` | `Math.Sin/Cos/Tan` | → `double` |
| `asin`, `acos`, `atan` | `Math.Asin/Acos/Atan` | → `double` |
| `sinh`, `cosh`, `tanh` | `Math.Sinh/Cosh/Tanh` | → `double` |
| `abs(x)` | `Math.Abs` | Preserves `int`/`long`/`float`/`double`/`decimal` |
| `sqrt(x)`, `cbrt(x)` | `Math.Sqrt/Cbrt` | → `double` |
| `log(x)`, `ln(x)` | `Math.Log` | Natural log → `double` |
| `log2(x)` | `Math.Log(x, 2)` | → `double` |
| `log10(x)` | `Math.Log10` | → `double` |
| `exp(x)` | `Math.Exp` | → `double` |
| `floor(x)`, `ceil(x)` | `Math.Floor/Ceiling` | Preserves `decimal`, otherwise `double` |
| `round(x)`, `round(x, digits)` | `Math.Round` | Preserves `decimal`, otherwise `double` |
| `truncate(x)` | `Math.Truncate` | Preserves `decimal`, otherwise `double` |
| `sign(x)` | `Math.Sign` | → `int` (-1, 0, 1) |
| `atan2(y, x)` | `Math.Atan2` | → `double` |
| `min(a, b)`, `max(a, b)` | `Math.Min/Max` | Preserves type |
| `pow(x, y)` | `Math.Pow` | → `double` |
| `clamp(value, min, max)` | `Math.Min(Math.Max(...))` | Preserves type |

<!-- test: ExtRef_Math_Functions.csx -->

## Aggregate Built-in Functions

Operate on collections. Implemented in `AggregateBuiltins`, which delegates to the corresponding LINQ `Enumerable` methods with type-specific dispatch.

```csharp
sum(new[] { 1, 2, 3, 4 })        // 10
avg(new[] { 2, 4, 6, 8 })        // 5.0
count(new[] { 10, 20, 30 })      // 3
min(new[] { 5, 3, 8, 1, 4 })     // 1
max(new[] { 5, 3, 8, 1, 4 })     // 8
```

<!-- test: ExtRef_Aggregate.csx -->

`min` and `max` with a single collection argument are aggregates. With two scalar arguments (`min(a, b)`, `max(a, b)`), they are bare math functions that delegate to `Math.Min`/`Math.Max`.

## Date/Time Sugar

### Clock Functions

Implemented in `DateArithmeticSugar`. These are zero-argument functions resolved at runtime.

| Function | Returns |
|----------|---------|
| `now()` | `DateTime.Now` |
| `today()` | `DateTime.Today` |

### TimeSpan Unit Properties

Applied to numeric values via member access. Resolved in `DateArithmeticSugar.TryResolveTimeSpanUnit` when the target is a numeric type and the member name matches a unit. Singular and plural forms both work.

| Syntax | Equivalent |
|--------|------------|
| `n.day` / `n.days` | `TimeSpan.FromDays(n)` |
| `n.hour` / `n.hours` | `TimeSpan.FromHours(n)` |
| `n.minute` / `n.minutes` | `TimeSpan.FromMinutes(n)` |
| `n.second` / `n.seconds` | `TimeSpan.FromSeconds(n)` |
| `n.millisecond` / `n.milliseconds` | `TimeSpan.FromMilliseconds(n)` |
| `n.week` / `n.weeks` | `TimeSpan.FromDays(n * 7)` |

```csharp
new DateTime(2026, 1, 1) + 30.days    // 2026-01-31
```

<!-- test: ExtRef_DateTime.csx -->

User variables shadow unit names — if you define `var days = 5;`, then `days` refers to your variable.

## Control Flow

### Unless

`unless (cond) { body }` desugars to `if (!cond) { body }` at parse time in `StatementParser`.

```csharp
var x = 5;
var result = "default";
unless (x > 10) { result = "small"; }
return result;    // "small"
```

<!-- test: ExtRef_Stmt_Unless.csx -->

### Until

`until (cond) { body }` desugars to `while (!cond) { body }` at parse time in `StatementParser`.

```csharp
var i = 0;
until (i >= 3) { i++; }
return i;    // 3
```

<!-- test: ExtRef_Stmt_Until.csx -->
