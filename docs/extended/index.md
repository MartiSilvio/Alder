---
title: "Extended Mode"
description: "What Extended mode is, how to enable it, philosophy, and a complete feature registry."
sidebar:
  order: 1
---

Extended mode is CsEval's opt-in language superset. It adds operators, syntax sugar, built-in functions, and convenience features inspired by Python, Ruby, Perl, SQL, and Rust.

Everything in Standard mode still works. Extended mode only **adds** features -- Standard mode code is valid Extended mode code.

## How to Enable

Pass `LanguageMode.Extended` when constructing the engine:

```csharp
var engine = new CsEvalEngine(new CsEvalOptions { LanguageMode = LanguageMode.Extended });
```

Or create new options from existing ones using `with`:

```csharp
var options = CsEvalOptions.Default with { LanguageMode = LanguageMode.Extended };
var engine = new CsEvalEngine(options);
```

## Philosophy

Extended mode is for scripting convenience, not language purity. It favors expressiveness and readability for non-C# users -- data analysts, rule authors, configuration writers. It draws from languages users already know.

If you are embedding CsEval as a rule engine or calculator, Extended mode lets your users write `price between 10 and 100` instead of `price >= 10 && price <= 100`. If you are building a data pipeline, `values |> sum` reads more naturally than calling aggregate functions directly.

## Feature Registry

Every Extended-mode feature, organized by category. Each links to the page where it is documented.

### Operators

| Feature | Syntax | Page |
|---|---|---|
| Power | `**`, `**=` | [Operators](./operators) |
| Inclusive range | `..=` | [Operators](./operators) |
| Explicit exclusive range | `..<` | [Operators](./operators) |
| Pipeline | `\|>` | [Operators](./operators) |
| Spaceship (three-way comparison) | `<=>` | [Operators](./operators) |
| Chained comparisons | `0 < x < 10` | [Operators](./operators) |
| Regex match / not match | `=~`, `!~` | [Operators](./operators) |
| Strict equality / inequality | `===`, `!==` | [Operators](./operators) |
| Containment | `in`, `not in` | [Operators](./operators) |
| SQL pattern matching | `like`, `not like` | [Operators](./operators) |
| Range check | `between ... and` | [Operators](./operators) |
| Word logical operators | `and`, `or`, `not` | [Operators](./operators) |

### Syntax Sugar

| Feature | Syntax | Page |
|---|---|---|
| Unless statement | `unless (cond) { ... }` | [Syntax Sugar](./syntax-sugar) |
| Until loop | `until (cond) { ... }` | [Syntax Sugar](./syntax-sugar) |
| Let-in binding | `let x = expr in body` | [Syntax Sugar](./syntax-sugar) |
| Destructuring let-in | `let {a, b} = expr in body` | [Syntax Sugar](./syntax-sugar) |
| List comprehension | `[x * x for x in items]` | [Syntax Sugar](./syntax-sugar) |
| Filtered comprehension | `[x for x in items if x > 0]` | [Syntax Sugar](./syntax-sugar) |
| Array literals | `[1, 2, 3]` | [Syntax Sugar](./syntax-sugar) |
| Array spread | `[..a, ..b]` | [Syntax Sugar](./syntax-sugar) |
| Object spread | `new { ..existing, Name = "new" }` | [Syntax Sugar](./syntax-sugar) |
| If-expression | `if (cond) expr else expr` | [Syntax Sugar](./syntax-sugar) |
| String repetition | `"ha" * 3` | [Syntax Sugar](./syntax-sugar) |
| Slicing | `arr[1:3]`, `arr[::2]` | [Syntax Sugar](./syntax-sugar) |

### Built-in Functions

| Feature | Examples | Page |
|---|---|---|
| Bare math functions | `sin`, `cos`, `sqrt`, `abs`, `log`, `round`, ... | [Built-in Functions](./built-in-functions) |
| Math constants | `pi`, `e`, `tau`, `infinity`, `nan` | [Built-in Functions](./built-in-functions) |
| Date/time functions | `now()`, `today()` | [Built-in Functions](./built-in-functions) |
| Date arithmetic sugar | `5.days`, `2.hours`, `30.minutes` | [Built-in Functions](./built-in-functions) |
| Aggregate builtins | `sum()`, `avg()`, `count()`, `min()`, `max()` | [Built-in Functions](./built-in-functions) |
| `it` placeholder | `items.Where(it > 5)` | [Built-in Functions](./built-in-functions) |

### Convenience

| Feature | Example | Page |
|---|---|---|
| Negative indexing | `arr[-1]` | [Negative Indexing](./negative-indexing) |
| Range iteration | `foreach (var i in 1..=5)` | [Operators](./operators) |

## Standard Mode Errors

Attempting an Extended-only feature in Standard mode throws `CsEvalLanguageModeException` with a message identifying the feature:

```csharp
var engine = new CsEvalEngine(); // Standard mode (default)

engine.Evaluate("2 ** 10");
// throws CsEvalLanguageModeException: "Feature '**' requires Extended mode"
```

## See Also

- [Operators](./operators) -- power, range, pipeline, spaceship, regex, strict equality, containment, pattern matching, word operators
- [Syntax Sugar](./syntax-sugar) -- unless, until, let-in, comprehensions, array literals, spread, slicing
- [Built-in Functions](./built-in-functions) -- math, date, aggregates, `it` placeholder
- [Negative Indexing](./negative-indexing) -- `arr[-1]` semantics
