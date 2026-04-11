# Runtime Defect Eradication Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Eliminate the highest-risk runtime defects found in the pre-release audit with test-first fixes that harden engine isolation, delegate compilation semantics, scope consistency, and case-sensitivity behavior.

**Architecture:** The work starts by codifying the broken invariants as failing tests. Then the runtime is reworked so engine lifetime is instance-owned, typed delegates compile against isolated bindings instead of mutating shared engine state, expression-tree compilation uses the same visible scope as evaluation, and case-sensitivity rules are applied consistently across runtime resolution paths.

**Tech Stack:** .NET 8, C# 12, NUnit, Alder interpreter, Alder.Compiled, AOT typed dispatch infrastructure.

---

### Task 1: Engine Lifetime Isolation

**Files:**
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Core/ThreadSafetyTests.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/AlderEngine.cs`
- Test: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Core/ThreadSafetyTests.cs`

**Step 1: Write the failing test**

Add tests that dispose a child engine and then assert the parent and sibling engines still evaluate correctly.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~DisposeChild" -v minimal`

Expected: FAIL because disposing a child currently poisons the parent engine family.

**Step 3: Write minimal implementation**

Refactor engine disposal so each engine instance owns its disposed state while shared caches remain independently reference-safe.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~DisposeChild" -v minimal`

Expected: PASS.

**Step 5: Commit**

```bash
git add tests/Alder.Test/Core/ThreadSafetyTests.cs src/Alder/AlderEngine.cs
git commit -m "Preserve engine isolation when child scopes are disposed"
```

### Task 2: Typed Delegate Compilation Isolation

**Files:**
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Core/TypedDelegateCompilationTests.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder.Compiled/AlderCompiledEngineExtensions.cs`
- Test: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Core/TypedDelegateCompilationTests.cs`

**Step 1: Write the failing test**

Add tests proving:
- `Compile<TDelegate>` does not overwrite pre-existing engine variables.
- failed delegate compilation does not leave typed parameter placeholders behind.
- custom delegate types compile with the same signature rules as runtime delegate conversion.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~Compile_DoesNotOverwrite|Name~Compile_FailureDoesNotLeak|Name~Compile_CustomDelegate" -v minimal`

Expected: FAIL because typed delegate compilation currently mutates shared engine state and only understands `Func`/`Action` generic patterns.

**Step 3: Write minimal implementation**

Redesign delegate compilation to bind against an isolated typed scope instead of ambient engine variables, and infer delegate signatures from `Invoke`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~Compile_DoesNotOverwrite|Name~Compile_FailureDoesNotLeak|Name~Compile_CustomDelegate" -v minimal`

Expected: PASS.

**Step 5: Commit**

```bash
git add tests/Alder.Test/Core/TypedDelegateCompilationTests.cs src/Alder.Compiled/AlderCompiledEngineExtensions.cs
git commit -m "Compile typed delegates without mutating ambient engine state"
```

### Task 3: Expression Tree Scope Consistency

**Files:**
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Compilation/ExpressionTreeTests.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/Runtime/AlderContext.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder.Compiled/AlderCompiledEngineExtensions.cs`
- Test: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Compilation/ExpressionTreeTests.cs`

**Step 1: Write the failing test**

Add a test that creates a child engine inheriting a parent variable and asserts `ParseAsExpression` and `CompileExpression` can use that inherited variable just like `Evaluate` can.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~ChildEngine_ParseAsExpression_SeesInheritedVariables" -v minimal`

Expected: FAIL because expression-tree compilation currently snapshots only child-local variables.

**Step 3: Write minimal implementation**

Change variable snapshotting to walk visible parent scope so expression-tree compilation sees the same binding environment as evaluation.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~ChildEngine_ParseAsExpression_SeesInheritedVariables" -v minimal`

Expected: PASS.

**Step 5: Commit**

```bash
git add tests/Alder.Test/Compilation/ExpressionTreeTests.cs src/Alder/Runtime/AlderContext.cs src/Alder.Compiled/AlderCompiledEngineExtensions.cs
git commit -m "Align expression tree scope with engine evaluation scope"
```

### Task 4: Case-Insensitive Type Resolution Consistency

**Files:**
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Runtime/TypeResolverTests.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/Runtime/TypeResolver.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/Runtime/MemberAccess.cs`
- Test: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/Runtime/TypeResolverTests.cs`

**Step 1: Write the failing test**

Add tests that use lower-cased fully qualified type names under case-insensitive mode and assert both resolution and static member access succeed.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~CaseInsensitive_FullyQualifiedType" -v minimal`

Expected: FAIL because namespace-prefix checks currently use ordinal matching.

**Step 3: Write minimal implementation**

Refactor type-resolution prefix tracking to honor the engine comparer through the full resolution path.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~CaseInsensitive_FullyQualifiedType" -v minimal`

Expected: PASS.

**Step 5: Commit**

```bash
git add tests/Alder.Test/Runtime/TypeResolverTests.cs src/Alder/Runtime/TypeResolver.cs src/Alder/Runtime/MemberAccess.cs
git commit -m "Honor case-insensitive type resolution for fully qualified names"
```

### Task 5: AOT Delegate Factory Isolation

**Files:**
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/AOT/GeneratedContextIntegrationTests.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/AlderEngine.cs`
- Modify: `/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/Runtime/LambdaDelegateConverter.cs`
- Test: `/Users/silviomartignetti/Developer/Wovera/CsEval/tests/Alder.Test/AOT/GeneratedContextIntegrationTests.cs`

**Step 1: Write the failing test**

Add tests that create two engines with different AOT delegate-factory contexts and prove one engine’s factories do not change the other engine’s lambda conversion behavior.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~AotFactories_AreEngineScoped" -v minimal`

Expected: FAIL because AOT factories are currently stored as global process state.

**Step 3: Write minimal implementation**

Move delegate-factory ownership into engine/config-local state and thread that state explicitly into delegate conversion.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Alder.Test/Alder.Test.csproj --filter "Name~AotFactories_AreEngineScoped" -v minimal`

Expected: PASS.

**Step 5: Commit**

```bash
git add tests/Alder.Test/AOT/GeneratedContextIntegrationTests.cs src/Alder/AlderEngine.cs src/Alder/Runtime/LambdaDelegateConverter.cs
git commit -m "Scope AOT delegate factories to individual engine instances"
```
