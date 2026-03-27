# Language Reference — Standard Mode

Alder implements C# expression and statement semantics per ECMA-334, with modern C# features through C# 11. This reference covers every construct available in `LanguageMode.Standard` — the default mode. Every construct listed here is verified against the parser and evaluator source code.

Alder is an expression evaluator. It evaluates expressions and statement blocks at runtime. It does not support type declarations (`class`, `struct`, `enum`, `record`, `interface`), namespace declarations, `using` directives, `async`/`await`, or `yield`. See [What Is Not Supported](#what-is-not-supported) for the complete list.

For Extended mode features (comparison chaining, pipeline operators, collection literals, built-in aggregates, and more), see [Extended Mode](extended.md).

## Literals

### Numeric Literals

| Form | Example | Type | Notes |
|------|---------|------|-------|
| Integer | `42` | `int` | Promotes: `int` → `uint` → `long` → `ulong` per ECMA-334 §6.4.5.3 |
| Hex | `0xFF`, `0XFF` | `int` | Same promotion rules |
| Binary | `0b1010`, `0B1010` | `int` | Same promotion rules |
| Leading decimal | `.5` | `double` | Dot followed by digit |
| Decimal point | `3.14` | `double` | |
| Scientific | `1e10`, `1.5E-3` | `double` | `e`/`E` with optional `+`/`-` sign |
| Long suffix | `42L`, `42l` | `long` | Promotes: `long` → `ulong` if value requires it |
| Unsigned suffix | `42U`, `42u` | `uint` | Promotes: `uint` → `ulong` if value requires it |
| Unsigned long | `42UL`, `42ul`, `42LU`, `42lu` | `ulong` | |
| Float suffix | `3.14f`, `3.14F` | `float` | |
| Double suffix | `3.14d`, `3.14D` | `double` | |
| Decimal suffix | `3.14m`, `3.14M` | `decimal` | |
| Digit separators | `1_000_000` | (per base) | Anywhere within digits; trailing `_` is an error |

<!-- test: LangRef_Literal_Numeric.csx -->

Hex and binary literals support `L`, `U`, `UL` suffixes. They do not support `F`, `D`, or `M`.

### String Literals

| Form | Example | Notes |
|------|---------|-------|
| Regular | `"hello\nworld"` | Escape sequences processed |
| Verbatim | `@"C:\Users\path"` | No escapes; `""` for literal quote |
| Raw (C# 11) | `"""raw content"""` | 3+ quote delimiters; no escapes |
| Interpolated | `$"Hello {name}"` | Expressions in `{}`; alignment and format specifiers |
| Verbatim interpolated | `$@"C:\{folder}"` or `@$"C:\{folder}"` | Both prefix orderings |
| Raw interpolated | `$"""Hello {name}"""` | Raw string with interpolation holes |

<!-- test: LangRef_Literal_String.csx -->

### Interpolation Specifiers

| Form | Example | Description |
|------|---------|-------------|
| Expression | `{expr}` | `$"{name}"` |
| Alignment | `{expr,width}` | `$"{"right",10}"` — right-padded to 10 |
| Format | `{expr:fmt}` | `$"{Math.PI:F4}"` — 4 decimal places |
| Both | `{expr,width:fmt}` | `$"{42,-10:X}"` — left-aligned hex |

`{{` and `}}` produce literal braces.

### Escape Sequences

Available in regular strings, interpolated strings, and character literals:

| Sequence | Character | Sequence | Character |
|----------|-----------|----------|-----------|
| `\n` | Newline | `\a` | Alert |
| `\r` | Carriage return | `\b` | Backspace |
| `\t` | Tab | `\f` | Form feed |
| `\0` | Null | `\v` | Vertical tab |
| `\\` | Backslash | `\uXXXX` | Unicode (4 hex) |
| `\"` | Double quote | `\UXXXXXXXX` | Unicode (8 hex, up to U+10FFFF) |
| `\'` | Single quote | `\xN..NNNN` | Hex (1–4 hex digits) |

### Character Literals

| Example | Notes |
|---------|-------|
| `'a'` | Single character |
| `'\n'` | Escape sequence |
| `'\u0041'` | Unicode escape (`A`) |

<!-- test: LangRef_Literal_Char.csx -->

Empty character literals (`''`) produce a parse error.

### Boolean and Null

| Literal | Type |
|---------|------|
| `true` | `bool` |
| `false` | `bool` |
| `null` | null reference |

## Operators

### Precedence Table (highest binding to lowest)

| Level | Category | Operators | Assoc. |
|-------|----------|-----------|--------|
| 14 | Primary | `x.y` `x?.y` `x[i]` `x?[i]` `f(x)` `x++` `x--` `new` `typeof` `sizeof` `nameof` `default` `checked` `unchecked` | Left |
| 13 | Unary | `-x` `+x` `!x` `~x` `^x` `++x` `--x` `(T)x` `throw` | Right |
| 12 | Range | `..` | — |
| 11 | Multiplicative | `*` `/` `%` | Left |
| 10 | Additive | `+` `-` | Left |
| 9 | Shift | `<<` `>>` `>>>` | Left |
| 8 | Relational | `<` `<=` `>` `>=` `is` `as` | Left |
| 7 | Equality | `==` `!=` | Left |
| 6 | Bitwise AND | `&` | Left |
| 5 | Bitwise XOR | `^` | Left |
| 4 | Bitwise OR | `\|` | Left |
| 3 | Logical AND | `&&` | Left |
| 2 | Logical OR | `\|\|` | Left |
| 1 | Null-coalescing | `??` | Right |
| 0 | Conditional | `? :` | Right |
| — | Assignment | `=` `+=` `-=` `*=` `/=` `%=` `&=` `\|=` `^=` `<<=` `>>=` `>>>=` `??=` | Right |

### Arithmetic Operators

| Operator | Name | Types | Additional behaviors |
|----------|------|-------|---------------------|
| `+` | Addition | All numeric | String concatenation, `DateTime` + `TimeSpan`, delegate combination |
| `-` | Subtraction | All numeric | `DateTime` - `DateTime` → `TimeSpan`, `DateTime` - `TimeSpan` |
| `*` | Multiplication | All numeric | |
| `/` | Division | All numeric | Integer division truncates; floating-point divides normally |
| `%` | Modulo | All numeric | |

<!-- test: LangRef_Op_Arithmetic.csx -->

### Comparison Operators

| Operator | Name |
|----------|------|
| `==` | Equality |
| `!=` | Inequality |
| `<` | Less than |
| `<=` | Less than or equal |
| `>` | Greater than |
| `>=` | Greater than or equal |

<!-- test: LangRef_Op_Comparison.csx -->

Tuple equality is element-wise with numeric promotion. Numeric comparisons follow ECMA-334 binary numeric promotion.

### Logical Operators

| Operator | Name | Notes |
|----------|------|-------|
| `&&` | Logical AND | Short-circuits: right not evaluated if left is false |
| `\|\|` | Logical OR | Short-circuits: right not evaluated if left is true |
| `!` | Logical NOT | Unary prefix |

<!-- test: LangRef_Op_Logical.csx -->

### Bitwise Operators

| Operator | Name | Notes |
|----------|------|-------|
| `&` | Bitwise AND | Also: non-short-circuit `bool` AND, three-valued `bool?` logic |
| `\|` | Bitwise OR | Also: non-short-circuit `bool` OR, three-valued `bool?` logic |
| `^` | Bitwise XOR | Also: `bool` XOR |
| `~` | Bitwise NOT | Also: enum complement |
| `<<` | Left shift | |
| `>>` | Right shift | Arithmetic (sign-extending) |
| `>>>` | Unsigned right shift | Zero-filling |

<!-- test: LangRef_Op_Bitwise.csx -->

### Null Operators

| Operator | Name | Description |
|----------|------|-------------|
| `??` | Null-coalescing | Returns left if non-null, otherwise right |
| `??=` | Null-coalescing assignment | Assigns right to left if left is null |
| `?.` | Null-conditional member | Returns null if target is null; short-circuits chain |
| `?[` | Null-conditional index | Returns null if target is null; short-circuits chain |

<!-- test: LangRef_Op_Null.csx -->

### Assignment Operators

| Operator | Equivalent |
|----------|------------|
| `=` | Simple assignment |
| `+=` | `x = x + value` |
| `-=` | `x = x - value` |
| `*=` | `x = x * value` |
| `/=` | `x = x / value` |
| `%=` | `x = x % value` |
| `&=` | `x = x & value` |
| `\|=` | `x = x \| value` |
| `^=` | `x = x ^ value` |
| `<<=` | `x = x << value` |
| `>>=` | `x = x >> value` |
| `>>>=` | `x = x >>> value` |

Assignment targets: variables (`x`), properties (`obj.Name`), indexers (`arr[0]`), multi-dimensional indexers (`matrix[1, 2]`).

<!-- test: LangRef_Op_Assignment.csx -->

### Increment and Decrement

| Form | Example | Behavior |
|------|---------|----------|
| Prefix `++` | `++i` | Increments, returns new value |
| Postfix `++` | `i++` | Returns current value, then increments |
| Prefix `--` | `--i` | Decrements, returns new value |
| Postfix `--` | `i--` | Returns current value, then decrements |

Targets: variables (`++x`), member access (`obj.Count++`), index access (`arr[0]++`).

<!-- test: LangRef_Op_Increment.csx -->

### Type Testing

| Operator | Example | Description |
|----------|---------|-------------|
| `is` | `x is string` | Type test; supports full pattern matching |
| `is` with binding | `x is string s` | Type test + variable binding |
| `as` | `x as string` | Safe cast; returns null on failure |
| `(T)x` | `(int)x` | Explicit cast; throws on failure |

<!-- test: LangRef_Op_TypeTest.csx -->

### Index and Range

| Operator | Example | Description |
|----------|---------|-------------|
| `^n` | `arr[^1]` | Index from end (`^1` = last element) |
| `start..end` | `arr[1..3]` | Range (exclusive end) |
| `..end` | `arr[..2]` | Range from start |
| `start..` | `arr[3..]` | Range to end |

<!-- test: LangRef_Op_Range.csx -->

### User-Defined Operators

When built-in operators don't match the operand types, Alder searches both operand types via reflection for operator methods: `op_Addition`, `op_Subtraction`, `op_Multiply`, `op_Division`, `op_Modulus`, `op_Equality`, `op_Inequality`, `op_LessThan`, `op_LessThanOrEqual`, `op_GreaterThan`, `op_GreaterThanOrEqual`, `op_BitwiseAnd`, `op_BitwiseOr`, `op_ExclusiveOr`. Checked variants (e.g., `op_CheckedAddition`) are tried first in a `checked` context.

## Expressions

### Conditional (Ternary)

```csharp
score >= 90 ? "A" : score >= 80 ? "B" : "C"
```
<!-- test: LangRef_Expr_Ternary.csx -->

Right-associative. Nested ternary chains from right: `a ? b : c ? d : e` parses as `a ? b : (c ? d : e)`.

### Lambda Expressions

| Form | Example |
|------|---------|
| Expression body, inferred params | `x => x * 2` |
| Expression body, typed params | `(int x) => x * 2` |
| Multi-param | `(a, b) => a + b` |
| Parameterless | `() => 42` |
| Block body | `(int n) => { var r = 1; for (var i = 2; i <= n; i++) r *= i; return r; }` |

<!-- test: LangRef_Expr_Lambda.csx -->

Parameter types can be omitted when inferred from context (LINQ methods, delegate-typed parameters).

### Method Calls

| Form | Example |
|------|---------|
| Instance method | `"hello".ToUpper()` |
| Static method | `string.Join(", ", items)` |
| Extension method | `items.Where(x => x > 0)` |
| Generic method | `Enumerable.Empty<int>()` |
| Named argument | `string.Format(format: "{0}", arg0: "hi")` |
| `out` parameter | `int.TryParse("42", out var n)` |
| `out` discard | `int.TryParse("42", out _)` |
| `out` with type | `int.TryParse("42", out int n)` |

<!-- test: LangRef_Expr_MethodCall.csx -->

Overload resolution follows ECMA-334 §12.6.4, including generic type inference.

### Object Creation

| Form | Example |
|------|---------|
| Parameterless constructor | `new List<int>()` |
| With arguments | `new DateTime(2024, 1, 1)` |
| Collection initializer | `new List<int> { 1, 2, 3 }` |
| Anonymous object | `new { Name = "Alice", Age = 30 }` |
| Property initializer | `new Exception("msg") { Source = "test" }` |
| Indexer initializer | `new Dictionary<string, int> { ["a"] = 1 }` |
| Without parens (with initializer) | `new List<int> { 1, 2, 3 }` |
| Implicitly typed array | `new[] { 1, 2, 3 }` |
| Explicitly typed array | `new int[] { 1, 2, 3 }` |
| Sized array | `new int[10]` |
| Multi-dimensional array | `new int[3, 3]` |
| Multi-dim with initializer | `new int[,] { { 1, 2 }, { 3, 4 } }` |
| Jagged array | `new int[3][]` |

<!-- test: LangRef_Expr_ObjectCreation.csx -->

### Tuples

| Form | Example |
|------|---------|
| Unnamed | `(1, "hello")` |
| Named elements | `(x: 1, y: 2)` |
| Deconstruction | `var (a, b) = (10, 20)` |
| Element-wise equality | `(1, 2) == (1, 2)` → `true` |

<!-- test: LangRef_Expr_Tuple.csx -->

### Introspection

| Expression | Example | Result |
|------------|---------|--------|
| `typeof(T)` | `typeof(int)` | `System.Int32` |
| `typeof` with generics | `typeof(List<int>)` | `System.Collections.Generic.List'1[System.Int32]` |
| `sizeof(T)` | `sizeof(int)` | `4` |
| `nameof(x)` | `nameof(myVar)` | `"myVar"` |
| `nameof` with member | `nameof(string.Length)` | `"Length"` |
| `default(T)` | `default(int)` | `0` |
| `default` (contextual) | `default` | Context-dependent default |
| `checked(expr)` | `checked(int.MaxValue + 1)` | Throws `OverflowException` |
| `unchecked(expr)` | `unchecked(int.MaxValue + 1)` | Wraps to `int.MinValue` |

<!-- test: LangRef_Expr_Introspection.csx -->

`checked` and `unchecked` also support block form: `checked { statements }`.

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

Supports the full pattern matching system (see [Pattern Matching](#pattern-matching)). `when` guards add boolean conditions. Arms are separated by commas.

### Throw Expression

| Context | Example |
|---------|---------|
| Null-coalescing | `x ?? throw new ArgumentNullException()` |
| Conditional | `valid ? value : throw new Exception()` |
| Switch arm | `_ => throw new NotSupportedException()` |

<!-- test: LangRef_Expr_Throw.csx -->

### Member Access

| Form | Example | Description |
|------|---------|-------------|
| Direct | `obj.Property` | Member access |
| Null-conditional | `obj?.Property` | Returns null if `obj` is null |
| Chained | `a.b.c.d` | Left-to-right |
| Null-conditional chained | `a?.b?.c` | Short-circuits on first null |

### Index Access

| Form | Example | Description |
|------|---------|-------------|
| Single index | `arr[0]` | Array, list, dictionary index |
| Null-conditional | `arr?[0]` | Returns null if target is null |
| Multi-dimensional | `matrix[1, 2]` | Multi-dim array access |
| Index from end | `arr[^1]` | Last element |
| Range | `arr[1..3]` | Slice (exclusive end) |

## Statements

Multi-statement logic uses semicolons between statements. The last expression is the return value, or use explicit `return`.

### Variable Declarations

| Form | Example |
|------|---------|
| Inferred type | `var name = "Alice";` |
| Explicit type (keyword) | `int count = 0;` |
| Explicit type (generic) | `List<int> items = new List<int>();` |
| Explicit type (FQN) | `System.Text.StringBuilder sb = new System.Text.StringBuilder();` |
| Multiple declarations | `int a = 1, b = 2, c = 3;` |
| Constant | `const double PI = 3.14159;` |
| Deconstruction | `var (x, y) = (10, 20);` |

<!-- test: LangRef_Stmt_VarDecl.csx -->

### Local Functions

```csharp
int Add(int a, int b) { return a + b; }
return Add(3, 4);
```
<!-- test: LangRef_Stmt_LocalFunc.csx -->

Parsed as a `VariableDeclExpr` with a `LambdaExpr` initializer. Return type can be explicit or `var`.

### Control Flow

| Statement | Syntax |
|-----------|--------|
| `if` / `else` | `if (cond) { } else if (cond) { } else { }` |
| `for` | `for (var i = 0; i < n; i++) { }` |
| `while` | `while (cond) { }` |
| `do...while` | `do { } while (cond);` |
| `foreach` | `foreach (var item in collection) { }` |
| `break` | `break;` — exits innermost loop or switch |
| `continue` | `continue;` — skips to next iteration |
| `return` | `return expr;` — exits with value. `return;` for void. |
| `goto` | `goto label;` — jumps to label |
| `goto case` | `goto case value;` — within switch |
| `goto default` | `goto default;` — within switch |
| Label | `label:` — jump target for `goto` |

<!-- test: LangRef_Stmt_ControlFlow.csx -->

### Switch Statement

```csharp
switch (expr)
{
    case pattern when guard:
        // statements
        break;
    default:
        break;
}
```

| Feature | Example |
|---------|---------|
| Constant case | `case 42:`, `case "hello":`, `case null:` |
| Type pattern | `case int n:`, `case string s:` |
| Relational pattern | `case > 100:`, `case >= 0 and < 50:` |
| Property pattern | `case { Length: > 0 }:` |
| `when` guard | `case string s when s.Contains("x"):` |
| `goto case` | `goto case 1;` |
| `goto default` | `goto default;` |
| Fall-through (explicit) | Via `goto case` / `goto default` only |

<!-- test: LangRef_Stmt_Switch.csx -->

### Exception Handling

| Statement | Syntax |
|-----------|--------|
| `try`/`catch`/`finally` | `try { } catch (ExType ex) { } finally { }` |
| Typed catch | `catch (FormatException ex) { }` |
| Filtered catch | `catch (Exception ex) when (condition) { }` |
| Generic catch | `catch { }` |
| Rethrow | `throw;` — inside catch block |
| FQN exception type | `catch (System.IO.IOException ex) { }` |

<!-- test: LangRef_Stmt_TryCatch.csx -->

Must have at least one `catch` or `finally`. Generic catch (no type) must be the last catch clause.

### Resource Management

| Statement | Syntax | Description |
|-----------|--------|-------------|
| `using` | `using (var r = expr) { }` | Disposes `r` on exit |
| `using` (expression) | `using (expr) { }` | No variable declared |
| `lock` | `lock (obj) { }` | Mutual exclusion |

<!-- test: LangRef_Stmt_UsingLock.csx -->

## Pattern Matching

Patterns appear in three contexts:

| Context | Syntax |
|---------|--------|
| `is` expression | `expr is pattern` |
| Switch expression arm | `expr switch { pattern => value }` |
| Switch statement case | `case pattern when guard:` |

### All Pattern Types

| Pattern | Syntax | Example | Description |
|---------|--------|---------|-------------|
| Constant | `value` | `is 42`, `is "hello"`, `is null`, `is true` | Exact value match |
| Type | `Type` | `is int`, `is string`, `is List<int>` | Runtime type test |
| Type + binding | `Type name` | `is string s`, `is Exception ex` | Type test + variable binding |
| Nullable type | `Type?` | `is int?` | Nullable type test |
| FQN type | `Namespace.Type` | `is System.IO.IOException` | Fully qualified type |
| Generic type | `Type<T>` | `is List<int> items` | Generic type test |
| Relational | `op value` | `is > 0`, `is <= 100`, `is >= 0` | Compare with `<`, `<=`, `>`, `>=` |
| AND | `p and p` | `is > 0 and < 100` | Both patterns must match |
| OR | `p or p` | `is 1 or 2 or 3` | Either pattern must match |
| NOT | `not p` | `is not null`, `is not 0` | Pattern negation |
| Property | `{ Prop: p }` | `is { Length: > 0 }` | Test property values |
| Property + type | `Type { Prop: p }` | `is string { Length: > 3 }` | Type + property |
| Property + binding | `Type { Props } name` | `is Exception { Message: "err" } ex` | Full binding |
| Multiple properties | `{ P1: p, P2: p }` | `is { Count: > 0, Length: < 100 }` | All must match |
| Var | `var name` | `is var x` | Always matches, binds value |
| Discard | `_` | `is _` | Always matches, no binding |
| Parenthesized | `(p)` | `is (not null)` | Grouping for precedence |
| Positional (tuple) | `(p, p)` | `is (0, 0)`, `is (> 0, _)` | Tuple element matching |
| List (C# 11) | `[p, p, ...]` | `is [1, 2, 3]` | List/array element matching |
| Slice (C# 11) | `..` | `is [1, .., 5]` | Zero or more elements |
| Slice + binding | `..var rest` | `is [_, ..var rest]` | Bind remaining elements |

<!-- test: LangRef_Pattern_All.csx -->

### Pattern Precedence (lowest to highest binding)

| Level | Pattern | Example |
|-------|---------|---------|
| 1 | `or` | `is 1 or 2 or 3` |
| 2 | `and` | `is > 0 and < 100` |
| 3 | `not` | `is not null` |
| 4 | Relational | `is > 0`, `is <= 100` |
| 5 | Primary | All others: constant, type, property, list, var, discard |

## LINQ

### Method Syntax

All `System.Linq.Enumerable` extension methods are available by default:

| Category | Methods |
|----------|---------|
| Filtering | `Where` |
| Projection | `Select`, `SelectMany` |
| Ordering | `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Reverse` |
| Grouping | `GroupBy` |
| Joining | `Join`, `GroupJoin` |
| Set | `Distinct`, `Union`, `Intersect`, `Except` |
| Partitioning | `Skip`, `Take`, `SkipWhile`, `TakeWhile` |
| Element | `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault`, `ElementAt`, `ElementAtOrDefault` |
| Aggregation | `Count`, `LongCount`, `Sum`, `Average`, `Min`, `Max`, `Aggregate` |
| Quantifiers | `Any`, `All`, `Contains` |
| Concatenation | `Concat`, `Zip`, `Append`, `Prepend` |
| Conversion | `ToList`, `ToArray`, `ToDictionary`, `ToHashSet`, `ToLookup`, `Cast`, `OfType`, `AsEnumerable` |
| Generation | `Enumerable.Range`, `Enumerable.Repeat`, `Enumerable.Empty<T>` |
| Other | `DefaultIfEmpty`, `SequenceEqual` |

<!-- test: LangRef_Linq_Methods.csx -->

### Query Syntax

LINQ query expressions are desugared at parse time into method calls:

| Clause | Syntax | Desugars to |
|--------|--------|-------------|
| Source | `from x in source` | Initial source |
| Filter | `where predicate` | `.Where(x => predicate)` |
| Projection | `select projection` | `.Select(x => projection)` |
| Order ascending | `orderby key` | `.OrderBy(x => key)` |
| Order descending | `orderby key descending` | `.OrderByDescending(x => key)` |
| Multiple orderings | `orderby k1, k2` | `.OrderBy(..).ThenBy(..)` |
| Let binding | `let v = expr` | `.Select(x => new { x, v = expr })` |
| Group | `group elem by key` | `.GroupBy(x => key, x => elem)` |
| Inner join | `join y in s on k1 equals k2` | `.Join(s, x => k1, y => k2, ..)` |
| Group join | `join y in s on k1 equals k2 into g` | `.GroupJoin(..)` |
| Cross join | Multiple `from` clauses | `.SelectMany(..)` |
| Continuation | `into name` | New query over prior result |

<!-- test: LangRef_Linq_Query.csx -->

Transparent identifier nesting is fully implemented for multi-clause queries with `let`, multiple `from`, and `join`.

## Type System

### Type Keywords

| Keyword | CLR Type | Keyword | CLR Type |
|---------|----------|---------|----------|
| `sbyte` | `System.SByte` | `float` | `System.Single` |
| `byte` | `System.Byte` | `double` | `System.Double` |
| `short` | `System.Int16` | `decimal` | `System.Decimal` |
| `ushort` | `System.UInt16` | `bool` | `System.Boolean` |
| `int` | `System.Int32` | `char` | `System.Char` |
| `uint` | `System.UInt32` | `string` | `System.String` |
| `long` | `System.Int64` | `object` | `System.Object` |
| `ulong` | `System.UInt64` | `dynamic` | `System.Object` (mapped at parse time) |
| `nint` | `System.IntPtr` | `nuint` | `System.UIntPtr` |
| `void` | `System.Void` (for `typeof` only) | | |

All support nullable syntax (`int?`, `bool?`) and can be used in `typeof()`, casts, `is`/`as`, and variable declarations.

### Type Syntax

| Feature | Example |
|---------|---------|
| Generic types | `List<int>`, `Dictionary<string, List<int>>` |
| Array types | `int[]`, `int[,]`, `int[][]` |
| Nullable | `int?`, `string?` |
| Fully qualified | `System.Text.StringBuilder`, `System.IO.MemoryStream` |

### Default Available Types

| Namespace | Examples |
|-----------|---------|
| `System` | `Math`, `Convert`, `DateTime`, `Guid`, `Random`, `TimeSpan`, `Array`, `Tuple`, `StringComparison` |
| `System.Collections.Generic` | `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, `Stack<T>` |
| `System.Linq` | All `Enumerable` extension methods |
| `System.Threading.Tasks` | `Task`, `Task<T>` |
| Via FQN | Any type from loaded assemblies: `new System.Text.StringBuilder()` |

Types outside default namespaces require either fully qualified names or `o.Types.AddNamespace(...)`.

### Numeric Promotion

| Operand combination | Result type |
|---------------------|-------------|
| Any + `decimal` | `decimal` (error if mixed with `float`/`double`) |
| Any + `double` | `double` |
| Any + `float` | `float` |
| Any + `ulong` | `ulong` |
| Any + `long` | `long` |
| `uint` + signed type | `long` |
| Both `int` or smaller | `int` |

Nullable arithmetic: if either operand is `null`, the result is `null`.

### Enum Arithmetic

| Operation | Result |
|-----------|--------|
| `Enum + int` | `Enum` |
| `Enum - int` | `Enum` |
| `Enum - Enum` | Underlying type |
| `Enum & Enum`, `Enum \| Enum`, `Enum ^ Enum` | `Enum` |
| `~Enum` | `Enum` |

### Comments

| Form | Syntax |
|------|--------|
| Single-line | `// comment` |
| Multi-line | `/* comment */` |

## What Is Not Supported

Alder is an expression evaluator, not a full C# compiler. These features are not available:

| Feature | What happens |
|---------|-------------|
| `class`, `struct`, `interface`, `enum`, `record` declarations | CS1525: reserved keyword, parser error |
| `namespace` declarations | Not parsed |
| `using` directives (not `using` statement) | Not parsed — use `o.Types.AddNamespace()` |
| `async` / `await` | CS1525: reserved keyword, parser error |
| `yield return` / `yield break` | `yield` is a contextual keyword, not parsed as a statement |
| `ref` parameter modifier | CS1525: reserved keyword, parser error |
| `params` array modifier | CS1525: reserved keyword, parser error |
| `this` / `base` | CS1525: reserved keyword, parser error |
| `with` expression (`record with { }`) | `with` is a contextual keyword, not parsed as an expression operator |
| `stackalloc` | CS1525: reserved keyword, parser error |
| `fixed` statement | CS1525: reserved keyword, parser error |
| `unsafe` block | CS1525: reserved keyword, parser error |
| Operator overloading declarations | Cannot declare — can call existing user-defined operators |
| Indexer / property declarations | Cannot declare — can use indexers on objects |
| Generic type / method declarations | Cannot declare — can use generic types and call generic methods |
| Collection expression `[1, 2, 3]` | Extended mode only (Standard uses `new[] { 1, 2, 3 }`) |
