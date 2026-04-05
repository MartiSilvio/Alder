namespace Alder.Test._Infrastructure;

public enum CompilationMode { Interpreted, Compiled, CompiledFec }

internal static class TestEngineFactory
{
    internal static AlderEngine Create(CompilationMode mode, Action<AlderOptions>? configure = null)
    {
        return new AlderEngine(o =>
        {
            configure?.Invoke(o);
            switch (mode)
            {
                case CompilationMode.Compiled:
                    o.UseCompiler();
                    break;
                case CompilationMode.CompiledFec:
                    o.UseCompiler(new FastExpressionCompilerAdapter());
                    break;
            }
        });
    }

    internal static AlderEngine Create(CompilationMode mode, LanguageMode lang)
        => Create(mode, o => o.LanguageMode = lang);
}
