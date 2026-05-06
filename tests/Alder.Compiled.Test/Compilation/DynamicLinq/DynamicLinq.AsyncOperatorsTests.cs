using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class AsyncOperators : CompilerFixtureBase
    {
        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                await Task.Yield();
                yield return item;
            }
        }

        [Test]
        public async Task Async_WhereDynamic_FiltersStream()
        {
            var result = new List<Product>();
            await foreach (var product in ToAsyncEnumerable(Products).WhereDynamic("p => p.Price > @0", 50m))
            {
                result.Add(product);
            }

            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public async Task Async_WhereDynamic_CustomEngine()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var result = new List<Product>();
            await foreach (var product in ToAsyncEnumerable(Products).WhereDynamic(engine, "p => p.Price > @0", 100m))
            {
                result.Add(product);
            }

            Assert.That(result, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task Async_SelectDynamic_ProjectsStream()
        {
            var names = new List<string>();
            var nongenericNames = new List<string>();
            await foreach (var name in ToAsyncEnumerable(Products).SelectDynamic<Product, string>("p => p.Name"))
            {
                names.Add(name);
            }
            await foreach (var name in ToAsyncEnumerable(Products).SelectDynamic("p => p.Name"))
            {
                nongenericNames.Add((string)name!);
            }

            Assert.That(names, Has.Count.EqualTo(5));
            Assert.That(nongenericNames, Is.EqualTo(names));
            Assert.That(nongenericNames[0].GetType(), Is.EqualTo(names[0].GetType()));
        }

        [TestCase("p => p.Price > 200m", true)]
        [TestCase("p => p.Price > 500m", false)]
        public async Task Async_AnyDynamic(string predicate, bool expected) =>
            Assert.That(await ToAsyncEnumerable(Products).AnyDynamic(predicate), Is.EqualTo(expected));

        [TestCase("p => p.Price > 1m", true)]
        [TestCase("p => p.Price > 50m", false)]
        public async Task Async_AllDynamic(string predicate, bool expected) =>
            Assert.That(await ToAsyncEnumerable(Products).AllDynamic(predicate), Is.EqualTo(expected));

        [Test]
        public async Task Async_CountDynamic() =>
            Assert.That(await ToAsyncEnumerable(Products).CountDynamic("p => p.Price > @0", 50m), Is.EqualTo(2));

        [Test]
        public async Task Async_FirstDynamic() =>
            Assert.That((await ToAsyncEnumerable(Products).FirstDynamic("p => p.Category == @0", "Electronics")).Name, Is.EqualTo("Gadget"));

        [Test]
        public void Async_FirstDynamic_ThrowsWhenNoMatch() =>
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await ToAsyncEnumerable(Products).FirstDynamic("p => p.Category == @0", "X"));

        [Test]
        public async Task Async_FirstOrDefaultDynamic_ReturnsNull() =>
            Assert.That(await ToAsyncEnumerable(Products).FirstOrDefaultDynamic("p => p.Category == @0", "X"), Is.Null);

        [Test]
        public async Task Async_SumDynamic()
        {
            var result = await ToAsyncEnumerable(Products).SumDynamic("p => p.Price");
            Assert.That(result, Is.EqualTo(514.95m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public async Task Async_WhereDynamic_ParsedPlanExpressionInterop()
        {
            var predicate = AlderEval.GetEngine()
                .ParsePredicate<Product>("Price > 50m")
                .ToExpression<Func<Product, bool>>();

            var result = new List<Product>();
            await foreach (var product in ToAsyncEnumerable(Products).WhereDynamic(predicate))
            {
                result.Add(product);
            }

            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public async Task Async_AverageDynamic()
        {
            var result = await ToAsyncEnumerable(Products).AverageDynamic("p => (double)p.Price");
            Assert.That(result, Is.EqualTo(102.99).Within(0.01));
            Assert.That(result.GetType(), Is.EqualTo(typeof(double)));
        }

        [Test]
        public async Task Async_MinDynamic()
        {
            var result = await ToAsyncEnumerable(Products).MinDynamic("p => p.Price");
            Assert.That(result, Is.EqualTo(4.99m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public async Task Async_MaxDynamic()
        {
            var result = await ToAsyncEnumerable(Products).MaxDynamic("p => p.Price");
            Assert.That(result, Is.EqualTo(299.99m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public async Task Async_SkipDynamic()
        {
            var result = new List<Product>();
            await foreach (var product in ToAsyncEnumerable(Products).SkipDynamic(2))
            {
                result.Add(product);
            }

            Assert.That(result.Select(p => p.Name), Is.EqualTo(new[] { "Doohickey", "Thingamajig", "Whatchamacallit" }));
        }

        [Test]
        public async Task Async_TakeDynamic()
        {
            var result = new List<Product>();
            await foreach (var product in ToAsyncEnumerable(Products).TakeDynamic(2))
            {
                result.Add(product);
            }

            Assert.That(result.Select(p => p.Name), Is.EqualTo(new[] { "Widget", "Gadget" }));
        }

        [Test]
        public async Task Async_DistinctDynamic()
        {
            var result = new List<string>();
            await foreach (var category in ToAsyncEnumerable(Products.Select(p => p.Category)).DistinctDynamic())
            {
                result.Add(category);
            }

            Assert.That(result, Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public async Task Async_ReverseDynamic()
        {
            var result = new List<Product>();
            await foreach (var product in ToAsyncEnumerable(Products).ReverseDynamic())
            {
                result.Add(product);
            }

            Assert.That(result[0].Name, Is.EqualTo("Whatchamacallit"));
            Assert.That(result[^1].Name, Is.EqualTo("Widget"));
        }

        [Test]
        public async Task Async_LastDynamic() =>
            Assert.That((await ToAsyncEnumerable(Products).LastDynamic("p => p.InStock")).Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public async Task Async_SingleDynamic() =>
            Assert.That((await ToAsyncEnumerable(Products).SingleDynamic("""p => p.Category == "Premium" """)).Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public async Task Async_LongCountDynamic() =>
            Assert.That(await ToAsyncEnumerable(Products).LongCountDynamic("p => p.Price > @0", 50m), Is.EqualTo(2L));

        [Test]
        public async Task Async_SelectManyDynamic()
        {
            var result = new List<Order>();
            var nongeneric = new List<Order>();
            await foreach (var order in ToAsyncEnumerable(Customers).SelectManyDynamic<Customer, Order>("c => c.Orders"))
            {
                result.Add(order);
            }
            await foreach (var order in ToAsyncEnumerable(Customers).SelectManyDynamic("c => c.Orders"))
            {
                nongeneric.Add((Order)order!);
            }

            Assert.That(result.Select(o => o.Product), Does.Contain("Laptop"));
            Assert.That(result.Select(o => o.Product), Does.Contain("Phone"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public async Task Async_Chain_WhereSelectCount()
        {
            var names = new List<string>();
            await foreach (var name in ToAsyncEnumerable(Products)
                .WhereDynamic("p => p.InStock")
                .SelectDynamic<Product, string>("p => p.Name"))
            {
                names.Add(name);
            }

            Assert.That(names, Has.Count.EqualTo(4));
            Assert.That(names, Does.Not.Contain("Doohickey"));
        }

        [Test]
        public async Task Async_ComplexPredicate_WithNestedProperties()
        {
            var result = new List<Customer>();
            await foreach (var customer in ToAsyncEnumerable(Customers)
                .WhereDynamic("c => c.Address != null && c.Address.Country == @0 && c.Orders.Count > @1", "US", 1))
            {
                result.Add(customer);
            }

            Assert.That(result.Single().Name, Is.EqualTo("Bob"));
        }

        [Test]
        public async Task Async_StringConcat_InSelector()
        {
            var labels = new List<string>();
            await foreach (var label in ToAsyncEnumerable(Products)
                .SelectDynamic<Product, string>("""p => p.Category + ": " + p.Name"""))
            {
                labels.Add(label);
            }

            Assert.That(labels, Does.Contain("Tools: Widget"));
        }
    }
}
