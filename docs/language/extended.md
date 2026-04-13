Extended mode is a strict superset of Standard C#. Every valid Standard expression works unchanged, produces the same result. Extended adds operators, collection features, control flow sugar, bare math functions, aggregate built-ins, and date/time arithmetic. Extended features are syntactic sugar over .NET APIs: `sum()` calls `Enumerable.Sum()`, `sin()` calls `Math.Sin()`, comprehensions desugar to LINQ. Nothing is reimplemented.

```csharp
var engine = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);
```

Using Extended features in Standard mode throws `ALDR0020`.

For Standard C# features, see [Standard Mode](standard.md).

## Operators

### Power (`**`)

Both operands convert to `double`. Right-associative: `2 ** 3 ** 2` = `2 ** 9` = 512.

```csharp
2 ** 10             // 1024.0
var x = 3; x **= 2; return x;   // 9.0
```

<!-- test: ExtRef_Op_Power.csx -->

`null ** x` and `x ** null` return `null`.

### Pipeline (`|>`)

Passes the left value as the argument to the right callable (lambda, function, delegate, module method, method reference).

```csharp
5 |> (x => x * 2)    // 10
```

<!-- test: ExtRef_Op_Pipeline.csx -->

Arithmetic binds tighter: `2 + 3 |> (x => x * 2)` = `5 |> (x => x * 2)` = 10.

The binder desugars identifier and call pipelines into direct calls at bind time.

### Spaceship (`<=>`)

Three-way comparison. Returns `-1`, `0`, or `1`. `null` orders before any non-null value.

```csharp
1 <=> 2             // -1
"b" <=> "a"         // 1
null <=> 0          // -1
```

<!-- test: ExtRef_Op_Spaceship.csx -->

### Regex Match (`=~`, `!~`)

`=~` tests regex match. `!~` tests inverse. Left operand converted to string via `.ToString()`. Regex timeout: 1 second.

```csharp
"hello123" =~ @"\d+"     // true
"hello" !~ @"\d+"        // true
```

<!-- test: ExtRef_Op_Regex.csx -->

### Strict Equality (`===`, `!==`)

No numeric widening. Types must match exactly.

```csharp
1 === 1       // true
1 === 1L      // false (int vs long)
1 === 1.0     // false (int vs double)
```

<!-- test: ExtRef_Op_StrictEquality.csx -->

### Membership (`in`, `not in`)

```csharp
3 in new[] { 1, 2, 3, 4 }        // true
5 not in new[] { 1, 2, 3, 4 }    // true
```

<!-- test: ExtRef_Op_Membership.csx -->

`not in` desugars to `!(x in collection)` at parse time.

### Like (`like`, `not like`)

SQL wildcard patterns: `%` matches any characters, `_` matches one character.

```csharp
"hello" like "hel%"      // true
"hello" like "h_llo"     // true
"hello" not like "xyz%"  // true
```

<!-- test: ExtRef_Op_Like.csx -->

The matcher classifies patterns into modes (exact, prefix, suffix, contains, general) for performance.

### Between

`x between low and high` desugars to `x >= low && x <= high` at parse time.

```csharp
5 between 1 and 10    // true
15 between 1 and 10   // false
```

<!-- test: ExtRef_Op_Between.csx -->

### Chained Comparisons

Multiple comparisons chain naturally. Each middle operand evaluated exactly once. Short-circuits on first `false`.

```csharp
var x = 50;
return 0 <= x <= 100;     // true (equivalent to 0 <= x && x <= 100)
```

<!-- test: ExtRef_Op_ChainedComparison.csx -->

Chainable: `<`, `<=`, `>`, `>=`, `==`, `!=`.

### Word Operators (`and`, `or`, `not`)

Contextual keywords mapping to `&&`, `||`, `!`. Same precedence, same short-circuit behavior.

```csharp
true and false    // false
true or false     // true
not true          // false
```

<!-- test: ExtRef_Op_WordOps.csx -->

In Standard mode, `and`, `or`, `not` are parsed as identifiers.

### Inclusive Range (`..=`, `..<`)

Standard `..` produces `System.Range` (exclusive end). Extended adds `..=` (inclusive) and `..<` (explicit exclusive) producing `IEnumerable<int>` sequences.

```csharp
// 1..=5 produces { 1, 2, 3, 4, 5 }
// 1..<5 produces { 1, 2, 3, 4 }
```

<!-- test: ExtRef_Op_InclusiveRange.csx -->

## Expressions

### If-Expression

When `if` is followed by a condition without `{`, it is parsed as an expression. Both branches required.

```csharp
var x = 5;
return if (x > 0) "positive" else "non-positive";
```

<!-- test: ExtRef_Expr_IfExpr.csx -->

The parser disambiguates: `{` after the condition triggers statement form.

### Let-In

Scoped variable binding for a single expression. Variables don't leak.

```csharp
let price = 100m in let tax = price * 0.1m in price + tax    // 110.0m
```

<!-- test: ExtRef_Expr_LetIn.csx -->

Destructuring form:

```csharp
let { Name, Age } = new { Name = "Ada", Age = 20 } in Name + ":" + Age    // "Ada:20"
```

<!-- test: ExtRef_Expr_LetInDestructure.csx -->

Desugars to a block with variable declarations at parse time.

## Collection Features

### Array Literals

```csharp
[1, 2, 3]       // int[]
[]               // empty array
```

<!-- test: ExtRef_Collection_ArrayLiteral.csx -->

Standard mode requires `new[] { 1, 2, 3 }`.

### Spread

`..` inside array and object literals spreads elements:

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

Right-side properties override left-side on key collision.

### Comprehensions

Desugar to LINQ at parse time. `[expr for x in source]` becomes `source.Select(x => expr).ToArray()`. Add `if` for filtering.

```csharp
[x * x for x in Enumerable.Range(1, 10) if x % 2 == 0]    // [4, 16, 36, 64, 100]
```

<!-- test: ExtRef_Collection_Comprehension.csx -->

### Object Merge (`+`)

Merges properties of two objects into `Dictionary<string, object?>`. Right-side overrides on collision.

```csharp
new { A = 1 } + new { B = 2 }    // { A: 1, B: 2 }
```

<!-- test: ExtRef_Collection_ObjectMerge.csx -->

### Slicing

Python-style `[start:end]` (exclusive end) and `[start:end:step]` on arrays and strings.

```csharp
new[] { 10, 20, 30, 40, 50 }[1:4]       // [20, 30, 40]
"hello"[1:4]                              // "ell"
new[] { 0, 1, 2, 3, 4, 5 }[0:6:2]       // [0, 2, 4]
```

<!-- test: ExtRef_Collection_Slice.csx -->

Negative indices, omitted start/end, and negative steps are supported.

## Bare Math Functions

Available without `Math.` prefix. User variables with the same name take priority.

### Constants

| Name | Value |
|------|-------|
| `pi` | `Math.PI` |
| `e` | `Math.E` |
| `tau` | `2 * Math.PI` |
| `infinity` | `double.PositiveInfinity` |
| `nan` | `double.NaN` |

### Functions

| Function | Maps to | Return type |
|----------|---------|-------------|
| `sin`, `cos`, `tan` | `Math.Sin/Cos/Tan` | `double` |
| `asin`, `acos`, `atan` | `Math.Asin/Acos/Atan` | `double` |
| `sinh`, `cosh`, `tanh` | `Math.Sinh/Cosh/Tanh` | `double` |
| `abs(x)` | `Math.Abs` | preserves input type |
| `sqrt(x)`, `cbrt(x)` | `Math.Sqrt/Cbrt` | `double` |
| `log(x)`, `ln(x)` | `Math.Log` | `double` |
| `log2(x)` | `Math.Log(x, 2)` | `double` |
| `log10(x)` | `Math.Log10` | `double` |
| `exp(x)` | `Math.Exp` | `double` |
| `floor(x)`, `ceil(x)` | `Math.Floor/Ceiling` | preserves `decimal`, else `double` |
| `round(x)`, `round(x, n)` | `Math.Round` | preserves `decimal`, else `double` |
| `truncate(x)` | `Math.Truncate` | preserves `decimal`, else `double` |
| `sign(x)` | `Math.Sign` | `int` |
| `atan2(y, x)` | `Math.Atan2` | `double` |
| `min(a, b)`, `max(a, b)` | `Math.Min/Max` | preserves type |
| `pow(x, y)` | `Math.Pow` | `double` |
| `clamp(v, min, max)` | `Math.Min(Math.Max(...))` | preserves type |

<!-- test: ExtRef_Math_Functions.csx -->

## Aggregate Functions

Operate on collections. Delegate to `Enumerable` methods with type-specific dispatch for `int`, `long`, `float`, `double`, `decimal` (and nullable variants).

```csharp
sum(new[] { 1, 2, 3, 4 })        // 10
avg(new[] { 2, 4, 6, 8 })        // 5.0
count(new[] { 10, 20, 30 })      // 3
min(new[] { 5, 3, 8, 1, 4 })     // 1
max(new[] { 5, 3, 8, 1, 4 })     // 8
```

<!-- test: ExtRef_Aggregate.csx -->

`min` and `max` with two scalar arguments are math functions (`Math.Min`/`Math.Max`). With one collection argument, they are aggregates.

## Date/Time Sugar

### Clock Functions

| Function | Returns |
|----------|---------|
| `now()` | `DateTime.Now` |
| `today()` | `DateTime.Today` |

### TimeSpan Unit Properties

Applied to numeric values via member access. Singular and plural forms.

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

User variables shadow unit names.

## Control Flow

### Unless

`unless (cond) { body }` desugars to `if (!cond) { body }`.

```csharp
var x = 5;
var result = "default";
unless (x > 10) { result = "small"; }
return result;    // "small"
```

<!-- test: ExtRef_Stmt_Unless.csx -->

### Until

`until (cond) { body }` desugars to `while (!cond) { body }`.

```csharp
var i = 0;
until (i >= 3) { i++; }
return i;    // 3
```

<!-- test: ExtRef_Stmt_Until.csx -->
