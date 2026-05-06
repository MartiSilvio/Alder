using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class DispatchShape : CompilerFixtureBase
    {
        [Test]
        public void EnumerableDispatcher_OrderBy_UsesInferredKeyType()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var source = Products.AsEnumerable();

            var ordered = DynamicQueryDispatcher.OrderBy(
                source,
                engine,
                "Price",
                variables: null,
                descending: false);

            Assert.That(ordered, Is.InstanceOf<IOrderedEnumerable<Product>>());
            Assert.That(((IEnumerable<Product>)ordered).Select(static product => product.Name),
                Is.EqualTo(new[] { "Thingamajig", "Widget", "Gadget", "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void QueryableDispatcher_OrderBy_UsesInferredKeyType()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var source = Products.AsQueryable();

            var ordered = DynamicQueryDispatcher.OrderBy(
                source,
                engine,
                "Price",
                variables: null,
                descending: false);

            Assert.That(ordered, Is.InstanceOf<IOrderedQueryable<Product>>());
            Assert.That(((IQueryable<Product>)ordered).Expression, Is.InstanceOf<MethodCallExpression>());
        }

        [Test]
        public void QueryableDispatcher_Select_UsesInferredScalarResultType()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var source = Products.AsQueryable();

            var projected = DynamicQueryDispatcher.Select(
                source,
                engine,
                "Name",
                variables: null);

            Assert.That(projected, Is.InstanceOf<IQueryable<string>>());
            Assert.That(((IQueryable<string>)projected).ToList(),
                Is.EqualTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit" }));
        }
    }
}
