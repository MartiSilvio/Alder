using Alder.Test.Integration;

namespace Alder.Test.Compilation.DynamicLinqEfCore;

public sealed partial class DynamicLinqEfCoreTests
{
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

        [Test]
        public void IQueryable_LastDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var last = db.Orders.OrderBy(o => o.Id).LastDynamic("o => o.IsActive");
            Assert.That(last.Customer, Is.EqualTo("Ari"));
        }

        [Test]
        public void IQueryable_LastOrDefaultDynamic_NoMatch()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.OrderBy(o => o.Id).LastOrDefaultDynamic("o => o.Total > 1000m");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void IQueryable_SingleDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var single = db.Orders.SingleDynamic("o => o.Id == 3");
            Assert.That(single.Customer, Is.EqualTo("Cara"));
        }

        [Test]
        public void IQueryable_SingleOrDefaultDynamic_NoMatch()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.SingleOrDefaultDynamic("o => o.Id == 999");
            Assert.That(result, Is.Null);
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
        public void IQueryable_LongCountDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            Assert.That(db.Orders.LongCountDynamic("o => o.Total > 50m"), Is.EqualTo(3L));
        }

        [Test]
        public void IQueryable_SumDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            var result = queryable.SumDynamic("o => o.Total");
            Assert.That(result, Is.EqualTo(275m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public void IQueryable_AverageDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            var result = queryable.AverageDynamic("o => (double)o.Total");
            Assert.That(result, Is.EqualTo(68.75d).Within(0.0001));
            Assert.That(result.GetType(), Is.EqualTo(typeof(double)));
        }

        [Test]
        public void IQueryable_MinDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            var typed = queryable.MinDynamic<EfOrder, decimal>("o => o.Total");
            var nongeneric = queryable.MinDynamic("o => o.Total");
            Assert.That(typed, Is.EqualTo(20m));
            Assert.That(nongeneric, Is.EqualTo(typed));
            Assert.That(nongeneric.GetType(), Is.EqualTo(typed.GetType()));
        }

        [Test]
        public void IQueryable_MaxDynamic()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var queryable = db.Orders.ToList().AsQueryable();
            var typed = queryable.MaxDynamic<EfOrder, decimal>("o => o.Total");
            var nongeneric = queryable.MaxDynamic("o => o.Total");
            Assert.That(typed, Is.EqualTo(120m));
            Assert.That(nongeneric, Is.EqualTo(typed));
            Assert.That(nongeneric.GetType(), Is.EqualTo(typed.GetType()));
        }
    }
}
