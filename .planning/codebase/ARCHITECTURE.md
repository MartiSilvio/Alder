# Architecture

**Analysis Date:** 2026-03-17

## Pattern Overview

**Overall:** Multi-stage expression compiler/interpreter pipeline with optional IL compilation backend

**Key Characteristics:**
- Parse → Bind → Execute pipeline modeled after the Roslyn compiler architecture
- Two execution backends: tree-walking interpreter (`BoundEvaluator`) and LINQ expression tree compiler (`BoundExpressionEmitter` → IL delegates)
- Immutable, frozen configuration (`CsEvalConfig`) after first evaluation; mutable setup phase before
- Engine is thread-safe after freezing; parent/child engine relationships share frozen config and expression cache
- Sandbox permission model enforced at the binding/evaluation layers via `SandboxOptions`

## Layers

**Public API Layer:**
- Purpose: User-facing surface for parsing, evaluating, and configuring expressions
- Location: `src/CsEval/CsEvalEngine.cs`, `src/CsEval/CsEvalExpression.cs`, `src/CsEval/CompiledExpression.cs`
- Contains: `CsEvalEngine` (main entry point), `CsEvalExpression` (pre-parsed expression handle), `CsEvalCompiledExpression<T>` (compiled delegate wrapper)
- Depends on: Parsing, Binding, Interpretation, Compilation, Runtime layers
- Used by: External consumers; no internal layer depends on it

**Parsing Layer:**
- Purpose: Lex and parse expression strings into an untyped AST (`Expr` record hierarchy)
- Location: `src/CsEval/Parsing/`
- Contains: `Lexer`, `ExpressionParser`, `PrimaryParser`, `PatternParser`, `StatementParser`, `QueryParser`, `Ast.cs` (abstract `Expr` records), `Token.cs`, `AstDepthValidator`, `AstWalker`
- Depends on: Nothing internal
- Used by: `CsEvalEngine.Parse()`, `TryValidate()`, compilation pipeline

**Binding Layer:**
- Purpose: Semantic analysis — resolve types, validate operators, produce typed `BoundExpr` tree from untyped `Expr` AST
- Location: `src/CsEval/Binding/`
- Contains: `Binder` (central dispatch via pattern-matching switch), `BindingContext`, `BoundExpr` (abstract base), all `Bound*Expr` record types in `BoundNodes/`, `BoundCallPlan`/`BoundIndexPlan`/`BoundMemberPlan` in `Plans/`, `CallBinderService`/`MemberBinderService` in `Services/`
- Depends on: Parsing layer (consumes `Expr` AST), Runtime layer (`CsEvalContext`, `TypeResolver`)
- Used by: Interpretation layer, Compilation layer, `TryValidate()`

**Interpretation Layer:**
- Purpose: Tree-walk evaluation of bound AST nodes at runtime
- Location: `src/CsEval/Interpretation/`
- Contains: `BoundEvaluator` — large switch dispatch over all `BoundExpr` subtypes; runtime semantics helpers in `src/CsEval/Runtime/Semantics/` (`IdentifierRuntime`, `AssignmentRuntime`, `ConstructionRuntime`, `PatternRuntime`, `ExecutionRuntime`, `NumericPromotionRuntime`)
- Depends on: Binding layer (consumes `BoundExpr`), Runtime layer
- Used by: `CsEvalEngine.Evaluate()` when `CompilationMode.Interpreted`, or as fallback

**Compilation Layer (core):**
- Purpose: Plugin registration point for the IL compilation backend; expression caching
- Location: `src/CsEval/Compilation/`
- Contains: `CompiledProviderRegistry` (static singleton registry for `ICompiledProvider`), `ExpressionCache` (FIFO-bounded `ConcurrentDictionary`, capacity 10,000 entries)
- Depends on: Binding layer interfaces, Parsing layer
- Used by: `CsEvalEngine`, `CsEvalExpression.TryCompile()`

**Compilation Layer (IL backend — separate package):**
- Purpose: Compile `BoundExpr` trees to LINQ expression trees, then to native IL delegates
- Location: `src/CsEval.Compiled/Compilation/`
- Contains: `ILExpressionCompiler` (orchestrator), `BoundExpressionEmitter` (emits `System.Linq.Expressions.Expression` trees from bound nodes), `ExpressionTreeEmitter` (AST-level fallback path), `BoundRuntimeMethodCache`, `CompilerReflectionCache`
- Depends on: Core `CsEval` package (Binding, Parsing, Runtime layers)
- Used by: Registered into `CompiledProviderRegistry` at startup via `CsEvalCompiledExtensions.RegisterCompiledProvider()`

**Runtime Layer:**
- Purpose: Execution context, type resolution, reflection helpers, operator dispatch, method invocation
- Location: `src/CsEval/Runtime/`
- Contains: `CsEvalContext` (scoped variable store; parent/child hierarchy; thread-safe with `ConcurrentDictionary`), `CsEvalConfig` (immutable frozen configuration), `TypeResolver` (Roslyn-precedence type lookup), `TypeMetadataProvider`, `MethodResolver`, `MethodDispatchCache`, `OperatorRegistry`, `NumericDispatch`, `ReflectionRuntime`, `ExtensionMethodResolver`, `LambdaDelegateFactory`, `LambdaDelegateConverter`
- Collections: `FixedDictionary<K,V>`, `FixedSet<T>` (frozen, read-optimized wrappers backed by `FrozenDictionary`)
- Depends on: Nothing internal
- Used by: All other layers

**Source Generator (separate package):**
- Purpose: AOT-safe type metadata generation; emit `CsEvalTypeContext` subclasses that pre-register member metadata
- Location: `src/CsEval.Generators/`
- Contains: `CsEvalSourceGenerator` (Roslyn `IIncrementalGenerator`), `ContextEmitter`, `TypeMetadataEmitter`, model types in `Model/`
- Bundled: Shipped as an analyzer inside the `CsEval` NuGet package (via `analyzers/dotnet/cs` pack path)

**Diagnostics Layer:**
- Purpose: Structured error reporting with C# compiler-style error codes
- Location: `src/CsEval/Diagnostics/`
- Contains: `DiagnosticDescriptor`, `DiagnosticDescriptors` (static catalog of all error templates), `DiagnosticCode` (enum mapping to CS#### codes)
- Used by: All layers — every `CsEvalException` throw must reference a descriptor from `DiagnosticDescriptors`

## Data Flow

**Standard Evaluated Path (Interpreted mode):**

1. Caller invokes `CsEvalEngine.Evaluate(string expression)`
2. `Lexer` tokenizes the string → `List<Token>`
3. `ExpressionParser` (Pratt-style recursive descent) parses tokens → `Expr` AST
4. `CsEvalExpression` wraps the `Expr` and caches it
5. `Binder.Bind(ast, bindingContext)` traverses the `Expr` tree, resolves types, validates semantics → `BoundExpr` tree (cached per-context via `ConditionalWeakTable`)
6. `BoundEvaluator.Evaluate(boundExpr)` tree-walks the `BoundExpr`, dispatching to runtime helpers → `object?` result
7. Result is optionally coerced via `ConvertResult<T>`

**Compiled Path (CompilationMode.Compiled):**

1–4 same as above
5. `Binder.Bind()` → `BoundExpr`
6. `ILExpressionCompiler.TryCompile(bound)` traverses `BoundExpr`, calls `BoundExpressionEmitter.Emit()` to produce a `System.Linq.Expressions.Expression` tree
7. `IExpressionCompiler.Compile()` (default: `Expression<T>.Compile()`) JIT-compiles to `CompiledExpressionDelegate`
8. Cached in `ExpressionCache` keyed by expression text
9. Delegate is stored as `CompiledNoCancellationFastPath` for single-expression hot paths (zero allocation re-invocation)

**Child Engine / Per-Invocation Variables:**

- `CreateChild()` produces a new engine sharing the parent's frozen `CsEvalConfig` and `ExpressionCache`
- `CsEvalContext.CreateChild()` produces a scoped variable context that inherits parent variables read-only
- Per-invocation `variables` dict creates a temporary child engine + child context

**State Management:**
- Before first evaluation: mutable (`_registeredTypes`, `_functions`, `_pendingVariables`, `_usingNamespaces`)
- After first evaluation: `_frozenConfig` is set via `Interlocked.CompareExchange`; further mutations throw `InvalidOperationException`
- Variables: `SetVariable` → staged in `_pendingVariables` until context is created; then `Define()` on `CsEvalContext`

## Key Abstractions

**`Expr` (untyped AST):**
- Purpose: Represents the syntactic structure of a parsed expression, type-agnostic
- Examples: `src/CsEval/Parsing/Ast.cs` — `LiteralExpr`, `BinaryExpr`, `CallExpr`, `BlockExpr`, ~80 node types
- Pattern: Abstract `record` hierarchy; visitor interface `IExprVisitor<T>`

**`BoundExpr` (typed, semantically-analyzed AST):**
- Purpose: Represents a semantically-resolved expression with static type information attached to every node
- Examples: `src/CsEval/Binding/BoundNodes/` — `BoundBinaryExpr`, `BoundCallExpr`, `BoundMemberAccessExpr`, ~65 node types
- Pattern: Abstract `record BoundExpr(Type StaticType)` hierarchy; switch-dispatched in `Binder` and `BoundEvaluator`

**`CsEvalEngine`:**
- Purpose: User-facing façade; owns configuration lifecycle, expression caching, child engine creation
- Location: `src/CsEval/CsEvalEngine.cs`
- Pattern: Fluent builder API (returns `this`) for pre-freeze configuration; freeze-on-first-use pattern

**`CsEvalConfig` (frozen configuration):**
- Purpose: Immutable snapshot of all engine registrations; shared safely across threads and child engines
- Location: `src/CsEval/Runtime/CsEvalConfig.cs`
- Pattern: Factory method `Create()`, private constructor; all collections stored as `FixedDictionary` / `ImmutableArray`

**`CsEvalContext` (execution context):**
- Purpose: Scoped variable store and config accessor; forms a parent/child tree per evaluation scope
- Location: `src/CsEval/Runtime/CsEvalContext.cs`
- Pattern: Parent context is never mutated by children; concurrent store for shared contexts, local `Dictionary` for child scopes

**`ControlFlowSignal`:**
- Purpose: Sentinel value for `return`, `break`, `continue`, `goto` — avoids using exceptions for control flow
- Location: `src/CsEval/CsEvalException.cs`
- Pattern: Value object returned by `BoundEvaluator` methods; callers check and propagate

**`SandboxOptions`:**
- Purpose: Fine-grained permission flags for expression capabilities; enforced during binding and evaluation
- Location: `src/CsEval/CsEvalOptions.cs`
- Pattern: Immutable record; factory presets `Trusted()`, `Safe()`, `Strict()`; `AllowedTypes` allowlist for type-level restriction

## Entry Points

**`CsEvalEngine.Evaluate(string expression)`:**
- Location: `src/CsEval/CsEvalEngine.cs` line 214
- Triggers: Public API call
- Responsibilities: Parse, bind, select execution path (interpreted vs compiled), enforce constraints, return result

**`CsEvalEngine.Parse(string expression)`:**
- Location: `src/CsEval/CsEvalEngine.cs` line 173
- Triggers: Public API call or internally before `Evaluate(CsEvalExpression)`
- Responsibilities: Lex + parse only; returns a reusable `CsEvalExpression` handle

**`CsEvalEngine.TryValidate(string expression, out IReadOnlyList<CsEvalDiagnostic> diagnostics)`:**
- Location: `src/CsEval/CsEvalEngine.cs` line 553
- Triggers: Public API call
- Responsibilities: Parse + bind + identifier resolution; collect and return structured diagnostics without evaluating

**`Binder.Bind(Expr, BindingContext)`:**
- Location: `src/CsEval/Binding/Binder.cs`
- Triggers: Called from `CsEvalExpression.GetOrCreateBoundExpression()` and from `ILExpressionCompiler.TryCompile()`
- Responsibilities: Full semantic analysis; produces typed `BoundExpr` tree

**`BoundExpressionEmitter.EmitRoot(BoundExpr)`:**
- Location: `src/CsEval.Compiled/Compilation/BoundExpressionEmitter.cs`
- Triggers: Called by `ILExpressionCompiler.TryCompile(BoundExpr)`
- Responsibilities: Convert typed bound tree into a `System.Linq.Expressions` tree for IL compilation

## Error Handling

**Strategy:** Structured exceptions with Roslyn-style diagnostic codes; `ControlFlowSignal` value objects for non-exceptional control flow

**Patterns:**
- All `CsEvalException` throws must use a `DiagnosticDescriptor` from `DiagnosticDescriptors` — never a raw string message
- Exception hierarchy: `CsEvalException` (base) → `CsEvalDepthException`, `CsEvalLanguageModeException`, `CsEvalExecutionLimitException`, `CsEvalSandboxException`, `CsEvalParserException`
- `TryEvaluate` / `TryParse` / `TryValidate` variants provide non-throwing APIs; `TryValidate` returns `IReadOnlyList<CsEvalDiagnostic>`
- `ControlFlowSignal` (not an `Exception`) propagates `return`/`break`/`continue`/`goto` values through the interpreter tree to avoid SEH overhead and prevent user `catch` blocks from intercepting them

## Cross-Cutting Concerns

**Logging:** None — no logging framework. Diagnostic output is via structured `CsEvalDiagnostic` records and exceptions.

**Validation:** Two-phase: parse-time (syntax) via `ExpressionParser`; bind-time (semantic) via `Binder`; optional explicit validation via `TryValidate()`

**Authentication:** N/A — sandbox enforced via `SandboxOptions` flags and `AllowedTypes` allowlist checked during binding/evaluation

**AOT Compatibility:** `CsEval.csproj` sets `<IsAotCompatible>true</IsAotCompatible>`; `[DynamicallyAccessedMembers]` annotations on all reflection-heavy registration APIs; `CsEvalTypeContext` / source generator path provides pre-computed type metadata to avoid reflection at runtime

**Thread Safety:** `CsEvalEngine` is fully thread-safe for `Evaluate`/`Parse`/`Compile` after first evaluation; `CsEvalContext` uses `ConcurrentDictionary` for shared contexts; per-invocation isolation via child contexts for `ExecutionConstraints`

---

*Architecture analysis: 2026-03-17*
