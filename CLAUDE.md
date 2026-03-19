# CsEval Project Instructions

## Git Commit Rules

- One commit per phase (not per plan or task) — accumulate all changes, commit once at the very end
- Do NOT include plan numbers, phase numbers, or GSD references in commit messages

## Code Style

- Do NOT write self-explanatory comments. Only add comments where the logic is genuinely non-obvious.
- Never throw `CsEvalException` with just a raw message string. Always use a `DiagnosticDescriptor` from `DiagnosticDescriptors` so that errors have proper codes and structured formatting.
- Do NOT create helper methods, wrappers, or static utilities to work around design constraints. If the design doesn't support what you need, fix the design. No hacks, no patches, no workarounds — always the proper solution.
- Prefer existing Roslyn CS error codes over custom CSEV codes. Only create a CSEV code when there is genuinely no Roslyn equivalent. The goal is seamless transition for developers already familiar with C# diagnostics — don't create more edge cases to check.
- In test expressions and documentation examples, do NOT wrap multi-statement code in `{ }` blocks unless the parser requires statement mode for that construct. Braces are needed for: control flow (`if`, `for`, `while`, `foreach`, `switch`, `try/catch`), `lock`, `using`, empty statements (`;`), `Action`/delegate invocations, and fully-qualified typed variable declarations (`System.X y = ...`). For simple `var` declarations + return, braces are NOT needed — write `"var x = 1; return x + 1;"` not `"{ var x = 1; return x + 1; }"`.
- In test expressions, prefer raw string literals (`"""..."""`) over verbatim strings (`@""`), and prefer verbatim over escaped sequences. Never use `"x == \"hello\""` — write `"""x == "hello" """` instead. Escaped quotes are hard to read and obscure what the expression actually looks like.
- In `.csx` parity test files, write multi-statement expressions on multiple lines for readability. Don't cram everything onto one line.
- Every piece of data should have a single source of truth. Don't store the same information in two places with fallback logic between them. Derive computed properties from the canonical source. Example: `CsEvalException.ErrorCode` derives from `Diagnostics[0].Code`, not from a separate backing field.
- `ControlFlowSignal` is an evaluator-internal concept. Signals propagate through all intermediate constructs (blocks, loops, branches). Unwrapping happens only at function boundaries: the engine entry point (`CsEvalEngine`), lambda invocation (`MethodInvoker.InvokeLambda`), and compiled root (`EmitRoot`). The evaluator itself has no function-boundary awareness.
- C# and the .NET runtime are the source of truth. Do not reimplement what .NET already provides — delegate to it. If .NET has `Enumerable.Sum()`, use it instead of hand-rolling numeric accumulation. CsEval's job is to bridge dynamic evaluation to .NET, not to rewrite .NET.
- Extended-mode features (like `sum()`, `avg()`, `count()`) are syntactic sugar — shortcuts to .NET APIs, not reimplementations. If C# can't do something (like summing a `List<object>` with mixed types), extended mode shouldn't invent that behavior either. Don't support what C# doesn't support.
- Prefer Roslyn `.csx` parity tests over engine-only tests with hardcoded expected values. If the extended-mode CsEval expression has a standard C# equivalent, write a `.roslyn.csx` sibling so the test proves CsEval matches real C#.
- If a parity test reveals that CsEval can't handle valid C# that Roslyn can, that's a gap to fix now — not a reason to dodge the test or simplify the expression. Never work around CsEval's limitations in tests.
