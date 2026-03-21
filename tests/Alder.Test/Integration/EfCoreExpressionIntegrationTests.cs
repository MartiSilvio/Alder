using System.Linq.Expressions;
using Alder.Test._Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alder.Test.Integration;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public sealed class EfCoreExpressionIntegrationTests(CompilationMode mode)
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<EfOrdersDbContext> _dbOptions = null!;
    private AlderEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = TestEngineFactory.Create(mode);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<EfOrdersDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new EfOrdersDbContext(_dbOptions);
        db.Database.EnsureCreated();
        Seed(db);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [Test]
    public void ParseAsExpression_WherePredicate_TranslatesAndExecutes()
    {
        _engine.SetVariable("minTotal", 50m);
        _engine.SetVariable("requiredActive", true);
        Expression<Func<EfOrder, bool>> predicate =
            _engine.ParseAsExpression<Func<EfOrder, bool>>("o => o.Total >= minTotal && o.IsActive == requiredActive");

        using var db = new EfOrdersDbContext(_dbOptions);
        var sql = db.Orders.Where(predicate).ToQueryString();
        var ids = db.Orders.Where(predicate).OrderBy(o => o.Id).Select(o => o.Id).ToList();

        Assert.That(sql, Does.Contain("WHERE"));
        Assert.That(sql, Does.Contain("Total"));
        Assert.That(sql, Does.Contain("IsActive"));
        Assert.That(ids, Is.EqualTo(new[] { 2, 4 }));
    }

    [Test]
    public void ParseAsExpression_StringMethodPredicate_TranslatesAndExecutes()
    {
        _engine.SetVariable("prefix", "A");
        Expression<Func<EfOrder, bool>> predicate =
            _engine.ParseAsExpression<Func<EfOrder, bool>>("o => o.Customer.StartsWith(prefix)");

        using var db = new EfOrdersDbContext(_dbOptions);
        var sql = db.Orders.Where(predicate).ToQueryString();
        var names = db.Orders.Where(predicate).OrderBy(o => o.Id).Select(o => o.Customer).ToList();

        Assert.That(sql, Does.Contain("Customer"));
        Assert.That(sql, Does.Contain("LIKE").Or.Contain("instr"));
        Assert.That(names, Is.EqualTo(new[] { "Alice", "Ari" }));
    }

    [Test]
    public void ParseAsExpression_UnsupportedBlockLambda_Throws()
    {
        var ex = Assert.Throws<AlderException>(
            () => _engine.ParseAsExpression<Func<EfOrder, bool>>("o => { var x = o.Total; return x > 10; }"));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.ErrorCode, Is.EqualTo(Alder.Diagnostics.DiagnosticCode.CSEV0002));
        Assert.That(ex.Message, Does.Contain("Expression tree output does not support"));
    }

    private static void Seed(EfOrdersDbContext db)
    {
        db.Orders.AddRange(
            new EfOrder { Id = 1, Customer = "Alice", Total = 20m, IsActive = true },
            new EfOrder { Id = 2, Customer = "Bob", Total = 80m, IsActive = true },
            new EfOrder { Id = 3, Customer = "Cara", Total = 120m, IsActive = false },
            new EfOrder { Id = 4, Customer = "Ari", Total = 55m, IsActive = true });
        db.SaveChanges();
    }
}

internal sealed class EfOrdersDbContext(DbContextOptions<EfOrdersDbContext> options) : DbContext(options)
{
    public DbSet<EfOrder> Orders => Set<EfOrder>();
}

internal sealed class EfOrder
{
    public int Id { get; init; }
    public string Customer { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public bool IsActive { get; init; }
}
