# Extended Mode

CsEval's Extended mode transforms C# evaluation from a strict-spec engine into a powerful expression language. It keeps full C# compatibility while adding the best ideas from Python, Ruby, Kotlin, F#, and SQL — the syntax you wish C# had for scripting, rule engines, and dynamic evaluation.

```csharp
var engine = new CsEvalEngine(CsEvalOptions.Default with
{
    LanguageMode = LanguageMode.Extended
});

// All standard C# still works — Extended adds, never removes
engine.Evaluate("Math.Pow(2, 3)");          // 8.0 (standard C#)
engine.Evaluate("2 ** 3");                   // 8.0 (extended sugar)
```

Every Extended feature is gated — Standard mode rejects them with a clear `CsEvalLanguageModeException`. Zero ambiguity between modes.

---

## Write Math Like Math

No more `Math.` prefix. Functions and constants are available bare.

```csharp
// Standard C#
Math.Sqrt(Math.Pow(Math.Sin(x), 2) + Math.Pow(Math.Cos(x), 2))

// Extended
sqrt(sin(x) ** 2 + cos(x) ** 2)
```

### Constants

| Name | Value |
|------|-------|
| `pi` | 3.14159... |
| `e` | 2.71828... |
| `tau` | 6.28318... |
| `infinity` | `double.PositiveInfinity` |
| `nan` | `double.NaN` |

### Functions

| Category | Functions |
|----------|-----------|
| Trig | `sin`, `cos`, `tan`, `asin`, `acos`, `atan` |
| Hyperbolic | `sinh`, `cosh`, `tanh` |
| Roots | `sqrt`, `cbrt` |
| Log | `log`, `log2`, `log10`, `ln`, `exp` |
| Rounding | `floor`, `ceil`, `round`, `truncate` |
| Other | `abs`, `sign`, `min`, `max`, `pow`, `atan2`, `clamp` |

Multi-argument: `round(x, 2)`, `log(x, base)`, `atan2(y, x)`, `min(a, b)`, `max(a, b)`, `clamp(v, lo, hi)`

`abs`, `min`, `max`, and `clamp` preserve the input type (`int`, `long`, `decimal`, etc.) rather than always returning `double`.

User variables and functions always shadow built-in names — no surprises.

### Power Operator (`**`, `**=`)

```csharp
// Before                          // After
Math.Pow(2, 3)                     2 ** 3           // 8.0
x = Math.Pow(x, 3);               x **= 3;
Math.Pow(2, Math.Pow(3, 2))       2 ** 3 ** 2      // 512.0 (right-associative)
```

---

## Comparisons That Read Like English

### Chained Comparisons

Middle operands are evaluated exactly once. Short-circuits on first `false`.

```csharp
// Before                          // After
0 < x && x < 10                   0 < x < 10
a <= b && b <= c                   a <= b <= c
```

### `between`, `in`, `like`

```csharp
// Before                                      // After
x >= 1 && x <= 10                              x between 1 and 10
arr.Contains(3)                                 3 in arr
!arr.Contains(3)                                3 not in arr
Regex.IsMatch("hello", "^hel.*")                "hello" like "hel%"
```

`like` uses SQL wildcards: `%` = any characters, `_` = single character. Regex metacharacters are literal.

```csharp
"abc" like "a_c"             // true  (_ matches single char)
"abc" like "a.c"             // false (dot is literal, not regex)
"abXXcdYYef" like "ab%cd%ef" // true
"hello" not like "xyz%"      // true
```

### `and`, `or`, `not`

Word-form boolean operators, available as general-purpose alternatives to `&&`, `||`, `!`.

```csharp
x > 0 and x < 100
name != null or fallback
not isEmpty
```

These work in **both Standard and Extended modes** — the lexer unconditionally maps `and`→`&&`, `or`→`||`, `not`→`!`.

> **Conformance note:** In real C#, `and`/`or`/`not` are contextual keywords that only have special meaning inside pattern matching (`is`, `switch`). Outside patterns, they're ordinary identifiers — `var and = 5;` is valid C#. CsEval treats them as reserved operators in all contexts, which means they cannot be used as variable names.

---

## Regex and Strict Equality

### Regex Match (`=~`, `!~`)

```csharp
// Before                              // After
Regex.IsMatch("hello", "^he")          "hello" =~ "^he"     // true
!Regex.IsMatch("hello", "^he")         "hello" !~ "^he"     // false
```

Both operands must be non-null strings.

### Strict Equality (`===`, `!==`)

Type-exact comparison — no numeric widening, no coercion.

```csharp
1 == 1L              // true  (C# widens int to long)
1 === 1L             // false (int is not long)
1 === 1              // true  (same type, same value)
1.0f === 1.0         // false (float is not double)
true === 1           // false (bool is not int)
null === null        // true
```

### Spaceship Operator (`<=>`)

Three-way comparison returning `-1`, `0`, or `1`.

```csharp
// Before                                  // After
Comparer<int>.Default.Compare(a, b)        a <=> b

5 <=> 10       // -1
5 <=> 5        // 0
10 <=> 5       // 1
null <=> null  // 0
null <=> 5     // -1
```

---

## Collections

### Literals, Spread, and Comprehensions

```csharp
// Array literals
[1, 2, 3]                               // int[]
["a", "b", "c"]                          // string[]

// Spread
[..arr1, ..arr2]                         // concatenate
[1, ..middle, 99]                        // mix scalars and spreads

// Comprehensions
[x * 2 for x in 1..5]                   // [2, 4, 6, 8, 10]
[x for x in 1..20 if x % 3 == 0]       // [3, 6, 9, 12, 15, 18]
```

### Range Literals

```csharp
1..5                 // [1, 2, 3, 4, 5]  (inclusive)
1..<5                // [1, 2, 3, 4]     (exclusive end)
```

Ranges are lazy `IEnumerable<int>`. Reversed ranges (e.g. `10..1`) return empty.

### Slice Notation

Python-style slicing on arrays and strings.

```csharp
"hello"[1:3]         // "el"
arr[1:4]             // elements at indices 1, 2, 3
arr[:3]              // first 3 elements
arr[2:]              // everything from index 2
arr[::2]             // every 2nd element
```

### Negative Indexing

Negative indices wrap from the end (Standard mode throws, matching .NET behavior).

```csharp
"hello"[-1]          // 'o'
list[-1]             // last element
```

### Aggregates

```csharp
// Before                              // After
new[] {1, 2, 3}.Sum()                  sum([1, 2, 3])       // 6
new[] {1, 2, 3}.Average()              avg([1, 2, 3])       // 2.0
new[] {1, 2, 3}.Count()                count([1, 2, 3])     // 3
new[] {1, 2, 3}.Min()                  min([1, 2, 3])       // 1
new[] {1, 2, 3}.Max()                  max([1, 2, 3])       // 3
```

---

## Objects

### Anonymous Object Literals

Creates `ExpandoObject` instances with property syntax.

```csharp
new { Name = "John", Age = 30 }
```

Result implements `IDictionary<string, object?>`.

### Object Spread

```csharp
new { ..obj }                        // shallow copy
new { ..obj, Status = "active" }     // copy + override/add
new { ..obj1, ..obj2 }               // merge (last wins)
new { ..person, City = "NYC" }       // works with typed objects
```

### Object Merge (`+`)

```csharp
new { A = 1 } + new { B = 2 }                // { A = 1, B = 2 }
new { A = 1, B = 2 } + new { B = 99 }        // { A = 1, B = 99 }
person + new { City = "NYC" }                  // merge typed + anonymous
```

Right operand wins on key conflicts. Works with `ExpandoObject`, `IDictionary<string, object?>`, and typed objects.

### String Multiplication

```csharp
"abc" * 3            // "abcabcabc"
"-" * 40             // "----------------------------------------"
```

---

## Functional Composition

### Pipeline Operator (`|>`)

```csharp
// Before                              // After
inc(5)                                  5 |> inc
((Func<int, int>)(x => x * 2))(5)     5 |> (x => x * 2)     // 10
```

Arithmetic binds tighter: `2 + 3 |> (x => x * 2)` is `(2 + 3) |> ...` = `10`.

### Let-In Expressions

Scoped variable bindings for single expressions, with destructuring support.

```csharp
let x = 5 in x * x                                // 25
let { Name, Age } = person in Name + ":" + Age    // "Ada:20"
```

### If Expressions

```csharp
// Before                     // After
x > 0 ? x : -x               if (x > 0) x else -x
```

---

## Date & Time

### Time Unit Sugar

```csharp
// Before                              // After
TimeSpan.FromDays(30)                  30.days
TimeSpan.FromHours(2)                  2.hours
TimeSpan.FromMinutes(45)               45.minutes
TimeSpan.FromDays(14)                  2.weeks
```

Both singular and plural forms: `day`/`days`, `hour`/`hours`, `minute`/`minutes`, `second`/`seconds`, `millisecond`/`milliseconds`, `week`/`weeks`.

### Date Arithmetic

```csharp
now() + 30.days              // 30 days from now
today() - 2.weeks            // two weeks ago
```

`now()` returns `DateTime.Now`, `today()` returns `DateTime.Today`.

---

## Control Flow

### `unless` and `until`

```csharp
// Before                              // After
while (!ready) { poll(); }             unless (ready) { poll(); }
do { work(); } while (!done);          until (done) { work(); }
```

---

## Keywords

### `let` (Statement)

```csharp
let x = 5;              // same as: var x = 5;
```

### `const`

Immutable local variable. Reassignment throws `CS0131`.

```csharp
const int x = 7;
x = 8;                  // error CS0131
```

