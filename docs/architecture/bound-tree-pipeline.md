---
title: "Bound Tree Pipeline"
description: "Optimization passes — security validation, constant folding, dead branch elimination, conversion insertion"
sidebar:
  order: 9
---

After the binder produces a bound tree, it passes through a configurable pipeline of transformation passes before execution. Each pass implements `IBoundTreePass` — it receives a `BoundExpr`, transforms it, and returns a (possibly modified) `BoundExpr`.

## Pipeline Configurations

Two pipeline configurations exist:

| Pass | Interpretation | Compilation | Tracing |
|------|:-:|:-:|:-:|
| SecurityValidationPass | 1st | 1st | 1st (only pass) |
| ConstantFoldingPass | 2nd | 2nd | — |
| DeadBranchEliminationPass | 3rd | 3rd | — |
| ConversionInsertionPass | — | 4th | — |

The tracing pipeline runs security validation only — constant folding and dead branch elimination are skipped so the tracer sees every node, including constant subexpressions and unreachable branches.

## SecurityValidationPass

The first pass in every pipeline. Walks the entire bound tree iteratively (using a stack, not recursion) and checks every node against the configured `SecurityPolicy`.

The pass always walks the full tree — there is no fast-path skip.

**Validation**: Each node is checked:

| Node type | Check |
|-----------|-------|
| `BoundObjectCreationExpr` | `AllowConstruction` + `IsTypeAllowed` |
| `BoundResolvedCallExpr` | `AllowMethodCalls` (skipped for module calls and extension methods) + `IsTypeAllowed` on declaring type |
| `BoundPropertyAccessExpr`, `BoundFieldAccessExpr` | `AllowPropertyRead` / `AllowStaticPropertyRead` / `AllowStaticFieldRead` + `IsTypeAllowed` |
| `BoundDynamicMemberAccessExpr` | `AllowPropertyRead` |
| `BoundAssignExpr`, `BoundCompoundAssignExpr`, `BoundNullCoalesceAssignExpr`, `BoundIncrementDecrementExpr` | `AllowAssignment` |
| `BoundMemberAssignExpr` and variants | `AllowPropertySet` |
| `BoundIndexAssignExpr` and variants | `AllowIndexSet` |

If any check fails, an `AlderException` is thrown with an `ALDR01xx` diagnostic code. Evaluation never begins — there is no partial execution.

## ConstantFoldingPass

Evaluates compile-time constant subexpressions, replacing them with literal values. This is a `BoundExprRewriter` that visits the tree bottom-up.

### What gets folded

| Expression | Folded to |
|-----------|-----------|
| `1 + 2` | `3` (int literal) |
| `-42` | `-42` (int literal) |
| `!true` | `false` |
| `~0xFF` | `-256` |
| `"hello" + " " + "world"` | `"hello world"` |
| `true && false` | `false` |
| `true \|\| false` | `true` |
| `true ? "yes" : "no"` | `"yes"` |
| `1 < 2` | `true` |

### What doesn't fold

- Expressions involving variables or function calls
- Division by zero (would throw — preserved for runtime)
- Operations that throw for any reason (folding is wrapped in try-catch, failure preserves the original node)
- Expressions with `BoundType.Unknown` operands

### Constant promotion in folding

When folding `uint + int_constant`, the standard `NumericDispatch` promotes to `long`. But ECMA-334 constant expressions preserve the unsigned type when the constant is non-negative. `ApplyConstantPromotion` corrects this — if the result is `long` but one operand is `uint` and the other is a non-negative `int`, the result is cast back to `uint`.

### Ternary folding

When the condition of a ternary expression is a constant `bool`, the entire ternary is replaced with the chosen branch. If the branches have different types, a `BoundCastExpr` is inserted to preserve the ternary's result type (which is the common type of both branches, not the chosen branch's type).

## DeadBranchEliminationPass

Removes unreachable `if` branches when the condition is a constant `bool`. This is a `BoundExprRewriter` that only visits `if` statements.

| Condition | Result |
|-----------|--------|
| `if (true) { then } else { else }` | Replaced with `then` block |
| `if (false) { then } else { else }` | Replaced with `else` block |
| `if (false) { then }` (no else) | Replaced with a void no-op literal |

This pass runs after constant folding, so conditions like `if (1 > 0)` are already reduced to `if (true)` before this pass sees them.

## ConversionInsertionPass

Compilation-only. Inserts explicit `BoundCastExpr` nodes for binary operands with mismatched numeric types.

The interpreter handles numeric promotion at runtime via `NumericDispatch.PromoteOperands`. But the LINQ expression tree compiler needs exact type matching — `Expression.Add(Expression<int>, Expression<long>)` is invalid. This pass inserts the conversion nodes that the compiler needs.

### What it does

For each `BoundBinaryExpr` where the left and right types differ and both are numeric:
1. Compute the promoted type via `TypeHelpers.TryGetBinaryNumericPromotionType`
2. If either operand's type differs from the promoted type, wrap it in a `BoundCastExpr`

### What it skips

- Strict equality (`===`, `!==`) — promotion would defeat the purpose
- Operands typed as `object` — runtime dispatch handles these
- Same-type operands — no conversion needed

## Pass Architecture

All optimization passes extend `BoundExprRewriter`, which implements the visitor pattern over bound nodes. The rewriter traverses the tree bottom-up (children first, then parent), producing a new tree where modified nodes are replaced and unchanged nodes are reused.

The security pass is different — it extends `IBoundTreePass` directly and walks the tree iteratively with an explicit stack. It doesn't transform the tree, only validates it.

The pipeline itself is a simple sequential chain:

```csharp
internal BoundExpr Execute(BoundExpr tree, PipelineContext context)
{
    foreach (var pass in _passes)
        tree = pass.Execute(tree, context);
    return tree;
}
```

`PipelineContext` carries the `SecurityPolicy` and `CancellationToken`.
