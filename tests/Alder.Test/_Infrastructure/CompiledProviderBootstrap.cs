namespace Alder.Test._Infrastructure;

public enum CompilationMode { Interpreted, Compiled, CompiledFec }

internal static class TestEngineFactory
{
    internal static AlderEngine Create(CompilationMode mode, AlderOptions? options = null)
    {
        var opts = options ?? AlderOptions.Default;
        return mode switch
        {
            CompilationMode.Compiled => new AlderEngine(opts.UseCompiler()),
            CompilationMode.CompiledFec => new AlderEngine(opts.UseCompiler(new FastExpressionCompilerAdapter())),
            _ => new AlderEngine(opts)
        };
    }
}
