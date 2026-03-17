# Codebase Structure

**Analysis Date:** 2026-03-17

## Directory Layout

```
CsEval/
├── src/
│   ├── CsEval/                        # Core library (NuGet: CsEval)
│   │   ├── Parsing/                   # Lexer, parser, AST node records
│   │   │   ├── Extensions/            # Parser extension helpers
│   │   │   ├── Ast.cs                 # All Expr record types + IExprVisitor<T>
│   │   │   ├── Lexer.cs
│   │   │   ├── ExpressionParser.cs    # Main entry; Pratt-style precedence climbing
│   │   │   ├── PrimaryParser.cs
│   │   │   ├── PatternParser.cs
│   │   │   ├── StatementParser.cs
│   │   │   ├── QueryParser.cs
│   │   │   ├── Token.cs
│   │   │   ├── AstDepthValidator.cs
│   │   │   ├── AstWalker.cs
│   │   │   ├── IdentifierOccurrenceCollector.cs
│   │   │   └── VariableCollector.cs
│   │   ├── Binding/                   # Semantic analysis; typed BoundExpr tree
│   │   │   ├── BoundNodes/            # All Bound*Expr record types (~65 files)
│   │   │   ├── Plans/                 # BoundCallPlan, BoundIndexPlan, BoundMemberPlan
│   │   │   ├── Services/              # CallBinderService, MemberBinderService
│   │   │   ├── Binder.cs              # Central bind dispatch
│   │   │   ├── BindingContext.cs
│   │   │   ├── BoundExpr.cs           # Abstract base: record BoundExpr(Type StaticType)
│   │   │   └── BindingNotSupportedException.cs
│   │   ├── Interpretation/            # Tree-walking evaluation
│   │   │   └── BoundEvaluator.cs      # Evaluates BoundExpr via switch dispatch
│   │   ├── Compilation/               # Compiled-mode plugin registry + expression cache
│   │   │   ├── CompiledProviderRegistry.cs   # Static ICompiledProvider singleton
│   │   │   └── ExpressionCache.cs            # FIFO-bounded concurrent cache (10k entries)
│   │   ├── Runtime/                   # Context, type resolution, reflection helpers
│   │   │   ├── Collections/           # FixedDictionary<K,V>, FixedSet<T>
│   │   │   ├── Extensions/            # Runtime extension methods
│   │   │   ├── Semantics/             # IdentifierRuntime, AssignmentRuntime,
│   │   │   │                          #   ConstructionRuntime, ExecutionRuntime,
│   │   │   │                          #   NumericPromotionRuntime, PatternRuntime
│   │   │   ├── CsEvalContext.cs       # Scoped variable store (parent/child tree)
│   │   │   ├── CsEvalConfig.cs        # Immutable frozen engine configuration
│   │   │   ├── TypeResolver.cs        # Roslyn-precedence type lookup
│   │   │   ├── TypeMetadataProvider.cs
│   │   │   ├── MethodResolver.cs
│   │   │   ├── MethodDispatchCache.cs
│   │   │   ├── OperatorRegistry.cs
│   │   │   ├── NumericDispatch.cs
│   │   │   ├── ReflectionRuntime.cs
│   │   │   ├── ExtensionMethodResolver.cs
│   │   │   ├── LambdaDelegateFactory.cs
│   │   │   └── LambdaDelegateConverter.cs
│   │   ├── Diagnostics/               # Error codes, descriptors, structured diagnostics
│   │   │   ├── DiagnosticCode.cs      # CS#### enum
│   │   │   ├── DiagnosticDescriptor.cs
│   │   │   └── DiagnosticDescriptors.cs  # Static catalog of all error templates
│   │   ├── Aot/                       # AOT support: built-in type context
│   │   │   ├── CsEvalBuiltInContext.cs   # Pre-registered BCL types via [CsEvalRegistered]
│   │   │   ├── CsEvalTypeContext.cs      # Abstract base for generated type contexts
│   │   │   ├── CsEvalRegisteredAttribute.cs
│   │   │   └── IAotTypeMetadata.cs
│   │   ├── Attributes/                # CsEvalModuleAttribute, CsEvalFunctionAttribute
│   │   ├── Tracing/                   # EvaluationTraceResult, EvaluationTraceStep
│   │   ├── Compatibility/             # netstandard2.0 polyfills (NetStandardPolyfills.cs)
│   │   ├── CsEvalEngine.cs            # Main public entry point
│   │   ├── CsEvalExpression.cs        # Pre-parsed expression handle
│   │   ├── CompiledExpression.cs      # CsEvalCompiledExpression<T> — compiled delegate wrapper
│   │   ├── CsEvalOptions.cs           # CsEvalOptions, SandboxOptions, CompilationMode, LanguageMode
│   │   ├── CsEvalException.cs         # Exception hierarchy + ControlFlowSignal
│   │   ├── CsEvalDiagnostic.cs        # Structured diagnostic record
│   │   ├── ExecutionConstraints.cs    # MaxStatements / MaxTimeout resource limits
│   │   ├── IExpressionCompiler.cs     # Extension point for LINQ compiler backend
│   │   └── GlobalUsings.cs
│   ├── CsEval.Compiled/               # IL compilation backend (separate NuGet package)
│   │   └── Compilation/
│   │       ├── BoundEmission/         # BoundExpressionEmitter partial class files
│   │       ├── BoundExpressionEmitter.cs     # Core emitter: BoundExpr → LINQ Expression tree
│   │       ├── BoundExpressionEmitter.Assignments.cs
│   │       ├── ILExpressionCompiler.cs       # Orchestrator; registers ICompiledProvider
│   │       ├── ExpressionTreeEmitter.cs      # AST-level fallback emitter
│   │       ├── BoundRuntimeMethodCache.cs
│   │       ├── BoundEmitterSupport.cs
│   │       └── CompilerReflectionCache.cs
│   └── CsEval.Generators/             # Roslyn source generator (bundled as analyzer)
│       ├── Emitters/                  # ContextEmitter, TypeMetadataEmitter
│       ├── Model/                     # ContextModel, MemberModel, TypeRegistrationModel
│       └── CsEvalSourceGenerator.cs   # IIncrementalGenerator entry point
├── tests/
│   ├── CsEval.Test/                   # Main test suite (xUnit)
│   │   ├── Binding/                   # Binding-layer unit tests
│   │   ├── Compilation/               # Compiled-mode tests
│   │   ├── Compliance/                # ECMA-334 compliance tests
│   │   ├── Core/                      # Core engine tests
│   │   ├── Extensions/                # Extension method tests
│   │   ├── Integration/               # Engine integration and caching tests
│   │   ├── Linq/                      # LINQ expression tests
│   │   ├── Loops/                     # Loop statement tests
│   │   ├── Operators/                 # Operator tests
│   │   ├── Parity/                    # Roslyn parity tests
│   │   ├── Parsing/                   # Parser tests
│   │   ├── PatternMatching/           # Pattern matching tests
│   │   ├── Performance/               # Performance regression tests
│   │   ├── Runtime/                   # Runtime behavior tests
│   │   ├── Security/                  # Sandbox/reflection-blocking tests
│   │   ├── Stress/                    # Stress and concurrency tests
│   │   ├── Types/                     # Type-system tests
│   │   ├── AOT/                       # AOT path tests
│   │   └── TestData/                  # Data-driven test files (.txt expression pairs)
│   │       ├── ValidExpressions/      # ~50 categories of valid expression test cases
│   │       └── InvalidExpressions/    # ~15 categories of error case test cases
│   ├── CsEval.Generators.Tests/       # Source generator tests
│   └── CsEval.AotMatrix/              # AOT compatibility matrix tests
├── benchmarks/
│   ├── CsEval.Benchmarks/             # BenchmarkDotNet benchmarks
│   └── CsEval.Benchmarks.Tests/       # Benchmark smoke tests
├── docs/                              # VitePress documentation site
│   ├── getting-started/
│   ├── guide/
│   ├── reference/
│   ├── advanced/
│   └── security/
├── scripts/                           # Build/release scripts
├── CsEval.sln                         # Solution file
└── Directory.Build.props              # Shared MSBuild properties
```

## Directory Purposes

**`src/CsEval/Parsing/`:**
- Purpose: Everything needed to turn a string into an `Expr` AST
- Contains: Lexer, all parser classes (split by concern: primary, patterns, statements, query expressions), all AST node record definitions, AST utilities (depth validator, walker, variable collector)
- Key files: `Ast.cs` (all `Expr` types and `IExprVisitor<T>`), `ExpressionParser.cs` (main entry), `Lexer.cs`

**`src/CsEval/Binding/BoundNodes/`:**
- Purpose: One file per bound node type — each is an `internal record` extending `BoundExpr`
- Contains: ~65 typed node records, e.g. `BoundBinaryExpr.cs`, `BoundCallExpr.cs`, `BoundMemberAccessExpr.cs`

**`src/CsEval/Runtime/Semantics/`:**
- Purpose: Runtime behavior helpers called by `BoundEvaluator` and the IL emitter for complex operations
- Contains: `IdentifierRuntime` (variable lookup), `AssignmentRuntime`, `ConstructionRuntime` (sandbox-checked `new`), `ExecutionRuntime` (constraint checks), `NumericPromotionRuntime`, `PatternRuntime`

**`src/CsEval/Runtime/Collections/`:**
- Purpose: Frozen, read-optimized collection wrappers for hot lookup paths
- Contains: `FixedDictionary<K,V>` (wraps `FrozenDictionary`), `FixedSet<T>` (wraps `FrozenSet`)

**`tests/CsEval.Test/TestData/`:**
- Purpose: Data-driven test cases stored as plain-text files; each file contains expression/expected-result pairs
- Generated: No — hand-authored test data
- Committed: Yes

**`src/CsEval.Generators/`:**
- Purpose: Roslyn source generator that emits `CsEvalTypeContext` subclasses from `[CsEvalRegistered]` attributes
- Generated: DLL output bundled into `CsEval` NuGet package at `analyzers/dotnet/cs`
- Committed: Yes (source)

## Key File Locations

**Entry Points:**
- `src/CsEval/CsEvalEngine.cs`: Main user-facing API — `Evaluate()`, `Parse()`, `TryValidate()`, `CreateChild()`, all `Register*` methods
- `src/CsEval/CsEvalOptions.cs`: All configuration types — `CsEvalOptions`, `SandboxOptions`, `CompilationMode`, `LanguageMode`, `ExecutionConstraints`
- `src/CsEval/CsEvalException.cs`: Full exception hierarchy and `ControlFlowSignal`

**Configuration:**
- `CsEval.sln`: Solution with all project references
- `Directory.Build.props`: Shared MSBuild properties applied to all projects
- `src/CsEval/CsEval.csproj`: Core package; targets `net8.0;netstandard2.0`; bundles the source generator as an analyzer

**Core Logic:**
- `src/CsEval/Binding/Binder.cs`: Central semantic analysis dispatch — pattern-match switch over all `Expr` types
- `src/CsEval/Interpretation/BoundEvaluator.cs`: Tree-walking execution — pattern-match switch over all `BoundExpr` types
- `src/CsEval.Compiled/Compilation/ILExpressionCompiler.cs`: IL compilation orchestrator
- `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`: `BoundExpr` → `System.Linq.Expressions` tree emitter
- `src/CsEval/Runtime/CsEvalContext.cs`: Scoped variable store; parent/child context tree
- `src/CsEval/Runtime/CsEvalConfig.cs`: Immutable frozen engine configuration
- `src/CsEval/Diagnostics/DiagnosticDescriptors.cs`: Catalog of all structured error descriptors

**Testing:**
- `tests/CsEval.Test/`: Main test project; xUnit
- `tests/CsEval.Test/TestData/`: Data-driven expression test cases

## Naming Conventions

**Files:**
- Production C# files: PascalCase matching the primary type (`CsEvalEngine.cs`, `BoundBinaryExpr.cs`)
- Partial class files: `ClassName.PartialPurpose.cs` (e.g., `BoundExpressionEmitter.Assignments.cs`, `ExpressionParser.CallArguments.cs`)
- Test files: `FeatureNameTests.cs` (e.g., `SandboxModeTests.cs`, `ExpressionCachingTests.cs`)
- Test data files: category name without extension (e.g., `Arithmetic`, `Cast`)

**Directories:**
- PascalCase matching the layer or concern (`Parsing`, `Binding`, `BoundNodes`, `Runtime`, `Semantics`)
- Test subdirectories mirror source layer names (`Binding/`, `Parsing/`, `Runtime/`, `Compilation/`)

**Types:**
- Public API types prefixed with `CsEval` (`CsEvalEngine`, `CsEvalOptions`, `CsEvalException`)
- AST node types suffixed with `Expr` (`LiteralExpr`, `BinaryExpr`)
- Bound node types prefixed with `Bound` and suffixed with `Expr` (`BoundBinaryExpr`, `BoundCallExpr`)
- Diagnostic descriptors: static readonly fields on `DiagnosticDescriptors`, named after the error concept (`BadBinaryOps`, `NameNotInContext`)

## Where to Add New Code

**New AST node type:**
- Add `record YourExpr(...) : Expr` to `src/CsEval/Parsing/Ast.cs`
- Add `T VisitYour(YourExpr expr)` to `IExprVisitor<T>` in `src/CsEval/Parsing/Ast.cs`
- Add parser logic in the appropriate parser class under `src/CsEval/Parsing/`
- Add binding case in `src/CsEval/Binding/Binder.cs`
- Add `BoundYourExpr.cs` to `src/CsEval/Binding/BoundNodes/`
- Add evaluation case in `src/CsEval/Interpretation/BoundEvaluator.cs`
- Add IL emission case in `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`

**New diagnostic error:**
- Add a `DiagnosticCode` enum value to `src/CsEval/Diagnostics/DiagnosticCode.cs`
- Add a static `DiagnosticDescriptor` field to `src/CsEval/Diagnostics/DiagnosticDescriptors.cs`
- Use `new CsEvalException(DiagnosticDescriptors.YourDescriptor, ...)` at throw sites

**New sandbox permission:**
- Add a property to `SandboxOptions` in `src/CsEval/CsEvalOptions.cs`
- Enforce it in the relevant runtime helper under `src/CsEval/Runtime/Semantics/`
- Add `CsEvalSandboxException` throw using a new descriptor from `DiagnosticDescriptors`

**New runtime helper (complex operation):**
- Add a static class to `src/CsEval/Runtime/Semantics/`
- Call from `BoundEvaluator` (interpretation) and from the appropriate emitter partial in `src/CsEval.Compiled/Compilation/BoundEmission/`

**New test:**
- Unit/integration tests: `tests/CsEval.Test/<Category>/YourFeatureTests.cs`
- Data-driven expression tests: add a directory under `tests/CsEval.Test/TestData/ValidExpressions/` or `InvalidExpressions/`

**New public option:**
- Add a property to `CsEvalOptions` in `src/CsEval/CsEvalOptions.cs`
- Thread through to `CsEvalConfig` if it affects frozen configuration; to `BoundEvaluator`/emitter if it affects execution

## Special Directories

**`.planning/codebase/`:**
- Purpose: GSD codebase analysis documents used by planning and execution agents
- Generated: By GSD map-codebase command
- Committed: Yes

**`.tmp/`:**
- Purpose: Temporary build artifacts (size test scratch files)
- Generated: Yes (tooling)
- Committed: No (in .gitignore)

**`benchmarks/CsEval.Benchmarks/BenchmarkDotNet.Artifacts/`:**
- Purpose: BenchmarkDotNet output artifacts
- Generated: Yes
- Committed: No

**`tests/CsEval.Test/TestResults/`:**
- Purpose: Test run output from `dotnet test`
- Generated: Yes
- Committed: No

---

*Structure analysis: 2026-03-17*
