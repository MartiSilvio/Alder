The compiler is Alder's second execution backend. It translates the bound tree into `System.Linq.Expressions.Expression` trees, which are then compiled to native delegates. The same bound tree that the interpreter walks is the input: the compiler produces IL instead of walking nodes.

## Compilation Flow

```mermaid
graph LR
    B["BoundExpr"] --> E["Emitter"]
    E -->|"System.Linq.Expressions"| L["LINQ Expression Tree"]
    L --> C["IExpressionCompiler"]
    C -->|"Expression.Compile()"| D["Native Delegate"]
```

Each bound node kind has a dedicated emitter class implementing `INodeEmitter<TNode>`, registered with the emission context at construction time.

## Two Emitters

Alder has two separate expression tree emitters for different use cases:

**BoundExpressionEmitter**: the full emitter used by `Evaluate` in compiled mode. Handles all bound node kinds: loops, blocks, variable declarations, try-catch, assignments, control flow signals, pattern matching, and all Extended mode features. Produces delegates with the signature `(AlderContext, AlderConfig, ExecutionConstraintState, CancellationToken) → object?`.

**ExpressionTreeEmitter**: a lightweight emitter used by `ParseAsExpression<TDelegate>`. Produces clean, provider-transparent expression trees suitable for Entity Framework and IQueryable providers. Supports the expression subset: no loops, blocks, variable declarations, assignments, try-catch, or collection expressions. Produces typed expression trees like `Expression<Func<int, bool>>` with no Alder runtime dependencies.

## Binary Operation Optimization

The binary emitter uses a tiered strategy:

1. **Primitive fast path**: When the binder has pre-computed a `PromotedType`, the emitter generates direct LINQ expression tree operators (`Expression.Add`, `Expression.LessThan`, etc.) with checked variants in a `checked` context. No runtime dispatch, no boxing.

2. **String concatenation fast path**: When either operand of `+` is `string`, emits a direct call to `string.Concat`. Non-string operands are converted via `ToString()`.

3. **Runtime fallback**: All other combinations delegate to the runtime operator methods with full support for `**`, `<=>`, `in`, `like`, and user-defined operators.

ECMA-334 §10.2.11 constant promotion is applied when one operand is a literal (e.g., `int` constant 0 promoting to `uint`).

Left-associative chains (`a + b + c + d`) are flattened iteratively: the emitter walks the left spine, collects all links, then folds them in a single pass. This produces flatter expression trees and avoids deep recursion.

## Local Promotion

The compiler's most significant optimization. Before emission, the emitter analyzes the bound tree for variable declarations that can be "promoted" from context-based storage (dictionary lookup) to typed LINQ `ParameterExpression` locals (register/stack variables).

A variable is promoted when:
- It has a known static type (not `object`) and is not a `ValueTuple` type
- There are no lambdas in the expression (lambdas capture variables by reference through the context, incompatible with local promotion)
- The variable name is unique across scopes

Promoted variables eliminate dictionary lookups, string comparisons, and boxing for value types.

The identifier emitter resolves each identifier through three tiers: (1) promoted local → direct variable reference, (2) hoisted identifier → direct variable reference, (3) typed context lookup or runtime resolution.

## Identifier Hoisting

For engine-level variables (from `SetVariable<T>`), the emitter hoists frequently-accessed identifiers into typed locals at the start of the compiled body. An identifier is hoisted when it's referenced more than once and its type is known from the binding context. This converts N dictionary lookups into 1 lookup + N-1 local reads.

## Control Flow Signal Handling

The compiled expression handles `ControlFlowSignal` the same way the interpreter does:

1. **At the root**: Wraps the body in signal unwrapping: if the result is a signal, extract its value.
2. **At loop boundaries**: Emits `break`/`continue` label targets with signal-to-jump translation.
3. **At block boundaries**: Signals flow naturally as `object?` values.

## Bound Tree Pipeline

The compilation path adds a `ConversionInsertionPass` that the interpreter doesn't need: it inserts explicit cast nodes for numeric type promotions because LINQ expression trees require exact type matching.

See [Bound Tree Pipeline](bound-tree-pipeline.md) for all passes.

## Swappable Compiler Backend

The final compilation step is abstracted behind `IExpressionCompiler`:

```csharp
public interface IExpressionCompiler
{
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}
```

The default implementation calls `expression.Compile()`. Users can substitute any alternative LINQ expression tree compiler by implementing this interface and passing it to `UseCompiler()`. See [Compilation: Swapping the Expression Compiler](../engine/compilation.md#swapping-the-expression-compiler).

## Delegate Caching

Compiled delegates are cached at two levels:

1. **Per-expression**: Stored on the `AlderExpression` as a volatile field with double-checked locking. Compilation locks on the expression object, so multiple threads compiling different expressions proceed in parallel.

2. **Per-engine**: When compiling from a string, the engine cache stores compiled results with FIFO eviction. Shared between parent and child engines.

## NativeAOT Guard

`UseCompiler()` checks `RuntimeFeature.IsDynamicCodeSupported` on .NET 7+. On NativeAOT and IL2CPP, `Expression.Compile()` is unavailable: the engine throws `PlatformNotSupportedException` directing users to the interpreter with AOT metadata instead.

## Lambda Emission

Lambdas are not compiled to LINQ expression tree lambdas. Instead, the emitter creates a runtime lambda object that stores the original AST, parameter names, and a reference to the enclosing context. When invoked, the lambda body is evaluated by the interpreter (or bound and compiled on first call). This preserves full closure semantics: variables are captured by reference through the context chain, and updates are visible across all invocations.

## ForEach Emission

The foreach emitter generates a full IEnumerable/IEnumerator pattern: validate collection, call `GetEnumerator()`, loop with `MoveNext()` and `Current`, handle break/continue signals, and dispose the enumerator in a `finally` block. Execution constraints are checked at the top of each iteration.

## Switch and Pattern Emission

Switch expressions delegate pattern matching entirely to the pattern runtime: the emitter passes the pattern AST as-is rather than generating IL for each pattern kind. Each arm gets an isolated child context for pattern variable bindings, with cleanup in a `finally` block. Simple type patterns without variable binding compile to `Expression.TypeIs` (the IL `isinst` instruction) for maximum performance.

## Collection Size Enforcement

Every non-value-type, non-string method return value is wrapped with a collection size check that validates against the security policy's `MaxCollectionSize`. This prevents methods like `.ToList()` from producing unbounded collections.
