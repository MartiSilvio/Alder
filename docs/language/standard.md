---
title: "Standard Mode"
description: "Full ECMA-334 language reference — literals, operators, expressions, statements, patterns, LINQ"
sidebar:
  order: 2
---

Alder implements C# expression and statement semantics per ECMA-334 (7th edition, December 2023). Every expression passes through a full compilation pipeline (lexer, parser, semantic binder, type resolution, and operator dispatch) before execution. This reference covers every construct available in `LanguageMode.Standard`, the default mode.

**Scope**: expressions, statements, lambdas, LINQ (method and query syntax), full pattern matching (§11.2), control flow (`if`/`for`/`while`/`foreach`/`switch`/`try`), variable declarations, local functions, `checked`/`unchecked`, `using`/`lock`, `goto`. Type declarations (`class`, `struct`, `enum`, `record`) and compilation-unit constructs (`namespace`, `using` directives, `async`/`await`, `yield`) are outside scope — see [Scope Boundaries](#scope-boundaries).

For Extended mode features, see [Extended Mode](extended.md).

## Literals

### Numeric

Integer literals follow the ECMA-334 §6.4.5.3 type promotion chain: a literal's type is the first type in `int` → `uint` → `long` → `ulong` that can represent its value.

```csharp
42              // int
0xFF            // int (hex)
0b1010          // int (binary)
1_000_000       // int (digit separators allowed between digits)
42L             // long (promotes to ulong if value requires)
42U             // uint (promotes to ulong if value requires)
42UL            // ulong
3.14            // double
.5              // double (leading decimal)
1.5e-3          // double (scientific notation)
3.14f           // float
3.14m           // decimal
```

<!-- test: LangRef_Literal_Numeric.csx -->

Hex and binary literals support `L`, `U`, `UL` suffixes but not `F`, `D`, or `M`.

### String

```csharp
"hello\nworld"                   // regular — escape sequences processed
@"C:\Users\path"                 // verbatim — no escapes, "" for literal quote
"""raw content"""                // raw (C# 11) — no escapes
$"Hello {name}"                  // interpolated — expressions in {}
$@"C:\{folder}"                  // verbatim interpolated (either prefix order)
$"""Hello {name}"""              // raw interpolated
```

<!-- test: LangRef_Literal_String.csx -->

Interpolation holes support alignment and format specifiers: `{expr,10:F2}`. `{{` and `}}` produce literal braces.

Escape sequences in regular and interpolated strings: `\n`, `\r`, `\t`, `\0`, `\a`, `\b`, `\f`, `\v`, `\\`, `\"`, `\'`, `\xHH` (1–4 hex digits), `\uHHHH` (4 hex), `\UHHHHHHHH` (8 hex, up to U+10FFFF).

### Character

```csharp
'a'             // single character
'\n'            // escape sequence
'\u0041'        // unicode escape (A)
```

<!-- test: LangRef_Literal_Char.csx -->

Empty character literals (`''`) produce a parse error.

### Boolean and Null

`true`, `false`, `null`. No special behavior — they work as in C#.

## Operators

### Precedence

From highest binding to lowest:

| Level | Category | Operators | Assoc. |
|-------|----------|-----------|--------|
| 18 | Primary | `x.y` `x?.y` `x[i]` `x?[i]` `f(x)` `x++` `x--` `new` `typeof` `sizeof` `nameof` `default` `checked` `unchecked` | Left |
| 17 | Unary | `-x` `+x` `!x` `~x` `^x` `++x` `--x` `(T)x` `throw` | Right |
| 16 | Range | `..` | — |
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

`+`, `-`, `*`, `/`, `%` follow ECMA-334 §12.4.7.3 binary numeric promotion. Both operands are widened to a common type before the operation:

```csharp
1 + 2.0         // double — int promoted to double
1.0m + 2        // decimal — int promoted to decimal
7 / 2           // 3 — integer division truncates
7.0 / 2         // 3.5 — floating-point division
```

<!-- test: LangRef_Op_Arithmetic.csx -->

`+` also handles string concatenation, `DateTime` + `TimeSpan`, and delegate combination. `-` handles `DateTime` - `DateTime` → `TimeSpan` and `DateTime` - `TimeSpan`.

Mixing `decimal` with `float` or `double` is a compile-time error, per ECMA-334.

### Comparison

`==`, `!=`, `<`, `<=`, `>`, `>=`. Numeric comparisons follow binary numeric promotion. Tuple equality is element-wise with promotion.

```csharp
1 == 1.0            // true — int promoted to double
(1, 2) == (1, 2)    // true — element-wise comparison
"abc" == "abc"       // true — string value equality
```

<!-- test: LangRef_Op_Comparison.csx -->

### Logical

`&&` and `||` short-circuit: `&&` skips the right operand if the left is `false`; `||` skips the right if the left is `true`.

```csharp
true || throw new Exception()    // true — right side never evaluated
false && throw new Exception()   // false — right side never evaluated
```

<!-- test: LangRef_Op_Logical.csx -->

### Bitwise

`&`, `|`, `^` operate on integers and enums. `~` is bitwise NOT. `<<` and `>>` are left and right shift (arithmetic, sign-extending). `>>>` is unsigned right shift (zero-filling).

`&` and `|` on `bool` are non-short-circuiting — both sides always evaluate. On `bool?`, they implement ECMA-334's three-valued logic (§12.13.5, §12.14.2).

```csharp
0xFF & 0x0F         // 15
true & false        // false — both sides evaluated
null | true         // true — three-valued bool? logic
```

<!-- test: LangRef_Op_Bitwise.csx -->

### Null Operators

```csharp
null ?? "fallback"              // "fallback" — returns right if left is null
string? s = null; s ??= "set"  // s is now "set"
obj?.Property                   // null if obj is null (short-circuits)
arr?[0]                         // null if arr is null
```

<!-- test: LangRef_Op_Null.csx -->

`??` is right-associative: `a ?? b ?? c` evaluates as `a ?? (b ?? c)`.

### Assignment

`=` assigns to variables, properties, and indexers. Compound assignment operators (`+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `|=`, `^=`, `<<=`, `>>=`, `>>>=`, `??=`) combine the operation with assignment.

```csharp
var x = 10; x += 5; return x;      // 15
var arr = new int[] { 1, 2, 3 }; arr[0] = 99; return arr[0];   // 99
```

<!-- test: LangRef_Op_Assignment.csx -->

Assignment targets: variables (`x`), properties (`obj.Name`), indexers (`arr[0]`), multi-dimensional indexers (`matrix[1, 2]`).

### Increment and Decrement

Prefix `++x` / `--x` returns the new value. Postfix `x++` / `x--` returns the old value, then modifies. Works on variables, member access (`obj.Count++`), and index access (`arr[0]++`).

```csharp
var i = 5; var a = i++; var b = ++i; return (a, b);    // (5, 7)
```

<!-- test: LangRef_Op_Increment.csx -->

### Type Testing

```csharp
obj is string               // true if obj is a string
obj is string s             // true + binds s to the value
obj as string               // returns the string or null
(int)obj                    // explicit cast — throws InvalidCastException on failure
```

<!-- test: LangRef_Op_TypeTest.csx -->

`is` supports the full pattern matching system — see [Pattern Matching](#pattern-matching).

Explicit cast follows ECMA-334 §10.3: `(long)(object)42` throws because the boxed value is `int`, not `long`. Unboxing requires the exact type.

### Index and Range

```csharp
arr[^1]         // last element (Index from end, §12.8.11)
arr[1..3]       // elements 1, 2 (Range, exclusive end)
arr[..2]        // elements 0, 1
arr[3..]        // element 3 through end
```

<!-- test: LangRef_Op_Range.csx -->

### User-Defined Operators

When built-in operators don't match the operand types, Alder searches both operand types for operator methods via reflection: `op_Addition`, `op_Subtraction`, `op_Multiply`, etc. In a `checked` context, checked variants (e.g., `op_CheckedAddition`) are tried first.

## Expressions

### Conditional (Ternary)

Right-associative. Nested chains parse from the right: `a ? b : c ? d : e` is `a ? b : (c ? d : e)`.

```csharp
score >= 90 ? "A" : score >= 80 ? "B" : "C"
```

<!-- test: LangRef_Expr_Ternary.csx -->

The result type is the common type of both branches. If the branches are different numeric types, binary numeric promotion applies.

### Lambda

```csharp
x => x * 2                                 // expression body, inferred param
(int x) => x * 2                           // typed parameter
(a, b) => a + b                            // multi-param
() => 42                                   // parameterless
(int n) => { var r = 1; for (var i = 2; i <= n; i++) r *= i; return r; }  // block body
```

<!-- test: LangRef_Expr_Lambda.csx -->

Parameter types can be omitted when inferred from context — LINQ methods, delegate-typed parameters. The binder resolves parameter types through ECMA-334 §12.6.3 generic type inference when the lambda is passed to a generic method like `Where<T>` or `Select<T, TResult>`.

Lambdas cannot be `async`. `async`/`await` is not supported.

### Method Calls

Overload resolution follows ECMA-334 §12.6.4: candidate construction, applicability filtering, best function member selection with conversion ranking.

```csharp
"hello".ToUpper()                               // instance method
string.Join(", ", items)                         // static method
items.Where(x => x > 0)                         // extension method
Enumerable.Empty<int>()                          // generic method
string.Format(format: "{0}", arg0: "hi")         // named arguments
int.TryParse("42", out var n)                    // out parameter
int.TryParse("42", out _)                        // out discard
```

<!-- test: LangRef_Expr_MethodCall.csx -->

Generic type inference (§12.6.3) runs in phases: lower-bound inference from arguments, iterative fixing of type parameters, and output type inference from lambda return types. This is what makes `items.Select(x => x.Name)` work without specifying `<T, string>` explicitly.

### Object Creation

You can construct any type that's registered or available via fully qualified name. You cannot declare new types — no `class`, `struct`, `record`, or `enum` definitions.

```csharp
new List<int>()                                  // parameterless constructor
new DateTime(2024, 1, 1)                         // with arguments
new List<int> { 1, 2, 3 }                        // collection initializer
new { Name = "Alice", Age = 30 }                 // anonymous object
new Exception("msg") { Source = "test" }         // property initializer
new Dictionary<string, int> { ["a"] = 1 }        // indexer initializer
new[] { 1, 2, 3 }                                // implicitly typed array
new int[] { 1, 2, 3 }                            // explicitly typed array
new int[10]                                       // sized array
new int[3, 3]                                     // multi-dimensional
new int[,] { { 1, 2 }, { 3, 4 } }                // multi-dim with initializer
new int[3][]                                      // jagged array
```

<!-- test: LangRef_Expr_ObjectCreation.csx -->

Collection expression syntax `[1, 2, 3]` is Extended mode only. In Standard mode, use `new[] { 1, 2, 3 }`.

### Tuples

```csharp
(1, "hello")                        // unnamed tuple
(x: 1, y: 2)                        // named elements
var (a, b) = (10, 20)               // deconstruction
(1, 2) == (1, 2)                    // true — element-wise with numeric promotion
```

<!-- test: LangRef_Expr_Tuple.csx -->

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

`checked` and `unchecked` also support block form: `checked { statements }`. In a `checked` context, integer arithmetic uses overflow-checking operators. Floating-point is unaffected (IEEE 754).

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

Supports the full pattern matching system. `when` guards add boolean conditions. Arms are separated by commas.

### Throw Expression

`throw` works as an expression in null-coalescing, conditional, and switch arm contexts:

```csharp
x ?? throw new ArgumentNullException()
valid ? value : throw new Exception()
_ => throw new NotSupportedException()
```

<!-- test: LangRef_Expr_Throw.csx -->

### Member and Index Access

```csharp
obj.Property            // member access
obj?.Property           // null if obj is null, short-circuits
a?.b?.c                 // chained null-conditional
arr[0]                  // single index
arr?[0]                 // null-conditional index
matrix[1, 2]            // multi-dimensional index
arr[^1]                 // index from end
arr[1..3]               // range slice
```

When the binder knows the static type (via `SetVariable<T>` or type inference), member access resolves at bind time — producing `BoundPropertyAccessExpr` or `BoundFieldAccessExpr` nodes. When the type is `object`, the binder produces `BoundDynamicMemberAccessExpr` and resolution falls back to runtime reflection.

## Statements

Alder evaluates statement blocks delimited by semicolons. Use `return` to produce a value from a block. If omitted, the value of the last expression is returned implicitly — but explicit `return` is the recommended pattern.

### Variable Declarations

```csharp
var name = "Alice"                                              // inferred type
int count = 0                                                   // explicit type (keyword)
List<int> items = new List<int>()                               // explicit generic type
const double PI = 3.14159                                       // constant (§10.2.11)
int a = 1, b = 2, c = 3                                        // multiple declarations
var (x, y) = (10, 20)                                           // deconstruction
```

<!-- test: LangRef_Stmt_VarDecl.csx -->

`const` declarations enforce ECMA-334 §10.2.11: the initializer must be a constant expression, and implicit constant conversions apply (a non-negative `int` constant can implicitly convert to `uint`).

### Local Functions

```csharp
int Add(int a, int b) { return a + b; }
return Add(3, 4);   // 7
```

<!-- test: LangRef_Stmt_LocalFunc.csx -->

Parsed as a variable declaration with a lambda initializer. Return type can be explicit or `var`. These are local functions only — you cannot declare methods on types, and modifiers like `static`, `public`, or `async` are not supported.

### Control Flow

```csharp
if (cond) { } else if (cond) { } else { }
for (var i = 0; i < n; i++) { }
while (cond) { }
do { } while (cond);
foreach (var item in collection) { }
break;                  // exits innermost loop or switch
continue;               // skips to next iteration
return expr;            // exits with value
goto label;             // jumps to label
goto case value;        // within switch
goto default;           // within switch
label: statement        // jump target
```

<!-- test: LangRef_Stmt_ControlFlow.csx -->

`foreach` requires the source to implement `IEnumerable` or `IEnumerable<T>` (§13.9). `yield return` and `yield break` are not supported — Alder does not generate iterators.

### Switch Statement

```csharp
switch (expr)
{
    case 42:                                    // constant
        break;
    case string s when s.Contains("x"):         // type pattern + guard
        break;
    case > 100:                                 // relational pattern
        break;
    case { Length: > 0 }:                        // property pattern
        break;
    default:
        break;
}
```

<!-- test: LangRef_Stmt_Switch.csx -->

Implicit fall-through is not allowed — each case must end with `break`, `return`, `goto case`, or `goto default`. Explicit fall-through uses `goto case value;` or `goto default;`.

### Exception Handling

```csharp
try { }
catch (FormatException ex) { }                   // typed catch
catch (Exception ex) when (ex.Message != "") { }  // filtered catch
catch { }                                         // generic catch (must be last)
finally { }                                       // always runs
```

<!-- test: LangRef_Stmt_TryCatch.csx -->

Must have at least one `catch` or `finally`. `throw;` (parameterless) inside a catch block rethrows the current exception. Fully qualified exception types work: `catch (System.IO.IOException ex)`.

### Resource Management

```csharp
using (var r = expr) { }        // disposes r on exit
using (expr) { }                // no variable declared
lock (obj) { }                  // mutual exclusion
```

<!-- test: LangRef_Stmt_UsingLock.csx -->

`lock` requires a reference type — value types produce a diagnostic.

## Pattern Matching

Patterns appear in `is` expressions, `switch` expression arms, and `switch` statement cases. Alder implements the full ECMA-334 §11.2 pattern system.

### All Pattern Types

```csharp
// Constant
x is 42                              // exact value match
x is "hello"
x is null
x is true

// Type
x is int                             // runtime type test
x is string s                        // type test + variable binding
x is List<int> items                 // generic type
x is int?                            // nullable type

// Relational (§11.2.3)
x is > 0
x is <= 100
x is >= 0 and < 50

// Logical combinators (§11.2.5)
x is 1 or 2 or 3                     // OR
x is > 0 and < 100                   // AND
x is not null                        // NOT

// Property (§11.2.5)
x is { Length: > 0 }
x is string { Length: > 3 } s        // type + property + binding
x is { Count: > 0, Length: < 100 }   // multiple properties

// Var and discard
x is var v                           // always matches, binds value
x is _                               // always matches, no binding

// Positional / tuple (§11.2.6)
point is (0, 0)
point is (> 0, _)

// List (C# 11)
arr is [1, 2, 3]
arr is [1, .., 5]                    // slice — zero or more elements
arr is [_, ..var rest]               // slice with binding

// Parenthesized
x is (not null)                       // grouping for precedence
```

<!-- test: LangRef_Pattern_All.csx -->

### Pattern Precedence

From lowest to highest binding: `or` → `and` → `not` → relational → primary (constant, type, property, list, var, discard).

This matters: `x is not null and string` parses as `x is (not null) and string`, not `x is not (null and string)`.

## LINQ

### Method Syntax

All `System.Linq.Enumerable` extension methods are available by default. The full set:

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

### Query Syntax

LINQ query expressions desugar to method calls at parse time. The desugared result is indistinguishable from a hand-written method chain — the AST has no query-specific node types.

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
| `ulong` | `UInt64` | `dynamic` | `Object` (mapped at parse time) |
| `nint` | `IntPtr` | `nuint` | `UIntPtr` |

All support nullable syntax (`int?`, `bool?`) and can be used in `typeof()`, casts, `is`/`as`, and variable declarations.

### Type Syntax

```csharp
List<int>                                    // generic type
Dictionary<string, List<int>>                // nested generics
int[]                                        // array
int[,]                                       // multi-dimensional array
int[][]                                      // jagged array
int?                                         // nullable value type
System.Text.StringBuilder                    // fully qualified name
```

### Numeric Promotion

ECMA-334 §12.4.7.3 defines eight rules applied in order. Both operands of a binary operation are promoted to the first matching type:

1. If either is `decimal` → `decimal` (error if mixed with `float`/`double`)
2. If either is `double` → `double`
3. If either is `float` → `float`
4. If either is `ulong` → `ulong` (error if other is signed and not constant)
5. If either is `long` → `long`
6. If one is `uint` and other is `sbyte`/`short`/`int` → `long`
7. If either is `uint` → `uint`
8. Both promoted to `int`

`char` has implicit conversions to `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal` per §10.2.3. Notably, `uint + char` → `uint` (rule 7), not `long` (rule 6), because `char` is not in the `sbyte`/`short`/`int` set.

Constant expressions get additional promotion per §10.2.11: a non-negative `int` constant can implicitly convert to `uint` if the value fits.

Nullable arithmetic: if either operand is `null`, the result is `null`.

### Implicit Conversions

Alder implements the implicit conversions from ECMA-334 §10.2:

- **Numeric** (§10.2.3): `int` → `long`, `float` → `double`, etc.
- **Nullable** (§10.2.6): `T` → `T?`, and lifted conversions `T` → `U?` when `T` → `U` exists
- **Reference** (§10.2.8): inheritance, interface implementation, boxing
- **User-defined** (§10.5.4): `op_Implicit` methods on source and target type hierarchies

### Explicit Conversions

- **Numeric** (§10.3.2): narrowing conversions with overflow checking in `checked` context
- **Unboxing**: requires exact type — `(long)(object)42` throws because the boxed value is `int`
- **User-defined** (§10.5.5): `op_Explicit` methods

### Enum Arithmetic

```csharp
DayOfWeek.Monday + 1                   // DayOfWeek.Tuesday (Enum + int → Enum)
DayOfWeek.Wednesday - DayOfWeek.Monday  // 2 (Enum - Enum → underlying type)
~DayOfWeek.Monday                       // enum complement
```

### Default Available Types

| Namespace | Examples |
|-----------|---------|
| `System` | `Math`, `Convert`, `DateTime`, `Guid`, `Random`, `TimeSpan`, `Array`, `Tuple`, `StringComparison` |
| `System.Collections.Generic` | `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>` |
| `System.Linq` | All `Enumerable` extension methods |
| `System.Threading.Tasks` | `Task`, `Task<T>` |
| Via FQN | Any type from loaded assemblies: `new System.Text.StringBuilder()` |

Types outside default namespaces require fully qualified names or registration via `AlderOptions.Types.AddNamespace()`.

### Comments

```csharp
// single-line comment
/* multi-line
   comment */
```

## Scope Boundaries

These constructs are outside Alder's scope — they produce parse errors. Each row shows the recommended alternative:

| Not supported | Use instead |
|---------------|-------------|
| `class Foo { }` | Pass objects in via `SetVariable<T>`, or use anonymous objects: `new { Name = "x" }` |
| `struct`, `interface`, `enum`, `record` | Same — use existing types from the host application |
| `namespace` | Not needed — expressions don't define compilation units |
| `using System.IO;` | Register namespaces via `AlderOptions`: `o.Types.AddNamespace("System.IO")` |
| `async` / `await` | Not supported — call async methods from the host, pass results in as variables |
| `yield return` / `yield break` | Not supported — use LINQ to produce sequences |
| `ref` / `params` in declarations | Not supported — calling methods with `out` parameters works: `int.TryParse("42", out var n)` |
| `this` / `base` | No enclosing type — pass the instance in as a variable |
| `record with { }` | Not supported |
| `stackalloc` / `fixed` / `unsafe` | Not supported — low-level memory operations |
| `class Foo { public int X { get; } }` | Cannot declare types or members — can use existing ones from registered assemblies |
| `void Foo<T>(T x) { }` | Cannot declare generic types or methods — can call them: `Enumerable.Empty<int>()` |
| `[1, 2, 3]` | Extended mode only — in Standard mode use `new[] { 1, 2, 3 }` |
