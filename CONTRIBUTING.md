# Contributing to Alder

Alder is an embeddable C# expression runtime with compiler-style parsing and semantic binding. Changes need to preserve C# semantics, keep the interpreted and compiled backends aligned, and avoid adding runtime dependencies.

## Start Here

1. Build first:

   ```bash
   dotnet build
   ```

2. Run the relevant tests before changing behavior, then run them again after the change.

3. Keep changes focused. Do not mix runtime behavior, documentation rewrites, and broad cleanup in one patch unless they are part of the same fix.

## Test Strategy

Use the smallest test shape that proves the behavior without duplicating coverage.

| Change | Test location |
| --- | --- |
| C# language semantics, operators, conversions, statements, syntax, or runtime value parity | `.csx` corpus files under `tests/Alder.Test/TestData/` |
| Alder syntax that lowers to different valid C# for the reference result | A `.csx` corpus file plus a `.roslyn.csx` sibling |
| Known unsupported syntax or a tracked limitation | `.ignore.csx` only when the limitation is intentional and documented by the surrounding test area |
| Parser or lexer shape | `tests/Alder.Test/Parsing/` plus corpus coverage when the parsed behavior is executable |
| Binder services, overload resolution services, or dispatch internals | `tests/Alder.Test/Binding/` or `tests/Alder.Test/Runtime/` |
| Engine API behavior, caching, variables, child engines, lifecycle, options, or diagnostics surface | Focused fixture tests under `tests/Alder.Test/Core/`, `Integration/`, `Runtime/`, or `Verification/` |
| Compiled backend behavior | `tests/Alder.Compiled.Test/Compilation/` and parity coverage in `tests/Alder.Test/` where the behavior is shared |
| Dynamic LINQ provider behavior | `tests/Alder.Compiled.Test/Compilation/DynamicLinq/` or `DynamicLinqEfCore/` |
| AOT or generated dispatch behavior | `tests/Alder.Test/AOT/`, `tests/Alder.Test/Verification/`, `scripts/aot-publish-check.sh --strict`, and targeted `scripts/aot-matrix.sh` runs when broad corpus coverage is needed |
| Documentation examples | `tests/*/Docs/` with a matching `<!-- test: TestName -->` marker in the Markdown |

The `.csx` corpus is the default place for language behavior. Do not add a hand-written fixture just to assert that an expression returns the same value under both backends. The parity runner already does that for corpus files in interpreted and compiled modes and compares the result against Roslyn.

Use a backend-parameterized fixture when the behavior is not just a language expression: engine API calls, option wiring, cache state, compiled delegate availability, diagnostics metadata, security configuration, generated dispatch integration, or provider translation.

## Corpus Tests

The parity runner discovers:

- `tests/Alder.Test/TestData/ValidExpressions/**/*.csx`
- `tests/Alder.Test/TestData/InvalidExpressions/**/*.csx`
- `tests/Alder.Test/TestData/ValidAsyncExpressions/**/*.csx`
- `tests/Alder.Test/TestData/InvalidAsyncExpressions/**/*.csx`

For ordinary C# semantics, add one `.csx` file. The same source is evaluated by Alder and Roslyn.

Add a `.roslyn.csx` sibling only when Alder source is intentionally not valid C# or when the reference C# source must be written differently to express the same result. Do not add `.roslyn.csx` files by habit.

Use `.ignore.csx` for known limitations, not for hiding regressions. If a case should work, leave it as an active failing test while fixing the engine.

Test expressions should be readable:

- Use explicit `return`.
- Use multiple lines for multi-statement expressions.
- Prefer raw string literals over escaped quotes.
- Do not wrap the whole expression in an extra outer block.

## Fixture Tests

Fixture tests are for behavior that cannot be represented as a corpus expression or where the assertion is about the engine around the expression.

Good fixture targets include:

- `AlderEngine` API behavior.
- Parse once, evaluate many times.
- Variable scoping and per-call variable behavior.
- Diagnostic spans, codes, and exception shape.
- Security policy decisions.
- Compiled delegate generation, fallback counts, and provider translation.
- AOT generated context registration and dispatch paths.

If the assertion is only "this C# expression evaluates to this value", add a corpus test instead.

## Documentation Tests

Doc samples are executable contracts. A Markdown marker like:

```markdown
<!-- test: TypeRegistration_AddNamespace_ResolvesUnqualifiedType -->
```

must point to a real test that covers the exact sample or claim nearby. Do not add aspirational markers, and do not point a marker at a loosely related test.

Documentation tests should prove documented Alder behavior. They are not a replacement for corpus parity tests.

## Test Projects

`tests/Alder.Test` is the shared runtime and parity suite. It targets `net8.0` and `net472`; compiled-mode fixture coverage runs only on `net8.0`, while `net472` runs the interpreted lane.

`tests/Alder.Compiled.Test` is the compiled backend suite. It targets `net8.0` and owns IL compiler, FastExpressionCompiler adapter, Dynamic LINQ, and EF Core provider coverage.

## Useful Commands

```bash
dotnet build
dotnet test --configuration Release --framework net8.0
dotnet test tests/Alder.Test/Alder.Test.csproj --configuration Release --framework net472
dotnet test --filter "FullyQualifiedName~Alder.Test.Docs"
dotnet test tests/Alder.Compiled.Test/Alder.Compiled.Test.csproj --filter "FullyQualifiedName~DynamicLinq"
dotnet test --filter "FullyQualifiedName~AotGeneratedDispatchDocTests"
dotnet test --filter "DisplayName~LangRef_Literal"
./scripts/aot-publish-check.sh --strict
./scripts/aot-matrix.sh
```

Run the narrowest useful command while developing. On macOS or Linux, the `net472` test command requires Mono; CI runs the `net8.0` lane across Linux, Windows, and macOS. Before merging a runtime change, run the broader tests that cover the affected backend, parser, binder, security, and AOT paths. Treat `aot-publish-check.sh --strict` as the NativeAOT warning gate; CI runs it on supported Unix hosts, and `aot-matrix.sh` is a broader diagnostic corpus that may be narrowed when validating the supported AOT surface.

## Runtime Changes

Runtime changes need to account for:

- Interpreted execution.
- Compiled execution.
- AOT generated dispatch.
- Security policy behavior.
- Diagnostic code and span fidelity.

When changing method dispatch, overload resolution, member access, or type registration, verify that generated `TryInvoke` and `TryInvokeStatic` paths still integrate.

## Code Style

- Use Roslyn-style diagnostic codes where they apply.
- Use `DiagnosticDescriptors` for diagnostic exceptions.
- Use canonical token lexemes instead of magic operator strings.
- `Try*` methods return `false` or `null` for expected failure.
- Do not use exception-driven control flow.
- Keep `CancellationToken` as the last parameter.
- Avoid wrapper methods that only delegate to one public method.
- Keep comments for non-obvious logic; avoid section-divider comments.

## Pull Requests

Before opening a PR:

- Build succeeds.
- Relevant tests pass.
- New language behavior has corpus parity coverage.
- New docs samples have matching doc tests.
- Runtime changes have both backend coverage when the behavior is shared.
- AOT impact is tested or explicitly explained.
- The patch does not include generated `bin/` or `obj/` output.
