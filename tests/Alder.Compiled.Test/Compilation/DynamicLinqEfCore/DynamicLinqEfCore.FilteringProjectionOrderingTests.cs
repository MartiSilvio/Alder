using Alder.Test._Infrastructure;
using Alder.Test.Integration;
using Microsoft.EntityFrameworkCore;

namespace Alder.Test.Compilation.DynamicLinqEfCore;

public sealed partial class DynamicLinqEfCoreTests
{
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

        [Test]
        public void IQueryable_WhereDynamic_EfPropertyPredicate_TranslatesToSql()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders.WhereDynamic("""o => EF.Property<decimal>(o, "Total") >= 80m""");
            var sql = query.ToQueryString();
            var ids = query.OrderBy(o => o.Id).Select(o => o.Id).ToList();

            Assert.That(sql, Does.Contain("Total"));
            Assert.That(ids, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void IQueryable_WhereDynamic_EfPropertyStringMethod_TranslatesToSql()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders.WhereDynamic("""o => EF.Property<string>(o, "Customer").StartsWith("A")""");
            var sql = query.ToQueryString();
            var names = query.OrderBy(o => o.Id).Select(o => o.Customer).ToList();

            Assert.That(sql, Does.Contain("Customer"));
            Assert.That(sql, Does.Contain("LIKE").Or.Contain("instr"));
            Assert.That(names, Is.EqualTo(new[] { "Alice", "Ari" }));
        }

        [Test]
        public void IQueryable_WhereDynamic_NullCoalesceOnNullableDecimal_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var query = db.Orders.WhereDynamic("o => (o.Discount ?? 0m) >= 10m");
            var sql = query.ToQueryString();
            var ids = query.OrderBy(o => o.Id).Select(o => o.Id).ToList();

            Assert.That(sql, Does.Contain("Discount"));
            Assert.That(ids, Is.EqualTo(new[] { 2 }));
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
            var nongeneric = db.Orders
                .SelectDynamic("Customer")
                .Cast<string>()
                .OrderBy(c => c)
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { "Alice", "Ari", "Bob", "Cara" }));
            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
        }

        [Test]
        public void IQueryable_SelectDynamic_ProjectsStructuralObject()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders
                .OrderBy(o => o.Id)
                .SelectDynamic<EfOrder, IReadOnlyDictionary<string, object?>>("new { Customer, Total }")
                .ToList();
            var nongeneric = db.Orders
                .OrderByDynamic("Id")
                .SelectDynamic("new { Customer, Total }")
                .Cast<IReadOnlyDictionary<string, object?>>()
                .ToList();
            var first = result[0];
            var nongenericFirst = nongeneric[0]!;

            Assert.That(TestHelpers.ReadProjectedMember(first, "Customer"), Is.EqualTo("Alice"));
            Assert.That(TestHelpers.ReadProjectedMember(first, "Total"), Is.EqualTo(20m));
            Assert.That(nongenericFirst.GetType(), Is.EqualTo(first.GetType()));
            Assert.That(TestHelpers.ReadProjectedMember(nongenericFirst, "Customer"), Is.EqualTo("Alice"));
            Assert.That(TestHelpers.ReadProjectedMember(nongenericFirst, "Total"), Is.EqualTo(20m));
        }
    }

    [TestFixture]
    internal class Ordering : FixtureBase
    {
        [Test]
        public void IQueryable_OrderByDynamic_BodyOnly_KeySelector()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.OrderByDynamic<EfOrder, int>("Id")
                .ToList();
            var nongeneric = db.Orders
                .OrderByDynamic("Id")
                .ToList();
            var ids = result
                .Select(o => o.Id)
                .ToList();
            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
            Assert.That(ids, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void IQueryable_OrderByDescendingDynamic_BodyOnly_KeySelector()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var result = db.Orders.OrderByDescendingDynamic<EfOrder, int>("Id")
                .ToList();
            var nongeneric = db.Orders
                .OrderByDescendingDynamic("Id")
                .ToList();
            var ids = result
                .Select(o => o.Id)
                .ToList();
            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
            Assert.That(ids, Is.EqualTo(new[] { 4, 3, 2, 1 }));
        }

        [Test]
        public void IQueryable_ThenByDynamic_SecondarySort()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders
                .OrderByDynamic<EfOrder, bool>("IsActive")
                .ThenByDynamic<EfOrder, int>("Id")
                .ToList();
            var nongeneric = db.Orders
                .OrderByDynamic("IsActive")
                .ThenByDynamic("Id")
                .ToList();

            Assert.That(nongeneric, Is.EqualTo(ids));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(ids[0].GetType()));

            var orderedIds = ids
                .Select(o => o.Id)
                .ToList();

            Assert.That(orderedIds, Is.EqualTo(new[] { 3, 1, 2, 4 }));
        }

        [Test]
        public void IQueryable_ThenByDescendingDynamic_SecondarySort()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var ids = db.Orders
                .OrderByDynamic<EfOrder, bool>("IsActive")
                .ThenByDescendingDynamic<EfOrder, int>("Id")
                .ToList();
            var nongeneric = db.Orders
                .OrderByDynamic("IsActive")
                .ThenByDescendingDynamic("Id")
                .ToList();

            Assert.That(nongeneric, Is.EqualTo(ids));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(ids[0].GetType()));

            var orderedIds = ids
                .Select(o => o.Id)
                .ToList();

            Assert.That(orderedIds, Is.EqualTo(new[] { 3, 4, 2, 1 }));
        }
    }
}
