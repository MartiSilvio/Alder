using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compilation;

internal interface ICompiledProvider
{
    CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null);
    CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null, CsEvalContext? typeHintContext = null);
}

internal static class CompiledProviderRegistry
{
    private static readonly object Sync = new();
    private static ICompiledProvider? _provider;

    private const string MissingProviderReason =
        "Compiled provider is not registered. Add package 'CsEval.Compiled' and call CsEvalCompiledExtensions.RegisterCompiledProvider() or CsEvalOptions.UseCompiled().";

    internal static void Register(ICompiledProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Sync)
            _provider = provider;
    }

    internal static CompiledExpressionInfo GetOrCompile(
        string expressionText,
        Expr ast,
        ExpressionCache cache,
        CsEvalOptions? options = null)
    {
        var provider = GetProvider();
        return provider == null
            ? new CompiledExpressionInfo(null, false, MissingProviderReason)
            : provider.GetOrCompile(expressionText, ast, cache, options);
    }

    internal static CompiledExpressionInfo TryCompile(
        Expr ast,
        CsEvalOptions? options = null,
        CsEvalContext? typeHintContext = null)
    {
        var provider = GetProvider();
        return provider == null
            ? new CompiledExpressionInfo(null, false, MissingProviderReason)
            : provider.TryCompile(ast, options, typeHintContext);
    }

    private static ICompiledProvider? GetProvider() => _provider;
}
