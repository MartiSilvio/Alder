using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class PreparedPlans : CompilerFixtureBase
    {
        [Test]
        public void ParsePredicate_ReusesPlanAcrossEnumerableAndQueryable()
        {
            var plan = AlderEval.GetEngine().ParsePredicate<Product>("Price > 50m");

            var enumerable = Products.WhereDynamic(plan).Select(p => p.Name).ToList();
            var queryable = Products.AsQueryable().WhereDynamic(plan).Select(p => p.Name).ToList();

            Assert.That(enumerable, Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
            Assert.That(queryable, Is.EqualTo(enumerable));
        }

        [Test]
        public void ParseSelector_ReusesPlanForTypedProjection()
        {
            var plan = AlderEval.GetEngine().ParseSelector<Product, decimal>("Price");

            var enumerable = Products.SelectDynamic<Product, decimal>(plan).ToList();
            var queryable = Products.AsQueryable().SelectDynamic<Product, decimal>(plan).ToList();

            Assert.That(enumerable, Is.EqualTo(Products.Select(p => p.Price).ToList()));
            Assert.That(queryable, Is.EqualTo(enumerable));
        }

        [Test]
        public void ParsedPlans_FeedTerminalAndOrderingOperators()
        {
            var engine = AlderEval.GetEngine();
            var predicate = engine.ParsePredicate<Product>("InStock");
            var price = engine.ParseSelector<Product, decimal>("Price");

            var count = Products.CountDynamic(predicate);
            var total = Products.SumDynamic(price);
            var ordered = Products.OrderByDescendingDynamic<Product, decimal>(price).Select(p => p.Name).First();

            Assert.That(count, Is.EqualTo(4));
            Assert.That(total, Is.EqualTo(514.95m));
            Assert.That(ordered, Is.EqualTo("Whatchamacallit"));
        }

        [Test]
        public void ParsePredicate_ExposesExpressionInteropView()
        {
            var engine = AlderEval.GetEngine();
            var plan = engine.ParsePredicate<Product>("Price > 50m");
            var expression = plan.ToExpression<Func<Product, bool>>();

            Assert.That(expression.ToString(), Is.EqualTo("it => (it.Price > 50)"));
        }
    }
}
