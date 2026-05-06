using Alder.Binding;
using Alder.Diagnostics;
using Alder.Parsing;
using Binder = Alder.Binding.Binder;

namespace Alder;

public sealed partial class AlderEngine
{
    /// <summary>
    /// Validates an expression for syntax and binding errors without evaluating it.
    /// </summary>
    /// <param name="expression">Expression source to validate.</param>
    /// <param name="diagnostics">When validation fails, the list of diagnostics; otherwise, an empty list.</param>
    /// <returns><c>true</c> if the expression is valid; otherwise, <c>false</c>.</returns>
    public bool TryValidate(string expression, out IReadOnlyList<AlderDiagnostic> diagnostics)
    {
        ThrowIfDisposed();
        var parseAttempt = ParseCore(expression);
        if (!parseAttempt.Success)
        {
            diagnostics = parseAttempt.Diagnostics;
            return false;
        }

        try
        {
            var context = GetOrCreateContext();
            var binder = new Binder(new Text.SourceText(expression));
            var bindingContext = new BindingContext(context);
            var validationDiagnostics = new AlderDiagnosticBag();
            validationDiagnostics.AddRange(binder.CollectDiagnostics(parseAttempt.Expression!.Ast, bindingContext));

            var collector = new IdentifierOccurrenceCollector();
            collector.Collect(parseAttempt.Expression!.Ast);
            foreach (var identifier in collector.GetUnboundTokens(_config.Comparer))
            {
                var name = identifier.Lexeme;
                if (context.TryGet(name, out _)) continue;
                if (context.Functions.ContainsKey(name)) continue;
                if (context.Modules.ContainsKey(name)) continue;
                if (context.TypeResolver.IsNamespaceOrPrefix(name)) continue;
                if (context.TypeResolver.TryResolveType(name) != null) continue;

                var message = $"{DiagnosticDescriptors.NameNotInContext.Code.ToDiagnosticId()}: {DiagnosticDescriptors.NameNotInContext.FormatMessage(name)}";
                validationDiagnostics.Add(new AlderDiagnostic(
                    DiagnosticSeverity.Error, message, DiagnosticDescriptors.NameNotInContext.Code,
                    identifier.Span, identifier.Line, identifier.Column));
            }

            var deduplicated = DeduplicateDiagnostics(validationDiagnostics.ToReadOnly());
            if (deduplicated.Count > 0)
            {
                diagnostics = deduplicated;
                return false;
            }
        }
        catch (Exception ex) when (!ShouldRethrowTryApiException(ex))
        {
            diagnostics = [AlderDiagnostic.FromException(ex)];
            return false;
        }

        diagnostics = [];
        return true;
    }

    private static IReadOnlyList<AlderDiagnostic> DeduplicateDiagnostics(IReadOnlyList<AlderDiagnostic> diagnostics)
    {
        if (diagnostics.Count <= 1)
            return diagnostics;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<AlderDiagnostic>(diagnostics.Count);
        foreach (var diagnostic in diagnostics)
        {
            var key = $"{diagnostic.Code}|{diagnostic.Span}|{diagnostic.Message}";
            if (!seen.Add(key))
                continue;
            result.Add(diagnostic);
        }

        return result;
    }
}
