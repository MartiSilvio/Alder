using DynamicExpresso;
using Flee.PublicTypes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using CsEval.Compiled;

namespace CsEval.Benchmarks;

public enum CompilationMode { Interpreted, Compiled, CompiledFec }

public abstract class BenchmarkBase
{
    protected static readonly ScriptOptions RoslynOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Collections.Generic", "System.Linq")
        .WithLanguageVersion(LanguageVersion.CSharp12);

    protected CsEvalEngine InterpretedEngine = null!;
    protected CsEvalEngine CompiledEngine = null!;
    protected CsEvalEngine CompiledFecEngine = null!;

    public static CsEvalEngine CreateEngine(
        CompilationMode mode,
        BenchmarkGlobalData globals,
        LanguageMode languageMode = LanguageMode.Standard)
    {
        var opts = CsEvalOptions.Default with { LanguageMode = languageMode };
        opts = mode switch
        {
            CompilationMode.Compiled => opts.UseCompiler(),
            CompilationMode.CompiledFec => opts.UseCompiler(new FastExpressionCompilerAdapter()),
            _ => opts
        };
        var engine = new CsEvalEngine(opts);
        ApplyGlobals(engine, globals);
        return engine;
    }

    protected void SetupEngines(BenchmarkGlobalData globals)
    {
        InterpretedEngine = CreateEngine(CompilationMode.Interpreted, globals);
        CompiledEngine = CreateEngine(CompilationMode.Compiled, globals);
        CompiledFecEngine = CreateEngine(CompilationMode.CompiledFec, globals);
    }

    protected static void ApplyGlobals(CsEvalEngine engine, BenchmarkGlobalData globals)
    {
        engine.SetVariable<int>("x", globals.X);
        engine.SetVariable<int>("y", globals.Y);
        engine.SetVariable<int>("z", globals.Z);
        engine.SetVariable<string>("text", globals.Text);
        engine.SetVariable<int>("value", globals.Value);
        engine.SetVariable<List<int>>("numbers", globals.Numbers);
        engine.SetVariable<List<Order>>("orders", globals.Orders);
    }

    public static Interpreter CreateDynamicExpressoInterpreter(BenchmarkGlobalData globals)
    {
        var interpreter = new Interpreter()
            .Reference(typeof(Math))
            .Reference(typeof(Enumerable));

        interpreter.SetVariable("x", globals.X);
        interpreter.SetVariable("y", globals.Y);
        interpreter.SetVariable("z", globals.Z);
        interpreter.SetVariable("text", globals.Text);
        interpreter.SetVariable("value", globals.Value);
        interpreter.SetVariable("numbers", globals.Numbers);
        interpreter.SetVariable("orders", globals.Orders);

        return interpreter;
    }

    public static ExpressionContext CreateFleeContext(BenchmarkGlobalData globals)
    {
        var context = new ExpressionContext();
        context.Imports.AddType(typeof(Math));
        context.Imports.AddType(typeof(Enumerable));

        context.Variables["x"] = globals.X;
        context.Variables["y"] = globals.Y;
        context.Variables["z"] = globals.Z;
        context.Variables["text"] = globals.Text;
        context.Variables["value"] = globals.Value;
        context.Variables["numbers"] = globals.Numbers;
        context.Variables["orders"] = globals.Orders;

        return context;
    }

    public static async Task<object?> EvaluateRoslynAsync(string code, BenchmarkGlobalData globals)
    {
        return await CSharpScript.EvaluateAsync<object>(code, RoslynOptions, globals, typeof(BenchmarkGlobalData));
    }

    internal static Script<object> CreateRoslynScript(string code)
    {
        return CSharpScript.Create<object>(code, RoslynOptions, typeof(BenchmarkGlobalData));
    }
}
