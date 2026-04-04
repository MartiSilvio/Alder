Alder evaluates user-supplied C# code safely in production environments: multi-tenant SaaS, rule engines processing untrusted formulas, configuration-driven business logic, interactive REPLs. Three layers of control:

**Operation permissions**: eight flags controlling what evaluated code can do (call methods, read properties, assign variables, construct objects, access static members, write to indexers). Three presets (`Trusted`, `Safe`, `Strict`) cover common scenarios, customizable via `with` on the `SandboxOptions` record.

**Type and namespace blocking**: a four-layer evaluation chain (hard-denied → trusted → denied → default) that controls which .NET types the code can access. Default deny lists cover file I/O, networking, process execution, reflection, threading, and interop.

**Execution limits**: caps on statement count, loop iterations, wall-clock time, and collection size.

Operation permissions and type blocking are enforced as a bound tree pipeline pass **before execution begins**. A blocked expression produces a diagnostic, not a partially-executed side effect. Execution limits are enforced at runtime during evaluation.

## Full Reference

For the complete security model with permission matrices, type blocking rules, execution limits, and architectural details, see the [Security Model](sandbox.md) reference.

## Quick Start

```csharp
var engine = new AlderEngine(o =>
{
    o.Sandbox = SandboxOptions.Safe();
    o.Constraints = new ExecutionConstraints
    {
        MaxStatements = 10_000,
        MaxLoopIterations = 1_000,
        MaxTimeout = TimeSpan.FromSeconds(5)
    };
});
```
