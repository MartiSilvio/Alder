namespace CsEval.Test._Infrastructure;

public enum CompilationMode { Interpreted, Compiled, CompiledFec }

internal static class TestEngineFactory
{
    internal static CsEvalEngine Create(CompilationMode mode, CsEvalOptions? options = null)
    {
        var opts = options ?? CsEvalOptions.Default;
        return mode switch
        {
            CompilationMode.Compiled => new CsEvalEngine(opts.UseCompiler()),
            CompilationMode.CompiledFec => new CsEvalEngine(opts.UseCompiler(new FastExpressionCompilerAdapter())),
            _ => new CsEvalEngine(opts)
        };
    }
}
