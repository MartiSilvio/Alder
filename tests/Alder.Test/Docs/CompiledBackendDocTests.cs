using System.Linq.Expressions;
using Alder.Compiled;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[Parallelizable(ParallelScope.Children)]
public class CompiledBackendDocTests
{
    [Test]
    public void UseCompiler()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }

    [Test]
    public async Task EvaluateAsync_UsesAsyncInterpreterSurface_WhenCompilerIsConfigured()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var result = await engine.EvaluateAsync<int>("""
            var first = await Task.FromResult(20);
            var second = await Task.FromResult(22);
            return first + second;
            """);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void EvaluateWithTrace_ProducesInterpreterTrace_WhenCompilerIsConfigured()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());
        engine.SetVariable("price", 100m);
        engine.SetVariable("discount", 0.15m);
        engine.SetVariable("tax", 8m);

        var trace = engine.EvaluateWithTrace("price * (1 - discount) + tax");

        Assert.That(trace.Error, Is.Null);
        Assert.That(trace.Result, Is.EqualTo(93.00m));
        Assert.That(trace.Tree.NodeKind, Is.EqualTo("BinaryOperator"));
        Assert.That(trace.Tree.Source, Is.EqualTo("price * (1 - discount) + tax"));
        Assert.That(trace.Tree.ValueType, Is.EqualTo(typeof(decimal)));
        Assert.That(trace.Tree.Children, Has.Count.EqualTo(2));
    }

    [Test]
    public void CompiledExpressionWrapper_SeesValueChanges_AndRejectsTypeSurfaceChanges()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());
        engine.SetVariable<int>("offset", 10);

        var compiled = engine.Compile<int>("x + offset");

        Assert.That(compiled.Invoke(new Dictionary<string, object?> { ["x"] = 5 }), Is.EqualTo(15));

        engine.SetVariable<int>("offset", 20);
        Assert.That(compiled.Invoke(new Dictionary<string, object?> { ["x"] = 5 }), Is.EqualTo(25));

        engine.SetVariable<string>("offset", "twenty");
        var ex = Assert.Throws<AlderException>(() =>
            compiled.Invoke(new Dictionary<string, object?> { ["x"] = 5 }));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0003));
    }

    [Test]
    public void CompileTypedDelegate_UsesDelegateSignatureAsParameterContract()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var rule = engine.Compile<Func<decimal, decimal, bool>>(
            "total >= minimum",
            "total",
            "minimum");

        Assert.That(rule(125m, 100m), Is.True);
        Assert.That(rule(75m, 100m), Is.False);
    }

    [Test]
    public void ParseAsExpression()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        Expression<Func<int, bool>> predicate =
            engine.ParseAsExpression<Func<int, bool>>("x => x > 18 && x < 65");

        var fn = predicate.Compile();
        Assert.That(fn(25), Is.True);
        Assert.That(fn(10), Is.False);
        Assert.That(fn(70), Is.False);
    }

    [Test]
    public void ParseAsExpression_ParsesStandardMode_AndRejectsExtendedOnlySyntax()
    {
        using var engine = new AlderEngine(options =>
        {
            options.LanguageMode = LanguageMode.Extended;
            options.UseCompiler();
        });

        var ex = Assert.Throws<AlderException>(() =>
            engine.ParseAsExpression<Func<int, int>>("x => x ** 2"));

        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void TryParseAsExpression_ReturnsFalseWithDiagnostics_ForExportUnsupportedBody()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var ok = engine.TryParseAsExpression<Func<DocProduct, bool>>(
            "product => { return product.Price > 100m; }",
            out var tree,
            out var diagnostics);

        Assert.That(ok, Is.False);
        Assert.That(tree, Is.Null);
        Assert.That(diagnostics, Is.Not.Empty);
    }
}
