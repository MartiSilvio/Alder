namespace Alder.Test._Infrastructure;

public enum CompilationMode { Interpreted, Compiled }

public static class CompilationModeFixtures
{
    public static IEnumerable<CompilationMode> AllModes
    {
        get
        {
            yield return CompilationMode.Interpreted;
            yield return CompilationMode.Compiled;
        }
    }

    public static IEnumerable<TestFixtureData> All
    {
        get
        {
            foreach (var mode in AllModes)
                yield return new TestFixtureData(mode);
        }
    }
}

internal static class TestEngineFactory
{
    internal static AlderEngine Create(CompilationMode mode, Action<AlderOptions>? configure = null)
    {
        return new AlderEngine(options =>
        {
            configure?.Invoke(options);
            switch (mode)
            {
                case CompilationMode.Interpreted:
                    break;
                case CompilationMode.Compiled:
                    options.UseCompiler();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        });
    }

    internal static AlderEngine Create(CompilationMode mode, LanguageMode lang)
        => Create(mode, options => options.LanguageMode = lang);
}
