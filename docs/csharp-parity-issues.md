# CsEval C# Parity Issues

This document tracks discrepancies between CsEval and actual C# behavior, verified against Roslyn scripting and ECMA-334 7th Edition.

## Summary of Issues

### Type System (Critical - Breaks C# Parity)
| Issue | C# | CsEval | Spec Reference |
|-------|-----|--------|---------------|
| `int x = 10; x = 5.5;` | CS0266 Error | Accepts ❌ | §10.2 |
| `int x = 10; x += 5.5;` | CS0266 Error | Returns 15.5 ❌ | §12.21.4 |
| `byte a = 10;` | OK | "Cannot assign Int32 to byte" ❌ | §10.2.11 |
| `string? s = null;` | OK | "Unknown type 'string?'" ❌ | §8.3.12 |
| `(int)x` cast | OK | Not supported ❌ | §12.9.7 |
| `x is string` | OK | Not supported ❌ | §12.12.12 |
| `x as string` | OK | Not supported ❌ | §12.12.13 |

### Literals (Missing)
| Issue | C# | CsEval | Spec Reference |
|-------|-----|--------|---------------|
| `'a'` char literal | OK | Not supported ❌ | §6.4.5.5 |
| `0xFF` hex literal | OK | Not supported ❌ | §6.4.5.3 |
| `0b1010` binary | OK | Not supported ❌ | §6.4.5.3 |

### Expressions (Missing)
| Issue | C# | CsEval | Notes |
|-------|-----|--------|-------|
| `default(int)` | 0 | Not supported | §12.8.20 |
| `nameof(x)` | "x" | Not supported | §12.8.22 |
| `checked(x + y)` | Overflow check | Not supported | §12.8.19 |
| `typeof(T)` | Returns Type | Blocked | Security |

---

## Verified C# Rules (from ECMA-334 §12.21.4)

### Compound Assignment (`x op= y`)

1. If the return type of the operator is **implicitly** convertible to the type of x:
   - Evaluate as `x = x op y`

2. Otherwise, if the operator is predefined AND return type is **explicitly** convertible to x AND **y is implicitly convertible to type of x**:
   - Evaluate as `x = (T)(x op y)` where T is type of x

3. Otherwise: **compile-time error**

### Key Insight

For `int x = 10; x += 5.5;`:
- `int + double` returns `double`
- `double` is NOT implicitly convertible to `int` (rule 1 fails)
- `5.5` (double) is NOT implicitly convertible to `int` (rule 2 fails)
- **Result: CS0266 compile error**

This is different from `byte a = 200; a += 100;` which works because:
- `byte + int` returns `int`
- `int` is explicitly convertible to `byte` ✓
- `100` IS implicitly convertible to `byte` ✓
- **Result: `a = (byte)(a + 100)` = 44**

## Identified Discrepancies

| Code | C# Result | CsEval Result | Issue |
|------|-----------|---------------|-------|
| `var x = 10; x = 5.5;` | CS0266 Error | `5.5` (Double) | Missing type check on assignment |
| `var x = 10; x = 5L;` | CS0266 Error | `5` (Int64) | Missing type check on assignment |
| `var x = 10; x += 5.5;` | CS0266 Error | `15.5` (Double) | Missing compound assignment rule check |
| `var x = 10; x -= 2.5;` | CS0266 Error | Would accept | Missing compound assignment rule check |
| `var x = 10; x *= 1.5;` | CS0266 Error | Would accept | Missing compound assignment rule check |
| `var x = 10; x /= 2.5;` | CS0266 Error | Would accept | Missing compound assignment rule check |
| `long x = 10L; x = 5;` | `5` (Int64) | `5` (Int32) | Type not preserved in assignment |

## What Works Correctly

- `int? x = null; x ??= 42;` → Works ✓
- `var x = 10; x ??= 42;` → Correctly rejects ✓
- `var x = 10.0; x += 5;` → Works (int implicitly converts to double) ✓
- `var x = 10; x += 5;` → Works ✓

## Fix Requirements

### 1. Track Variable Types
- `var x = 10` should record that x is `Int32`
- Currently tracks runtime type but may not enforce on assignment

### 2. Validate Assignment Type Compatibility
In `VisitAssign`:
```csharp
// Check: is newValue implicitly convertible to variable's declared type?
if (!IsImplicitlyConvertible(newValue.GetType(), varType))
    throw new CsEvalException($"Cannot implicitly convert type '{newValue.GetType().Name}' to '{varType.Name}'");
```

### 3. Validate Compound Assignment per §12.21.4
In `VisitCompoundAssign`:
```csharp
// Get types
var targetType = _context.GetVariableType(name);
var rightType = rightValue?.GetType();

// Rule 2: Check if RHS is implicitly convertible to target type
if (!IsImplicitlyConvertible(rightType, targetType))
    throw new CsEvalException($"Cannot implicitly convert type '{resultType.Name}' to '{targetType.Name}'");

// If we get here, apply the cast: result = (T)(x op y)
result = Convert.ChangeType(result, targetType);
```

### 4. Preserve Type on Assignment
When assigning compatible values, ensure the result maintains the variable's type:
```csharp
// long x = 10L; x = 5; // 5 (int) should become 5L (long)
value = Convert.ChangeType(value, varType);
```

## Additional Discrepancies Found

### 1. Constant Expression Conversions (§10.2.11)

C# allows implicit conversion of constant expressions within range:
```csharp
byte a = 10;  // Works in C# - 10 is in byte range
```

**CsEval behavior:** Fails with "Cannot assign Int32 to byte variable"

**Fix needed:** Check if the source is a constant int within the target type's range.

### 2. Nullable Reference Types

C# syntax:
```csharp
string? s = null;
```

**CsEval behavior:** Fails with "Unknown type 'string?'"

**Fix needed:** Add `string?` to TypeNameToClrType (resolves to `string` since reference types are already nullable).

### 3. Increment/Decrement on Small Types

For `byte x = 255; x++;`:
- C#: Works, wraps to 0 (predefined ++ for byte exists per §12.8.15)
- CsEval: Currently may have issues with type preservation

## Implicit Conversion Table (from ECMA-334 §10.2.3)

From → To (implicit):
- `sbyte` → `short`, `int`, `long`, `float`, `double`, `decimal`
- `byte` → `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`
- `short` → `int`, `long`, `float`, `double`, `decimal`
- `ushort` → `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`
- `int` → `long`, `float`, `double`, `decimal`
- `uint` → `long`, `ulong`, `float`, `double`, `decimal`
- `long` → `float`, `double`, `decimal`
- `ulong` → `float`, `double`, `decimal`
- `char` → `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`
- `float` → `double`

Note: There is NO implicit conversion from `double` to `int`, `long` to `int`, etc.

## Implicit Constant Expression Conversions (§10.2.11)

A constant expression of type `int` can be converted to:
- `sbyte`, `byte`, `short`, `ushort`, `uint`, `ulong`

**Provided** the value is within the range of the destination type.

Example:
```csharp
byte b = 255;    // OK - 255 in range [0, 255]
byte c = 256;    // Error - 256 out of range
sbyte s = -128;  // OK - -128 in range [-128, 127]
sbyte t = 128;   // Error - 128 out of range
```

## What CsEval Gets Right ✅

These features have been verified to match C# behavior:

- Integer division truncation: `5/2` = 2
- Modulo with negatives: `-7 % 3` = -1
- Numeric promotions: `int + long` → `long`, `int + double` → `double`
- Comparison operators: `<`, `>`, `<=`, `>=`, `==`, `!=`
- Bitwise operators: `&`, `|`, `^`, `~`, `<<`, `>>`
- Boolean logic & short-circuit: `&&`, `||`, `!`
- Ternary operator: `a ? b : c`
- Null coalesce: `??`, `??=` (on nullable types)
- `var` type inference
- Implicit widening conversions: `long x = 10;`, `double x = 10;`
- Control flow: `if`, `for`, `while`, `do-while`, `foreach`, `switch`
- LINQ methods (via delegation to System.Linq)
- String interpolation: `$"Hello {name}"`
- Anonymous objects: `new { Name = "John" }`
- Array literals: `[1, 2, 3]`

## Priority Order for Fixes

### High Priority (Breaks basic C# expectations)
1. **Assignment type checking** - `int x = 10; x = 5.5;` should error
2. **Compound assignment rules** - `int x = 10; x += 5.5;` should error
3. **Constant expression conversions** - `byte a = 10;` should work
4. **Cast expressions** - `(int)x`, `(double)y` are essential
5. **Type pattern `is`** - `x is string` is very common

### Medium Priority
6. **Char literals** - `'a'` is basic C#
7. **Hex literals** - `0xFF` is common
8. **`as` operator** - `x as string` for safe casting
9. **Nullable reference type syntax** - `string?`

### Low Priority
10. **`default(T)`** - Can use literal `0`, `null` instead
11. **`nameof`** - Convenience only
12. **`checked`/`unchecked`** - Rare in expressions
