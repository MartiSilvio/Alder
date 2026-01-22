# CsEval Roadmap

Features to implement for full C# developer familiarity, plus useful additions from other languages.

---

## C# Parity Features

### Type Operations

| Feature | Syntax | Notes |
|---------|--------|-------|
| Type casting | `(int)x`, `(string)obj` | Use `Convert.ChangeType` |
| `is` operator | `x is string`, `x is null` | Type checking |
| `as` operator | `x as string` | Safe cast, returns null |
| `typeof` | `typeof(int)` | Get Type reference |
| `default` | `default(int)`, `default` | Default value |
| `nameof` | `nameof(property)` | Get name as string |

### Bitwise Operators

| Operator | Syntax |
|----------|--------|
| Bitwise AND | `x & y` |
| Bitwise OR | `x \| y` |
| Bitwise XOR | `x ^ y` |
| Bitwise NOT | `~x` |
| Left shift | `x << n` |
| Right shift | `x >> n` |

### Assignment Operators

| Operator | Syntax | Notes |
|----------|--------|-------|
| Assignment | `x = value` | Basic assignment |
| Add-assign | `x += value` | Desugar to `x = x + value` |
| Subtract-assign | `x -= value` | |
| Multiply-assign | `x *= value` | |
| Divide-assign | `x /= value` | |
| Increment | `x++`, `++x` | Pre/post variants |
| Decrement | `x--`, `--x` | |

### Loops

| Feature | Syntax | Notes |
|---------|--------|-------|
| `foreach` | `foreach (var x in items) { }` | Most useful |
| `while` | `while (cond) { }` | Add iteration limit |
| `for` | `for (var i = 0; i < n; i++) { }` | |
| `break` | `break;` | Exit loop |
| `continue` | `continue;` | Next iteration |

### Switch

| Feature | Syntax | Notes |
|---------|--------|-------|
| Switch expression | `x switch { 1 => "one", _ => "other" }` | C# 8+ style |
| Switch statement | `switch (x) { case 1: ... }` | Classic style |

### Constructors & Types

| Feature | Syntax | Notes |
|---------|--------|-------|
| Named constructor | `new DateTime(2024, 1, 1)` | Requires type registry |
| Object initializer | `new Person { Name = "John" }` | |
| Collection initializer | `new List<int> { 1, 2, 3 }` | |
| Typed array | `new int[5]` | |
| Array initializer | `new[] { 1, 2, 3 }` | Type inference |
| Generic calls | `list.Cast<int>()` | |
| Generic construction | `new List<int>()` | |

### Pattern Matching

| Feature | Syntax | Notes |
|---------|--------|-------|
| Type pattern with variable | `x is string s` | Declares `s` |
| Property pattern | `x is { Name: "John" }` | |
| Relational pattern | `x is > 0 and < 100` | |
| List pattern | `x is [1, 2, ..]` | |
| `not` pattern | `x is not null` | |

### Additional LINQ

Missing methods:
- `GroupBy`, `Join`, `GroupJoin`
- `SelectMany`, `Zip`
- `OfType<T>`, `Cast<T>`
- `ToDictionary`, `ToLookup`, `ToHashSet`
- `Chunk`, `DistinctBy`, `ExceptBy`, `IntersectBy`, `UnionBy`
- `MinBy`, `MaxBy`
- `ThenBy`, `ThenByDescending`

### Range & Index (C# 8+)

| Feature | Syntax | Notes |
|---------|--------|-------|
| Index from end | `arr[^1]` | Last element |
| Range | `arr[1..3]` | Slice |
| Range from end | `arr[1..^1]` | |
| Open range | `arr[1..]`, `arr[..3]` | |

### Strings

| Feature | Syntax | Notes |
|---------|--------|-------|
| Verbatim strings | `@"path\to\file"` | No escaping |
| Raw strings | `"""text"""` | C# 11 |

### Exception Handling

| Feature | Syntax |
|---------|--------|
| `throw` | `throw new Exception("msg")` |
| `try-catch` | `try { } catch (Exception e) { }` |
| `try-finally` | `try { } finally { }` |

---

## Features from Other Languages

### JavaScript/TypeScript

| Feature | Syntax | Notes |
|---------|--------|-------|
| ~~Spread operator~~ | `[...arr1, ...arr2]` | ✅ Implemented |
| ~~Object spread~~ | `new { ...obj1, ...obj2 }` | ✅ Implemented |
| Optional chaining call | `obj?.Method()` | Already have `?.` for props |
| Nullish assignment | `x ??= value` | ✅ Already implemented |
| Destructuring | `var { Name, Age } = person` | Extract properties |
| Template literals | `` `Hello ${name}` `` | We have `$""` |

### Python

| Feature | Syntax | Notes |
|---------|--------|-------|
| List comprehension | `[x * 2 for x in items if x > 0]` | Alt syntax for LINQ |
| `in` operator | `x in [1, 2, 3]` | Contains check |
| `not in` operator | `x not in list` | |
| Slice syntax | `arr[1:3]` | We'd use `arr[1..3]` |
| Walrus operator | `if ((x := GetValue()) != null)` | Assign in expression |
| Chained comparison | `0 < x < 100` | |

### Kotlin

| Feature | Syntax | Notes |
|---------|--------|-------|
| Elvis operator | `x ?: default` | We have `??` |
| Safe call | `x?.let { it.Process() }` | |
| `when` expression | `when (x) { 1 -> "one" }` | Like switch |
| `in` range check | `x in 1..10` | |
| String templates | `"Hello $name"` | Simpler than `{name}` |

### Rust

| Feature | Syntax | Notes |
|---------|--------|-------|
| `if let` | `if let Some(x) = opt { }` | Pattern + condition |
| Match guards | `x switch { n when n > 0 => ... }` | |
| `..` ignore pattern | `{ Name, .. }` | Ignore rest |

### F#/Functional

| Feature | Syntax | Notes |
|---------|--------|-------|
| Pipe operator | `x \|> Process \|> Format` | Chain functions |
| Function composition | `f >> g` | Combine functions |
| Option binding | `x.Map(v => v * 2)` | On nullables |

---

## Unique CsEval Features (Keep/Enhance)

These differentiate CsEval - keep and improve:

| Feature | Current | Enhancement Ideas |
|---------|---------|-------------------|
| Object merging | `a + b` | ✅ Also supports spread `new { ...a, ...b }` |
| Block expressions | `{ var x = 1; return x; }` | Add more statement types |
| DI integration | Module resolution | Auto-discover from assembly |
| Async + cancellation | `EvaluateAsync` | Progress reporting |

---

## Expression Tree Output

For EF Core / IQueryable integration:

```csharp
// Parse to expression tree instead of evaluating
Expression<Func<T, bool>> predicate = engine.ParseAsExpression<Func<T, bool>>("x => x.Active");
dbContext.Users.Where(predicate);
```

This is a significant feature that enables database query translation.

---

## Developer Experience

### Error Messages
- Line/column numbers in errors
- "Did you mean X?" suggestions
- Syntax highlighting for error location

### Debugging
- Step-through evaluation
- Variable watch
- Expression trace/explain

### Tooling
- VS Code syntax highlighting
- Expression validation without execution
- Type inference / schema detection

---

## Non-Goals

Intentionally not supporting:

- **Full C# compilation** - Use Roslyn
- **Class/method definitions** - Expressions only
- **Async/await syntax** - Methods can be async internally
- **LINQ query syntax** - Method syntax only (`from x in y select x` → `y.Select(x => x)`)
- **Unsafe code** - No pointers
- **Preprocessor** - No `#if`
