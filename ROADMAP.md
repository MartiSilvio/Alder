# CsEval Roadmap

Features to implement for full C# developer familiarity, plus useful additions from other languages.

## Legend

- ~~Strikethrough~~ = Implemented
- **Bold** = High priority (based on Dynamic Expresso user demand)
- Regular = Nice to have

---

## CsEval Advantages (Already Implemented)

These features differentiate CsEval from competitors like Dynamic Expresso:

| Feature | Status | Notes |
|---------|--------|-------|
| ~~Full lambda in LINQ~~ | ✅ | `list.Where(x => x > 0)` - Dynamic Expresso's #1 missing feature! |
| ~~Block expressions~~ | ✅ | `{ var x = 1; if (x > 0) return x; }` |
| ~~If statements~~ | ✅ | `if (cond) { } else { }` |
| ~~Variable declarations~~ | ✅ | `var x = 5;` and `int x = 5;` |
| ~~Return statements~~ | ✅ | Early returns in blocks |
| ~~Object merging~~ | ✅ | `entity + new { Extra = value }` |
| ~~Spread operator~~ | ✅ | `[...arr1, ...arr2]`, `new { ...obj1, ...obj2 }` |
| ~~Null-coalescing assignment~~ | ✅ | `x ??= default` |
| ~~Null-conditional access~~ | ✅ | `obj?.Property` |
| ~~Bitwise operators~~ | ✅ | `&`, `\|`, `^`, `~`, `<<`, `>>` |
| ~~Interpolated strings~~ | ✅ | `$"Hello {name}"` |
| ~~Thread-safe evaluation~~ | ✅ | Child contexts for parallel execution |
| ~~DI integration~~ | ✅ | `IServiceProvider` at evaluation time |
| ~~Async methods~~ | ✅ | Methods returning `Task<T>` auto-unwrapped |
| ~~Cancellation support~~ | ✅ | `CancellationToken` auto-passed |
| ~~Pre-parsing~~ | ✅ | Parse once, evaluate many times |
| ~~All C# keywords reserved~~ | ✅ | Forward-compatible syntax |
| ~~Short-circuit &&/\|\|~~ | ✅ | `obj != null && obj.Prop` works correctly |

---

## High Priority Features (User Demand from Dynamic Expresso)

Based on [Dynamic Expresso GitHub issues](https://github.com/dynamicexpresso/DynamicExpresso/issues) analysis - these are the most requested features users can't get elsewhere:

### **1. Loops (MASSIVE market gap)**

Dynamic Expresso has ZERO loop support. This is their most painful limitation.

| Feature | Syntax | Priority | Notes |
|---------|--------|----------|-------|
| **`foreach`** | `foreach (var x in items) { }` | Critical | Most requested |
| **`while`** | `while (cond) { }` | Critical | Add iteration limit for safety |
| **`for`** | `for (var i = 0; i < n; i++) { }` | High | |
| `do-while` | `do { } while (cond)` | Medium | |
| `break` | `break;` | Required | Exit loop |
| `continue` | `continue;` | Next iteration |

### **2. Assignment & Mutation**

| Feature | Syntax | Priority | Notes |
|---------|--------|----------|-------|
| **Basic assignment** | `x = value` | Critical | Update existing variable |
| **Compound assignment** | `x += value` | Critical | Issue #251 in DE |
| **Index set** | `arr[0] = value` | Critical | DE can read but not write! |
| **Property set** | `obj.Prop = value` | High | |
| Increment/Decrement | `x++`, `--x` | Medium | Pre/post variants |

### **3. Type Operations**

| Feature | Syntax | Priority | Notes |
|---------|--------|----------|-------|
| **`is` operator** | `x is string`, `x is null` | High | Issue #202 in DE |
| **`as` operator** | `x as string` | High | Safe cast |
| **Type casting** | `(int)x` | High | |
| **`nameof`** | `nameof(property)` | Medium | Issue #112 in DE |
| `typeof` | `typeof(int)` | Medium | |
| `default` | `default(int)` | Low | |

### **4. Pattern Matching**

| Feature | Syntax | Priority | Notes |
|---------|--------|----------|-------|
| **`is` with type** | `x is string s` | High | Declare variable |
| **`is not`** | `x is not null` | High | Common pattern |
| Property pattern | `x is { Name: "John" }` | Medium | |
| Relational pattern | `x is > 0 and < 100` | Medium | |
| `switch` expression | `x switch { 1 => "one", _ => "other" }` | Medium | |

---

## Medium Priority Features

### Constructors & Generics

| Feature | Syntax | Notes |
|---------|--------|-------|
| Named constructor | `new DateTime(2024, 1, 1)` | Requires type registry |
| Generic method calls | `list.Cast<int>()` | Issue in DE |
| Object initializer | `new Person { Name = "John" }` | |
| Collection initializer | `new List<int> { 1, 2, 3 }` | |

### ~~LINQ Methods~~ ✅

**Implemented:**
- ~~Filtering: `Where`, `Distinct`~~
- ~~Projection: `Select`, `SelectMany`~~
- ~~Element: `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault`~~
- ~~Quantifiers: `Any`, `All`, `Contains`~~
- ~~Aggregation: `Count`, `Sum`, `Average`, `Min`, `Max`, `Aggregate`~~
- ~~Ordering: `OrderBy`, `OrderByDescending`~~
- ~~Grouping: `GroupBy` (returns `List<Dictionary>` with `Key` and `Items`)~~
- ~~Combining: `Zip` (with/without selector), `Concat`~~
- ~~Partitioning: `Take`, `Skip`~~
- ~~Conversion: `ToList`, `ToArray`, `Reverse`~~

**Not Implemented:**
| Method | Priority | Notes |
|--------|----------|-------|
| `Join`, `GroupJoin` | Low | Complex multi-collection |
| `OfType<T>`, `Cast<T>` | Medium | Need generic support |
| `ToDictionary` | Medium | Useful |
| `TakeWhile`, `SkipWhile` | Low | |
| `Except`, `Intersect`, `Union` | Low | Set operations |
| `MinBy`, `MaxBy` | Low | .NET 6+ |

### Range & Index (C# 8+)

| Feature | Syntax | Notes |
|---------|--------|-------|
| Index from end | `arr[^1]` | Last element |
| Range | `arr[1..3]` | Slice |
| Range from end | `arr[1..^1]` | |

### Strings

| Feature | Syntax | Notes |
|---------|--------|-------|
| Verbatim strings | `@"path\to\file"` | No escaping |
| Raw strings | `"""text"""` | C# 11 |

---

## Low Priority Features

### Exception Handling

| Feature | Syntax |
|---------|--------|
| `throw` | `throw new Exception("msg")` |
| `try-catch` | `try { } catch (Exception e) { }` |
| `try-finally` | `try { } finally { }` |

### Switch Statement

| Feature | Syntax |
|---------|--------|
| Switch statement | `switch (x) { case 1: ... }` |

---

## Features from Other Languages

### JavaScript/TypeScript

| Feature | Syntax | Status |
|---------|--------|--------|
| ~~Spread operator~~ | `[...arr1, ...arr2]` | ✅ |
| ~~Object spread~~ | `new { ...obj1, ...obj2 }` | ✅ |
| ~~Nullish assignment~~ | `x ??= value` | ✅ |
| Optional chaining call | `obj?.Method()` | Medium priority |
| Destructuring | `var { Name, Age } = person` | Low |

### Python

| Feature | Syntax | Notes |
|---------|--------|-------|
| `in` operator | `x in [1, 2, 3]` | Contains check |
| Chained comparison | `0 < x < 100` | Nice to have |
| Walrus operator | `if ((x := GetValue()) != null)` | Assign in expression |

### Functional (F#/Kotlin)

| Feature | Syntax | Notes |
|---------|--------|-------|
| Pipe operator | `x \|> Process \|> Format` | Chain functions |
| `in` range check | `x in 1..10` | Kotlin style |

---

## Infrastructure & DX

### AOT Support (Issue #283 in DE)

Important for modern .NET deployments - ensure no reflection-only patterns that break AOT.

### Developer Experience

| Feature | Status | Notes |
|---------|--------|-------|
| ~~Line/column in errors~~ | ✅ | Already have |
| "Did you mean?" suggestions | | Nice to have |
| Expression validation | | Without execution |
| VS Code syntax highlighting | | Tooling |

### Expression Tree Output

For EF Core / IQueryable integration:

```csharp
Expression<Func<T, bool>> predicate = engine.ParseAsExpression<Func<T, bool>>("x => x.Active");
dbContext.Users.Where(predicate);
```

---

## Non-Goals

Intentionally not supporting:

- **Full C# compilation** - Use Roslyn for that
- **Class/method definitions** - Expressions only
- **LINQ query syntax** - Method syntax only (`from x in y select x` → `y.Select(x => x)`)
- **Unsafe code** - No pointers
- **Preprocessor** - No `#if`

---

## Priority Summary

### Must Have (Unique Differentiators)
1. ~~Full lambda in LINQ~~ ✅ - **DONE, huge win over DE**
2. ~~Block expressions with if/return~~ ✅ - **DONE**
3. ~~Thread-safe evaluation~~ ✅ - **DONE**
4. Loops (`foreach`, `while`, `for`) - **#1 market gap**
5. Assignment operators (`=`, `+=`, index set) - **#2 market gap**

### Should Have (Common Requests)
6. Pattern matching (`is`, `is not`, `switch` expression)
7. Type operations (`as`, casting)
8. `nameof` operator
9. Generic method calls

### Nice to Have
10. Range/index operators
11. More LINQ methods
12. Exception handling
13. AOT optimization

---

## Competitor Analysis Summary

| Feature | CsEval | Dynamic Expresso | NCalc |
|---------|--------|------------------|-------|
| Lambda in LINQ | ✅ | ❌ (partial) | ❌ |
| Block expressions | ✅ | ❌ | ❌ |
| If statements | ✅ | ❌ | ❌ |
| Loops | ❌ | ❌ | ❌ |
| Assignment | ❌ | ❌ | ❌ |
| Object merging | ✅ | ❌ | ❌ |
| Spread operator | ✅ | ❌ | ❌ |
| Thread-safe | ✅ | ⚠️ (had issues) | ? |
| DI integration | ✅ | ❌ | ❌ |
| Performance | Fast | ~0.1ms | Fast |

**Key insight**: Adding loops and assignment would make CsEval the most feature-complete expression evaluator in the .NET ecosystem.
