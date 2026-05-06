namespace Alder.Test._Infrastructure;

public enum CompilationMode { Interpreted, Compiled }

public static class CompilationModeFixtures
{
    public static IEnumerable<CompilationMode> AllModes
    {
        get
        {
            yield return CompilationMode.Interpreted;
#if NET8_0_OR_GREATER
            yield return CompilationMode.Compiled;
#endif
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
#if !NET8_0_OR_GREATER
        if (mode != CompilationMode.Interpreted)
            throw new NotSupportedException("Compiled test mode requires net8.0 or greater.");
#endif
        return new AlderEngine(o =>
        {
            configure?.Invoke(o);
#if NET8_0_OR_GREATER
            switch (mode)
            {
                case CompilationMode.Interpreted:
                    break;
                case CompilationMode.Compiled:
                    o.UseCompiler();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
#endif
        });
    }

    internal static AlderEngine Create(CompilationMode mode, LanguageMode lang)
        => Create(mode, o => o.LanguageMode = lang);
}
