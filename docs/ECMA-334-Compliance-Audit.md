# ECMA-334 C# Specification Compliance Audit

**Specification Version:** ECMA-334 7th Edition (December 2023)
**CsEval Commit:** 6ecaa02
**Audit Date:** 2026-02-01

---

## Executive Summary

CsEval implements a **substantial subset** of the ECMA-334 C# specification. The implementation is intentionally scoped as an **expression and statement evaluator**, not a full C# compiler.

### Scope Clarification

**In Scope:** Expressions, control flow statements, LINQ, lambdas, variable declarations, method/property access on provided objects.

**Out of Scope (by design):** Type definitions (classes, structs, enums, interfaces), method/property definitions, namespace declarations, using directives, attributes, and other compile-time constructs. These require a full compiler, not an expression evaluator.

### Overall Compliance by Section

| ECMA-334 Section | Coverage | Notes |
|------------------|:--------:|-------|
| §6 Lexical Structure | ~80% | Missing: unicode escapes, digit separators |
| §8 Types | ~85% | Usage of types ✅; Type definitions out of scope |
| §10 Conversions | ~70% | Missing: explicit cast syntax `(T)x` |
| §12 Expressions | ~75% | Missing: is/as, typeof, default |
| §13 Statements | ~80% | Core statements ✅; yield/lock/using out of scope |
| Operator Precedence | 100% | Correct for all implemented operators |

---

## §6 Lexical Structure

### §6.3 Lexical Analysis

| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| Line terminators (CR/LF) | ✅ | Lexer.cs:314-316 | Basic support |
| Unicode line separators (LS/PS) | ❌ | — | Not detected |
| Single-line comments `//` | ✅ | Lexer.cs:284-287 | |
| Multi-line comments `/* */` | ✅ | Lexer.cs:289-297 | |
| Whitespace (space/tab/CR) | ✅ | Lexer.cs:309-312 | Unicode Zs not checked |

### §6.4.2 Unicode Character Escape Sequences

| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| `\uHHHH` in strings | ❌ | — | 🔴 HIGH PRIORITY |
| `\uHHHH` in chars | ❌ | — | 🔴 HIGH PRIORITY |
| `\uHHHH` in identifiers | ❌ | — | |
| `\UHHHHHHHH` (8 digits) | ❌ | — | 🔴 HIGH PRIORITY |

### §6.4.3 Identifiers

| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| Basic identifiers | ✅ | Lexer.cs:656-670 | Uses `char.IsLetter` |
| @ prefix for keywords | ❌ | — | Cannot use `@if` as identifier |
| Unicode categories | ❌ | — | Only ASCII letters |
| Unicode normalization Form C | ❌ | — | |

### §6.4.5 Literals

#### Boolean Literals (§6.4.5.2)
| Feature | Status | Notes |
|---------|:------:|-------|
| `true`, `false` | ✅ | Token.cs:17-18 |

#### Integer Literals (§6.4.5.3)
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| Decimal: `123` | ✅ | Lexer.cs:571-606 | |
| Decimal: `1_000_000` | ❌ | — | 🟡 Digit separators |
| Hex: `0xFF` | ✅ | Lexer.cs:608-629 | With auto type promotion |
| Binary: `0b1010` | ✅ | Lexer.cs:631-652 | With auto type promotion |
| Suffix: L, U, UL | ✅ | Lexer.cs:624-654 | |

#### Real Literals (§6.4.5.4)
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| Decimal point: `3.14` | ✅ | Lexer.cs:577-582 | |
| Exponent: `1e10` | ❌ | — | 🟡 MEDIUM |
| Leading decimal: `.5` | ❌ | — | 🟡 MEDIUM |
| Suffix: F, D, M | ✅ | Lexer.cs:633-635 | |

#### Character Literals (§6.4.5.5)
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| `'a'`, `'Z'` | ✅ | Lexer.cs:400-444 | TokenType.Character |
| Escape sequences | ✅ | Lexer.cs:413-427 | All C# escapes supported |
| `\xHH` hex escape | ❌ | — | 🟡 Variable-length hex |
| `\uHHHH` unicode | ❌ | — | 🟡 Unicode escapes |

#### String Literals (§6.4.5.6)
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| Regular strings `"hello"` | ✅ | Lexer.cs:366-398 | |
| Escape: `\n`, `\r`, `\t`, `\\` | ✅ | Lexer.cs:375-389 | |
| Escape: `\0`, `\a`, `\b`, `\f`, `\v` | ✅ | Lexer.cs:380-384 | All C# escapes supported |
| Verbatim strings `@"..."` | ✅ | Lexer.cs:469-503 | |
| Interpolated `$"...{x}..."` | ✅ | Lexer.cs:335-336 | |
| Interpolated verbatim `@$"..."` | ✅ | Lexer.cs:505-569 | |

---

## §8 Types

### §8.3 Value Types

#### Simple Types (§8.3.1-8.3.5)
| Type | Status | Notes |
|------|:------:|-------|
| sbyte, byte | ✅ | Token.cs:193-194 |
| short, ushort | ✅ | Token.cs:195-196 |
| int, uint | ✅ | Token.cs:184,197 |
| long, ulong | ✅ | Token.cs:185,198 |
| char | ✅ | Token keyword only, no char literals |
| float, double | ✅ | Token.cs:186-187 |
| decimal | ✅ | Token.cs:188 |
| bool | ✅ | Token.cs:190 |

#### Enum Types (§8.3.10)
| Feature | Status | Notes |
|---------|:------:|-------|
| Enum declaration | N/A | Out of scope (type definition) |
| Enum value usage | ✅ | Via registered modules/variables |
| Enum comparison | ✅ | Works with passed enum values |

#### Tuple Types (§8.3.11)
| Feature | Status | Notes |
|---------|:------:|-------|
| Tuple literals `(1, "x")` | ❌ | 🟡 Could be added |
| Named elements `(count: 1, name: "x")` | ❌ | 🟡 Could be added |
| Tuple deconstruction | ❌ | 🔵 Low priority |

#### Nullable Value Types (§8.3.12)
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| `T?` syntax | ✅ | Parser.cs:131-135 | |
| All integral types nullable | ✅ | TypeHelpers.cs:39-54 | |
| float?, double?, decimal? | ✅ | | |
| bool? | ✅ | | |
| HasValue, Value properties | ⚠️ | Via CLR | |

### §8.4 Constructed Types (Generics)
| Feature | Status | Notes |
|---------|:------:|-------|
| Generic type declarations | N/A | Out of scope (type definition) |
| Generic method calls `Method<T>()` | ❌ | 🟡 Could parse explicit type args |
| Using generic types | ✅ | Via registered modules/variables |

---

## §10 Conversions

### §10.2 Implicit Conversions

#### Implicit Numeric Conversions (§10.2.3)
| Conversion | Status | Location | Notes |
|------------|:------:|----------|-------|
| sbyte → short, int, long, float, double, decimal | ✅ | TypeHelpers.cs:56 | |
| byte → short, ushort, int, uint, long, ulong, float, double, decimal | ✅ | TypeHelpers.cs:57 | |
| short → int, long, float, double, decimal | ✅ | TypeHelpers.cs:58 | |
| ushort → int, uint, long, ulong, float, double, decimal | ✅ | TypeHelpers.cs:59 | |
| int → long, float, double, decimal | ✅ | TypeHelpers.cs:60 | |
| uint → long, ulong, float, double, decimal | ✅ | TypeHelpers.cs:61 | |
| long → float, double, decimal | ✅ | TypeHelpers.cs:62 | |
| ulong → float, double, decimal | ✅ | TypeHelpers.cs:63 | |
| char → ushort, int, uint, long, ulong, float, double, decimal | ✅ | TypeHelpers.cs:67 | |
| float → double | ✅ | TypeHelpers.cs:64 | |

**Implementation:** Uses `ImplicitConversions` dictionary in TypeHelpers.cs - matches ECMA-334 exactly.

#### Implicit Constant Expression Conversions (§10.2.11)
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| int constant → sbyte, byte, short, ushort | ✅ | TypeHelpers.cs:194-230 | Range checked |

### §10.3 Explicit Conversions

| Feature | Status | Notes |
|---------|:------:|-------|
| Cast syntax `(T)x` | ❌ | 🔴 HIGH PRIORITY - Parser treats `(expr)` as grouping only |
| Unboxing `object → int` | ❌ | Requires cast syntax |
| Narrowing conversions | ❌ | Requires cast syntax |

### §10.5 User-Defined Conversions
| Feature | Status | Notes |
|---------|:------:|-------|
| `implicit operator` | ❌ | Not supported |
| `explicit operator` | ❌ | Not supported |

### Decimal/Float Mixing
| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| decimal + float throws | ✅ | Operators.cs:133-142 | Matches C# spec |
| decimal + double throws | ✅ | Operators.cs:133-142 | Matches C# spec |

---

## §12 Expressions

### §12.4 Operator Precedence

CsEval implements **correct precedence and associativity** for all supported operators via recursive descent parsing.

| Level | ECMA Category | Operators | Status | Location |
|:-----:|---------------|-----------|:------:|----------|
| 1 | Primary | `.` `?.` `f()` `a[]` `x++` `x--` | ✅ | ParsePostfix() |
| 2 | Unary | `-` `!` `~` `++x` `--x` | ⚠️ | ParseUnary() - missing unary `+`, cast |
| 3 | Multiplicative | `*` `/` `%` | ✅ | ParseFactor() |
| 4 | Additive | `+` `-` | ✅ | ParseTerm() |
| 5 | Shift | `<<` `>>` | ✅ | ParseShift() |
| 6 | Relational | `<` `>` `<=` `>=` `is` `as` | ⚠️ | ParseComparison() - missing `is`, `as` |
| 7 | Equality | `==` `!=` | ✅ | ParseEquality() |
| 8 | Logical AND | `&` | ✅ | ParseBitwiseAnd() |
| 9 | Logical XOR | `^` | ✅ | ParseBitwiseXor() |
| 10 | Logical OR | `\|` | ✅ | ParseBitwiseOr() |
| 11 | Conditional AND | `&&` | ✅ | ParseAnd() |
| 12 | Conditional OR | `\|\|` | ✅ | ParseOr() |
| 13 | Null Coalescing | `??` | ✅ | ParseNullCoalesce() |
| 14 | Conditional | `? :` | ✅ | ParseConditional() |
| 15 | Assignment | `=` `+=` etc. | ✅ | ParseAssignment() |

**Associativity:** All correctly implemented
- Left-associative: Binary operators ✅
- Right-associative: Assignment, ternary, null-coalescing ✅

### §12.6 Function Members

| Feature | Status | Location | Notes |
|---------|:------:|----------|-------|
| Method invocation | ✅ | Evaluator.cs:125-145 | |
| Named arguments | ✅ | Parser.Expressions.cs:326-338 | |
| Property access | ✅ | Evaluator.cs:85-105 | |
| Indexer access | ✅ | Evaluator.cs:107-116 | |
| Overload resolution | ⚠️ | Via reflection | Simplified |

### §12.7 Primary Expressions

| Feature | Status | Notes |
|---------|:------:|-------|
| Literals | ✅ | int, long, double, decimal, string, char, bool, null |
| Simple names (identifiers) | ✅ | |
| Parenthesized `(expr)` | ✅ | |
| Member access `x.y` | ✅ | |
| Null-conditional `x?.y` | ✅ | |
| Invocation `f(x)` | ✅ | |
| Index access `a[x]` | ✅ | |
| `this` | N/A | Out of scope (requires class context) |
| `base` | N/A | Out of scope (requires class context) |
| `new` | ⚠️ | Anonymous objects only; typed constructors 🔴 |
| `typeof` | ❌ | Blocked for security (returns System.Type) |
| `default` | ❌ | 🔵 Could be added |
| `checked`/`unchecked` | ❌ | 🔵 Low priority |
| `delegate` | N/A | Out of scope (delegate declaration) |
| `stackalloc` | N/A | Out of scope (unsafe/low-level) |

### §12.9 Unary Operators

| Operator | Status | Notes |
|----------|:------:|-------|
| `-x` (negation) | ✅ | |
| `!x` (logical NOT) | ✅ | |
| `~x` (bitwise NOT) | ✅ | |
| `++x` (prefix increment) | ✅ | |
| `--x` (prefix decrement) | ✅ | |
| `+x` (unary plus) | ❌ | Missing |
| `(T)x` (cast) | ❌ | 🔴 HIGH PRIORITY |
| `await x` | ❌ | 🔵 |

### Type Testing Operators

| Operator | Status | Notes |
|----------|:------:|-------|
| `is` | ❌ | 🔴 HIGH PRIORITY |
| `is not` | ❌ | 🔴 HIGH PRIORITY |
| `is T name` | ❌ | 🔴 HIGH PRIORITY |
| `as` | ❌ | 🔴 HIGH PRIORITY |

---

## §13 Statements

### Implemented Statements

| Statement | Status | Location | Notes |
|-----------|:------:|----------|-------|
| Block `{ }` | ✅ | Evaluator.cs:369-392 | Proper scoping |
| Variable declaration | ✅ | Evaluator.cs:394-407 | `var` and typed |
| Expression statement | ✅ | Parser.Statements.cs:96-98 | |
| If statement | ✅ | Evaluator.cs:414-458 | |
| Switch statement | ✅ | Evaluator.Switch.cs | Fall-through prevented |
| While loop | ✅ | Evaluator.Loops.cs:18-56 | |
| Do-while loop | ✅ | Evaluator.Loops.cs:122-160 | |
| For loop | ✅ | Evaluator.Loops.cs:58-120 | |
| Foreach loop | ✅ | Evaluator.Loops.cs:162-210 | Per-iteration scoping (C# 5+) |
| Break | ✅ | Exception-based control flow |
| Continue | ✅ | Exception-based control flow |
| Return | ✅ | Evaluator.cs:460-464 | |

### Missing / Out of Scope Statements

| Statement | Status | Notes |
|-----------|:------:|-------|
| Empty statement `;` | ❌ | 🔵 Trivial to add |
| `throw` | ❌ | 🔵 Could be added |
| `try-catch-finally` | ❌ | 🔵 Could be added |
| `checked`/`unchecked` | ❌ | 🔵 Low priority |
| Labeled statement | N/A | Out of scope (requires goto) |
| `goto` | N/A | Out of scope (control flow complexity) |
| `lock` | N/A | Out of scope (threading construct) |
| `using` | N/A | Out of scope (resource management) |
| `yield` | N/A | Out of scope (iterator blocks) |
| Local functions | N/A | Out of scope (function definitions) |
| `const` declarations | ❌ | 🔵 Could be added |

### Foreach Loop Compliance (§13.9.5)

| Feature | Status | Notes |
|---------|:------:|-------|
| Per-iteration variable scope | ✅ | C# 5+ semantics |
| Closure capture fix | ✅ | Lambdas capture correctly |
| IDisposable cleanup | ⚠️ | IL mode only (ILCompiler.ControlFlow.cs:270-285) |

---

## Critical Gaps Summary

### 🔴 HIGH PRIORITY (Remaining Core Features)

1. **Explicit Cast Operator (§10.3)** - `(int)x`, `(long)y` not supported
   - Impact: No type narrowing, no explicit conversions
   - Fix: ~100 LOC in Parser + AST node + Evaluator

2. **Type Testing Operators (§12.12)** - `is`, `as` not implemented
   - Impact: Cannot check types at runtime
   - Fix: ~150 LOC

3. **Unicode Escapes (§6.4.2)** - `\uHHHH` not supported in strings/chars
   - Impact: International text handling limited
   - Fix: ~100 LOC in Lexer

### ✅ RECENTLY IMPLEMENTED

1. ~~Character Literals (§6.4.5.5)~~ - `'a'`, `'\n'`, `'\t'` ✅
2. ~~Hexadecimal Literals (§6.4.5.3)~~ - `0xFF`, `0x1A` ✅
3. ~~Binary Literals (§6.4.5.3)~~ - `0b1010` ✅
4. ~~Additional Escape Sequences~~ - `\0`, `\a`, `\b`, `\f`, `\v` ✅

### 🟡 MEDIUM PRIORITY (Nice to Have)

1. Digit separators `1_000_000`
2. Exponent notation `1e10`, `1.5E-3`
3. Tuple literals `(1, "x")`
4. Typed constructors `new DateTime(2024, 1, 1)`

### 🔵 LOW PRIORITY (Could Add)

1. Exception handling (try/catch/throw)
2. `default(T)` / `default`
3. checked/unchecked context

---

## Implementation Quality Assessment

### Strengths

1. **Correct Operator Precedence** - All 15 levels implemented correctly
2. **Proper Associativity** - Left/right associativity matches spec
3. **Implicit Numeric Conversions** - Complete per §10.2.3
4. **Decimal/Float Enforcement** - Correctly throws on mixing
5. **Nullable Types** - Full `T?` support for all value types
6. **Control Flow** - All loop types with proper scoping
7. **C# Parity Testing** - Tests compare against Roslyn

### Actual Gaps (Could Be Implemented)

1. **Limited Pattern Matching** - No `is`, `as`, property patterns
2. **No Explicit Cast** - Cannot write `(int)x` for type conversion
3. **No Exception Handling** - No try/catch in expressions
4. **Simplified Overload Resolution** - Uses reflection, not full C# rules

### Out of Scope (By Design)

1. **Type Definitions** - Classes, interfaces, enums, structs
2. **Method/Property Definitions** - Function bodies, accessors
3. **Namespace/Using Directives** - Compile-time constructs
4. **Attributes** - Compile-time metadata

---

## Compliance Testing Methodology

Tests use `TestHelpers.EvaluateCSharpAsync()` to compare CsEval output against actual C# behavior via Roslyn scripting:

```csharp
var csEvalResult = engine.Evaluate(expr);
var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);
Assert.That(csEvalResult, Is.EqualTo(csharpResult));
```

This ensures no false positives - if CsEval claims to support a feature, it must match C# behavior exactly.

---

## Recommendations

### Phase 1: Remaining Critical Fixes
1. ~~Implement character literals~~ ✅ Done
2. ~~Add hexadecimal/binary parsing~~ ✅ Done
3. Add explicit cast operator `(T)x`
4. Implement `is` and `as` operators

### Phase 2: Usability Improvements
1. Unicode escapes `\uHHHH`
2. Digit separators `1_000_000`
3. Exponent notation `1e10`
4. Typed constructors `new Type(...)`

### Phase 3: Nice to Have
1. Tuple literals `(1, "x")`
2. Exception handling (try/catch)
3. Pattern matching extensions

---

*This audit was conducted against ECMA-334 7th Edition (December 2023) using automated analysis tools and manual code review.*
