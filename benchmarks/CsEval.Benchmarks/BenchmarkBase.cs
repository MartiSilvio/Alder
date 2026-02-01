using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Benchmarks;

public abstract class BenchmarkBase
{
    protected static readonly ScriptOptions RoslynOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Collections.Generic", "System.Linq")
        .WithLanguageVersion(LanguageVersion.CSharp12);

    protected CsEvalEngine InterpretedEngine = null!;
    protected CsEvalEngine CompiledEngine = null!;

    protected void SetupEngines()
    {
        InterpretedEngine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.Interpreted });
        CompiledEngine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = CompilationMode.StrictCompiled });
    }

    protected static async Task<object?> EvaluateRoslynAsync(string code)
    {
        return await CSharpScript.EvaluateAsync<object>(code, RoslynOptions);
    }

    protected static Script<object> CreateRoslynScript(string code)
    {
        return CSharpScript.Create<object>(code, RoslynOptions);
    }
}
