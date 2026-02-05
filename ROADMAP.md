# CsEval Roadmap

Features for full C# developer familiarity, plus additions from other languages.

## Legend

| Symbol | Meaning                           |
| ------ | --------------------------------- |
| ✅     | Implemented                       |
| 🔴     | Not implemented - High priority   |
| 🟡     | Not implemented - Medium priority |
| 🔵     | Not implemented - Low priority    |

**Execution Modes:**

- **AST**: Tree-walking interpretation (feature-complete, baseline performance)
- **IL**: Compiled to IL via Expression Trees (faster, near-complete feature parity)

Features marked ✅ in both AST and IL columns are fully optimized. Features with ✅ AST but ❌ IL will silently fall back to tree-walking (or throw in `StrictCompiled` mode). Most features are now IL-compiled.

---

## Operators

| Status | Feature                | Syntax                           | AST | IL  | Notes                       |
| :----: | ---------------------- | -------------------------------- | :-: | :-: | --------------------------- |
|   ✅   | Arithmetic             | `+`, `-`, `*`, `/`, `%`          | ✅  | ✅  | Standard operators          |
|   ✅   | Comparison             | `==`, `!=`, `<`, `<=`, `>`, `>=` | ✅  | ✅  |                             |
|   ✅   | Logical                | `&&`, `\|\|`, `!`                | ✅  | ✅  | Short-circuit evaluation    |
|   ✅   | Bitwise AND/OR/XOR     | `&`, `\|`, `^`                   | ✅  | ✅  |                             |
|   ✅   | Bitwise NOT            | `~`                              | ✅  | ✅  |                             |
|   ✅   | Shift                  | `<<`, `>>`                       | ✅  | ✅  |                             |
|   ✅   | Ternary                | `a ? b : c`                      | ✅  | ✅  |                             |
|   ✅   | Null-coalescing        | `??`                             | ✅  | ✅  |                             |
|   ✅   | Null-coalescing assign | `??=`                            | ✅  | ✅  |                             |
|   ✅   | Null-conditional       | `?.`                             | ✅  | ✅  | Property access only        |
|   🟡   | Null-conditional index | `arr?[0]`                        | ❌  | ❌  | C# 6+                       |
|   ✅   | Assignment             | `=`                              | ✅  | ✅  | Variable reassignment       |
|   ✅   | Compound arithmetic    | `+=`, `-=`, `*=`, `/=`, `%=`     | ✅  | ✅  |                             |
|   ✅   | Compound bitwise       | `&=`, `\|=`, `^=`, `<<=`, `>>=`  | ✅  | ✅  |                             |
|   ✅   | Increment/decrement    | `++x`, `x++`, `--x`, `x--`       | ✅  | ✅  | Prefix and postfix          |
|   ✅   | Unary plus             | `+x`                             | ✅  | ✅  | Identity operator           |
|   ❌   | Containment            | `x in list`                      | ❌  | ❌  | Removed - use `.Contains()` |
|   🔵   | Null-forgiving         | `x!`                             | ❌  | ❌  | C# 8+                       |
|   🔵   | Unsigned shift         | `x >>> y`                        | ❌  | ❌  | C# 11                       |

---

## Literals

| Status | Feature           | Syntax                       | AST | IL  | Notes                          |
| :----: | ----------------- | ---------------------------- | :-: | :-: | ------------------------------ |
|   ✅   | Numeric literals  | `42`, `3.14`, `42L`, `3.14m` | ✅  | ✅  | int, long, double, decimal     |
|   ✅   | String literals   | `"hello"`                    | ✅  | ✅  |                                |
|   ✅   | Boolean literals  | `true`, `false`              | ✅  | ✅  |                                |
|   ✅   | Null literal      | `null`                       | ✅  | ✅  |                                |
|   ✅   | Char literals     | `'a'`, `'\t'`, `'\n'`        | ✅  | ✅  | Single quotes for characters   |
|   ✅   | Hex literals      | `0xFF`, `0x1A`               | ✅  | ✅  | Hexadecimal integers           |
|   ✅   | Binary literals   | `0b1010`                     | ✅  | ✅  | C# 7.0                         |
|   ✅   | Digit separators  | `1_000_000`, `0xFF_FF`       | ✅  | ✅  | C# 7.0                         |
|   ✅   | Unicode escapes   | `'\u0041'`, `"\u0048\u0069"` | ✅  | ✅  | In char and string literals    |
|   ✅   | Escape sequences  | `\t`, `\n`, `\r`, `\\`       | ✅  | ✅  | In string literals             |
|   ✅   | More escapes      | `\0`, `\a`, `\b`, `\f`, `\v` | ✅  | ✅  | Additional C# escape sequences |
|   ✅   | Exponent notation | `1e10`, `1.5E-3`, `3e+5`     | ✅  | ✅  | Scientific notation            |
|   ✅   | Hex escape        | `'\x41'`, `"\x48\x69"`       | ✅  | ✅  | 1-4 hex digits                 |
|   🟡   | Leading decimal   | `.5`, `.123`                 | ❌  | ❌  | Requires `0.5` currently       |
|   🔵   | DateTime literals | `#2024-01-01#`               | ❌  | ❌  | NCalc-style date literals      |
|   🔵   | TimeSpan literals | `TimeSpan.FromHours(1)`      | ❌  | ❌  | Via registered module          |
|   🔵   | Guid literals     | `Guid.NewGuid()`             | ❌  | ❌  | Via registered module          |

---

## Control Flow

| Status | Feature             | Syntax                                  | AST | IL  | Notes                         |
| :----: | ------------------- | --------------------------------------- | :-: | :-: | ----------------------------- |
|   ✅   | Block expressions   | `{ var x = 1; return x; }`              | ✅  | ✅  |                               |
|   ✅   | If statements       | `if (cond) { } else { }`                | ✅  | ✅  |                               |
|   ✅   | Return              | `return value;`                         | ✅  | ✅  | Early return in blocks        |
|   ✅   | While loop          | `while (cond) { }`                      | ✅  | ✅  | With iteration limit          |
|   ✅   | For loop            | `for (var i = 0; i < n; i++) { }`       | ✅  | ✅  |                               |
|   ✅   | Foreach loop        | `foreach (var x in items) { }`          | ✅  | ✅  | Iterator disposal handled     |
|   ✅   | Do-while loop       | `do { } while (cond)`                   | ✅  | ✅  |                               |
|   ✅   | Break/continue      | `break;`, `continue;`                   | ✅  | ✅  | In all loop types             |
|   ✅   | Switch statement    | `switch (x) { case 1: ... }`            | ✅  | ✅  | With fall-through and default |
|   🟡   | `goto case/default` | `goto case 1;`, `goto default;`         | ❌  | ❌  | ECMA §13.10.4                 |
|   🟡   | `switch` expression | `x switch { 1 => "one", _ => "other" }` | ❌  | ❌  |                               |
|   🔵   | `try-catch`         | `try { } catch { }`                     | ❌  | ❌  |                               |
|   🔵   | `throw`             | `throw new Exception("msg")`            | ❌  | ❌  |                               |
|   🔵   | `await`             | `await Task`                            | ❌  | ❌  | For async method calls        |
|   🔵   | `checked/unchecked` | `checked { ... }`                       | ❌  | ❌  | Overflow context              |

---

## Variables & Types

| Status | Feature               | Syntax                       | AST | IL  | Notes                                                                   |
| :----: | --------------------- | ---------------------------- | :-: | :-: | ----------------------------------------------------------------------- |
|   ✅   | Variable declaration  | `var x = 5;`                 | ✅  | ✅  | Type inferred                                                           |
|   ✅   | Typed declaration     | `int x = 5;`                 | ✅  | ✅  | `int`, `long`, `double`, `float`, `decimal`, `string`, `bool`, `object` |
|   ✅   | Nullable types        | `int? x = null;`             | ✅  | ✅  | Value type nullability                                                  |
|   🟡   | Multiple declaration  | `int x = 1, y = 2;`          | ❌  | ❌  | Multiple vars in one statement                                          |
|   🟡   | Tuple literals        | `(1, "hello")`               | ❌  | ❌  | C# 7.0                                                                  |
|   🟡   | Tuple types           | `(int, string) t = (1, "a")` | ❌  | ❌  | C# 7.0                                                                  |
|   🟡   | Named tuple elements  | `(count: 1, name: "test")`   | ❌  | ❌  | C# 7.0                                                                  |
|   🔵   | Tuple deconstruction  | `var (a, b) = tuple;`        | ❌  | ❌  | C# 7.0                                                                  |
|   ✅   | Interpolated strings  | `$"Hello {name}"`            | ✅  | ✅  |                                                                         |
|   ✅   | `is` operator         | `x is string`, `x is null`   | ✅  | ✅  | Type checking                                                           |
|   ✅   | `is not`              | `x is not null`, `x is not T`| ✅  | ✅  | Full support for null and type patterns                                 |
|   ✅   | `is` with variable    | `x is string s`              | ✅  | ✅  | Declare variable in type pattern                                        |
|   ✅   | `as` operator         | `x as string`                | ✅  | ✅  | Safe cast                                                               |
|   ✅   | Type casting          | `(int)x`                     | ✅  | ✅  |                                                                         |
|   🟡   | `nameof`              | `nameof(property)`           | ❌  | ❌  |                                                                         |
|   🔵   | `default`             | `default(int)`               | ❌  | ❌  |                                                                         |
|   ✅   | Verbatim strings      | `@"path\to\file"`            | ✅  | ✅  | Backslashes literal                                                     |
|   ✅   | Verbatim interpolated | `$@"path\{name}"`, `@$"..."` | ✅  | ✅  |                                                                         |
|   🔵   | Raw strings           | `"""text"""`                 | ❌  | ❌  | C# 11                                                                   |

---

## Collections & Objects

| Status | Feature                    | Syntax                             | AST | IL  | Notes                       |
| :----: | -------------------------- | ---------------------------------- | :-: | :-: | --------------------------- |
|   ✅   | Array literals             | `[1, 2, 3]`                        | ✅  | ✅  |                             |
|   ✅   | Anonymous objects          | `new { Name = "John", Age = 30 }`  | ✅  | ✅  |                             |
|   ✅   | Object spread              | `new { ...obj1, ...obj2 }`         | ✅  | ✅  |                             |
|   ✅   | Array spread               | `[...arr1, ...arr2]`               | ✅  | ✅  |                             |
|   ✅   | Object merging             | `obj1 + obj2`                      | ✅  | ✅  | Via `+` operator            |
|   ✅   | Index access               | `arr[0]`, `dict["key"]`            | ✅  | ✅  | Read and write              |
|   ✅   | Index assignment           | `arr[0] = value`                   | ✅  | ✅  | Arrays, lists, dictionaries |
|   ✅   | Property access            | `obj.Property`                     | ✅  | ✅  | Read and write              |
|   ✅   | Property assignment        | `obj.Prop = value`                 | ✅  | ✅  |                             |
|   ✅   | Method calls               | `obj.Method()`, `Math.Abs(x)`      | ✅  | ✅  |                             |
|   🔴   | Typed constructor          | `new DateTime(2024, 1, 1)`         | ❌  | ❌  | Requires type registry      |
|   🔴   | Object initializer         | `new Point { X = 10, Y = 20 }`     | ❌  | ❌  |                             |
|   🟡   | Constructor + initializer  | `new Person("John") { Age = 30 }`  | ❌  | ❌  |                             |
|   ✅   | Named parameters           | `Method(count: 10, enabled: true)` | ✅  | ✅  | For method calls only       |
|   🔵   | Collection initializer     | `new List<int> { 1, 2, 3 }`        | ❌  | ❌  | Use `[1,2,3]` instead       |
|   🔵   | Array creation             | `new int[] { 1, 2, 3 }`            | ❌  | ❌  | Use `[1,2,3]` instead       |
|   🔵   | Implicitly typed array     | `new[] { 1, 2, 3 }`                | ❌  | ❌  | Type inferred from elements |
|   🔵   | Generic type instantiation | `new List<int>()`                  | ❌  | ❌  |                             |
|   🟡   | Index from end             | `arr[^1]`                          | ❌  | ❌  | C# 8+                       |
|   🟡   | Range                      | `arr[1..3]`                        | ❌  | ❌  | C# 8+                       |
|   🟡   | `with` expression          | `obj with { Prop = val }`          | ❌  | ❌  | C# 9+                       |
|   🔵   | Destructuring              | `var { Name, Age } = person`       | ❌  | ❌  |                             |

---

## Pattern Matching

| Status | Feature            | Syntax                  | AST | IL  | Notes                  |
| :----: | ------------------ | ----------------------- | :-: | :-: | ---------------------- |
|   🟡   | Var pattern        | `x is var y`            | ❌  | ❌  | ECMA §11.2.4           |
|   🟡   | Property pattern   | `x is { Name: "John" }` | ❌  | ❌  | C# 8+                  |
|   🟡   | Relational pattern | `x is > 0 and < 100`    | ❌  | ❌  | C# 9+                  |
|   🟡   | Logical patterns   | `x is A and B or C`     | ❌  | ❌  | C# 9+                  |
|   🟡   | Switch case guards | `case int n when n > 0` | ❌  | ❌  | ECMA §13.8.3           |
|   🔵   | List patterns      | `x is [1, 2, ..]`       | ❌  | ❌  | C# 11                  |

---

## LINQ

> **Note:** LINQ methods use **native C# extension method resolution** via `System.Linq.Enumerable`. Lambda bodies are converted to typed `Func<>` delegates. Custom extension methods can be registered via `RegisterExtensionMethods(typeof(MyExtensions))`.

| Status | Feature                      | Syntax                                                                          | AST | IL  | Notes                      |
| :----: | ---------------------------- | ------------------------------------------------------------------------------- | :-: | :-: | -------------------------- |
|   ✅   | Filtering                    | `Where`, `Distinct`                                                             | ✅  | ✅  |                            |
|   ✅   | Projection                   | `Select`, `SelectMany`                                                          | ✅  | ✅  |                            |
|   ✅   | Element                      | `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault` | ✅  | ✅  |                            |
|   ✅   | Quantifiers                  | `Any`, `All`, `Contains`                                                        | ✅  | ✅  |                            |
|   ✅   | Aggregation                  | `Count`, `Sum`, `Average`, `Min`, `Max`, `Aggregate`                            | ✅  | ✅  |                            |
|   ✅   | Ordering                     | `OrderBy`, `OrderByDescending`, `Reverse`                                       | ✅  | ✅  |                            |
|   ✅   | Grouping                     | `GroupBy`                                                                       | ✅  | ✅  | Returns `[{ Key, Items }]` |
|   ✅   | Combining                    | `Zip`, `Concat`                                                                 | ✅  | ✅  |                            |
|   ✅   | Partitioning                 | `Take`, `Skip`                                                                  | ✅  | ✅  |                            |
|   ✅   | Set Operations               | `Except`, `Intersect`, `Union`                                                  | ✅  | ✅  |                            |
|   ✅   | Min/Max by Key               | `MinBy`, `MaxBy`                                                                | ✅  | ✅  | .NET 6+                    |
|   ✅   | Conversion                   | `ToList`, `ToArray`                                                             | ✅  | ✅  |                            |
|   🔵   | `Join`, `GroupJoin`          |                                                                                 | ❌  | ❌  | Complex multi-source join  |
|   🟡   | `TakeWhile`, `SkipWhile`     |                                                                                 | ❌  | ❌  | Easy with lazy eval        |
|   🟡   | `ThenBy`, `ThenByDescending` |                                                                                 | ❌  | ❌  | Secondary ordering         |
|   🟡   | `Append`, `Prepend`          |                                                                                 | ❌  | ❌  | Easy with lazy eval        |
|   🟡   | `Chunk`                      | `.Chunk(3)`                                                                     | ❌  | ❌  | .NET 6+                    |

### Known Deviations from C#

These are intentional differences from standard C# LINQ behavior:

| Method   | CsEval Behavior    | C# Standard Behavior       | Rationale                          |
| -------- | ------------------ | -------------------------- | ---------------------------------- |
| `OfType` | Filters nulls only | Filters by type `T`        | No runtime generic type parameters |
| `Cast`   | No-op              | Throws if element isn't `T`| No runtime generic type parameters |

---

## Security

| Status | Feature                      | Notes                                                            |
| :----: | ---------------------------- | ---------------------------------------------------------------- |
|   ✅   | `MaxIterations`              | Loop limit protection (100,000 default)                          |
|   ✅   | Explicit module registration | No arbitrary namespace access                                    |
|   ✅   | No `new Type()` syntax       | Can't instantiate arbitrary types                                |
|   ✅   | `SandboxMode.Trusted`        | Full access (default)                                            |
|   ✅   | `SandboxMode.Safe`           | Blocks method calls on variables                                 |
|   ✅   | `SandboxMode.Strict`         | Read-only mode (no mutations)                                    |
|   ✅   | Granular overrides           | `AllowMethodCalls`, `AllowPropertyRead`, `AllowAssignment`, etc. |
|   🔴   | `AllowNewKeyword`            | Enable/disable `new { }` syntax                                  |
|   🟡   | `MaxStringLength`            | Prevent string bombs (default: 1,000,000)                        |
|   🟡   | `MaxArrayLength`             | Prevent memory exhaustion (default: 100,000)                     |
|   🟡   | `MaxRecursionDepth`          | Prevent stack overflow (default: 100)                            |
|   🟡   | `BlockedTypes`               | Type blacklist                                                   |
|   🔵   | `AllowedTypes`               | Type whitelist (stricter than blocklist)                         |

---

## Infrastructure

| Status | Feature                  | Notes                                                        |
| :----: | ------------------------ | ------------------------------------------------------------ |
|   ✅   | Zero dependencies        | No external NuGet packages required                          |
|   ✅   | Pre-parsing              | `engine.Parse()` for repeated evaluation                     |
|   ✅   | Thread-safe contexts     | `engine.CreateChild()` with `Interlocked.CompareExchange`    |
|   ✅   | DI integration           | `IServiceProvider` at evaluation time                        |
|   ✅   | Cancellation             | `CancellationToken` auto-passed to methods                   |
|   ✅   | Module system            | `engine.RegisterModule()`                                    |
|   ✅   | Custom functions         | `engine.RegisterFunction()`                                  |
|   ✅   | Sandbox modes            | `Sandbox.Trusted()`, `Safe()`, `Strict()` presets            |
|   ✅   | Compilation modes        | `Interpreted`, `Compiled`, `StrictCompiled`                  |
|   🔴   | **Lazy parameter resolver** | `ParameterResolver` delegate (NCalc's killer feature)     |
|   🔴   | **Expression caching**   | `ConcurrentDictionary` auto-cache for parsed expressions     |
|   🔴   | **Compile to delegate**  | `engine.Compile<Func<T>>()` - 10,000%+ perf for hot paths    |
|   🟡   | Pluggable compiler       | `CustomCompiler` option for FastExpressionCompiler etc.      |
|   🟡   | Compile with DI          | `Compile<T>(IServiceProvider)` for scoped DI in compiled fns |
|   🟡   | Immutable fluid options  | `CsEvalOptions.WithCaseInsensitive()`, `WithSandbox()` pattern |
|   🟡   | `Run()` / `CsEvalState`  | Roslyn-style: state with `ReturnValue`, `Variables`, `ContinueWith()` |
|   🔵   | Expression tree export   | `Expression<Func<T>>` for LINQ/EF queries                    |
|   🔵   | Rich diagnostics         | Line/column tracking, severity levels in errors              |

---

## JavaScript Compatibility

| Status | Feature                | Syntax                          | Notes                    |
| :----: | ---------------------- | ------------------------------- | ------------------------ |
|   ✅   | `let`                  | `let x = 5;`                    | Treated as `var`         |
|   ✅   | `undefined`            | `undefined`                     | Maps to `null`           |
|   ✅   | Strict equality        | `===`, `!==`                    | Same as `==`/`!=`        |
|   🟡   | Template literals      | `` `Hello ${name}` ``           | Backtick strings         |
|   🟡   | `typeof` operator      | `typeof x`                      | Returns type name string |
|   🔵   | `instanceof`           | `x instanceof Y`                | Like C# `is`             |
|   🔵   | `console.log`          | `console.log(x)`                | Register as module       |
|   🔵   | `JSON.stringify/parse` | `JSON.stringify(obj)`           | Register as module       |
|   🔵   | Destructuring          | `const { a, b } = obj`          |                          |

> **Note:** JavaScript method aliases (`map`, `filter`, `reduce`, etc.) were removed in favor of native C# LINQ methods (`Select`, `Where`, `Aggregate`, etc.).

---

## Other Language Features

| Status | Feature                | Syntax                     | Notes                       |
| :----: | ---------------------- | -------------------------- | --------------------------- |
|   🟡   | Optional chaining call | `obj?.Method()`            | Currently only `?.Property` |
|   🔵   | Pipe operator          | `x \|> Process \|> Format` | F#/Kotlin style             |
|   🔵   | Chained comparison     | `0 < x < 100`              | Python style                |

---

## Not Implementing

| Feature                                     | Rationale                                                     |
| ------------------------------------------- | ------------------------------------------------------------- |
| `typeof(T)`                                 | Returns `System.Type` which is blocked by reflection security |
| `ToDictionary`                              | Redundant - use anonymous objects `new { Key = value }`       |
| `OfType<T>`, `Cast<T>` (full behavior)      | Requires runtime generic type parameters (stubs exist)        |
| `MethodFilter`, `MemberFilter`              | Would require `MethodInfo`/`MemberInfo` which are blocked     |
| Generic method calls                        | `list.Cast<int>()` requires runtime generic type resolution   |
| Full C# compilation                         | Use Roslyn for that                                           |
| Class/method definitions                    | Expressions only, not type definitions                        |
| LINQ query syntax                           | Method syntax only (`from x in y` → `y.Select()`)             |
| Unsafe code / pointers                      | Security                                                      |
| Preprocessor (`#if`)                        | Not applicable to expressions                                 |
| Static constructors                         | Class definition syntax, not expressions                      |
| Primary constructors (C# 12)                | Class declaration syntax                                      |
| Partial constructors (C# 14)                | Class definition syntax                                       |
| Constructor chaining (`:this()`, `:base()`) | Class definition syntax                                       |
| `sizeof`, `stackalloc`                      | Security / Unsafe code                                        |
| `::` (Namespace alias qualifier)            | Not applicable to expressions                                 |
| Pointer operators (`->`, `&`, `*`)          | Security / Unsafe code                                        |

---

## JavaScript Method Aliases

| Status | JavaScript | LINQ Equivalent  | Notes                            |
| :----: | ---------- | ---------------- | -------------------------------- |
|   ✅   | `map`      | `Select`         |                                  |
|   ✅   | `filter`   | `Where`          |                                  |
|   ✅   | `reduce`   | `Aggregate`      | JS arg order: `reduce(fn, seed)` |
|   ✅   | `flatMap`  | `SelectMany`     |                                  |
|   ✅   | `find`     | `FirstOrDefault` |                                  |
|   ✅   | `some`     | `Any`            |                                  |
|   ✅   | `every`    | `All`            |                                  |
|   ✅   | `includes` | `Contains`       |                                  |

### Additional JavaScript Methods

| Status | JavaScript    | Description              | Notes                 |
| :----: | ------------- | ------------------------ | --------------------- |
|   ✅   | `slice`       | Extract portion of array | Maps to `Skip`        |
|   ✅   | `flat`        | Flatten nested arrays    | Maps to `SelectMany`  |
|   🟡   | `findIndex`   | Index of first match     | Like `FindIndex`      |
|   🟡   | `indexOf`     | Index of element         | Like `IndexOf`        |
|   🟡   | `lastIndexOf` | Last index of element    | Like `LastIndexOf`    |
|   🟡   | `sort`        | Sort in place            | Like `OrderBy`        |
|   🟡   | `forEach`     | Execute for each element | Side-effect iteration |
|   🟡   | `push`        | Add to end               | Mutating              |
|   🟡   | `pop`         | Remove from end          | Mutating              |
|   🟡   | `shift`       | Remove from start        | Mutating              |
|   🟡   | `unshift`     | Add to start             | Mutating              |
|   🟡   | `join`        | Join to string           | Like `String.Join`    |
|   🟡   | `length`      | Property for count       | Like `Count()`        |

---

## Competitive Landscape & Critical Analysis

> **Last Updated:** February 2026
> **Purpose:** Brutally honest assessment of CsEval vs the ecosystem to identify gaps blocking adoption.

### The Competition

#### Category 1: C#-Like Expression Evaluators (Direct Competitors)

| Library | Stars | Execution | C# Parity | Maintenance | Key Differentiator |
|---------|-------|-----------|-----------|-------------|-------------------|
| **ExpressionEvaluator** | 627 | Pure AST interpret | ~85% | ❌ **Abandoned** | Single file, on-the-fly vars |
| **DynamicExpresso** | 2,000+ | Expression Trees | ~40% | ✅ Active | Community, stable API |
| **CsEval** | New | Hybrid (IL+AST) | ~90% | ✅ Active | LINQ, loops, sandbox |

#### Category 2: Math-Only (Simpler use cases)

| Library | Stars | Execution | Key Strength | Why Not Enough |
|---------|-------|-----------|--------------|----------------|
| **NCalc** | 1,000+ | AST + optional Lambda | Caching, `EvaluateParameter` event | No LINQ, no loops, no statements |
| **Flee** | 600+ | Direct IL (`DynamicMethod`) | Fastest execution, garbage-collected IL | Math only, LGPL license |
| **Jace.NET** | ~300 | Interpret + IL | Multi-mode, benchmarked | Math only |

#### Category 3: Different Languages (Not competitors)

| Library | Stars | Language | When to use instead of CsEval |
|---------|-------|----------|-------------------------------|
| **Jint** | 4,500+ | JavaScript ES2024 | Need JS interop, browser parity |
| **Scriban** | 3,800+ | Liquid-like | Text templating, code generation |

#### Category 4: Full C# (Overkill for expressions)

| Library | When to use | Why CsEval is better |
|---------|-------------|---------------------|
| **Roslyn** | Full C# compilation needed | 30MB SDK, slow startup, no sandbox |
| **Z.Expressions.Eval** | Enterprise support needed | **Paid** (50 char free limit), $299+/year |
| **CS-Script** | Full scripts with compilation | Memory leaks if misused, slow compile |

---

### Critical Self-Assessment: What CsEval Lacks

#### 🔴 BLOCKING ISSUES (Competitors have, we don't)

| Feature | Who Has It | Impact | Effort |
|---------|-----------|--------|--------|
| **Typed constructors** `new DateTime(2024,1,1)` | Everyone except math-only libs | Blocks real-world use cases | Medium |
| **Lazy parameter resolution** | NCalc (`EvaluateParameter`), ExpressionEvaluator (events) | Inefficient for large/expensive data | Small |
| **Expression caching** | NCalc (`ConcurrentDictionary`), Z.Expressions | Repeated evals are slow | Small |
| **`Compile<Func<T>>()`** | NCalc2 (10,000%+ speedup), Flee, Z.Expressions | Hot path performance | Medium |

#### 🟡 COMPETITIVE GAPS (Nice to have for parity)

| Feature | Who Has It | Notes |
|---------|-----------|-------|
| **`default(T)`** | ExpressionEvaluator, Z.Expressions | Common C# pattern |
| **`try-catch-finally`** | ExpressionEvaluator, Z.Expressions | Script error handling |
| **Dynamic LINQ** `.WhereDynamic("x > 5")` | Z.Expressions (selling point) | EF/LINQ-to-SQL scenarios |
| **Culture-sensitive parsing** | Flee | European decimal separators |
| **Error diagnostics** | Z.Expressions (#55 issue), Roslyn | Line/column in errors |

#### 🟢 CsEval ADVANTAGES (What we do better)

| Feature | CsEval | Competitors |
|---------|--------|-------------|
| **Full LINQ with lambdas** | ✅ 30+ methods, lazy, IL-compiled | DynamicExpresso: limited; NCalc: none |
| **Control flow** | ✅ if/for/while/foreach/switch | ExpressionEvaluator: yes; DynamicExpresso: no |
| **Granular sandbox** | ✅ 3 presets + overrides | Z.Expressions: config-based; others: none |
| **Zero dependencies** | ✅ | Same as Flee/ExpressionEvaluator |
| **Thread-safe contexts** | ✅ `Interlocked.CompareExchange` | Most: not designed for it |
| **IL + AST hybrid** | ✅ Best of both worlds | Flee: IL only; NCalc: AST only |

---

### Competitor Deep Dive: What We Can Learn

#### From NCalc: Lazy Parameter Resolution
```csharp
// NCalc pattern - we should copy this
expression.DynamicParameters["Pi"] = _ => {
    Console.WriteLine("I'm evaluating π!"); // Only called if needed
    return 3.14;
};

// CsEval today - must push everything upfront
engine.SetVariable("Pi", 3.14); // Always loaded, even if unused
```
**Action**: Add `ParameterResolver` delegate to `CsEvalOptions`.

#### From Z.Expressions: Compile to Delegate
```csharp
// Z.Expressions pattern
var compiled = Eval.Compile<Func<int, int>>("x * 2");
for (int i = 0; i < 1_000_000; i++)
    compiled(i); // Direct delegate call, no parsing

// CsEval today
var expr = engine.Parse("x * 2");
for (int i = 0; i < 1_000_000; i++)
    engine.Evaluate(expr, new { x = i }); // Still has overhead
```
**Action**: Add `engine.Compile<Func<T, TResult>>()` that returns a raw delegate.

#### From ExpressionEvaluator: On-the-Fly Events
```csharp
// ExpressionEvaluator pattern
evaluator.EvaluateVariable += (name, args) => {
    if (name == "expensive")
        args.Result = LoadFromDatabase(); // Only if accessed
};
evaluator.EvaluateFunction += (name, args) => {
    if (name == "custom")
        args.Result = MyFunction(args.Parameters);
};
```
**Action**: Consider event-based extensibility for advanced scenarios.

#### From Flee: Direct IL Generation
```csharp
// Flee generates IL directly via DynamicMethod
// Results are garbage-collected when expression is disposed
// No Expression Tree overhead
```
**Consideration**: For hot paths, `ILGenerator` could be 2-5x faster than Expression Trees. Worth profiling.

---

### Honest Comparison Matrix

| Capability | CsEval | Z.Expressions | ExpressionEvaluator | DynamicExpresso | NCalc |
|------------|--------|---------------|---------------------|-----------------|-------|
| **Loops** | ✅ | ✅ | ✅ | ❌ | ❌ |
| **LINQ lambdas** | ✅ Full | ✅ Full | ⚠️ Basic | ❌ | ❌ |
| **IL compilation** | ✅ | ✅ | ❌ | ✅ | ⚠️ Addon |
| **Lazy params** | ❌ **GAP** | ✅ | ✅ | ✅ | ✅ |
| **Typed constructors** | ❌ **GAP** | ✅ | ✅ | ✅ | N/A |
| **try-catch** | ❌ | ✅ | ✅ | ❌ | ❌ |
| **Sandbox** | ✅ Best | ✅ | ❌ | ⚠️ | ❌ |
| **Zero deps** | ✅ | ✅ | ✅ | ✅ | ❌ |
| **Active maintenance** | ✅ | ✅ | ❌ Dead | ✅ | ✅ |
| **Free** | ✅ | ❌ $299+ | ✅ | ✅ | ✅ |
| **AOT support** | ⚠️ Partial | ⚠️ Partial | ✅ | ✅ | ✅ |

**Legend:** ✅ = Yes | ⚠️ = Partial | ❌ = No | N/A = Not applicable

---

### Priority Roadmap (Critical Path to #1)

#### Phase 1: Remove Blockers (Must have for adoption)

| Priority | Feature | Why Critical | Effort |
|----------|---------|--------------|--------|
| 🔴 P0.1 | **Typed constructors** | Every competitor has this | Medium |
| 🔴 P0.2 | **Lazy parameter resolver** | NCalc's killer feature | Small |
| 🔴 P0.3 | **Expression caching** | Performance parity with NCalc | Small |

#### Phase 2: Performance Parity

| Priority | Feature | Benchmark Target | Effort |
|----------|---------|------------------|--------|
| 🟡 P1.1 | **`Compile<Func<T>>()`** | Match Z.Expressions hot path | Medium |
| 🟡 P1.2 | **Optimized interpreter** | AOT fallback performance | Medium |

#### Phase 3: Differentiation

| Priority | Feature | Unique Value | Effort |
|----------|---------|--------------|--------|
| 🟡 P2.1 | **Dynamic LINQ** | Z.Expressions' selling point, free | Medium |
| 🟡 P2.2 | **Switch expressions** | Modern C# syntax | Medium |
| 🔵 P3.1 | **Property patterns** | C# 8+ differentiation | Medium |

---

### Resources for Building Better Evaluators

> From the [ExpressionEvaluator similar projects list](https://github.com/codingseb/ExpressionEvaluator#similar-projects)

**Parser/Lexer Libraries:**
- [Parlot](https://github.com/sebastienros/parlot) - Fast parser combinator (used by NCalc)
- [Sprache](https://github.com/sprache/Sprache) - Lightweight parser
- [CSLY](https://github.com/b3b00/csly) - C# lex and yacc
- [Pidgin](https://github.com/benjamin-hodgson/Pidgin) - Parser combinator

**Learning Resources:**
- [Crafting Interpreters](http://www.craftinginterpreters.com) - Free book covering lexing → bytecode VM
- [Building a Compiler](https://www.youtube.com/playlist?list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y) - YouTube tutorial series

**Key Concepts:** LEX, YACC, AST, Syntactic trees, Pratt parsing, Expression Trees, DynamicMethod IL generation

---

## ECMA-334 Compliance Audit

> **Audit Date:** February 2026
> **Specification:** ECMA-334 7th Edition (December 2023)

This section documents CsEval's compliance with the official C# language specification.

### Overall Compliance Summary

| Spec Section | Category | Compliance | Notes |
|:-------------|:---------|:----------:|:------|
| §6 | Lexical Structure | 97% | Missing: `@keyword` verbatim identifiers |
| §8 | Types | 95% | All 16 simple types + nullable; no struct/enum definitions |
| §10 | Conversions | 85% | Full numeric conversions; partial nullable unwrapping |
| §11 | Patterns | 70% | Base patterns only; `is not` limited to `null`; no property/relational/logical |
| §12 | Expressions | 92% | Full operators (nullable correct); no typeof/default/nameof/tuples |
| §13 | Statements | 90% | Full control flow; no try-catch/throw |

### §6 Lexical Structure - Detailed Findings

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Keywords (54 reserved + 25 contextual) | §6.4.4 | ✅ | All C# keywords recognized |
| Integer literals (decimal/hex/binary) | §6.4.5.3 | ✅ | With digit separators, all suffixes |
| Real literals (float/double/decimal) | §6.4.5.4 | ✅ | Exponent notation, all suffixes |
| Character literals | §6.4.5.5 | ✅ | All escape sequences |
| String literals (regular/verbatim/interpolated) | §6.4.5.6 | ✅ | All escape sequences |
| Unicode escapes (`\uXXXX`, `\UXXXXXXXX`) | §6.4.2 | ✅ | In chars, strings, interpolated |
| Escape sequences (11 standard) | §6.4.5.5 | ✅ | `\0`, `\a`, `\b`, `\f`, `\n`, `\r`, `\t`, `\v`, `\\`, `\'`, `\"` |
| Verbatim identifiers (`@keyword`) | §6.4.3 | ❌ | Not implemented (only `@""` strings) |
| Leading decimal (`.5`) | §6.4.5.4 | ❌ | Requires `0.5` form |

### §8 Types - Detailed Findings

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| All 16 simple types | §8.3.5 | ✅ | sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, bool, string, object, dynamic |
| Nullable value types (`T?`) | §8.3.12 | ✅ | Full syntax and validation |
| Boxing/unboxing | §8.3.13 | ✅ | Implicit via `dynamic` dispatch |
| Type keyword resolution | §8.3.5 | ✅ | All keywords map to CLR types |
| Numeric type promotion | §12.4.7 | ✅ | Via `dynamic` dispatch |

**Not Applicable (Expression Evaluator):**
- Struct/enum definitions (§8.3.4, §8.3.10)
- Generic type definitions (§8.4)
- Type parameter constraints (§8.5)

### §10 Conversions - Detailed Findings

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Implicit numeric conversions | §10.2.3 | ✅ | Full table implemented |
| Explicit numeric conversions | §10.3.2 | ✅ | Truncation correct for float→int |
| Nullable wrapping (S → S?) | §10.6.1 | ✅ | Automatic wrapping |
| Nullable unwrapping (S? → T) | §10.6.1 | ⚠️ | Partial - no explicit null check |
| Cast expressions | §12.9.7 | ✅ | All type keywords |
| Constant expression conversions | §10.2.11 | ✅ | Range-checked narrowing |
| Boxing conversions | §10.2.9 | ✅ | Implicit via Expression Trees |
| Unboxing conversions | §10.3.7 | ✅ | Correct - throws `InvalidCastException` on type mismatch |
| Decimal/float mixing | §10.2.3 | ✅ | Correctly throws RuntimeBinderException |

### §11 Patterns - Detailed Findings

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Type pattern (`x is Type`) | §11.2.2 | ✅ | Full support |
| Declaration pattern (`x is Type v`) | §11.2.2 | ✅ | Variable binding in scope |
| Constant pattern (`x is null`, `x is 5`) | §11.2.3 | ✅ | In `is` and switch |
| Negation pattern (`x is not null`, `x is not Type`) | §11.2 | ✅ | Full support for null and type negation |
| Var pattern (`x is var y`) | §11.2.4 | ❌ | Not implemented |
| Property pattern (`x is { P: v }`) | C# 8+ | ❌ | Not implemented |
| Relational pattern (`x is > 0`) | C# 9+ | ❌ | Not implemented |
| Logical patterns (`and`, `or`, `not`) | C# 9+ | ❌ | Not implemented |

### §12 Expressions - Detailed Findings

#### §12.4 Operators

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Operator precedence | §12.4.2 | ✅ | Exact match to ECMA table |
| Unary minus (`-x`) | §12.9.3 | ✅ | All numeric types |
| Unary plus (`+x`) | §12.9.2 | ✅ | Identity operation |
| Logical NOT (`!x`) | §12.9.4 | ✅ | Bool required |
| Bitwise NOT (`~x`) | §12.9.5 | ✅ | Integral types |
| Arithmetic (`+`, `-`, `*`, `/`, `%`) | §12.10 | ✅ | Nullable returns null |
| Shift (`<<`, `>>`) | §12.11 | ✅ | Count masking implicit |
| Relational (`<`, `>`, `<=`, `>=`) | §12.12 | ✅ | Nullable returns false |
| Equality (`==`, `!=`) | §12.12 | ✅ | Reference and value equality |
| Bitwise (`&`, `\|`, `^`) | §12.13 | ✅ | Bool and integral |
| Conditional (`&&`, `\|\|`) | §12.14 | ✅ | Short-circuit |
| Null-coalescing (`??`) | §12.15 | ✅ | Right-associative (ECMA compliant) |
| Conditional (`?:`) | §12.18 | ✅ | Right-associative |
| Assignment | §12.21 | ✅ | All compound operators |

#### §12.8 Primary Expressions

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Literals | §12.8.2 | ✅ | All types |
| Interpolated strings | §12.8.3 | ✅ | With verbatim variants |
| Simple names | §12.8.4 | ✅ | Identifier resolution |
| Parenthesized expressions | §12.8.5 | ✅ | Grouping |
| Tuple expressions | §12.8.6 | ❌ | Not implemented |
| Member access (`obj.Member`) | §12.8.7 | ✅ | Read and write |
| Null-conditional member (`obj?.Member`) | §12.8.8 | ✅ | Full support |
| Invocation expressions | §12.8.9 | ✅ | Named arguments |
| Null-conditional invocation (`obj?.Method()`) | §12.8.10 | ✅ | Full support |
| Element access (`arr[i]`) | §12.8.11 | ✅ | Read and write |
| Null-conditional element (`arr?[i]`) | §12.8.12 | ❌ | Not implemented |
| Postfix increment/decrement | §12.8.15-16 | ✅ | Correct semantics |
| `new` operator (anonymous objects) | §12.8.16.7 | ✅ | With spread operator |
| `new` operator (typed constructors) | §12.8.16.2 | ❌ | Security restriction |
| `typeof` | §12.8.17 | ❌ | Security restriction |
| `default` | §12.8.20 | ❌ | Not implemented |
| `nameof` | §12.8.22 | ❌ | Not implemented |

#### §12.19-12.20 Anonymous Functions & LINQ

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Expression lambdas | §12.19 | ✅ | `x => x + 1` |
| Statement lambdas | §12.19.3 | ✅ | `x => { return x; }` |
| Closure capture | §12.19.6 | ✅ | Correct scoping |
| Foreach closure semantics | §12.19.6.3 | ✅ | **C# 5+ per-iteration capture** |
| Query expressions (`from...select`) | §12.20 | ❌ | Method syntax only (intentional) |
| LINQ method semantics | §12.20 | ✅ | 30+ methods with lazy evaluation |

### §13 Statements - Detailed Findings

| Feature | ECMA Section | Status | Notes |
|---------|:-------------|:------:|-------|
| Blocks | §13.3 | ✅ | Proper scope isolation |
| Variable declarations | §13.6 | ✅ | `var` and explicit types |
| Expression statements | §13.7 | ✅ | All valid expressions |
| `if` statement | §13.8.2 | ✅ | With optional else |
| `switch` statement | §13.8.3 | ✅ | **Strict no-fall-through (CS0163)** |
| `switch` case guards (`when`) | §13.8.3 | ❌ | Not implemented |
| `goto case`/`goto default` | §13.10.4 | ❌ | Not implemented |
| `while` loop | §13.9.2 | ✅ | With iteration limit |
| `do-while` loop | §13.9.3 | ✅ | Condition after body |
| `for` loop | §13.9.4 | ✅ | Optional clauses |
| `foreach` loop | §13.9.5 | ✅ | **Enumerator disposal, C# 5+ closure** |
| `break` | §13.10.2 | ✅ | Nearest loop/switch |
| `continue` | §13.10.3 | ✅ | Loop-only |
| `return` | §13.10.5 | ✅ | With optional value |
| `throw` | §13.10.6 | ❌ | Not implemented |
| `try-catch-finally` | §13.11 | ❌ | Not implemented |
| `goto` | §13.10.4 | ❌ | Not implemented |

### Priority Fixes from Audit

**High Priority (ECMA Non-Compliance - Bugs):**
1. ~~✅ **Nullable arithmetic** - Fixed: `5 + (int?)null` now returns `null`~~
2. ~~✅ **Nullable comparison** - Fixed: `5 > (int?)null` now returns `false`~~
3. ~~✅ **Unboxing exact type match** - Fixed: Correctly throws `InvalidCastException` when unboxing to wrong type~~
4. ~~✅ **Unary plus operator (`+x`)** - Fixed: Identity operation for numeric types~~
5. ~~✅ **Null-coalescing associativity** - Fixed: `??` is now right-associative~~

**Medium Priority (Common C# Features):**
1. ~~✅ Hex escape sequences (`\xHH`) in strings/chars - Fixed~~
2. 🔴 Char arithmetic (`'A' + 1` => 66, `'B' - 'A'` => 1) - ECMA §12.10.2
3. 🔴 Bitwise operators with bool promotion (`5 & true` should work)
4. 🟡 Property patterns (`x is { Name: "John" }`)
5. 🟡 Null-conditional indexer (`arr?[0]`)
6. 🟡 Var pattern (`x is var y`)

**Low Priority (Rarely Used):**
1. 🔵 Verbatim identifiers (`@if`, `@class`)
2. 🔵 Leading decimal (`.5`)
3. 🔵 Switch case guards (`when`)

### Test Coverage Notes

All implemented features are verified against Roslyn via `TestHelpers.EvaluateCSharpAsync()` to ensure C# parity. Tests run in three compilation modes:
- `CompilationMode.Interpreted` (AST tree-walking)
- `CompilationMode.Compiled` (IL with fallback)
- `CompilationMode.StrictCompiled` (IL only)

**ECMA-334 Compliance Tests:** See `tests/CsEval.Test/Compliance/Ecma334EdgeCaseTests.cs` for edge case tests derived from the ECMA-334 7th Edition specification covering:
- Binary numeric promotion (§12.4.7.3)
- Lifted operators for nullable types (§12.4.8)
- Null-coalescing right-associativity (§12.15)
- Operator precedence edge cases (§12.4.2)
- Char arithmetic and conversions (§12.10.2)
- IEEE 754 NaN/Infinity handling
- Unboxing semantics (§10.3.7)

---

## Configuration Options

> **Inspiration:** Z.Expressions.Eval offers 35+ configuration options. This section identifies options worth adding to CsEval, prioritized by user impact.

### Currently Implemented

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `CompilationMode` | Enum | `Interpreted` | `Interpreted`, `Compiled`, `StrictCompiled` |
| `MaxIterations` | `int` | `100_000` | Loop iteration limit |
| `IsCaseSensitive` | `bool` | `true` | Case-sensitive member lookup (set `false` for case-insensitive) |
| `StringComparison` | Enum | `Ordinal` | String comparison mode |
| `SandboxMode` | Enum | `Trusted` | `Trusted`, `Safe`, `Strict` |
| `AllowMethodCalls` | `bool` | `true` | Enable/disable method invocation |
| `AllowPropertyRead` | `bool` | `true` | Enable/disable property reads |
| `AllowPropertyWrite` | `bool` | `true` | Enable/disable property writes |
| `AllowAssignment` | `bool` | `true` | Enable/disable variable assignment |

### High Priority (Matches Competitor Features)

| Status | Option | Type | Default | Description | Z.Expressions Equivalent |
|:------:|--------|------|---------|-------------|-------------------------|
| 🔴 | `VariableFactory` | `Func<string, object?>` | `null` | Lazy parameter resolution - only fetch values when accessed | `VariableFactory` |
| 🔴 | `UseCache` | `bool` | `true` | Cache parsed expressions by string | `UseCache` |
| 🔴 | `CacheOptions` | `CacheOptions` | `Default` | Cache expiration, size limits | `MemoryCacheEntryOptionsFactory` |
| 🔴 | `CustomCompiler` | `Func<Expression, Delegate>` | `null` | Use FastExpressionCompiler etc. | `CustomCompile` |

### Medium Priority (Common Use Cases)

| Status | Option | Type | Default | Description | Z.Expressions Equivalent |
|:------:|--------|------|---------|-------------|-------------------------|
| 🟡 | `DefaultNumberType` | `Type` | `int` | Make `1/2` return `0.5` when set to `double` | `DefaultNumberType` |
| 🟡 | `UseCaretForExponent` | `bool` | `false` | `2^3` = `8` instead of XOR | `UseCaretForExponent` |
| 🟡 | `AutoResolveTypes` | `bool` | `false` | Auto-discover types without full namespace (`Math.Min` works without `System.`) | `UseSmartTypeResolution` |
| 🟡 | `MemberBindingFlags` | `BindingFlags` | `Public \| Instance` | Control member lookup visibility | `BindingFlags` |
| 🟡 | `AllowPrivateAccess` | `bool` | `false` | Enable access to private/internal members | Part of `BindingFlags` |
| 🟡 | `IncludeParameterMembers` | `bool` | `false` | Include members from all parameters without qualification | `IncludeMemberFromAllParameters` |

### Lower Priority (Edge Cases)

| Status | Option | Type | Default | Description | Z.Expressions Equivalent |
|:------:|--------|------|---------|-------------|-------------------------|
| 🔵 | `ForceCharAsString` | `bool` | `false` | `'x'` parsed as `"x"` for method overload resolution | `ForceCharAsString` |
| 🔵 | `DisableDynamicResolution` | `bool` | `false` | Strict typing - no `dynamic` operator dispatch | `DisableDynamicResolution` |
| 🔵 | `UseEqualsForComparison` | `bool` | `false` | `x = 5` means `x == 5` (formula mode) | `UseEqualsAssignmentAsEqualsOperator` |
| 🔵 | `AllowCollectionOperators` | `bool` | `false` | `list - item` removes item from list | `AllowAddSubtractOperatorToCollection` |
| 🔵 | `DisableConstantFolding` | `bool` | `false` | Keep `2+3` as expression instead of `5` (debugging) | `DisableConstantFolding` |
| 🔵 | `ThrowOnMissingType` | `bool` | `true` | Throw vs return null for unknown types | Part of `RetryAndThrowMissingTypes` |

### Diagnostics & Debugging

| Status | Option | Type | Default | Description | Z.Expressions Equivalent |
|:------:|--------|------|---------|-------------|-------------------------|
| 🟡 | `TrackSourceLocations` | `bool` | `false` | Include line/column in error messages | (Z.Expressions issue #55) |
| 🔵 | `LastCompiledExpression` | Property | N/A | Read-only access to last Expression Tree | `LastCompiledExpression` |
| 🔵 | `MissingTypes` | Property | N/A | List of types that couldn't be resolved | `MissingTypes` |

### Not Implementing

| Z.Expressions Option | Rationale |
|---------------------|-----------|
| `AutoAddMissingTypes` | Security concern - auto-scanning assemblies |
| `DynamicMemberNames` | CsEval uses explicit property names |
| `DynamicGetMemberMissingValueFactory` | Use `VariableFactory` instead |
| `UseNonGenericAnonymousType` | CsEval anonymous objects are `Dictionary<string, object?>` |
| `DisableAutoRegisterEntityFramework` | CsEval has no EF integration |
| `DisableAutoReplaceDictionaryKey` | CsEval doesn't auto-replace keys |
| `DisableMethodActionFuncCreation` | Security handled by SandboxMode |
| `UseLocalCache` | Use single cache with `CacheKeyPrefix` instead |
| `UseShortCacheKey` | Premature optimization |

### Example: Lazy Parameter Resolution

```csharp
// Z.Expressions pattern (we should support this)
var options = CsEvalOptions.Default with
{
    VariableFactory = name => name switch
    {
        "expensive" => LoadFromDatabase(), // Only called if accessed
        "user" => GetCurrentUser(),
        _ => null // Unknown variable
    }
};

var engine = new CsEvalEngine(options);
engine.Evaluate("cheap + 1"); // VariableFactory not called for "cheap" if already set
engine.Evaluate("expensive * 2"); // VariableFactory("expensive") called lazily
```

### Example: Formula Mode

```csharp
// Excel-style formulas for business users
var options = CsEvalOptions.Default with
{
    UseCaretForExponent = true,      // 2^3 = 8
    UseEqualsForComparison = true,   // x = 5 means x == 5
    DefaultNumberType = typeof(double) // 1/2 = 0.5
};

var engine = new CsEvalEngine(options);
engine.SetVariable("x", 10);
engine.Evaluate("x = 10");      // true (comparison, not assignment)
engine.Evaluate("2^8");         // 256 (exponent, not XOR)
engine.Evaluate("1/2");         // 0.5 (double, not int)
```

---

## Technical Debt & Known Constraints

### ✅ Resolved

1. ~~**Thread Safety**: The engine is explicitly not thread-safe.~~ ✅ Improved - Config/context initialization uses `Interlocked.CompareExchange` (Roslyn pattern). `CreateChild()` provides isolated contexts. Fresh evaluator per LINQ lambda invocation prevents state leakage.
2. ~~**Memory Usage**: Intermediate `List<object?>` creation in LINQ chains causes high GC pressure.~~ ✅ Fixed - LINQ now uses lazy `IEnumerable<object?>` evaluation.

### ⚠️ Known Limitations

3. **Security Boundaries**: Sandbox modes rely on reflection blocking which can be bypassed if a "Trusted" module leaks a `System.Type` or `MethodInfo` object.

4. **No Lazy Parameter Resolution**: Unlike NCalc (`EvaluateParameter` event) and ExpressionEvaluator (on-the-fly events), CsEval requires all variables to be pushed upfront via `SetVariable()`. This is inefficient for:
   - Large datasets where only a subset is accessed
   - Expensive database/API lookups that may not be needed
   - Dynamic property resolution (ExpandoObject patterns)

5. **No Expression Caching**: Unlike NCalc's `ConcurrentDictionary` cache, CsEval re-parses the same string expression every time. Users must manually cache `CsEvalExpression` objects.

6. **No Typed Delegate Export**: Unlike Z.Expressions' `Compile<Func<T>>()` or NCalc2's `ToLambda<T>()`, CsEval doesn't expose compiled expressions as reusable typed delegates. This means:
   - Hot loops still have per-call overhead
   - Can't integrate with LINQ-to-SQL/EF providers

7. **LINQ is Simulated, Not Native**: CsEval's LINQ support uses hardcoded method handlers (`LinqDispatcher`), not true C# overload resolution. This means:
   - Extension methods don't work
   - Custom LINQ providers (IQueryable) unsupported
   - Complex generic constraints may fail

8. **AOT Untested**: `Expression.Compile()` is problematic on iOS (IL2CPP), WebAssembly (AOT), and .NET Native. CsEval has an interpreter fallback but it's not optimized for AOT-only scenarios.

9. **Error Messages Lack Context**: Unlike Z.Expressions (which had [issue #55](https://github.com/zzzprojects/Eval-Expression.NET/issues/55) demanding better errors), CsEval doesn't track line/column numbers in exceptions. Debugging complex expressions is painful.

10. ~~**Unboxing Bug**: Fixed - now correctly throws `InvalidCastException` when unboxing to wrong type per ECMA §10.3.7.~~

11. **Lambda Arity Limited to 2 Parameters**: Extension method resolution (`ExtensionMethodResolver.cs`) only supports lambdas with 1-2 parameters via `CreateFunc1`/`CreateFunc2`. LINQ methods requiring 3+ parameter lambdas (e.g., `Aggregate` with index, custom extension methods) will fail. Adding `CreateFunc3`+ is straightforward but not yet implemented.

~~12. **`is not <type>` Pattern Incomplete**: The negation pattern only supports `is not null`. Patterns like `x is not string` are not implemented - the parser only recognizes `not` followed by `null`, not arbitrary type patterns.~~ ✅ Fixed - Full `is not Type` pattern now supported (e.g., `x is not string`).
