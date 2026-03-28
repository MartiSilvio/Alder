---
title: "Security"
description: "Sandbox permissions, type blocking, execution limits — evaluating untrusted expressions safely"
sidebar:
  order: 1
---

Alder evaluates user-supplied C# expressions safely in production environments — multi-tenant SaaS, rule engines, configuration-driven business logic, interactive REPLs. The security model provides three layers of control:

**Operation permissions** — eight flags controlling what expressions can do: call methods, read properties, assign variables, construct objects, access static members, write to indexers. Three presets (`Trusted`, `Safe`, `Strict`) cover common scenarios, and `SandboxOptions` is a record with `init` properties for full customization via `with` expressions.

**Type and namespace blocking** — a four-layer evaluation chain (hard-denied → trusted → denied → default) that controls which .NET types expressions can access. Default deny lists cover file I/O, networking, process execution, reflection, threading, and interop. Custom trusted/denied sets provide fine-grained overrides.

**Execution limits** — caps on statement count, loop iterations, and wall-clock time that prevent denial-of-service from runaway or malicious expressions. Limits throw `AlderExecutionLimitException` with the specific limit type, configured value, and actual value.

Security enforcement is a bound tree pipeline pass — the entire expression tree is validated before any execution begins. A blocked expression produces a diagnostic, never a partially-executed side effect.

## Deep-Dive

| Page | What it covers |
|------|---------------|
| [Security Model](sandbox.md) | Sandbox presets, permission flags, type blocking, namespace blocking, execution limits, reflection blocking, pipeline pass architecture |
