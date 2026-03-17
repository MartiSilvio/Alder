# Codebase Concerns

**Analysis Date:** 2026-03-17

## Tech Debt

**`BindingNotSupportedException` carries raw message strings, not diagnostic codes:**
- Issue: `BindingNotSupportedException` is constructed with interpolated strings throughout. It bypasses the `DiagnosticDescriptor` system that all other exceptions use, making it impossible to programmatically identify or localize these errors.
- Files: `src/CsEval/Binding/BindingNotSupportedException.cs`, `src/CsEval/Binding/Binder.cs:97,394`, `src/CsEval/Interpretation/BoundEvaluator.cs:112,394,411,455,492,528`, `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs:143,297,379,521,556,2062,2110,2299,2315,2323,2526`
- Impact: Errors surfaced from binding/emission mismatches have no diagnostic code. They get caught and re-wrapped at higher levels, losing structure. Also violates the project rule "Never throw `CsEvalException` with just a raw message string."
- Fix approach: Either give `BindingNotSupportedException` a `DiagnosticDescriptor` constructor or replace it with typed `CsEvalException` variants. It is an internal class so no public API change required.

**ExpressionCache key is raw expression text only:**
- Issue: `ILExpressionCompiler.GetOrCompile` keys compiled delegates by `expressionText` string alone (`src/CsEval.Compiled/Compilation/ILExpressionCompiler.cs:23`). `CsEvalOptions` (e.g., `LanguageMode`, `IsCaseSensitive`, `CompilationMode`) is not part of the cache key.
- Files: `src/CsEval/Compilation/ExpressionCache.cs`, `src/CsEval.Compiled/Compilation/ILExpressionCompiler.cs:21-23`
- Impact: Currently safe because the `ExpressionCache` is per-engine instance and engine options are immutable after freeze. However, if child engines ever share a cache with different options (currently they share the same `ExpressionCache` reference from the root), a compilation under different `LanguageMode` or `IsCaseSensitive` could return a mismatched delegate. The `CreateChild` flow should be verified.
- Fix approach: Include a compact options fingerprint in the cache key, or verify and document in code that child engines always inherit identical options.

**`MethodDispatchCache` and `ExtensionMethodsByNameCache`/`ArityCache` are unbounded static singletons:**
- Issue: `MethodDispatchCache.ParameterCache` and `FastInvokerCache` (`src/CsEval/Runtime/MethodDispatchCache.cs:13-14`) grow without bound keyed by `MethodInfo`. `ExtensionMethodsByNameCache` and `ExtensionMethodsByArityCache` in `ExtensionMethodResolver` (`src/CsEval/Runtime/ExtensionMethodResolver.cs:32-33`) also have no eviction. Only `ResolvedPlanByInvocationCache` has a FIFO cap (4096 entries).
- Files: `src/CsEval/Runtime/MethodDispatchCache.cs`, `src/CsEval/Runtime/ExtensionMethodResolver.cs:32-34`
- Impact: In long-running servers processing expressions over many distinct types (e.g., dynamic schema exploration), these static dictionaries grow permanently. The number of unique `MethodInfo` keys is bounded by the registered assemblies, so this is low risk for typical single-application use but notable for multi-tenant scenarios.
- Fix approach: These are keyed by `MethodInfo` (reference-typed, bounded by loaded assemblies) so actual growth is finite. Document the bound explicitly, or add FIFO caps matching the pattern already established for `ResolvedPlanByInvocationCache`.

**`AllowedTypes` allowlist is only enforced for static method calls and construction; not for instance member access:**
- Issue: `SandboxOptions.IsTypeAllowed` is checked in `MethodInvoker` for static method refs (`src/CsEval/Runtime/MethodInvoker.cs:74`) and in `ConstructionRuntime` (`src/CsEval/Runtime/Semantics/ConstructionRuntime.cs:16`). It is not checked in `MemberAccess.GetMember` for instance property/field reads or in the instance method invocation path (`src/CsEval/Runtime/MethodInvoker.cs:130-203`).
- Files: `src/CsEval/CsEvalOptions.cs:189`, `src/CsEval/Runtime/MemberAccess.cs`, `src/CsEval/Runtime/MethodInvoker.cs`
- Impact: An expression operating on a variable of a type not in `AllowedTypes` can still read its properties and call its instance methods when `AllowPropertyRead`/`AllowMethodCalls` are enabled. The `AllowedTypes` contract is documented as restricting "resolved, constructed, or accessed" types but enforcement is incomplete for instance access.
- Fix approach: Add `IsTypeAllowed(obj.GetType())` checks at the top of the instance member access and instance method invocation paths in `MemberAccess.GetMember` and `MethodInvoker.TryInvokeMethod`.

**`CancellationToken` is not propagated into `InvokeLambda`:**
- Issue: `MethodInvoker.InvokeLambda` creates a `BoundEvaluator` without passing the caller's `CancellationToken` (`src/CsEval/Runtime/MethodInvoker.cs:1561`). The token is available at all call sites but the method signature does not accept it.
- Files: `src/CsEval/Runtime/MethodInvoker.cs:1550-1562`, `src/CsEval/Runtime/LambdaDelegateFactory.cs`
- Impact: Cancellation inside lambdas invoked via LINQ operators (e.g., `.Select(x => ...)`, `.Where(x => ...)`) is not honoured. A long-running lambda body cannot be cancelled even if the outer `CancellationToken` fires.
- Fix approach: Add `CancellationToken ct` parameter to `InvokeLambda` and pass it to `new BoundEvaluator(childContext, lambda.Options!, ct)`. Update all call sites including `LambdaDelegateFactory`.

**`IAsyncDisposable.DisposeAsync()` is called with sync-over-async blocking:**
- Issue: Both `ExecutionRuntime.DisposeResource` and `BoundEvaluator.DisposeUsingResource` call `.AsTask().GetAwaiter().GetResult()` on `IAsyncDisposable` objects.
- Files: `src/CsEval/Runtime/Semantics/ExecutionRuntime.cs:100-101`, `src/CsEval/Interpretation/BoundEvaluator.cs:1139-1140`
- Impact: Thread-pool starvation risk if async disposables block. Deadlock risk on single-threaded synchronization contexts (e.g., ASP.NET Framework, Unity). Evaluating `await using` with async resources inside a web request synchronization context could deadlock.
- Fix approach: This is an architectural limitation of the synchronous evaluation model. Document it explicitly or add a note to the `using` statement evaluation that `IAsyncDisposable` resources may deadlock on synchronization-context-constrained hosts.

**`Delegate.DynamicInvoke` used as last-resort callable path:**
- Issue: When a target is a `Delegate` type that is not `LambdaValue` or `CompiledLambdaValue`, `MethodInvoker` falls back to `del.DynamicInvoke(args)` (`src/CsEval/Runtime/MethodInvoker.cs:66`).
- Files: `src/CsEval/Runtime/MethodInvoker.cs:65-66`
- Impact: `DynamicInvoke` boxes all arguments and is ~10-100x slower than a typed invocation. It also suppresses exceptions in `TargetInvocationException`. This is a known .NET perf trap.
- Fix approach: This path is only reached for host-provided `Delegate` objects, which is an edge case. Add a fast-path using `MethodInfo.Invoke` or a typed wrapper when the delegate type is known, or document the limitation and recommend registering delegates as `FunctionRef` instead.

**`TypeResolver._cache` is unbounded:**
- Issue: `TypeResolver` has a `ConcurrentDictionary<string, Type?> _cache` (`src/CsEval/Runtime/TypeResolver.cs:27`) with no size cap. It grows for every unique type name string resolved.
- Files: `src/CsEval/Runtime/TypeResolver.cs:27`
- Impact: Low in practice (bounded by unique type name strings evaluated), but in a REPL or agent loop generating many distinct type references it could grow large. Each `TypeResolver` is per-engine so the cache is bounded to the engine's lifetime.
- Fix approach: Add a FIFO cap at a reasonable size (e.g., 2048 entries) using the same pattern as `ExpressionCache`.

**`Regex.IsMatch` uses interpreted regex with no caching:**
- Issue: `RegexMatchOperator.IsMatch` calls `Regex.IsMatch(left.ToString()!, pattern)` directly (`src/CsEval/Runtime/Extensions/RegexMatchOperator.cs:31`), which constructs and JIT-compiles the regex on every call.
- Files: `src/CsEval/Runtime/Extensions/RegexMatchOperator.cs`
- Impact: Repeated use of `=~` with the same pattern (common in filter expressions) compiles the regex each time. This is purely a performance issue, not a correctness issue.
- Fix approach: Add a bounded `ConcurrentDictionary<string, Regex>` cache keyed by pattern string, using compiled `RegexOptions.Compiled`.

## Security Considerations

**`AllowedTypes` does not restrict instance member access (see Tech Debt above):**
- Risk: Expressions can enumerate/traverse properties of a type even when it is not in `AllowedTypes`, as long as `AllowPropertyRead` is true and the value is already in a variable.
- Files: `src/CsEval/Runtime/MemberAccess.cs`, `src/CsEval/Runtime/MethodInvoker.cs`
- Current mitigation: `GuardReflectionLeak` blocks reflection types from being returned. `AllowPropertyRead = false` on `Strict()` blocks all reads.
- Recommendations: Enforce `IsTypeAllowed` at instance member access boundaries, or document clearly that `AllowedTypes` restricts construction and static resolution only, not instance access.

**`GuardReflectionLeak` is not applied at all return paths:**
- Risk: The guard (`src/CsEval/Runtime/TypeHelpers.cs:1104`) is applied at known reflection-returning points (property getter results, method return values, indexer results). However, the interpreter switch in `BoundEvaluator.Evaluate` (`src/CsEval/Interpretation/BoundEvaluator.cs:37`) does not apply a universal post-evaluation guard. A new expression type added without a guard call would silently leak.
- Files: `src/CsEval/Interpretation/BoundEvaluator.cs`, `src/CsEval/Runtime/TypeHelpers.cs:1104`
- Current mitigation: `GuardReflectionLeak` is applied at all specific reflection-call sites (19 call sites confirmed). The `IsForbiddenReflectionType` check excludes value types and `string` for performance.
- Recommendations: Consider a universal post-evaluation guard at the top-level `Evaluate` return or document that guard placement is the responsibility of each new expression handler.

**Default `SandboxOptions` is `Trusted()` (fully open):**
- Risk: `CsEvalOptions.Sandbox` defaults to `SandboxOptions.Trusted()` (`src/CsEval/CsEvalOptions.cs:59`), which grants all permissions. Callers who construct `CsEvalEngine` without explicitly configuring sandbox options have full access by default.
- Files: `src/CsEval/CsEvalOptions.cs:59`
- Current mitigation: This is a deliberate API design choice for trusted-context use cases.
- Recommendations: For the planned AI agent/sandbox use case, ensure documentation and samples prominently show `SandboxOptions.Safe()` or `SandboxOptions.Strict()` for untrusted input.

## Performance Bottlenecks

**`BoundEvaluator` re-binds lambda bodies on every invocation:**
- Problem: `MethodInvoker.InvokeLambda` calls `binder.Bind(lambda.Body, ...)` on every lambda call (`src/CsEval/Runtime/MethodInvoker.cs:1559-1561`). This means LINQ operators like `.Select(x => x * 2)` re-bind the lambda body on every element.
- Files: `src/CsEval/Runtime/MethodInvoker.cs:1559-1562`
- Cause: The lambda's `BoundExpr` is not cached on the `LambdaValue`. Binding is stateless and repeatable but not free.
- Improvement path: Cache the bound body on `LambdaValue` after first binding (similar to how `CsEvalExpression` caches bound nodes via `ConditionalWeakTable`), or bind eagerly at lambda definition time.

**`Binder` is instantiated on every lambda invocation:**
- Problem: `new Binding.Binder()` is called inside `InvokeLambda` for every call (`src/CsEval/Runtime/MethodInvoker.cs:1559`).
- Files: `src/CsEval/Runtime/MethodInvoker.cs:1559`
- Cause: Binder is stateless per-call but allocation is unnecessary.
- Improvement path: Make `Binder` reusable or cache a `static readonly` instance if it is truly stateless.

**`AstDepthValidator.EnsureWithinLimit` traverses the full AST on every lambda invocation:**
- Problem: Called inside `InvokeLambda` at `src/CsEval/Runtime/MethodInvoker.cs:1558` before every lambda execution. A 100-element LINQ Select traverses the lambda AST 100 times.
- Files: `src/CsEval/Runtime/MethodInvoker.cs:1558`, `src/CsEval/Parsing/AstDepthValidator.cs`
- Improvement path: Validate depth once at lambda definition time and cache the result on `LambdaValue`.

## Fragile Areas

**`BoundExpressionEmitter.cs` (2646 lines) — single-file IL emitter:**
- Files: `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`
- Why fragile: At 2646 lines it is the largest file in the codebase by a large margin. It handles IL emission for every bound node type through a large switch. Adding a new bound node type requires changes in both the interpreter (`BoundEvaluator`) and the emitter, and the emitter's size makes it easy to miss cases.
- Safe modification: Every new `BoundExpr` subtype added to `Binding/BoundNodes/` must have a corresponding arm in `BoundExpressionEmitter.Emit` (the `_ =>` fallthrough throws `BindingNotSupportedException`). Run the full `ILCompilationTests` and `CompiledHotPathRegressionTests` suites after any changes.
- Test coverage: `tests/CsEval.Test/Compilation/ILCompilationTests.cs`, `tests/CsEval.Test/Compilation/CompilationTests.cs`

**`BoundEvaluator.cs` (1669 lines) — monolithic interpreter:**
- Files: `src/CsEval/Interpretation/BoundEvaluator.cs`
- Why fragile: Mirrors the emitter's structure. Control flow signals (`ControlFlowSignal`) thread through all statement handlers manually. The `_caughtExceptions` stack and `_breakContextDepth`/`_loopDepth` counters are mutable instance state. Incorrect handling of nested loops, try/catch/finally, and lock statements could corrupt these counters.
- Safe modification: Nested control flow tests are in `tests/CsEval.Test/Runtime/BoundExecutionTests.cs` and `tests/CsEval.Test/GapAuditTests.cs`.

**`netstandard2.0` target diverges silently on collection APIs:**
- Files: `src/CsEval/Compatibility/NetStandardPolyfills.cs`, `src/CsEval/Runtime/Collections/FixedDictionary.cs`, `src/CsEval/Runtime/Collections/FixedSet.cs`
- Why fragile: On `netstandard2.0`, `FixedDictionary` falls back to a plain `Dictionary<TKey, TValue>` (not frozen), and `FixedSet` has a similarly different implementation. These two code paths diverge silently — there is no CI matrix that runs tests against `netstandard2.0` explicitly.
- Safe modification: Any change to `FixedDictionary` or `FixedSet` must verify both `#if NET8_0_OR_GREATER` and `#else` branches. Range/Index polyfills in `NetStandardPolyfills.cs` are minimal custom implementations; their correctness is not covered by a dedicated polyfill test class.

**`CompiledProviderRegistry` uses a global static mutable slot:**
- Files: `src/CsEval/Compilation/CompiledProviderRegistry.cs`
- Why fragile: `_provider` is a global static that is set once by `CsEval.Compiled` registration. If two packages tried to register different providers (e.g., FastExpressionCompiler backend vs default), the last write wins silently. The lock is only on the write, not reads, so there is a potential race between `GetProvider()` (non-locked read) and `Register()` under extremely early concurrent access.
- Safe modification: The current usage pattern (register once at startup) is safe. A future plugin architecture for IL backends must design a proper registration model.

## Test Coverage Gaps

**`netstandard2.0` code paths not tested in CI:**
- What's not tested: `FixedDictionary` plain-Dictionary fallback, `FixedSet` fallback, `Index`/`Range` polyfill implementations, `NetStandardPolyfills.TryAdd`.
- Files: `src/CsEval/Compatibility/NetStandardPolyfills.cs`, `src/CsEval/Runtime/Collections/FixedDictionary.cs`, `src/CsEval/Runtime/Collections/FixedSet.cs`
- Risk: A bug in the polyfill path would be invisible until a consumer targets `netstandard2.0`.
- Priority: Medium

**`AllowedTypes` instance access enforcement not tested:**
- What's not tested: No test verifies that a type in a variable but not in `AllowedTypes` has its instance members blocked.
- Files: `tests/CsEval.Test/Security/SandboxModeTests.cs`
- Risk: The gap between documented and actual `AllowedTypes` behavior could be discovered by a security audit rather than a test.
- Priority: High

**`InvokeLambda` cancellation propagation not tested:**
- What's not tested: No test verifies that cancelling the outer `CancellationToken` interrupts a long-running lambda body inside a LINQ `.Select` or `.Where`.
- Files: `tests/CsEval.Test/` (no dedicated cancellation-in-lambda test found)
- Risk: Timeout/cancellation guarantees in agent/server contexts may silently not apply to lambda-heavy expressions.
- Priority: High

**`IAsyncDisposable` sync-over-async deadlock not tested:**
- What's not tested: No test exercises `IAsyncDisposable` objects inside `using` statements on a constrained synchronization context.
- Files: `src/CsEval/Runtime/Semantics/ExecutionRuntime.cs:96-101`
- Risk: Deadlock in ASP.NET Framework or similar hosts would only surface at runtime.
- Priority: Low

---

*Concerns audit: 2026-03-17*
