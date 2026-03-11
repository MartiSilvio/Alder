# CsEval Diagnostics Contract

## Overview

CsEval reports diagnostics through:

- `CsEvalException` for throw-based APIs (`Evaluate`, `ParseAsExpression`, etc.)
- `CsEvalDiagnostic` for non-throwing APIs (`TryValidate`, `TryParseAsExpression`)

Diagnostics use either:

- Roslyn-aligned `CSxxxx` codes when parity exists
- CsEval-specific `CSEVxxxx` codes for engine-only cases

## TryValidate Behavior

`TryValidate` now aggregates semantic diagnostics in one pass:

- Statement-level binding diagnostics (via bound-node binder pipeline)
- Unresolved identifier diagnostics with token locations

Each diagnostic includes:

- `Code` (`DiagnosticCode`)
- `Message`
- `Line` / `Column` (when available)
- `SpanStart` / `SpanLength` (when available)

## ParseAsExpression Behavior

`ParseAsExpression` and `TryParseAsExpression` return coded diagnostics for expression-tree limitations.

Examples:

- `CSEV0004` for unsupported expression-tree nodes
- `CSEV0005` for unsupported call shapes (for example named arguments)
- `CSEV0006`-`CSEV0009` for ParseAsExpression contract mismatches

## Stability Notes

- Diagnostic codes are the stable contract.
- Message text may evolve for clarity; consumers should key on `Code`.

## Language Mode Diagnostics

Extended-only syntax in `LanguageMode.Standard` is reported using:

- `CsEvalLanguageModeException`
- `DiagnosticCode.CSEV0002` (`Feature '{0}' is not available in Standard mode...`)

Common `FeatureName` values for explicit parser gates:

- `let-in`
- `if-expression`
- `comprehension`

Other Extended-only runtime conveniences (for example scope functions and aggregate built-ins) are rejected through normal identifier/member resolution diagnostics when used in Standard mode.

## Exception Parity Normalization (Test Contract)

Parity tests normalize exception identity as:

- `CSxxxx:ExceptionType` or `CSEVxxxx:ExceptionType` when an error code exists
- otherwise `ExceptionType`

This keeps CsEval-vs-Roslyn parity assertions stable even when message text differs.

## Tracing API Error Behavior

`EvaluateWithTrace(...)` does not introduce a separate diagnostic channel. It returns:

- `EvaluationTraceResult.Result`
- `EvaluationTraceResult.Steps`

and propagates evaluation/binding/parser exceptions with the same diagnostic behavior as `Evaluate(...)`.
