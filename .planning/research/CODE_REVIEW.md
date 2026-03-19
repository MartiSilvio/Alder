# CsEval Code Review

## Bugs & Correctness Issues

### 1. `AstWalker.VisitSwitch` doesn't walk case patterns
**File:** `Parsing/AstWalker.cs:508-519`

Switch statement visitor visits when-guards and bodies but never calls `WalkPattern` on `caseExpr.CasePattern`. The `VisitSwitchExpression` variant does walk patterns. Any analysis walker (variable collection, depth validation) misses expressions in switch case patterns.

### 2. `AstWalker.VisitMemberAccess` skips intermediate nodes
**File:** `Parsing/AstWalker.cs:58-66`

Iteratively walks `MemberAccessExpr.Object` chains but only calls `Visit` on the innermost node. Intermediate `MemberAccessExpr` nodes never get `OnEnter`/`OnLeave`. Any subclass overriding `VisitMemberAccess` silently misses intermediate nodes.

### 3. `BoundExprWalker.VisitMemberAccess` drops intermediate bound nodes
**File:** `Binding/BoundExprVisitor.cs:203-210`

Same problem in the bound tree walker. For `a.b.c.d`, only `a` is visited. `DiagnosticCollector` extends this walker -- if any intermediate member access has `HasErrors = true`, the diagnostic is silently lost.

### 4. `TryPreparePlannedArgumentsInPlace` mutates input array on partial failure
**File:** `Runtime/MethodInvoker.cs:458-492`

Modifies `sourceArgs` in-place. If conversion succeeds for early args but fails later, the caller's array is partially corrupted. The corrupted array is then passed to `TryBuildPlannedArguments` which works with bad data.

### 5. `EnumArithmetic` hardcoded to Int64/Int32
**File:** `Runtime/EnumArithmetic.cs:22-23,41`

`Add` always uses `Convert.ToInt64`, `Subtract` uses `Convert.ToInt32`. Enums with `ulong` underlying type and values > `long.MaxValue` will throw `OverflowException`. `BitwiseOp` already handles this correctly via the underlying type.

### 6. `EnumArithmetic.BitwiseOp` hardcodes `"|"` in error message
**File:** `Runtime/EnumArithmetic.cs:63`

Called for `&`, `|`, and `^`, but error message always says `"|"`.

### 7. `MemberAccess.GetIndex` discards `GuardReflectionLeak` return value
**File:** `Runtime/MemberAccess.cs:263-264`

Calls `GuardReflectionLeak(val, ...)` for side effects but returns `val` directly, not the guard's return value.

### 8. `ConstructionRuntime.InvokeConstructor` re-throws without preserving stack trace
**File:** `Semantics/ConstructionRuntime.cs:43-45`

`throw ex.InnerException;` resets the stack trace. Every other re-throw site uses `ExceptionDispatchInfo.Capture(ex).Throw()`.

### 9. Fast compiled delegate silently ignores CancellationToken
**File:** `CsEval.Compiled/ILExpressionCompiler.cs:111-119`

Bakes `default(CancellationToken)` as a constant. Cancellation/timeout enforcement is silently bypassed on the fast path.

### 10. AOT generator silently drops constructor/method overloads with same parameter count
**File:** `Generators/TypeMetadataEmitter.cs:289,336,389`

Takes `group.First()` for each param count, silently making other overloads uncallable. `Dictionary<string,object>` has `(int)` and `(IEqualityComparer<string>)` constructors -- one is silently dropped.

### 11. `QueryParser.RewriteIdentifiers` has incomplete AST coverage
**File:** `Parsing/QueryParser.cs:608-705`

Manual pattern-matching against ~20 node types with `_ => expr` catch-all. Missing `TupleExpr`, so `from x in items select (x, x*2)` won't rewrite `x` in tuple elements. Every new node type is a potential silent bug.

---

## Design & Architecture Issues

### 12. `BindingContext.TryGetVariableType` infers type from runtime value
**File:** `Binding/BindingContext.cs:68-87`

Third fallback calls `TryGet` on the runtime context and uses `GetType()` on the current value. If `x = 42` then `x = "hello"`, the static type changes between bind passes. Violates single-source-of-truth principle.

### 13. `BindIdentifier` uses `typeof(object)` as a sentinel
**File:** `Binding/Binder.Expressions.cs:45-55`

Checks `if (staticType != typeof(object))` to decide if a variable was found. But `object` is a legitimate declared type -- a variable explicitly typed as `object` falls through to the type-resolver path unnecessarily.

### 14. `CsEvalContext` triple-dictionary pattern
**File:** `Runtime/CsEvalContext.cs:17-22`

Values, types, and read-only flags stored in 3 parallel dictionaries (6 with local variants). A single `Dictionary<string, VariableSlot>` would eliminate consistency risks and reduce lookup cost.

### 15. `CsEvalException(string)` constructor exists and is used
**File:** `CsEvalException.cs:16-19`

CLAUDE.md forbids raw message strings. Used in `BoundEvaluator.cs:75` and `CsEvalExpression.cs:91`. Produces exceptions with empty diagnostics and null `ErrorCode`.

### 16. `CsEvalDepthException` and `CsEvalExecutionLimitException` bypass diagnostics
**File:** `CsEvalException.cs:55-99`

Both call `base(string)`, producing empty `Diagnostics` and null `ErrorCode`.

### 17. `BindingNotSupportedException` carries no diagnostic code
**File:** `Binding/BindingNotSupportedException.cs`

Used at 5+ call sites as an error signaling mechanism. When it escapes to the engine, the resulting diagnostic lacks a code.

### 18. `SandboxOptions.AllowedTypes` is mutable `HashSet<Type>` on immutable record
**File:** `CsEvalOptions.cs:135`

Anyone with a reference can modify the set after construction, bypassing the frozen-after-first-evaluation guarantee.

### 19. `Dispose()` TOCTOU race despite recent fix
**File:** `CsEvalEngine.cs:57-64`

`if (_disposed) return; _disposed = true;` is check-then-act. Two concurrent threads can both proceed. Should use `Interlocked.CompareExchange`.

### 20. `_context` field mutation pattern in `BoundEvaluator`
**File:** `Interpretation/BoundEvaluator.cs`

Save-restore pattern (`var prev = _context; _context = ...; try/finally`) repeated in 10+ methods. Any missed restore leaks context into wrong scope. Consider passing context as a parameter.

---

## Performance Issues

### 21. Exception-driven control flow for expected binding failures
**File:** `Binding/Binder.Invocations.cs:85-248`

`BindSingleMemberAccess`, `BindIndexAccess`, `BindCall`, `BindCallCallee`, `TryBindStaticModuleCall` all catch `CsEvalException` for expected fallbacks. Stack trace capture on every failed static binding attempt.

### 22. `TypeMetadataProvider.CompileGetter` doesn't actually compile
**File:** `Runtime/TypeMetadataProvider.cs:112-122`

Named as if it produces a compiled delegate, but wraps `PropertyInfo.GetMethod.Invoke()` in a lambda. No performance benefit over `GetValue()`. Should use `Expression.Lambda<Func<object,object?>>` compiled to a real delegate.

### 23. `MemberBinderService`/`CallBinderService` re-instantiated on every bind
**File:** `Binding/Binder.Invocations.cs:83-195`

Stateless service objects created fresh on every member access and call binding. Should be cached on `Binder` or `BindingContext`.

### 24. `Regex.IsMatch` with no caching
**File:** `Runtime/Extensions/RegexMatchOperator.cs:31`

Compiles regex from scratch on every invocation. Should cache compiled `Regex` objects.

### 25. `EvaluateForEach` creates child context per iteration
**File:** `Interpretation/BoundEvaluator.cs:1146-1170`

Unlike `while`/`do-while` which create once and call `ClearScope()`, foreach allocates a new child context each iteration.

### 26. Static caches grow unboundedly
**Files:** `Runtime/MethodDispatchCache.cs:13-14`, `ExtensionMethodResolver.cs:32-36`, `TypeHelpers.cs:228-230`

`ParameterCache`, `FastInvokerCache` have no eviction. Pin assemblies in memory via `MethodInfo` keys.

### 27. `CsEvalOptions.Default` creates new instance on every access
**File:** `CsEvalOptions.cs:24`

`=> new()` allocates two objects per access. Should be `static readonly`.

### 28. `FindIndexer` hardcodes `"Item"` name
**File:** `Runtime/ReflectionRuntime.cs:104-115`

C# allows custom indexer names via `IndexerNameAttribute`. Types like `string` use `"Chars"`. Should use `DefaultMemberAttribute`.

---

## Code Duplication

### 29. `BoundRuntimeMethodCache` / `CompilerReflectionCache` massive duplication
**File:** `CsEval.Compiled/Compilation/`

50+ identical `MethodInfo` resolutions in two parallel classes. Highest volume duplication in the codebase.

### 30. Duplicated overload scoring in `MethodInvoker` vs `MethodResolver`
**Files:** `Runtime/MethodInvoker.cs:1083-1321` vs `Runtime/MethodResolver.cs:48-130`

Same scoring logic with identical magic numbers (100/10/1, 1000/500) in two separate code paths.

### 31. Duplicated binary operator dispatch
**Files:** `Interpretation/BoundEvaluator.cs:480-510` vs `Semantics/AssignmentRuntime.cs:376-399`

Near-identical `TokenType`-to-`Operators.*` switch in two files.

### 32. Duplicated interpolated string scanning
**File:** `Parsing/Lexer.cs:484-688`

`ScanInterpolatedString` and `ScanVerbatimInterpolatedString` are ~95% identical. Only difference is escape handling.

### 33. Duplicated identifier resolution for pipelines
**File:** `Semantics/IdentifierRuntime.cs:25-138`

`InvokeIdentifierCall` and `InvokePipelineIdentifier` share near-identical resolution chains.

### 34. Duplicated variable assignment validation
**File:** `Semantics/AssignmentRuntime.cs`

`ValidateVariableAssignment`/`ValidateVariableAssignmentLocal` and `ValidateCompoundAssignment`/`ValidateCompoundAssignmentLocal` -- identical logic, different sources for variable type.

### 35. Duplicated deconstruction logic
**Files:** `BoundEvaluator.cs:360-399` vs `ConstructionRuntime.cs:94-127`

Identical ITuple check + iterate + DefineNew logic.

### 36. Duplicated bracket-depth scanning
**Files:** `ExpressionParser.cs:389-576`, `PrimaryParser.cs:214-255`

Same pattern independently reimplemented 3+ times.

### 37. Duplicated `SanitizeIdentifier`
**Files:** `Generators/CsEvalSourceGenerator.cs:269-286` vs `TypeMetadataEmitter.cs:420-434`

### 38. Duplicated `TryCompile` overloads
**File:** `ILExpressionCompiler.cs:29-93`

Two overloads with identical try-catch wrapping and result construction.

### 39. Six `WithLeft` emitter method clones
**File:** `BoundExpressionEmitter.cs:397-575`

Three pairs of near-identical methods differing only in how left operand is obtained.

---

## Code Smells & Tech Debt

### 40. `EnsureSufficientExecutionStack` in parser
**File:** `ExpressionParser.cs:172`

Violates project rule: "No hacks for stack safety."

### 41. `And`/`Or`/`Not` token types exist but aren't produced by lexer
**File:** `Parsing/Token.cs:215,231,234`

Lexer maps keywords to `AmpAmp`/`PipePipe`/`Bang` then disambiguates by lexeme string. Fragile and confusing.

### 42. `IdentifierOccurrenceCollector` scoping is flat
**File:** `Parsing/IdentifierOccurrenceCollector.cs`

Uses flat `HashSet` for declarations. Inner-scope declarations shadow outer scope for the entire tree, affecting `TryLowerImplicitPlaceholderLambda`.

### 43. `ExtensionMethodResolver` executes user lambdas for type inference
**File:** `Runtime/ExtensionMethodResolver.cs:685-701`

Actually invokes lambda bodies with fabricated default arguments to discover return types. Side effects happen during type inference.

### 44. `InvokeBareMathOrCall` is a dead passthrough
**File:** `Semantics/IdentifierRuntime.cs:140-147`

One-liner that delegates to `InvokeIdentifierCall` with the same params.

### 45. `BoundLambdaExpr.Body` is unbound AST
**File:** `BoundNodes/BoundLambdaExpr.cs:8`

Only bound node storing `Expr` instead of `BoundExpr`. `EnumerateChildren` returns nothing, making lambda bodies invisible to tree analysis.

### 46. Increment/decrement representation inconsistency
`BoundIncrementDecrementExpr` uses `TokenType Operator`, peers use `bool IsIncrement`.

### 47. Scoped-context save/restore boilerplate in emitter
**File:** `BoundExpressionEmitter.cs`

Same 5-line pattern repeated 10+ times across all scoped emission methods.

### 48. `IsDepthFailure` relies on string matching
**Files:** `CsEvalEngine.cs:755-759` and `ILExpressionCompiler.cs:123-137`

`message.Contains("nesting depth exceeded")` -- fragile, will break if message text changes.

### 49. `AggregateBuiltins.Sum` is 150 lines of manual type dispatch
**File:** `Extensions/AggregateBuiltins.cs:37-189`

Hand-rolled promotion logic with subtle precision issues in float->double->decimal chains.

### 50. `AggregateBuiltins.Enumerate` abuses wrong diagnostic codes
**File:** `Extensions/AggregateBuiltins.cs:270-283`

Uses `CS0103` (name not found) for null arguments and `CS0021` (bad indexer) for type mismatches.

---

# Round 2: Deep-Dive Findings

## Semantic / Evaluator Bugs

### 51. `for` loop silently swallows `Goto` signals
**File:** `Interpretation/BoundEvaluator.cs:1062-1066`

The `for` loop handles `Break` and `Return` but has no catch-all propagation. If a `goto label` signal is produced inside a `for` body, it's not `Break` or `Return`, so it falls through — increments execute, loop continues, and the `Goto` signal is lost. `while` and `do-while` correctly propagate unhandled signals via `return signal`.

**Reproducer:** `{ for (var i = 0; i < 10; i++) { goto done; } done: return 42; }` — the `goto` is swallowed.

### ~~52. Member `??=` and index `??=` eagerly evaluate RHS (violates C# short-circuit)~~ FIXED

~~**File:** `Interpretation/BoundEvaluator.cs:797-815`~~

~~For simple variable `??=` (line 714-728), the code correctly short-circuits: checks current value BEFORE evaluating RHS. But for member and index `??=`, the RHS is evaluated BEFORE checking the current value. Side effects in the RHS execute even when the target is already non-null.~~

~~**Reproducer:** `obj.X = 5; obj.X ??= SideEffectFunction()` — function is called despite `obj.X` being non-null.~~

### ~~53. `EvaluateBlock` unwraps `Return` signal, breaking nested block propagation~~ FIXED

~~**File:** `Interpretation/BoundEvaluator.cs:631-632`~~

~~When a `return` signal is hit, `EvaluateBlock` returns the raw unwrapped value instead of the signal. In nested blocks like `{ { return 42; } return 99; }`, the inner block's return unwraps to `42`, the outer block sees a raw value (not a signal), continues execution, and hits `return 99`.~~

### 54. `Divide`/`Modulo` ignore `checked` context
**File:** `Runtime/Operators.cs:158-162`

Neither accepts nor propagates the `isChecked` flag. `checked(int.MinValue / -1)` should throw `OverflowException` but returns `int.MinValue` (wrapping).

### 55. Positional patterns only work with `ITuple`, not user-defined `Deconstruct`
**File:** `Semantics/PatternRuntime.cs:64-74`

C# positional patterns work with any type that has a `Deconstruct` method. The evaluator only checks `ITuple`, so `record Point(int X, int Y)` and custom classes with `Deconstruct` fail to match.

### 56. `finally` block discards control flow signals silently
**File:** `Interpretation/BoundEvaluator.cs:883-888`

Results of `Evaluate(statement)` in the `finally` block are discarded. While C# forbids control transfer out of `finally`, the evaluator doesn't diagnose it — it silently ignores `return`/`break`/`continue` in `finally`.

### 57. Unresolved `goto` label leaks `ControlFlowSignal` as evaluation result
**File:** `Interpretation/BoundEvaluator.cs:633-643`

When a `goto` targets a label not in the current block, the signal propagates up. If the label doesn't exist anywhere, a `ControlFlowSignal` object leaks out as the evaluation result with no diagnostic.

---

## Parser Bugs

### 58. `x as int ? y : z` misparses — `?` consumed as nullable instead of ternary
**File:** `Parsing/ParserBase.cs:342-346`, `Parsing/ExpressionParser.cs:903`

`TryParseTypeName` unconditionally consumes `?` after a type name. In `x as int ? y : z`, the `?` is consumed as nullable suffix producing `int?`, then parser errors on `y : z`. In C#, this should be `(x as int) ? y : z`.

### 59. Interpolated string lexer blind to string/char literals inside expression holes
**File:** `Parsing/Lexer.cs:484-540`

`ScanInterpolatedString` tracks brace depth but not string literals inside holes. `$"result: {dict["key"]}"` breaks — the `"` inside `dict["key"]` prematurely terminates the interpolated string scan.

### 60. Ternary `:` inside interpolation hole mistaken for format specifier
**File:** `Parsing/PrimaryParser.cs:911-923`

Format specifier detection checks for `:` at top-level depth but doesn't track ternary `?`. `$"{x > 0 ? x : -x}"` treats `:` as a format specifier, parsing the expression as `x > 0 ? x` with format ` -x`.

### 61. `(Exception?[])obj` cast not recognized
**File:** `Parsing/ExpressionParser.cs:1210-1216`

`IsCastExpression` for identifier types doesn't re-scan for array rank specifiers after consuming nullable `?`. `(Exception?[])obj` fails — no array re-scan after nullable suffix.

### 62. `>>` token splitting corrupts token list on backtrack
**File:** `Parsing/ParserBase.cs:252-287`

`MatchClosingAngleBracket` splits `>>` by inserting a new `>` token. If `TryParseTypeArguments` backtracks after the split, the orphaned `>` remains permanently in the token list, causing downstream parse errors.

### 63. `nint`, `nuint`, `dynamic` missing from `IsTypeKeyword`
**File:** `Parsing/ParserBase.cs:168-174`

These are registered as keywords in the lexer but not recognized as type keywords. `(nint)42` is not recognized as a cast, `nint x = 5` not recognized as a variable declaration.

### 64. Generic member access without call errors — `list.Cast<int>.First()`
**File:** `Parsing/ExpressionParser.cs:1384-1388`

`TryParseTypeArguments` accepts `Dot`/`QuestionDot` as valid continuations, but the caller unconditionally does `Consume(LeftParen)`, producing "Expected '(' after generic type arguments".

### 65. Numeric suffix consumed even when followed by identifier chars
**File:** `Parsing/Lexer.cs:1038-1063`

`ParseNumericSuffix` doesn't verify the suffix isn't followed by identifier characters. `123def` silently becomes `123.0` (double) + identifier `ef` with no error.

---

## Lifecycle & Threading Issues

### 66. `ConditionalWeakTable` Remove+Add race can throw `ArgumentException`
**File:** `CsEvalExpression.cs:96-97`

Two threads calling `GetOrCreateBoundExpression` with the same context can both pass the version check. Thread A: Remove, Add. Thread B: Remove (deletes A's entry), but if timing differs: Thread B calls Add when A's entry still exists → `ArgumentException` for duplicate key.

### 67. `_bindingUnavailable` is permanently sticky — poisons shared expressions
**File:** `CsEvalExpression.cs:117-119`

Once set to `true`, never cleared. An expression that fails binding in one context (e.g., missing variable) is permanently marked unbindable even when used with a different engine that has the required registrations.

### 68. Parent dispose clears shared caches, breaking live child engines
**File:** `CsEvalEngine.cs:57-64, 87-103`

Child constructor receives parent's `_expressionCache` and `TypeMetadataProvider` by reference. Parent's `Dispose()` calls `Clear()` on both, silently degrading all live child engines.

### 69. `TryCompileFromAst` writes `CompiledInfo` without lock
**File:** `CsEvalEngine.cs:287`, `CsEvalExpression.cs:57`

`TryCompileInternal` uses `lock(expression)` to protect writes to `CompiledInfo`, but `TryCompileFromAst` writes it locklessly. Concurrent compilation from both paths can overwrite a successful result with a less-capable one.

### 70. `CsEvalCompiledExpression<T>` holds engine reference, prevents GC after dispose
**File:** `CompiledExpression.cs:14-16`

Captures the entire `CsEvalEngine` by reference. Even after `Dispose()`, the engine's context, config, and registered types remain rooted in memory as long as any compiled expression is alive.

---

## Diagnostic System Violations

### 71. `CsEvalDepthException` bypasses diagnostics entirely
**File:** `CsEvalException.cs:55-64`

Calls `base(string)` despite `DiagnosticCode.CSEV0033` (`ExpressionNestingDepthExceeded`) existing. Produces empty `Diagnostics`, null `ErrorCode`.

### 72. `CsEvalExecutionLimitException` bypasses diagnostics — no codes exist
**File:** `CsEvalException.cs:72-99`

Calls `base(string)`. No `DiagnosticCode` entries exist for statement or timeout limits. Produces empty `Diagnostics`, null `ErrorCode`.

---

## Method Resolution Bugs

### 73. Instance methods with generic parameters never attempt type inference
**File:** `Runtime/MethodInvoker.cs:178-191`

Generic instance methods without explicit type args are added to candidates with unresolved type parameters and always fail invocation. Static method resolution has a `TryMakeConcreteMethod` inference fallback (lines 367-387), but instance resolution does not.

**Reproducer:** `list.Select(x => x + 1)` — if `Select` is resolved as an instance method candidate (before extension methods), type inference is never attempted.

### 74. `CompareMethodSpecificity` violates C# spec for better-function-member
**File:** `Runtime/MethodInvoker.cs:1388-1413`

Compares parameter types against each other rather than comparing conversions from the argument types. Per ECMA-334 §12.6.4.3, the comparison should be "which parameter type has a better conversion FROM the corresponding argument type", not "which parameter type is more derived."

### 75. Unresolved generic type parameters silently filled with `typeof(object)`
**File:** `Runtime/MethodInvoker.cs:720-725`

When generic type inference fails to resolve all type parameters, remaining ones are replaced with `typeof(object)` instead of removing the method from candidates. This can cause a wrong overload to be selected.

### 76. `null` argument scoring allows CsEval to resolve overloads C# considers ambiguous
**File:** `Runtime/MethodInvoker.cs:1286`

Uses `CompareBetterConversionTarget` as a tiebreaker for `null` arguments when the C# spec does not permit one. `Method(string)` vs `Method(Exception)` with `null` argument — C# reports CS0121 (ambiguous), CsEval silently picks one.

---

# Round 3: Deep-Dive Findings

## IL Emitter / Compiled Path

### 77. `EmitMultiDimArrayInit` ignores `ExplicitSizes`
**File:** `CsEval.Compiled/BoundExpressionEmitter.cs:2033-2048`

Always uses `init.InferredDimensions` as compile-time constants. The interpreter checks for `init.ExplicitSizes` and evaluates those runtime expressions. `new int[n, m] { {1,2}, {3,4} }` where `n`/`m` are variables — emitter ignores them, using only initializer shape.

## Binder Semantic Bugs

### 78. `NormalizeArithmeticType` only promotes `char`, missing `byte`/`sbyte`/`short`/`ushort`
**File:** `Binding/Binder.Operators.cs:215-220`

Per ECMA-334 §12.4.7, `byte + byte` results in `int`. The binder returns `typeof(byte)`, causing downstream `CallBinderService` to look for `byte`-accepting overloads instead of `int`-accepting ones.

### 79. Unary operator result type ignores numeric promotion for sub-int types
**File:** `Binding/Binder.Operators.cs:63`

For unary `-`, `+`, `~` on `byte`/`sbyte`/`short`/`ushort`, the binder sets `resultType = operand.StaticType` instead of `int`. Same downstream overload resolution impact as #78.

### 80. `BindIdentifier` drops `LocalId` for locals typed as `object`
**File:** `Binding/Binder.Expressions.cs:45-55`

Check `staticType != typeof(object)` falls through without calling `TryGetLocal`. Variables typed as `object` never get a `LocalId`, forcing the compiled emitter to slow dictionary-based lookups. If two scopes both have an `object`-typed local named `x`, the emitter cannot distinguish them.

### 81. `CallBinderService` rejects `in` parameters entirely
**File:** `Binding/Services/CallBinderService.cs:145`

Check `parameters.Any(p => p.ParameterType.IsByRef)` rejects methods with `in` parameters, even though `in` is pass-by-value at the call site. Methods with `in` parameters always fall back to slower `BoundInvokeExpr` runtime dispatch.

## Operator & Extension Bugs

### 82. `AggregateBuiltins.Sum` returns stale `decimalTotal` when float/double follows decimal
**File:** `Runtime/Extensions/AggregateBuiltins.cs:92-103, 182-188`

Sequence `[1m, 2, 3.0f]`: decimal accumulator reaches 3, float triggers promotion to double (doubleTotal=6.0), but `usesDecimal` stays true. Return logic checks `usesDecimal` first, returns stale `decimalTotal = 3m` instead of correct `6.0`.

### 83. `AggregateBuiltins.Compare` crashes on Infinity/NaN
**File:** `Runtime/Extensions/AggregateBuiltins.cs:258`

`Convert.ToDecimal(double.PositiveInfinity)` throws `OverflowException`. `min([1.0, double.PositiveInfinity])` crashes.

### 84. `RangeHelpers.GenerateRange` overflows on inclusive `int.MaxValue`
**File:** `Runtime/Extensions/RangeHelpers.cs:19`

`end + 1` overflows to `int.MinValue` when `end = int.MaxValue` and `exclusiveEnd = false`. Loop exits immediately, producing empty sequence.

### 85. `BareMathNames.cbrt` returns NaN for negative numbers
**File:** `Runtime/Extensions/BareMathNames.cs:98`

Uses `Math.Pow(x, 1.0/3.0)` which returns `NaN` for negative bases. Should use `Math.Cbrt` which handles negative inputs correctly.

### 86. RegexMatchOperator has no timeout — ReDoS vulnerability
**File:** `Runtime/Extensions/RegexMatchOperator.cs:31`

`Regex.IsMatch` without timeout. Patterns like `(a+)+$` against crafted input cause catastrophic backtracking. Security concern when evaluating user-provided expressions.

### 87. `Count` includes nulls but `Sum`/`Average` skip them — inconsistent aggregate semantics
**File:** `Runtime/Extensions/AggregateBuiltins.cs:212-221`

`avg([1, null, 3])` returns `2.0` (count=2) but `count([1, null, 3])` returns `3`. So `sum(x) / count(x)` differs from `avg(x)` when nulls are present.

## Lambda & Delegate Bugs

### 88. Nested implicit placeholder lambda lowering fails due to flat scope tracking
**File:** `Parsing/IdentifierOccurrenceCollector.cs:10-11, 48-53`

`_declared` is a flat `HashSet`. Inner lambda's `it` parameter leaks into outer scope's declaration set. `list.Select(it + list2.Count(it > 3))` — inner `Count` lowers `it > 3` to `it => it > 3`, then outer `it` is filtered out because `it` appears in `_declared`. Outer expression never lowers.

### 89. Lambda invocation does not propagate `CancellationToken`
**File:** `Runtime/MethodInvoker.cs:1561`

`InvokeLambda` creates `BoundEvaluator` with `default(CancellationToken)`. Lambdas converted to delegates (e.g., for LINQ `.Where()`, `.Select()`) ignore the cancellation token from the outer evaluation. Long-running lambda bodies in deferred LINQ chains are uncancellable.

## API Surface Issues

### 90. No null argument validation on any public method
**File:** `CsEvalEngine.cs`

`Parse(null)`, `Evaluate(null)`, `SetVariable(null, ...)`, `RegisterModule(null, ...)`, etc. all produce `NullReferenceException` deep in internals instead of `ArgumentNullException` at the entry point.

### 91. `CsEvalCompiledExpression<T>` has no use-after-dispose protection
**File:** `CompiledExpression.cs`

`Invoke()` calls `_engine.GetContextForCompiled()` which does not call `ThrowIfDisposed()`. Invoking a compiled expression after engine disposal uses stale/cleared state.

### 92. `TryParse`/`TryEvaluate` swallow `ObjectDisposedException`
**File:** `CsEvalEngine.cs:194-209, 620, 644`

`catch (Exception ex)` catches `ObjectDisposedException` and reports it as a parse/evaluation error. Use-after-dispose is silently converted to a "failure" result.

### 93. `Evaluate(string, object variables)` silently accepts primitives with no properties
**File:** `CsEvalEngine.cs:555-560, 747-753`

`engine.Evaluate("x + 1", 42)` — `ToVariableDictionary` reflects on `int`'s properties (none), produces empty dict. User's variable is silently ignored.

### 94. `EvaluateWithTrace` silently ignores compiled path
**File:** `CsEvalEngine.cs:413-454`

Always forces the bound evaluator path. When the compiler can handle an expression but the binder cannot, `EvaluateWithTrace` throws while `Evaluate` succeeds.

### 95. `TryCompileInternal` locks on public `expression` object
**File:** `CsEvalEngine.cs:256`

`lock (expression)` where `expression` is a public `CsEvalExpression`. External code locking on the same object causes deadlocks. Should lock on a private object.
