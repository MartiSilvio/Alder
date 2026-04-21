# Alder

C# runtime expression engine for .NET. ECMA-334 semantics, full compiler pipeline, two execution backends (interpreter + IL compiler), security sandbox, AOT source generators for NativeAOT and IL2CPP.

## Instruction Precedence

- `CLAUDE.md` is mandatory and authoritative for this repository.
- `AGENTS.md` complements `CLAUDE.md` and must never be interpreted in ways that conflict with it.
- For any ambiguity or conflict, follow `CLAUDE.md`.
- This applies to all agents and workflows, including code simplification/refactoring passes.
- The `code-simplifier` workflow must preserve behavior and enforce `CLAUDE.md` standards while simplifying code.

## Code-Simplifier Non-Negotiables

- The simplification pass is not complete until touched code has no obvious dead scaffolding, dead branches, or unused adapter paths.
- If simplification introduces a new abstraction (helper, interface, pipeline, option type), it must either be used by at least one concrete path now or be removed in the same pass.
- Always run a dead-code verification sweep on touched areas (`rg` for stale symbols and old path names) before concluding.
- Always run targeted tests for touched behavior after simplification.
- Never report “simplified” while leaving known unused transitional code behind.

## Tech Stack

- .NET 8+ / C# 12, nullable reference types enabled
- Projects: `Alder` (core), `Alder.Compiled` (IL compiler), `Alder.Generators` (incremental source generator)
- Zero external dependencies

## Project Structure

- `src/Alder/` — Core: lexer, parser, binder, interpreter, security, runtime
- `src/Alder.Compiled/` — IL compiler backend (LINQ expression tree emission)
- `src/Alder.Generators/` — AOT source generator (emits `TypedDispatch` implementations)
- `tests/Alder.Test/` — Tests
- `docs/` — Documentation (Markdown with Astro/Starlight frontmatter)

## Build & Test

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~SecurityDocTests"   # run specific test class
dotnet test --filter "DisplayName~LangRef_Literal"           # run tests matching a pattern
```

## Testing Conventions

- Parity tests use `.roslyn.csx` sibling files that run under Roslyn to verify Alder matches real C#. Only create these when there is genuine uncertainty about whether Alder matches C# behavior.
- Doc code samples are tested via `<!-- test: TestName -->` markers — each maps to a test method.
- Multi-statement test expressions: use multiple lines for readability, don't cram onto one line.
- Prefer raw string literals (`"""..."""`) over escaped quotes in test expressions.

## Code Style

```csharp
// GOOD: Use DiagnosticDescriptors for errors
throw DiagnosticDescriptors.CS0029_CannotImplicitlyConvert(sourceType, targetType).ToException(span);

// BAD: Raw message strings
throw new AlderException("Cannot convert type");

// GOOD: Canonical operator symbols
var symbol = TokenLexemes.GetCanonical(TokenType.Plus);

// BAD: Magic strings
var symbol = "+";
```

- `Try*` methods return `false`/`null` on failure — never throw for expected failures
- No exception-driven control flow — design checks to return results, not throw-and-catch
- No wrapper methods that delegate to a single public method — call the canonical method directly
- `CancellationToken` is always the last parameter
- Prefer Roslyn CS error codes over custom ALDR codes — only create ALDR codes when no Roslyn equivalent exists
- No self-explanatory comments, no section divider comments (`// ── Section ──`)
- `#region` only for 50+ line blocks

## Git Workflow

- One commit per logical change
- Do not include plan numbers, phase numbers, or internal tool references in commit messages
- Commit messages should describe the "why", not enumerate files changed

## Boundaries

**Never modify:**

- Generated files in `obj/` or `bin/`
- `.env` files or anything containing credentials

**Always verify:**

- Every runtime change must work with both execution backends (interpreter and compiler)
- Every runtime change must account for the AOT source generator path (`Alder.Generators`)
- If you change method dispatch, verify the generated `TryInvoke`/`TryInvokeStatic` code still integrates

**Design rules:**

- The ECMA-334 spec is the authority for language semantics — read the spec, don't go from memory
- C# and .NET are the source of truth — delegate to .NET, don't reimplement what it already provides
- Extended-mode features are syntactic sugar over .NET APIs, not reimplementations
- The binder produces **resolved** nodes (exact member selected at bind time) when types are known, and **dynamic** nodes (deferred to runtime) when types are `object`
- `ControlFlowSignal` propagates through all constructs; unwrapping happens only at function boundaries
