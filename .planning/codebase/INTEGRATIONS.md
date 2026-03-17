# External Integrations

**Analysis Date:** 2026-03-17

## APIs & External Services

None. CsEval is a self-contained in-process expression evaluation library. It makes no outbound HTTP calls and integrates with no external SaaS APIs.

## Data Storage

**Databases:** None in production.
- `Microsoft.EntityFrameworkCore.Sqlite` appears only in `tests/CsEval.Test/` as an integration test target for the `ParseAsExpression<TDelegate>` feature (verifying that parsed LINQ expression trees work with EF IQueryable providers).

**File Storage:** Local filesystem only — test data `.csx` files at `tests/CsEval.Test/TestData/` are copied to output directory at build time.

**Caching:** In-process only.
- `src/CsEval/Compilation/ExpressionCache.cs` — bounded `ConcurrentDictionary` with FIFO eviction (default capacity 10,000). Per-engine instance, shared with child engines.

## Authentication & Identity

**Auth Provider:** None.
- The library has a sandboxing system (`SandboxOptions` in `src/CsEval/CsEvalOptions.cs`) that controls which operations expressions may perform, but this is an application-level permission model, not an identity/auth integration.

## Monitoring & Observability

**Error Tracking:** None. No Sentry, Application Insights, or similar SDK.

**Logs:** No logging framework. The library uses structured diagnostics via `DiagnosticDescriptor` / `DiagnosticDescriptors` in `src/CsEval/Diagnostics/` and surfaces errors as `CsEvalException` with diagnostic codes. Tracing is purely in-memory via `src/CsEval/Tracing/EvaluationTraceResult.cs` and `EvaluationTraceStep.cs`.

## CI/CD & Deployment

**Hosting:** NuGet packages (`CsEval`, `CsEval.Compiled`).

**CI Pipeline:** GitHub Actions — `.github/workflows/dotnet.yml`
- Trigger: push/PR to `main` or `master`
- Runner: `ubuntu-latest`
- Steps: checkout → setup .NET 8.0.x → `dotnet restore` → `dotnet build --configuration Release` → `dotnet test --configuration Release`
- No publish/pack step configured in CI yet.

## Environment Configuration

**Required env vars:** None. The library has no runtime environment dependencies.

**Secrets location:** Not applicable — no secrets used.

## Webhooks & Callbacks

**Incoming:** None.

**Outgoing:** None.

## Pluggable Extension Points

These are integration points within the library, not external services:

**`IExpressionCompiler` (`src/CsEval/IExpressionCompiler.cs`):**
- Allows consumers to substitute an alternative LINQ-to-IL compiler backend (e.g., FastExpressionCompiler).
- Default: `DefaultExpressionCompiler` (wraps `System.Linq.Expressions.LambdaExpression.Compile()`).

**`CsEval.Compiled` package (`src/CsEval.Compiled/`):**
- Optional companion package that adds `ParseAsExpression<TDelegate>` for producing typed `Expression<TDelegate>` trees compatible with Entity Framework and other IQueryable providers.

**Roslyn Source Generator (`src/CsEval.Generators/`):**
- Bundled inside the `CsEval` NuGet package as a Roslyn analyzer.
- Generates AOT type-context classes at consumer compile time from `[CsEvalRegistered]` attributes on `CsEvalTypeContext` subclasses.

---

*Integration audit: 2026-03-17*
