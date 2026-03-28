---
title: "Numeric Promotion"
description: "ECMA-334 §12.4.7.3 binary numeric promotion, unary promotion, constant promotion, char edge cases"
sidebar:
  order: 8
---

Alder implements ECMA-334 numeric promotion rules for both binary and unary operators. These rules determine what type the operands are promoted to before an arithmetic, comparison, or bitwise operation is executed.

## Binary Numeric Promotion (§12.4.7.3)

When a binary operator is applied to two numeric operands, both are promoted to a common type. The rules are applied in order — the first matching rule wins:

| Rule | Condition | Promoted type |
|------|-----------|--------------|
| 1 | Either operand is `decimal` | `decimal` — error if the other is `float` or `double` |
| 2 | Either operand is `double` | `double` |
| 3 | Either operand is `float` | `float` |
| 4 | Either operand is `ulong` | `ulong` — error if the other is a signed integer type |
| 5 | Either operand is `long` | `long` |
| 6 | One is `uint`, other is `sbyte`/`short`/`int` | `long` |
| 7 | Either operand is `uint` | `uint` |
| 8 | Default | `int` |

Rule 8 applies to `byte`, `sbyte`, `short`, `ushort`, and `char` — all promote to `int`.

### The `char` edge case

`char` has implicit conversions to `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal` per §10.2.3. The critical detail: **`char` is not a signed integer type**. This affects Rule 4 and Rule 6:

- **Rule 4**: `ulong + char` → `ulong` (valid — `char` is not signed, so no error)
- **Rule 6**: `uint + char` → `uint` (Rule 7, not Rule 6 — because `char` is not in the `sbyte`/`short`/`int` set)

If `char` were treated as signed, `uint + char` would promote to `long` via Rule 6. Instead, it stays `uint` via Rule 7. This matches the C# specification exactly.

### `decimal` isolation

Rule 1 enforces that `decimal` cannot be mixed with `float` or `double`. The expression `1.0m + 1.0` throws a compile-time error — there is no implicit conversion between `decimal` and floating-point types. This prevents precision loss from the different numeric representations.

### `ulong` + signed error

Rule 4 throws an error when `ulong` is mixed with a signed integer type (`sbyte`, `short`, `int`, `long`). The signed value could be negative, which has no representation in `ulong`. Rather than silently truncating, Alder (like the C# compiler) rejects this at compile time.

## Unary Numeric Promotion (§12.4.7.2)

For unary operators (`-`, `+`, `~`), the operand is promoted:

| Operand type | Promoted to |
|-------------|------------|
| `byte`, `sbyte`, `short`, `ushort`, `char` | `int` |
| All other numeric types | Unchanged |

Special case: unary `-` on `uint` promotes to `long` (because `-uint.MaxValue` doesn't fit in `uint` or `int`).

## Constant Expression Promotion (§10.2.11)

ECMA-334 allows implicit conversions for constant expressions that don't apply to non-constant values:

| Constant value | Can implicitly convert to |
|---------------|--------------------------|
| Non-negative `int` constant | `uint` (if value ≤ `uint.MaxValue`) |
| Non-negative `int` constant | `ulong` (if value ≤ `ulong.MaxValue`) |
| Non-negative `long` constant | `ulong` (if value ≤ `ulong.MaxValue`) |

This is why `uint x = 0` compiles — `0` is an `int` constant, but §10.2.11 allows the conversion because the value is non-negative and fits.

In Alder, constant promotion is applied at runtime in the interpreter's binary evaluation path. When one operand is a literal and the other is `uint` or `ulong`, `TryConstantPromotion` checks the value and promotes if safe:

```
uint x = 5;
x + 1          // 1 is int, but constant → promoted to uint, result is uint
x + (-1)       // -1 is int, negative → cannot promote, falls to Rule 6: long
```

## Fast Path vs Fallback

The interpreter uses two dispatch tiers for numeric operations:

**Fast path** (`NumericDispatch` with `PromotedType`): When the binder has computed a `PromotedType` at bind time and the runtime types match the static types, the engine routes directly to pre-built delegate tables keyed by `(Type, Type)`. No promotion at runtime — the binder already determined the promoted type. This is the path for expressions like `1 + 2` where both types are known.

**Fallback path** (`NumericDispatch.PromoteOperands`): When types don't match the fast path (e.g., untyped variables, mixed-type arithmetic where the binder produced `BoundType.Unknown`), the runtime applies the 8-rule promotion chain, converts both operands, and then dispatches to the delegate table with the promoted type pair.

The delegate tables contain 7 entries each: `(int,int)`, `(long,long)`, `(float,float)`, `(double,double)`, `(decimal,decimal)`, `(uint,uint)`, `(ulong,ulong)`. After promotion, both operands always have the same type, so the key is always `(T, T)`.

## Checked Arithmetic

In a `checked` context, integer arithmetic operators use separate delegate tables (`CheckedAddOps`, `CheckedSubtractOps`, `CheckedMultiplyOps`) that wrap operations in `checked()`:

```csharp
checked(int.MaxValue + 1)   // throws OverflowException
unchecked(int.MaxValue + 1)  // wraps to int.MinValue
```

Floating-point operations are unaffected by `checked`/`unchecked` — they follow IEEE 754 regardless. Division by zero produces `Infinity` or `NaN` for floating-point, and throws `DivideByZeroException` for integers.

## Nullable Arithmetic

When either operand of a binary operation is `null`, the result is `null`. This applies to all arithmetic, comparison, and bitwise operations on nullable numeric types.

For `bool?`, the `&` and `|` operators implement ECMA-334's three-valued logic (§12.13.5, §12.14.2):

| `&` | `true` | `false` | `null` |
|-----|--------|---------|--------|
| `true` | `true` | `false` | `null` |
| `false` | `false` | `false` | `false` |
| `null` | `null` | `false` | `null` |

| `\|` | `true` | `false` | `null` |
|------|--------|---------|--------|
| `true` | `true` | `true` | `true` |
| `false` | `true` | `false` | `null` |
| `null` | `true` | `null` | `null` |
