# ECMA Subset Parity Test Design

## Goal
Create explicit, non-duplicative ECMA-334 subset compliance evidence for CsEval through tests, not prose.

## Scope
Expression-evaluator subset only (lexical, expressions/operators, conversions, control-flow statements supported by CsEval).
Out of scope: full-program constructs (types/members/declarations not evaluable as expressions/scripts in CsEval runtime contract).

## Design
- Add a compact machine-readable matrix linking ECMA rule IDs to concrete tests.
- Add validator tests that ensure each reference points to a real file and each required rule has at least one reference.
- Reuse existing parity and engine-only suites; add only gap tests where uncovered.

## Non-Goals
- Full-language coverage claims.
- Duplicating existing parity cases.
