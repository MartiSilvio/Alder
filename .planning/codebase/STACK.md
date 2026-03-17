# Technology Stack

**Analysis Date:** 2026-03-17

## Languages

**Primary:**
- C# (`latest` LangVersion) - All source, library, tests, benchmarks

## Runtime

**Environment:**
- .NET 8.0 (primary target for all runnable projects)
- .NET Standard 2.0 (secondary target for `CsEval` core library and `CsEval.Generators`)

**Package Manager:**
- NuGet via `dotnet` CLI
- Lockfile: not present (no `packages.lock.json`)

## Frameworks

**Core:**
- None — pure BCL library. No web or application framework.

**Testing:**
- NUnit 4.x — test runner for all test projects
- NUnit3TestAdapter + Microsoft.NET.Test.Sdk — VS/CLI test execution

**Build/Dev:**
- BenchmarkDotNet 0.14.0 — micro-benchmark harness (`benchmarks/CsEval.Benchmarks/`)
- Roslyn Source Generators (`Microsoft.CodeAnalysis.CSharp` 4.3.1) — `CsEval.Generators` emits AOT type-context code at compile time

## Key Dependencies

**Critical (production):**
- `System.Collections.Immutable` 8.0.0 — `netstandard2.0` only polyfill; built-in on net8.0
- `System.Threading.Tasks.Extensions` 4.5.4 — `netstandard2.0` async backport
- `Microsoft.Bcl.AsyncInterfaces` 8.0.0 — `netstandard2.0` `IAsyncEnumerable` backport
- `Microsoft.CodeAnalysis.CSharp` 4.3.1 — used inside `CsEval.Generators` (analyzer, private assets)
- `Microsoft.CodeAnalysis.Analyzers` 3.3.4 — source generator rule enforcement (private assets)

**Optional compiled backend (`CsEval.Compiled`):**
- No additional NuGet dependencies; uses BCL `System.Linq.Expressions` for IL compilation via `IExpressionCompiler`

**Testing only:**
- `FastExpressionCompiler` 5.3.0 — alternative `IExpressionCompiler` backend tested in `CsEval.Test`
- `Microsoft.EntityFrameworkCore` 8.0.4 + `Microsoft.EntityFrameworkCore.Sqlite` 8.0.4 — integration tests for `ParseAsExpression<T>` with EF IQueryable
- `Microsoft.CodeAnalysis.CSharp.Scripting` 5.0.0 — used as a competitor baseline in tests; 4.12.0 in benchmarks
- `NCalcSync` 5.11.0 — competitor expression library used in tests and benchmarks
- `Newtonsoft.Json` 13.0.3 — test utilities
- `DynamicExpresso.Core` 2.19.3 — competitor, benchmarks only
- `Flee` 1.2.0 — competitor, benchmarks only

## Configuration

**Environment:**
- No environment variables required. The library is purely in-process.
- Configuration is code-only via `CsEvalOptions` (record with `init` setters):
  - `CompilationMode` (Interpreted / Compiled)
  - `LanguageMode` (Standard / Extended)
  - `SandboxOptions` (Trusted / Safe / Strict factory methods + `AllowedTypes`)
  - `ExecutionConstraints` (max statements, timeout)
  - `IExpressionCompiler` (pluggable delegate compilation backend)

**Build:**
- `Directory.Build.props` — suppresses `CA1822` solution-wide
- `CsEval.sln` — solution root
- Each project sets `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`
- `CsEval` core: `<IsAotCompatible>true</IsAotCompatible>`
- `CsEval.AotMatrix`: `<PublishAot>true</PublishAot>` — smoke-tests Native AOT publishing

## Platform Requirements

**Development:**
- .NET SDK 8.0.x
- Any OS (CI runs ubuntu-latest)

**Production:**
- .NET 8.0 or .NET Standard 2.0 compatible runtime
- Native AOT publishing supported for the core `CsEval` library (verified via `CsEval.AotMatrix`)

---

*Stack analysis: 2026-03-17*
