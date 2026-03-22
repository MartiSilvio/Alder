# Alder Project Instructions

## Quality Standard

Alder targets Roslyn-grade engineering. Every subsystem should be designed as if it belongs in a production compiler toolchain. This means:

- **Algorithms must match the spec.** Don't approximate with heuristics what the C# specification defines precisely. Overload resolution uses ECMA-334 §12.6.4 pairwise elimination — not numeric scoring. Type inference follows the spec's constraint-solving rules — not "fill unknowns with `object`." If the spec has an algorithm, implement that algorithm.
- **`Try` methods don't throw.** If a method is named `Try*`, it returns `false`/`null` on failure. Never use exceptions for control flow. If you find a `Try` method that throws on an expected failure path, that's a bug — fix the method, don't catch the exception.
- **No exception-driven control flow.** `catch` blocks that silently swallow exceptions to mean "this path didn't work, try the next one" are hacks. Design the check so it returns a result instead of throwing.
- **Source generators use structural indentation.** Use `SourceWriter` with `Block()` scopes — never count spaces manually or pass `int indent` parameters. The generated code's structure should be visible in the emitter's structure.
- **AOT dispatch must be type-safe.** Generated dispatch code uses `is` type checks for same-arity overloads — never blind casts that rely on exception fallback. If the AOT path can't handle an argument shape (named args, nulls, out markers), it must return `false` and let the reflection path handle it.

## Git Commit Rules

- One commit per phase (not per plan or task) — accumulate all changes, commit once at the very end
- Do NOT include plan numbers, phase numbers, or GSD references in commit messages

## Code Style

- Do NOT write self-explanatory comments. Only add comments where the logic is genuinely non-obvious.
- Never throw `AlderException` with just a raw message string. Always use a `DiagnosticDescriptor` from `DiagnosticDescriptors` so that errors have proper codes and structured formatting.
- Do NOT create helper methods, wrappers, or static utilities to work around design constraints. If the design doesn't support what you need, fix the design. No hacks, no patches, no workarounds — always the proper solution.
- Prefer existing Roslyn CS error codes over custom ALDR codes. Only create an ALDR code when there is genuinely no Roslyn equivalent. The goal is seamless transition for developers already familiar with C# diagnostics — don't create more edge cases to check.
- In test expressions and documentation examples, do NOT wrap multi-statement code in `{ }` blocks unless the parser requires statement mode for that construct. Braces are needed for: control flow (`if`, `for`, `while`, `foreach`, `switch`, `try/catch`), `lock`, `using`, empty statements (`;`), `Action`/delegate invocations, and fully-qualified typed variable declarations (`System.X y = ...`). For simple `var` declarations + return, braces are NOT needed — write `"var x = 1; return x + 1;"` not `"{ var x = 1; return x + 1; }"`.
- In test expressions, prefer raw string literals (`"""..."""`) over verbatim strings (`@""`), and prefer verbatim over escaped sequences. Never use `"x == \"hello\""` — write `"""x == "hello" """` instead. Escaped quotes are hard to read and obscure what the expression actually looks like.
- In `.csx` parity test files, write multi-statement expressions on multiple lines for readability. Don't cram everything onto one line.
- Every piece of data should have a single source of truth. Don't store the same information in two places with fallback logic between them. Derive computed properties from the canonical source. Example: `AlderException.ErrorCode` derives from `Diagnostics[0].Code`, not from a separate backing field.
- `ControlFlowSignal` is an evaluator-internal concept. Signals propagate through all intermediate constructs (blocks, loops, branches). Unwrapping happens only at function boundaries: the engine entry point (`AlderEngine`), lambda invocation (`MethodInvoker.InvokeLambda`), and compiled root (`EmitRoot`). The evaluator itself has no function-boundary awareness.
- C# and the .NET runtime are the source of truth. Do not reimplement what .NET already provides — delegate to it. If .NET has `Enumerable.Sum()`, use it instead of hand-rolling numeric accumulation. Alder's job is to bridge dynamic evaluation to .NET, not to rewrite .NET.
- Extended-mode features (like `sum()`, `avg()`, `count()`) are syntactic sugar — shortcuts to .NET APIs, not reimplementations. If C# can't do something (like summing a `List<object>` with mixed types), extended mode shouldn't invent that behavior either. Don't support what C# doesn't support.
- `.roslyn.csx` parity siblings are ONLY needed when Alder behavior diverges or might diverge from Roslyn — i.e., when there is genuine uncertainty about whether Alder matches real C#. Do NOT create `.roslyn.csx` files for straightforward behavior that obviously matches C# (like basic arithmetic, simple string operations, standard control flow). A regular engine test with a hardcoded expected value is fine when the expected result is unambiguous. Reserve parity tests for edge cases, tricky semantics, or areas where Alder's implementation could plausibly differ from the runtime.
- If a parity test reveals that Alder can't handle valid C# that Roslyn can, that's a gap to fix now — not a reason to dodge the test or simplify the expression. Never work around Alder's limitations in tests.
