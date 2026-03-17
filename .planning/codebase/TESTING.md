# Testing Patterns

**Analysis Date:** 2026-03-17

## Test Framework

**Runner:**
- NUnit 4.x (`NUnit` 4.0.1 in `CsEval.Test`, 4.3.2 in `CsEval.Generators.Tests`)
- Config: no `nunit.config`; driven by `.csproj` and `NUnit3TestAdapter`

**Assertion Library:**
- NUnit constraint model (`Assert.That(result, Is.EqualTo(...))`)

**Run Commands:**
```bash
dotnet test tests/CsEval.Test                   # Run main test suite
dotnet test tests/CsEval.Generators.Tests       # Run source generator tests
dotnet test                                     # Run all tests from solution root
```

## Test File Organization

**Location:**
- Tests live under `tests/` at solution root, separate from `src/`
- Main test project: `tests/CsEval.Test/`
- Generator test project: `tests/CsEval.Generators.Tests/`
- AOT smoke test: `tests/CsEval.AotMatrix/` (console app, not NUnit)

**Naming:**
- Files named `{FeatureArea}Tests.cs`
- Multiple `[TestFixture]` classes per file are common when topics are closely related (e.g., `EngineTests.cs` contains `BasicEvaluationTests`, `BuiltInProxyTests`, `CustomRegistrationTests`)

**Structure:**
```
tests/CsEval.Test/
├── CompiledProviderBootstrap.cs   # OneTimeSetUp: registers compiled provider
├── TestHelpers.cs                 # Static parity helpers, Roslyn scripting
├── TestModels.cs                  # Shared POCOs (TestPerson, TestAddress)
├── GlobalUsings.cs                # global using NUnit.Framework, CsEval.Compiled
├── Core/                          # Engine API, diagnostics, surface tests
├── Runtime/                       # Evaluator behavior, memory, overloads
├── Binding/                       # Binder unit tests, parity fixture
├── Compilation/                   # IL compiler, expression trees, compiled paths
├── Parsing/                       # Lexer, parser, token tests
├── Types/                         # Numeric, string, tuple, conversion tests
├── Operators/                     # Cast, logical, nameof, throw expression
├── PatternMatching/               # switch expression, is-pattern
├── Linq/                          # LINQ extension method tests
├── Loops/                         # while, for, do-while
├── Extensions/                    # Extended-mode syntax (spread, merge, polyglot)
├── Security/                      # Sandbox, reflection blocking, cache bounds
├── Integration/                   # EF Core, attribute registration, caching
├── Compliance/                    # C#12 feature inventory, ECMA section parity
├── AOT/                           # Source generator context integration
├── Parity/                        # Interpreted vs. Compiled execution parity
├── Stress/                        # Concurrency, fuzz, pathological inputs
├── Performance/                   # Micro-benchmark smoke tests
└── TestData/                      # .csx expression files (CopyToOutputDirectory)
```

## Test Structure

**Suite Organization:**
```csharp
// Parameterized over CompilationMode — most suites run twice (interpreted + compiled)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ValidationTests(CompilationMode mode)
{
    [Test]
    public void TryParse_ValidExpression_ReturnsTrue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var success = engine.TryParse("1 + 2", out var result, out var error);
        Assert.That(success, Is.True);
        Assert.That(result, Is.Not.Null);
        Assert.That(error, Is.Null);
    }
}
```

**Patterns:**
- `[SetUp]` creates the engine under test; used when a single engine is shared across many tests in the same fixture
- `[OneTimeSetUp]` in `CompiledProviderBootstrap` registers the compiled provider once for the entire assembly
- `[TearDown]` disposes resources (e.g., EF Core DbContext in `EfCoreExpressionIntegrationTests`)
- Primary assertion style: `Assert.That(result, Is.EqualTo(expected))`
- Exception assertions: `Assert.Throws<CsEvalException>(() => engine.Evaluate(...))`
- Diagnostic code assertions include both `.ErrorCode` (enum) and `.FormattedCode` (string) and absence of `{0}` placeholders

## Mocking

**Framework:** None — no mocking library is used.

**Patterns:**
- Tests use real `CsEvalEngine` instances with small, purpose-built inputs
- Custom modules and proxies are implemented as private inner classes inside the test fixture:
```csharp
private class GreetingProxy
{
    public string Greet(string name) => $"Hello, {name}!";
}
// ...
engine.RegisterModule("Custom", instance: new GreetingProxy());
```
- Competitor engines (Roslyn scripting, NCalc) are injected for parity checks — see `TestHelpers.EvaluateCSharpAsync`

**What to Mock:**
- Nothing is mocked — all tests run against the real evaluation pipeline

**What NOT to Mock:**
- The engine itself; tests verify actual evaluation behavior

## Fixtures and Factories

**Test Data:**
```csharp
// Shared scenario data for parity tests
public static IEnumerable<TestCaseData> StandardScenarios()
{
    yield return CreateScenario(
        "Standard/MathMix",
        "Math.Abs(x - y) + Math.Max(y, z)",
        ("x", -5), ("y", 2), ("z", 9));
}
```

**Location:**
- `tests/CsEval.Test/Binding/BinderParityFixture.cs` — `TestCaseData` source for parity suites
- `tests/CsEval.Test/TestModels.cs` — shared POCO types (`TestPerson`, `TestAddress`)
- `tests/CsEval.Test/TestData/*.csx` — expression files loaded at runtime via `TestHelpers.LoadTestExpression`

**Helper utilities in `TestHelpers.cs`:**
- `RunCSharpParityTestAsync` — evaluates expression in both CsEval and Roslyn, asserts value/type match
- `EvaluateCSharpAsync` — evaluates via Roslyn scripting for parity baselines
- `CreateItem` — creates `ExpandoObject` dict for object-expression tests

## Coverage

**Requirements:** No minimum coverage enforced.

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Types

**Unit Tests:**
- Lexer, parser, binder, and evaluator components tested in isolation per subdirectory
- `DiagnosticCodeTests` verifies every error code throw site end-to-end through the public API

**Integration Tests:**
- `Integration/EfCoreExpressionIntegrationTests.cs` — runs CsEval expressions as EF Core LINQ predicates against an in-memory SQLite DB
- `Integration/AttributeRegistrationTests.cs` — tests `[CsEvalRegistered]` attribute scanning
- `Integration/ExpressionCachingTests.cs` — verifies expression cache behavior

**Parity Tests:**
- `Parity/ExecutionModeParityTests.cs` — asserts Interpreted and Compiled modes produce identical results for every scenario in `BinderParityFixture`
- `ConformanceAuditTests.cs`, `ConformanceAudit2Tests.cs`, `ConformanceAudit3Tests.cs` — adversarial ECMA-334 compliance, each test cites a spec section

**Stress Tests:**
- `Stress/ConcurrencyHammerTests.cs` — parallel evaluation hammering
- `Stress/EvaluationChaosTests.cs` — random/adversarial inputs
- `Stress/ParsingFuzzTests.cs` — fuzz parser with random strings
- `Stress/ParsingPathologicalTests.cs` — deeply nested, very long expressions

**Security Tests:**
- `Security/SandboxModeTests.cs` — parameterized over `CompilationMode`; verifies Trusted/Safe/Custom sandbox behavior
- `Security/ReflectionBlockingTests.cs` — ensures reflection type leakage is blocked in all modes
- `Security/CacheBoundsTests.cs` — FIFO eviction, cache size limits

**Memory Tests:**
- `Runtime/MemoryLeakTests.cs` — tagged `[Category("Memory")]`, uses `[Retry(3)]`
- Verifies `WeakReference` GC collection after `Dispose()` and plateau behavior after 12k unique evaluations

**Generator Tests:**
- `tests/CsEval.Generators.Tests/` — uses Roslyn `CSharpGeneratorDriver` to run the source generator against synthetic C# source and assert on generated output diagnostics and syntax trees

## Common Patterns

**Parameterized CompilationMode:**
```csharp
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class MyTests(CompilationMode mode)
{
    [Test]
    public void SomeTest()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // ...
    }
}
```

**Exception with Diagnostic Code Assertions:**
```csharp
var ex = Assert.Throws<CsEvalException>(() => _engine.Evaluate("\"hello\" - 5"));
Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
Assert.That(ex.FormattedCode, Is.EqualTo("CS0019"));
Assert.That(ex.Message, Does.Contain("-"));
Assert.That(ex.Message, Does.Not.Contain("{0}")); // No unfilled placeholders
```

**Sandbox Exception Assertions:**
```csharp
var ex = Assert.Throws<CsEvalSandboxException>(() => engine.Evaluate("text.ToUpper()"));
Assert.That(ex!.Message, Does.Contain("sandbox"));
```

**Roslyn Parity (async):**
```csharp
await TestHelpers.RunCSharpParityTestAsync("1 + 2.0", 3.0, mode);
```

**TestCaseSource Data:**
```csharp
[TestCaseSource(typeof(BinderParityFixture), nameof(BinderParityFixture.StandardScenarios))]
public void StandardMode_InterpretedAndCompiled_ShouldMatch(ExecutionParityScenario scenario)
{ ... }
```

**[TestCase] for inline data:**
```csharp
[TestCase("1 + 1L", typeof(long), TestName = "NumericPromotion_IntPlusLong_IsLong")]
[TestCase("1L + 1", typeof(long), TestName = "NumericPromotion_LongPlusInt_IsLong")]
public void NumericPromotion_ResultType(string expr, Type expectedType) { ... }
```

**StressTestBase for randomized tests:**
```csharp
// Inherit StressTestBase for fixed-seed Random and pre-configured engine
public class MyStressTests(CompilationMode mode) : StressTestBase(mode)
{
    [Test]
    public void SomeStressTest()
    {
        var expr = GenerateDeeplyNestedExpression(depth: 100);
        // ...
    }
}
```

---

*Testing analysis: 2026-03-17*
