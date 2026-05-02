using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Alder.Compiled;
using Alder.Compiled.DynamicLinq;

namespace Alder.Test.Docs;

[NonParallelizable]
public class DynamicLinqDocTests
{
    [Test]
    public void DynamicLinqRequiresCompilerConfiguration_ForExplicitEngine()
    {
        using var engine = new AlderEngine();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DocSamples.Products.WhereDynamic(engine, "InStock").ToList());

        Assert.That(ex!.Message, Does.Contain("UseCompiler"));
    }

    [Test]
    public void FilteringOrderingPagingAndProjection_ComposeInOneRuntimeQuery()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var page = DocSamples.Products
            .WhereDynamic(engine, "InStock")
            .OrderByDynamic<DocProduct, string>(engine, "Category")
            .ThenByDynamic<DocProduct, string>(engine, "Name")
            .SkipDynamic(1)
            .TakeDynamic(2)
            .SelectDynamic<DocProduct, DocProductSummaryDto>(engine, "new { Name, Price }")
            .ToList();

        Assert.That(page.Select(p => p.Name), Is.EqualTo(new[] { "Whatchamacallit", "Thingamajig" }));
    }

    [Test]
    public void RuntimeValues_SupportPositionalNamedAndMixedBinding()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var positional = DocSamples.Products
            .WhereDynamic(engine, "Category == @0 && Price >= @1", "Electronics", 50m)
            .Select(p => p.Name)
            .ToList();

        var named = DocSamples.Products
            .WhereDynamic(engine, "product => product.Price <= maxPrice && product.Category == category",
                new { maxPrice = 100m, category = "Electronics" })
            .Select(p => p.Name)
            .ToList();

        var mixed = DocSamples.Products
            .WhereDynamic(engine, "product => product.Price > @0 && product.Category == category",
                10m,
                new { category = "Electronics" })
            .Select(p => p.Name)
            .ToList();

        Assert.That(positional, Is.EqualTo(new[] { "Doohickey" }));
        Assert.That(named, Is.EqualTo(new[] { "Gadget" }));
        Assert.That(mixed, Is.EqualTo(new[] { "Gadget", "Doohickey" }));
    }

    [Test]
    public void ProjectionSupportsScalarDtoStructuralAndRuntimeShapedRows()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var prices = DocSamples.Products
            .SelectDynamic<DocProduct, decimal>(engine, "Price")
            .ToList();

        var summaries = DocSamples.Products
            .SelectDynamic<DocProduct, DocProductSummaryDto>(engine, "new { Name, Price }")
            .ToList();

        var rows = DocSamples.Products
            .SelectDynamic<DocProduct, IReadOnlyDictionary<string, object?>>(engine, "new { ProductName = Name, Category, Price }")
            .ToList();

        var configuredRows = DocSamples.Products
            .SelectDynamic(engine, "new { Name, Category, Price }")
            .Cast<IReadOnlyDictionary<string, object?>>()
            .ToList();

        Assert.That(prices[0], Is.EqualTo(9.99m));
        Assert.That(summaries[0].Name, Is.EqualTo("Widget"));
        Assert.That(rows[0]["ProductName"], Is.EqualTo("Widget"));
        Assert.That(configuredRows[0]["Name"], Is.EqualTo("Widget"));
    }

    [Test]
    public void GroupingFlatteningJoinsAndGroupJoins_UseDynamicKeysAndSelectors()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var orderedProducts = DocSamples.Customers
            .SelectManyDynamic<DocCustomer, DocOrder>(engine, "Orders")
            .Select(order => order.Product)
            .ToList();

        var byCategory = DocSamples.Products
            .GroupByDynamic<DocProduct, string>(engine, "Category")
            .Select(group => new { Category = group.Key, Count = group.Count() })
            .OrderBy(group => group.Category)
            .ToList();

        var joined = DocSamples.Products
            .JoinDynamic<DocProduct, DocWarehouseStock, string, string>(
                DocSamples.Stock,
                engine,
                "product => product.Category",
                "stock => stock.Category",
                """(outer, inner) => outer.Name + ":" + inner.Count""")
            .ToList();

        var grouped = DocSamples.Products
            .GroupJoinDynamic<DocProduct, DocWarehouseStock, string, string>(
                DocSamples.Stock,
                engine,
                "product => product.Category",
                "stock => stock.Category",
                """(outer, group) => outer.Name + ":" + group.Count()""")
            .ToList();

        Assert.That(orderedProducts, Is.EqualTo(new[] { "Widget", "Gadget", "Doohickey" }));
        Assert.That(byCategory.Select(x => $"{x.Category}:{x.Count}"), Is.EqualTo(new[] { "Electronics:2", "Specialty:1", "Tools:2" }));
        Assert.That(joined, Does.Contain("Widget:12"));
        Assert.That(grouped, Does.Contain("Widget:1"));
    }

    [Test]
    public void AggregatesElementAndSequenceOperators_FollowLinqBehavior()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        Assert.That(DocSamples.Products.CountDynamic(engine, "InStock"), Is.EqualTo(4));
        Assert.That(DocSamples.Products.SumDynamic<DocProduct, decimal>(engine, "Price"), Is.EqualTo(514.95m));
        Assert.That(DocSamples.Products.AverageDynamic(engine, "product => (double)product.Price"), Is.EqualTo(102.99d).Within(0.001));
        Assert.That(DocSamples.Products.AnyDynamic(engine, """Category == "Specialty" """), Is.True);
        Assert.That(DocSamples.Products.AllDynamic(engine, "Price > 0m"), Is.True);
        Assert.That(DocSamples.Products.FirstDynamic(engine, """Category == "Specialty" """).Name, Is.EqualTo("Whatchamacallit"));
        Assert.That(DocSamples.Products.Select(product => product.Name).ElementAtDynamic(2), Is.EqualTo("Doohickey"));
        Assert.That(DocSamples.Products.Select(product => product.Category).DistinctDynamic().OrderBy(x => x),
            Is.EqualTo(new[] { "Electronics", "Specialty", "Tools" }));
        Assert.That(DocSamples.Products.DistinctByDynamic<DocProduct, string>(engine, "Category").Select(p => p.Category),
            Is.EqualTo(new[] { "Tools", "Electronics", "Specialty" }));
    }

    [Test]
    public void ReusablePlansExposeExpressionAndCompiledDelegateViews()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var filter = engine.ParsePredicate<DocProduct>("Price > 50m");
        var price = engine.ParseSelector<DocProduct, decimal>("Price");

        var inMemory = DocSamples.Products
            .WhereDynamic(filter)
            .OrderByDescendingDynamic<DocProduct, decimal>(price)
            .Select(p => p.Name)
            .ToList();

        var query = DocSamples.Products
            .AsQueryable()
            .WhereDynamic(filter)
            .SelectDynamic<DocProduct, decimal>(price)
            .ToList();

        Expression<Func<DocProduct, bool>> expression = filter.ToExpression<Func<DocProduct, bool>>();
        Func<DocProduct, bool> localPredicate = filter.Compile<Func<DocProduct, bool>>();

        Assert.That(inMemory, Is.EqualTo(new[] { "Whatchamacallit", "Doohickey" }));
        Assert.That(query, Is.EqualTo(new[] { 149.99m, 299.99m }));
        Assert.That(expression.Compile()(DocSamples.Products[2]), Is.True);
        Assert.That(localPredicate(DocSamples.Products[0]), Is.False);
    }

    [Test]
    public void ProviderExport_ProducesExpressionTrees_ButProviderTranslationIsSeparate()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var query = DocSamples.Products
            .AsQueryable()
            .WhereDynamic(engine, "product => product.Price >= @0", 50m)
            .OrderByDynamic<DocProduct, decimal>(engine, "Price")
            .SelectDynamic<DocProduct, DocProductSummaryDto>(engine, "new { Name, Price }")
            .ToList();

        Expression<Func<DocProduct, bool>> directExpression =
            engine.ParseAsExpression<Func<DocProduct, bool>>(
                "product => product.Price >= 50m && product.InStock");

        Assert.That(query.Select(p => p.Name), Is.EqualTo(new[] { "Doohickey", "Whatchamacallit" }));
        Assert.That(directExpression.Compile()(DocSamples.Products[4]), Is.True);
    }

    [Test]
    public void DataRowIndexerQueries_WorkForSchemaShapedData()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());
        var rows = DocSamples.CreateCityTable().AsEnumerable();

        var result = rows
            .WhereDynamic<DataRow>(engine, """(string)it["City"] == @0""", "Seattle")
            .OrderByDynamic<DataRow, int>(engine, """(int)it["Size"]""")
            .SelectDynamic<DataRow, IReadOnlyDictionary<string, object?>>(
                engine,
                """new { City = (string)it["City"], Size = (int)it["Size"] }""")
            .ToList();

        Assert.That(result.Select(row => $"{row["City"]}:{row["Size"]}"), Is.EqualTo(new[] { "Seattle:3", "Seattle:5" }));
    }

    [Test]
    public async Task AsyncStreams_SupportFilteringProjectionPagingAndAggregates()
    {
        using var engine = new AlderEngine(options => options.UseCompiler());

        var names = new List<string>();
        await foreach (var name in DocSamples.ToAsyncEnumerable(DocSamples.Products)
            .WhereDynamic(engine, "product => product.InStock && product.Price >= @0", 50m)
            .SelectDynamic<DocProduct, string>(engine, "product => product.Name")
            .TakeDynamic(10))
        {
            names.Add(name);
        }

        var count = await DocSamples.ToAsyncEnumerable(DocSamples.Products)
            .CountDynamic(engine, "product => product.InStock");
        var total = await DocSamples.ToAsyncEnumerable(DocSamples.Products)
            .SumDynamic<DocProduct, decimal>(engine, "product => product.Price");
        var first = await DocSamples.ToAsyncEnumerable(DocSamples.Products)
            .FirstOrDefaultDynamic(engine, "product => product.Price > 100m");

        Assert.That(names, Is.EqualTo(new[] { "Whatchamacallit" }));
        Assert.That(count, Is.EqualTo(4));
        Assert.That(total, Is.EqualTo(514.95m));
        Assert.That(first!.Name, Is.EqualTo("Doohickey"));
    }

    [Test]
    public void AsyncStreamSurface_DoesNotExposeOrderingOrJoinOperators()
    {
        var methods = typeof(AlderLinqExtensions)
            .GetMethods()
            .Where(method => method.Name is "OrderByDynamic" or "JoinDynamic" or "GroupJoinDynamic")
            .ToList();

        Assert.That(methods.Any(HasAsyncEnumerableFirstParameter), Is.False);
    }

    private static bool HasAsyncEnumerableFirstParameter(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return false;

        var first = parameters[0].ParameterType;
        return first.IsGenericType && first.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>);
    }
}
