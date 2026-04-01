using Alder.Test.Integration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alder.Test.Compilation;

public record Product(string Name, decimal Price, string Category, bool InStock);

[TestFixture]
public class LinqDynamicTests
{
    private static readonly List<Product> Products =
    [
        new("Widget", 9.99m, "Tools", true),
        new("Gadget", 49.99m, "Electronics", true),
        new("Doohickey", 149.99m, "Electronics", false),
        new("Thingamajig", 4.99m, "Tools", true),
        new("Whatchamacallit", 299.99m, "Premium", true)
    ];

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        AlderEval.Reset();
    }

    [Test]
    public void WhereDynamic_FiltersByPredicate()
    {
        var result = Products.WhereDynamic("p => p.Price > 50m").ToList();
        Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
    }

    [Test]
    public void WhereDynamic_WithVariable()
    {
        var engine = AlderEval.GetEngine();
        engine.SetVariable("threshold", 100m);
        var result = Products.WhereDynamic("p => p.Price > threshold").ToList();
        Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
    }

    [Test]
    public void WhereDynamic_StringMethod()
    {
        var result = Products.WhereDynamic("""p => p.Category == "Electronics" """).ToList();
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void WhereDynamic_BooleanProperty()
    {
        var result = Products.WhereDynamic("p => p.InStock").ToList();
        Assert.That(result, Has.Count.EqualTo(4));
    }

    [Test]
    public void WhereDynamic_CompoundPredicate()
    {
        var result = Products.WhereDynamic("p => p.InStock && p.Price < 20m").ToList();
        Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Widget", "Thingamajig" }));
    }

    [Test]
    public void SelectDynamic_ProjectsToNewType()
    {
        var result = Products.SelectDynamic<Product, string>("p => p.Name").ToList();
        Assert.That(result, Is.EquivalentTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit" }));
    }

    [Test]
    public void SelectDynamic_ProjectsToDecimal()
    {
        var result = Products.SelectDynamic<Product, decimal>("p => p.Price").ToList();
        Assert.That(result, Has.Count.EqualTo(5));
        Assert.That(result, Does.Contain(9.99m));
    }

    [Test]
    public void OrderByDynamic_SortsByKey()
    {
        var result = Products.OrderByDynamic<Product, decimal>("p => p.Price").ToList();
        Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
        Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
    }

    [Test]
    public void OrderByDescendingDynamic_SortsByKeyDescending()
    {
        var result = Products.OrderByDescendingDynamic<Product, decimal>("p => p.Price").ToList();
        Assert.That(result[0].Name, Is.EqualTo("Whatchamacallit"));
        Assert.That(result[^1].Name, Is.EqualTo("Thingamajig"));
    }

    [Test]
    public void AnyDynamic_ReturnsTrueWhenMatch()
    {
        Assert.That(Products.AnyDynamic("p => p.Price > 200m"), Is.True);
    }

    [Test]
    public void AnyDynamic_ReturnsFalseWhenNoMatch()
    {
        Assert.That(Products.AnyDynamic("p => p.Price > 1000m"), Is.False);
    }

    [Test]
    public void AllDynamic_ReturnsTrueWhenAllMatch()
    {
        Assert.That(Products.AllDynamic("p => p.Price > 0m"), Is.True);
    }

    [Test]
    public void AllDynamic_ReturnsFalseWhenSomeDontMatch()
    {
        Assert.That(Products.AllDynamic("p => p.InStock"), Is.False);
    }

    [Test]
    public void CountDynamic_CountsMatching()
    {
        Assert.That(Products.CountDynamic("p => p.InStock"), Is.EqualTo(4));
    }

    [Test]
    public void CountDynamic_WithParameter()
    {
        var engine = AlderEval.GetEngine();
        engine.SetVariable("min", 40m);
        Assert.That(Products.CountDynamic("p => p.Price > min"), Is.EqualTo(3));
    }

    [Test]
    public void FirstDynamic_ReturnsFirstMatch()
    {
        var result = Products.FirstDynamic("""p => p.Category == "Premium" """);
        Assert.That(result.Name, Is.EqualTo("Whatchamacallit"));
    }

    [Test]
    public void FirstDynamic_NoMatch_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Products.FirstDynamic("""p => p.Category == "NonExistent" """));
    }

    [Test]
    public void FirstOrDefaultDynamic_ReturnsNullWhenNoMatch()
    {
        var result = Products.FirstOrDefaultDynamic("""p => p.Category == "NonExistent" """);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void FirstOrDefaultDynamic_ReturnsFirstWhenMatch()
    {
        var result = Products.FirstOrDefaultDynamic("p => p.Price < 5m");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Thingamajig"));
    }

    [Test]
    public void LastDynamic_ReturnsLastMatch()
    {
        var result = Products.LastDynamic("p => p.InStock");
        Assert.That(result.Name, Is.EqualTo("Whatchamacallit"));
    }

    [Test]
    public void LastOrDefaultDynamic_ReturnsNullWhenNoMatch()
    {
        var result = Products.LastOrDefaultDynamic("p => p.Price > 1000m");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SingleDynamic_ReturnsSingleMatch()
    {
        var result = Products.SingleDynamic("""p => p.Category == "Premium" """);
        Assert.That(result.Name, Is.EqualTo("Whatchamacallit"));
    }

    [Test]
    public void SingleDynamic_MultipleMatches_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Products.SingleDynamic("""p => p.Category == "Tools" """));
    }

    [Test]
    public void SingleOrDefaultDynamic_ReturnsNullWhenNoMatch()
    {
        var result = Products.SingleOrDefaultDynamic("p => p.Price > 1000m");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void NoCompiler_ThrowsClearError()
    {
        AlderEval.Reset();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Products.WhereDynamic("p => p.InStock"));
        Assert.That(ex!.Message, Does.Contain("UseCompiler"));

        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());
    }

    [Test]
    public void EmptyCollection_WhereDynamic_ReturnsEmpty()
    {
        var empty = new List<Product>();
        var result = empty.WhereDynamic("p => p.InStock").ToList();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ThenByDynamic_SecondarySort()
    {
        var result = Products
            .OrderByDynamic<Product, string>("p => p.Category")
            .ThenByDynamic<Product, decimal>("p => p.Price")
            .ToList();

        Assert.That(result[0].Name, Is.EqualTo("Gadget"));
        Assert.That(result[1].Name, Is.EqualTo("Doohickey"));
    }

    [Test]
    public void ThenByDescendingDynamic_SecondarySort()
    {
        var result = Products
            .OrderByDynamic<Product, string>("p => p.Category")
            .ThenByDescendingDynamic<Product, decimal>("p => p.Price")
            .ToList();

        Assert.That(result[0].Name, Is.EqualTo("Doohickey"));
        Assert.That(result[1].Name, Is.EqualTo("Gadget"));
    }

    [Test]
    public void GroupByDynamic_GroupsByKey()
    {
        var result = Products
            .GroupByDynamic<Product, string>("p => p.Category")
            .ToList();

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(g => g.Key), Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
    }

    [Test]
    public void GroupByDynamic_GroupCountsCorrect()
    {
        var result = Products
            .GroupByDynamic<Product, string>("p => p.Category")
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.That(result["Tools"], Is.EqualTo(2));
        Assert.That(result["Electronics"], Is.EqualTo(2));
        Assert.That(result["Premium"], Is.EqualTo(1));
    }

    [Test]
    public void DistinctByDynamic_RemovesDuplicateKeys()
    {
        var result = Products
            .DistinctByDynamic<Product, string>("p => p.Category")
            .ToList();

        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void SumDynamic_SumsValues()
    {
        var result = Products.SumDynamic("p => p.Price");
        Assert.That(result, Is.EqualTo(514.95m));
    }

    [Test]
    public void AverageDynamic_AveragesValues()
    {
        var result = Products.AverageDynamic("p => (double)p.Price");
        Assert.That(result, Is.EqualTo(102.99).Within(0.01));
    }

    [Test]
    public void MinDynamic_FindsMinimum()
    {
        var result = Products.MinDynamic<Product, decimal>("p => p.Price");
        Assert.That(result, Is.EqualTo(4.99m));
    }

    [Test]
    public void MaxDynamic_FindsMaximum()
    {
        var result = Products.MaxDynamic<Product, decimal>("p => p.Price");
        Assert.That(result, Is.EqualTo(299.99m));
    }

    [Test]
    public void GroupByDynamic_WithAggregation()
    {
        var result = Products
            .GroupByDynamic<Product, string>("p => p.Category")
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Price));

        Assert.That(result["Tools"], Is.EqualTo(14.98m));
        Assert.That(result["Electronics"], Is.EqualTo(199.98m));
        Assert.That(result["Premium"], Is.EqualTo(299.99m));
    }

    [Test]
    public void ChainedDynamic_WhereAndOrderBy()
    {
        var result = Products
            .WhereDynamic("p => p.InStock")
            .OrderByDynamic<Product, decimal>("p => p.Price")
            .ToList();

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
        Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
    }
}

[TestFixture]
public sealed class LinqDynamicEfCoreTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<EfOrdersDbContext> _dbOptions = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        AlderEval.Reset();
        AlderEval.Configure(o => o.UseCompiler());

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<EfOrdersDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new EfOrdersDbContext(_dbOptions);
        db.Database.EnsureCreated();
        db.Orders.AddRange(
            new EfOrder { Id = 1, Customer = "Alice", Total = 20m, IsActive = true },
            new EfOrder { Id = 2, Customer = "Bob", Total = 80m, IsActive = true },
            new EfOrder { Id = 3, Customer = "Cara", Total = 120m, IsActive = false },
            new EfOrder { Id = 4, Customer = "Ari", Total = 55m, IsActive = true });
        db.SaveChanges();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _connection.Dispose();
        AlderEval.Reset();
    }

    [Test]
    public void WhereDynamic_IQueryable_TranslatesToSql()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var query = db.Orders.WhereDynamic("o => o.Total > 50m");
        var sql = query.ToQueryString();
        var ids = query.OrderBy(o => o.Id).Select(o => o.Id).ToList();

        Assert.That(sql, Does.Contain("WHERE"));
        Assert.That(ids, Is.EqualTo(new[] { 2, 3, 4 }));
    }

    [Test]
    public void WhereDynamic_IQueryable_BooleanProperty()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var result = db.Orders.WhereDynamic("o => o.IsActive").OrderBy(o => o.Id).Select(o => o.Id).ToList();
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 4 }));
    }

    [Test]
    public void WhereDynamic_IQueryable_StringMethod()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var result = db.Orders.WhereDynamic("""o => o.Customer.StartsWith("A")""")
            .OrderBy(o => o.Id).Select(o => o.Customer).ToList();
        Assert.That(result, Is.EqualTo(new[] { "Alice", "Ari" }));
    }

    [Test]
    public void WhereDynamic_IQueryable_CompoundPredicate()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var result = db.Orders.WhereDynamic("o => o.IsActive && o.Total >= 55m")
            .OrderBy(o => o.Id).Select(o => o.Id).ToList();
        Assert.That(result, Is.EqualTo(new[] { 2, 4 }));
    }

    [Test]
    public void AnyDynamic_IQueryable()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        Assert.That(db.Orders.AnyDynamic("o => o.Total > 100m"), Is.True);
        Assert.That(db.Orders.AnyDynamic("o => o.Total > 1000m"), Is.False);
    }

    [Test]
    public void AllDynamic_IQueryable()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        Assert.That(db.Orders.AllDynamic("o => o.Total > 0m"), Is.True);
        Assert.That(db.Orders.AllDynamic("o => o.IsActive"), Is.False);
    }

    [Test]
    public void CountDynamic_IQueryable()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        Assert.That(db.Orders.CountDynamic("o => o.IsActive"), Is.EqualTo(3));
    }

    [Test]
    public void FirstDynamic_IQueryable()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var result = db.Orders.FirstDynamic("o => o.Total > 100m");
        Assert.That(result.Customer, Is.EqualTo("Cara"));
    }

    [Test]
    public void FirstOrDefaultDynamic_IQueryable_NoMatch()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var result = db.Orders.FirstOrDefaultDynamic("o => o.Total > 1000m");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void WhereDynamic_IQueryable_SqlContainsColumns()
    {
        using var db = new EfOrdersDbContext(_dbOptions);
        var sql = db.Orders.WhereDynamic("o => o.IsActive && o.Total > 50m").ToQueryString();
        Assert.That(sql, Does.Contain("Total"));
        Assert.That(sql, Does.Contain("IsActive"));
    }
}
