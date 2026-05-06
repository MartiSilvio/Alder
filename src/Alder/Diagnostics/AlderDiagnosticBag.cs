namespace Alder.Diagnostics;

/// <summary>
/// Lightweight internal collector for structured diagnostics during front-end processing.
/// </summary>
internal sealed class AlderDiagnosticBag
{
    private List<AlderDiagnostic>? _diagnostics;

    internal bool IsEmpty => _diagnostics is null || _diagnostics.Count == 0;

    internal void Add(AlderDiagnostic diagnostic)
    {
        (_diagnostics ??= []).Add(diagnostic);
    }

    internal void Add(Exception exception)
    {
        Add(AlderDiagnostic.FromException(exception));
    }

    internal void AddRange(IEnumerable<AlderDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            Add(diagnostic);
    }

    internal IReadOnlyList<AlderDiagnostic> ToReadOnly() =>
        _diagnostics ?? (IReadOnlyList<AlderDiagnostic>)[];
}
