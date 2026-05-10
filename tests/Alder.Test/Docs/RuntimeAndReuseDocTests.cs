#if NET8_0_OR_GREATER
using Alder.Compiled;
using Alder.Compiled.DynamicLinq;
#endif
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Docs;

[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
[Parallelizable(ParallelScope.Children)]
public class RuntimeAndReuseDocTests(CompilationMode mode)
{
    [Test]
    public void FluentChaining()
    {
        using var engine = TestEngineFactory.Create(mode);

        engine
            .SetVariable<double>("rate", 0.05)
            .SetVariable<int>("years", 10)
            .SetVariable<double>("principal", 1000.0);

        var result = engine.Evaluate<double>(
            "principal * Math.Pow(1 + rate, years)");

        Assert.That(result, Is.EqualTo(1000.0 * Math.Pow(1.05, 10)).Within(0.001));
    }

#if NET8_0_OR_GREATER
    [Test]
    public void EngineConfiguration_CanCombinePolicyAndCompilerSettings()
    {
        using var engine = new AlderEngine(options =>
        {
            options.LanguageMode = LanguageMode.Standard;
            options.Security = new SecurityOptions { AllowPropertyRead = true, AllowStaticPropertyRead = true, AllowStaticFieldRead = true, AllowAssignment = true, AllowPropertySet = true, AllowIndexSet = true };
            options.Constraints = new ExecutionConstraints
            {
                MaxStatements = 10_000,
                MaxLoopIterations = 1_000,
                MaxTimeout = TimeSpan.FromSeconds(5)
            };
            options.UseCompiler();
        });

        Assert.That(engine.Evaluate<int>("1 + 2"), Is.EqualTo(3));
    }
#endif

    [Test]
    public void Dictionary()
    {
        using var engine = TestEngineFactory.Create(mode);
        var vars = new Dictionary<string, object?>
        {
            ["threshold"] = 100,
            ["multiplier"] = 1.5
        };

        var result = engine.Evaluate<double>(
            "threshold * multiplier",
            vars);

        Assert.That(result, Is.EqualTo(150.0));
    }

    [Test]
    public void ObjectShapedDictionaryVariables_UseDeclaredObjectSurface()
    {
        using var engine = TestEngineFactory.Create(mode);
        var expression = engine.Parse("(long)x");

        engine.SetVariable<int>("x", 42);
        Assert.That(engine.Evaluate(expression), Is.EqualTo(42L));

        engine.SetVariable<object>("x", 42);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(expression));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0030));
    }

    [Test]
    public void SetVariablesPreservingRuntimeTypes_UsesConcreteDictionaryValueTypes()
    {
        using var engine = TestEngineFactory.Create(mode);
        var order = new DocOrderRow(125m, new DocCustomerInfo("Ada"));
        var inputs = new Dictionary<string, object?>
        {
            ["order"] = order,
            ["minimum"] = 100m
        };

        var child = engine.CreateChild()
            .SetVariablesPreservingRuntimeTypes(inputs);

        Assert.That(
            child.Evaluate<bool>(
                """order.Total >= minimum && order.Customer.Name.StartsWith("A")"""),
            Is.True);
    }

    [Test]
    public void TypedAnonymousInputs_PreservePerCallBindingSurface()
    {
        using var engine = TestEngineFactory.Create(mode);
        var item = new DocOrderRow(75m, new DocCustomerInfo("Ada"));
        var expression = engine.Parse("item.Total >= minimum");

        var visible = engine.Evaluate<bool>(
            expression,
            new { item, minimum = 50m });

        Assert.That(visible, Is.True);
    }

    [Test]
    public void PerCallVariables_DoNotPersist()
    {
        using var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<int>("x", 10);

        Assert.That(engine.Evaluate<int>("x + y", new { y = 20 }), Is.EqualTo(30));
        Assert.Throws<AlderException>(() => engine.Evaluate("y"));
    }

    [Test]
    public void TemporaryDictionaryAndPositionalValues_DoNotMutateSharedScope()
    {
        using var engine = TestEngineFactory.Create(mode);

        var result = engine.Evaluate<long>(
            "item * multiplier",
            new Dictionary<string, object?>
            {
                ["item"] = 5L,
                ["multiplier"] = 2L
            });

        var sum = engine.Evaluate<int>("@0 + @1 + @2", 1, 2, 3);

        Assert.That(result, Is.EqualTo(10L));
        Assert.That(sum, Is.EqualTo(6));
        Assert.Throws<AlderException>(() => engine.Evaluate("item"));
    }

    [Test]
    public void ChildEngines()
    {
        using var parent = TestEngineFactory.Create(mode);
        parent.SetVariable<double>("baseFee", 50.0);

        var tenantA = parent.CreateChild();
        tenantA.SetVariable<double>("discount", 0.1);

        var tenantB = parent.CreateChild();
        tenantB.SetVariable<double>("discount", 0.25);

        Assert.That(tenantA.Evaluate<double>("baseFee * (1 - discount)"), Is.EqualTo(45.0));
        Assert.That(tenantB.Evaluate<double>("baseFee * (1 - discount)"), Is.EqualTo(37.5));
        Assert.That(parent.Evaluate<double>("baseFee"), Is.EqualTo(50.0));
        Assert.Throws<AlderException>(() => parent.Evaluate("discount"));
    }

    [Test]
    public void ParsedExpressions_EvaluateManyPerCallValueSets()
    {
        using var engine = TestEngineFactory.Create(mode);
        var expression = engine.Parse("price * (1 - discount)");

        var first = engine.Evaluate<double>(
            expression,
            new { price = 100.0, discount = 0.10 });

        var second = engine.Evaluate<double>(
            expression,
            new { price = 250.0, discount = 0.10 });

        Assert.That(first, Is.EqualTo(90.0));
        Assert.That(second, Is.EqualTo(225.0));
    }

    [Test]
    public void ValueOnlyChanges_ReuseParsedExpressionBinding()
    {
        using var engine = TestEngineFactory.Create(mode);
        var expression = engine.Parse("x + 1");

        engine.SetVariable<int>("x", 1);
        var first = engine.Evaluate<int>(expression);

        engine.SetVariable<int>("x", 10);
        var second = engine.Evaluate<int>(expression);

        Assert.That(first, Is.EqualTo(2));
        Assert.That(second, Is.EqualTo(11));
    }

    [Test]
    public void ParsedExpression_Rebinds_WhenVisibleTypeSurfaceChanges()
    {
        using var engine = TestEngineFactory.Create(mode);
        var expression = engine.Parse("x.Length");

        engine.SetVariable<string>("x", "hello");
        Assert.That(engine.Evaluate<int>(expression), Is.EqualTo(5));

        engine.SetVariable<int[]>("x", [1, 2, 3]);
        Assert.That(engine.Evaluate<int>(expression), Is.EqualTo(3));
    }

    [Test]
    public void CreateChild_IsUsableForParallelLocalState()
    {
        using var engine = TestEngineFactory.Create(mode);
        engine.SetVariable<double>("taxRate", 0.08);

        var amounts = Enumerable.Range(1, 25).Select(i => (double)i * 100).ToList();
        var results = new System.Collections.Concurrent.ConcurrentBag<double>();

        Parallel.ForEach(amounts, amount =>
        {
            var child = engine.CreateChild();
            child.SetVariable<double>("amount", amount);
            results.Add(child.Evaluate<double>("amount * taxRate"));
        });

        Assert.That(results.OrderBy(x => x), Is.EqualTo(amounts.Select(a => a * 0.08).OrderBy(x => x)));
    }

    [Test]
    public void ChildEngines_IsolateConcurrentLocalValues()
    {
        using var parent = TestEngineFactory.Create(mode);
        parent.SetVariable<int>("taxRateBasisPoints", 825);
        var orders = Enumerable.Range(1, 20).Select(i => new { Subtotal = i * 10m }).ToList();
        var totals = new System.Collections.Concurrent.ConcurrentBag<decimal>();

        Parallel.ForEach(orders, order =>
        {
            var child = parent.CreateChild();
            child.SetVariable("order", order);

            totals.Add(child.Evaluate<decimal>(
                "order.Subtotal * (1 + taxRateBasisPoints / 10000m)"));
        });

        Assert.That(totals.OrderBy(x => x),
            Is.EqualTo(orders.Select(order => order.Subtotal * 1.0825m).OrderBy(x => x)));
        Assert.Throws<AlderException>(() => parent.Evaluate("order"));
    }

#if NET8_0_OR_GREATER
    [Test]
    public void DynamicQueryPlan_ReusesPredicateForEnumerableQueryableExpressionAndDelegate()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var filter = engine.ParsePredicate<DocProduct>("Price > 50m");

        var enumerable = DocSamples.Products.WhereDynamic(filter).Select(p => p.Name).ToList();
        var queryable = DocSamples.Products.AsQueryable().WhereDynamic(filter).Select(p => p.Name).ToList();
        var expression = filter.ToExpression<Func<DocProduct, bool>>();
        var compiled = filter.Compile<Func<DocProduct, bool>>();

        Assert.That(enumerable, Is.EqualTo(new[] { "Doohickey", "Whatchamacallit" }));
        Assert.That(queryable, Is.EqualTo(enumerable));
        Assert.That(DocSamples.Products.Where(expression.Compile()).Select(p => p.Name), Is.EqualTo(enumerable));
        Assert.That(DocSamples.Products.Where(compiled).Select(p => p.Name), Is.EqualTo(enumerable));
    }
#endif

    [Test]
    public void TypedResultConversion_MaterializesStructuralProjection()
    {
        using var engine = TestEngineFactory.Create(mode);

        var summary = engine.Evaluate<DocProductSummaryDto>("""
            return new { Name = "Widget", Price = 9.99m };
            """);

        Assert.That(summary, Is.Not.Null);
        Assert.That(summary!.Name, Is.EqualTo("Widget"));
        Assert.That(summary.Price, Is.EqualTo(9.99m));
    }
}
