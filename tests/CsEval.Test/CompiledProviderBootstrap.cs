using CsEval.Compiled;

namespace CsEval.Test;

public enum CompilationMode { Interpreted, Compiled }

internal static class TestEngineFactory
{
    internal static CsEvalEngine Create(CompilationMode mode, CsEvalOptions? options = null)
    {
        var opts = options ?? CsEvalOptions.Default;
        return new CsEvalEngine(mode == CompilationMode.Compiled ? opts.UseCompiler() : opts);
    }
}
