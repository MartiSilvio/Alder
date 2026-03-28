# Security

Alder evaluates user-supplied C# expressions safely in production environments. The security model provides three layers of control: operation permissions, type and namespace blocking, and execution limits.

## Sandbox Presets

| Permission | `Trusted()` | `Safe()` | `Strict()` |
|------------|:-----------:|:--------:|:----------:|
| Method calls | yes | **no** | **no** |
| Property read (instance) | yes | yes | yes |
| Static property/field read | yes | yes | yes |
| Variable assignment | yes | yes | **no** |
| Property write | yes | yes | **no** |
| Index write | yes | yes | **no** |
| Object construction | yes | **no** | **no** |

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

## Type and Namespace Blocking

A four-layer evaluation chain controls which .NET types expressions can access:

1. **Hard-denied**: `AlderEngine`, `AlderOptions`, `AlderExpression` are always blocked
2. **Trusted**: Types in `TrustedTypes` or `TrustedNamespaces` are always allowed
3. **Denied**: Types in `DeniedTypes` or `DeniedNamespaces` are blocked
4. **Default**: Everything else is allowed

Default denied namespaces include `System.IO`, `System.Net`, `System.Diagnostics`, `System.Reflection`, `System.Threading`, `System.Runtime.InteropServices`, and others covering file I/O, networking, process execution, reflection, threading, and interop.

## Execution Limits

| Constraint | Diagnostic |
|-----------|------------|
| `MaxStatements` | `ALDR0200` |
| `MaxTimeout` | `ALDR0201` |
| `MaxLoopIterations` | `ALDR0203` |

## Complete Validation Before Execution

Alder validates the entire expression tree before any execution begins. Every member access, method call, constructor invocation, and assignment in the expression is checked against the security policy as a pre-execution pipeline pass. If any operation violates the policy, evaluation never starts.

This is a fundamental guarantee: a blocked expression produces a diagnostic, never a partially-executed side effect. There is no scenario where an expression writes to a file, makes a network call, or mutates state before a security violation is detected later in the same expression. The answer is either a security error or a complete, safe result.

## Full Documentation

For the complete security model documentation, see [docs/security/](docs/security/index.md).

## Reporting Vulnerabilities

If you discover a security vulnerability in Alder, please report it privately via [GitHub Security Advisories](../../security/advisories/new) rather than opening a public issue.
