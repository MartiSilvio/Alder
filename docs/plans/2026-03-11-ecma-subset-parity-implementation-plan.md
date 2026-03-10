# ECMA Subset Parity Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Enforce ECMA-334 expression-subset compliance evidence via a compact rule matrix tied to explicit tests.

**Architecture:** Store rule-to-test mappings in JSON under `docs/`; validate mappings from NUnit by checking referenced files and required-rule coverage. Extend tests only for uncovered required rules to avoid duplication.

**Tech Stack:** C#, NUnit, .NET 8, existing CsEval parity harness and csx test data.

---

### Task 1: Add failing validator tests first

**Files:**
- Create: `tests/CsEval.Test/Compliance/EcmaSubsetCoverageTests.cs`

1. Write tests that load `docs/ecma-subset-expression-matrix.json` and fail if missing.
2. Run only this fixture to verify RED state.
3. Implement minimal loader/validators in the same file.
4. Re-run fixture expecting pass once matrix exists.

### Task 2: Create compact ECMA subset matrix

**Files:**
- Create: `docs/ecma-subset-expression-matrix.json`

1. Add required subset rules with `ruleId`, `title`, `status`, and `testRefs`.
2. Prefer references to existing tests and csx files.
3. Mark non-required/out-of-scope explicitly.

### Task 3: Fill only true gaps

**Files:**
- Create or modify only if gap found in Task 2 inventory.

1. Add minimal parity/explicit tests for required rules with no adequate references.
2. Keep new tests narrowly scoped and non-overlapping.

### Task 4: Verify

**Files:**
- N/A

1. Run the new compliance fixture.
2. Run targeted existing suites referenced by new matrix entries.
3. Run full `tests/CsEval.Test` suite if feasible.
