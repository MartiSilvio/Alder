using Alder.Test.Integration;

namespace Alder.Test.Compilation;

public sealed partial class DynamicLinqEfCoreTests
{
    [TestFixture]
    internal class RelationshipOperators : RelationshipFixtureBase
    {
        [Test]
        public void IQueryable_SelectManyDynamic_Composes()
        {
            using var db = new EfCustomerOrdersDbContext(DbOptions);
            var products = db.Customers
                .OrderBy(customer => customer.Id)
                .SelectManyDynamic<EfCustomer, EfCustomerOrder>("c => c.Orders")
                .Select(order => order.Product)
                .ToList();

            Assert.That(products, Is.EqualTo(new[] { "Laptop", "Mouse", "Keyboard", "Tablet" }));
        }

        [Test]
        public void IQueryable_SelectManyDynamic_WithResultSelector_Composes()
        {
            using var db = new EfCustomerOrdersDbContext(DbOptions);
            var result = db.Customers
                .OrderBy(customer => customer.Id)
                .SelectManyDynamic<EfCustomer, EfCustomerOrder, string>(
                    "c => c.Orders",
                    """(outer, inner) => outer.Name + ":" + inner.Product""")
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { "Alice:Laptop", "Alice:Mouse", "Bob:Keyboard", "Cara:Tablet" }));
        }
    }

    [TestFixture]
    internal class JoinAndGroupJoin : FixtureBase
    {
        [Test]
        public void IQueryable_JoinDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var left = db.Orders.WhereDynamic("o => o.Id <= 2");
            var right = db.Orders.WhereDynamic("o => o.Id >= 2");

            var result = left.JoinDynamic<EfOrder, EfOrder, bool, string>(
                right,
                "o => o.IsActive",
                "i => i.IsActive",
                """(outer, inner) => outer.Customer + ":" + inner.Customer""")
                .ToList();

            Assert.That(result, Does.Contain("Alice:Bob"));
            Assert.That(result, Does.Contain("Bob:Ari"));
        }

        [Test]
        public void IQueryable_GroupJoinDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var left = db.Orders.WhereDynamic("o => o.Id <= 3");
            var right = db.Orders.WhereDynamic("o => o.Id >= 2");

            var result = left.GroupJoinDynamic<EfOrder, EfOrder, bool, int>(
                right,
                "o => o.IsActive",
                "i => i.IsActive",
                """(outer, group) => outer.Id * 10 + group.Count()""")
                .OrderBy(value => value)
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { 12, 22, 31 }));
        }
    }
}
