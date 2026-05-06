namespace Alder.Compiled;

public static partial class AlderLinqExtensions
{
    private static AlderEngine GetGlobalEngine()
    {
        var engine = AlderEval.GetEngine();
        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call AlderEval.Configure(o => o.UseCompiler()) before using WhereDynamic, SelectDynamic, etc.");
        return engine;
    }

    private static AlderEngine ValidateEngine(AlderEngine engine)
    {
        if (!engine.HasCompiler)
            throw new InvalidOperationException(
                "LINQ Dynamic methods require a compiler. " +
                "Call engine options UseCompiler() before using WhereDynamic, SelectDynamic, etc.");
        return engine;
    }

    private static AlderEngine ResolveEngine(AlderEngine? engine) =>
        engine is null ? GetGlobalEngine() : ValidateEngine(engine);

    private static Dictionary<string, object?>? BuildPositionalVars(object?[] variables) =>
        variables.Length == 0 ? null : VariableBindingProjector.BuildPositionalVariables(variables);

    private static IReadOnlyList<KeyValuePair<string, object?>>? AsOrderedValues(Dictionary<string, object?>? vars) =>
        vars == null ? null : [.. vars];

    private static IReadOnlyList<KeyValuePair<string, object?>>? BuildOrderedValues(object?[] variables) =>
        AsOrderedValues(BuildPositionalVars(variables));
}
