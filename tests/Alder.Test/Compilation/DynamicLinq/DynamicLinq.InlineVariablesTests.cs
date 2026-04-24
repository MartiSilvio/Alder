namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class InlineVariables : CompilerFixtureBase
    {
        [Test]
        public void WhereDynamic_InlineVariable() =>
            Assert.That(Products.WhereDynamic("p => p.Price > @0", 50m).Count(), Is.EqualTo(2));

        [Test]
        public void WhereDynamic_MultipleInlineVariables()
        {
            var result = Products.WhereDynamic("""p => p.Price > @0 && p.Category == @1""", 10m, "Electronics").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Gadget", "Doohickey" }));
        }

        [Test]
        public void WhereDynamic_CustomEngine()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            Assert.That(Products.WhereDynamic(engine, "p => p.Price > @0", 100m).Count(), Is.EqualTo(2));
        }

        [Test]
        public void WhereDynamic_NamedVariablesViaAnonymousObject() =>
            Assert.That(Products.WhereDynamic("p => p.Price > threshold", new { threshold = 50m }).Count(), Is.EqualTo(2));

        [Test]
        public void WhereDynamic_MixedInlineAndNamed()
        {
            var result = Products.WhereDynamic(
                """p => p.Price > @0 && p.Category == category""",
                10m, new { category = "Electronics" }).ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Gadget", "Doohickey" }));
        }

        [Test]
        public void SelectDynamic_WithInlineVariable()
        {
            var result = Products.SelectDynamic<Product, decimal>("p => p.Price * @0", 2m).ToList();
            var nongeneric = Products.SelectDynamic("p => p.Price * @0", 2m).Cast<decimal>().ToList();
            Assert.That(result, Does.Contain(19.98m));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void Engine_Evaluate_InlineVariables()
        {
            using var engine = new AlderEngine();
            Assert.That(engine.Evaluate("return @0 + @1;", 3, 4), Is.EqualTo(7));
        }

        [Test]
        public void Engine_Evaluate_InlineVariables_Generic()
        {
            using var engine = new AlderEngine();
            Assert.That(engine.Evaluate<int>("return @0 * @1;", 6, 7), Is.EqualTo(42));
        }

        [Test]
        public void Engine_Evaluate_NamedVariablesViaAnonymousObject()
        {
            using var engine = new AlderEngine();
            Assert.That(engine.Evaluate("return x + y;", new { x = 10, y = 20 }), Is.EqualTo(30));
        }

        [Test]
        public void Engine_Evaluate_DictionaryVariables()
        {
            using var engine = new AlderEngine();
            var vars = new Dictionary<string, object?> { ["a"] = 5, ["b"] = 3 };
            Assert.That(engine.Evaluate("return a - b;", vars), Is.EqualTo(2));
        }

        [Test]
        public void ParallelInlineVariables_ThreadSafe()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());
            var results = Enumerable.Range(0, 100)
                .AsParallel()
                .Select(i => Products.WhereDynamic(engine, "p => p.Price > @0", (decimal)i).Count())
                .ToList();
            Assert.That(results, Has.Count.EqualTo(100));
            Assert.That(results[0], Is.EqualTo(5));
            Assert.That(results[99], Is.EqualTo(2));
        }
    }
}
