After the binder produces a bound tree, it passes through a configurable pipeline of transformation passes before execution. Each pass receives a `BoundExpr`, transforms it, and returns a (possibly modified) `BoundExpr`. This page is the canonical reference for all pipeline passes.

## Pipeline Configurations

| Pass | Interpretation | Compilation | Tracing |
|------|:-:|:-:|:-:|
| SecurityValidationPass | 1st | 1st | 1st (only pass) |
| ConstantFoldingPass | 2nd | 2nd | - |
| DeadBranchEliminationPass | 3rd | 3rd | - |
| ConversionInsertionPass | - | 4th | - |

The tracing pipeline runs security validation only: constant folding and dead branch elimination are skipped so the tracer sees every node, including constant subexpressions and unreachable branches.

The pipeline is a simple sequential chain: each pass receives the tree from the previous pass and returns the result.

## SecurityValidationPass

The first pass in every pipeline. Walks the entire bound tree iteratively (using an explicit stack, not recursion) and checks every node against the configured security policy.

The pass always walks the full tree; there is no fast-path skip, ensuring consistent behavior regardless of sandbox configuration.

**What's checked:**

| Node type | Check |
|-----------|-------|
| Object creation | Construction permission + type allowed |
| Resolved method calls | Method call permission (skipped for module and extension methods) + declaring type allowed |
| Property/field access | Property read permission + type allowed |
| Dynamic member access | Property read permission |
| Assignment, compound assignment, increment/decrement | Assignment permission |
| Member assignment | Property set permission |
| Index assignment | Index set permission |

If any check fails, an `AlderException` is thrown with an `ALDR01xx` diagnostic code. Evaluation never begins; there is no partial execution.

## ConstantFoldingPass

Evaluates compile-time constant subexpressions, replacing them with literal values. Visits the tree bottom-up.

### What gets folded

| Expression | Folded to |
|-----------|-----------|
| `1 + 2` | `3` (int literal) |
| `-42` | `-42` (int literal) |
| `!true` | `false` |
| `~0xFF` | `-256` |
| `"hello" + " " + "world"` | `"hello world"` |
| `true && false` | `false` |
| `true ? "yes" : "no"` | `"yes"` |
| `1 < 2` | `true` |

### What doesn't fold

- Expressions involving variables or function calls
- Division by zero (would throw; preserved for runtime)
- Operations that fail for any reason (failure preserves the original node)
- Expressions with unknown-typed operands

### Constant promotion

When folding `uint + int_constant`, the standard numeric dispatch promotes to `long`. ECMA-334 constant expressions preserve the unsigned type when the constant is non-negative. The pass corrects this: if the result is `long` but one operand is `uint` and the other is a non-negative `int`, the result is cast back to `uint`.

### Ternary folding

When the condition is a constant `bool`, the entire ternary is replaced with the chosen branch. If the branches have different types, a cast is inserted to preserve the ternary's result type (the common type of both branches, not the chosen branch's type).

## DeadBranchEliminationPass

Removes unreachable `if` branches when the condition is a constant `bool`:

| Condition | Result |
|-----------|--------|
| `if (true) { then } else { else }` | Replaced with `then` block |
| `if (false) { then } else { else }` | Replaced with `else` block |
| `if (false) { then }` (no else) | Replaced with a void no-op |

This pass runs after constant folding, so conditions like `if (1 > 0)` are already reduced to `if (true)` before this pass sees them.

## ConversionInsertionPass

Compilation-only. Inserts explicit cast nodes for binary operands with mismatched numeric types.

The interpreter handles numeric promotion at runtime via its promote-operands logic. The compiler needs exact type matching. `Expression.Add(Expression<int>, Expression<long>)` is invalid in LINQ expression trees. This pass computes the promoted type and wraps operands as needed.

### What it skips

- Strict equality (`===`, `!==`). promotion would defeat the purpose
- Operands typed as `object`: runtime dispatch handles these
- Same-type operands: no conversion needed

## Pass Architecture

All optimization passes extend a base rewriter that implements the visitor pattern over bound nodes. The rewriter traverses bottom-up (children first, then parent), producing a new tree where modified nodes are replaced and unchanged nodes are reused.

The security pass is different: it walks the tree iteratively with an explicit stack and doesn't transform the tree, only validates it. This prevents stack overflow on deeply nested expressions.
