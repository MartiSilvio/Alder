# Security

Alder evaluates user-supplied C# expressions safely in production environments. The security model provides three layers of control: operation permissions, type and namespace blocking, and execution limits.

## Pre-Execution Validation

Security enforcement is a bound tree pipeline pass that runs **before execution begins**. The binder produces the semantic tree, and the `SecurityValidationPass` walks every node, checking each member access, method call, constructor invocation, and assignment against the configured policy. If any node violates the policy, evaluation never starts and an `AlderException` with an `ALDR01xx` diagnostic is thrown.

A blocked expression produces a diagnostic, not a partially-executed side effect. The expression either fails validation entirely or executes completely within the configured policy.

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

`SandboxOptions` is a `record` with `init` properties. Customize any preset with `with`:

```csharp
o.Sandbox = SandboxOptions.Safe() with
{
    AllowMethodCalls = true,
    TrustedTypes = new HashSet<Type> { typeof(System.IO.MemoryStream) }
};
```

## Type and Namespace Blocking

A four-layer evaluation chain controls which .NET types expressions can access:

1. **Hard-denied**: `AlderEngine`, `AlderOptions`, `AlderExpression` are always blocked
2. **Trusted**: Types in `TrustedTypes` or `TrustedNamespaces` are always allowed
3. **Denied**: Types in `DeniedTypes` or `DeniedNamespaces` are blocked
4. **Default**: Everything else is allowed

Default denied namespaces cover file I/O (`System.IO`), networking (`System.Net`, `System.Net.Http`, `System.Net.Sockets`), process execution (`System.Diagnostics`), reflection (`System.Reflection`, `System.Reflection.Emit`), threading (`System.Threading`), interop (`System.Runtime.InteropServices`), and data access (`System.Data`).

Default denied types include `Activator`, `AppDomain`, `Console`, `Environment`, `GC`, `Process`, `Thread`, `ThreadPool`, and `Marshal`.

## Execution Limits

| Constraint | Diagnostic | Exception |
|-----------|------------|-----------|
| `MaxStatements` | `ALDR0200` | `AlderExecutionLimitException` |
| `MaxTimeout` | `ALDR0201` | `AlderExecutionLimitException` |
| `MaxLoopIterations` | `ALDR0203` | `AlderExecutionLimitException` |
| `MaxArrayLength` | `ALDR0202` | `AlderException` |

## Reflection Blocking

Access to reflection APIs on `Type` objects is blocked even in `Trusted()` mode. Calling `.GetMethods()`, `.GetProperties()`, or similar reflection methods on a `typeof()` result throws `ALDR0108`. This prevents expressions from discovering and invoking methods outside the sandbox.

## Full Documentation

For the complete security model with detailed permission descriptions, type blocking rules, and architectural details, see [docs/security/](docs/security/index.md).

## Reporting Vulnerabilities

If you discover a security vulnerability in Alder, please report it privately via [GitHub Security Advisories](../../security/advisories/new) rather than opening a public issue.
