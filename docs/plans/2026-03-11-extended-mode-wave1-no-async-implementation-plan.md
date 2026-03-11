# Extended Mode Wave 1 (No Async) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ship Wave 1 Extended-mode features (implicit `it`, comprehensions, `let-in`, scope functions, if-as-expression, tracing, aggregate built-ins, date arithmetic sugar, property destructuring) while preserving strict Standard mode and interpreted/compiled parity.

**Architecture:** Keep a single semantic core. New Extended syntax must lower to existing core AST/bound semantics whenever possible (parse-time or bind-time desugaring). Both executors must consume equivalent bound semantics, and parity must be enforced with paired `.csx` and `.roslyn.csx` oracles.

**Tech Stack:** C#/.NET 8, existing parser/binder/bound evaluator, expression-tree compiled backend, NUnit, Roslyn script parity harness, `TestData` corpus.

---

## Non-Negotiable Constraints

1. Exactly two user-visible modes:
   - `Standard` = ECMA-334 behavior only
   - `Extended` = all sugar
2. Core-first semantics:
   - Extended features lower to core semantics whenever feasible.
3. Executor parity:
   - Interpreted and compiled must produce identical value/type/exception class.
4. Roslyn oracle for custom syntax:
   - Custom `.csx` must have a canonical `.roslyn.csx` sibling when equivalent C# exists.
5. Deferred out of scope:
   - `async/await` in expressions.

---

### Task 1: Lock the Parity Contract First

**Files:**
- Create: `tests/CsEval.Test/Compliance/CustomSyntaxRoslynPairingTests.cs`
- Modify: `tests/CsEval.Test/ParityTests.cs`
- Modify: `tests/CsEval.Test/TestHelpers.cs`

**Step 1: Write the failing contract tests**

```csharp
[Test]
public void ExtendedSyntax_CustomSyntaxCases_MustHaveRoslynSibling_WhenEquivalentExists()
{
    // Scan TestData/ValidExpressions/ExtendedSyntax/**/*.csx
    // Fail if file is custom syntax and sibling .roslyn.csx is missing.
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CustomSyntaxRoslynPairingTests" -v minimal`  
Expected: FAIL (fixture missing and/or missing pair files)

**Step 3: Implement minimal pairing checks and exception normalization helper**

```csharp
public static string NormalizeExceptionKey(Exception ex) =>
    ex is CsEvalException cs && cs.ErrorCode is not null
        ? $"{cs.FormattedCode}:{ex.GetType().Name}"
        : ex.GetType().Name;
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CustomSyntaxRoslynPairingTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add tests/CsEval.Test/Compliance/CustomSyntaxRoslynPairingTests.cs tests/CsEval.Test/ParityTests.cs tests/CsEval.Test/TestHelpers.cs
git commit -m "test: enforce custom syntax roslyn pairing contract"
```

### Task 2: Add the Wave 1 Test Corpus Before Implementation

**Files:**
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/implicit-it-where.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/implicit-it-where.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/comprehension-even-squares.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/comprehension-even-squares.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/let-in-tax.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/let-in-tax.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/scope-let.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/scope-let.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/if-expression-basic.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/if-expression-basic.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/aggregate-sum.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/aggregate-sum.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/date-plus-days.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/date-plus-days.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/destructure-object-letin.csx`
- Create: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/destructure-object-letin.roslyn.csx`
- Create: `tests/CsEval.Test/TestData/InvalidExpressions/ExtendedSyntax/*.csx` (invalid forms per feature)

**Step 1: Add failing parity corpus files**

```csharp
// implicit-it-where.csx
new[] { 1, 2, 3, 4 }.Where(it > 2).ToArray()

// implicit-it-where.roslyn.csx
new[] { 1, 2, 3, 4 }.Where(x => x > 2).ToArray()
```

**Step 2: Run corpus parity to verify RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParityTests.ValidExpressionsShouldPass" -v minimal`  
Expected: FAIL on new Wave 1 cases

**Step 3: Add minimal invalid corpus cases**

```csharp
// Invalid: comprehension missing 'in'
[x * x for x 1..10]
```

**Step 4: Run invalid parity test to verify expected failures are exercised**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParityTests.InvalidExpressionsShouldThrow" -v minimal`  
Expected: FAIL until feature-specific diagnostics are implemented

**Step 5: Commit**

```bash
git add tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax tests/CsEval.Test/TestData/InvalidExpressions/ExtendedSyntax
git commit -m "test: add extended syntax corpus with roslyn oracles"
```

### Task 3: Implement Implicit `it` Placeholder Lambdas

**Files:**
- Modify: `src/CsEval/Binding/Binder.cs`
- Modify: `src/CsEval/Binding/Services/CallBinderService.cs`
- Modify: `src/CsEval/Parsing/IdentifierOccurrenceCollector.cs`
- Test: `tests/CsEval.Test/Runtime/LinqTests.cs`
- Test: `tests/CsEval.Test/Parsing/ParserTests.cs`
- Test: `tests/CsEval.Test/Parity/ExecutionModeParityTests.cs`

**Step 1: Add failing tests for implicit placeholder behavior**

```csharp
Assert.That(engine.Evaluate("numbers.Where(it > 2).ToArray()"), Is.EqualTo(new[] { 3, 4 }));
Assert.That(engine.Evaluate("numbers.Select(it * 10).ToArray()"), Is.EqualTo(new[] { 10, 20, 30, 40 }));
```

**Step 2: Run targeted tests to verify RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~LinqTests|FullyQualifiedName~ExecutionModeParityTests" -v minimal`  
Expected: FAIL on implicit-it tests

**Step 3: Implement bind-time placeholder lowering**

```csharp
// If method parameter expects Func<T,...> and argument is not LambdaExpr,
// and arg expression references placeholder identifier "it" (or "_"),
// rewrite argument to LambdaExpr((it) => <argExpr>) before normal call binding.
```

**Step 4: Re-run tests to verify GREEN**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~LinqTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: PASS for implicit-it cases, no compiled fallback

**Step 5: Commit**

```bash
git add src/CsEval/Binding/Binder.cs src/CsEval/Binding/Services/CallBinderService.cs src/CsEval/Parsing/IdentifierOccurrenceCollector.cs tests/CsEval.Test/Runtime/LinqTests.cs tests/CsEval.Test/Parsing/ParserTests.cs tests/CsEval.Test/Parity/ExecutionModeParityTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/implicit-it-*
git commit -m "feat: support implicit it placeholder lambdas in extended mode"
```

### Task 4: Add Aggregate Built-ins (`sum`, `avg`, `count`, `min`, `max`)

**Files:**
- Create: `src/CsEval/Runtime/Extensions/AggregateBuiltins.cs`
- Modify: `src/CsEval/Runtime/Semantics/IdentifierRuntime.cs`
- Modify: `src/CsEval/Runtime/Extensions/BareMathNames.cs`
- Test: `tests/CsEval.Test/Runtime/LinqTests.cs`
- Test: `tests/CsEval.Test/Parity/ExecutionModeParityTests.cs`

**Step 1: Add failing aggregate built-in tests**

```csharp
Assert.That(engine.Evaluate("sum(numbers)"), Is.EqualTo(10));
Assert.That(engine.Evaluate("count(numbers.Where(it > 2))"), Is.EqualTo(2));
Assert.That(engine.Evaluate("avg(numbers)"), Is.EqualTo(2.5));
```

**Step 2: Run aggregate-focused tests for RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~LinqTests.SumAverage|FullyQualifiedName~LinqTests.Count|FullyQualifiedName~ParityTests" -v minimal`  
Expected: FAIL on built-in names

**Step 3: Implement aggregate resolver with core fallback**

```csharp
public static object? Sum(object? source) => Enumerable.Cast<object?>(AsEnumerable(source)).SumDynamic();
public static object? Avg(object? source) => Enumerable.Cast<object?>(AsEnumerable(source)).AverageDynamic();
public static int Count(object? source) => AsEnumerable(source).Cast<object?>().Count();
```

**Step 4: Re-run targeted tests for GREEN**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~LinqTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Runtime/Extensions/AggregateBuiltins.cs src/CsEval/Runtime/Semantics/IdentifierRuntime.cs src/CsEval/Runtime/Extensions/BareMathNames.cs tests/CsEval.Test/Runtime/LinqTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/aggregate-*
git commit -m "feat: add aggregate built-ins for extended mode"
```

### Task 5: Add Date Arithmetic Sugar

**Files:**
- Create: `src/CsEval/Runtime/Extensions/DateArithmeticSugar.cs`
- Modify: `src/CsEval/Runtime/MemberAccess.cs`
- Modify: `src/CsEval/Runtime/Operators.cs`
- Test: `tests/CsEval.Test/Runtime/MiscTests.cs`
- Test: `tests/CsEval.Test/Parity/ExecutionModeParityTests.cs`

**Step 1: Add failing date sugar tests**

```csharp
Assert.That(engine.Evaluate("now() + 30.days") is DateTime, Is.True);
Assert.That(engine.Evaluate("date1 - date2") is TimeSpan, Is.True);
Assert.That(engine.Evaluate("2.hours + 30.minutes"), Is.EqualTo(TimeSpan.FromMinutes(150)));
```

**Step 2: Run date tests for RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~MiscTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: FAIL on unit-member sugar

**Step 3: Implement numeric-unit member sugar and date helpers**

```csharp
// Numeric literal member sugar:
// 30.days -> TimeSpan.FromDays(30)
// 2.hours -> TimeSpan.FromHours(2)
// plus now(), today() built-ins in Extended resolver.
```

**Step 4: Re-run targeted suites**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~MiscTests|FullyQualifiedName~ParityTests|FullyQualifiedName~ExecutionModeParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Runtime/Extensions/DateArithmeticSugar.cs src/CsEval/Runtime/MemberAccess.cs src/CsEval/Runtime/Operators.cs tests/CsEval.Test/Runtime/MiscTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/date-*
git commit -m "feat: add extended date arithmetic sugar"
```

### Task 6: Implement `let-in` Expression Lowering

**Files:**
- Modify: `src/CsEval/Parsing/ExpressionParser.cs`
- Modify: `src/CsEval/Parsing/TokenLexemes.cs`
- Test: `tests/CsEval.Test/Parsing/ParserTests.cs`
- Test: `tests/CsEval.Test/Runtime/ScopingTests.cs`
- Test: `tests/CsEval.Test/Compliance/StandardModeNegativeTests.cs`

**Step 1: Add failing parser/runtime tests**

```csharp
var expr = Parse("let x = 5 in x * x");
Assert.That(engine.Evaluate("let x = 5 in x * x"), Is.EqualTo(25));
```

**Step 2: Run for RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParserTests|FullyQualifiedName~ScopingTests|FullyQualifiedName~StandardModeNegativeTests" -v minimal`  
Expected: FAIL on `let-in`

**Step 3: Implement desugaring to block + return**

```csharp
// let x = init in body
// =>
// { var x = init; return body; }
```

**Step 4: Re-run tests for GREEN**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParserTests|FullyQualifiedName~ScopingTests|FullyQualifiedName~ParityTests|FullyQualifiedName~ExecutionModeParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Parsing/ExpressionParser.cs src/CsEval/Parsing/TokenLexemes.cs tests/CsEval.Test/Parsing/ParserTests.cs tests/CsEval.Test/Runtime/ScopingTests.cs tests/CsEval.Test/Compliance/StandardModeNegativeTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/let-in-*
git commit -m "feat: add let-in expression sugar in extended mode"
```

### Task 7: Add Scope Functions (`let`, `also`, `apply`, `run`, `with`)

**Files:**
- Create: `src/CsEval/Runtime/Extensions/ScopeFunctionExtensions.cs`
- Modify: `src/CsEval/CsEvalEngine.cs`
- Test: `tests/CsEval.Test/Extensions/PolyglotEdgeCaseTests.cs`
- Test: `tests/CsEval.Test/Runtime/MiscTests.cs`
- TestData: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/scope-*.csx` and `.roslyn.csx`

**Step 1: Add failing scope-function tests**

```csharp
Assert.That(engine.Evaluate("value.let(x => x * x)"), Is.EqualTo(49));
Assert.That(engine.Evaluate("value.also(x => x + 1)"), Is.EqualTo(7)); // returns original
```

**Step 2: Verify RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~PolyglotEdgeCaseTests|FullyQualifiedName~MiscTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: FAIL (unknown methods)

**Step 3: Implement extension methods and register extension type**

```csharp
public static TResult Let<T, TResult>(this T value, Func<T, TResult> f) => f(value);
public static T Also<T>(this T value, Action<T> f) { f(value); return value; }
```

**Step 4: Verify GREEN**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~PolyglotEdgeCaseTests|FullyQualifiedName~MiscTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Runtime/Extensions/ScopeFunctionExtensions.cs src/CsEval/CsEvalEngine.cs tests/CsEval.Test/Extensions/PolyglotEdgeCaseTests.cs tests/CsEval.Test/Runtime/MiscTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/scope-*
git commit -m "feat: add scope function extensions for extended mode"
```

### Task 8: Implement If-as-Expression

**Files:**
- Modify: `src/CsEval/Parsing/ExpressionParser.cs`
- Test: `tests/CsEval.Test/Parsing/ParserTests.cs`
- Test: `tests/CsEval.Test/Runtime/ControlFlowTests.cs`
- Test: `tests/CsEval.Test/Compliance/StandardModeNegativeTests.cs`

**Step 1: Add failing tests**

```csharp
Assert.That(engine.Evaluate("if (x > 0) x else -x"), Is.EqualTo(5));
```

**Step 2: Run RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParserTests|FullyQualifiedName~ControlFlowTests|FullyQualifiedName~StandardModeNegativeTests" -v minimal`  
Expected: FAIL

**Step 3: Parse and lower to existing `ConditionalExpr`**

```csharp
// if (cond) thenExpr else elseExpr
// => new ConditionalExpr(cond, thenExpr, elseExpr)
```

**Step 4: Re-run tests**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ControlFlowTests|FullyQualifiedName~ParityTests|FullyQualifiedName~ExecutionModeParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Parsing/ExpressionParser.cs tests/CsEval.Test/Parsing/ParserTests.cs tests/CsEval.Test/Runtime/ControlFlowTests.cs tests/CsEval.Test/Compliance/StandardModeNegativeTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/if-expression-*
git commit -m "feat: add if-as-expression syntax in extended mode"
```

### Task 9: Implement List Comprehensions

**Files:**
- Create: `src/CsEval/Parsing/Extensions/ComprehensionParser.cs`
- Modify: `src/CsEval/Parsing/PrimaryParser.cs`
- Modify: `src/CsEval/Parsing/Ast.cs` (only if a temporary comprehension AST node is needed)
- Test: `tests/CsEval.Test/Parsing/ParserTests.cs`
- Test: `tests/CsEval.Test/Runtime/CollectionTests.cs`
- TestData: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/comprehension-*.csx` and `.roslyn.csx`

**Step 1: Add failing comprehension tests**

```csharp
Assert.That(engine.Evaluate("[x * x for x in 1..10 if x % 2 == 0]"), Is.EqualTo(new[] { 4, 16, 36, 64, 100 }));
```

**Step 2: Verify RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParserTests|FullyQualifiedName~CollectionTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: FAIL

**Step 3: Lower comprehension to core method chain**

```csharp
// [proj for x in src if pred]
// => src.Where(x => pred).Select(x => proj).ToArray()
```

**Step 4: Verify GREEN**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~CollectionTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Parsing/Extensions/ComprehensionParser.cs src/CsEval/Parsing/PrimaryParser.cs src/CsEval/Parsing/Ast.cs tests/CsEval.Test/Parsing/ParserTests.cs tests/CsEval.Test/Runtime/CollectionTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/comprehension-*
git commit -m "feat: add list comprehension syntax in extended mode"
```

### Task 10: Implement Property Destructuring (Expression Scope)

**Files:**
- Create: `src/CsEval/Parsing/Extensions/DestructuringParser.cs`
- Modify: `src/CsEval/Parsing/ExpressionParser.cs`
- Modify: `src/CsEval/Parsing/StatementParser.cs`
- Test: `tests/CsEval.Test/Parsing/ParserTests.cs`
- Test: `tests/CsEval.Test/Runtime/ScopingTests.cs`
- TestData: `tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/destructure-*.csx` and `.roslyn.csx`

**Step 1: Add failing destructuring tests**

```csharp
Assert.That(engine.Evaluate("let { Name, Age } = person in Name + \"-\" + Age"), Is.EqualTo("Ada-20"));
```

**Step 2: Run RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParserTests|FullyQualifiedName~ScopingTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: FAIL

**Step 3: Lower destructuring to core variable declarations**

```csharp
// let {Name, Age} = person in body
// =>
// { var __tmp = person; var Name = __tmp.Name; var Age = __tmp.Age; return body; }
```

**Step 4: Run GREEN verification**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ScopingTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~ParityTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Parsing/Extensions/DestructuringParser.cs src/CsEval/Parsing/ExpressionParser.cs src/CsEval/Parsing/StatementParser.cs tests/CsEval.Test/Parsing/ParserTests.cs tests/CsEval.Test/Runtime/ScopingTests.cs tests/CsEval.Test/TestData/ValidExpressions/ExtendedSyntax/destructure-*
git commit -m "feat: add property destructuring lowering in extended mode"
```

### Task 11: Implement Expression Tracing (Interpreter + Compiled Parity)

**Files:**
- Create: `src/CsEval/Tracing/EvaluationTraceStep.cs`
- Create: `src/CsEval/Tracing/EvaluationTraceResult.cs`
- Modify: `src/CsEval/CsEvalEngine.cs`
- Modify: `src/CsEval/CsEvalExpression.cs`
- Modify: `src/CsEval/Interpretation/BoundEvaluator.cs`
- Modify: `src/CsEval.Compiled/Compilation/BoundRuntimeMethodCache.cs`
- Modify: `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`
- Test: `tests/CsEval.Test/Runtime/TracingTests.cs`
- Test: `tests/CsEval.Test/Parity/ExecutionModeParityTests.cs`

**Step 1: Add failing tracing API tests**

```csharp
var trace = engine.EvaluateWithTrace("4 * 5 + 2");
Assert.That(trace.Steps.Select(s => s.Display), Is.EqualTo(new[] { "4 * 5 + 2", "20 + 2", "22" }));
```

**Step 2: Run RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~TracingTests" -v minimal`  
Expected: FAIL (API missing)

**Step 3: Implement trace collector and runtime hooks**

```csharp
public sealed record EvaluationTraceStep(string NodeKind, object? Value, string? Display);
public sealed record EvaluationTraceResult(object? Result, IReadOnlyList<EvaluationTraceStep> Steps);
```

**Step 4: Verify trace parity across executors**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~TracingTests|FullyQualifiedName~ExecutionModeParityTests" -v minimal`  
Expected: PASS (same final result and equivalent trace step sequence)

**Step 5: Commit**

```bash
git add src/CsEval/Tracing/EvaluationTraceStep.cs src/CsEval/Tracing/EvaluationTraceResult.cs src/CsEval/CsEvalEngine.cs src/CsEval/CsEvalExpression.cs src/CsEval/Interpretation/BoundEvaluator.cs src/CsEval.Compiled/Compilation/BoundRuntimeMethodCache.cs src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs tests/CsEval.Test/Runtime/TracingTests.cs tests/CsEval.Test/Parity/ExecutionModeParityTests.cs
git commit -m "feat: add expression tracing with interpreter/compiled parity"
```

### Task 12: Harden Standard Mode Rejections for New Sugar

**Files:**
- Modify: `src/CsEval/Parsing/ExpressionParser.cs`
- Modify: `src/CsEval/Parsing/PrimaryParser.cs`
- Modify: `src/CsEval/Parsing/TokenLexemes.cs`
- Test: `tests/CsEval.Test/Compliance/StandardModeNegativeTests.cs`

**Step 1: Add failing Standard-mode rejection tests**

```csharp
Assert.Throws<CsEvalLanguageModeException>(() => standard.Evaluate("if (x > 0) x else -x"));
Assert.Throws<CsEvalLanguageModeException>(() => standard.Evaluate("[x for x in 1..3]"));
Assert.Throws<CsEvalLanguageModeException>(() => standard.Evaluate("let x = 1 in x"));
```

**Step 2: Run RED**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~StandardModeNegativeTests" -v minimal`  
Expected: FAIL until gates are explicit

**Step 3: Add explicit parser gates and `FeatureName` values**

```csharp
throw new CsEvalLanguageModeException("comprehension");
throw new CsEvalLanguageModeException("let-in");
throw new CsEvalLanguageModeException("if-expression");
```

**Step 4: Run GREEN**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~StandardModeNegativeTests" -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add src/CsEval/Parsing/ExpressionParser.cs src/CsEval/Parsing/PrimaryParser.cs src/CsEval/Parsing/TokenLexemes.cs tests/CsEval.Test/Compliance/StandardModeNegativeTests.cs
git commit -m "test: enforce standard-mode rejection for extended syntax"
```

### Task 13: Full Verification and Documentation

**Files:**
- Create: `docs/extended-mode-syntax.md`
- Modify: `docs/diagnostics.md`
- Modify: `benchmarks/CsEval.Benchmarks/BenchmarkScenarioCatalog.Extended.cs` (if benchmark scenarios added)
- Modify: `benchmarks/CsEval.Benchmarks/ExtendedSyntaxParityBenchmarks.cs` (if needed)

**Step 1: Add docs and syntax-to-core mapping table**

```markdown
| Extended syntax | Lowered core form |
|---|---|
| `items.Where(it > 0)` | `items.Where(x => x > 0)` |
| `let x = a in b` | `{ var x = a; return b; }` |
```

**Step 2: Run targeted verification suites**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj --filter "FullyQualifiedName~ParityTests|FullyQualifiedName~ExecutionModeParityTests|FullyQualifiedName~StandardModeNegativeTests|FullyQualifiedName~TracingTests" -v minimal`  
Expected: PASS

**Step 3: Run full test suite**

Run: `dotnet test tests/CsEval.Test/CsEval.Test.csproj -v minimal`  
Expected: PASS

**Step 4: Run extended benchmarks smoke (optional but recommended)**

Run: `dotnet test benchmarks/CsEval.Benchmarks.Tests/CsEval.Benchmarks.Tests.csproj -v minimal`  
Expected: PASS

**Step 5: Commit**

```bash
git add docs/extended-mode-syntax.md docs/diagnostics.md benchmarks/CsEval.Benchmarks/BenchmarkScenarioCatalog.Extended.cs benchmarks/CsEval.Benchmarks/ExtendedSyntaxParityBenchmarks.cs
git commit -m "docs: publish extended syntax and parity mapping"
```

---

## Feature-by-Feature Roslyn Oracle Examples (Required)

1. Implicit `it`:
   - `.csx`: `orders.Where(it.Total > 100).Select(it.Total)`
   - `.roslyn.csx`: `orders.Where(x => x.Total > 100).Select(x => x.Total)`
2. Comprehension:
   - `.csx`: `[x * x for x in 1..10 if x % 2 == 0]`
   - `.roslyn.csx`: `Enumerable.Range(1, 10).Where(x => x % 2 == 0).Select(x => x * x).ToArray()`
3. `let-in`:
   - `.csx`: `let tax = price * 0.1m in price + tax`
   - `.roslyn.csx`: `{ var tax = price * 0.1m; return price + tax; }`
4. Property destructuring:
   - `.csx`: `let {Name, Age} = person in Name + ":" + Age`
   - `.roslyn.csx`: `{ var __p = person; var Name = __p.Name; var Age = __p.Age; return Name + ":" + Age; }`

---

## Custom Test Function Rule

Add custom test helper methods only when at least one applies:

1. No direct Roslyn equivalent exists.
2. Exception normalization is required.
3. Complex host setup repeats across 3+ tests.
4. AST/bound-node shape assertions are the test objective.

Otherwise, use plain paired corpus files (`.csx` + `.roslyn.csx`) and existing parity fixtures.

---

## Execution Order

1. Task 1  
2. Task 2  
3. Task 3  
4. Task 4  
5. Task 5  
6. Task 6  
7. Task 7  
8. Task 8  
9. Task 9  
10. Task 10  
11. Task 11  
12. Task 12  
13. Task 13

