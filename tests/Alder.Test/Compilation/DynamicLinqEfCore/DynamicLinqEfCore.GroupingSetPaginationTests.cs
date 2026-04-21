using Alder.Test.Integration;

namespace Alder.Test.Compilation;

public sealed partial class DynamicLinqEfCoreTests
{
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

        [Test]
        public void IQueryable_GroupByDynamic_NullCoalesceKey_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var groups = db.Orders.GroupByDynamic<EfOrder, string>("Notes ?? \"none\"")
                .Select(g => new { g.Key, Count = g.Count() })
                .OrderBy(g => g.Key)
                .ToList();

            Assert.That(groups, Has.Count.EqualTo(2));
            Assert.That(groups[0], Has.Property("Key").EqualTo("none"));
            Assert.That(groups[0], Has.Property("Count").EqualTo(2));
            Assert.That(groups[1], Has.Property("Key").EqualTo("vip"));
            Assert.That(groups[1], Has.Property("Count").EqualTo(2));
        }
    }

    [TestFixture]
    internal class SetOperations : FixtureBase
    {
        [Test]
        public void IQueryable_OfTypeDynamic_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            IQueryable projection = db.Orders.Select(order => (object)order.Customer);

            Assert.Throws<InvalidOperationException>(() =>
                projection.OfTypeDynamic<string>().OrderBy(value => value).ToList());
        }

        [Test]
        public void IQueryable_CastDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            IQueryable projection = db.Orders.Select(order => (object)order.Customer);

            var result = projection.CastDynamic<string>().OrderBy(value => value).ToList();

            Assert.That(result, Is.EqualTo(new[] { "Alice", "Ari", "Bob", "Cara" }));
        }

        [Test]
        public void IQueryable_ContainsDynamic_FindsExistingValue()
        {
            using var db = new EfOrdersDbContext(DbOptions);

            Assert.That(
                db.Orders.SelectDynamic<EfOrder, string>("Customer").ContainsDynamic("Bob"),
                Is.True);
        }

        [Test]
        public void IQueryable_ConcatDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var first = db.Orders.WhereDynamic("o => o.Id <= 2");
            var second = db.Orders.WhereDynamic("o => o.Id >= 3 && o.Id <= 4");

            var ids = first.ConcatDynamic(second).OrderBy(o => o.Id).Select(o => o.Id).ToList();

            Assert.That(ids, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void IQueryable_UnionDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var first = db.Orders.WhereDynamic("o => o.Id <= 2");
            var second = db.Orders.WhereDynamic("o => o.Id >= 2 && o.Id <= 3");

            var ids = first.UnionDynamic(second).OrderBy(o => o.Id).Select(o => o.Id).ToList();

            Assert.That(ids, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void IQueryable_IntersectDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var first = db.Orders.WhereDynamic("o => o.Total >= 55m");
            var second = db.Orders.WhereDynamic("o => o.IsActive");

            var ids = first.IntersectDynamic(second).OrderBy(o => o.Id).Select(o => o.Id).ToList();

            Assert.That(ids, Is.EqualTo(new[] { 2, 4 }));
        }

        [Test]
        public void IQueryable_ExceptDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var first = db.Orders.WhereDynamic("o => o.Total >= 55m");
            var second = db.Orders.WhereDynamic("o => o.IsActive");

            var ids = first.ExceptDynamic(second).OrderBy(o => o.Id).Select(o => o.Id).ToList();

            Assert.That(ids, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void IQueryable_SequenceEqualDynamic_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var first = db.Orders.OrderBy(o => o.Id).Select(o => o.Id);
            var second = db.Orders.OrderBy(o => o.Id).Select(o => o.Id);

            Assert.Throws<InvalidOperationException>(() => first.SequenceEqualDynamic(second));
        }

        [Test]
        public void IQueryable_DistinctDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders
                .SelectDynamic<EfOrder, bool>("IsActive")
                .DistinctDynamic()
                .OrderBy(value => value)
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void IQueryable_ReverseDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders
                .OrderByDynamic<EfOrder, int>("Id")
                .ReverseDynamic()
                .Select(o => o.Id)
                .ToList();

            Assert.That(ids, Is.EqualTo(new[] { 4, 3, 2, 1 }));
        }
    }

    [TestFixture]
    internal class Pagination : FixtureBase
    {
        [Test]
        public void IQueryable_ElementAtDynamic_ReturnsElementAtIndex()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var total = db.Orders
                .OrderBy(o => o.Id)
                .SelectDynamic<EfOrder, decimal>("Total")
                .ElementAtDynamic(2);

            Assert.That(total, Is.EqualTo(120m));
        }

        [Test]
        public void IQueryable_ElementAtOrDefaultDynamic_OutOfRange_ReturnsDefault()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var total = db.Orders
                .OrderBy(o => o.Id)
                .SelectDynamic<EfOrder, decimal>("Total")
                .ElementAtOrDefaultDynamic(99);

            Assert.That(total, Is.EqualTo(0m));
        }

        [Test]
        public void IQueryable_DefaultIfEmptyDynamic_EmptyProjection_ReturnsDefaultValue()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var totals = db.Orders
                .WhereDynamic("o => o.Id > 100")
                .SelectDynamic<EfOrder, decimal>("Total")
                .DefaultIfEmptyDynamic()
                .ToList();

            Assert.That(totals, Is.EqualTo(new[] { 0m }));
        }

        [Test]
        public void IQueryable_DefaultIfEmptyDynamic_WithDefaultValue_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders
                .WhereDynamic("o => o.Id > 100")
                .SelectDynamic<EfOrder, decimal>("Total")
                .DefaultIfEmptyDynamic(42m);

            Assert.Throws<InvalidOperationException>(() => query.ToList());
        }

        [Test]
        public void IQueryable_AppendDynamic_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders
                .OrderBy(o => o.Id)
                .SelectDynamic<EfOrder, int>("Id")
                .AppendDynamic(99);

            Assert.Throws<InvalidOperationException>(() => query.ToList());
        }

        [Test]
        public void IQueryable_PrependDynamic_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders
                .OrderBy(o => o.Id)
                .SelectDynamic<EfOrder, int>("Id")
                .PrependDynamic(99);

            Assert.Throws<InvalidOperationException>(() => query.ToList());
        }

        [Test]
        public void IQueryable_SkipDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders.OrderBy(o => o.Id).SkipDynamic(1).Select(o => o.Id).ToList();
            Assert.That(ids, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void IQueryable_TakeDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders.OrderBy(o => o.Id).TakeDynamic(2).Select(o => o.Id).ToList();
            Assert.That(ids, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void IQueryable_SkipWhileDynamic_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders
                .OrderByDynamic<EfOrder, decimal>("Total")
                .SkipWhileDynamic("o => o.Total < 55m")
                .Select(o => o.Id);

            Assert.Throws<InvalidOperationException>(() => query.ToList());
        }

        [Test]
        public void IQueryable_TakeWhileDynamic_IsRejectedByEfCoreSqlite()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders
                .OrderByDynamic<EfOrder, decimal>("Total")
                .TakeWhileDynamic("o => o.Total < 80m")
                .Select(o => o.Id);

            Assert.Throws<InvalidOperationException>(() => query.ToList());
        }
    }
}
