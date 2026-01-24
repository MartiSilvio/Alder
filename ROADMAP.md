# CsEval Roadmap

Features for full C# developer familiarity, plus additions from other languages.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| 🔴 | Not implemented - High priority |
| 🟡 | Not implemented - Medium priority |
| 🔵 | Not implemented - Low priority |

---

## Operators

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| ✅ | Arithmetic | `+`, `-`, `*`, `/`, `%` | Standard operators |
| ✅ | Comparison | `==`, `!=`, `<`, `<=`, `>`, `>=` | |
| ✅ | Logical | `&&`, `\|\|`, `!` | Short-circuit evaluation |
| ✅ | Bitwise | `&`, `\|`, `^`, `~`, `<<`, `>>` | |
| ✅ | Ternary | `a ? b : c` | |
| ✅ | Null-coalescing | `??`, `??=` | |
| ✅ | Null-conditional | `?.` | Property access only |
| ✅ | Assignment | `=` | Variable reassignment |
| ✅ | Compound assignment | `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `\|=`, `^=`, `<<=`, `>>=` | All 10 operators |
| ✅ | Increment/decrement | `++x`, `x++`, `--x`, `x--` | Prefix and postfix |

---

## Control Flow

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| ✅ | Block expressions | `{ var x = 1; return x; }` | |
| ✅ | If statements | `if (cond) { } else { }` | |
| ✅ | Return | `return value;` | Early return in blocks |
| ✅ | While loop | `while (cond) { }` | With iteration limit |
| ✅ | For loop | `for (var i = 0; i < n; i++) { }` | |
| ✅ | Foreach loop | `foreach (var x in items) { }` | |
| ✅ | Do-while loop | `do { } while (cond)` | |
| ✅ | Break/continue | `break;`, `continue;` | In all loop types |
| ✅ | Switch statement | `switch (x) { case 1: ... }` | With fall-through and default |
| 🟡 | `switch` expression | `x switch { 1 => "one", _ => "other" }` | |
| 🔵 | `try-catch` | `try { } catch { }` | |
| 🔵 | `throw` | `throw new Exception("msg")` | |

---

## Variables & Types

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| ✅ | Variable declaration | `var x = 5;` | Type inferred |
| ✅ | Typed declaration | `int x = 5;` | `int`, `long`, `double`, `float`, `decimal`, `string`, `bool`, `object` |
| ✅ | Interpolated strings | `$"Hello {name}"` | |
| 🔴 | `is` operator | `x is string`, `x is null` | Type checking |
| 🔴 | `is not` | `x is not null` | Common pattern |
| 🔴 | `is` with variable | `x is string s` | Declare variable |
| 🔴 | `as` operator | `x as string` | Safe cast |
| 🔴 | Type casting | `(int)x` | |
| 🟡 | `nameof` | `nameof(property)` | |
| 🔵 | `default` | `default(int)` | |
| ✅ | Verbatim strings | `@"path\to\file"` | Backslashes literal |
| ✅ | Verbatim interpolated | `$@"path\{name}"`, `@$"..."` | Combined syntax |
| 🔵 | Raw strings | `"""text"""` | C# 11 |

---

## Collections & Objects

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| ✅ | Array literals | `[1, 2, 3]` | |
| ✅ | Anonymous objects | `new { Name = "John", Age = 30 }` | |
| ✅ | Object spread | `new { ...obj1, ...obj2 }` | |
| ✅ | Array spread | `[...arr1, ...arr2]` | |
| ✅ | Object merging | `obj1 + obj2` | Via `+` operator |
| ✅ | Index access | `arr[0]`, `dict["key"]` | Read and write |
| ✅ | Index assignment | `arr[0] = value` | Arrays, lists, dictionaries |
| ✅ | Property access | `obj.Property` | Read and write |
| ✅ | Property assignment | `obj.Prop = value` | Anonymous objects, typed objects |
| 🔴 | Typed constructor | `new DateTime(2024, 1, 1)` | Requires type registry |
| 🔴 | Object initializer | `new Point { X = 10, Y = 20 }` | |
| 🟡 | Constructor + initializer | `new Person("John") { Age = 30 }` | |
| 🔵 | Collection initializer | `new List<int> { 1, 2, 3 }` | Use `[1,2,3]` instead |
| 🔵 | Array creation | `new int[] { 1, 2, 3 }` | Use `[1,2,3]` instead |
| 🔵 | Generic type instantiation | `new List<int>()` | |
| 🟡 | Index from end | `arr[^1]` | C# 8+ |
| 🟡 | Range | `arr[1..3]` | C# 8+ |
| 🔵 | Destructuring | `var { Name, Age } = person` | |

---

## Pattern Matching

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| 🟡 | Property pattern | `x is { Name: "John" }` | |
| 🟡 | Relational pattern | `x is > 0 and < 100` | |

---

## LINQ

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| ✅ | Filtering | `Where`, `Distinct` | |
| ✅ | Projection | `Select`, `SelectMany` | |
| ✅ | Element | `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault` | |
| ✅ | Quantifiers | `Any`, `All`, `Contains` | |
| ✅ | Aggregation | `Count`, `Sum`, `Average`, `Min`, `Max`, `Aggregate` | |
| ✅ | Ordering | `OrderBy`, `OrderByDescending`, `Reverse` | |
| ✅ | Grouping | `GroupBy` | Returns `[{ Key, Items }]` |
| ✅ | Combining | `Zip`, `Concat` | |
| ✅ | Partitioning | `Take`, `Skip` | |
| ✅ | Set Operations | `Except`, `Intersect`, `Union` | |
| ✅ | Min/Max by Key | `MinBy`, `MaxBy` | .NET 6+ |
| ✅ | Conversion | `ToList`, `ToArray` | |
| 🔵 | `Join`, `GroupJoin` | | Complex |
| 🔵 | `TakeWhile`, `SkipWhile` | | |

---

## Security

| Status | Feature | Notes |
|:------:|---------|-------|
| ✅ | `MaxIterations` | Loop limit protection (100,000 default) |
| ✅ | Explicit module registration | No arbitrary namespace access |
| ✅ | No `new Type()` syntax | Can't instantiate arbitrary types |
| ✅ | `SandboxMode.Trusted` | Full access (default) |
| ✅ | `SandboxMode.Safe` | Blocks method calls on variables |
| ✅ | `SandboxMode.Strict` | Read-only mode (no mutations) |
| ✅ | Granular overrides | `AllowMethodCalls`, `AllowPropertyRead`, `AllowAssignment`, etc. |
| 🔴 | `AllowNewKeyword` | Enable/disable `new { }` syntax |
| 🟡 | `MaxStringLength` | Prevent string bombs (default: 1,000,000) |
| 🟡 | `MaxArrayLength` | Prevent memory exhaustion (default: 100,000) |
| 🟡 | `MaxRecursionDepth` | Prevent stack overflow (default: 100) |
| 🟡 | `BlockedTypes` | Type blacklist |
| 🔵 | `AllowedTypes` | Type whitelist (stricter than blocklist) |

---

## Infrastructure

| Status | Feature | Notes |
|:------:|---------|-------|
| ✅ | Pre-parsing | `engine.Parse()` for repeated evaluation |
| ✅ | Thread-safe contexts | `engine.CreateChild()` |
| ✅ | DI integration | `IServiceProvider` at evaluation time |
| ✅ | Async methods | `Task<T>` auto-unwrapped |
| ✅ | Cancellation | `CancellationToken` auto-passed |
| ✅ | Module system | `engine.RegisterModule()` |
| ✅ | Custom functions | `engine.RegisterFunction()` |
| ✅ | Sandbox modes | `Sandbox.Trusted()`, `Safe()`, `Strict()` presets |

---

## JavaScript Compatibility

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| ✅ | `let` | `let x = 5;` | Treated as `var` |
| ✅ | `undefined` | `undefined` | Maps to `null` |
| ✅ | Strict equality | `===`, `!==` | Same as `==`/`!=` |
| ✅ | Method aliases | `map`, `filter`, `reduce`, etc. | Maps to LINQ equivalents |
| 🟡 | Template literals | `` `Hello ${name}` `` | Backtick strings |
| 🟡 | `typeof` operator | `typeof x` | Returns type name string |
| 🔵 | `instanceof` | `x instanceof Y` | Like C# `is` |
| 🔵 | `console.log` | `console.log(x)` | Register as module |
| 🔵 | `JSON.stringify/parse` | `JSON.stringify(obj)` | Register as module |
| 🔵 | Destructuring | `const { a, b } = obj` | |

---

## Other Language Features

| Status | Feature | Syntax | Notes |
|:------:|---------|--------|-------|
| 🟡 | Optional chaining call | `obj?.Method()` | Currently only `?.Property` |
| 🔵 | Pipe operator | `x \|> Process \|> Format` | F#/Kotlin style |
| ✅ | `in` operator | `x in [1, 2, 3]` | Python style |
| 🔵 | Chained comparison | `0 < x < 100` | Python style |

---

## Not Implementing

| Feature | Rationale |
|---------|-----------|
| `typeof(T)` | Returns `System.Type` which is blocked by reflection security |
| `ToDictionary` | Redundant - use anonymous objects `new { Key = value }` |
| `OfType<T>`, `Cast<T>` | Requires runtime generic type parameters |
| `MethodFilter`, `MemberFilter` | Would require `MethodInfo`/`MemberInfo` which are blocked |
| Generic method calls | `list.Cast<int>()` requires runtime generic type resolution |
| Full C# compilation | Use Roslyn for that |
| Class/method definitions | Expressions only, not type definitions |
| LINQ query syntax | Method syntax only (`from x in y` → `y.Select()`) |
| Unsafe code / pointers | Security |
| Preprocessor (`#if`) | Not applicable to expressions |
| Static constructors | Class definition syntax, not expressions |
| Primary constructors (C# 12) | Class declaration syntax |
| Partial constructors (C# 14) | Class definition syntax |
| Constructor chaining (`:this()`, `:base()`) | Class definition syntax |

---

## JavaScript Method Aliases

| Status | JavaScript | LINQ Equivalent | Notes |
|:------:|------------|-----------------|-------|
| ✅ | `map` | `Select` | |
| ✅ | `filter` | `Where` | |
| ✅ | `reduce` | `Aggregate` | JS arg order: `reduce(fn, seed)` |
| ✅ | `flatMap` | `SelectMany` | |
| ✅ | `find` | `FirstOrDefault` | |
| ✅ | `some` | `Any` | |
| ✅ | `every` | `All` | |
| ✅ | `includes` | `Contains` | |

---

## Competitor Comparison

Feature parity check: does CsEval have what other .NET expression evaluators have?

### Libraries

| Library | License | Stars | Activity | NuGet |
|---------|---------|:-----:|:--------:|-------|
| **CsEval** | MIT | - | - | - |
| [NCalc2](https://github.com/ncalc/ncalc) | MIT | 2.2k | High | [NuGet](https://www.nuget.org/packages/NCalc2) |
| [Flee](https://github.com/mparlak/Flee) | LGPL | 1k | Low | [NuGet](https://www.nuget.org/packages/Flee) |
| [DynamicExpresso](https://github.com/dynamicexpresso/DynamicExpresso) | MIT | 1k | Medium | [NuGet](https://www.nuget.org/packages/DynamicExpresso.Core) |
| [ExpressionEvaluator](https://github.com/codingseb/ExpressionEvaluator) | MIT | 400 | Low | [NuGet](https://www.nuget.org/packages/CodingSeb.ExpressionEvaluator) |
| [Eval-Expression.NET](https://eval-expression.net/) | Commercial | - | High | [NuGet](https://www.nuget.org/packages/Z.Expressions.Eval) |

---

### How They Work Internally

| Library | Parser | Evaluation | Performance |
|---------|--------|------------|-------------|
| **CsEval** | Custom recursive descent | Visitor pattern on AST, optional Expression<> compilation | Interpreted (compiled for simple exprs) |
| **NCalc** | Parlot (parser combinator) | Visitor pattern on AST | Interpreted, optional lambda compilation |
| **Flee** | Grammatica (parser generator) | **Compiles to IL via DynamicMethod** | Fastest (1M+ evals/sec) |
| **DynamicExpresso** | Custom parser | **Compiles to Expression<> trees** | Fast (delegate invocation) |
| **ExpressionEvaluator** | Custom parser | Pure interpretation, no compilation | Slowest (re-evaluates every time) |
| **Eval-Expression.NET** | Unknown | Compiles to IL | Fast |

**Benchmark** (1M iterations of `cos(x)^0.5*cos(200*x)+abs(x)^0.5-0.7)*(4-x^2)^0.01`):
- Native C#: 194ms
- Flee: 377ms (2x slower)
- NCalc: 3,280ms (17x slower)

---

### Library Profiles

#### NCalc2
**Standout Feature**: Math-focused simplicity with built-in functions (Sin, Cos, Sqrt, etc.)

| Strengths | Weaknesses |
|-----------|------------|
| Simple API for math formulas | No C# syntax (custom expression language) |
| Active development (654 commits) | No bitwise operators |
| Async variant available | No method calls on objects |
| JSON serialization for AST | No LINQ, no lambdas |
| Expression caching via ConcurrentDictionary | Backslash handling bugs |

**Most Requested**: XOR operator, BigInteger support, position tracking in AST

**Common Complaints**:
- Strings with backslashes throw errors
- NaN in conditions evaluates incorrectly
- Non-English decimal separators (`,` vs `.`) fail
- AOT compilation crashes

---

#### Flee
**Standout Feature**: IL compilation via DynamicMethod - orders of magnitude faster than interpreters

| Strengths | Weaknesses |
|-----------|------------|
| Compiles to IL (garbage-collectible) | Last release March 2022 |
| 1M+ evaluations per second | LGPL license (commercial restrictions) |
| Strong typing like C# | No ternary operator |
| Power operator `^` built-in | No LINQ support |
| CalculationEngine for dependent expressions | Null variable handling broken |

**Most Requested**: Ternary operator, .NET 8 support, LINQ expressions, variable assignment

**Common Complaints**:
- TimeSpan property access fails
- Null values can't be added as variables
- String tokenization bugs
- No ternary operator (`? :`)
- Void method evaluation unsupported

---

#### DynamicExpresso
**Standout Feature**: Generates `Expression<>` trees for EF/LINQ provider integration

| Strengths | Weaknesses |
|-----------|------------|
| C# syntax subset | Lambda disabled by default (performance) |
| Compiles to delegates | Partial generic method support |
| LINQ provider integration | No anonymous object creation |
| ExpandoObject support (partial) | Limited dynamic type support |
| Type checking before execution | No pattern matching |

**Most Requested**: Null-coalescing patterns, named arguments, pattern matching, AOT support, anonymous objects

**Common Complaints**:
- Lambda parsing fails with generics
- Enumerable extensions don't work on ExpandoObject
- Implicit operator support gaps
- Threading issues with non-concurrent collections

---

#### ExpressionEvaluator (CodingSeb)
**Standout Feature**: Single-file library, full C# scripting (if/while/for), no dependencies

| Strengths | Weaknesses |
|-----------|------------|
| Full control flow (if, while, for, foreach) | Re-evaluates every time (no caching) |
| Single .cs file, no dependencies | Maintainer inactive ("no time to maintain") |
| Multiline lambda as method declarations | Performance penalty on repeated use |
| ExpandoObject support | Short-circuit `&&`/`||` broken |
| Dynamic `new()` syntax | Unity/il2cpp compatibility issues |

**Most Requested**: Named arguments, decimal support in math, strong naming, numeric parsing flexibility

**Common Complaints**:
- Nested ternary operators fail
- Short-circuit evaluation doesn't work like C#
- Method overload resolution fails with Func<> params
- Performance issues with cast operations

---

#### Eval-Expression.NET (Z.Expressions)
**Standout Feature**: Full C# compilation, Entity Framework integration, commercial support

| Strengths | Weaknesses |
|-----------|------------|
| Full C# syntax | **$499+ commercial license** |
| Compiles to IL | Free tier: 50 char limit |
| Entity Framework integration | Closed source |
| Dynamic LINQ methods | No iteration limits |
| Async support | No reflection blocking |

**Pricing**: Free for LINQ methods only. Execute/Compile limited to 50 chars. Monthly trial resets.

---

### Feature Comparison Tables

#### Core Expression Features

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| Arithmetic operators | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Comparison operators | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Logical operators | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Bitwise operators | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |
| Ternary `? :` | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| Null-coalescing `??` | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Null-conditional `?.` | ✅ | ❌ | ❌ | ⚠️ | ✅ | ✅ |
| Compound assignment | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Increment/decrement | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |

#### Variables & Types

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| Variable binding | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Typed variables | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ |
| Variable declaration | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Type casting `(int)x` | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ |
| `is` operator | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| `as` operator | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

#### Control Flow

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| If/else statements | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Switch statement | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| For loop | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| While loop | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Foreach loop | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Break/continue | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Try-catch | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |

#### Functions & Methods

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| Built-in functions | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| Custom functions | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Method calls on objects | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |
| Lambda expressions | ✅ | ❌ | ❌ | ⚠️ | ✅ | ✅ |
| LINQ methods | ✅¹ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Extension methods | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

#### Strings & Collections

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| String concatenation | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Interpolated strings | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Array literals | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Anonymous objects | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Index access `[]` | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |
| Property access | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |

#### Infrastructure

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| Pre-parsing/caching | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Compilation to IL | ⚠️³ | ⚠️ | ✅ | ✅ | ❌ | ✅ |
| Thread-safe | ⚠️² | ✅ | ⚠️ | ⚠️ | ⚠️ | ⚠️ |
| Case-insensitive option | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| DI integration | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Async support | ✅ | ⚠️ | ❌ | ❌ | ❌ | ✅ |
| CancellationToken | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

#### Security

| Feature | CsEval | NCalc | Flee | Expresso | ExprEval | Eval.NET |
|---------|:------:|:-----:|:----:|:--------:|:--------:|:--------:|
| Sandbox modes | ✅ | ❌ | ❌ | ❌ | ⚠️ | ✅ |
| Iteration limits | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Reflection blocking | ✅ | N/A | N/A | N/A | ⚠️ | ❌ |
| Type restrictions | ✅ | N/A | ⚠️ | ⚠️ | ⚠️ | ✅ |

### Legend

- ✅ Supported
- ⚠️ Partial/limited support
- ❌ Not supported
- N/A Not applicable (library doesn't expose objects)

### CsEval Notes

¹ **LINQ methods**: Built-in handlers for common LINQ operations (Where, Select, Sum, etc.), not actual System.Linq extension method resolution.

² **Thread-safe**: Engine itself is not thread-safe. Use `CreateChild()` for concurrent evaluation (same pattern as other libs).

³ **Compilation**: Compiles simple expressions to Expression<> delegates. Falls back to tree-walking for complex features (LINQ, control flow).

---

## Implementation Notes

### Type Registry for Constructors

Typed constructors require explicit type registration for security:

```csharp
engine.RegisterType<DateTime>("DateTime");
engine.RegisterType<Point>("Point");

// Then use in expressions:
engine.Evaluate("new DateTime(2024, 1, 1)");
engine.Evaluate("new Point { X = 10, Y = 20 }");
```

Only registered types can be instantiated (prevents arbitrary type creation).

### Benchmarking

See [docs/benchmarks.md](docs/benchmarks.md) for performance documentation.

```bash
cd benchmarks/CsEval.Benchmarks
dotnet run -c Release
```
