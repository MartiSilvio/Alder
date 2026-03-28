---
title: "Language Reference"
description: "C# expression and statement semantics — Standard and Extended modes"
sidebar:
  order: 1
---

Alder implements C# expression and statement semantics per ECMA-334 (7th edition, December 2023). The language coverage includes literals, all operators with correct precedence and associativity, LINQ (method and query syntax), full pattern matching, lambdas with type inference, control flow, exception handling, tuples, deconstruction, checked/unchecked arithmetic, and more.

Two language modes are available:

**Standard** — ECMA-334 C# semantics. Every expression that compiles in C# with the same result works in Alder. The binder resolves types, the overload resolution follows §12.6.4, generic type inference follows §12.6.3, and numeric promotion follows §12.4.7.3.

**Extended** — a strict superset of Standard. Adds the power operator (`**`), pipeline operator (`|>`), chained comparisons (`0 <= x <= 100`), collection literals and comprehensions, `let..in` expressions, bare math functions (`sin`, `cos`, `sqrt`), aggregate built-ins (`sum`, `avg`, `count`), date/time sugar (`30.days`), SQL-style operators (`in`, `like`, `between`), and more. Every Standard expression is valid in Extended mode.

```csharp
// Standard
var engine = new AlderEngine();
engine.Evaluate("new[] { 1, 2, 3 }.Where(x => x > 1).Sum()"); // 5

// Extended
var ext = new AlderEngine(o => o.LanguageMode = LanguageMode.Extended);
ext.Evaluate("[x * x for x in 1..=10 if x % 2 == 0]"); // [4, 16, 36, 64, 100]
```

| Page | What it covers |
|------|---------------|
| [Standard Mode](standard.md) | Full ECMA-334 language reference — literals, operators, expressions, statements, pattern matching, LINQ, type system, scope boundaries |
| [Extended Mode](extended.md) | Extended operators, control flow sugar, collection features, bare math, aggregates, date/time, comprehensions, slicing |
