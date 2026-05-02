using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[Parallelizable(ParallelScope.Children)]
public class FunctionsModulesAndAsyncDocTests(CompilationMode mode)
{
    [Test]
    public void Functions_Register()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Functions.Register("clamp", args =>
            {
                var value = Convert.ToDouble(args[0]);
                var min = Convert.ToDouble(args[1]);
                var max = Convert.ToDouble(args[2]);
                return Math.Min(Math.Max(value, min), max);
            });
        });

        Assert.That(engine.Evaluate<double>("clamp(rawScore, 0, 100)", new { rawScore = 127 }), Is.EqualTo(100.0));
    }

    [Test]
    public void AttributedFunctions_RegisterAsGlobalFunctions_WithOptionalParameters()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.RegisterFromType<DocGlobalHelpers>();
        });

        Assert.That(engine.Evaluate<string>("""greet("Ada")"""), Is.EqualTo("Hello, Ada!"));
        Assert.That(engine.Evaluate<int>("Add(40, 2)"), Is.EqualTo(42));
        Assert.That(engine.Evaluate<int>("Add(40)"), Is.EqualTo(40));
    }

    [Test]
    public void Modules_Register()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register<DocMathTools>("math");
        });

        Assert.That(engine.Evaluate<double>("math.CircleArea(5)"), Is.EqualTo(Math.PI * 25).Within(0.001));
        Assert.That(engine.Evaluate<double>("math.Tau"), Is.EqualTo(Math.PI * 2));
    }

    [Test]
    public void ExplicitModuleInstances_AreReused()
    {
        var module = new DocStatefulModule();
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register("state", typeof(DocStatefulModule), instance: module);
        });

        Assert.That(engine.Evaluate<int>("state.Next()"), Is.EqualTo(1));
        Assert.That(engine.Evaluate<int>("state.Next()"), Is.EqualTo(2));
        Assert.That(module.Calls, Is.EqualTo(2));
    }

    [Test]
    public void Modules_ExplicitOnly()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register<DocAccountModule>("accounts", explicitOnly: true);
        });

        Assert.That(engine.Evaluate<bool>("accounts.IsActive(42)"), Is.True);
        Assert.Throws<AlderException>(() => engine.Evaluate("accounts.InternalToken"));
    }

    [Test]
    public async Task AsyncModuleMethods_CanBeAwaited()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register("pricing", typeof(DocPricingService), instance: new DocPricingService());
        });

        var accepted = await engine.EvaluateAsync<bool>(
            """
            var minimum = await pricing.GetMinimumAsync(category);
            return total >= minimum;
            """,
            new { total = 300m, category = "Specialty" });

        Assert.That(accepted, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_ReturnsRawTask_WhenExpressionDoesNotAwait()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register("pricing", typeof(DocPricingService), instance: new DocPricingService());
        });

        var raw = await engine.EvaluateAsync("pricing.ComputeAsync(10, 20)");

        Assert.That(raw, Is.InstanceOf<Task<int>>());
        Assert.That(await (Task<int>)raw!, Is.EqualTo(30));
    }

    [Test]
    public async Task Await_CanSuspendInsideControlFlow()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.Register("pricing", typeof(DocPricingService), instance: new DocPricingService());
        });

        var result = await engine.EvaluateAsync<int>("""
            var sum = 0;
            for (var i = 0; i < 3; i++)
            {
                sum += await pricing.ComputeAsync(i, 1);
            }
            return sum;
            """);

        Assert.That(result, Is.EqualTo(6));
    }
}
