# CsEval Documentation

## What This Is

Authoritative engineering documentation for CsEval — a C# expression evaluator and compiler library. The documentation covers the complete language surface (types, operators, statements, expressions), engine API, security model, diagnostics, and architecture. It follows the structure and rigor of the Roslyn C# language reference and is optimized for consumption by developers, compiler engineers, and LLMs.

## Core Value

Every documented feature is source-verified. If the code doesn't confirm it, it doesn't appear in the docs. Accuracy over completeness.

## Requirements

### Validated

- ✓ CsEval codebase is stable and feature-complete for v1 — existing
- ✓ Codebase mapped with full architecture, stack, conventions, testing, and concerns analysis — existing

### Active

- [ ] Complete language reference documentation (types, operators, statements, expressions)
- [ ] Extended mode documentation fully isolated in docs/extended/
- [ ] Engine API documentation (CsEvalEngine, options, variables, functions, modules, compilation, thread safety, AOT)
- [ ] Security documentation (sandbox, execution limits, common mistakes)
- [ ] Diagnostics documentation (exception hierarchy, error codes)
- [ ] Architecture documentation (pipeline, Mermaid diagrams)
- [ ] Benchmarks documentation (interpreted vs compiled, warm/cold)
- [ ] Explicit unsupported features list
- [ ] LLM compatibility files (llms.txt, llms-full.txt, context7.json) at repo root
- [ ] Astro Starlight compatible frontmatter on all pages
- [ ] All examples executable with // output: comments

### Out of Scope

- README.md — requires collaborative human input, not automatable
- Marketing copy or tutorials — this is engineering reference material
- API reference (xmldoc) generation — separate tooling concern
- Hosting/deployment of docs site — only the content and structure

## Context

CsEval is a C# expression evaluator with two modes: Standard (strict C# spec compliance) and Extended (additional operators, sugar, built-in functions). It has a multi-stage pipeline: Lexer → Parser → Binder → Evaluator/Compiler. The compiled path emits IL via LINQ expression trees. The library targets both .NET 8.0 and .NET Standard 2.0.

Previous documentation attempts were scrapped — too dry, no engineering substance, mixed Extended mode into Standard docs. This restart follows the Roslyn docs model: one concept per page, precise semantics, edge cases documented, source-verified.

The codebase has 10,400+ passing tests across interpreted and compiled modes.

## Constraints

- **Quality**: Roslyn language reference quality bar — specification-grade, not tutorial-grade
- **Accuracy**: Every feature claim must be verified against the implementation before documenting
- **Mode separation**: Extended mode content must NEVER appear in Standard language reference pages
- **LLM optimization**: Deterministic terminology, explicit unsupported lists, minimal ambiguity
- **Starlight**: All .md files must include Astro Starlight frontmatter (title, description)
- **Examples**: Must be executable CsEval expressions with // output: comments, prefer verbatim strings when escaping is needed

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Follow Roslyn docs structure | Proven model for language documentation, familiar to C# developers | — Pending |
| Extended mode in docs/extended/ | Prevents confusion, keeps Standard docs pure C# semantics | — Pending |
| llms.txt at repo root | Convention — agents and tools look for it there by default | — Pending |
| ..= operator for inclusive ranges | C# .. must always be exclusive-end; ..= (Rust-style) for inclusive | ✓ Good |
| One concept per page | Roslyn pattern — enables precise cross-referencing and LLM chunking | — Pending |
| unsupported.md as explicit page | Prevents LLM hallucination of features CsEval doesn't have | — Pending |

---
*Last updated: 2026-03-17 after initialization*
