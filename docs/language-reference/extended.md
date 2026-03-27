# Language Reference — Extended Mode

Extended mode is a strict superset of Standard C#. Every valid Standard expression also works in Extended mode. Extended mode adds operators, control flow sugar, collection features, bare math functions, aggregate built-ins, and date/time arithmetic.

Enable it:
```csharp
var engine = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);
```

For Standard mode C# features, see [Standard Mode](standard.md).

## Operators

### Power Operator

| Operator | Syntax | Result type | Associativity |
|----------|--------|-------------|---------------|
| `**` | `2 ** 10` | `double` | Right |
| `**=` | `x **= 2` | `double` | Right |

<!-- test: ExtRef_Op_Power.csx -->

Both operands are converted to `double` via `Convert.ToDouble`. Nullable: `null ** x` and `x ** null` return `null`.

### Pipeline Operator

| Operator | Syntax | Description |
|----------|--------|-------------|
| `\|>` | `value \|> func` | Passes left as single argument to right |

```csharp
5 |> (x => x * 2)    // 10
```
<!-- test: ExtRef_Op_Pipeline.csx -->

The right operand must be a callable: lambda, registered function, module method, delegate, or method reference. Arithmetic binds tighter than pipe: `2 + 3 |> (x => x * 2)` evaluates as `(2 + 3) |> (x => x * 2)`.

### Spaceship Operator

| Operator | Syntax | Returns |
|----------|--------|---------|
| `<=>` | `a <=> b` | `-1`, `0`, or `1` (`int`) |

<!-- test: ExtRef_Op_Spaceship.csx -->

Three-way comparison. Returns `-1` if `a < b`, `0` if equal, `1` if `a > b`. Null is ordered before any non-null value.

### Regex Match Operators

| Operator | Syntax | Returns |
|----------|--------|---------|
| `=~` | `str =~ pattern` | `true` if matches |
| `!~` | `str !~ pattern` | `true` if does NOT match |

```csharp
"hello123" =~ @"\d+"     // true
"hello" !~ @"\d+"        // true
```
<!-- test: ExtRef_Op_Regex.csx -->

Left operand is converted to string via `.ToString()`. Right must be a string regex pattern. Regex timeout is 1 second.

### Strict Equality Operators

| Operator | Syntax | Description |
|----------|--------|-------------|
| `===` | `a === b` | Type-exact equality (no numeric widening) |
| `!==` | `a !== b` | Type-exact inequality |

```csharp
1 === 1       // true
1 === 1L      // false (int vs long)
1 === 1.0     // false (int vs double)
```
<!-- test: ExtRef_Op_StrictEquality.csx -->

### Membership Operators

| Operator | Syntax | Description |
|----------|--------|-------------|
| `in` | `x in collection` | True if collection contains x |
| `not in` | `x not in collection` | True if collection does NOT contain x |

```csharp
3 in new[] { 1, 2, 3, 4 }        // true
5 not in new[] { 1, 2, 3, 4 }    // true
```
<!-- test: ExtRef_Op_Membership.csx -->

### Like Operator (SQL-style)

| Operator | Syntax | Wildcards |
|----------|--------|-----------|
| `like` | `str like pattern` | `%` = any chars, `_` = single char |
| `not like` | `str not like pattern` | |

```csharp
"hello" like "hel%"      // true
"hello" like "h_llo"     // true
"hello" not like "xyz%"  // true
```
<!-- test: ExtRef_Op_Like.csx -->

### Between Operator

| Syntax | Desugars to |
|--------|-------------|
| `x between low and high` | `x >= low && x <= high` |

```csharp
5 between 1 and 10    // true
15 between 1 and 10   // false
```
<!-- test: ExtRef_Op_Between.csx -->

### Chained Comparisons

| Syntax | Desugars to |
|--------|-------------|
| `a < b < c` | `a < b && b < c` |
| `0 <= x <= 100` | `0 <= x && x <= 100` |

<!-- test: ExtRef_Op_ChainedComparison.csx -->

Each middle operand evaluated exactly once. Short-circuits on first false. Chainable operators: `<`, `<=`, `>`, `>=`, `==`, `!=`.

### Word Operators

| Word | Equivalent | Precedence |
|------|------------|------------|
| `and` | `&&` | Logical AND |
| `or` | `\|\|` | Logical OR |
| `not` | `!` | Unary NOT |

```csharp
true and false    // false
true or false     // true
not true          // false
```
<!-- test: ExtRef_Op_WordOps.csx -->

### String Repetition

| Syntax | Example | Result |
|--------|---------|--------|
| `str * count` | `"ab" * 3` | `"ababab"` |
| `count * str` | `3 * "ab"` | `"ababab"` |

<!-- test: ExtRef_Op_StringRepeat.csx -->

Count must be a non-negative integer. Count of 0 returns `""`.

### Inclusive Range

| Syntax | Description |
|--------|-------------|
| `start..=end` | Inclusive range (both endpoints included) |
| `start..<end` | Exclusive range (explicit) |

```csharp
1..=5    // 1, 2, 3, 4, 5
1..<5    // 1, 2, 3, 4
```
<!-- test: ExtRef_Op_InclusiveRange.csx -->

Standard `..` is exclusive end. Extended adds `..=` (inclusive) and `..<` (explicit exclusive).

## Expressions

### If-Expression

| Syntax | Desugars to |
|--------|-------------|
| `if (cond) thenExpr else elseExpr` | `cond ? thenExpr : elseExpr` |

```csharp
if (x > 0) "positive" else "non-positive"
```
<!-- test: ExtRef_Expr_IfExpr.csx -->

Only available when `if` is NOT followed by `{` — that triggers the statement form. Both branches are required.

### Let-In Expression

| Syntax | Description |
|--------|-------------|
| `let x = init in body` | Scoped variable binding |
| `let { a, b } = init in body` | Destructuring binding |

```csharp
let price = 100m in let tax = price * 0.1m in price + tax    // 110m
```
<!-- test: ExtRef_Expr_LetIn.csx -->

```csharp
let { Name, Age } = new { Name = "Ada", Age = 20 } in Name + ":" + Age    // "Ada:20"
```
<!-- test: ExtRef_Expr_LetInDestructure.csx -->

Desugars to a `BlockExpr` with variable declarations. Variables are scoped to the body expression.

### Implicit Iterator (`it`)

In Extended mode, bare `it` in LINQ method arguments auto-wraps into a lambda:

| Written | Desugars to |
|---------|-------------|
| `items.Where(it > 2)` | `items.Where(it => it > 2)` |
| `items.Select(it * 10)` | `items.Select(it => it * 10)` |

<!-- test: ExtRef_Expr_ImplicitIt.csx -->

## Collection Features

### Array Literals

| Syntax | Example | Notes |
|--------|---------|-------|
| `[elements]` | `[1, 2, 3]` | Creates an array |
| `[]` | `[]` | Empty array |

<!-- test: ExtRef_Collection_ArrayLiteral.csx -->

Standard mode requires `new[] { 1, 2, 3 }`. Extended mode adds the `[1, 2, 3]` shorthand.

### Spread Operator

| Syntax | Example |
|--------|---------|
| `[..collection]` | `[..existing]` |
| `[a, ..b, c]` | `[1, ..middle, 5]` |

```csharp
var a = new[] { 2, 3 };
[1, ..a, 4]    // [1, 2, 3, 4]
```
<!-- test: ExtRef_Collection_Spread.csx -->

### Object Spread

| Syntax | Example |
|--------|---------|
| `new { ..obj }` | Copy all properties |
| `new { ..obj, Extra = 1 }` | Copy + add properties |
| `new { ..obj1, ..obj2 }` | Merge (right overrides left) |

<!-- test: ExtRef_Collection_ObjectSpread.csx -->

### Comprehensions

| Syntax | Desugars to |
|--------|-------------|
| `[expr for x in source]` | `source.Select(x => expr).ToArray()` |
| `[expr for x in source if cond]` | `source.Where(x => cond).Select(x => expr).ToArray()` |

```csharp
[x * x for x in 1..=10 if x % 2 == 0]    // [4, 16, 36, 64, 100]
```
<!-- test: ExtRef_Collection_Comprehension.csx -->

### Object Merge with `+`

| Syntax | Description |
|--------|-------------|
| `obj1 + obj2` | Merge all properties into dictionary |

```csharp
new { A = 1 } + new { B = 2 }    // { A: 1, B: 2 }
```
<!-- test: ExtRef_Collection_ObjectMerge.csx -->

Returns `Dictionary<string, object?>`. Right-side properties override left-side on key collision.

### Slicing

| Syntax | Description |
|--------|-------------|
| `arr[start:end]` | Slice from start to end (exclusive) |
| `arr[:end]` | Slice from beginning |
| `arr[start:]` | Slice to end |
| `arr[start:end:step]` | Slice with step |

```csharp
new[] { 10, 20, 30, 40, 50 }[1:4]    // [20, 30, 40]
"hello"[1:4]                           // "ell"
```
<!-- test: ExtRef_Collection_Slice.csx -->

Works with arrays and strings.

## Bare Math Functions

Available without `Math.` prefix. User variables with the same name shadow these.

### Constants

| Name | Value |
|------|-------|
| `pi` | `Math.PI` (3.14159...) |
| `e` | `Math.E` (2.71828...) |
| `tau` | `2 * Math.PI` (6.28318...) |
| `infinity` | `double.PositiveInfinity` |
| `nan` | `double.NaN` |

<!-- test: ExtRef_Math_Constants.csx -->

### Functions (1 argument)

| Function | Maps to | Returns |
|----------|---------|---------|
| `sin(x)` | `Math.Sin` | `double` |
| `cos(x)` | `Math.Cos` | `double` |
| `tan(x)` | `Math.Tan` | `double` |
| `asin(x)` | `Math.Asin` | `double` |
| `acos(x)` | `Math.Acos` | `double` |
| `atan(x)` | `Math.Atan` | `double` |
| `sinh(x)` | `Math.Sinh` | `double` |
| `cosh(x)` | `Math.Cosh` | `double` |
| `tanh(x)` | `Math.Tanh` | `double` |
| `abs(x)` | `Math.Abs` | Preserves: `int`, `long`, `float`, `double`, `decimal` |
| `sqrt(x)` | `Math.Sqrt` | `double` |
| `cbrt(x)` | `Math.Cbrt` | `double` |
| `log(x)` | `Math.Log` | `double` (natural log) |
| `log2(x)` | `Math.Log(x, 2)` | `double` |
| `log10(x)` | `Math.Log10` | `double` |
| `ln(x)` | `Math.Log` | `double` (alias for `log`) |
| `exp(x)` | `Math.Exp` | `double` |
| `floor(x)` | `Math.Floor` | Preserves `decimal`; otherwise `double` |
| `ceil(x)` | `Math.Ceiling` | Preserves `decimal`; otherwise `double` |
| `round(x)` | `Math.Round` | Preserves `decimal`; otherwise `double` |
| `truncate(x)` | `Math.Truncate` | Preserves `decimal`; otherwise `double` |
| `sign(x)` | `Math.Sign` | `int` (-1, 0, 1) |

### Functions (2 arguments)

| Function | Maps to | Returns |
|----------|---------|---------|
| `round(x, digits)` | `Math.Round(x, digits)` | Preserves `decimal`; otherwise `double` |
| `log(x, base)` | `Math.Log(x, base)` | `double` |
| `atan2(y, x)` | `Math.Atan2` | `double` |
| `min(a, b)` | `Math.Min` | Preserves: `int`, `long`, `float`, `double`, `decimal` |
| `max(a, b)` | `Math.Max` | Preserves: `int`, `long`, `float`, `double`, `decimal` |
| `pow(x, y)` | `Math.Pow` | `double` |

### Functions (3 arguments)

| Function | Description | Returns |
|----------|-------------|---------|
| `clamp(value, min, max)` | Clamp value to range | Preserves: `int`, `long`, `float`, `double`, `decimal` |

<!-- test: ExtRef_Math_Functions.csx -->

## Aggregate Built-in Functions

Operate on collections. Delegate to LINQ `Enumerable` methods.

| Function | Accepted types | Returns |
|----------|---------------|---------|
| `sum(collection)` | `IEnumerable<int\|long\|float\|double\|decimal>` and nullable variants | Same numeric type |
| `avg(collection)` | Same as `sum` | `double` or `decimal` |
| `count(collection)` | Any `IEnumerable` | `int` |
| `min(collection)` | Same as `sum`, plus `IEnumerable<string>` | Same type |
| `max(collection)` | Same as `min` | Same type |

```csharp
sum(new[] { 1, 2, 3, 4 })        // 10
avg(new[] { 2, 4, 6, 8 })        // 5.0
count(new[] { 10, 20, 30 })      // 3
min(new[] { 5, 3, 8, 1, 4 })     // 1
max(new[] { 5, 3, 8, 1, 4 })     // 8
```
<!-- test: ExtRef_Aggregate.csx -->

Note: `min` and `max` with a single collection argument are aggregate functions. With two arguments (`min(a, b)`, `max(a, b)`), they are bare math functions that return the smaller/larger of two values.

## Date/Time Sugar

### Clock Functions

| Function | Returns |
|----------|---------|
| `now()` | `DateTime.Now` |
| `today()` | `DateTime.Today` |

### TimeSpan Unit Properties

Applied to numeric values via member access:

| Syntax | Equivalent |
|--------|------------|
| `n.day`, `n.days` | `TimeSpan.FromDays(n)` |
| `n.hour`, `n.hours` | `TimeSpan.FromHours(n)` |
| `n.minute`, `n.minutes` | `TimeSpan.FromMinutes(n)` |
| `n.second`, `n.seconds` | `TimeSpan.FromSeconds(n)` |
| `n.millisecond`, `n.milliseconds` | `TimeSpan.FromMilliseconds(n)` |
| `n.week`, `n.weeks` | `TimeSpan.FromDays(n * 7)` |

```csharp
new DateTime(2026, 1, 1) + 30.days    // 2026-01-31
now() - 1.hours                         // one hour ago
```
<!-- test: ExtRef_DateTime.csx -->

User variables shadow these unit names — if you define `var days = 5;`, then `days` refers to your variable, not the TimeSpan accessor.

## Control Flow

### Unless Statement

| Syntax | Desugars to |
|--------|-------------|
| `unless (cond) { body }` | `if (!cond) { body }` |
| `unless (cond) { body } else { alt }` | `if (!cond) { body } else { alt }` |

<!-- test: ExtRef_Stmt_Unless.csx -->

### Until Statement

| Syntax | Desugars to |
|--------|-------------|
| `until (cond) { body }` | `while (!cond) { body }` |

<!-- test: ExtRef_Stmt_Until.csx -->
