---
title: "Interpreter"
description: "Tree-walking evaluation, NumericDispatch, ControlFlowSignal, AOT integration"
sidebar:
  order: 6
---

The interpreter is Alder's default execution backend. It walks the bound tree node by node, evaluating each expression and propagating results through the tree. Every `BoundNodeKind` has a corresponding evaluation method.

## Dispatch

The entry point `Evaluate(BoundExpr)` dispatches on `BoundNodeKind` via a switch expression covering every node kind. Each kind maps to a specific evaluation method. The dispatch is a flat switch — no virtual method calls, no visitor pattern overhead.

## Fast Path vs Fallback

Binary and unary operators have a two-tier dispatch:

### Fast path (NumericDispatch)

When the binder has computed a `PromotedType` AND the runtime values' types match their static types, the evaluator routes directly to `NumericDispatch` — pre-built delegate tables keyed by `(Type, Type)` pairs:

```
Add: { (int,int)→(l,r)=>l+r, (long,long)→(l,r)=>l+r, (double,double)→(l,r)=>l+r, ... }
Subtract: { (int,int)→(l,r)=>l-r, ... }
Compare: { (int,int)→(l,r)=>l.CompareTo(r), ... }
```

This is how `1 + 2` evaluates without boxing, `dynamic`, or reflection. The binder pre-computes the promoted type (`int` for `int + int`), and the interpreter uses it as a direct table lookup key.

Checked arithmetic has separate delegate tables (`CheckedAddOps`, `CheckedSubtractOps`, `CheckedMultiplyOps`) that use `checked()` for integer types and standard operations for floating-point (per IEEE 754).

### Fallback path (Operators)

When types don't match the fast path (mixed types, nullables, `string + object`, `DateTime ± TimeSpan`, enum arithmetic, user-defined operators), the evaluator falls through to the general `Operators` class. This handles:

- String concatenation (any operand is `string`)
- `DateTime` arithmetic (`DateTime + TimeSpan`, `DateTime - DateTime`)
- Enum arithmetic (`Enum + int`, `Enum - Enum`, `~Enum`)
- Nullable three-valued logic (`bool? & bool?`, `bool? | bool?`)
- User-defined operators (`op_Addition`, `op_Subtraction`, etc.)
- Cross-type numeric promotion via `NumericDispatch.PromoteOperands`
- String repetition in Extended mode (`"ab" * 3`)
- Object merge in Extended mode (`new { A = 1 } + new { B = 2 }`)

Before entering the fallback path, `NumericPromotionRuntime.ApplyConstantNumericPromotion` handles ECMA-334 §10.2.11 — if one operand is a literal constant, it may be implicitly promoted (e.g., `int` 0 to `uint`).

## Control Flow

### ControlFlowSignal

Control flow uses a signal propagation pattern. `break`, `continue`, `return`, `goto`, `goto case`, and `goto default` produce `ControlFlowSignal` values that propagate upward through the evaluation stack:

| Signal | Produced by | Consumed by |
|--------|------------|-------------|
| `Return(value)` | `return expr;` | Engine entry point, lambda invocation |
| `Break` | `break;` | Innermost loop or switch |
| `Continue` | `continue;` | Innermost loop |
| `GotoCase(value)` | `goto case value;` | Switch statement |
| `GotoDefault` | `goto default;` | Switch statement |
| `Goto(label)` | `goto label;` | Block containing the target label |

Signals are NOT exceptions — they're ordinary return values of type `ControlFlowSignal`. Every intermediate construct (blocks, if-statements, try-catch) checks for signals and propagates them. Unwrapping happens at three boundaries:

1. **Engine entry point** (`AlderEngine.UnwrapControlFlowSignal`): Extracts the return value
2. **Lambda invocation** (`MethodInvoker.InvokeLambda`): Extracts the return value for the lambda's caller
3. **Compiled root** (`BoundExpressionEmitter.EmitUnwrapSignal`): Emits IL to extract the return value

### Loop Evaluation

Each loop creates a child context (`_context.CreateChild()`) for the iteration scope. The child context is cleared between iterations via `ClearScope()` — this is cheaper than creating a new context each time.

Loops check execution constraints on each iteration:
- `ExecutionRuntime.CheckExecutionConstraints` — statement count and timeout
- `ExecutionRuntime.CheckLoopIterationConstraint` — per-loop iteration count

The `_loopDepth` and `_breakContextDepth` counters track nesting for `break`/`continue` validation.

### Goto and Labels

`goto label` in a block produces a `Goto("label")` signal. The block executor (`ExecuteStatementBlock`) handles it: when a goto signal is received, it scans the statement list for a `BoundLabelExpr` with the matching name and restarts execution from that point.

## Member Access

### Resolved member access

When the binder produced a `BoundPropertyAccessExpr` or `BoundFieldAccessExpr`, the interpreter has the `PropertyInfo`/`FieldInfo` and calls `GetValue` directly:

- Properties: `property.GetValue(target)` (with AOT metadata check first)
- Fields: `field.GetValue(target)` (with AOT metadata check first)
- Null-conditional (`?.`): If the target is null, returns null without accessing the member

### Dynamic member access

When the binder produced `BoundDynamicMemberAccessExpr`, the interpreter uses `MemberAccess.ResolveMember` which:
1. Checks AOT metadata (`IAotTypeMetadata.TryGetProperty/TryGetField`)
2. Checks `TypeMetadataProvider` cache for property/field
3. For `ExpandoObject`, reads from the `IDictionary<string, object?>` interface
4. Extended mode: checks `DateArithmeticSugar.TryResolveTimeSpanUnit` for numeric.days/hours/etc.

## Method Invocation

### Resolved calls

`BoundResolvedCallExpr` carries the selected `MethodInfo` from bind-time overload resolution. The interpreter:
1. Evaluates all arguments
2. Converts lambda arguments to delegates via `LambdaDelegateConverter`
3. Handles `out` parameters by creating wrapper variables
4. Invokes the method directly

### Dynamic calls

`BoundDynamicCallExpr` runs full runtime resolution:
1. Identifies the target (function, module method, instance method, extension method)
2. Runs overload resolution via `MethodInvoker`
3. Handles generic type inference for extension methods
4. Converts lambdas to delegates
5. Invokes the selected method

## AOT Integration

Before any reflection-based member access or method invocation, the interpreter checks for AOT metadata:

```
if config.TryGetAotMetadata(type, out var metadata)
    if metadata.TryGetProperty(name, instance, out var value)
        return value;
// fallback to reflection
```

The AOT check walks the type hierarchy — if metadata exists for a base type, it's used. This means registering `[AlderRegistered(typeof(List<int>))]` covers access to members inherited from `IList<int>`, `ICollection<int>`, etc.

## Tracing

When `EvaluateWithTrace` is called, the interpreter receives an `EvaluationTracer`. Before each node evaluation, `_tracer.Push(expr)` records the node. After evaluation, `_tracer.Pop(result)` records the result. On error, `_tracer.PopError(exception)` records the failure.

The tracer builds a `TraceNode` tree that mirrors the evaluation order. The trace uses the security-only pipeline (no constant folding or dead branch elimination) so every node is visible.

## Context Management

The `_context` field on `BoundEvaluator` points to the current scope. It changes during evaluation:
- Block entry → `_context = _context.CreateChild()`
- Loop iteration → child context reused, cleared via `ClearScope()`
- `for` loop → two child contexts (initializer scope + body scope)

The evaluator saves and restores `_context` around scope changes using local variables, ensuring that scope exits always restore the previous context even on exceptions.

## Boxed Constants

Frequently-returned values (`true`, `false`, `0`, `1`, `null`) use pre-allocated boxed instances from `BoxedConstants` to avoid repeated boxing allocations on the hot path.
