# CsEval Roadmap

Features for full C# developer familiarity, plus additions from other languages.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| 🔴 | Critical priority |
| 🟠 | High priority |
| 🟡 | Medium priority |
| ⚪ | Low priority |

---

## Implemented Features

### Core Language

| Feature | Syntax | Notes |
|---------|--------|-------|
| Arithmetic | `+`, `-`, `*`, `/`, `%` | Standard operators |
| Comparison | `==`, `!=`, `<`, `<=`, `>`, `>=` | |
| Logical | `&&`, `\|\|`, `!` | Short-circuit evaluation |
| Bitwise | `&`, `\|`, `^`, `~`, `<<`, `>>` | |
| Ternary | `a ? b : c` | |
| Null-coalescing | `??`, `??=` | |
| Null-conditional | `?.` | Property access only |
| Assignment | `=` | Variable reassignment |
| Compound assignment | `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `\|=`, `^=`, `<<=`, `>>=` | All 10 operators |
| Increment/decrement | `++x`, `x++`, `--x`, `x--` | Prefix and postfix |

### Control Flow

| Feature | Syntax | Notes |
|---------|--------|-------|
| Block expressions | `{ var x = 1; return x; }` | |
| If statements | `if (cond) { } else { }` | |
| Return | `return value;` | Early return in blocks |
| While loop | `while (cond) { }` | With iteration limit |
| For loop | `for (var i = 0; i < n; i++) { }` | |
| Foreach loop | `foreach (var x in items) { }` | |
| Do-while loop | `do { } while (cond)` | |
| Break/continue | `break;`, `continue;` | In all loop types |
| Switch statement | `switch (x) { case 1: ... }` | With fall-through and default |

### Variables & Types

| Feature | Syntax | Notes |
|---------|--------|-------|
| Variable declaration | `var x = 5;` | Type inferred |
| Typed declaration | `int x = 5;` | `int`, `long`, `double`, `float`, `decimal`, `string`, `bool`, `object` |
| Interpolated strings | `$"Hello {name}"` | |

### Collections & Objects

| Feature | Syntax | Notes |
|---------|--------|-------|
| Array literals | `[1, 2, 3]` | |
| Anonymous objects | `new { Name = "John", Age = 30 }` | |
| Object spread | `new { ...obj1, ...obj2 }` | |
| Array spread | `[...arr1, ...arr2]` | |
| Object merging | `obj1 + obj2` | Via `+` operator |
| Index access | `arr[0]`, `dict["key"]` | Read only |
| Property access | `obj.Property` | |

### LINQ Methods

| Category | Methods |
|----------|---------|
| Filtering | `Where`, `Distinct` |
| Projection | `Select`, `SelectMany` |
| Element | `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault` |
| Quantifiers | `Any`, `All`, `Contains` |
| Aggregation | `Count`, `Sum`, `Average`, `Min`, `Max`, `Aggregate` |
| Ordering | `OrderBy`, `OrderByDescending`, `Reverse` |
| Grouping | `GroupBy` (returns `[{ Key, Items }]`) |
| Combining | `Zip`, `Concat` |
| Partitioning | `Take`, `Skip` |
| Conversion | `ToList`, `ToArray` |

### Infrastructure

| Feature | Notes |
|---------|-------|
| Pre-parsing | `engine.Parse()` for repeated evaluation |
| Thread-safe contexts | `engine.CreateChild()` |
| DI integration | `IServiceProvider` at evaluation time |
| Async methods | `Task<T>` auto-unwrapped |
| Cancellation | `CancellationToken` auto-passed |
| Module system | `engine.RegisterModule()` |
| Custom functions | `engine.RegisterFunction()` |

---

## Planned Features

### Critical Priority

| Feature | Syntax | Notes |
|---------|--------|-------|
| 🔴 Index set | `arr[0] = value` | DE can read but not write |
| 🔴 Property set | `obj.Prop = value` | |

### High Priority

| Feature | Syntax | Notes |
|---------|--------|-------|
| 🟠 `is` operator | `x is string`, `x is null` | Type checking |
| 🟠 `is not` | `x is not null` | Common pattern |
| 🟠 `is` with variable | `x is string s` | Declare variable |
| 🟠 `as` operator | `x as string` | Safe cast |
| 🟠 Type casting | `(int)x` | |
| 🟠 Typed constructor | `new DateTime(2024, 1, 1)` | Requires type registry |
| 🟠 Object initializer | `new Point { X = 10, Y = 20 }` | |

### Medium Priority

| Feature | Syntax | Notes |
|---------|--------|-------|
| 🟡 Constructor + initializer | `new Person("John") { Age = 30 }` | |
| 🟡 `nameof` | `nameof(property)` | |
| 🟡 `typeof` | `typeof(int)` | |
| 🟡 Property pattern | `x is { Name: "John" }` | |
| 🟡 Relational pattern | `x is > 0 and < 100` | |
| 🟡 `switch` expression | `x switch { 1 => "one", _ => "other" }` | |
| 🟡 Generic method calls | `list.Cast<int>()` | |
| 🟡 Optional chaining call | `obj?.Method()` | Currently only `?.Property` |
| 🟡 `ToDictionary` | `items.ToDictionary(x => x.Id)` | |
| 🟡 `OfType<T>`, `Cast<T>` | LINQ methods | Need generic support |
| 🟡 Index from end | `arr[^1]` | C# 8+ |
| 🟡 Range | `arr[1..3]` | C# 8+ |
| 🟡 Verbatim strings | `@"path\to\file"` | |

### Low Priority

| Feature | Syntax | Notes |
|---------|--------|-------|
| ⚪ `default` | `default(int)` | |
| ⚪ Collection initializer | `new List<int> { 1, 2, 3 }` | Use `[1,2,3]` instead |
| ⚪ Array creation | `new int[] { 1, 2, 3 }` | Use `[1,2,3]` instead |
| ⚪ Generic type instantiation | `new List<int>()` | |
| ⚪ `throw` | `throw new Exception("msg")` | |
| ⚪ `try-catch` | `try { } catch { }` | |
| ⚪ Raw strings | `"""text"""` | C# 11 |
| ⚪ `Join`, `GroupJoin` | LINQ methods | Complex |
| ⚪ `TakeWhile`, `SkipWhile` | LINQ methods | |
| ⚪ `Except`, `Intersect`, `Union` | LINQ set operations | |
| ⚪ `MinBy`, `MaxBy` | LINQ methods | .NET 6+ |
| ⚪ Destructuring | `var { Name, Age } = person` | |
| ⚪ Pipe operator | `x \|> Process \|> Format` | F#/Kotlin style |
| ⚪ `in` operator | `x in [1, 2, 3]` | Python style |
| ⚪ Chained comparison | `0 < x < 100` | Python style |

---

## Not Implementing

| Feature | Rationale |
|---------|-----------|
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

## Competitor Comparison

| Feature | CsEval | Dynamic Expresso | NCalc |
|---------|:------:|:----------------:|:-----:|
| Lambda in LINQ | ✅ | ⚠️ partial | ❌ |
| Block expressions | ✅ | ❌ | ❌ |
| If statements | ✅ | ❌ | ❌ |
| All loops | ✅ | ❌ | ❌ |
| Break/continue | ✅ | ❌ | ❌ |
| Assignment | ✅ | ❌ | ❌ |
| Compound assignment | ✅ | ❌ | ❌ |
| Increment/decrement | ✅ | ❌ | ❌ |
| Object merging | ✅ | ❌ | ❌ |
| Spread operator | ✅ | ❌ | ❌ |
| Thread-safe | ✅ | ⚠️ issues | ? |
| DI integration | ✅ | ❌ | ❌ |
| Benchmarked | ✅ | ⚠️ | ✅ |

**CsEval is the most feature-complete expression evaluator in the .NET ecosystem.**

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
