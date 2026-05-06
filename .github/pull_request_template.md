## Summary

Describe the change and why it is needed.

## Testing

List the commands run, for example:

```bash
dotnet build
dotnet test
```

## Checklist

- [ ] New language behavior has `.csx` corpus coverage under `tests/Alder.Test/TestData/`.
- [ ] `.roslyn.csx` siblings are used only when the Roslyn reference source must differ.
- [ ] Shared runtime behavior is covered for interpreted and compiled execution.
- [ ] AOT or generated dispatch impact is covered or explained.
- [ ] Documentation samples have matching `<!-- test: TestName -->` markers when applicable.
- [ ] Security policy, diagnostics, and provider/export boundaries are considered when affected.
