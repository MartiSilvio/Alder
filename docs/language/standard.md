Standard mode implements C# expression and statement semantics per ECMA-334 (7th edition, December 2023). Every expression passes through a five-stage compiler pipeline (lexer, parser, semantic binder, optimization passes, execution) before producing a result. This is the default `LanguageMode`. Alder delegates to .NET wherever possible: LINQ calls the real `Enumerable` methods, `Math.Round` calls the real `Math.Round`, conversions follow CLR rules. The engine bridges dynamic evaluation to .NET, it doesn't reimplement it.

**Scope**: expressions, statements, lambdas (including async and anonymous delegates), LINQ (method and query syntax), pattern matching (§11.2), control flow (`if`/`for`/`while`/`foreach`/`switch`/`try`/`using`/`lock`), variable declarations, local functions, iterators (`yield return`/`yield break`), async/await, `checked`/`unchecked`, `goto`. Type declarations (`class`, `struct`, `enum`, `record`) and compilation-unit constructs (`namespace`, `using` directives) are outside scope. See [Scope Boundaries](#scope-boundaries).

For Extended mode features, see [Extended Mode](extended.md).

## Literals

### Numeric

Integer literal type follows §6.4.5.3: the first type in `int` > `uint` > `long` > `ulong` that can represent the value.

```csharp
42              // int
0xFF            // int (hex)
0b1010          // int (binary)
1_000_000       // int (digit separators)
42L             // long
42U             // uint
42UL            // ulong
3.14            // double
.5              // double (leading decimal)
1.5e-3          // double (scientific)
3.14f           // float
3.14m           // decimal
```

<!-- test: LangRef_Literal_Numeric.csx -->

Hex and binary literals support `L`, `U`, `UL` suffixes. `F`, `D`, `M` are not valid on hex/binary.

### String

Six string forms:

```csharp
"hello\nworld"                   // regular (escape sequences processed)
@"C:\Users\path"                 // verbatim (no escapes, "" for literal quote)
"""raw content"""                // raw (C# 11, no escapes)
$"Hello {name}"                  // interpolated
$@"C:\{folder}"                  // verbatim interpolated (either prefix order)
$"""Hello {name}"""              // raw interpolated
$$"""{ "key": {{value}} }"""     // multi-dollar raw (C# 11)
```

<!-- test: LangRef_Literal_String.csx -->

**Interpolation holes** support alignment and format specifiers: `{expr,10:F2}`. `{{` and `}}` produce literal braces in single-dollar strings.

**Multi-dollar interpolation** (C# 11): prefix a raw string with multiple `$` to change the brace count for interpolation. `$$"""{{expr}}"""` requires `{{` and `}}` as delimiters. Single braces become literal. Useful for embedding JSON or other brace-heavy content.

**Escape sequences**: `\n`, `\r`, `\t`, `\0`, `\a`, `\b`, `\f`, `\v`, `\\`, `\"`, `\'`, `\xHH` (1-4 hex digits), `\uHHHH` (4 hex), `\UHHHHHHHH` (8 hex, up to U+10FFFF).

### Character

```csharp
'a'             // single character
'\n'            // escape sequence
'\u0041'        // unicode escape (A)
```

<!-- test: LangRef_Literal_Char.csx -->

Empty character literals (`''`) produce a parse error.

### Boolean and Null

`true`, `false`, `null`.

## Operators

### Precedence

Highest to lowest:

| Level | Category | Operators | Assoc. |
|-------|----------|-----------|--------|
| 18 | Primary | `x.y` `x?.y` `x[i]` `x?[i]` `f(x)` `x++` `x--` `new` `typeof` `sizeof` `nameof` `default` `checked` `unchecked` | Left |
| 17 | Unary | `-x` `+x` `!x` `~x` `^x` `++x` `--x` `(T)x` `throw` | Right |
| 16 | Range | `..` | |
| 15 | Multiplicative | `*` `/` `%` | Left |
| 14 | Additive | `+` `-` | Left |
| 13 | Shift | `<<` `>>` `>>>` | Left |
| 12 | Relational | `<` `<=` `>` `>=` `is` `as` | Left |
| 11 | Equality | `==` `!=` | Left |
| 10 | Bitwise AND | `&` | Left |
| 9 | Bitwise XOR | `^` | Left |
| 8 | Bitwise OR | `\|` | Left |
| 7 | Logical AND | `&&` | Left |
| 6 | Logical OR | `\|\|` | Left |
| 5 | Null-coalescing | `??` | Right |
| 4 | Conditional | `? :` | Right |
| 3 | Assignment | `=` `+=` `-=` `*=` `/=` `%=` `&=` `\|=` `^=` `<<=` `>>=` `>>>=` `??=` | Right |

### Arithmetic

`+`, `-`, `*`, `/`, `%` follow §12.4.7.3 binary numeric promotion. Both operands are widened to a common type before the operation.

```csharp
1 + 2.0         // 3.0 (double: int promoted)
1.0m + 2        // 3.0m (decimal: int promoted)
7 / 2           // 3 (integer division truncates)
7.0 / 2         // 3.5 (floating-point division)
'A' + 1         // 66 (char promotes to int per §10.2.3)
```

<!-- test: LangRef_Op_Arithmetic.csx -->

`+` also handles string concatenation (`"a" + 1` produces `"a1"`), `DateTime + TimeSpan`, `TimeSpan + TimeSpan`, and `Delegate.Combine`. `-` handles `DateTime - DateTime` (produces `TimeSpan`), `DateTime - TimeSpan`, and `Delegate.Remove`.

Mixing `decimal` with `float` or `double` is a compile-time error per §10.2.3.

### Comparison

`==`, `!=`, `<`, `<=`, `>`, `>=`. Numeric comparisons follow binary numeric promotion.

```csharp
1 == 1.0            // true (int promoted to double)
(1, 2) == (1, 2)    // true (element-wise with per-element promotion)
"abc" == "abc"       // true (string value equality)
double.NaN == double.NaN  // false (IEEE 754)
```

<!-- test: LangRef_Op_Comparison.csx -->

Tuple equality compares element-wise. Each pair is promoted independently.

NaN follows IEEE 754: `NaN != NaN` is `true`, `NaN == anything` is `false`.

### Logical

`&&` and `||` short-circuit.

```csharp
true || throw new Exception()    // true (right side never evaluated)
false && throw new Exception()   // false (right side never evaluated)
```

<!-- test: LangRef_Op_Logical.csx -->

### Bitwise

`&`, `|`, `^` on integers and enums. `~` is bitwise NOT. `<<` and `>>` are arithmetic shifts (sign-extending). `>>>` is unsigned right shift (zero-filling).

`&` and `|` on `bool` are non-short-circuiting (both sides always evaluate). On `bool?`, they implement three-valued logic per §12.13.5 and §12.14.2:

```csharp
0xFF & 0x0F         // 15
true & false        // false (both sides evaluated)
null | true         // true (three-valued logic)
null & false        // false
null & null         // null
```

<!-- test: LangRef_Op_Bitwise.csx -->

### Null Operators

```csharp
null ?? "fallback"              // "fallback"
string? s = null; s ??= "set"  // s is now "set"
obj?.Property                   // null if obj is null, short-circuits the chain
arr?[0]                         // null if arr is null
```

<!-- test: LangRef_Op_Null.csx -->

`??` is right-associative: `a ?? b ?? c` is `a ?? (b ?? c)`.

### Assignment

`=` assigns to variables, properties, indexers. Compound operators: `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `|=`, `^=`, `<<=`, `>>=`, `>>>=`, `??=`.

```csharp
var x = 10; x += 5; return x;      // 15
var arr = new int[] { 1, 2, 3 }; arr[0] = 99; return arr[0]; // 99
```

<!-- test: LangRef_Op_Assignment.csx -->

Targets: variables (`x`), properties (`obj.Name`), indexers (`arr[0]`), multi-dimensional indexers (`matrix[1, 2]`). All compound variants work on all targets: `obj.Count += 1`, `arr[0] *= 2`, `dict["key"] ??= "default"`.

### Increment and Decrement

Prefix `++x` / `--x` returns the new value. Postfix `x++` / `x--` returns the old value then modifies.

```csharp
var i = 5; var a = i++; var b = ++i; return (a, b); // (5, 7)
```

<!-- test: LangRef_Op_Increment.csx -->

Works on variables, member access (`obj.Count++`), and index access (`arr[0]++`).

### Type Testing

```csharp
obj is string               // type test
obj is string s             // type test + variable binding
obj as string               // safe cast (returns null on failure)
(int)obj                    // explicit cast (throws on failure)
```

<!-- test: LangRef_Op_TypeTest.csx -->

`is` supports the full [Pattern Matching](#pattern-matching) system.

Explicit cast follows §10.3. Unboxing requires the exact type: `(long)(object)42` throws because the boxed value is `int`. To widen after unboxing: `(long)(int)(object)42`.

### Index and Range

```csharp
arr[^1]         // last element (§12.8.11)
arr[1..3]       // elements at index 1, 2 (exclusive end)
arr[..2]        // first two elements
arr[3..]        // from index 3 through end
```

<!-- test: LangRef_Op_Range.csx -->

`^n` creates a `System.Index` from end. `a..b` creates a `System.Range`.

### User-Defined Operators

When built-in operators don't match, Alder searches both operand types for `op_Addition`, `op_Subtraction`, `op_Multiply`, etc. In a `checked` context, checked variants (`op_CheckedAddition`) are tried first, falling back to unchecked.

## Expressions

### Conditional (Ternary)

```csharp
score >= 90 ? "A" : score >= 80 ? "B" : "C"
```

<!-- test: LangRef_Expr_Ternary.csx -->

Right-associative: `a ? b : c ? d : e` is `a ? b : (c ? d : e)`. Result type is the common type of both branches via binary numeric promotion.

### Lambda

```csharp
x => x * 2                                     // expression body, inferred parameter
(int x) => x * 2                               // typed parameter
(a, b) => a + b                                // multi-parameter
() => 42                                       // parameterless
(int n) => { var r = 1; for (var i = 2; i <= n; i++) r *= i; return r; }  // block body
async x => await Task.FromResult(x * 2)        // async lambda
```

<!-- test: LangRef_Expr_Lambda.csx -->

Parameter types are inferred through §12.6.3 generic type inference when passed to generic methods like `Where<T>` or `Select<T, TResult>`.

Closures capture variables by reference:

```csharp
var count = 0;
var items = new[] { 1, 2, 3 };
items.ToList().ForEach(x => count += x);
return count;   // 6
```

### Anonymous Delegates

```csharp
delegate(int x) { return x * 2; }     // typed parameters
delegate { return 42; }                // parameterless (matches any delegate type)
```

### Method Calls

Overload resolution per §12.6.4: candidate construction, applicability filtering, best function member selection.

```csharp
"hello".ToUpper()                               // instance method
string.Join(", ", items)                         // static method
items.Where(x => x > 0)                         // extension method (System.Linq.Enumerable)
Enumerable.Empty<int>()                          // explicit generic type arguments
string.Format(format: "{0}", arg0: "hi")         // named arguments
int.TryParse("42", out var n)                    // out parameter with inline declaration
int.TryParse("42", out _)                        // out discard
```

<!-- test: LangRef_Expr_MethodCall.csx -->

Generic type inference (§12.6.3) runs in phases: lower-bound inference from arguments, iterative fixing, output type inference from lambda return types. This resolves `items.Select(x => x.Name)` to `Select<Item, string>` automatically.

Best function member selection (§12.6.4.3) uses 7 tie-breaking rules: non-generic over generic, normal over expanded form, fewer expanded params, more specific parameter types, better conversion targets, fewer defaults used, fewer type parameters.

### Object Creation

```csharp
new List<int>()                                  // parameterless constructor
new DateTime(2024, 1, 1)                         // with arguments
new List<int> { 1, 2, 3 }                        // collection initializer
new { Name = "Alice", Age = 30 }                 // anonymous object (ExpandoObject)
new Exception("msg") { Source = "test" }         // property initializer
new Dictionary<string, int> { ["a"] = 1 }        // indexer initializer
new[] { 1, 2, 3 }                                // implicitly typed array (element type inferred)
new int[] { 1, 2, 3 }                            // explicitly typed array
new int[10]                                       // sized array
new int[3, 3]                                     // multi-dimensional
new int[,] { { 1, 2 }, { 3, 4 } }                // multi-dim with initializer
new int[3][]                                      // jagged array
```

<!-- test: LangRef_Expr_ObjectCreation.csx -->

Constructor overload resolution follows the same §12.6.4 pipeline as method calls.

### With Expressions

Non-destructive mutation for records and structs:

```csharp
person with { Name = "Bob" }                     // clone with modified property
point with { X = 10 }                            // struct copy
p with { Name = "Bob" } with { Age = 25 }        // chained
```

### Tuples

```csharp
(1, "hello")                        // unnamed ValueTuple
(x: 1, y: 2)                        // named elements
var (a, b) = (10, 20)               // deconstruction
(1, 2) == (1, 2)                    // true (element-wise, per-element promotion)
```

<!-- test: LangRef_Expr_Tuple.csx -->

Named tuples preserve element names for member access: `t.x`, `t.y`. Deconstruction works on tuples and any type with a `Deconstruct` method.

### Introspection

```csharp
typeof(int)                          // System.Int32
typeof(List<int>)                    // generic type
sizeof(int)                          // 4
nameof(myVar)                        // "myVar"
nameof(string.Length)                // "Length"
default(int)                         // 0
default                              // context-dependent default
checked(int.MaxValue + 1)           // throws OverflowException
unchecked(int.MaxValue + 1)         // wraps to int.MinValue
```

<!-- test: LangRef_Expr_Introspection.csx -->

`checked` / `unchecked` also support block form: `checked { statements }`. In checked context, integer arithmetic uses overflow-checking. Floating-point is unaffected (IEEE 754).

### Switch Expression

```csharp
x switch
{
    pattern => value,
    pattern when guard => value,
    _ => defaultValue
}
```

<!-- test: LangRef_Expr_SwitchExpr.csx -->

Supports the full [Pattern Matching](#pattern-matching) system. `when` guards add boolean conditions. Arms separated by commas. Non-exhaustive matches throw `SwitchExpressionException` at runtime.

### Throw Expression

`throw` as an expression in null-coalescing, conditional, and switch arm contexts:

```csharp
x ?? throw new ArgumentNullException()
valid ? value : throw new Exception()
_ => throw new NotSupportedException()
```

<!-- test: LangRef_Expr_Throw.csx -->

### String Interpolation

```csharp
$"Hello {name}"                                  // basic
$"{value,10}"                                    // alignment (right-pad to 10)
$"{price:C}"                                     // format specifier (currency)
$"{amount,12:F2}"                                // alignment + format
$"{{literal braces}}"                            // escaped braces
$"{(condition ? "yes" : "no")}"                  // expressions with colons need parens
```

### Member and Index Access

```csharp
obj.Property            // member access
obj?.Property           // null-conditional (short-circuits)
a?.b?.c                 // chained null-conditional
arr[0]                  // index
arr?[0]                 // null-conditional index
matrix[1, 2]            // multi-dimensional index
arr[^1]                 // from end
arr[1..3]               // range slice
```

When the binder knows the static type (via `SetVariable<T>` or inference), member access is resolved at bind time with the exact `PropertyInfo`/`FieldInfo`. When the type is `object`, resolution is deferred to runtime.

## Statements

Use `return` to produce a value from a statement block. Multi-statement blocks are delimited by semicolons.

### Variable Declarations

```csharp
var name = "Alice"                                // inferred type
int count = 0                                     // explicit type
List<int> items = new List<int>()                 // explicit generic type
const double PI = 3.14159                         // constant (§10.2.11)
int a = 1, b = 2, c = 3                          // multiple declarations
var (x, y) = (10, 20)                             // deconstruction
```

<!-- test: LangRef_Stmt_VarDecl.csx -->

`const` enforces §10.2.11: initializer must be a constant expression, implicit constant conversions apply (non-negative `int` constant converts to `uint`).

### Local Functions

```csharp
int Add(int a, int b) { return a + b; }
return Add(3, 4);   // 7
```

<!-- test: LangRef_Stmt_LocalFunc.csx -->

Return type can be explicit or `var`. Local functions support recursion:

```csharp
int Factorial(int n) { return n <= 1 ? 1 : n * Factorial(n - 1); }
return Factorial(10);   // 3628800
```

### Control Flow

```csharp
if (cond) { } else if (cond) { } else { }
for (var i = 0; i < n; i++) { }
while (cond) { }
do { } while (cond);
foreach (var item in collection) { }
break;
continue;
return expr;
goto label;
goto case value;
goto default;
label: statement
yield return expr;
yield break;
```

<!-- test: LangRef_Stmt_ControlFlow.csx -->

`for` supports multiple initializers and multiple increments: `for (int i = 0, j = 10; i < j; i++, j--)`.

`foreach` requires `IEnumerable` or `IEnumerable<T>` (§13.9). The iteration variable is scoped per-iteration for correct closure capture.

### Iterators

Local functions containing `yield return` or `yield break` are iterator functions that return `IEnumerable<T>` and produce elements lazily.

```csharp
IEnumerable<int> Gen()
{
    yield return 1;
    yield return 2;
    yield return 3;
}
return Gen().ToList();   // [1, 2, 3]
```

Iterators support all control flow: loops, conditionals, try-catch, nested blocks. Infinite sequences work with `.Take(n)`:

```csharp
IEnumerable<int> Fib()
{
    var a = 0;
    var b = 1;
    while (true)
    {
        yield return a;
        var temp = a;
        a = b;
        b = temp + b;
    }
}
return Fib().Take(8).ToList();   // [0, 1, 1, 2, 3, 5, 8, 13]
```

The returned `IEnumerable<T>` is fully compatible with LINQ and `foreach`.

### Async/Await

```csharp
var result = await Task.FromResult(42);
return result;
```

`await` unwraps `Task<T>`, `Task`, `ValueTask<T>`, `ValueTask`. Requires `EvaluateAsync`; using `await` with synchronous `Evaluate` produces `CS4033`. `await` inside `lock` is prohibited per §12.9.8.1 (`CS1996`).

Async lambdas:

```csharp
Func<int, Task<int>> doubler = async x => await Task.FromResult(x * 2);
return await doubler(21);   // 42
```

`CancellationToken` is auto-injected when a method's last parameter is `CancellationToken` and the caller provides one fewer argument.

### Switch Statement

```csharp
switch (expr)
{
    case 42:
        break;
    case string s when s.Contains("x"):
        break;
    case > 100:
        break;
    case { Length: > 0 }:
        break;
    default:
        break;
}
```

<!-- test: LangRef_Stmt_Switch.csx -->

Each case must end with `break`, `return`, `goto case`, `goto default`, or `throw`. Implicit fall-through produces `CS0163`. Explicit fall-through: `goto case value;` or `goto default;`.

### Exception Handling

```csharp
try { }
catch (FormatException ex) { }                   // typed catch
catch (Exception ex) when (ex.Message != "") { }  // catch filter (when guard)
catch { }                                         // untyped catch (must be last)
finally { }                                       // always runs
```

<!-- test: LangRef_Stmt_TryCatch.csx -->

Must have at least one `catch` or `finally`. `throw;` inside a catch block rethrows with original stack trace (via `ExceptionDispatchInfo`). Fully qualified exception types: `catch (System.IO.IOException ex)`.

When guards: if the guard expression throws, the exception is suppressed and the guard is treated as `false`, allowing the next catch clause to be tried.

### Using and Lock

```csharp
using (var r = expr) { }        // disposes IDisposable on exit
using (expr) { }                // without variable declaration
lock (obj) { }                  // Monitor.Enter/Exit
```

<!-- test: LangRef_Stmt_UsingLock.csx -->

`using` disposes both `IDisposable` and `IAsyncDisposable`. In async context (`EvaluateAsync`), `IAsyncDisposable` is preferred and properly awaited.

`lock` requires a reference type (CS0185 on value types). The body is evaluated synchronously even in async context per §12.9.8.1.

## Pattern Matching

The complete ECMA-334 §11.2 pattern system. Patterns appear in `is` expressions, `switch` expression arms, and `switch` statement cases.

### Constant

```csharp
x is 42
x is "hello"
x is null
x is true
```

### Type

```csharp
x is int
x is string s                   // binds s on match
x is List<int> items            // generic types
x is int?                       // nullable
```

### Relational

```csharp
x is > 0
x is <= 100
x is >= 0 and < 50
```

### Logical Combinators

```csharp
x is 1 or 2 or 3
x is > 0 and < 100
x is not null
x is not (null or "")           // parenthesized for grouping
```

Precedence: `or` < `and` < `not` < relational < primary. So `x is not null and string` parses as `(not null) and string`.

### Property (§11.2.5)

```csharp
x is { Length: > 0 }
x is string { Length: > 3 } s            // type + property + binding
x is { Name: "Alice", Age: >= 18 }       // multiple properties
x is { Address: { City: "London" } }     // nested property patterns
```

### Var and Discard

```csharp
x is var v           // always matches, binds value
x is _               // always matches, no binding
```

### Positional

```csharp
point is (0, 0)
point is (> 0, _)
point is (var x, var y) when x > 0      // var positional with guard
```

Works on `ITuple` types (ValueTuple). Validates element count at runtime.

### List (C# 11)

```csharp
arr is [1, 2, 3]                         // exact match
arr is [1, .., 5]                        // first is 1, last is 5, any length >= 2
arr is [_, ..var rest]                   // skip first, capture rest
arr is [> 0, > 0, ..]                   // first two positive, any remaining
```

Works on `IList`, arrays, and strings.

### Combined Patterns

Patterns compose. A real-world switch expression:

```csharp
order switch
{
    { Status: "shipped", Total: > 1000 } => "high-value shipped",
    { Status: "shipped" } => "shipped",
    { Status: "pending", Items: [_, _, ..] } => "pending with 2+ items",
    { Status: var s } when s != null => $"status: {s}",
    _ => "unknown"
}
```

<!-- test: LangRef_Pattern_All.csx -->

## LINQ

### Method Syntax

All `System.Linq.Enumerable` extension methods are available by default:

```csharp
// Filtering, projection, ordering
items.Where(x => x > 0).Select(x => x * 2).OrderBy(x => x)

// Grouping and joining
items.GroupBy(x => x.Category)
orders.Join(products, o => o.ProductId, p => p.Id, (o, p) => new { o, p })

// Aggregation
items.Count()
items.Sum(x => x.Price)
items.Average()
items.Min()
items.Max()
items.Aggregate(0, (acc, x) => acc + x)

// Element access
items.First()
items.FirstOrDefault()
items.Single(x => x.Id == 1)
items.ElementAt(3)

// Set operations
a.Distinct().Union(b).Intersect(c).Except(d)

// Partitioning
items.Skip(5).Take(10)
items.SkipWhile(x => x < 0).TakeWhile(x => x < 100)

// Conversion
items.ToList()
items.ToArray()
items.ToDictionary(x => x.Id)
items.ToHashSet()
items.Cast<int>()
items.OfType<string>()

// Generation
Enumerable.Range(1, 10)
Enumerable.Repeat("x", 5)
Enumerable.Empty<int>()
```

<!-- test: LangRef_Linq_Methods.csx -->

Lambda parameter types are inferred from the collection's element type. `items.Select(x => x.Name)` infers `x` as the element type and `Name` as the return type through §12.6.3.

### Query Syntax

Query expressions desugar to method calls at parse time:

```csharp
from x in source
where x > 0
orderby x.Name descending
select x.Value
```

Desugars to: `source.Where(x => x > 0).OrderByDescending(x => x.Name).Select(x => x.Value)`.

<!-- test: LangRef_Linq_Query.csx -->

| Clause | Desugars to |
|--------|-------------|
| `from x in source` | Initial source |
| `where predicate` | `.Where(x => predicate)` |
| `select projection` | `.Select(x => projection)` |
| `orderby key` | `.OrderBy(x => key)` |
| `orderby key descending` | `.OrderByDescending(x => key)` |
| `orderby k1, k2` | `.OrderBy(..).ThenBy(..)` |
| `let v = expr` | `.Select(x => new { x, v = expr })` |
| `group elem by key` | `.GroupBy(x => key, x => elem)` |
| `join y in s on k1 equals k2` | `.Join(s, x => k1, y => k2, ..)` |
| `join y in s on k1 equals k2 into g` | `.GroupJoin(..)` |
| Multiple `from` clauses | `.SelectMany(..)` |
| `into name` | New query over prior result |

Transparent identifier nesting is fully implemented for multi-clause queries with `let`, multiple `from`, and `join`.

## Type System

### Type Keywords

| Keyword | CLR Type | Keyword | CLR Type |
|---------|----------|---------|----------|
| `sbyte` | `SByte` | `float` | `Single` |
| `byte` | `Byte` | `double` | `Double` |
| `short` | `Int16` | `decimal` | `Decimal` |
| `ushort` | `UInt16` | `bool` | `Boolean` |
| `int` | `Int32` | `char` | `Char` |
| `uint` | `UInt32` | `string` | `String` |
| `long` | `Int64` | `object` | `Object` |
| `ulong` | `UInt64` | `dynamic` | `Object` |
| `nint` | `IntPtr` | `nuint` | `UIntPtr` |

All support nullable syntax (`int?`, `bool?`) and work in `typeof()`, casts, `is`/`as`, variable declarations.

### Type Syntax

```csharp
List<int>                                    // generic
Dictionary<string, List<int>>                // nested generics
int[]                                        // array
int[,]                                       // multi-dimensional
int[][]                                      // jagged
int?                                         // nullable value type
System.Text.StringBuilder                    // fully qualified name
```

### Numeric Promotion

§12.4.7.3 defines eight rules applied in order. Both operands promoted to the first match:

| Rule | Condition | Result |
|------|-----------|--------|
| 1 | Either is `decimal` | `decimal` (error if other is `float`/`double`) |
| 2 | Either is `double` | `double` |
| 3 | Either is `float` | `float` |
| 4 | Either is `ulong` | `ulong` (error if other is signed) |
| 5 | Either is `long` | `long` |
| 6 | One `uint`, other `sbyte`/`short`/`int` | `long` |
| 7 | Either is `uint` | `uint` |
| 8 | Default | `int` |

`char` has implicit conversions to `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal` per §10.2.3. `char` is **not** in the `sbyte`/`short`/`int` set, so `uint + char` hits rule 7 (`uint`), not rule 6 (`long`).

Constant expressions get additional promotion per §10.2.11: a non-negative `int` constant can convert to `uint`.

Nullable arithmetic: if either operand is `null`, result is `null`.

### Implicit Conversions

Per §10.2:

- **Numeric** (§10.2.3): `int` to `long`, `float` to `double`, etc.
- **Nullable** (§10.2.6): `T` to `T?`, lifted `T` to `U?` when `T` to `U` exists
- **Reference** (§10.2.8): inheritance, interface implementation, boxing
- **Tuple** (§10.2.13): element-wise implicit convertibility
- **User-defined** (§10.5.4): `op_Implicit` on source and target type hierarchies

### Explicit Conversions

- **Numeric** (§10.3.2): narrowing, with overflow checking in `checked` context
- **Unboxing** (§10.3.7): requires exact type. `(long)(object)42` throws because the boxed value is `int`. To unbox then widen: `(long)(int)(object)42`
- **User-defined** (§10.5.5): `op_Explicit`

### Enum Arithmetic

```csharp
DayOfWeek.Monday + 1                   // DayOfWeek.Tuesday
DayOfWeek.Wednesday - DayOfWeek.Monday  // 2 (underlying type)
~DayOfWeek.Monday                       // bitwise complement
```

### Default Available Types

| Namespace | Examples |
|-----------|---------|
| `System` | `Math`, `Convert`, `DateTime`, `Guid`, `Random`, `TimeSpan`, `Array`, `Tuple`, `StringComparison`, `DayOfWeek`, `TypeCode` |
| `System.Collections.Generic` | `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`, `KeyValuePair<K,V>`, `SortedList<K,V>`, `LinkedList<T>` |
| `System.Linq` | All `Enumerable` extension methods |
| `System.Threading.Tasks` | `Task`, `Task<T>` |
| Via FQN | Any type from loaded assemblies: `new System.Text.StringBuilder()` |

Additional namespaces: `AlderOptions.Types.AddNamespace()`. Additional assemblies: `AlderOptions.Types.AddAssembly()`.

### Comments

```csharp
// single-line
/* multi-line */
```

## Scope Boundaries

These constructs produce parse errors:

| Construct | Alternative |
|-----------|-------------|
| `class`, `struct`, `interface`, `enum`, `record` | Pass existing types via `SetVariable<T>`, or use `new { Name = "x" }` |
| `namespace` | Not needed |
| `using System.IO;` | `o.Types.AddNamespace("System.IO")` |
| `async` local functions | Use async lambdas: `async x => await ...` |
| `ref` / `params` in declarations | Calling `out` methods works: `int.TryParse("42", out var n)` |
| `this` / `base` | Pass the instance as a variable |
| `stackalloc` / `fixed` / `unsafe` | Not applicable |
| Type/member declarations | Use existing types from registered assemblies |
| Generic method declarations | Calling generic methods works: `Enumerable.Empty<int>()` |
| `[1, 2, 3]` collection expressions | Extended mode only. Use `new[] { 1, 2, 3 }` in Standard |
