using Alder.Test.Integration;

namespace Alder.Test.Compilation.DynamicLinqEfCore;

public sealed partial class DynamicLinqEfCoreTests
{
    [TestFixture]
    internal class RelationshipOperators : RelationshipFixtureBase
    {
        [Test]
        public void IQueryable_SelectManyDynamic_Composes()
        {
            using var db = new EfCustomerOrdersDbContext(DbOptions);
            var result = db.Customers
                .OrderBy(customer => customer.Id)
                .SelectManyDynamic<EfCustomer, EfCustomerOrder>("c => c.Orders")
                .ToList();
            var nongeneric = db.Customers
                .OrderBy(customer => customer.Id)
                .SelectManyDynamic("c => c.Orders")
                .Cast<EfCustomerOrder>()
                .ToList();
            var products = result
                .Select(order => order.Product)
                .ToList();

            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
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
            var nongeneric = db.Customers
                .OrderBy(customer => customer.Id)
                .SelectManyDynamic(
                    "c => c.Orders",
                    """(outer, inner) => outer.Name + ":" + inner.Product""")
                .Cast<string>()
                .ToList();

            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
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
            var left = db.Orders.WhereDynamic<EfOrder>("o => o.Id <= 2");
            var right = db.Orders.WhereDynamic<EfOrder>("o => o.Id >= 2");

            var result = left.JoinDynamic<EfOrder, EfOrder, bool, string>(
                right,
                "o => o.IsActive",
                "i => i.IsActive",
                """(outer, inner) => outer.Customer + ":" + inner.Customer""")
                .ToList();
            var nongeneric = left.JoinDynamic(
                    right,
                    "o => o.IsActive",
                    "i => i.IsActive",
                    """(outer, inner) => outer.Customer + ":" + inner.Customer""")
                .Cast<string>()
                .ToList();

            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
            Assert.That(result, Does.Contain("Alice:Bob"));
            Assert.That(result, Does.Contain("Bob:Ari"));
        }

        [Test]
        public void IQueryable_GroupJoinDynamic_Composes()
        {
            using var db = new EfOrdersDbContext(DbOptions);
            var left = db.Orders.WhereDynamic<EfOrder>("o => o.Id <= 3");
            var right = db.Orders.WhereDynamic<EfOrder>("o => o.Id >= 2");

            var result = left.GroupJoinDynamic<EfOrder, EfOrder, bool, int>(
                right,
                "o => o.IsActive",
                "i => i.IsActive",
                """(outer, group) => outer.Id * 10 + group.Count()""")
                .OrderBy(value => value)
                .ToList();
            var nongeneric = left.GroupJoinDynamic(
                    right,
                    "o => o.IsActive",
                    "i => i.IsActive",
                    """(outer, group) => outer.Id * 10 + group.Count()""")
                .Cast<int>()
                .OrderBy(value => value)
                .ToList();

            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
            Assert.That(result, Is.EqualTo(new[] { 12, 22, 31 }));
        }
    }
}
