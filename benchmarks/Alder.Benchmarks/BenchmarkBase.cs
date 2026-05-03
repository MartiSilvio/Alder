using DynamicExpresso;
using Flee.PublicTypes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Alder.Compiled;

namespace Alder.Benchmarks;

public enum CompilationMode { Interpreted, Compiled, CompiledFec }

public abstract class BenchmarkBase
{
    protected static readonly ScriptOptions RoslynOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(BenchmarkData).Assembly)
        .AddImports("System", "System.Collections.Generic", "System.Linq", "Alder.Benchmarks")
        .WithLanguageVersion(LanguageVersion.CSharp12);

    protected AlderEngine InterpretedEngine = null!;
    protected AlderEngine CompiledEngine = null!;

    public static AlderEngine CreateEngine(
        CompilationMode mode,
        BenchmarkData data,
        LanguageMode languageMode = LanguageMode.Standard,
        Action<AlderOptions>? configure = null)
    {
        var opts = new AlderOptions { LanguageMode = languageMode };
        opts = mode switch
        {
            CompilationMode.Compiled => opts.UseCompiler(),
            CompilationMode.CompiledFec => opts.UseCompiler(new FastExpressionCompilerAdapter()),
            _ => opts
        };
        configure?.Invoke(opts);
        var engine = new AlderEngine(opts);
        ApplyVariables(engine, data);
        return engine;
    }

    public static void ApplyVariables(AlderEngine engine, BenchmarkData data)
    {
        engine.SetVariable<int>("x", data.X);
        engine.SetVariable<int>("y", data.Y);
        engine.SetVariable<int>("z", data.Z);
        engine.SetVariable<string>("text", data.Text);
        engine.SetVariable<int>("value", data.Value);
        engine.SetVariable<List<int>>("numbers", data.Numbers);
        engine.SetVariable<List<Order>>("orders", data.Orders);
        engine.SetVariable<List<Product>>("products", data.Products);
        engine.SetVariable<List<OrderLine>>("orderLines", data.OrderLines);
        engine.SetVariable<List<Employee>>("employees", data.Employees);
    }

    public static Interpreter CreateDynamicExpressoInterpreter(BenchmarkData data)
    {
        var interpreter = new Interpreter()
            .Reference(typeof(Math))
            .Reference(typeof(Enumerable));

        interpreter.SetVariable("x", data.X);
        interpreter.SetVariable("y", data.Y);
        interpreter.SetVariable("z", data.Z);
        interpreter.SetVariable("text", data.Text);
        interpreter.SetVariable("value", data.Value);
        interpreter.SetVariable("numbers", data.Numbers);
        interpreter.SetVariable("orders", data.Orders);

        return interpreter;
    }

    public static ExpressionContext CreateFleeContext(BenchmarkData data)
    {
        var context = new ExpressionContext();
        context.Imports.AddType(typeof(Math));
        context.Imports.AddType(typeof(Enumerable));

        context.Variables["x"] = data.X;
        context.Variables["y"] = data.Y;
        context.Variables["z"] = data.Z;
        context.Variables["text"] = data.Text;
        context.Variables["value"] = data.Value;
        context.Variables["numbers"] = data.Numbers;
        context.Variables["orders"] = data.Orders;

        return context;
    }

    public static async Task<object?> EvaluateRoslynAsync(string code, BenchmarkData data)
    {
        return await CSharpScript.EvaluateAsync<object>(code, RoslynOptions, data, typeof(BenchmarkData));
    }

    internal static Script<object> CreateRoslynScript(string code)
    {
        return CSharpScript.Create<object>(code, RoslynOptions, typeof(BenchmarkData));
    }
}
