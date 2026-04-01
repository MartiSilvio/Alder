# Security

Alder evaluates user-supplied C# code safely in production environments. The security model provides three layers of control: operation permissions, type and namespace blocking, and execution limits.

## How Enforcement Works

**Operation permissions and type blocking** are enforced as a bound tree pipeline pass that runs **before execution begins**. The entire expression tree is validated against the configured policy. If any node violates the policy, evaluation never starts — a blocked expression produces a diagnostic, not a partially-executed side effect.

**Execution limits** (statement count, loop iterations, timeout, collection size) are enforced at runtime during evaluation, since they depend on the dynamic behavior of the expression.

## Sandbox Presets

| Permission | `Trusted()` | `Safe()` | `Strict()` |
|------------|:-----------:|:--------:|:----------:|
| Method calls | yes | **no** | **no** |
| Property read (instance) | yes | yes | yes |
| Static property read | yes | yes | yes |
| Static field read | yes | yes | yes |
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

## Full Documentation

For the complete security model — permission details, type blocking chains, namespace blocking, reflection blocking, execution limits, and architectural details — see [docs/security/sandbox.md](docs/security/sandbox.md).

## Reporting Vulnerabilities

If you discover a security vulnerability in Alder, please report it privately via GitHub Security Advisories rather than opening a public issue.
