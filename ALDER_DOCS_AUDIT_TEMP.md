# Alder Surface and Docs Audit

Grounding: public/API shape from [src/Alder/AlderEngine.cs](/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/AlderEngine.cs:1), [src/Alder/AlderOptions.cs](/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/AlderOptions.cs:1), [src/Alder.Compiled/AlderCompiledEngineExtensions.cs](/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder.Compiled/AlderCompiledEngineExtensions.cs:1), AOT types under [src/Alder/Aot](/Users/silviomartignetti/Developer/Wovera/CsEval/src/Alder/Aot), and feature support from `tests/Alder.Test/*`, especially `Core`, `Runtime`, `Security`, `Compilation`, `AOT`, `Extensions`, `Compliance`, `Integration`, and `Docs`.

# 1. Alder Surface Area Inventory

| Domain | What exists | Key capabilities | Maturity | User importance | Notes |
| --- | --- | --- | --- | --- | --- |
| Core engine API | `AlderEngine`, `AlderExpression`, `AlderEval`, string extension helpers | Parse, validate, sync/async evaluate, try-APIs, parsed-expression reuse, child engines, variable registration | High | High | Public surface is broad and tested in `Core`, `Integration`, `Docs`. |
| Configuration | `AlderOptions`, `LanguageMode`, `SandboxOptions`, `ExecutionConstraints`, nested builders | Case sensitivity, language mode, sandbox, limits, DI service provider, modules, functions, type resolution, AOT contexts, compiler selection | High | High | Builders are stable and explicitly tested in `ApiSurfaceTests`, docs tests, security tests. |
| Variables and context/scoping | Engine variables, per-call variables, anonymous-object projection, positional `@0`, child contexts, typed/runtime-typed variable paths | Type-preserving binding, isolated per-call vars, parent/child visibility, scoping rules, rebinding on type-surface changes | High | High | `SetVariablesPreservingRuntimeTypes` exists but is undocumented in current docs set. |
| Standard language surface | Parser/binder/evaluator plus large bound-node/evaluator set | Expressions, statements, loops, switch, try/catch/finally, `await`, `yield`, tuples, deconstruction, query expressions, lambdas, conversions, pattern matching | High | High | Strongest evidence comes from `Compliance`, `Runtime`, `Operators`, `Types`, `Parsing`, `Parity`. |
| Extended mode | `LanguageMode.Extended` plus extension runtime/parser support | `**`, pipeline `|>`, chained comparisons, `let ... in`, list comprehension forms, date/time sugar, strict equality, array spread | Medium | Medium | Tests show object spread is not supported; array spread is. Extended surface is real but underdocumented. |
| Binding and type resolution | Binder, binding services, type resolver, extension method resolver, module metadata | Resolved vs dynamic nodes, overload resolution, extension methods, imported namespaces/assemblies, validation diagnostics | High | High | This is a core internal domain with strong tests but mostly explanation-only docs. |
| Execution backends and compilation | Interpreter plus compiled backend in `Alder.Compiled` | `UseCompiler()`, `TryCompile`, compiled sync evaluation, `Compile<T>`, `CompileToFunc`, typed delegates, expression-tree export | High | High | Async evaluation stays on interpreter; compiled path is sync-only in current implementation. |
| Dynamic LINQ | `Alder.Compiled` query extensions and lambda factory | `IEnumerable`, `IQueryable`, `IAsyncEnumerable` operators; inline/named vars; structural projections; EF translation; DataRow support | High | High | Operator surface is large and heavily tested, including provider-limited cases. |
| AOT / typed dispatch | `AlderTypeContext`, `TypedDispatch`, `GenericStaticDispatch`, built-in context, generators | Generated member/method/index/constructor dispatch, delegate factories, reflection fallback, authoritative generated-mode failures | Medium | Medium | Good test coverage, including simulated AOT and parity against reflection. |
| Security and execution constraints | Sandbox validation, security policy, reflection guard, constraint state | Trusted/Safe/Strict presets, per-operation gating, deny/trust type/namespace lists, reflection blocking, collection limits, regex timeout, statement/loop/timeout limits | High | High | Very strong test coverage, including attack-oriented cases. |
| Diagnostics and tracing | `AlderDiagnostic`, `AlderException`, `EvaluateWithTrace`, `TraceNode` | Structured diagnostics, Roslyn-style codes, try-APIs, trace trees with values/errors/source spans | Medium | High | Diagnostics are well-tested; tracing exists and is public but barely documented. |
| Caching, reuse, and concurrency | Expression runtime state, compiled invalidation, child-engine model | Parsed reuse, bound/compiled reuse, type-version invalidation, thread-safe root access, child isolation | Medium | Medium | Behavior is tested, but current docs only mention parts of it. |

# 2. Existing Docs Inventory

| File | Type | Quality | Accuracy vs code | Audience fit | Recommendation |
| --- | --- | --- | --- | --- | --- |
| `docs/explanation/architecture.md` | explanation | High | accurate | correct | KEEP |
| `docs/explanation/binding-system.md` | explanation | High | accurate | correct | KEEP |
| `docs/explanation/typed-dispatch.md` | explanation | High | accurate | correct | KEEP |
| `docs/how-to/add-function.md` | how-to | Medium | accurate | correct | KEEP BUT EDIT |
| `docs/how-to/add-module.md` | how-to | Medium | accurate | correct | KEEP BUT EDIT |
| `docs/how-to/use-dynamic-linq.md` | how-to | Medium | outdated | correct | KEEP BUT EDIT |
| `docs/meta/writing-guidelines.md` | explanation / internal policy | High | accurate | maintainer-biased | KEEP |
| `docs/reference/configuration.md` | reference | Medium | accurate | correct | KEEP BUT EDIT |
| `docs/reference/execution-model.md` | reference | Medium | accurate | correct | KEEP BUT EDIT |
| `docs/reference/language/ecma-conformance.md` | reference | Medium | unclear | correct | REWRITE |
| `docs/reference/language/operator-status.md` | reference | High | accurate | correct | KEEP |

Strict take: the current docs set is small, generally solid, and badly incomplete.

# 3. Coverage Gap Analysis

High-value gaps:

- No getting-started page, despite `tests/Alder.Test/Docs/GettingStartedDocTests.cs`.
- No variables/context page, despite `VariablesDocTests`; current docs do not cover child engines, per-call isolation, type-version rebinding, or `SetVariablesPreservingRuntimeTypes`.
- No security page, despite `SecurityDocTests` and the much larger `tests/Alder.Test/Security/*` suite.
- No compilation page, despite `CompilationDocTests`, `CompilationTests`, `ExpressionTreeTests`, and typed-delegate coverage.
- No type-registration page, despite `TypeRegistrationDocTests` and runtime type-resolution tests.
- No tracing/diagnostics page, even though `EvaluateWithTrace`, `TraceNode`, `EvaluationTraceResult`, `TryParse`, `TryValidate`, and Roslyn-style diagnostics are public.
- Extended mode is underrepresented. Current docs mention it, but there is no focused reference for what Extended actually adds and what remains unsupported.
- Dynamic LINQ how-to underrepresents the real surface: async operators, joins/group joins, grouping, set/paging operators, EF-specific behavior, DataRow usage, pre-parsed lambdas, provider-limited cases.
- AOT docs explain the model but do not document the operational boundary users hit in authoritative generated mode.
- No public API reference for secondary surfaces like `AlderStringExtensions`, `GetRegisteredModules`, `ParseAsExpression`, `TryParseAsExpression`, `CompileExpression`, `CreateDynamicLambdaFactory`.

# 4. Rewrite Targets (Top 10)

| Title | Domain | Why it matters | Depends on code areas |
| --- | --- | --- | --- |
| Getting Started | Core API | The repo has tests for this doc, but no page. New users currently have no code-backed entry point. | `src/Alder/AlderEngine*.cs`, `tests/Alder.Test/Docs/GettingStartedDocTests.cs` |
| Variables, Context, and Child Engines | Variables/scoping | Variable behavior is central to correctness and easy to misuse. Current docs barely cover it. | `AlderEngine.Registration`, `AlderContext`, `VariableBindingProjector`, `VariablesDocTests`, `ScopingTests` |
| Security Sandbox and Execution Limits | Security | This is a major product surface with many operational rules and many tests, but no page. | `AlderOptions`, `SecurityPolicy`, `tests/Alder.Test/Security/*`, `SecurityDocTests` |
| Compiled Backend and Expression Trees | Compilation | Compiled execution, typed delegates, and expression-tree export are public and important, but undocumented as a unified surface. | `src/Alder.Compiled/*`, `CompilationTests`, `ExpressionTreeTests`, `CompilationDocTests` |
| Type Registration and Resolution | Type resolution | Namespace/assembly/extension-method registration affects binding quality and runtime behavior. | `AlderOptions.TypeBuilder`, `TypeResolver`, `ExtensionMethodResolver`, `TypeRegistrationDocTests` |
| Functions and Modules | Extensibility | Current how-tos are narrow; the real surface includes DI resolution, assembly scan, explicit member maps, async methods, and collision behavior. | `AlderOptions.ModuleBuilder`, `FunctionBuilder`, `ModuleMemberMetadata`, doc tests, integration tests |
| Dynamic LINQ | Dynamic LINQ | The current how-to is too small for the actual tested surface and limitations. | `src/Alder.Compiled/AlderLinqExtensions*.cs`, `DynamicLinq*Tests`, EF Core tests |
| Tracing and Diagnostics | Diagnostics/tracing | Public debugging surfaces exist but are effectively invisible. | `AlderDiagnostic`, `AlderException`, `EvaluateWithTrace`, `TraceNode`, `TracingTests`, validation tests |
| Extended Mode Reference | Extended language | Extended mode is real, user-visible, and only partially described today. | parser/runtime extension code, `Extensions/*`, `GettingStartedDocTests.ExtendedMode` |
| AOT and Generated Contexts | AOT | The explanation page is useful, but users still need the concrete setup/boundary/failure-mode doc. | `src/Alder/Aot/*`, generator output model, `AOT/*`, `AotDispatchVerificationTests` |

# 5. Risks

- `EvaluateAsync` is likely to be misunderstood. Compiled mode does not make async evaluation compiled; async stays on the interpreter path.
- Variable injection is easy to misuse. Using erased `object`-typed variables instead of typed/runtime-typed paths can change binding quality and overload selection.
- Shared parent-engine mutation is likely to be misread as thread-safe business logic. The code supports concurrent access, not atomic multi-step updates or snapshots.
- Safe sandbox behavior is non-obvious. Method calls can be blocked while extension-method-based LINQ still works.
- Reflection blocking is stricter than many users will expect. `Type` objects are allowed, but `MemberInfo`, `Assembly`, and related metadata surfaces are blocked even in trusted mode.
- Extended mode looks broader than it is. Array spread is supported; object spread is not.
- Dynamic LINQ provider failures are likely to be blamed on Alder unless docs clearly separate Alder support from `IQueryable` provider translation limits.
- AOT authoritative generated mode will surprise users unless the generated-context requirement is documented as an operational boundary, not an optimization.
- Tracing is effectively invisible today, so debugging is harder than necessary for users who do not read the API surface directly.
- The ECMA conformance page is high-risk as written because it makes a wide reference claim surface with limited page-level grounding compared with the rest of the docs set.
