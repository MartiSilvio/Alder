# CsEval Gap Audit

Comprehensive audit of CsEval against ECMA-334 (7th edition) and standard C# behavior.
Cross-referenced against parser, binder, runtime, compiled emitter, and existing test coverage.

---

## Spec Coverage Matrix

| Spec Section | Rule | Implemented | Tested | Status |
|---|---|---|---|---|
| §6.4.2 | Unicode escapes in identifiers | No | No | ❌ L2 |
| §6.4.3 | Verbatim identifiers (`@keyword`) | No | No | ❌ L1 |
| §6.4.5.2 | Boolean literals | Yes | Yes | ✅ |
| §6.4.5.3 | Integer literals (hex, binary, separators, suffixes) | Yes | Yes | ✅ |
| §6.4.5.4 | Real literals (float, double, decimal, scientific) | Yes | Yes | ✅ |
| §6.4.5.5 | Character literals (escapes, unicode, hex) | Yes | Yes | ✅ |
| §6.4.5.6 | String literals (regular, verbatim, interpolated, raw) | Yes | Yes | ✅ |
| §6.4.6 | `::` operator (namespace alias qualifier) | No | No | ❌ L4 |
| §6.5 | Preprocessing directives | No | No | ❌ L3 |
| §8.3.11 | Tuple types — named elements | Partial | No | 🐞 R25 |
| §8.3.12 | Nullable value types | Yes | Yes | ✅ |
| §8.3.13 | Boxing/unboxing | Yes | Partial | ⚠️ |
| §10.2.3 | Implicit numeric conversions | Yes | Yes | ✅ |
| §10.2.4 | Implicit enumeration conversion (const 0 → enum) | Partial | No | ⚠️ P22 |
| §10.2.11 | Implicit constant expression conversions | Yes | Yes | ✅ |
| §10.3.2 | Explicit numeric conversions | Yes | Yes | ✅ |
| §10.3.3 | Explicit enumeration conversions | Yes | Yes | ✅ |
| §10.5 | User-defined conversions (op_Implicit/op_Explicit) | No | No | ❌ R5/R14 |
| §10.6 | Nullable conversions | Yes | Yes | ✅ |
| §11.2 | Pattern forms — positional/tuple patterns | No | No | ❌ P19 |
| §11.2.2 | Declaration pattern | Yes | Yes | ✅ |
| §11.2.3 | Constant pattern | Yes | Yes | ✅ |
| §11.2.4 | Var pattern | Yes | Yes | ✅ |
| §12.4.7.2 | Unary numeric promotions | Yes | Yes | ✅ |
| §12.4.7.3 | Binary numeric promotions | Partial | Partial | 🐞 R22 |
| §12.4.8 | Lifted operators | Yes | Yes | ✅ |
| §12.6.3 | Type inference (generic methods) | Partial | Partial | ⚠️ R6 |
| §12.6.4 | Overload resolution | Partial | Yes | ⚠️ R5/R8/R9 |
| §12.8.3 | Interpolated string expressions | Yes | Yes | ✅ |
| §12.8.4 | Simple names | Yes | Yes | ✅ |
| §12.8.6 | Tuple expressions | Partial | Partial | 🐞 R25 |
| §12.8.7 | Member access | Yes | Yes | ✅ |
| §12.8.8 | Null-conditional member access | Yes | Yes | ✅ |
| §12.8.9 | Invocation expressions | Yes | Yes | ✅ |
| §12.8.10 | Null-conditional invocation | Yes | Yes | ✅ |
| §12.8.11 | Element access | Yes | Yes | ✅ |
| §12.8.12 | Null-conditional element access | Yes | Yes | ✅ |
| §12.8.15 | Postfix increment/decrement | Yes | Yes | ✅ |
| §12.8.16.2 | Object creation expressions | Yes | Yes | ✅ |
| §12.8.16.3 | Object initializers (dictionary indexer form) | Partial | No | 🐞 P21 |
| §12.8.16.4 | Collection initializers | Yes | Yes | ✅ |
| §12.8.16.5 | Array creation expressions | Partial | Yes | ⚠️ P8 |
| §12.8.17 | typeof operator | Yes | Yes | ✅ |
| §12.8.18 | sizeof operator | Partial | Yes | ✅ |
| §12.8.19 | checked/unchecked expressions | Yes | Yes | ✅ |
| §12.8.20 | Default value expressions | Yes | Yes | ✅ |
| §12.8.22 | nameof expressions | Yes | Yes | ✅ |
| §12.9.2 | Unary plus | Yes | Yes | ✅ |
| §12.9.3 | Unary minus | Yes | Yes | ✅ |
| §12.9.4 | Logical negation | Yes | Yes | ✅ |
| §12.9.5 | Bitwise complement | Yes | Yes | ✅ |
| §12.9.6 | Prefix increment/decrement | Yes | Yes | ✅ |
| §12.9.7 | Cast expressions | Partial | Yes | ⚠️ P2a |
| §12.9.8 | Await expressions | No | No | ❌ P13 |
| §12.10.2 | Multiplication | Yes | Yes | ✅ |
| §12.10.3 | Division | Yes | Yes | ✅ |
| §12.10.4 | Remainder | Yes | Yes | ✅ |
| §12.10.5 | Addition (numeric, string, enum, delegate) | Partial | Partial | 🐞 R13/R23 |
| §12.10.6 | Subtraction (numeric, enum, delegate) | Partial | Partial | 🐞 R13 |
| §12.11 | Shift operators | Yes | Yes | ✅ |
| §12.12.2-4 | Comparison operators (int, float, decimal) | Yes | Yes | ✅ |
| §12.12.5 | Boolean equality | Yes | Yes | ✅ |
| §12.12.6 | Enumeration comparison | Yes | Yes | ✅ |
| §12.12.7 | Reference equality | Yes | Yes | ✅ |
| §12.12.8 | String equality | Yes | Yes | ✅ |
| §12.12.9 | Delegate equality | No | No | ❌ |
| §12.12.10 | Nullable equality with null literal | Yes | Yes | ✅ |
| §12.12.11 | Tuple equality | Yes | Yes | ✅ |
| §12.12.12 | is operator / patterns | Yes | Yes | ✅ |
| §12.12.13 | as operator | Yes | Yes | ✅ |
| §12.13.2-3 | Integer/enum logical operators (&, |, ^) | Yes | Yes | ✅ |
| §12.13.4 | Boolean logical operators | Yes | Yes | ✅ |
| §12.13.5 | Nullable Boolean & and | operators | Yes | Yes | ✅ |
| §12.14.2 | Boolean conditional logical operators (&&, ||) | Partial | Partial | 🐞 B4 |
| §12.15 | Null-coalescing operator (??) | Yes | Yes | ✅ |
| §12.16 | Throw expression | Partial | Yes | ⚠️ R16 |
| §12.17 | Declaration expressions (out var) | Yes | Yes | ✅ |
| §12.18 | Conditional operator (?:) | Yes | Yes | ✅ |
| §12.19 | Anonymous functions (lambdas) | Partial | Partial | 🐞 R24/P20 |
| §12.20 | Query expressions | Partial | Partial | ⚠️ T20 |
| §12.21.2 | Simple assignment | Yes | Yes | ✅ |
| §12.21.4 | Compound assignment | Yes | Yes | ✅ |
| §13.3 | Blocks | Yes | Yes | ✅ |
| §13.5 | Labeled statements | Yes | Yes | ✅ |
| §13.6.2 | Local variable declarations | Partial | Partial | ⚠️ P4/P22 |
| §13.6.3 | Local constant declarations | Yes | Yes | ✅ |
| §13.6.4 | Local function declarations | Partial | Yes | ⚠️ P10/P18 |
| §13.7 | Expression statements | Yes | Yes | ✅ |
| §13.8.2 | if statement | Yes | Yes | ✅ |
| §13.8.3 | switch statement | Partial | Yes | ⚠️ R17 |
| §13.9.2 | while statement | Yes | Yes | ✅ |
| §13.9.3 | do statement | Yes | Yes | ✅ |
| §13.9.4 | for statement | Yes | Yes | ✅ |
| §13.9.5 | foreach statement | Partial | Yes | ⚠️ B2/B3/R4/R20 |
| §13.10.2 | break statement | Partial | Yes | ⚠️ R19 |
| §13.10.3 | continue statement | Partial | Yes | ⚠️ R19 |
| §13.10.4 | goto statement | Partial | Yes | ⚠️ R18 |
| §13.10.5 | return statement | Yes | Yes | ✅ |
| §13.10.6 | throw statement | Yes | Yes | ✅ |
| §13.11 | try statement | Partial | Yes | ⚠️ B1 |
| §13.12 | checked/unchecked statements | No | No | ❌ P15 |
| §13.13 | lock statement | Yes | Yes | ✅ |
| §13.14 | using statement | Partial | Partial | ⚠️ P16/P17 |
| §13.15 | yield statement | No | No | ❌ P9 |

**Legend:** ✅ Fully implemented and tested | ⚠️ Implemented but gaps exist | ❌ Not implemented | 🐞 Implemented incorrectly

---

## Parser Gaps

### P1. ~~Cast to array type — `(int[])obj`~~ FIXED
**Severity:** High
**Spec:** §12.9.7 Cast expressions
**Fix:** Rewrote `IsCastExpression()` with `SkipArrayRankSpecifiers()` helper and switched `ParseUnary()` to use `TryParseTypeName()` for cast type parsing.
**Parity:** `Cast_ToIntArray.csx`, `Cast_ToStringArray.csx`

### P2. ~~Cast to generic type — `(List<int>)obj`~~ FIXED
**Severity:** High
**Spec:** §12.9.7 Cast expressions
**Fix:** Added `SkipBalancedAngleBrackets()` to `IsCastExpression()` lookahead.
**Parity:** `Cast_ToGenericList.csx`

### P2a. ~~Cast to nullable array type — `(int?[])obj`~~ FIXED
**Severity:** Medium
**Spec:** §12.9.7 Cast expressions
**Fix:** Added `QuestionLeftBracket` handling in both `IsCastExpression` (lookahead) and `TryParseTypeName` (type parsing). The lexer tokenizes `?[` as a single `QuestionLeftBracket` token; both methods now recognize this as nullable + array start.
**Parity:** `Cast_ToNullableIntArray.csx`

### P3. ~~Multidimensional array initializer — `new int[,] { {1,2}, {3,4} }`~~ FIXED
**Severity:** High
**Spec:** §12.8.16.5 Array creation expressions
**Fix:** Added unsized and sized multidim initializer paths in `PrimaryParser.cs`, new `MultiDimArrayInitExpr` AST node, binder/evaluator/emitter support.
**Parity:** `MultiDimArray_InitUnsized2D.csx`, `MultiDimArray_InitSized2D.csx`, `MultiDimArray_InitUnsized_Sum.csx`

### P4. ~~Multiple variable declarations — `int x = 1, y = 2;`~~ FIXED
**Severity:** Medium
**Spec:** §13.6.2 Local variable declarations
**Fix:** Added `_pendingDecls` list in `StatementParser.cs`. Updated `ExpressionParser.ParseProgram` to call `ParseStatementInto` (which drains pending decls) instead of `ParseStatement` directly. Works in both block and top-level scope.
**Parity:** `MultipleDecl_SameType.csx`, `MultipleDecl_WithExpressions.csx`

### P5. ~~`goto` / labels~~ FIXED (block scope only)
**Severity:** Medium
**Spec:** §13.10.4 The goto statement
**Fix:** Added `goto label`, `goto case`, `goto default` parsing, AST nodes, binder dispatch, `ControlFlowSignal` kinds, interpreted evaluator support with label scanning and re-entry, compiled emitter support with loop-based re-entry for both blocks and switch statements.
**Remaining:** Works inside `{ }` blocks but not at top-level statement scope.
**Parity:** `Goto_SkipStatement.csx`, `Goto_ForwardLabel.csx` (currently failing — top-level gap)

### P6. `with` expression (records)
**Severity:** Medium
**Spec:** §12.18 The with expression
**File:** Not implemented
**Issue:** `with` is lexed as `TokenType.With` but no parser method handles `expr with { ... }` syntax. No `WithExpr` AST node exists.

### P7. ~~Range/Index operators — `arr[^1]`, `arr[1..3]`~~ FIXED
**Severity:** Medium
**Spec:** §12.8.7 Element access, System.Index/System.Range
**Fix:** Added `IndexFromEndExpr` AST node for `^expr`. Unified `..` to produce `System.Range` in both modes. Added `System.Index`/`System.Range` handling in `MemberAccess.GetIndex` for array slicing and from-end indexing. Extended mode iteration handles `Range` via `RangeHelpers.EnsureEnumerable`.

### P8. ~~Jagged array with size AND initializer — `new int[3][] { ... }`~~ FIXED
**Severity:** Low
**Spec:** §12.8.16.5 Array creation expressions
**Fix:** Added `{` initializer check after jagged array bracket parsing in `PrimaryParser`. When present, delegates to `ParseArrayLiteralBody` and produces `TypedArrayLiteralExpr`.

### P9. `yield return` / `yield break`
**Severity:** Low (scripting context)
**Spec:** §13.15 The yield statement
**File:** Not implemented
**Issue:** `yield` is lexed but never consumed. No AST nodes exist.

### P10. `static` local functions
**Severity:** Low
**Spec:** §13.6.4 Local function declarations
**File:** `StatementParser.cs` lines 189-215
**Issue:** Local functions are supported but the `static` modifier is not recognized or consumed.

### P11. Null-forgiving operator `!` (postfix)
**Severity:** Low (compile-time only)
**Spec:** §12.8.9 Postfix ! operator
**File:** `ExpressionParser.cs`
**Issue:** `!` is only handled as prefix unary. Postfix `!` is not parsed. In C# this is compile-time only, so impact is cosmetic.

### P12. ~~`goto case` / `goto default` in switch~~ FIXED
**Severity:** Low
**Spec:** §13.10.4 The goto statement
**Fix:** Handled as part of P5 fix.
**Parity:** `Switch_GotoCase.csx`, `Switch_GotoDefault.csx`

---

## Binding Gaps

### B1. ~~Catch variable typed as `Exception` regardless of declared type~~ FIXED
**Severity:** High
**Spec:** §13.11 The try statement
**Fix:** Updated binder to resolve the declared exception type name via `TypeResolver.TryResolveType` instead of hardcoding `typeof(Exception)`.

### B2. ~~Foreach variable type not propagated to BoundForEachExpr~~ FIXED
**Severity:** Medium
**Fix:** Added `ElementType` field to `BoundForEachExpr`. Binder now passes the inferred element type from `InferElementType` into the bound node.

### B3. ~~Compiled emitter hardcodes foreach variable as `typeof(object)`~~ FIXED
**Severity:** Medium
**Fix:** Updated `EmitForeachIteration` to accept and use `forEachExpr.ElementType` instead of hardcoded `typeof(object)`. Also fixed interpreted evaluator (R4) to use `forEachExpr.ElementType` instead of `item?.GetType()`.

---

## Runtime / Type System Gaps

### R1. ~~Enum arithmetic not supported~~ FIXED
**Severity:** High
**Spec:** §12.10.5-§12.10.6 Enum addition/subtraction
**Fix:** Added `EnumArithmetic.cs` with `Add`, `Subtract`, `BitwiseOp`, `BitwiseNot` methods. Integrated into `Operators.cs` with enum type checks before `IsArithmetic` dispatch.
**Parity:** `Enum_PlusInt.csx`, `Enum_MinusEnum.csx`, `Enum_MinusInt.csx`, `Enum_IntPlusEnum.csx`, `Enum_BitwiseOr.csx`, `Enum_BitwiseAnd.csx`

### R2. ~~Array element assignment lacks type coercion~~ FIXED
**Severity:** High
**Spec:** §12.18.3 Simple assignment
**Fix:** Added element type coercion in `MemberAccess.SetIndex` — when storing to an array and the value type doesn't match the element type but is implicitly convertible, converts via `Convert.ChangeType` before storing.

### R3. ~~Unary negation of `uint` not promoted to `long`~~ FIXED
**Severity:** High
**Spec:** §12.9.3 Unary minus operator, §12.4.7.2 Unary numeric promotions
**Fix:** Added `typeof(uint)` entries in `NegateOps` and `CheckedNegateOps` dictionaries in `NumericDispatch.cs` with `-(long)(uint)v` promotion.
**Parity:** `UnaryNegate_Uint_PromotesToLong.csx`

### R4. Foreach runtime types variable dynamically instead of using inferred type
**Severity:** Medium
**File:** `BoundEvaluator.cs` line 1002
**Issue:** `DefineNew(name, item, item?.GetType() ?? typeof(object))` makes the variable type change every iteration based on the runtime type of each element. If collection is `List<Animal>` and element is `Dog`, variable type is `Dog`, not `Animal`. Should use the inferred element type from binding.

### R5. Overload resolution ignores user-defined implicit conversions
**Severity:** Medium
**Spec:** §12.6.4.3 Better function member
**File:** `MethodInvoker.cs`, `TypeHelpers.cs`
**Issue:** `CanImplicitlyConvert` only covers numeric implicit conversions and reference assignability. `op_Implicit` operators are not consulted during overload resolution scoring.

### R6. Generic type inference limited to single type parameter
**Severity:** Medium
**Spec:** §12.6.3 Type inference
**File:** `MethodInvoker.cs` — `TryMakeConcreteMethod` line 659
**Issue:** Returns `null` for `genericArgs.Length != 1` (line 663). Methods with 2+ generic type parameters cannot be inferred. Inference only examines the first parameter.

### R7. ~~Multi-parameter indexers unsupported~~ FIXED
**Severity:** Medium
**Spec:** §12.8.11.3 Indexer access
**Fix:** Replaced throw with reflection-based indexer lookup matching parameter count. Both get and set paths now find the indexer property by parameter count and invoke via `GetValue`/`SetValue` with converted indices.

### R8. Overload resolution missing expanded-form params tiebreaker
**Severity:** Low
**Spec:** §12.6.4.3 Better function member, rule 3
**Files:** `CallBinderService.cs` line 430, `MethodInvoker.cs` line 1291
**Issue:** When both methods have params arrays and are applicable only in expanded forms, the one with fewer params elements should win. Neither implementation checks this.

### R9. CallBinderService has non-standard parameter-count tiebreaker
**Severity:** Low
**File:** `CallBinderService.cs` lines 443-444
**Issue:** Prefers methods with fewer parameters (`leftParams.Length < rightParams.Length ? 1 : -1`), which is not a spec-defined tiebreaker rule.

### R10. Array index truncation to int
**Severity:** Low
**Spec:** §12.8.11.2 Array access
**File:** `MemberAccess.cs` lines 165, 170
**Issue:** Uses `Convert.ToInt32` which truncates `long`/`ulong` indices. Spec allows `int`, `uint`, `long`, `ulong` indices.

### R11. Compound assignment doesn't verify operator is predefined before narrowing
**Severity:** Low
**Spec:** §13.3.5 Compound assignment
**File:** `AssignmentRuntime.cs` lines 328-330
**Issue:** Spec rule 2 requires the operator be predefined before allowing the explicit-conversion narrowing path. Implementation doesn't check this.

### R12. Extension method resolution order doesn't follow namespace proximity
**Severity:** Low
**Spec:** §12.8.9.3 Extension method invocations
**File:** `ExtensionMethodResolver.cs`
**Issue:** Iterates extension types in registration order instead of nearest-enclosing-namespace-first.

---

## Test Coverage Gaps

### T1. Enum operations — near-absent
Only 2 trivial tests (cast to int, comparison). Missing: `Enum.Parse`, flags operations, enum switch, default values, int-to-enum cast.
**Parity added:** `Enum_PlusInt.csx`, `Enum_MinusEnum.csx`, `Enum_MinusInt.csx`, `Enum_IntPlusEnum.csx`, `Enum_BitwiseOr.csx`, `Enum_BitwiseAnd.csx`

### T2. Array covariance — completely absent
No tests for `string[] → object[]` assignment or `ArrayTypeMismatchException` on invalid stores.

### T3. ~~Multidimensional array initializer — absent~~ FIXED
**Parity added:** `MultiDimArray_InitUnsized2D.csx`, `MultiDimArray_InitSized2D.csx`, `MultiDimArray_InitUnsized_Sum.csx`

### T4. Compound assignment cross-type — absent
All existing tests are same-type. Missing: `double += int` (should work), `int += double` (should fail).

### T5. ~~Multiple variable declarations — absent outside for-loop~~ PARTIALLY FIXED
**Parity added:** `MultipleDecl_SameType.csx`, `MultipleDecl_WithExpressions.csx` (currently failing — top-level scope gap)

### T6. Foreach with type cast — absent
No test for `(double)i` where `i` is a foreach iteration variable.

### T7. ~~Cast to array/generic types — absent~~ FIXED
**Parity added:** `Cast_ToIntArray.csx`, `Cast_ToStringArray.csx`, `Cast_ToGenericList.csx`, `Cast_ToNullableIntArray.csx` (nullable currently failing)

### T8. Jagged array element mutation — thin
No test for `jagged[0][1] = 99` (reassigning individual elements in a jagged array).

### T9. String concatenation with null — absent
No test for `null + "hello"`, `"hello" + null`, `(object)42 + "str"`.

### T10. Char arithmetic completeness — thin
Only +/- tested. Missing: `char * int`, `char / int`, `c++`, `c--`.

### T11. Catch-specific exception members — thin
Only `.Message` tested. Missing: `.ParamName` on `ArgumentException`, `.InnerException`, custom exception properties.

### T12. ~~Unary negation of uint — absent~~ FIXED
**Parity added:** `UnaryNegate_Uint_PromotesToLong.csx`

---

## Lexical Gaps

### L1. ~~Verbatim identifiers — `@class`, `@if`, `@return`~~ FIXED
**Severity:** High
**Spec:** §6.4.3 Identifiers
**Fix:** Added verbatim identifier handling in lexer's `@` case: when `@` is followed by a letter or `_`, delegates to `ScanIdentifier()`. The `@` prefix becomes part of the lexeme, allowing keywords to be used as identifiers.

### L2. Unicode escape sequences in identifiers — `\u0068ello`
**Severity:** Low
**Spec:** §6.4.2 Unicode character escape sequences, §6.4.3 Identifiers
**File:** `Lexer.cs` — `ScanIdentifier()` lines 1077-1085
**Issue:** Per spec, `\uXXXX` and `\UXXXXXXXX` can appear in identifiers. `ScanIdentifier()` only handles `char.IsLetterOrDigit()` and `'_'`. Backslash is not intercepted during identifier scanning.
**Example:**
```csharp
var \u0068ello = 5; // Should create identifier "hello", Current: LexError
```

### L3. Preprocessing directives — `#if`, `#define`, `#region`
**Severity:** Low (scripting context)
**Spec:** §6.5 Pre-processing directives
**File:** `Lexer.cs` line 417
**Issue:** No handling of `#` character. All preprocessing directives (`#if`, `#else`, `#endif`, `#define`, `#region`, `#pragma`, `#line`, `#error`, `#warning`) trigger "Unexpected character" error.
**Example:**
```csharp
#region test
var x = 5;
#endregion
// Current: LexError on '#'
```

### L4. Namespace alias qualifier — `global::System.Console`
**Severity:** Medium
**Spec:** §6.4.6 Operators and punctuators, §14.8 Qualified alias member
**File:** `Lexer.cs` lines 203-204
**Issue:** `::` is not lexed as a single token. When encountering `:`, the lexer emits a single `Colon` token without checking for a second `:`. This prevents `global::System.Console` and other qualified alias member expressions from working.
**Example:**
```csharp
var t = global::System.Int32.MaxValue; // Current: parser error (two Colon tokens)
```

---

## Parser Gaps (continued)

### P13. `await` expression
**Severity:** Medium (scripting context)
**Spec:** §12.9.8 Await expressions
**File:** Not implemented
**Issue:** `await` is lexed as a keyword but never consumed by the parser. No `AwaitExpr` AST node exists. Async/await is completely unsupported.
**Example:**
```csharp
var result = await Task.FromResult(42); // Current: parse error
```

### P14. `stackalloc` expression
**Severity:** Low (unsafe/performance context)
**Spec:** §12.8.21 Stack allocation
**File:** Not implemented
**Issue:** `stackalloc` is not recognized. No AST node exists.

### P15. `checked` / `unchecked` statements (block form)
**Severity:** Low
**Spec:** §13.12 The checked and unchecked statements
**File:** `StatementParser.cs`
**Issue:** `checked { ... }` and `unchecked { ... }` as statement blocks are not parsed. Only the expression forms `checked(expr)` and `unchecked(expr)` are supported via `CheckedExpr`. The statement parser has no case for `TokenType.Checked` or `TokenType.Unchecked`.
**Example:**
```csharp
checked {
    int x = int.MaxValue;
    x++; // Should throw OverflowException
}
// Current: parse error — 'checked' not recognized as statement
```

### P16. `using` declaration (C# 8.0 pattern)
**Severity:** Medium
**Spec:** §13.14 The using statement (extended by C# 8.0 using declarations)
**File:** `StatementParser.cs` lines 711-752
**Issue:** Only the classic `using (resource) { body }` form with parentheses is supported. The C# 8.0 declaration form `using var x = expr;` (no parentheses, no statement body, scope extends to end of enclosing block) is not recognized. Parser unconditionally calls `Consume(TokenType.LeftParen, ...)` after `using`.
**Example:**
```csharp
{
    using var stream = new System.IO.MemoryStream();
    stream.WriteByte(42);
    // stream disposed at end of block
}
// Current: parse error — Expected '(' after 'using'
```

### P17. Multiple resource declarations in `using`
**Severity:** Low
**Spec:** §13.14 The using statement
**File:** `StatementParser.cs` lines 711-752
**Issue:** Only a single resource variable is parsed in `using (Type x = expr)`. The spec allows `using (R r1 = e1, r2 = e2, rN = eN) statement` which expands to nested using statements.
**Example:**
```csharp
using (var a = new System.IO.MemoryStream(), b = new System.IO.MemoryStream())
{
    // Current: parse error after first resource
}
```

### P18. Generic local functions
**Severity:** Low
**Spec:** §13.6.4 Local function declarations
**File:** `StatementParser.cs` lines 245-271
**Issue:** Local function parsing does not support generic type parameters. `void F<T>(T x) { ... }` is not recognized — the `<T>` after the function name is not parsed.
**Example:**
```csharp
{
    T Identity<T>(T x) { return x; }
    return Identity(42); // Current: parse error on '<'
}
```

### P19. ~~Tuple patterns in switch expressions — `(0, 0) =>`~~ FIXED
**Severity:** Medium
**Spec:** §11.2 Pattern forms (positional pattern)
**Fix:** Added `PositionalPattern` AST node. Updated `PatternParser.ParsePrimaryPattern` to detect comma after first subpattern and parse as positional pattern. Added `PositionalPattern` case in `PatternRuntime.MatchPatternCore` matching against `ITuple` elements.

### P20. ~~`Action` / delegate-type variable declarations fail to parse~~ FIXED
**Severity:** Medium
**Spec:** §13.6.2 Local variable declarations
**Fix:** Added `Identifier Identifier =` pattern detection in `StatementParser.ParseStatement()` for non-generic, non-keyword type name declarations. Also added non-generic `Action` support in `LambdaDelegateConverter` and `LambdaDelegateFactory` (0-arity action wasn't handled because `IsSupportedDelegateType` rejected non-generic types).

### P21. ~~Dictionary indexer initializers blocked in Standard mode~~ FIXED
**Severity:** Medium
**Spec:** §12.8.16.3 Object initializers
**Fix:** Added `[expr] = value` indexer initializer path in `ParseObjectInitializer()`, new `IndexerKey` field on `InitializerEntry`/`BoundInitializerEntry`, evaluator dispatches to `MemberAccess.SetIndex`, compiled emitter dispatches to `ConstructionRuntime.ApplyIndexerInitializer`.

### P22. ~~Typed variable declaration with FQN type fails with literal assignment~~ FIXED
**Severity:** Medium
**Spec:** §13.6.2 Local variable declarations
**Fix:** Added `TryParseFqnTypeDeclaration()` in `StatementParser` for dotted type names. Also fixed `ValidateAndCoerceType` to handle int→enum conversion via `Enum.ToObject` (§10.2.4) and excluded enums from the int→integer constant conversion path.

---

## Binding Gaps (continued)

### B4. ~~Conditional logical operators (`&&`, `||`) reject nullable bool~~ FIXED
**Severity:** High
**Spec:** §12.14.2 Boolean conditional logical operators, §12.13.5 Nullable Boolean & and | operators
**Fix:** Added `EvaluateNullableBoolLogical` in interpreted evaluator with three-value logic. Added `NullableBoolAnd`/`NullableBoolOr` runtime helpers for compiled path. Both check static types to detect `bool?` operands before entering the standard bool path.

---

## Runtime / Type System Gaps (continued)

### R13. ~~Delegate combination and removal operators~~ FIXED
**Severity:** Medium
**Spec:** §12.10.5 Addition operator (delegate combination), §12.10.6 Subtraction operator (delegate removal)
**Fix:** Added `Delegate.Combine` check in `Operators.Add` and `Delegate.Remove` check in `Operators.Subtract`, before enum arithmetic dispatch.

### R14. User-defined conversions not consulted during casts
**Severity:** Medium
**Spec:** §10.5 User-defined conversions
**File:** `TypeHelpers.cs`, `MethodInvoker.cs`
**Issue:** Extends R5. Beyond overload resolution, explicit casts like `(TargetType)expr` do not look up `op_Explicit` or `op_Implicit` on the source or target type via reflection. Only built-in numeric conversions and reference casts are attempted.
**Example:**
```csharp
// Given: class Meters { public static explicit operator double(Meters m) => m.Value; }
var m = new Meters(5.0);
double d = (double)m; // Current: InvalidCastException — op_Explicit not consulted
```

### R15. Checked context does not validate NaN/infinity in float→integral casts
**Severity:** Low
**Spec:** §10.3.2 Explicit numeric conversions
**File:** `NumericDispatch.cs`
**Issue:** Per spec, in a `checked` context, converting `float.NaN` or `double.PositiveInfinity` to an integral type should throw `OverflowException`. The runtime delegates to `Convert.ToXXX()` which may handle some cases, but explicit validation is absent.
**Example:**
```csharp
checked((int)double.NaN) // Expected: OverflowException
```

### R16. Throw expression allowed in unrestricted positions
**Severity:** Low
**Spec:** §12.16 The throw expression operator
**File:** `ExpressionParser.cs` lines 184-188
**Issue:** Spec restricts throw expressions to: (1) second/third operand of `?:`, (2) second operand of `??`, (3) body of expression-bodied lambda/member. The parser allows `throw expr` in any expression position, e.g., `var x = throw new Exception();` would parse rather than error.
**Example:**
```csharp
var x = throw new System.Exception(); // Should be compile error, may parse
```

### R17. Switch statement allows fall-through (no validation)
**Severity:** Low
**Spec:** §13.8.3 The switch statement
**File:** `BoundEvaluator.cs`
**Issue:** Spec requires "the end point of the statement list of a switch section to be unreachable" — i.e., every case must end with `break`, `return`, `goto`, `throw`, or similar. CsEval does not enforce this and silently allows fall-through.
**Example:**
```csharp
{
    var x = 1;
    switch (x)
    {
        case 1: var a = 10; // No break — should error, may silently fall through
        case 2: return 20;
    }
}
```

### R18. `goto` scope validation missing
**Severity:** Low
**Spec:** §13.10.4 The goto statement
**File:** `Binder.cs`, `BoundEvaluator.cs`
**Issue:** Spec forbids goto from jumping *into* a block (only out). Also restricts goto from leaving a finally block. The binder and evaluator perform no scope validation — any goto label is accepted regardless of target scope.
**Example:**
```csharp
{
    goto inner;
    if (false) { inner: var x = 1; } // goto into a block — should error
}
```

### R19. `break`/`continue` can exit `finally` block
**Severity:** Low
**Spec:** §13.10.2 The break statement, §13.10.3 The continue statement
**File:** `BoundEvaluator.cs`
**Issue:** Spec states "A break statement cannot exit a finally block" and same for continue. No validation exists; a break/continue inside a finally block targeting an outer loop will execute without error.
**Example:**
```csharp
{
    for (var i = 0; i < 3; i++)
    {
        try { throw new System.Exception(); }
        finally { break; } // Should be compile error
    }
}
```

### R21. ~~`string.Join(string, IEnumerable<string>)` overload resolution fails with lazy enumerables~~ FIXED
**Severity:** Medium
**Spec:** §12.6.4 Overload resolution
**Fix:** Root cause was generic type inference for lambda return types. When `TryInvokeLambdaForTypeInference` failed (e.g., `"".Substring(0,1)` throws on empty test string), the return type fell back to `typeof(object)`, making `.Select()` produce `IEnumerable<object>` instead of `IEnumerable<string>`. Added `TryInferLambdaReturnTypeStatically` which walks the lambda's AST to infer return types from method signatures and expression structure without executing the lambda.

### R22. ~~`ulong + long` does not produce binding error~~ FIXED
**Severity:** Medium
**Spec:** §12.4.7.3 Binary numeric promotions
**Fix:** Added signed-integer check to `NumericDispatch.GetResultType` Rule 4 (ulong path), matching the existing check in `PromoteOperands`. Both interpreted and compiled paths now correctly throw for ulong + signed-type operations.

### R23. ~~`null + null` (string context) returns null instead of empty string~~ FIXED
**Severity:** Low
**Spec:** §12.10.5 Addition operator (string concatenation)
**Fix:** Added `isStringContext` parameter to `Operators.Add`. Evaluator passes `true` when either operand has `typeof(string)` static type. Compiled emitter passes the flag through `EmitBinaryCore`. When both operands are null in string context, returns `""` per spec.

### R24. ~~Lambda returning lambda fails with InvalidCastException~~ FIXED
**Severity:** Medium
**Spec:** §12.19 Anonymous function expressions
**Fix:** Updated `CastResult<TResult>` in `LambdaDelegateFactory` to detect when a return value is `LambdaValue`/`CompiledLambdaValue` and convert it to the expected delegate type via `LambdaDelegateConverter.TryConvert`.

### R25. ~~Named tuple element access returns MethodRef instead of value~~ FIXED
**Severity:** Medium
**Spec:** §12.8.6 Tuple expressions, §8.3.11 Tuple types
**Fix:** Added `NamedTupleValue` wrapper carrying name→index mapping. Named tuples now create `NamedTupleValue` at runtime. `MemberAccess.GetMember` checks for named elements before falling through to `ValueTuple` field access. Compiled emitter skips typed-variable and direct-field-access fast paths for `ValueTuple` types to avoid InvalidCastException with `NamedTupleValue` wrapper.

### R20. ~~Foreach does not dispose IDisposable enumerators~~ FIXED
**Severity:** Medium
**Spec:** §13.9.5 The foreach statement
**Fix:** Interpreted path already used C#'s native `foreach` which handles disposal. Fixed compiled emitter to wrap the loop in `TryFinally` with `IDisposable` check and `Dispose()` call in the finally block, matching the spec's expansion.

---

## Test Coverage Gaps (continued)

### T13. Verbatim identifiers — absent
No tests for `@class`, `@if`, `@return` as identifiers.

### T14. Nullable bool with conditional operators — absent
No tests for `bool? && bool?` or `bool? || bool?`.

### T15. Delegate combination/removal — absent
No tests for `delegate + delegate` or `delegate - delegate`.

### T16. Using declarations (C# 8) — absent
No tests for `using var x = expr;` pattern.

### T17. Checked/unchecked statements — absent
No tests for `checked { ... }` or `unchecked { ... }` block forms. Only expression forms tested.

### T18. Namespace alias qualifier — absent
No tests for `global::System.Int32` or similar `::` qualified names.

### T19. `with` expression — absent
No tests for `record with { Property = value }` syntax.

### T20. Query expression completeness — thin
Only basic `from...where...select` tested. Missing: `group by`, `join`, `let`, `orderby`, multiple `from` (SelectMany), `into`.

---

## Cross-Feature Interaction Matrix

| Feature A | Feature B | Tested | Status |
|---|---|---|---|
| nullable | arithmetic (+,-,*,/) | Yes | ✅ |
| nullable | comparisons (<,>,==,!=) | Yes | ✅ |
| nullable | equality with null literal | Yes | ✅ |
| nullable | unary operators (-,~) | Yes | ✅ |
| nullable bool | & and | operators | Yes | ✅ |
| nullable bool | && and || operators | No | 🐞 B4 |
| nullable | pattern matching (is int v) | Yes | ✅ |
| nullable | null-coalescing (??) | Yes | ✅ |
| nullable | ternary (?:) | Yes | ✅ |
| lambda | closure (capture variable) | Yes | ✅ |
| lambda | returning lambda | No | 🐞 R24 |
| lambda | LINQ integration | Yes | ✅ |
| lambda | immediate invocation | No | 🐞 R24 |
| foreach | switch inside | Yes | ✅ |
| foreach | try/catch inside | Yes | ✅ |
| foreach | lambda capture (per-iteration) | Yes | ✅ |
| foreach | break/continue | Yes | ✅ |
| switch | nested switch | Yes | ✅ |
| switch | string labels | Yes | ✅ |
| switch | null case | Yes | ✅ |
| switch expression | type patterns | Yes | ✅ |
| switch expression | when clauses | Yes | ✅ |
| switch expression | tuple patterns | No | ❌ P19 |
| try/catch | nested exceptions | Yes | ✅ |
| try/catch | finally ordering | Yes | ✅ |
| try/catch | specific before general | Yes | ✅ |
| using statement | exception handling | Yes | ✅ |
| null-conditional | null-coalescing | Yes | ✅ |
| null-conditional | method invocation | Yes | ✅ |
| string interpolation | ternary inside | Yes | ✅ |
| string interpolation | method call inside | Yes | ✅ |
| string interpolation | format specifiers | Yes | ✅ |
| enum | arithmetic (+,-) | Yes | ✅ |
| enum | bitwise (&,|,^,~) | Yes | ✅ |
| enum | comparison | Yes | ✅ |
| boxing | nullable (null → null ref) | Yes | ✅ |
| cast | checked overflow | Yes | ✅ |
| cast | NaN/infinity to integral | No | ⚠️ R15 |
| conversions | generics (type inference) | Partial | ⚠️ R6 |
| delegates | combination (+) | No | ❌ R13 |
| delegates | removal (-) | No | ❌ R13 |
| tuple | named element access | No | 🐞 R25 |
| tuple | equality | Yes | ✅ |
| query | orderby | Yes | ✅ |
| query | group by | Yes | ✅ |
| query | let | Yes | ✅ |
| query | multiple from | Yes | ✅ |

---

## Test Coverage Gaps (continued)

### T21. Tuple pattern matching — absent
No tests for tuple patterns in switch expressions like `(0, 0) =>`.

### T22. Named tuple element access — absent
No tests for `t.Name` where `t = (Name: "test", Value: 42)`. Existing tests use `Item1`/`Item2` positional access only.

### T23. Nested lambda / higher-order functions — absent
No tests for lambdas returning lambdas (e.g., `Func<int, Func<int, int>>`). No immediate invocation tests like `f(3)(4)`.

### T24. Binary numeric promotion edge cases — thin
Missing: `ulong + long` (should error), `uint + int` → `long`, `uint + sbyte` → `long` type verification.

### T25. `Action` delegate type variable declaration — absent
No tests for `Action varName = () => ...;` or `Action<int> f = x => ...;` syntax.

### T26. Dictionary indexer initializer in Standard mode — absent
No tests for `new Dictionary<string, int> { ["key"] = value }` in Standard mode.

### T27. String concatenation `null + null` — absent
No test verifying `null + null` in string context produces `""` per §12.10.5.

### T28. `string.Join` with lazy IEnumerable — absent
No test for `string.Join(sep, enumerable.Select(...))` without `.ToArray()`.
