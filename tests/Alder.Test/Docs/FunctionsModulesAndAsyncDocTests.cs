using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
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
    public void AttributedModules_RegisterFromType()
    {
        using var engine = TestEngineFactory.Create(mode, options =>
        {
            options.Modules.RegisterFromType<DocTextModule>();
        });

        Assert.That(engine.Evaluate<string>("""Text.TitleCase("quarterly report")"""), Is.EqualTo("Quarterly Report"));
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
        var value = await engine.EvaluateAsync("await pricing.ComputeAsync(10, 20)");

        Assert.That(raw, Is.InstanceOf<Task<int>>());
        Assert.That(await (Task<int>)raw!, Is.EqualTo(30));
        Assert.That(value, Is.EqualTo(30));
    }

    [Test]
    public async Task EvaluateAsync_ExecutesTextAndParsedExpressions()
    {
        using var engine = TestEngineFactory.Create(mode);

        var total = await engine.EvaluateAsync<int>("""
            var a = await Task.FromResult(20);
            var b = await Task.FromResult(22);
            return a + b;
            """);

        var expression = engine.Parse("await Task.FromResult(@0 + @1)");
        var parsedResult = await engine.EvaluateAsync<int>(expression, 30, 12);

        Assert.That(total, Is.EqualTo(42));
        Assert.That(parsedResult, Is.EqualTo(42));
    }

    [Test]
    public async Task Await_ProducesValuesAndNullForTaskResults()
    {
        using var engine = TestEngineFactory.Create(mode);

        var value = await engine.EvaluateAsync("""await Task.FromResult("hello")""");
        var completedTask = await engine.EvaluateAsync("await Task.Delay(1)");

        Assert.That(value, Is.EqualTo("hello"));
        Assert.That(completedTask, Is.Null);
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

    [Test]
    public async Task AsyncExpressions_CanReuseParsedRulesWithTypedPerCallVariables()
    {
        using var engine = TestEngineFactory.Create(mode);
        var expression = engine.Parse("""
            var value = await source();
            return value >= threshold;
            """);

        var accepted = await engine.EvaluateAsync<bool>(
            expression,
            new { source = (Func<Task<decimal>>)(() => Task.FromResult(125m)), threshold = 100m });

        var jobReady = await engine.EvaluateAsync<bool>(
            "await job.IsReadyAsync() && retries < maxRetries",
            new { job = new DocAsyncJob(), retries = 1, maxRetries = 3 });

        Assert.That(accepted, Is.True);
        Assert.That(jobReady, Is.True);
    }
}
