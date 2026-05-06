# Security Policy

## Reporting a Vulnerability

Please do not open a public issue for a suspected vulnerability.

If this repository is private, contact the repository maintainer privately before sharing details more broadly.

If this repository is public and GitHub private vulnerability reporting is enabled, use the **Report a vulnerability** flow from the repository's Security tab.

Include:

- affected version, package, or commit
- minimal reproduction
- expected and actual behavior
- whether the expression source is trusted, user-authored, or tenant-authored
- `SecurityOptions`, `ExecutionConstraints`, registered functions, modules, types, namespaces, and extension methods used by the host
- whether the interpreted backend, compiled backend, Dynamic LINQ, or AOT generated dispatch path is involved

## Scope

Security reports are especially relevant when they involve:

- security policy bypasses
- access to denied types, namespaces, reflection metadata, file I/O, networking, process execution, or other blocked host capabilities
- execution-limit bypasses or denial-of-service behavior
- differences between interpreted, compiled, Dynamic LINQ, or generated-dispatch behavior that change authority
- unsafe exposure of host APIs through Alder defaults

Configuration that deliberately registers broad host APIs or trusted types may still be risky, but it is usually a host integration issue rather than an Alder vulnerability.

## Security Model

Alder evaluates expressions in-process. The security policy controls host authority and execution guardrails inside that process. Operating-system isolation, process isolation, and separate runtime isolation remain host responsibilities.

For the technical model, see [docs/operations/security-model.md](docs/operations/security-model.md).

## Supported Versions

Security fixes are provided for the latest released version. If no release covers the affected code yet, report against the current `master` branch or the affected commit.
