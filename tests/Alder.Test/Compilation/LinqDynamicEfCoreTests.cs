using Alder.Test.Integration;
using Alder.Test._Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alder.Test.Compilation;

[TestFixture]
[NonParallelizable]
public sealed class LinqDynamicEfCoreTests
{
    internal abstract class FixtureBase
    {
        private SqliteConnection? _connection;

        protected DbContextOptions<EfOrdersDbContext> DbOptions { get; private set; } = null!;

        [OneTimeSetUp]
        public void BaseSetUp()
        {
            AlderEval.Reset();
            AlderEval.Configure(o => o.UseCompiler());

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            DbOptions = new DbContextOptionsBuilder<EfOrdersDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var db = new EfOrdersDbContext(DbOptions);
            db.Database.EnsureCreated();
            db.Orders.AddRange(
                new EfOrder { Id = 1, Customer = "Alice", Total = 20m, IsActive = true },
                new EfOrder { Id = 2, Customer = "Bob", Total = 80m, IsActive = true },
                new EfOrder { Id = 3, Customer = "Cara", Total = 120m, IsActive = false },
                new EfOrder { Id = 4, Customer = "Ari", Total = 55m, IsActive = true });
            db.SaveChanges();
        }

        [OneTimeTearDown]
        public void BaseTearDown()
        {
            _connection?.Dispose();
            AlderEval.Reset();
        }
    }

    [TestFixture]
    internal class Filtering : FixtureBase
    {
        [Test]
        public void IQueryable_WhereDynamic_TranslatesToSql()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders.WhereDynamic("o => o.Total > 50m");
            var sql = query.ToQueryString();
            var ids = query.OrderBy(o => o.Id).Select(o => o.Id).ToList();
            Assert.That(sql, Does.Contain("WHERE"));
            Assert.That(ids, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void IQueryable_WhereDynamic_BooleanProperty()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.WhereDynamic("o => o.IsActive").OrderBy(o => o.Id).Select(o => o.Id).ToList();
            Assert.That(result, Is.EqualTo(new[] { 1, 2, 4 }));
        }

        [Test]
        public void IQueryable_WhereDynamic_StringMethod()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.WhereDynamic("""o => o.Customer.StartsWith("A")""")
                .OrderBy(o => o.Id).Select(o => o.Customer).ToList();
            Assert.That(result, Is.EqualTo(new[] { "Alice", "Ari" }));
        }

        [Test]
        public void IQueryable_WhereDynamic_CompoundPredicate()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.WhereDynamic("o => o.IsActive && o.Total >= 55m")
                .OrderBy(o => o.Id).Select(o => o.Id).ToList();
            Assert.That(result, Is.EqualTo(new[] { 2, 4 }));
        }

        [Test]
        public void IQueryable_WhereDynamic_WithInlineVariable()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.WhereDynamic("o => o.Total > @0", 50m)
                .OrderBy(o => o.Id).Select(o => o.Id).ToList();
            Assert.That(result, Is.EqualTo(new[] { 2, 3, 4 }));
        }
    }

    [TestFixture]
    internal class Projection : FixtureBase
    {
        [Test]
        public void IQueryable_SelectDynamic_BodyOnly_ImplicitReceiverMember()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.SelectDynamic<EfOrder, string>("Customer")
                .OrderBy(c => c)
                .ToList();
            Assert.That(result, Is.EqualTo(new[] { "Alice", "Ari", "Bob", "Cara" }));
        }

        [Test]
        public void IQueryable_SelectDynamic_ProjectsStructuralObject()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders
                .OrderBy(o => o.Id)
                .SelectDynamic<EfOrder, object>("new { Customer, Total }")
                .ToList();
            var first = result[0];

            Assert.That(TestHelpers.ReadProjectedMember(first, "Customer"), Is.EqualTo("Alice"));
            Assert.That(TestHelpers.ReadProjectedMember(first, "Total"), Is.EqualTo(20m));
        }
    }

    [TestFixture]
    internal class Ordering : FixtureBase
    {
        [Test]
        public void IQueryable_OrderByDynamic_BodyOnly_KeySelector()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders.OrderByDynamic<EfOrder, int>("Id")
                .Select(o => o.Id)
                .ToList();
            Assert.That(ids, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void IQueryable_OrderByDescendingDynamic_BodyOnly_KeySelector()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders.OrderByDescendingDynamic<EfOrder, int>("Id")
                .Select(o => o.Id)
                .ToList();
            Assert.That(ids, Is.EqualTo(new[] { 4, 3, 2, 1 }));
        }

        [Test]
        public void IQueryable_ThenByDynamic_SecondarySort()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders
                .OrderByDynamic<EfOrder, bool>("IsActive")
                .ThenByDynamic<EfOrder, int>("Id")
                .Select(o => o.Id)
                .ToList();

            Assert.That(ids, Is.EqualTo(new[] { 3, 1, 2, 4 }));
        }

        [Test]
        public void IQueryable_ThenByDescendingDynamic_SecondarySort()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders
                .OrderByDynamic<EfOrder, bool>("IsActive")
                .ThenByDescendingDynamic<EfOrder, int>("Id")
                .Select(o => o.Id)
                .ToList();

            Assert.That(ids, Is.EqualTo(new[] { 3, 4, 2, 1 }));
        }
    }

    [TestFixture]
    internal class Grouping : FixtureBase
    {
        [Test]
        public void IQueryable_GroupByDynamic_BodyOnly_KeySelector()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var groups = db.Orders.GroupByDynamic<EfOrder, bool>("IsActive")
                .Select(g => new { g.Key, Count = g.Count() })
                .OrderBy(g => g.Key)
                .ToList();

            Assert.That(groups.Count, Is.EqualTo(2));
            Assert.That(groups[0], Has.Property("Key").EqualTo(false));
            Assert.That(groups[0], Has.Property("Count").EqualTo(1));
            Assert.That(groups[1], Has.Property("Key").EqualTo(true));
            Assert.That(groups[1], Has.Property("Count").EqualTo(3));
        }
    }

    [TestFixture]
    internal class Quantifier : FixtureBase
    {
        [Test]
        public void IQueryable_AnyDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            Assert.That(db.Orders.AnyDynamic("o => o.Total > 100m"), Is.True);
            Assert.That(db.Orders.AnyDynamic("o => o.Total > 1000m"), Is.False);
            Assert.That(db.Orders.AnyDynamic("Total > 100m"), Is.True);
        }

        [Test]
        public void IQueryable_AllDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            Assert.That(db.Orders.AllDynamic("o => o.Total > 0m"), Is.True);
            Assert.That(db.Orders.AllDynamic("o => o.IsActive"), Is.False);
        }
    }

    [TestFixture]
    internal class Element : FixtureBase
    {
        [Test]
        public void IQueryable_FirstDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            Assert.That(db.Orders.FirstDynamic("o => o.Total > 100m").Customer, Is.EqualTo("Cara"));
        }

        [Test]
        public void IQueryable_FirstOrDefaultDynamic_NoMatch()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            Assert.That(db.Orders.FirstOrDefaultDynamic("o => o.Total > 1000m"), Is.Null);
        }
    }

    [TestFixture]
    internal class Aggregation : FixtureBase
    {
        [Test]
        public void IQueryable_CountDynamic() =>
            Assert.That(new EfOrdersDbContext(DbOptions).Orders.CountDynamic("o => o.IsActive"), Is.EqualTo(3));

        [Test]
        public void IQueryable_CountDynamic_WithInlineVariable()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            Assert.That(db.Orders.CountDynamic("o => o.Total > @0", 50m), Is.EqualTo(3));
        }

        [Test]
        public void IQueryable_SumDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            Assert.That(queryable.SumDynamic("o => o.Total"), Is.EqualTo(275m));
        }

        [Test]
        public void IQueryable_AverageDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            Assert.That(queryable.AverageDynamic("o => (double)o.Total"), Is.EqualTo(68.75d).Within(0.0001));
        }

        [Test]
        public void IQueryable_MinDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            Assert.That(queryable.MinDynamic<EfOrder, decimal>("o => o.Total"), Is.EqualTo(20m));
        }

        [Test]
        public void IQueryable_MaxDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            Assert.That(queryable.MaxDynamic<EfOrder, decimal>("o => o.Total"), Is.EqualTo(120m));
        }
    }
}
