---
title: "Compiler"
description: "LINQ expression tree emission, local promotion, identifier hoisting, delegate caching"
sidebar:
  order: 7
---

The compiler is Alder's second execution backend. It translates the bound tree into `System.Linq.Expressions.Expression` trees (LINQ expression trees), which are then compiled to native delegates. The same bound tree that the interpreter walks is used as input — the compiler produces IL instead of walking nodes.

## Compilation Flow

```mermaid
graph LR
    B["BoundExpr"] --> E["BoundExpressionEmitter"]
    E -->|"System.Linq.Expressions"| L["LINQ Expression Tree"]
    L --> C["IExpressionCompiler"]
    C -->|"Expression.Compile()"| D["Native Delegate"]
```

The emitter handles all 63 `BoundNodeKind` values. Each node kind has a corresponding `Emit*` method that produces a LINQ expression subtree.

## Two Emitters

Alder has two separate expression tree emitters for different use cases:

**BoundExpressionEmitter** — full emitter used by `Evaluate` in compiled mode. Handles all node kinds including loops, blocks, variable declarations, try-catch, assignments, control flow signals, pattern matching, and all Extended mode features. Produces delegates with signature `(AlderContext, AlderConfig, ExecutionConstraintState, CancellationToken) → object?`.

**ExpressionTreeEmitter** — lightweight emitter used by `ParseAsExpression<TDelegate>`. Produces clean, provider-transparent expression trees suitable for Entity Framework and IQueryable providers. Supports a smaller subset: no loops, blocks, variable declarations, assignments, try-catch, or collection expressions. Produces typed expression trees like `Expression<Func<int, bool>>` with no Alder runtime dependencies in the tree.

## Local Promotion

The compiler's most significant optimization. Before emission, the emitter analyzes the bound tree for variable declarations that can be "promoted" from context-based storage (dictionary lookup) to typed LINQ `ParameterExpression` locals (register/stack variables).

A variable is promoted when:
- It's declared with `var` or an explicit type (not `const`)
- Its static type is known (not `object`)
- There are no lambdas in the expression (lambdas capture variables by reference through the context, which is incompatible with local promotion)
- The variable name is unique (no duplicate declarations with different `LocalId`s)

When promoted, variable reads and writes use the typed LINQ local directly instead of calling `context.Get(name)` and `context.Set(name, value)`. This eliminates dictionary lookups, string comparisons, and boxing for value types.

## Identifier Hoisting

For engine-level variables (from `SetVariable<T>`), the emitter can hoist frequently-accessed identifiers into typed locals at the start of the compiled body. An identifier is hoisted when:
- It's referenced more than once in the expression
- Its type is known from the binding context

Hoisted identifiers are loaded once via `context.GetVariableTyped<T>(name)` and stored in a `ParameterExpression`. Subsequent reads use the local directly. This converts N dictionary lookups into 1 lookup + N-1 local reads.

## Control Flow Signal Handling

The compiled expression must handle `ControlFlowSignal` the same way the interpreter does. The emitter:

1. **At the root** (`EmitRoot`): Wraps the body in signal unwrapping logic — if the result is a `ControlFlowSignal`, extract its `Value` property.

2. **At loop boundaries**: Emits `break`/`continue` label targets. `break` produces a signal that jumps to the loop's end label. `continue` produces a signal that jumps to the loop's continue label.

3. **At block boundaries**: Signal propagation is automatic — signals are `object?` values that flow through the expression tree naturally.

## Compilation Pipeline

The compilation path uses a different bound tree pipeline than interpretation:

| Pass | Interpretation | Compilation |
|------|:-:|:-:|
| SecurityValidationPass | yes | yes |
| ConstantFoldingPass | yes | yes |
| DeadBranchEliminationPass | yes | yes |
| ConversionInsertionPass | — | yes |

The `ConversionInsertionPass` is compilation-only because LINQ expression trees require exact type matching. The interpreter handles numeric promotion at runtime in `NumericDispatch.PromoteOperands`, but the compiler needs explicit `BoundCastExpr` nodes in the bound tree so it can emit `Expression.Convert` calls.

## IExpressionCompiler

The final step is compiling the LINQ expression tree to a native delegate. This is abstracted behind `IExpressionCompiler`:

```csharp
public interface IExpressionCompiler
{
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}
```

The default implementation calls `expression.Compile()`. Users can implement this interface to substitute an alternative compiler backend. The replacement must support all LINQ expression node types that Alder's emitter produces.

## Delegate Caching

Compiled delegates are cached at two levels:

1. **Per-AlderExpression**: The `CompiledExpressionInfo` (containing the delegate, compilability flag, and failure reason) is stored as a `volatile` field on the `AlderExpression`. Thread-safe via double-checked locking inside the compilation method.

2. **Per-engine ExpressionCache**: When compiling from a string (via the AST path), the `ExpressionCache` stores compiled results in a `ConcurrentDictionary<string, CompiledExpressionInfo>` with FIFO eviction at 10,000 entries. Shared between parent and child engines.

## NativeAOT Guard

`UseCompiler()` checks `RuntimeFeature.IsDynamicCodeSupported` on .NET 7+. If dynamic code is not supported (NativeAOT, IL2CPP), it throws `PlatformNotSupportedException` with a message directing the user to use the interpreter with AOT metadata instead.

This guard exists because `Expression.Compile()` requires a JIT compiler to emit IL at runtime. On AOT platforms, the interpreter with source-generator-produced type metadata provides the best performance.

## Runtime Method Cache

The emitter uses `BoundRuntimeMethodCache` to cache `MethodInfo` references for runtime helper methods it calls from generated code (e.g., `AlderContext.Get`, `AlderContext.Set`, `TypeHelpers.ExplicitCast`, `NumericDispatch.Add`). These are resolved once via reflection at static initialization and stored as constants in the emitted expression trees.
