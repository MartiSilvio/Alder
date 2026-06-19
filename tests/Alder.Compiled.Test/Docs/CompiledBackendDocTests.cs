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
    public void RootReadme_ParseAsExpression_CartPredicate_ExportsProviderShape()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        Expression<Func<Cart, bool>> predicate =
            engine.ParseAsExpression<Func<Cart, bool>>(
                """
                cart => cart.Subtotal - cart.Discount >= 100m &&
                    cart.ItemCount > 0
                """);

        var accepted = new Cart { Id = 1, Subtotal = 120m, Discount = 20m, Tax = 8m, ItemCount = 3 };
        var rejected = new Cart { Id = 2, Subtotal = 75m, Discount = 5m, Tax = 4m, ItemCount = 0 };
        var fn = predicate.Compile();

        Assert.That(fn(accepted), Is.True);
        Assert.That(fn(rejected), Is.False);
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

    [Test]
    public void CustomDelegateCompiler_CanReplaceFinalDelegateCompiler()
    {
        using var engine = new AlderEngine(options =>
            options.UseCompiler(new FastExpressionCompilerAdapter()));

        var fn = engine.Compile<Func<int, int>>("x * 2", "x");

        Assert.That(engine.Evaluate<int>("21 * 2"), Is.EqualTo(42));
        Assert.That(fn(21), Is.EqualTo(42));
    }
}
