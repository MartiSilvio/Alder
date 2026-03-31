---
title: "Security"
description: "Sandbox permissions, type blocking, execution limits — evaluating untrusted code safely"
sidebar:
  order: 1
---

Alder evaluates user-supplied C# code safely in production environments — multi-tenant SaaS, rule engines, configuration-driven business logic, interactive REPLs. The security model provides three layers of control:

**Operation permissions** — eight flags controlling what evaluated code can do: call methods, read properties, assign variables, construct objects, access static members, write to indexers. Three presets (`Trusted`, `Safe`, `Strict`) cover common scenarios, and `SandboxOptions` is a record with `init` properties for full customization via `with` expressions.

**Type and namespace blocking** — a four-layer evaluation chain (hard-denied → trusted → denied → default) that controls which .NET types the code can access. Default deny lists cover file I/O, networking, process execution, reflection, threading, and interop. Custom trusted/denied sets provide fine-grained overrides.

**Execution limits** — caps on statement count, loop iterations, wall-clock time, and collection size that prevent denial-of-service from runaway or malicious code.

Operation permissions and type blocking are enforced as a bound tree pipeline pass before execution begins. A blocked expression produces a diagnostic, not a partially-executed side effect. Execution limits are enforced at runtime during evaluation.

## Deep-Dive

| Page | What it covers |
|------|---------------|
| [Security Model](sandbox.md) | Sandbox presets, permission flags, type blocking, namespace blocking, execution limits, reflection blocking, pipeline pass architecture |
