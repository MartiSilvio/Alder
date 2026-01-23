# CsEval Design Decisions

This document explains key architectural decisions in CsEval and the rationale behind them.

## LINQ Returns `List<object?>` (Immediate Evaluation)

CsEval intentionally returns `List<object?>` from LINQ methods rather than `IEnumerable<T>`. This is a deliberate design choice, not a limitation.

**Why immediate evaluation?**

1. **Context Safety**: With deferred execution, the evaluation context may change or be disposed by the time the sequence is enumerated. Immediate evaluation ensures results are captured at evaluation time.

2. **Closure Capture**: Lambda expressions in deferred LINQ chains capture the evaluator's context. If the context changes between definition and enumeration, results become unpredictable.

3. **Multiple Enumeration**: `List<object?>` can be enumerated multiple times safely. Deferred sequences may have side effects on re-enumeration or may not support it at all.

4. **Index Access**: Lists support direct index access (`result[0]`), which is commonly needed in expressions. Deferred sequences require `.ElementAt()` or `.ToList()` first.

5. **Predictability**: Expression evaluation should be deterministic. Deferred execution introduces timing dependencies that make debugging difficult.

**Trade-off**: This means LINQ chains are always fully evaluated, even for large collections. For performance-critical scenarios with large datasets, consider filtering in the data source before passing to CsEval.

## Numeric Type Handling

CsEval matches C# numeric literal behavior:

**Literal parsing:**
- `42` → `int` (default, auto-promotes to `long` if too large for int)
- `42L` → `long` (explicit suffix)
- `42U` → `uint`, `42UL` → `ulong`
- `3.14` → `double` (default for floating-point)
- `3.14f` → `float`, `3.14m` → `decimal` (explicit suffixes)

**Arithmetic result types (matches C#):**

| Operation | Result Type |
|-----------|-------------|
| `decimal` op anything | `decimal` |
| `double`/`float` op non-decimal | `double` |
| `int` op `int` | `int` |
| `int` op `long` | `long` |
| `long` op `long` | `long` |
| small types (`byte`, `short`) | promote to `int` |
| Division (non-decimal) | `double` |

**Precision:**
- `decimal`: 28-29 significant digits
- `double`/`float`: 15-17 significant digits
- When mixing types, the higher-precision type wins

**Type coercion:**
- When comparing values (e.g., `list.Contains(2)`), CsEval automatically handles type mismatches between `int`, `long`, `double`, `float`, `decimal` by converting both values to `double` for comparison.
- External types (`float`, `decimal`, `short`, `byte`) work seamlessly in expressions.

## GroupBy Returns Dictionaries

Unlike C#'s `IGrouping<TKey, TElement>`, CsEval's `GroupBy` returns dictionaries with `Key` and `Items` properties:

```csharp
items.GroupBy(x => x.Category)
// Returns: [{ Key: "A", Items: [...] }, { Key: "B", Items: [...] }]
```

This simplifies access in expressions and avoids the complexity of generic interface handling.

## Zip Without Selector Returns Dictionaries

C# 10+ returns `ValueTuple<T1, T2>` for `Zip` without a result selector. CsEval returns dictionaries with `First` and `Second` properties:

```csharp
names.Zip(ages)
// Returns: [{ First: "Alice", Second: 30 }, { First: "Bob", Second: 25 }]
```

This provides named access without requiring tuple syntax support in the parser.
