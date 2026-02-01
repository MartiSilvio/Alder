# CsEval Roadmap

Features for full C# developer familiarity, plus additions from other languages.

> **ECMA-334 Compliance:** See [docs/ECMA-334-Compliance-Audit.md](docs/ECMA-334-Compliance-Audit.md) for a detailed specification compliance report. Overall compliance: **~65-70%** of features within scope.

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

| Status | Feature                | Syntax                                                        | AST | IL  | Notes                         |
| :----: | ---------------------- | ------------------------------------------------------------- | :-: | :-: | ----------------------------- |
|   ✅   | Arithmetic             | `+`, `-`, `*`, `/`, `%`                                       | ✅  | ✅  | Standard operators            |
|   ✅   | Comparison             | `==`, `!=`, `<`, `<=`, `>`, `>=`                              | ✅  | ✅  |                               |
|   ✅   | Logical                | `&&`, `\|\|`, `!`                                             | ✅  | ✅  | Short-circuit evaluation      |
|   ✅   | Bitwise AND/OR/XOR     | `&`, `\|`, `^`                                                | ✅  | ✅  |                               |
|   ✅   | Bitwise NOT            | `~`                                                           | ✅  | ✅  |                               |
|   ✅   | Shift                  | `<<`, `>>`                                                    | ✅  | ✅  |                               |
|   ✅   | Ternary                | `a ? b : c`                                                   | ✅  | ✅  |                               |
|   ✅   | Null-coalescing        | `??`                                                          | ✅  | ✅  |                               |
|   ✅   | Null-coalescing assign | `??=`                                                         | ✅  | ✅  |                               |
|   ✅   | Null-conditional       | `?.`                                                          | ✅  | ✅  | Property access only          |
|   🟡   | Null-conditional index | `arr?[0]`                                                     | ❌  | ❌  | C# 6+                         |
|   ✅   | Assignment             | `=`                                                           | ✅  | ✅  | Variable reassignment         |
|   ✅   | Compound arithmetic    | `+=`, `-=`, `*=`, `/=`, `%=`                                  | ✅  | ✅  |                               |
|   ✅   | Compound bitwise       | `&=`, `\|=`, `^=`, `<<=`, `>>=`                               | ✅  | ✅  |                               |
|   ✅   | Increment/decrement    | `++x`, `x++`, `--x`, `x--`                                    | ✅  | ✅  | Prefix and postfix            |
|   🟡   | Unary plus             | `+x`                                                          | ❌  | ❌  | Returns value unchanged       |
|   ✅   | Containment            | `x in list`                                                   | ✅  | ✅  | Python-style, via extension   |
|   🔵   | Null-forgiving         | `x!`                                                          | ❌  | ❌  | C# 8+                         |
|   🔵   | Unsigned shift         | `x >>> y`                                                     | ❌  | ❌  | C# 11                         |

---

## Literals

| Status | Feature           | Syntax                        | AST | IL  | Notes                          |
| :----: | ----------------- | ----------------------------- | :-: | :-: | ------------------------------ |
|   ✅   | Numeric literals  | `42`, `3.14`, `42L`, `3.14m`  | ✅  | ✅  | int, long, double, decimal     |
|   ✅   | String literals   | `"hello"`                     | ✅  | ✅  |                                |
|   ✅   | Boolean literals  | `true`, `false`               | ✅  | ✅  |                                |
|   ✅   | Null literal      | `null`                        | ✅  | ✅  |                                |
|   ✅   | Char literals     | `'a'`, `'\t'`, `'\n'`         | ✅  | ✅  | Single quotes for characters   |
|   ✅   | Hex literals      | `0xFF`, `0x1A`                | ✅  | ✅  | Hexadecimal integers           |
|   ✅   | Binary literals   | `0b1010`                      | ✅  | ✅  | C# 7.0                         |
|   ✅   | Digit separators  | `1_000_000`, `0xFF_FF`        | ✅  | ✅  | C# 7.0                         |
|   ✅   | Unicode escapes   | `'\u0041'`, `"\u0048\u0069"`  | ✅  | ✅  | In char and string literals    |
|   ✅   | Escape sequences  | `\t`, `\n`, `\r`, `\\`        | ✅  | ✅  | In string literals             |
|   ✅   | More escapes      | `\0`, `\a`, `\b`, `\f`, `\v`  | ✅  | ✅  | Additional C# escape sequences |
|   ✅   | Exponent notation | `1e10`, `1.5E-3`, `3e+5`      | ✅  | ✅  | Scientific notation            |
|   🟡   | Leading decimal   | `.5`, `.123`                  | ❌  | ❌  | Requires `0.5` currently       |
|   🔵   | DateTime literals | `#2024-01-01#`                | ❌  | ❌  | NCalc-style date literals      |
|   🔵   | TimeSpan literals | `TimeSpan.FromHours(1)`       | ❌  | ❌  | Via registered module          |
|   🔵   | Guid literals     | `Guid.NewGuid()`              | ❌  | ❌  | Via registered module          |

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
|   ✅   | `is not`              | `x is not null`              | ✅  | ✅  | Common pattern                                                          |
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

| Status | Feature                    | Syntax                             | AST | IL  | Notes                            |
| :----: | -------------------------- | ---------------------------------- | :-: | :-: | -------------------------------- |
|   ✅   | Array literals             | `[1, 2, 3]`                        | ✅  | ✅  |                                  |
|   ✅   | Anonymous objects          | `new { Name = "John", Age = 30 }`  | ✅  | ✅  |                                  |
|   ✅   | Object spread              | `new { ...obj1, ...obj2 }`         | ✅  | ✅  |                                  |
|   ✅   | Array spread               | `[...arr1, ...arr2]`               | ✅  | ✅  |                                  |
|   ✅   | Object merging             | `obj1 + obj2`                      | ✅  | ✅  | Via `+` operator                 |
|   ✅   | Index access               | `arr[0]`, `dict["key"]`            | ✅  | ✅  | Read and write                   |
|   ✅   | Index assignment           | `arr[0] = value`                   | ✅  | ✅  | Arrays, lists, dictionaries      |
|   ✅   | Property access            | `obj.Property`                     | ✅  | ✅  | Read and write                   |
|   ✅   | Property assignment        | `obj.Prop = value`                 | ✅  | ✅  |                                  |
|   ✅   | Method calls               | `obj.Method()`, `Math.Abs(x)`      | ✅  | ✅  |                                  |
|   🔴   | Typed constructor          | `new DateTime(2024, 1, 1)`         | ❌  | ❌  | Requires type registry           |
|   🔴   | Object initializer         | `new Point { X = 10, Y = 20 }`     | ❌  | ❌  |                                  |
|   🟡   | Constructor + initializer  | `new Person("John") { Age = 30 }`  | ❌  | ❌  |                                  |
|   ✅   | Named parameters           | `Method(count: 10, enabled: true)` | ✅  | ✅  | For method calls only            |
|   🔵   | Collection initializer     | `new List<int> { 1, 2, 3 }`        | ❌  | ❌  | Use `[1,2,3]` instead            |
|   🔵   | Array creation             | `new int[] { 1, 2, 3 }`            | ❌  | ❌  | Use `[1,2,3]` instead            |
|   🔵   | Implicitly typed array     | `new[] { 1, 2, 3 }`                | ❌  | ❌  | Type inferred from elements      |
|   🔵   | Generic type instantiation | `new List<int>()`                  | ❌  | ❌  |                                  |
|   🟡   | Index from end             | `arr[^1]`                          | ❌  | ❌  | C# 8+                            |
|   🟡   | Range                      | `arr[1..3]`                        | ❌  | ❌  | C# 8+                            |
|   🟡   | `with` expression          | `obj with { Prop = val }`          | ❌  | ❌  | C# 9+                            |
|   🔵   | Destructuring              | `var { Name, Age } = person`       | ❌  | ❌  |                                  |

---

## Pattern Matching

| Status | Feature            | Syntax                  | AST | IL  | Notes |
| :----: | ------------------ | ----------------------- | :-: | :-: | ----- |
|   🟡   | Property pattern   | `x is { Name: "John" }` | ❌  | ❌  |       |
|   🟡   | Relational pattern | `x is > 0 and < 100`    | ❌  | ❌  |       |

---

## LINQ

> **Note:** LINQ methods now use **lazy evaluation** (`IEnumerable<object?>`) and only materialize at terminal operations (`ToList`, `ToArray`, `Sum`, etc.). Lambda bodies are **IL-compiled** to native delegates via Expression Trees.

| Status | Feature                  | Syntax                                                                          | AST | IL  | Notes                      |
| :----: | ------------------------ | ------------------------------------------------------------------------------- | :-: | :-: | -------------------------- |
|   ✅   | Filtering                | `Where`, `Distinct`                                                             | ✅  | ✅  |                            |
|   ✅   | Projection               | `Select`, `SelectMany`                                                          | ✅  | ✅  |                            |
|   ✅   | Element                  | `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault` | ✅  | ✅  |                            |
|   ✅   | Quantifiers              | `Any`, `All`, `Contains`                                                        | ✅  | ✅  |                            |
|   ✅   | Aggregation              | `Count`, `Sum`, `Average`, `Min`, `Max`, `Aggregate`                            | ✅  | ✅  |                            |
|   ✅   | Ordering                 | `OrderBy`, `OrderByDescending`, `Reverse`                                       | ✅  | ✅  |                            |
|   ✅   | Grouping                 | `GroupBy`                                                                       | ✅  | ✅  | Returns `[{ Key, Items }]` |
|   ✅   | Combining                | `Zip`, `Concat`                                                                 | ✅  | ✅  |                            |
|   ✅   | Partitioning             | `Take`, `Skip`                                                                  | ✅  | ✅  |                            |
|   ✅   | Set Operations           | `Except`, `Intersect`, `Union`                                                  | ✅  | ✅  |                            |
|   ✅   | Min/Max by Key           | `MinBy`, `MaxBy`                                                                | ✅  | ✅  | .NET 6+                    |
|   ✅   | Conversion               | `ToList`, `ToArray`                                                             | ✅  | ✅  |                            |
|   🔵   | `Join`, `GroupJoin`      |                                                                                 | ❌  | ❌  | Complex multi-source join  |
|   🟡   | `TakeWhile`, `SkipWhile` |                                                                                 | ❌  | ❌  | Easy with lazy eval        |
|   🟡   | `ThenBy`, `ThenByDescending` |                                                                             | ❌  | ❌  | Secondary ordering         |
|   🟡   | `Append`, `Prepend`      |                                                                                 | ❌  | ❌  | Easy with lazy eval        |
|   🟡   | `Chunk`                  | `.Chunk(3)`                                                                     | ❌  | ❌  | .NET 6+                    |

### Known Deviations from C#

These are intentional differences from standard C# LINQ behavior:

| Method    | CsEval Behavior                                         | C# Standard Behavior                          | Rationale                                      |
| --------- | ------------------------------------------------------- | --------------------------------------------- | ---------------------------------------------- |
| `GroupBy` | Returns `List<Dictionary>` with `Key` and `Items` props | Returns `IEnumerable<IGrouping<TKey, TElem>>` | No runtime generic types; dictionary is usable |
| `Zip`     | Without selector returns `{ First, Second }` dictionary | Returns `IEnumerable<(T1, T2)>` tuples        | No tuple syntax support yet                    |
| `OfType`  | Filters nulls only                                      | Filters by type `T`                           | No runtime generic type parameters             |
| `Cast`    | No-op (returns input)                                   | Throws if element isn't `T`                   | No runtime generic type parameters             |

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

| Status | Feature              | Notes                                                        |
| :----: | -------------------- | ------------------------------------------------------------ |
|   ✅   | Zero dependencies    | No external NuGet packages required                          |
|   ✅   | Pre-parsing          | `engine.Parse()` for repeated evaluation                     |
|   ✅   | Thread-safe contexts | `engine.CreateChild()`                                       |
|   ✅   | DI integration       | `IServiceProvider` at evaluation time                        |
|   ✅   | Cancellation         | `CancellationToken` auto-passed to methods                   |
|   ✅   | Module system        | `engine.RegisterModule()`                                    |
|   ✅   | Custom functions     | `engine.RegisterFunction()`                                  |
|   ✅   | Sandbox modes        | `Sandbox.Trusted()`, `Safe()`, `Strict()` presets            |
|   ✅   | Compilation modes    | `Interpreted`, `Compiled`, `StrictCompiled`                  |
|   🟡   | Pluggable compiler   | `CustomCompiler` option for FastExpressionCompiler etc.      |
|   🟡   | Compile to delegate  | `engine.Compile<Func<T>>()` for typed, reusable delegates    |
|   🟡   | Compile with DI      | `Compile<T>(IServiceProvider)` for scoped DI in compiled fns |
|   🔵   | Expression tree export | `Expression<Func<T>>` for LINQ/EF queries                  |

---

## JavaScript Compatibility

| Status | Feature                | Syntax                          | Notes                    |
| :----: | ---------------------- | ------------------------------- | ------------------------ |
|   ✅   | `let`                  | `let x = 5;`                    | Treated as `var`         |
|   ✅   | `undefined`            | `undefined`                     | Maps to `null`           |
|   ✅   | Strict equality        | `===`, `!==`                    | Same as `==`/`!=`        |
|   ✅   | Method aliases         | `map`, `filter`, `reduce`, etc. | Maps to LINQ equivalents |
|   🟡   | Template literals      | `` `Hello ${name}` ``           | Backtick strings         |
|   🟡   | `typeof` operator      | `typeof x`                      | Returns type name string |
|   🔵   | `instanceof`           | `x instanceof Y`                | Like C# `is`             |
|   🔵   | `console.log`          | `console.log(x)`                | Register as module       |
|   🔵   | `JSON.stringify/parse` | `JSON.stringify(obj)`           | Register as module       |
|   🔵   | Destructuring          | `const { a, b } = obj`          |                          |

---

## Other Language Features

| Status | Feature                | Syntax                     | Notes                       |
| :----: | ---------------------- | -------------------------- | --------------------------- |
|   🟡   | Optional chaining call | `obj?.Method()`            | Currently only `?.Property` |
|   🔵   | Pipe operator          | `x \|> Process \|> Format` | F#/Kotlin style             |
|   ✅   | `in` operator          | `x in [1, 2, 3]`           | Python style                |
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

## Technical Comparison & Market Context

This section provides a candid audit of CsEval against the established ecosystem. This analysis is intended to identify technical gaps and architectural limitations rather than to market the project.

### Comparison Framework

| Feature Group          |    CsEval (Hybrid)     | Roslyn (Compiler)  | Z.Expressions (Commercial) | DynamicExpresso (Interpreter) |  NCalc (Math-Only)   |    Flee (IL-Gen)     |
| :--------------------- | :--------------------: | :----------------: | :------------------------: | :---------------------------: | :------------------: | :------------------: |
| **Parsing Strategy**   |   Recursive Descent    |   Full C# Parser   |        Proprietary         |            Custom             | Parlot (Combinator)  |      Grammatica      |
| **Execution Model**    | Expr. Tree / Tree-Walk | Native IL (Memory) |         Native IL          |     Expr. Tree / Visitor      |    Visitor (AST)     |   DynamicMethod IL   |
| **Primary Dependency** |          None          |     ~30MB SDK      |            None            |             None              |         None         |         None         |
| **C# Parity**          |  Subset (Loops/LINQ)   |        100%        |            >95%            |      ~40% (Expressions)       |   <10% (Math-only)   |         ~30%         |
| **AOT Compatibility**  |  Untested/Restricted   |       ❌ No        |         ⚠️ Partial         |            ✅ Yes             |        ✅ Yes        |        ❌ No         |
| **Security Model**     |    Opt-out Sandbox     | AppDomain/Assembly |       Configuration        |       Opt-in Whitelist        | N/A (Limited Syntax) | N/A (Limited Syntax) |
| **Ecosystem Size**     |      Experimental      |     50M+ Devs      |         54M NuGet          |           2k Stars            |       1k Stars       |      600 Stars       |

### Configuration & Ergonomics Comparison

This audit focuses on how each library manages its operational state, security boundaries, and runtime data binding.

| Feature             | CsEval (Current)    | Roslyn (Compiler)       | Z.Expressions (Commercial) | DynamicExpresso (Interpreter) | NCalc (Math-Only)      | Flee (IL-Gen)        |
| :------------------ | :------------------ | :---------------------- | :------------------------- | :---------------------------- | :--------------------- | :------------------- |
| **Config Model**    | Init-only Class     | **Immutable Fluid API** | Global + Instance Context  | Mutable Instance              | Mutable Instance       | Nested Property Tree |
| **Global Defaults** | ❌ No               | ❌ No                   | ✅ `EvalManager`           | ❌ No                         | ❌ No                  | ❌ No                |
| **Late-Binding**    | ❌ No               | ⚠️ `Globals` Object     | ✅ `RegisterMember`        | ✅ `SetVariable`              | ✅ `EvaluateParameter` | ✅ `Variables` Dict  |
| **Type Discovery**  | Explicit Module Reg | **Automatic Reference** | Whitelist-only (Optional)  | Context-based                 | N/A                    | Explicit Add Type    |
| **Security Preset** | Predefined Enums    | AppDomain / Sandbox     | **SafeMode** (Strict)      | Whitelist Only                | N/A                    | Options Flags        |

#### Critical Analysis of Configuration Gaps

1. **Configuration Immutability and Safety**
   - **Identified Weakness**: `CsEvalOptions` uses `init` properties but lacks a fluid API. In multi-threaded scenarios, creating variations of an engine's configuration is clunky compared to Roslyn's `ScriptOptions.WithReferences(...)`.
   - **Comparison**: **Roslyn**'s `ScriptOptions` are immutable, ensuring that a shared "base" configuration cannot be accidentally corrupted by a consumer.
   - **Action**: Transition `CsEvalOptions` to a record with a fluid API (`WithIgnoreCase`, `WithSandbox`) for better ergonomics and thread safety.

2. **Late-Binding and Lazy Data Retrieval**
   - **Identified Weakness**: Every variable must be pushed into the engine BEFORE evaluation via `SetVariable`. This is inefficient for large datasets or expensive database lookups that may not be needed by the expression.
   - **Comparison**: **NCalc** provides an `EvaluateParameter` event, allowing the library to "pull" data only when the expression actually encounters a variable name.
   - **Action**: Implement a `ParameterResolver` delegate in `CsEvalOptions` to support lazy-loading of context data.

3. **Whitelist-Based Security (SafeMode)**
   - **Identified Weakness**: CsEval's sandbox is additive (you block what you don't like). This is fundamentally less secure than a "Default Deny" policy.
   - **Comparison**: **Z.Expressions.Eval** features a `SafeMode` where the engine throws if any type or member is accessed that hasn't been explicitly whitelisted.
   - **Action**: Introduce a `WhitelistedTypes` collection. When enabled, the engine will block even "Safe" looking properties unless they are on the list.

### Critical Analysis of Gaps

#### 1. Performance and Compilation Architecture

- **Current Status**: CsEval compiles **all features** to IL via `System.Linq.Expressions`, including operators, method calls, control flow, object literals, and **lambda expressions**. Lambda bodies are compiled to native delegates (`CompiledLambdaValue`), eliminating the last major AST-only fallback.
- **Learning Opportunity**: Libraries like **Flee** and **Z.Expressions** generate raw IL via `DynamicMethod` or `Reflection.Emit`. This bypasses the overhead of the Expression Tree abstraction and allows for optimizations like direct stack manipulation that `Expression` trees cannot express.
- **Action**: Consider `ILGenerator` for specific hot paths if profiling reveals Expression Tree overhead.

#### 2. Language Parity (C# Compliance)

- **Identified Weakness**: CsEval lacks support for **Extension Methods**, **Generic Method Resolution**, and **Pattern Matching**. This makes its "LINQ" support a simulation (hardcoded handlers) rather than a true language feature.
- **Comparison**: **Roslyn** and **Z.Expressions** handle the full complexity of C# overload resolution rules. CsEval's resolution is simplistic and prone to failure with complex inheritance or generic constraints.
- **Action**: Implement a proper `MethodResolver` that respects C# overload resolution rules (including extension methods and implicit conversions).

#### 3. AOT (Ahead-of-Time) & Mobile Support

- **Identified Weakness**: The use of `Expression.Compile()` makes the library's performance unpredictable on AOT platforms (iOS/Console), as it either fails or triggers slow JIT-emulation.
- **Comparison**: **DynamicExpresso** and **NCalc** provide robust visitor-based interpreters that work identically in AOT. CsEval's "Hybrid" approach means performance drops off a cliff when compilation is unavailable.
- **Action**: Benchmark and document behavior on AOT platforms. Develop an "Interpreted-Only" mode that is optimized for performance without relying on JIT compilation.

#### 4. Tooling & Ecosystem

- **Identified Weakness**: Unlike **Roslyn** (Diagnostics) or **NCalc** (JSON AST), CsEval lacks visibility into its internal state (no serialized AST, no source mapping for errors).
- **Action**: Implement AST serialization (JSON) and provide column/line number tracking for runtime exceptions.

---

## Technical Roadmap (2026)

Derived from the competitive audit and ECMA-334 compliance analysis.

### Phase 0: ECMA-334 Critical Gaps (Highest Priority)

The following gaps were identified in the [ECMA-334 Compliance Audit](docs/ECMA-334-Compliance-Audit.md) and should be addressed first:

| Item | Technical Requirement | ECMA-334 Section |
| :--- | :-------------------- | :--------------- |
| ~~**Character Literals**~~ | ~~Parse `'a'`, `'\n'`, `'\t'` as char type, not string. Add `TokenType.Character`.~~ ✅ Done | §6.4.5.5 |
| ~~**Hexadecimal Literals**~~ | ~~Detect `0x` prefix in Lexer.ScanNumber() and parse hex digits.~~ ✅ Done | §6.4.5.3 |
| ~~**Explicit Cast Operator**~~ | ~~Add `CastExpr` AST node, parse `(type)expr` in ParseUnary(), implement in Evaluator.~~ ✅ Done | §10.3 |
| ~~**Type Testing (`is`, `as`)**~~ | ~~Add `IsExpr`, `AsExpr` AST nodes. Parse in comparison level. Implement runtime type checks.~~ ✅ Done | §12.12 |
| ~~**Unicode Escapes**~~ | ~~Parse `\uHHHH` and `\UHHHHHHHH` in strings and chars.~~ ✅ Done | §6.4.2 |

### Phase 1: Core Engine Hardening (High Priority)

| Item                       | Technical Requirement                                                                                                             | Comparison Context                          |
| :------------------------- | :-------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------ |
| ~~**LINQ Lambda Compilation**~~| ~~Compile LINQ lambda bodies to IL instead of tree-walking. Currently lambdas fall back to `Evaluator` for execution.~~      | ✅ Done - Lambdas compile to IL delegates.  |
| **C# Overload Resolution** | Implement `BindingFlags` and argument type matching for method calls, including implicit numeric conversions (`int` to `double`). | Fixes failures seen in **DynamicExpresso**. |
| **Immutable Fluid API**    | Refactor `CsEvalOptions` to use fluid methods (e.g., `options.WithIgnoreCase(true).WithSafeSandbox()`).                           | Parity with **Roslyn** ergonomics.          |
| **Lazy Late-Binding**      | Add `IParameterResolver` or `Func<string, object?>` support to fetch variables on-demand during execution.                        | Parity with **NCalc** lazy-loading.         |
| **SafeMode (Whitelist)**   | Implement a "Deny All" security policy requiring explicit `engine.AllowType<T>()` or `AllowMember("Name")`.                       | Parity with **Z.Expressions.Eval**.         |
| ~~**Expression Caching**~~ | ~~Implement an internal cache for compiled expressions to avoid redundant compilation costs in high-frequency loops.~~            | ✅ Done - `ExpressionCache` with thread-safe `ConcurrentDictionary`. |

### Phase 2: Feature Parity & Extensibility (Medium Priority)

| Item                       | Technical Requirement                                                                                  | Comparison Context                      |
| :------------------------- | :----------------------------------------------------------------------------------------------------- | :-------------------------------------- |
| **Extension Methods**      | Allow registration of static classes as extension method containers (e.g., custom `MyLinqExtensions`). | Parity with **Roslyn/Eval.NET**.        |
| **Pattern Matching**       | Support `is` operator and basic property patterns `{ Prop: value }`.                                   | Missing in almost all lightweight libs. |
| **Generic Method Support** | Allow calling `Method<T>()` via reflection-based discovery if the type can be inferred from arguments. | Requirement for true LINQ parity.       |
| **Typed Constructors**     | Implementation of `new T(...)` via a secure Type Registry.                                             | Common request in **Flee/Expresso**.    |
| **Global Manager**         | Implement `CsEvalManager` for setting process-wide default options and performance hooks.              | Feature found in **Z.Expressions**.     |
| **Compile to Delegate**    | `Compile<Func<T>>()` returns strongly-typed delegate; accepts `IServiceProvider` for scoped DI.        | Parity with **Z.Expressions.Eval**.     |
| **Pluggable Compiler**     | `CustomCompiler` hook for FastExpressionCompiler (10-40x faster). Zero deps; user provides the lambda. | Parity with **Z.Expressions.Eval**.     |
|                            |

### Phase 3: Developer Experience & Ecosystem (Low Priority)

| Item                          | Technical Requirement                                                                         | Comparison Context                 |
| :---------------------------- | :-------------------------------------------------------------------------------------------- | :--------------------------------- |
| **AST Serialization**         | Export/Import parsed expressions as JSON for storage or cross-process evaluation.             | Parity with **NCalc**.             |
| **Visualizer / Debugger**     | Provide a `ToString()` or `ToDebugString()` that reconstructs C# code from the AST.           | Improves upon **CodingSeb.EE**.    |
| **Config Serialization**      | Support loading `CsEvalOptions` from `appsettings.json` or custom JSON schemas.               | Improves upon **DynamicExpresso**. |
| **AOT Validation Suite**      | Setup CI tests for `.net8-ios` and `.net8-android` (AOT) to document performance degradation. | Parity with **NCalc** reliability. |
| **Benchmark Suite Expansion** | Add side-by-side benchmarks against **Z.Expressions** and **Flee** in the main documentation. | Honesty/Transparency requirement.  |

---

## Technical Debt & Known Constraints

1.  **Thread Safety**: The engine is explicitly not thread-safe. While `CreateChild()` mitigates this, the internal `TypeCache` and `ModuleRegistry` must be audited for lock contention under high load.
2.  ~~**Memory Usage**: Intermediate `List<object?>` creation in LINQ chains causes high GC pressure. No support for `IEnumerable` lazy evaluation.~~ ✅ Fixed - LINQ now uses lazy `IEnumerable<object?>` evaluation.
3.  **Security Boundaries**: Sandbox modes rely on reflection blocking which can be bypassed if a "Trusted" module leaks a `System.Type` or `MethodInfo` object.
