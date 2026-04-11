# Pre-Release Adversarial Audit

**Purpose:** This is not a code review or feature request. It is a systematic defect hunt across the entire Alder runtime engine. The goal is to find bugs that would ship to production — state corruption, isolation failures, security escapes, contract violations, parity divergences, and resource leaks.

**How to work:** For each section below, read the referenced source files, trace the code paths described, and identify concrete defects. For every defect found, write a failing NUnit test that proves the bug exists. Describe the minimal fix in a comment above the test. Do not implement fixes — only tests and descriptions.

**What counts as a defect:** Any behavior that (a) violates ECMA-334, (b) diverges between the interpreted and compiled paths, (c) corrupts state across engine instances, (d) leaks resources without bound, (e) escapes the security sandbox, (f) violates the API contract implied by the method name, or (g) produces incorrect diagnostics.

**Output format:** One test class per section. Each test method should have a descriptive name that explains the invariant being violated. Group all tests into `tests/Alder.Test/Audit/` with one file per section.

---

## Section 1: Thread Safety Under Concurrent Mutation

### Context

Alder supports concurrent evaluation via child engines (`CreateChild()`). The existing `ThreadSafetyTests.cs` verify that concurrent reads produce correct results. This section audits concurrent writes.

### Files to read

- `src/Alder/AlderEngine.cs` — Focus on `_context`, `_pendingVariables`, `_contextInitLock`, `GetOrCreateContext()`, `SetVariable`, `Evaluate`, `CreateChild`, `Dispose`
- `src/Alder/Runtime/AlderContext.cs` — Focus on `_variables` (the backing store), `Define`, `TryGet`, `Set`, `CreateChild`, `GetAllVisible`
- `src/Alder/Runtime/ExpressionCache.cs` — Is the cache thread-safe? What happens during concurrent `GetOrAdd` + `Clear`?

### Scenarios to test

1. **SetVariable during Evaluate.** Thread A evaluates `x + y + z` (a slow expression with 3 variable lookups). Thread B calls `SetVariable("y", newValue)` mid-evaluation. Does thread A see a torn state (old x, new y, old z)? Does it throw? Does it silently produce wrong results? The answer depends on whether `AlderContext._variables` is a `ConcurrentDictionary` or a plain `Dictionary`. Read the code and determine.

2. **Concurrent SetVariable on the same key.** Two threads call `SetVariable<int>("counter", N)` with different values simultaneously. After both complete, is the final value deterministic? Is the type inference version (`_variableTypeVersion`) consistent with the final value?

3. **CreateChild during Evaluate.** Thread A evaluates a long expression on the parent engine. Thread B calls `CreateChild()` which calls `GetOrCreateContext()`. If the parent context is being lazily initialized by thread A at the same time, does `_contextInitLock` correctly serialize? Read the lock usage and verify.

4. **Dispose during Evaluate.** Thread A starts evaluating `Enumerable.Range(0, 1000000).Sum()`. Thread B calls `Dispose()` after 1ms. Does thread A get `ObjectDisposedException` cleanly, or does it corrupt internal state? Note: `ThrowIfDisposed()` is only checked at the entry point of `Evaluate` — once evaluation starts, there's no further disposal check. Is this acceptable, or can disposal cause a `NullReferenceException` in the evaluator when `_context` is accessed after cache clearing?

5. **Concurrent CreateChild.** 10 threads each call `CreateChild()` on the same parent simultaneously. Each child has its own variables. Verify no child sees another child's variables, and the parent is unaffected.

6. **SetVariable with type change during compiled delegate invocation.** Compile a `Func<int, int>` that references engine variable `multiplier`. Thread A invokes the delegate in a loop. Thread B changes `multiplier` from `int` to `double` via `SetVariable<double>("multiplier", 2.5)`. Does the compiled delegate throw `CompiledExpressionStale`, produce wrong results, or crash?

---

## Section 2: Compilation Path Parity

### Context

Alder has two execution backends: the tree-walking interpreter (`src/Alder/Interpretation/`) and the IL compiler (`src/Alder.Compiled/`). Every expression should produce the same result regardless of which backend executes it. The parity test infrastructure (`tests/Alder.Test/Parity/`) catches many cases, but edge cases may diverge.

### Files to read

- `src/Alder/Interpretation/Evaluators/` — All evaluator files, especially `BinaryOperationEvaluator.cs`, `CastEvaluator.cs`, `ConditionalEvaluator.cs`, `NullCoalescingEvaluator.cs`
- `src/Alder.Compiled/Compilation/Emission/Emitters/` — The compiled equivalents of each evaluator
- `src/Alder/Runtime/NumericOperations.cs` — Shared numeric logic used by both paths
- `src/Alder.Compiled/Compilation/Emission/EmitHelpers.cs` — Compiled path type coercion

### Expressions to test in both modes

For each expression, create a test that evaluates in Interpreted mode and Compiled mode and asserts the results are identical (same value, same type, same exception type if it throws).

1. **Nullable arithmetic:** `int? x = null; return x + 1;` — should be `null`. `int? x = 5; int? y = null; return x * y;` — should be `null`. Verify both paths handle lifted operators per ECMA-334 S12.4.8.

2. **Mixed numeric widening:** `return 1 + 2L + 3.0f + 4.0;` — verify the promotion chain is `int -> long -> float -> double` and the final type is `double` in both paths.

3. **Checked overflow:** `checked { return int.MaxValue + 1; }` — should throw `OverflowException` in both paths. `unchecked { return int.MaxValue + 1; }` — should wrap to `int.MinValue` in both paths.

4. **Null-conditional chains:** `string s = null; return s?.Trim()?.ToUpper()?.Length;` — should be `null` (type `int?`). Verify the compiled path correctly emits null-check-and-short-circuit for each `?.` in the chain.

5. **Compound assignment on resolved members:** Create an object with a `Count` property. `obj.Count += 5;` — verify both paths read, add, and write back correctly, and that TypedDispatch is consulted in both paths.

6. **String interpolation with format specifiers:** `double pi = 3.14159; return $"{pi:F2}";` — should be `"3.14"`. `DateTime dt = new DateTime(2024, 1, 15); return $"{dt:yyyy-MM-dd}";`

7. **`as` cast returning null vs `is` check:** `object x = "hello"; return x as int?;` — should be `null`. `object x = 42; return x is string;` — should be `false`.

8. **Ternary with different arm types:** `bool b = true; return b ? 1 : 2.0;` — should be `2.0` (type `double`) when false, `1.0` (type `double`) when true (implicit widening of the `int` arm).

9. **Lambda capturing loop variable:** `var funcs = new List<Func<int>>(); for (int i = 0; i < 3; i++) { int captured = i; funcs.Add(() => captured); } return funcs[1]();` — should be `1`. Classic closure-over-loop-variable test.

10. **Nested null-conditional with method calls:** `string[] arr = null; return arr?.FirstOrDefault()?.ToUpper();` — should be `null` without throwing.

---

## Section 3: Memory and Resource Leaks

### Context

Alder uses several static and instance-level caches. In a long-running server, these caches must not grow without bound. Engines created and disposed must be fully GC-collectible.

### Files to read

- `src/Alder/Runtime/ExpressionCache.cs` — How are parsed/bound expressions cached? Is there an eviction policy?
- `src/Alder/Runtime/MethodDispatchCache.cs` — `ParameterCache`, `FastInvokerCache` — keyed by `MethodInfo`. If 100K different methods are called, does this grow to 100K entries?
- `src/Alder/Runtime/TypeMetadataProvider.cs` — All the `ConcurrentDictionary` caches for property/field/method lookups. Keyed by `(Type, string, BindingFlags)` structs. What's the growth pattern?
- `src/Alder/Runtime/LambdaDelegateConverter.cs` — `ConditionalWeakTable<object, ConcurrentDictionary<Type, Delegate>>`. The weak table keys on the lambda object. When the lambda is GC'd, does the entry disappear?
- `src/Alder/AlderEngine.cs` — `VariableAccessorCache` (static), `_pipelineCache` (instance `ConditionalWeakTable`)
- `src/Alder.Compiled/Compilation/BoundRuntimeMethodCache.cs` and `CompilerReflectionCache.cs` — Static `MethodInfo` caches

### Scenarios to test

1. **ExpressionCache unbounded growth.** Create one engine. Evaluate 10,000 unique expressions (`$"return {i};"` for i in 0..9999). Measure cache size or memory. Dispose the engine. Does the cache get cleared? Create a new engine — does it inherit the old cache?

2. **Child engine GC.** Create a parent engine. Create 1,000 child engines via `CreateChild()`. Set a variable on each child. Dispose all children. `GC.Collect()`. Verify the parent's memory hasn't grown proportionally — specifically, verify child `AlderContext` instances are collected. Use `WeakReference` to track a child engine and verify it becomes null after GC.

3. **Lambda delegate cache.** Evaluate `x => x + 1` and convert it to `Func<int, int>` 1,000 times. The `ConditionalWeakTable` should cache the delegate. Verify: (a) same lambda object produces cached delegate (not 1,000 compilations), (b) when the lambda object goes out of scope and is GC'd, the cache entry is reclaimed.

4. **MethodDispatchCache growth.** Call 1,000 different methods via the engine (different types, different method names). Verify `ParameterCache` and `FastInvokerCache` grow proportionally but don't leak. Then call the same 1,000 methods again — verify cache hits, no growth.

5. **Compiled expression delegate lifetime.** Compile an expression via `CompileExpression<Func<int>>`. Dispose the engine. The compiled delegate holds a reference to the engine's `AlderContext` via closure. Can the engine be GC'd, or does the delegate pin it? If it pins it, that's a leak for long-lived delegates.

---

## Section 4: Security Sandbox Escapes

### Context

Alder has a `SecurityPolicy` that restricts which types, namespaces, and members can be accessed. A sandbox escape means user-provided expression code can reach a blocked type or method despite the policy.

### Files to read

- `src/Alder/Runtime/Security/SecurityPolicy.cs` — What does it block? How is it checked?
- `src/Alder/Runtime/TypeResolver.cs` — `ResolveType` — does it check the security policy before returning?
- `src/Alder/Runtime/MemberAccess.cs` — `GetMember`, `SetMember` — are security checks applied to member access on allowed types?
- `src/Alder/Runtime/MethodInvoker.cs` — `InvokeMemberCall`, `InvokeMethodCore` — are security checks applied before method invocation?
- `src/Alder/Binding/Binder.cs` or equivalent — does the binder enforce security, or only the runtime?
- `src/Alder.Compiled/Compilation/Emission/Emitters/` — does the compiled path check security, or does it assume the binder already did?

### Escape vectors to test

Configure an engine with a restrictive sandbox (block `System.IO`, `System.Diagnostics`, `System.Reflection`). Then attempt each of the following. Every one should fail with a security error:

1. **Direct type access:** `System.IO.File.ReadAllText("/etc/passwd")` — should be blocked.

2. **typeof + reflection:** `typeof(System.IO.File).GetMethod("ReadAllText")` — should `typeof(System.IO.File)` itself be blocked? If not, can `.GetMethod().Invoke()` be used to call it?

3. **Generic type parameter:** `new List<System.IO.FileInfo>()` — does the security policy check generic type arguments?

4. **Extension method resolution:** If a blocked namespace has extension methods, can they be pulled in via `using`-equivalent resolution? E.g., if `System.Linq` is allowed but the engine resolves extension methods from all loaded assemblies, could a malicious extension method in a blocked namespace be resolved?

5. **Dynamic dispatch:** `dynamic d = GetSomething(); d.DangerousMethod();` — dynamic dispatch bypasses compile-time type checking. Does the runtime enforce security on dynamic member access?

6. **Lambda closure escape:** `var ctx = GetInternalContext(); return ctx.Config;` — if a user-defined function or variable somehow references an internal Alder object, can they traverse it to reach blocked types?

7. **Cast to object and reflect:** `object o = "hello"; return o.GetType().Assembly.GetTypes();` — this starts from an allowed type (`string`) but uses reflection to enumerate all types in the assembly, potentially including blocked ones.

8. **Nested type access:** `System.Environment.SpecialFolder.Desktop` — `System.Environment` might be blocked, but does the security check catch nested type/enum access?

9. **Compiled path bypass:** If the binder checks security but the compiled IL emitter doesn't re-check at runtime, can a time-of-check-time-of-use gap be exploited? Register a type, compile an expression, then change the security policy — does the compiled delegate respect the new policy?

---

## Section 5: Cancellation Correctness

### Context

Every public API accepts `CancellationToken`. Cancellation must be responsive — a cancelled token should interrupt evaluation within a bounded time, not just at the entry point.

### Files to read

- `src/Alder/Interpretation/EvaluationContext.cs` or wherever the cancellation token is checked during evaluation
- `src/Alder/Interpretation/Evaluators/` — which evaluators check `ct.ThrowIfCancellationRequested()`? Specifically check loop evaluators (`ForEvaluator`, `WhileEvaluator`, `ForEachEvaluator`)
- `src/Alder.Compiled/Compilation/Emission/Emitters/` — does the compiled path emit cancellation checks in loops?
- `src/Alder/AlderEngine.cs` — `EvaluateAsync` — does the async path properly flow the token?

### Scenarios to test

1. **Already-cancelled token on every API.** For each of: `Evaluate`, `Evaluate<T>`, `TryEvaluate`, `TryEvaluate<T>`, `TryValidate`, `Parse`, `CompileExpression`, `ParseAsExpression`, verify that passing `new CancellationTokenSource().Cancel().Token` throws `OperationCanceledException` (not swallowed, not wrapped in `AlderException`).

2. **Infinite loop cancellation.** `while (true) { }` with a token that cancels after 50ms. Verify `OperationCanceledException` is thrown within 200ms. If the loop runs forever, the cancellation token is not checked inside the `while` evaluator.

3. **Infinite for loop.** `for (;;) { }` — same test.

4. **Large foreach.** `foreach (var x in Enumerable.Range(0, int.MaxValue)) { }` with cancellation after 50ms.

5. **Nested function call loop.** `Enumerable.Range(0, int.MaxValue).Select(x => x * 2).ToList()` — cancellation during LINQ evaluation. Does the token propagate into the lambda evaluation?

6. **Compiled path loop cancellation.** Same `while(true)` test but on a compiled engine (`UseCompiler()`). Does the compiled IL emit `ct.ThrowIfCancellationRequested()` inside loops, or only the interpreted path?

7. **Async evaluation cancellation.** `await Task.Delay(10000)` with cancellation after 50ms via `EvaluateAsync`. Verify cancellation propagates through the async machinery.

8. **Cancellation during type resolution.** An expression like `new VeryExpensiveTypeToResolve()` where type resolution is slow (many assemblies loaded). Does cancellation interrupt the resolver?

---

## Section 6: Error Diagnostic Fidelity

### Context

Alder uses Roslyn CS-prefixed error codes for compatibility. Every CS code must match Roslyn's semantics — same code for the same error condition.

### Files to read

- `src/Alder/Diagnostics/DiagnosticDescriptors.cs` — the full catalog of error codes
- `src/Alder/Diagnostics/DiagnosticCode.cs` — the enum of codes
- For each CS code, verify against the Roslyn documentation or by running the equivalent code in a `.csx` script

### Codes to verify

For each code below, write the invalid expression, evaluate it in Alder, and assert the thrown `AlderException.ErrorCode` matches:

1. **CS0019** — `1 + true` (operator not applicable to operand types)
2. **CS0029** — `int x = "hello"; return x;` (cannot implicitly convert)
3. **CS0030** — `return (int)"hello";` (cannot convert type)
4. **CS0103** — `return undefinedVariable;` (name does not exist)
5. **CS0117** — `return string.NonExistentMethod();` (type does not contain definition)
6. **CS0176** — `"hello".Empty` (accessing static member on instance — `string.Empty` is static)
7. **CS0246** — `typeof(NonExistentType)` (type or namespace not found)
8. **CS1501** — `Math.Max(1, 2, 3)` (wrong number of arguments)
9. **CS1503** — `Math.Max("a", "b")` (argument type mismatch — Max expects numeric)
10. **CS1061** — `"hello".FakeMethod()` (type does not contain definition for member)

Also: grep the codebase for `new AlderException(` that takes a raw `string` first argument instead of a `DiagnosticDescriptor`. Every instance is a violation of the structured diagnostics contract.

---

## Section 7: Dispose Semantics

### Context

`AlderEngine` implements `IDisposable`. Dispose must be safe to call multiple times, must not corrupt state, and must make all subsequent API calls throw `ObjectDisposedException`.

### Files to read

- `src/Alder/AlderEngine.cs` — `Dispose()`, `ThrowIfDisposed()`, `IsDisposed()`, the `DisposalToken` class
- `src/Alder.Compiled/AlderCompiledEngineExtensions.cs` — `Compile<TDelegate>`, `CompileExpression`, `ParseAsExpression` — do they check disposal?

### Scenarios to test

1. **Double dispose.** `engine.Dispose(); engine.Dispose();` — must not throw.

2. **Every API after dispose.** For each public method on `AlderEngine`, call it after disposal and assert `ObjectDisposedException`: `Evaluate`, `Evaluate<T>`, `TryEvaluate`, `TryEvaluate<T>`, `TryValidate`, `Parse`, `SetVariable`, `CreateChild`, `CompileExpression`, `ParseAsExpression`, `Compile<Func<int>>`.

3. **Compiled delegate after engine dispose.** `var fn = engine.Compile<Func<int, int>>("x + 1", "x"); engine.Dispose(); fn(5);` — what happens? The delegate holds a closure over the engine's context. Does it throw `ObjectDisposedException`? Does it return a stale result? Does it crash? Document the current behavior and decide if it's acceptable.

4. **Dispose parent during child evaluation.** Parent creates child. Child starts evaluating a slow expression. Parent disposes mid-evaluation. Does the child throw `ObjectDisposedException` at the next `ThrowIfDisposed` check, or does it complete with potentially corrupted state?

5. **Dispose child during parent evaluation.** Parent evaluates a slow expression. Child (created earlier) is disposed by another thread. Parent should be completely unaffected — verify no exception, no state corruption, correct result.

6. **CreateChild after parent dispose.** `parent.Dispose(); parent.CreateChild();` — must throw `ObjectDisposedException`.

7. **SetVariable after dispose on child.** `child.Dispose(); child.SetVariable("x", 1);` — must throw.

---

## Section 8: AOT Dispatch Completeness and Parity

### Context

The AOT system generates `TypedDispatch` subclasses that handle member access without reflection. The interpreted evaluator and compiled emitter both consult `TypedDispatchHelper` before falling back to reflection. Every operation must produce identical results whether handled by AOT dispatch or reflection fallback.

### Files to read

- `src/Alder/Runtime/TypedDispatchHelper.cs` — all `Try*` methods
- `src/Alder/Aot/TypedDispatch.cs` — the base class and its virtual methods
- `src/Alder/Runtime/MemberAccess.cs` — `GetMember`, `SetMember`, `GetResolvedMember`, `SetResolvedMember`
- `src/Alder/Runtime/MethodInvoker.cs` — `InvokeMemberCall`, `InvokeResolvedMethod`
- `src/Alder/Interpretation/Evaluators/ResolvedCallEvaluator.cs` — how resolved calls consult dispatch
- `src/Alder.Compiled/Compilation/Emission/Emitters/ResolvedCallEmitter.cs` — how compiled calls consult dispatch

### Approach

Create a test `TypedDispatch` subclass (`ParityAuditDispatch`) that wraps reflection — it handles every operation by calling the real reflection path and recording that it was called. Register it for a test type. Then run every operation through both the AOT-dispatch path and the reflection-only path (by using `ClearBuiltInContext()`) and assert identical results.

### Scenarios to test

1. **Property get** — instance and static, value type and reference type return
2. **Property set** — instance, value type and reference type assignment
3. **Field get** — instance and static
4. **Field set** — instance, non-readonly
5. **Method invoke** — instance with args, static with args, void return
6. **Method invoke with overloads** — two methods with same name, different parameter types. Does AOT dispatch select the correct overload?
7. **Method invoke with default parameters** — `Method(int x, int y = 10)` called as `Method(5)`. Does AOT dispatch handle the default?
8. **Method invoke with params array** — `Method(params int[] values)` called as `Method(1, 2, 3)`. Does AOT dispatch handle params expansion?
9. **Indexer get/set** — `obj[key]` and `obj[key] = value`
10. **Constructor** — default and parameterized
11. **Null argument** — `Method(null)` where parameter type is `string`. Does AOT dispatch handle null without `NullReferenceException`?
12. **Inherited member** — base class property accessed on derived instance. Is the dispatch registered for the derived type or the declaring type? Does `TryGetDispatch` check the inheritance chain?
13. **Interface member** — type implements `IComparable<T>`. Is dispatch consulted for interface method calls?
14. **Case-insensitive mode** — `m.name` when dispatch handles `"Name"`. Verify canonical name resolution produces the same result as reflection with `BindingFlags.IgnoreCase`.
