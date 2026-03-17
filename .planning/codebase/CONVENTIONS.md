# Coding Conventions

**Analysis Date:** 2026-03-17

## Naming Patterns

**Files:**
- Source files use PascalCase matching their primary type: `BoundCallExpr.cs`, `DiagnosticDescriptors.cs`
- Partial class splits use dot notation: `BoundExpressionEmitter.Assignments.cs`, `ExpressionParser.CallArguments.cs`
- Extension/utility classes use descriptive compound names: `ChainedComparisonHelper.cs`, `DateArithmeticSugar.cs`

**Types:**
- Public types: PascalCase
- Internal types: PascalCase with `internal` keyword, frequently `internal sealed`
- AST nodes: `*Expr` suffix for parse tree (`BinaryExpr`, `LiteralExpr`)
- Bound nodes: `Bound*Expr` suffix for semantic tree (`BoundBinaryExpr`, `BoundCallExpr`)
- Exceptions: `CsEval*Exception` suffix hierarchy (`CsEvalException`, `CsEvalSandboxException`, `CsEvalLanguageModeException`)
- Diagnostic codes: `DiagnosticCode` enum with `CS####` members matching Roslyn, `CSEV####` for CsEval-specific

**Functions/Methods:**
- PascalCase for public and internal methods
- `Try*` prefix for methods returning bool with `out` result: `TryParse`, `TryEvaluate`, `TryValidate`
- `Evaluate*`, `Bind*`, `Emit*`, `Resolve*` prefixes for pipeline steps

**Variables/Fields:**
- Private fields: `_camelCase` prefix
- Local variables: `camelCase`
- Constants and readonly statics in `DiagnosticDescriptors`: PascalCase field names (`BadBinaryOps`, `NullMemberAccess`)

**Parameters:**
- `camelCase`

## Code Style

**Formatting:**
- No `.editorconfig` or `.prettierrc` found; style is enforced by convention
- File-scoped namespace declarations used throughout: `namespace CsEval.Binding;`
- `global using` statements in `GlobalUsings.cs` per project

**Linting:**
- `Directory.Build.props` suppresses CA1822 (`NoWarn`) globally
- Individual `.csproj` files also suppress CA1822
- `ImplicitUsings` enabled for test projects; `Nullable` enabled everywhere
- `LangVersion` set to `latest`

## Import Organization

**Order:**
1. System namespaces
2. Third-party (Microsoft.*, System.Collections.Immutable)
3. Internal project namespaces (CsEval.*)

**Global Usings:**
- `src/CsEval/GlobalUsings.cs` declares: `System.Collections`, `System.Reflection`, `System.Text`
- `tests/CsEval.Test/GlobalUsings.cs` declares: `NUnit.Framework`, `System.Collections`, `CsEval.Compiled`

**Path Aliases:**
- None; namespaces follow directory structure

## Error Handling

**Core Rule:** Never throw `CsEvalException` with a raw string message. Always use a `DiagnosticDescriptor` from `DiagnosticDescriptors`.

**Pattern:**
```csharp
// Correct
throw new CsEvalException(DiagnosticDescriptors.NullMemberAccess, "property", name);
throw new CsEvalSandboxException(DiagnosticDescriptors.SandboxAccessBlocked, "Static property", type.Name, name);

// Wrong — never do this
throw new CsEvalException("some raw message");
```

**Null guards:**
```csharp
if (expr is null) throw new ArgumentNullException(nameof(expr));
if (context is null) throw new ArgumentNullException(nameof(context));
```

**Exception Hierarchy:**
- `CsEvalException` — base, accepts `DiagnosticDescriptor` + format args
- `CsEvalDepthException` — nesting depth exceeded
- `CsEvalLanguageModeException` — Extended feature used in Standard mode
- `CsEvalExecutionLimitException` — statement count or timeout exceeded
- `CsEvalSandboxException` — sandbox blocks member/method/property/construction access
- Parser/lexer exceptions also derive from `CsEvalException`

**Control Flow Signals (internal):**
- `ControlFlowSignal` is a sentinel value (not an Exception) used for `return`/`break`/`continue`/`goto` — avoids stack trace cost and prevents user `catch` blocks from intercepting it.

**Diagnostic Code Registration:**
- All error codes live in `src/CsEval/Diagnostics/DiagnosticCode.cs` as `enum DiagnosticCode`
- All message templates live in `src/CsEval/Diagnostics/DiagnosticDescriptors.cs` as `public static readonly DiagnosticDescriptor`
- Every descriptor gets a `/// <summary>` comment with the formatted message
- CS-codes mirror Roslyn integer values; CSEV-codes use `1_000_NNN` offset

## Logging

**Framework:** None — no logging framework is used.

**Patterns:**
- Diagnostic information is surfaced exclusively through `CsEvalException` and its subtypes
- Tracing is opt-in via `EvaluateWithTrace()` which returns `EvaluationTraceResult` containing `EvaluationTraceStep` entries

## Comments

**When to Comment:**
- Only where logic is genuinely non-obvious (per CLAUDE.md: "Do NOT write self-explanatory comments")
- Algorithm-level comments cite ECMA-334 spec sections: `// §12.4.7.3: uint + sbyte → both promoted to long`
- Inline comments explain sentinel objects and non-obvious architectural decisions: `// Handle namespace sentinel: accumulate path segments for FQN type resolution.`
- Section dividers use `// ── Tier 1: Always allowed ──────────────────────────────────────`

**XML Docs (`///`):**
- Used on all public types and public members
- Used on `internal` types where the purpose is non-obvious
- `DiagnosticDescriptor` fields carry `<summary>` showing the formatted message template

## Type Design Patterns

**Immutability:**
- Bound nodes are `internal sealed record` with positional parameters — structurally immutable
- Options are `sealed record` with `init`-only properties: `CsEvalOptions`, `SandboxOptions`
- `with`-expressions used for option mutation: `CsEvalOptions.Default with { CompilationMode = mode }`

**Sealed classes:**
- Internal implementation types are `internal sealed class` to prevent unintended extension
- `CsEvalEngine` itself is `public sealed`

**Partial classes:**
- Used to split large files by concern: `BoundExpressionEmitter` + `BoundExpressionEmitter.Assignments`, `ExpressionParser` + `ExpressionParser.CallArguments`

**Static helper classes:**
- Internal runtime helpers are `internal static class`: `MemberAccess`, `MethodInvoker`, `MethodDispatchCache`, `TypeHelpers`

## Module Design

**Exports:**
- Public API is minimal and surface-tested via `ApiSurfaceTests.cs` — any new public method must appear in the inventory test
- Internal types have `InternalsVisibleTo` or are kept fully private to the assembly

**Namespace Structure:**
- `CsEval` — public engine and options
- `CsEval.Parsing` — lexer, parser, AST
- `CsEval.Binding` — binder, bound nodes, plans
- `CsEval.Binding.Services` — call/member binder services
- `CsEval.Interpretation` — tree-walking evaluator
- `CsEval.Runtime` — reflection runtime, method dispatch, type resolution
- `CsEval.Runtime.Semantics` — assignment, construction, pattern execution
- `CsEval.Runtime.Extensions` — built-in Extended mode functions
- `CsEval.Diagnostics` — error codes and descriptors
- `CsEval.Compilation` — cache and registry for compiled providers
- `CsEval.Tracing` — trace step models

---

*Convention analysis: 2026-03-17*
