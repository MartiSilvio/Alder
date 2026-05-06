using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
    [TestFixture]
    [NonParallelizable]
    public class Filtering : CompilerFixtureBase
    {
        [Test]
        public void WhereDynamic_FiltersByPredicate()
        {
            var result = Products.WhereDynamic("p => p.Price > 50m").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_WithVariable()
        {
            var engine = AlderEval.GetEngine();
            engine.SetVariable("threshold", 100m);
            var result = Products.WhereDynamic("p => p.Price > threshold").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_StringMethod()
        {
            var result = Products.WhereDynamic("""p => p.Category == "Electronics" """).ToList();
            Assert.That(result, Has.Count.EqualTo(2));
        }

        [Test]
        public void WhereDynamic_BooleanProperty() =>
            Assert.That(Products.WhereDynamic("p => p.InStock").Count(), Is.EqualTo(4));

        [Test]
        public void WhereDynamic_CompoundPredicate()
        {
            var result = Products.WhereDynamic("p => p.InStock && p.Price < 20m").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Widget", "Thingamajig" }));
        }

        [Test]
        public void WhereDynamic_BodyOnly_ImplicitReceiverMember()
        {
            var result = Products.WhereDynamic("Price > 50m").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_BodyOnly_ExplicitItMember()
        {
            var result = Products.WhereDynamic("it.Price > 50m").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_BodyOnly_ImplicitReceiverMethodCall()
        {
            var result = Products.WhereDynamic("Category.Contains(@0)", "tron").ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Gadget", "Doohickey" }));
        }

        [Test]
        public void WhereDynamic_BodyOnly_InlineVariable() =>
            Assert.That(Products.WhereDynamic("Price > @0", 50m).Count(), Is.EqualTo(2));

        [Test]
        public void WhereDynamic_ParsedPlanExpressionInterop()
        {
            var predicate = AlderEval.GetEngine()
                .ParsePredicate<Product>("Price > 50m")
                .ToExpression<Func<Product, bool>>();

            var result = Products.WhereDynamic(predicate).ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_ParsedPlanCompiledDelegateInterop()
        {
            var predicate = AlderEval.GetEngine()
                .ParsePredicate<Product>("Price > 50m")
                .ToExpression<Func<Product, bool>>();

            var result = Products.WhereDynamic(predicate.Compile()).ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_BodyOnly_UnknownIdentifier_Throws()
        {
            var ex = Assert.Throws<AlderException>(() => Products.WhereDynamic("MissingProp > 0").ToList());
            Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
        }

        [Test]
        public void EmptyCollection_WhereDynamic_ReturnsEmpty() =>
            Assert.That(new List<Product>().WhereDynamic("p => p.InStock").ToList(), Is.Empty);
    }

    [TestFixture]
    [NonParallelizable]
    public class Projection : CompilerFixtureBase
    {
        [Test]
        public void SelectDynamic_ProjectsToString()
        {
            var result = Products.SelectDynamic<Product, string>("p => p.Name").ToList();
            var nongeneric = Products.SelectDynamic("p => p.Name").Cast<string>().ToList();
            Assert.That(result, Is.EquivalentTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit" }));
            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
        }

        [Test]
        public void SelectDynamic_ProjectsToDecimal()
        {
            var result = Products.SelectDynamic<Product, decimal>("p => p.Price").ToList();
            var nongeneric = Products.SelectDynamic("p => p.Price").Cast<decimal>().ToList();
            Assert.That(result, Does.Contain(9.99m));
            Assert.That(nongeneric, Is.EqualTo(result));
            Assert.That(nongeneric[0].GetType(), Is.EqualTo(result[0].GetType()));
        }

        [Test]
        public void SelectDynamic_ParsedPlanExpressionInterop()
        {
            var selector = AlderEval.GetEngine()
                .ParseSelector<Product, decimal>("Price")
                .ToExpression<Func<Product, decimal>>();

            var result = Products.SelectDynamic(selector).ToList();
            Assert.That(result, Does.Contain(9.99m));
            Assert.That(result, Does.Contain(299.99m));
        }

        [Test]
        public void SelectDynamic_BodyOnly_ImplicitReceiverMember()
        {
            var result = Products.SelectDynamic<Product, string>("Name").ToList();
            var nongeneric = Products.SelectDynamic("Name").Cast<string>().ToList();
            Assert.That(result, Is.EquivalentTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit" }));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void SelectDynamic_ProjectsStructuralObject()
        {
            var result = Products.SelectDynamic<Product, IReadOnlyDictionary<string, object?>>("new { Name, Price }").ToList();
            var nongeneric = Products.SelectDynamic("new { Name, Price }").Cast<IReadOnlyDictionary<string, object?>>().ToList();
            var first = result[0];
            var nongenericFirst = nongeneric[0];

            Assert.That(first, Is.Not.InstanceOf<IDictionary<string, object?>>());
            Assert.That(TestHelpers.ReadProjectedMember(first, "Name"), Is.EqualTo("Widget"));
            Assert.That(TestHelpers.ReadProjectedMember(first, "Price"), Is.EqualTo(9.99m));
            Assert.That(nongenericFirst.GetType(), Is.EqualTo(first.GetType()));
            Assert.That(TestHelpers.ReadProjectedMember(nongenericFirst, "Name"), Is.EqualTo("Widget"));
            Assert.That(TestHelpers.ReadProjectedMember(nongenericFirst, "Price"), Is.EqualTo(9.99m));
        }

        [Test]
        public void SelectDynamic_ProjectsStructuralObject_WithAliases()
        {
            var result = Products.SelectDynamic<Product, IReadOnlyDictionary<string, object?>>("new { ProductName = Name, Price }").ToList();
            var nongeneric = Products.SelectDynamic("new { ProductName = Name, Price }").Cast<IReadOnlyDictionary<string, object?>>().ToList();
            var first = result[0];
            var nongenericFirst = nongeneric[0];

            Assert.That(TestHelpers.ReadProjectedMember(first, "ProductName"), Is.EqualTo("Widget"));
            Assert.That(TestHelpers.ReadProjectedMember(first, "Price"), Is.EqualTo(9.99m));
            Assert.That(nongenericFirst.GetType(), Is.EqualTo(first.GetType()));
            Assert.That(TestHelpers.ReadProjectedMember(nongenericFirst, "ProductName"), Is.EqualTo("Widget"));
            Assert.That(TestHelpers.ReadProjectedMember(nongenericFirst, "Price"), Is.EqualTo(9.99m));
        }

        [Test]
        public void SelectDynamic_StructuralProjection_MaterializesToRecordDto()
        {
            var result = Products
                .SelectDynamic<Product, ProductSummaryRecord>("new { Name, Price }")
                .ToList();

            Assert.That(result[0]!.name, Is.EqualTo("Widget"));
            Assert.That(result[0]!.price, Is.EqualTo(9.99m));
        }

        [Test]
        public void SelectDynamic_StructuralProjection_MaterializesToClassDto()
        {
            var result = Products
                .SelectDynamic<Product, ProductSummaryDto>("new { Name, Price }")
                .ToList();

            Assert.That(result[0]!.Name, Is.EqualTo("Widget"));
            Assert.That(result[0]!.Price, Is.EqualTo(9.99m));
        }

        [Test]
        public void IQueryable_SelectDynamic_StructuralProjection_MaterializesToClassDto()
        {
            var result = Products
                .AsQueryable()
                .SelectDynamic<Product, ProductSummaryDto>("new { Name, Price }")
                .ToList();

            Assert.That(result[0]!.Name, Is.EqualTo("Widget"));
            Assert.That(result[0]!.Price, Is.EqualTo(9.99m));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class Ordering : CompilerFixtureBase
    {
        [Test]
        public void OrderByDynamic_SortsByKey()
        {
            var result = Products.OrderByDynamic<Product, decimal>("p => p.Price").ToList();
            var nongeneric = Products.OrderByDynamic("p => p.Price").ToList();
            Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
            Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void OrderByDescendingDynamic_SortsByKeyDescending()
        {
            var result = Products.OrderByDescendingDynamic<Product, decimal>("p => p.Price").ToList();
            var nongeneric = Products.OrderByDescendingDynamic("p => p.Price").ToList();
            Assert.That(result[0].Name, Is.EqualTo("Whatchamacallit"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void ThenByDynamic_SecondarySort()
        {
            var result = Products
                .OrderByDynamic<Product, string>("p => p.Category")
                .ThenByDynamic<Product, decimal>("p => p.Price")
                .ToList();
            var nongeneric = Products
                .OrderByDynamic("p => p.Category")
                .ThenByDynamic("p => p.Price")
                .ToList();
            Assert.That(result[0].Name, Is.EqualTo("Gadget"));
            Assert.That(result[1].Name, Is.EqualTo("Doohickey"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void ThenByDescendingDynamic_SecondarySortDescending()
        {
            var result = Products
                .OrderByDynamic<Product, string>("p => p.Category")
                .ThenByDescendingDynamic<Product, decimal>("p => p.Price")
                .ToList();
            var nongeneric = Products
                .OrderByDynamic("p => p.Category")
                .ThenByDescendingDynamic("p => p.Price")
                .ToList();

            Assert.That(result[0].Name, Is.EqualTo("Doohickey"));
            Assert.That(result[1].Name, Is.EqualTo("Gadget"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void OrderByDynamic_BodyOnly_KeySelector()
        {
            var result = Products.OrderByDynamic<Product, decimal>("Price").ToList();
            var nongeneric = Products.OrderByDynamic("Price").ToList();
            Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
            Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void OrderByDynamic_ParsedPlanExpressionInterop()
        {
            var keySelector = AlderEval.GetEngine()
                .ParseSelector<Product, decimal>("Price")
                .ToExpression<Func<Product, decimal>>();

            var result = Products.OrderByDynamic(keySelector).ToList();
            Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
            Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class Quantifier : CompilerFixtureBase
    {
        [TestCase("p => p.Price > 200m", true)]
        [TestCase("p => p.Price > 1000m", false)]
        public void AnyDynamic(string predicate, bool expected) =>
            Assert.That(Products.AnyDynamic(predicate), Is.EqualTo(expected));

        [TestCase("p => p.Price > 0m", true)]
        [TestCase("p => p.InStock", false)]
        public void AllDynamic(string predicate, bool expected) =>
            Assert.That(Products.AllDynamic(predicate), Is.EqualTo(expected));
    }

    [TestFixture]
    [NonParallelizable]
    public class Element : CompilerFixtureBase
    {
        [Test]
        public void FirstDynamic_ReturnsFirstMatch() =>
            Assert.That(Products.FirstDynamic("""p => p.Category == "Premium" """).Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public void FirstDynamic_NoMatch_Throws() =>
            Assert.Throws<InvalidOperationException>(() => Products.FirstDynamic("""p => p.Category == "X" """));

        [Test]
        public void FirstOrDefaultDynamic_ReturnsNullWhenNoMatch() =>
            Assert.That(Products.FirstOrDefaultDynamic("""p => p.Category == "X" """), Is.Null);

        [Test]
        public void LastDynamic_ReturnsLastMatch() =>
            Assert.That(Products.LastDynamic("p => p.InStock").Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public void LastOrDefaultDynamic_ReturnsNullWhenNoMatch() =>
            Assert.That(Products.LastOrDefaultDynamic("""p => p.Category == "X" """), Is.Null);

        [Test]
        public void SingleDynamic_ReturnsMatch() =>
            Assert.That(Products.SingleDynamic("""p => p.Category == "Premium" """).Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public void SingleDynamic_MultipleMatches_Throws() =>
            Assert.Throws<InvalidOperationException>(() => Products.SingleDynamic("""p => p.Category == "Tools" """));

        [Test]
        public void SingleOrDefaultDynamic_ReturnsMatch() =>
            Assert.That(Products.SingleOrDefaultDynamic("""p => p.Category == "Premium" """)?.Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public void SingleOrDefaultDynamic_ReturnsNullWhenNoMatch() =>
            Assert.That(Products.SingleOrDefaultDynamic("""p => p.Category == "X" """), Is.Null);

        [Test]
        public void IQueryable_LastDynamic_ReturnsLastMatch() =>
            Assert.That(Products.AsQueryable().LastDynamic("p => p.InStock").Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public void IQueryable_LastOrDefaultDynamic_ReturnsNullWhenNoMatch() =>
            Assert.That(Products.AsQueryable().LastOrDefaultDynamic("""p => p.Category == "X" """), Is.Null);

        [Test]
        public void IQueryable_SingleDynamic_ReturnsMatch() =>
            Assert.That(Products.AsQueryable().SingleDynamic("""p => p.Category == "Premium" """).Name, Is.EqualTo("Whatchamacallit"));

        [Test]
        public void IQueryable_SingleOrDefaultDynamic_ReturnsNullWhenNoMatch() =>
            Assert.That(Products.AsQueryable().SingleOrDefaultDynamic("""p => p.Category == "X" """), Is.Null);
    }

    [TestFixture]
    [NonParallelizable]
    public class Grouping : CompilerFixtureBase
    {
        [Test]
        public void GroupByDynamic_GroupsByKey()
        {
            var groups = Products.GroupByDynamic<Product, string>("p => p.Category").ToList();
            var nongeneric = Products.GroupByDynamic("p => p.Category")
                .Cast<IGrouping<string, Product>>()
                .ToList();
            Assert.That(groups, Has.Count.EqualTo(3));
            Assert.That(groups.Select(g => g.Key), Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
            Assert.That(nongeneric.Select(g => g.Key), Is.EqualTo(groups.Select(g => g.Key)));
        }

        [Test]
        public void GroupByDynamic_BodyOnly_KeySelector()
        {
            var groups = Products.GroupByDynamic<Product, string>("Category").ToList();
            var nongeneric = Products.GroupByDynamic("Category")
                .Cast<IGrouping<string, Product>>()
                .ToList();
            Assert.That(groups, Has.Count.EqualTo(3));
            Assert.That(groups.Select(g => g.Key), Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
            Assert.That(nongeneric.Select(g => g.Key), Is.EqualTo(groups.Select(g => g.Key)));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class SetOperations : CompilerFixtureBase
    {
        [Test]
        public void ConcatDynamic_AppendsSecondSequence()
        {
            var first = Products.Take(2);
            var second = Products.Skip(2).Take(2);

            var result = first.ConcatDynamic(second).Select(p => p.Name).ToList();

            Assert.That(result, Is.EqualTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig" }));
        }

        [Test]
        public void IQueryable_ConcatDynamic_AppendsSecondSequence()
        {
            var first = Products.Take(2).AsQueryable();
            var second = Products.Skip(2).Take(2).AsQueryable();

            var result = first.ConcatDynamic(second).Select(p => p.Name).ToList();

            Assert.That(result, Is.EqualTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig" }));
        }

        [Test]
        public void UnionDynamic_RemovesDuplicatesAcrossSequences()
        {
            var first = new[] { "Tools", "Electronics" };
            var second = new[] { "Electronics", "Premium" };

            var result = first.UnionDynamic(second).ToList();

            Assert.That(result, Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void IQueryable_UnionDynamic_RemovesDuplicatesAcrossSequences()
        {
            var first = new[] { "Tools", "Electronics" }.AsQueryable();
            var second = new[] { "Electronics", "Premium" }.AsQueryable();

            var result = first.UnionDynamic(second).ToList();

            Assert.That(result, Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void IntersectDynamic_ReturnsSharedValues()
        {
            var first = new[] { "Tools", "Electronics", "Premium" };
            var second = new[] { "Electronics", "Premium", "Office" };

            var result = first.IntersectDynamic(second).ToList();

            Assert.That(result, Is.EquivalentTo(new[] { "Electronics", "Premium" }));
        }

        [Test]
        public void IQueryable_IntersectDynamic_ReturnsSharedValues()
        {
            var first = new[] { "Tools", "Electronics", "Premium" }.AsQueryable();
            var second = new[] { "Electronics", "Premium", "Office" }.AsQueryable();

            var result = first.IntersectDynamic(second).ToList();

            Assert.That(result, Is.EquivalentTo(new[] { "Electronics", "Premium" }));
        }

        [Test]
        public void ExceptDynamic_RemovesValuesPresentInSecondSequence()
        {
            var first = new[] { "Tools", "Electronics", "Premium" };
            var second = new[] { "Electronics" };

            var result = first.ExceptDynamic(second).ToList();

            Assert.That(result, Is.EquivalentTo(new[] { "Tools", "Premium" }));
        }

        [Test]
        public void IQueryable_ExceptDynamic_RemovesValuesPresentInSecondSequence()
        {
            var first = new[] { "Tools", "Electronics", "Premium" }.AsQueryable();
            var second = new[] { "Electronics" }.AsQueryable();

            var result = first.ExceptDynamic(second).ToList();

            Assert.That(result, Is.EquivalentTo(new[] { "Tools", "Premium" }));
        }

        [Test]
        public void DistinctByDynamic_RemovesDuplicateKeys() =>
            Assert.That(Products.DistinctByDynamic<Product, string>("p => p.Category").Count(), Is.EqualTo(3));

        [Test]
        public void DistinctByDynamic_BodyOnly_KeySelector() =>
            Assert.That(Products.DistinctByDynamic<Product, string>("Category").Count(), Is.EqualTo(3));

        [Test]
        public void DistinctDynamic_RemovesDuplicateValues()
        {
            var categories = new[] { "Tools", "Tools", "Electronics", "Premium", "Electronics" };
            var result = categories.DistinctDynamic().ToList();
            Assert.That(result, Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void ReverseDynamic_ReversesSequence()
        {
            var result = Products.Select(p => p.Name).ReverseDynamic().ToList();
            Assert.That(result[0], Is.EqualTo("Whatchamacallit"));
            Assert.That(result[^1], Is.EqualTo("Widget"));
        }

        [Test]
        public void DefaultIfEmptyDynamic_NonEmptySequence_ReturnsOriginalValues()
        {
            var result = Products.Select(p => p.Name).DefaultIfEmptyDynamic().ToList();
            Assert.That(result, Is.EqualTo(Products.Select(p => p.Name).ToList()));
        }

        [Test]
        public void DefaultIfEmptyDynamic_EmptyReferenceSequence_ReturnsNull()
        {
            string[] values = [];

            var result = values.DefaultIfEmptyDynamic().ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.Null);
        }

        [Test]
        public void DefaultIfEmptyDynamic_EmptyValueSequence_ReturnsDefaultValue()
        {
            int[] values = [];

            var result = values.DefaultIfEmptyDynamic().ToList();

            Assert.That(result, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void IQueryable_DefaultIfEmptyDynamic_EmptySequence_ReturnsDefaultValue()
        {
            var result = Array.Empty<int>().AsQueryable().DefaultIfEmptyDynamic().ToList();

            Assert.That(result, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void DefaultIfEmptyDynamic_WithDefaultValue_EmptySequence_ReturnsProvidedValue()
        {
            string[] values = [];

            var result = values.DefaultIfEmptyDynamic("fallback").ToList();

            Assert.That(result, Is.EqualTo(new[] { "fallback" }));
        }

        [Test]
        public void IQueryable_DefaultIfEmptyDynamic_WithDefaultValue_EmptySequence_ReturnsProvidedValue()
        {
            var result = Array.Empty<string>().AsQueryable().DefaultIfEmptyDynamic("fallback").ToList();

            Assert.That(result, Is.EqualTo(new[] { "fallback" }));
        }

        [Test]
        public void OfTypeDynamic_FiltersMatchingRuntimeValues()
        {
            IEnumerable values = new object?[] { 1, "two", null, 3, "four", 5L };

            var result = values.OfTypeDynamic<int>().ToList();

            Assert.That(result, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void IQueryable_OfTypeDynamic_FiltersMatchingRuntimeValues()
        {
            IQueryable values = new object?[] { 1, "two", null, 3, "four", 5L }.AsQueryable();

            var result = values.OfTypeDynamic<int>().ToList();

            Assert.That(result, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void CastDynamic_CastsMatchingValues()
        {
            IEnumerable values = new object[] { "alpha", "beta" };

            var result = values.CastDynamic<string>().ToList();

            Assert.That(result, Is.EqualTo(new[] { "alpha", "beta" }));
        }

        [Test]
        public void CastDynamic_InvalidValue_Throws()
        {
            IEnumerable values = new object?[] { "alpha", 2 };

            Assert.Throws<InvalidCastException>(() => values.CastDynamic<string>().ToList());
        }

        [Test]
        public void IQueryable_CastDynamic_CastsMatchingValues()
        {
            IQueryable values = new object[] { "alpha", "beta" }.AsQueryable();

            var result = values.CastDynamic<string>().ToList();

            Assert.That(result, Is.EqualTo(new[] { "alpha", "beta" }));
        }

        [Test]
        public void IQueryable_CastDynamic_InvalidValue_Throws()
        {
            IQueryable values = new object?[] { "alpha", 2 }.AsQueryable();

            Assert.Throws<InvalidCastException>(() => values.CastDynamic<string>().ToList());
        }

        [Test]
        public void ContainsDynamic_FindsExistingValue()
        {
            var categories = new[] { "Tools", "Electronics", "Premium" };

            Assert.That(categories.ContainsDynamic("Electronics"), Is.True);
        }

        [Test]
        public void ContainsDynamic_MissingValue_ReturnsFalse()
        {
            var categories = new[] { "Tools", "Electronics", "Premium" };

            Assert.That(categories.ContainsDynamic("Office"), Is.False);
        }

        [Test]
        public void IQueryable_ContainsDynamic_FindsExistingValue()
        {
            var categories = new[] { "Tools", "Electronics", "Premium" }.AsQueryable();

            Assert.That(categories.ContainsDynamic("Electronics"), Is.True);
        }

        [Test]
        public void SequenceEqualDynamic_ReturnsTrueForEqualSequences()
        {
            var first = new[] { "Tools", "Electronics", "Premium" };
            var second = new[] { "Tools", "Electronics", "Premium" };

            Assert.That(first.SequenceEqualDynamic(second), Is.True);
        }

        [Test]
        public void SequenceEqualDynamic_ReturnsFalseForDifferentSequences()
        {
            var first = new[] { "Tools", "Electronics", "Premium" };
            var second = new[] { "Tools", "Premium", "Electronics" };

            Assert.That(first.SequenceEqualDynamic(second), Is.False);
        }

        [Test]
        public void IQueryable_SequenceEqualDynamic_ReturnsTrueForEqualSequences()
        {
            var first = new[] { "Tools", "Electronics", "Premium" }.AsQueryable();
            var second = new[] { "Tools", "Electronics", "Premium" }.AsQueryable();

            Assert.That(first.SequenceEqualDynamic(second), Is.True);
        }

        [Test]
        public void AppendDynamic_AppendsValue()
        {
            var result = new[] { "Tools", "Electronics" }.AppendDynamic("Premium").ToList();

            Assert.That(result, Is.EqualTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void PrependDynamic_PrependsValue()
        {
            var result = new[] { "Electronics", "Premium" }.PrependDynamic("Tools").ToList();

            Assert.That(result, Is.EqualTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void IQueryable_AppendDynamic_AppendsValue()
        {
            var result = new[] { "Tools", "Electronics" }.AsQueryable().AppendDynamic("Premium").ToList();

            Assert.That(result, Is.EqualTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void IQueryable_PrependDynamic_PrependsValue()
        {
            var result = new[] { "Electronics", "Premium" }.AsQueryable().PrependDynamic("Tools").ToList();

            Assert.That(result, Is.EqualTo(new[] { "Tools", "Electronics", "Premium" }));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class Paging : CompilerFixtureBase
    {
        [Test]
        public void SkipDynamic_SkipsFirstItems()
        {
            var result = Products.SkipDynamic(2).Select(p => p.Name).ToList();
            Assert.That(result, Is.EqualTo(new[] { "Doohickey", "Thingamajig", "Whatchamacallit" }));
        }

        [Test]
        public void TakeDynamic_TakesFirstItems()
        {
            var result = Products.TakeDynamic(2).Select(p => p.Name).ToList();
            Assert.That(result, Is.EqualTo(new[] { "Widget", "Gadget" }));
        }

        [Test]
        public void SkipWhileDynamic_SkipsWhilePredicateMatches()
        {
            var ordered = Products.OrderByDynamic<Product, decimal>("Price");
            var result = ordered.SkipWhileDynamic<Product>("p => p.Price < 10m").Select(p => p.Name).ToList();
            Assert.That(result, Is.EqualTo(new[] { "Gadget", "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void TakeWhileDynamic_TakesWhilePredicateMatches()
        {
            var ordered = Products.OrderByDynamic<Product, decimal>("Price");
            var result = ordered.TakeWhileDynamic<Product>("p => p.Price < 10m").Select(p => p.Name).ToList();
            Assert.That(result, Is.EqualTo(new[] { "Thingamajig", "Widget" }));
        }

        [Test]
        public void IQueryable_SkipTakeDynamic_Composes()
        {
            var result = Products.AsQueryable()
                .SkipDynamic(1)
                .TakeDynamic(2)
                .Select(p => p.Name)
                .ToList();

            Assert.That(result, Is.EqualTo(new[] { "Gadget", "Doohickey" }));
        }

        [Test]
        public void ElementAtDynamic_ReturnsElementAtIndex()
        {
            var result = Products.Select(p => p.Name).ElementAtDynamic(2);

            Assert.That(result, Is.EqualTo("Doohickey"));
        }

        [Test]
        public void ElementAtDynamic_OutOfRange_Throws()
        {
            var names = Products.Select(p => p.Name);

            Assert.Throws<ArgumentOutOfRangeException>(() => names.ElementAtDynamic(99));
        }

        [Test]
        public void ElementAtOrDefaultDynamic_ReturnsElementAtIndex()
        {
            var result = Products.Select(p => p.Name).ElementAtOrDefaultDynamic(2);

            Assert.That(result, Is.EqualTo("Doohickey"));
        }

        [Test]
        public void ElementAtOrDefaultDynamic_OutOfRange_ReturnsDefault()
        {
            var result = Products.Select(p => p.Name).ElementAtOrDefaultDynamic(99);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void IQueryable_ElementAtDynamic_ReturnsElementAtIndex()
        {
            var result = Products.AsQueryable()
                .OrderByDynamic<Product, decimal>("Price")
                .Select(p => p.Name)
                .ElementAtDynamic(2);

            Assert.That(result, Is.EqualTo("Gadget"));
        }

        [Test]
        public void IQueryable_ElementAtOrDefaultDynamic_OutOfRange_ReturnsDefault()
        {
            var result = Products.AsQueryable()
                .Select(p => p.Name)
                .ElementAtOrDefaultDynamic(99);

            Assert.That(result, Is.Null);
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class Aggregation : CompilerFixtureBase
    {
        [TestCase("p => p.InStock", 4)]
        [TestCase("p => p.Price > 50m", 2)]
        public void CountDynamic(string predicate, int expected) =>
            Assert.That(Products.CountDynamic(predicate), Is.EqualTo(expected));

        [Test]
        public void SumDynamic_SumsValues()
        {
            var result = Products.SumDynamic("p => p.Price");
            Assert.That(result, Is.EqualTo(514.95m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public void SumDynamic_ParsedPlanExpressionInterop()
        {
            var selector = AlderEval.GetEngine()
                .ParseSelector<Product, decimal>("Price")
                .ToExpression<Func<Product, decimal>>();

            Assert.That(Products.SumDynamic(selector), Is.EqualTo(514.95m));
        }

        [Test]
        public void AverageDynamic_AveragesValues()
        {
            var result = Products.AverageDynamic("p => (double)p.Price");
            Assert.That(result, Is.EqualTo(102.99).Within(0.01));
            Assert.That(result.GetType(), Is.EqualTo(typeof(double)));
        }

        [Test]
        public void MinDynamic_FindsMinimum()
        {
            var result = Products.MinDynamic("p => p.Price");
            Assert.That(result, Is.EqualTo(4.99m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public void MaxDynamic_FindsMaximum()
        {
            var result = Products.MaxDynamic("p => p.Price");
            Assert.That(result, Is.EqualTo(299.99m));
            Assert.That(result.GetType(), Is.EqualTo(typeof(decimal)));
        }

        [Test]
        public void LongCountDynamic_CountsValues() =>
            Assert.That(Products.LongCountDynamic("p => p.Price > 50m"), Is.EqualTo(2L));
    }

    [TestFixture]
    [NonParallelizable]
    public class FlatteningAndJoins : CompilerFixtureBase
    {
        private static readonly List<WarehouseStock> WarehouseStocks =
        [
            new("Tools", 12),
            new("Electronics", 5),
            new("Premium", 1)
        ];

        [Test]
        public void SelectManyDynamic_FlattensNestedCollections()
        {
            var result = Customers
                .SelectManyDynamic<Customer, Order>("c => c.Orders")
                .Select(o => o.Product)
                .ToList();
            var nongeneric = Customers.SelectManyDynamic("c => c.Orders")
                .Cast<Order>()
                .Select(o => o.Product)
                .ToList();

            Assert.That(result, Does.Contain("Laptop"));
            Assert.That(result, Does.Contain("Phone"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void SelectManyDynamic_WithResultSelector_ProjectsOuterAndInner()
        {
            var result = Customers
                .SelectManyDynamic<Customer, Order, string>(
                    "c => c.Orders",
                    """(outer, inner) => outer.Name + ":" + inner.Product""")
                .ToList();
            var nongeneric = Customers
                .SelectManyDynamic(
                    "c => c.Orders",
                    """(outer, inner) => outer.Name + ":" + inner.Product""")
                .Cast<string>()
                .ToList();

            Assert.That(result, Does.Contain("Alice:Laptop"));
            Assert.That(result, Does.Contain("Bob:Monitor"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void IQueryable_SelectManyDynamic_FlattensNestedCollections()
        {
            var result = Customers.AsQueryable()
                .SelectManyDynamic<Customer, Order>("c => c.Orders")
                .Select(o => o.Product)
                .ToList();
            var nongeneric = Customers.AsQueryable()
                .SelectManyDynamic("c => c.Orders")
                .Cast<Order>()
                .Select(o => o.Product)
                .ToList();

            Assert.That(result, Does.Contain("Laptop"));
            Assert.That(result, Does.Contain("Phone"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void JoinDynamic_JoinsOnDynamicKeys()
        {
            var result = Products.JoinDynamic<Product, WarehouseStock, string, string>(
                WarehouseStocks,
                "p => p.Category",
                "s => s.Category",
                """(outer, inner) => outer.Name + ":" + inner.Count""")
                .ToList();
            var nongeneric = Products.JoinDynamic(
                    WarehouseStocks,
                    "p => p.Category",
                    "s => s.Category",
                    """(outer, inner) => outer.Name + ":" + inner.Count""")
                .Cast<string>()
                .ToList();

            Assert.That(result, Does.Contain("Widget:12"));
            Assert.That(result, Does.Contain("Doohickey:5"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void IQueryable_JoinDynamic_JoinsOnDynamicKeys()
        {
            var result = Products.AsQueryable().JoinDynamic<Product, WarehouseStock, string, string>(
                WarehouseStocks,
                "p => p.Category",
                "s => s.Category",
                """(outer, inner) => outer.Name + ":" + inner.Count""")
                .ToList();
            var nongeneric = Products.AsQueryable().JoinDynamic(
                    WarehouseStocks,
                    "p => p.Category",
                    "s => s.Category",
                    """(outer, inner) => outer.Name + ":" + inner.Count""")
                .Cast<string>()
                .ToList();

            Assert.That(result, Does.Contain("Widget:12"));
            Assert.That(result, Does.Contain("Doohickey:5"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void GroupJoinDynamic_GroupsMatches()
        {
            var result = Products.GroupJoinDynamic<Product, WarehouseStock, string, string>(
                WarehouseStocks,
                "p => p.Category",
                "s => s.Category",
                """(outer, group) => outer.Name + ":" + group.Count()""")
                .ToList();
            var nongeneric = Products.GroupJoinDynamic(
                    WarehouseStocks,
                    "p => p.Category",
                    "s => s.Category",
                    """(outer, group) => outer.Name + ":" + group.Count()""")
                .Cast<string>()
                .ToList();

            Assert.That(result, Does.Contain("Widget:1"));
            Assert.That(result, Does.Contain("Whatchamacallit:1"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void JoinDynamic_CustomEngineOverload_Works()
        {
            using var engine = new AlderEngine(o => o.UseCompiler());

            var result = Products.JoinDynamic<Product, WarehouseStock, string, string>(
                WarehouseStocks,
                engine,
                "p => p.Category",
                "s => s.Category",
                """(outer, inner) => outer.Name + ":" + inner.Count""")
                .ToList();

            Assert.That(result, Does.Contain("Widget:12"));
            Assert.That(result, Does.Contain("Doohickey:5"));
        }

        [Test]
        public void IQueryable_GroupJoinDynamic_GroupsMatches()
        {
            var result = Products.AsQueryable().GroupJoinDynamic<Product, WarehouseStock, string, string>(
                WarehouseStocks,
                "p => p.Category",
                "s => s.Category",
                """(outer, group) => outer.Name + ":" + group.Count()""")
                .ToList();
            var nongeneric = Products.AsQueryable().GroupJoinDynamic(
                    WarehouseStocks,
                    "p => p.Category",
                    "s => s.Category",
                    """(outer, group) => outer.Name + ":" + group.Count()""")
                .Cast<string>()
                .ToList();

            Assert.That(result, Does.Contain("Widget:1"));
            Assert.That(result, Does.Contain("Whatchamacallit:1"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class Diagnostics : CompilerFixtureBase
    {
        [Test]
        public void NoCompiler_ThrowsClearError()
        {
            using var engine = new AlderEngine();
            var ex = Assert.Throws<InvalidOperationException>(() => Products.WhereDynamic(engine, "p => p.InStock"));
            Assert.That(ex!.Message, Does.Contain("UseCompiler"));
        }
    }
}
