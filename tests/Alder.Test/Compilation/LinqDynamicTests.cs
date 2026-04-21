using Alder.Test.Integration;
using Alder.Test._Infrastructure;
using Alder.Diagnostics;
using System.Linq.Expressions;
using Alder.Compiled.DynamicLinq;

namespace Alder.Test.Compilation;

public record Product(string Name, decimal Price, string Category, bool InStock);

public record Customer(string Name, int Age, Address? Address, List<Order> Orders);
public record Address(string City, string Country, string? PostalCode);
public record Order(string Product, int Quantity, decimal UnitPrice, DateTime OrderDate, string? Notes);

[TestFixture]
[NonParallelizable]
public class LinqDynamicTests
{
    private static readonly object CompilerGate = new();
    private static bool _compilerConfigured;

    public abstract class CompilerFixtureBase
    {
        [OneTimeSetUp]
        public void EnsureCompiler()
        {
            if (_compilerConfigured)
            {
                return;
            }

            lock (CompilerGate)
            {
                if (_compilerConfigured)
                {
                    return;
                }

                AlderEval.Reset();
                AlderEval.Configure(o => o.UseCompiler());
                _compilerConfigured = true;
            }
        }

        [OneTimeTearDown]
        public void ResetCompiler()
        {
            lock (CompilerGate)
            {
                if (!_compilerConfigured)
                    return;

                AlderEval.Reset();
                _compilerConfigured = false;
            }
        }
    }

    private static readonly List<Product> Products =
    [
        new("Widget", 9.99m, "Tools", true),
        new("Gadget", 49.99m, "Electronics", true),
        new("Doohickey", 149.99m, "Electronics", false),
        new("Thingamajig", 4.99m, "Tools", true),
        new("Whatchamacallit", 299.99m, "Premium", true)
    ];

    private static readonly List<Customer> Customers =
    [
        new("Alice", 32, new Address("London", "UK", "EC1A 1BB"),
        [
            new("Laptop", 1, 999.99m, new DateTime(2025, 1, 15), "Express shipping"),
            new("Mouse", 2, 29.99m, new DateTime(2025, 2, 10), null)
        ]),
        new("Bob", 25, new Address("New York", "US", "10001"),
        [
            new("Keyboard", 1, 149.99m, new DateTime(2025, 3, 5), "Gift wrap"),
            new("Monitor", 1, 549.99m, new DateTime(2025, 3, 5), null),
            new("Cable", 5, 9.99m, new DateTime(2025, 4, 1), null)
        ]),
        new("Cara", 41, new Address("Berlin", "DE", null),
        [
            new("Tablet", 1, 399.99m, new DateTime(2025, 6, 20), "Engraved")
        ]),
        new("Dan", 19, null, []),
        new("Eve", 37, new Address("Tokyo", "JP", "100-0001"),
        [
            new("Phone", 1, 1199.99m, new DateTime(2024, 12, 25), null),
            new("Case", 3, 19.99m, new DateTime(2025, 1, 2), "Bulk order")
        ])
    ];

    #region Core LINQ operations

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
        public void WhereDynamic_PreParsedExpression()
        {
            var predicate = (Expression<Func<Product, bool>>)AlderEval.GetEngine()
                .ParsePredicateExpression(typeof(Product), "Price > 50m");

            var result = Products.WhereDynamic(predicate).ToList();
            Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
        }

        [Test]
        public void WhereDynamic_PreParsedDelegate()
        {
            var predicate = (Expression<Func<Product, bool>>)AlderEval.GetEngine()
                .ParsePredicateExpression(typeof(Product), "Price > 50m");

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
            Assert.That(result, Is.EquivalentTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit" }));
        }

        [Test]
        public void SelectDynamic_ProjectsToDecimal()
        {
            var result = Products.SelectDynamic<Product, decimal>("p => p.Price").ToList();
            Assert.That(result, Does.Contain(9.99m));
        }

        [Test]
        public void SelectDynamic_PreParsedExpression()
        {
            var selector = (Expression<Func<Product, decimal>>)AlderEval.GetEngine()
                .ParseSelectorExpression(typeof(Product), typeof(decimal), "Price");

            var result = Products.SelectDynamic(selector).ToList();
            Assert.That(result, Does.Contain(9.99m));
            Assert.That(result, Does.Contain(299.99m));
        }

        [Test]
        public void SelectDynamic_BodyOnly_ImplicitReceiverMember()
        {
            var result = Products.SelectDynamic<Product, string>("Name").ToList();
            Assert.That(result, Is.EquivalentTo(new[] { "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit" }));
        }

        [Test]
        public void SelectDynamic_ProjectsStructuralObject()
        {
            var result = Products.SelectDynamic<Product, object>("new { Name, Price }").ToList();
            var first = result[0];

            Assert.That(first, Is.Not.InstanceOf<IDictionary<string, object?>>());
            Assert.That(TestHelpers.ReadProjectedMember(first, "Name"), Is.EqualTo("Widget"));
            Assert.That(TestHelpers.ReadProjectedMember(first, "Price"), Is.EqualTo(9.99m));
        }

        [Test]
        public void SelectDynamic_ProjectsStructuralObject_WithAliases()
        {
            var result = Products.SelectDynamic<Product, object>("new { ProductName = Name, Price }").ToList();
            var first = result[0];

            Assert.That(TestHelpers.ReadProjectedMember(first, "ProductName"), Is.EqualTo("Widget"));
            Assert.That(TestHelpers.ReadProjectedMember(first, "Price"), Is.EqualTo(9.99m));
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
            Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
            Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
        }

        [Test]
        public void OrderByDescendingDynamic_SortsByKeyDescending()
        {
            var result = Products.OrderByDescendingDynamic<Product, decimal>("p => p.Price").ToList();
            Assert.That(result[0].Name, Is.EqualTo("Whatchamacallit"));
        }

        [Test]
        public void ThenByDynamic_SecondarySort()
        {
            var result = Products
                .OrderByDynamic<Product, string>("p => p.Category")
                .ThenByDynamic<Product, decimal>("p => p.Price")
                .ToList();
            Assert.That(result[0].Name, Is.EqualTo("Gadget"));
            Assert.That(result[1].Name, Is.EqualTo("Doohickey"));
        }

        [Test]
        public void ThenByDescendingDynamic_SecondarySortDescending()
        {
            var result = Products
                .OrderByDynamic<Product, string>("p => p.Category")
                .ThenByDescendingDynamic<Product, decimal>("p => p.Price")
                .ToList();

            Assert.That(result[0].Name, Is.EqualTo("Doohickey"));
            Assert.That(result[1].Name, Is.EqualTo("Gadget"));
        }

        [Test]
        public void OrderByDynamic_BodyOnly_KeySelector()
        {
            var result = Products.OrderByDynamic<Product, decimal>("Price").ToList();
            Assert.That(result[0].Name, Is.EqualTo("Thingamajig"));
            Assert.That(result[^1].Name, Is.EqualTo("Whatchamacallit"));
        }

        [Test]
        public void OrderByDynamic_PreParsedExpression()
        {
            var keySelector = (Expression<Func<Product, decimal>>)AlderEval.GetEngine()
                .ParseSelectorExpression(typeof(Product), typeof(decimal), "Price");

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
    }

    [TestFixture]
    [NonParallelizable]
    public class Grouping : CompilerFixtureBase
    {
        [Test]
        public void GroupByDynamic_GroupsByKey()
        {
            var groups = Products.GroupByDynamic<Product, string>("p => p.Category").ToList();
            Assert.That(groups, Has.Count.EqualTo(3));
            Assert.That(groups.Select(g => g.Key), Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
        }

        [Test]
        public void GroupByDynamic_BodyOnly_KeySelector()
        {
            var groups = Products.GroupByDynamic<Product, string>("Category").ToList();
            Assert.That(groups, Has.Count.EqualTo(3));
            Assert.That(groups.Select(g => g.Key), Is.EquivalentTo(new[] { "Tools", "Electronics", "Premium" }));
        }
    }

    [TestFixture]
    [NonParallelizable]
    public class SetOperations : CompilerFixtureBase
    {
        [Test]
        public void DistinctByDynamic_RemovesDuplicateKeys() =>
            Assert.That(Products.DistinctByDynamic<Product, string>("p => p.Category").Count(), Is.EqualTo(3));

        [Test]
        public void DistinctByDynamic_BodyOnly_KeySelector() =>
            Assert.That(Products.DistinctByDynamic<Product, string>("Category").Count(), Is.EqualTo(3));
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
        public void SumDynamic_SumsValues() =>
            Assert.That(Products.SumDynamic("p => p.Price"), Is.EqualTo(514.95m));

        [Test]
        public void SumDynamic_PreParsedExpression()
        {
            var selector = (Expression<Func<Product, decimal>>)AlderEval.GetEngine()
                .ParseSelectorExpression(typeof(Product), typeof(decimal), "Price");

            Assert.That(Products.SumDynamic(selector), Is.EqualTo(514.95m));
        }

        [Test]
        public void AverageDynamic_AveragesValues() =>
            Assert.That(Products.AverageDynamic("p => (double)p.Price"), Is.EqualTo(102.99).Within(0.01));

        [Test]
        public void MinDynamic_FindsMinimum() =>
            Assert.That(Products.MinDynamic<Product, decimal>("p => p.Price"), Is.EqualTo(4.99m));

        [Test]
        public void MaxDynamic_FindsMaximum() =>
            Assert.That(Products.MaxDynamic<Product, decimal>("p => p.Price"), Is.EqualTo(299.99m));
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

    #endregion

    #region Inline variables

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
        Assert.That(result, Does.Contain(19.98m));
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

    #endregion

    #region Lambda factory shape

    [TestFixture]
    [NonParallelizable]
    public class LambdaFactoryShape : CompilerFixtureBase
    {
    [Test]
    public void ParseLambdaExpression_ItTypeOverload_ReturnsLambdaExpression()
    {
        using var engine = new AlderEngine(o => o.UseCompiler());
        var lambda = engine.ParseLambdaExpression(
            typeof(Product),
            typeof(bool),
            "p => p.Price > @0",
            [new KeyValuePair<string, object?>("__p0", 50m)]);

        Assert.That(lambda, Is.Not.Null);
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.ReturnType, Is.EqualTo(typeof(bool)));

        var typed = (Expression<Func<Product, bool>>)lambda;
        var fn = typed.Compile();
        Assert.That(fn(new Product("Test", 75m, "X", true)), Is.True);
    }

    [Test]
    public void ParseLambdaExpression_ParameterTypesOverload_SupportsBodyWithoutLambdaSyntax()
    {
        using var engine = new AlderEngine(o => o.UseCompiler());
        var lambda = engine.ParseLambdaExpression(
            [typeof(Product), typeof(decimal)],
            ["p", "threshold"],
            typeof(bool),
            "p.Price > threshold");

        Assert.That(lambda.Parameters, Has.Count.EqualTo(2));
        Assert.That(lambda.ReturnType, Is.EqualTo(typeof(bool)));

        var typed = (Expression<Func<Product, decimal, bool>>)lambda;
        var fn = typed.Compile();
        Assert.That(fn(new Product("Test", 75m, "X", true), 50m), Is.True);
    }

    [Test]
    public void ParseLambdaExpression_ParameterExpressionOverload_BindsExplicitParameters()
    {
        using var engine = new AlderEngine(o => o.UseCompiler());
        var left = Expression.Parameter(typeof(int), "left");
        var right = Expression.Parameter(typeof(int), "right");

        var lambda = engine.ParseLambdaExpression(
            [left, right],
            typeof(int),
            "left + right");

        var typed = (Expression<Func<int, int, int>>)lambda;
        var fn = typed.Compile();
        Assert.That(fn(20, 22), Is.EqualTo(42));
    }
    }

    #endregion

    #region Real C# — Expressions no other dynamic LINQ library can handle

    [TestFixture]
    [NonParallelizable]
    public class LanguageSemantics : CompilerFixtureBase
    {
    [TestCase("""p => p.Price > 100m ? "Expensive" : "Affordable" """, "Doohickey", "Expensive")]
    [TestCase("""p => p.Price > 100m ? "Expensive" : "Affordable" """, "Widget", "Affordable")]
    public void Ternary_InSelector(string selector, string productName, string expected)
    {
        var product = Products.First(p => p.Name == productName);
        var result = new[] { product }.SelectDynamic<Product, string>(selector).Single();
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void NullCheck_PropertyAccess()
    {
        var withAddress = Customers.WhereDynamic("c => c.Address != null").ToList();
        Assert.That(withAddress.Select(c => c.Name), Does.Not.Contain("Dan"));
        Assert.That(withAddress, Has.Count.EqualTo(4));
    }

    [Test]
    public void NullCoalescing_InSelector()
    {
        var codes = Customers
            .WhereDynamic("c => c.Address != null")
            .SelectDynamic<Customer, string>("""c => c.Address!.PostalCode ?? "N/A" """)
            .ToList();
        Assert.That(codes, Does.Contain("N/A"));
        Assert.That(codes, Does.Contain("EC1A 1BB"));
    }

    [Test]
    public void StringConcat_InSelector()
    {
        var labels = Products
            .SelectDynamic<Product, string>("""p => p.Category + "/" + p.Name""")
            .ToList();
        Assert.That(labels, Does.Contain("Tools/Widget"));
        Assert.That(labels, Does.Contain("Premium/Whatchamacallit"));
    }

    [Test]
    public void NestedPropertyAccess()
    {
        var cities = Customers
            .WhereDynamic("c => c.Address != null")
            .SelectDynamic<Customer, string>("c => c.Address!.City")
            .ToList();
        Assert.That(cities, Is.EquivalentTo(new[] { "London", "New York", "Berlin", "Tokyo" }));
    }

    [Test]
    public void StringMethods_Contains()
    {
        var result = Products
            .WhereDynamic("""p => p.Name.Contains("dget")""")
            .ToList();
        Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Gadget", "Widget" }));
    }

    [Test]
    public void OrChain_InPredicate()
    {
        var result = Products
            .WhereDynamic("""p => p.Category == "Tools" || p.Category == "Premium" """)
            .ToList();
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void MathOperations_InSelector()
    {
        var rounded = Products
            .SelectDynamic<Product, double>("p => Math.Round((double)p.Price, 0)")
            .ToList();
        Assert.That(rounded, Does.Contain(10.0));
        Assert.That(rounded, Does.Contain(300.0));
    }

    [Test]
    public void Cast_InSelector()
    {
        var intPrices = Products
            .SelectDynamic<Product, int>("p => (int)p.Price")
            .ToList();
        Assert.That(intPrices, Does.Contain(9));
        Assert.That(intPrices, Does.Contain(299));
    }

    [Test]
    public void ComplexBooleanLogic()
    {
        var result = Products
            .WhereDynamic("p => !(p.InStock && p.Price < 10m) || p.Price > 200m")
            .ToList();
        Assert.That(result.Select(p => p.Name), Does.Contain("Gadget"));
        Assert.That(result.Select(p => p.Name), Does.Contain("Whatchamacallit"));
    }

    [Test]
    public void Arithmetic_InSelector()
    {
        var adjusted = Products
            .SelectDynamic<Product, decimal>("p => p.Price * 1.1m + 5m")
            .ToList();
        var widgetAdjusted = 9.99m * 1.1m + 5m;
        Assert.That(adjusted, Does.Contain(widgetAdjusted));
    }

    [Test]
    public void SubstringAndLength()
    {
        var result = Products
            .WhereDynamic("p => p.Name.Length > 6")
            .SelectDynamic<Product, string>("p => p.Name.Substring(0, 3)")
            .ToList();
        Assert.That(result, Does.Contain("Doo"));
        Assert.That(result, Does.Contain("Wha"));
    }

    [Test]
    public void LinqInsideLambda_CountOnNestedCollection()
    {
        var bigSpenders = Customers
            .WhereDynamic("c => c.Orders.Count > 1")
            .ToList();
        Assert.That(bigSpenders.Select(c => c.Name), Is.EquivalentTo(new[] { "Alice", "Bob", "Eve" }));
    }

    [Test]
    public void LinqInsideLambda_CountProperty()
    {
        var orderCounts = Customers
            .WhereDynamic("c => c.Orders.Count > 0")
            .SelectDynamic<Customer, int>("c => c.Orders.Count")
            .ToList();
        Assert.That(orderCounts, Does.Contain(2)); // Alice
        Assert.That(orderCounts, Does.Contain(3)); // Bob
    }

    [Test]
    public void NestedProperty_WithNullGuard()
    {
        var postalCodes = Customers
            .WhereDynamic("c => c.Address != null && c.Address.PostalCode != null")
            .SelectDynamic<Customer, string>("c => c.Address.PostalCode")
            .ToList();
        Assert.That(postalCodes, Does.Contain("EC1A 1BB"));
        Assert.That(postalCodes, Does.Contain("10001"));
    }

    [Test]
    public void ConditionalExpression_NestedTernary()
    {
        var tiers = Products
            .SelectDynamic<Product, string>("""
                p => p.Price > 200m ? "Premium"
                   : p.Price > 50m  ? "Mid-Range"
                   : "Budget"
                """)
            .ToList();
        Assert.That(tiers.Count(t => t == "Budget"), Is.EqualTo(3));
        Assert.That(tiers.Count(t => t == "Mid-Range"), Is.EqualTo(1));
        Assert.That(tiers.Count(t => t == "Premium"), Is.EqualTo(1));
    }

    [Test]
    public void StringConcat_MultiField()
    {
        var labels = Products
            .SelectDynamic<Product, string>("""p => p.Name + " (" + p.Category + ")" """)
            .ToList();
        Assert.That(labels, Does.Contain("Widget (Tools)"));
    }

    [Test]
    public void DateTimeProperty_InPredicate()
    {
        using var engine = new AlderEngine();
        engine.SetVariable<List<Customer>>("customers", Customers);
        engine.SetVariable("cutoff", new DateTime(2025, 5, 1));
        var result = engine.Evaluate<List<string>>("""
            return customers
                .Where(c => c.Orders.Any(o => o.OrderDate > cutoff))
                .Select(c => c.Name)
                .ToList();
            """);
        Assert.That(result, Is.EquivalentTo(new[] { "Cara" }));
    }

    [Test]
    public void ComplexChain_FilterProjectSort()
    {
        var result = Customers
            .WhereDynamic("c => c.Address != null && c.Orders.Count > 0")
            .SelectDynamic<Customer, int>("c => c.Orders.Count")
            .OrderByDynamic<int, int>("count => count")
            .ToList();

        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result[0], Is.LessThanOrEqualTo(result[^1]));
    }

    [Test]
    public void MultipleStringOperations()
    {
        var result = Products
            .WhereDynamic("""p => p.Name.StartsWith("W") || p.Name.EndsWith("et")""")
            .SelectDynamic<Product, string>("p => p.Name.ToUpper()")
            .ToList();
        Assert.That(result, Does.Contain("WIDGET"));
        Assert.That(result, Does.Contain("GADGET"));
        Assert.That(result, Does.Contain("WHATCHAMACALLIT"));
    }

    [Test]
    public void NullNotesFiltering_ViaInterpreter()
    {
        using var engine = new AlderEngine();
        engine.SetVariable<List<Customer>>("customers", Customers);
        var result = engine.Evaluate<List<string>>("""
            return customers
                .Where(c => c.Orders.Any(o => o.Notes != null))
                .Select(c => c.Name)
                .ToList();
            """);
        Assert.That(result, Has.Count.EqualTo(4));
    }

    [Test]
    public void InlineVariable_WithPropertyAccess()
    {
        var result = Customers
            .WhereDynamic("c => c.Orders.Count > @0", 1)
            .SelectDynamic<Customer, string>("c => c.Name")
            .ToList();
        Assert.That(result, Is.EquivalentTo(new[] { "Alice", "Bob", "Eve" }));
    }

    [Test]
    public void InlineVariable_WithAge()
    {
        var result = Customers
            .WhereDynamic("c => c.Age >= @0", 30)
            .SelectDynamic<Customer, string>("c => c.Name")
            .ToList();
        Assert.That(result, Is.EquivalentTo(new[] { "Alice", "Cara", "Eve" }));
    }

    [Test]
    public void GroupBy_ThenAggregate_DynamicKey()
    {
        var categoryTotals = Products
            .GroupByDynamic<Product, string>("p => p.Category")
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Price));
        Assert.That(categoryTotals["Tools"], Is.EqualTo(14.98m));
        Assert.That(categoryTotals["Electronics"], Is.EqualTo(199.98m));
    }

    [Test]
    public void NullCheck_IsNotNull()
    {
        var result = Customers
            .WhereDynamic("c => c.Address != null")
            .ToList();
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result.Select(c => c.Name), Does.Not.Contain("Dan"));
    }

    [Test]
    public void OrderBy_ThenBy_MultiLevel()
    {
        var result = Customers
            .WhereDynamic("c => c.Address != null")
            .OrderByDynamic<Customer, string>("c => c.Address!.Country")
            .ThenByDynamic<Customer, string>("c => c.Name")
            .SelectDynamic<Customer, string>("c => c.Name")
            .ToList();
        Assert.That(result[0], Is.EqualTo("Cara"));  // DE
        Assert.That(result[^1], Is.EqualTo("Bob"));   // US
    }

    [Test]
    public void Interpreter_NestedLinq_StringJoin()
    {
        using var engine = new AlderEngine();
        engine.SetVariable<List<Customer>>("customers", Customers);
        var result = engine.Evaluate<List<string>>("""
            return customers
                .Where(c => c.Orders.Count > 0)
                .Select(c => string.Join(", ", c.Orders.Select(o => o.Product)))
                .ToList();
            """);
        Assert.That(result, Does.Contain("Laptop, Mouse"));
    }

    [Test]
    public void Interpreter_ComplexProjection_StringInterpolation()
    {
        using var engine = new AlderEngine();
        engine.SetVariable<List<Customer>>("customers", Customers);
        var result = engine.Evaluate<List<string>>("""
            return customers
                .Where(c => c.Orders.Count > 0)
                .Select(c => $"{c.Name}: {c.Orders.Count} orders")
                .ToList();
            """);
        Assert.That(result!.Any(s => s.StartsWith("Alice:")), Is.True);
        Assert.That(result!.Any(s => s.Contains("orders")), Is.True);
    }

    [Test]
    public void MaxBy_ViaChainedDynamic()
    {
        var mostExpensive = Products
            .OrderByDescendingDynamic<Product, decimal>("p => p.Price")
            .FirstDynamic("p => p.InStock");
        Assert.That(mostExpensive.Name, Is.EqualTo("Whatchamacallit"));
    }

    [Test]
    public void Where_WithEnumerable_RangeCheck()
    {
        var midRange = Products
            .WhereDynamic("p => p.Price >= @0 && p.Price <= @1", 10m, 200m)
            .ToList();
        Assert.That(midRange.Select(p => p.Name), Is.EquivalentTo(new[] { "Gadget", "Doohickey" }));
    }

    [Test]
    public void Interpreter_NestedLinq_OrderBy()
    {
        using var engine = new AlderEngine();
        engine.SetVariable<List<Customer>>("customers", Customers);
        var result = engine.Evaluate<List<string>>("""
            return customers
                .Where(c => c.Orders.Count > 0)
                .Select(c => c.Orders.OrderBy(o => o.UnitPrice).First().Product)
                .ToList();
            """);
        Assert.That(result, Does.Contain("Mouse"));   // Alice
        Assert.That(result, Does.Contain("Cable"));   // Bob
    }

    [Test]
    public void StringConcat_WithVariables()
    {
        var result = Products
            .SelectDynamic<Product, string>("""p => p.Name + " [" + p.Category + "]" """)
            .ToList();
        Assert.That(result, Does.Contain("Widget [Tools]"));
    }

    [Test]
    public void Conditional_NullGuard_WithTernary()
    {
        var countryOrUnknown = Customers
            .SelectDynamic<Customer, string>("""c => c.Address != null ? c.Address.Country : "Unknown" """)
            .ToList();
        Assert.That(countryOrUnknown, Does.Contain("Unknown")); // Dan
        Assert.That(countryOrUnknown, Does.Contain("UK"));
        Assert.That(countryOrUnknown, Does.Contain("JP"));
    }
    }

    #endregion

    #region IAsyncEnumerable

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
        await foreach (var p in ToAsyncEnumerable(Products).WhereDynamic("p => p.Price > @0", 50m))
            result.Add(p);
        Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
    }

    [Test]
    public async Task Async_WhereDynamic_CustomEngine()
    {
        using var engine = new AlderEngine(o => o.UseCompiler());
        var result = new List<Product>();
        await foreach (var p in ToAsyncEnumerable(Products).WhereDynamic(engine, "p => p.Price > @0", 100m))
            result.Add(p);
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Async_SelectDynamic_ProjectsStream()
    {
        var names = new List<string>();
        await foreach (var name in ToAsyncEnumerable(Products).SelectDynamic<Product, string>("p => p.Name"))
            names.Add(name);
        Assert.That(names, Has.Count.EqualTo(5));
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
    public async Task Async_SumDynamic() =>
        Assert.That(await ToAsyncEnumerable(Products).SumDynamic("p => p.Price"), Is.EqualTo(514.95m));

    [Test]
    public async Task Async_WhereDynamic_PreParsedExpression()
    {
        var predicate = (Expression<Func<Product, bool>>)AlderEval.GetEngine()
            .ParsePredicateExpression(typeof(Product), "Price > 50m");

        var result = new List<Product>();
        await foreach (var p in ToAsyncEnumerable(Products).WhereDynamic(predicate))
            result.Add(p);
        Assert.That(result.Select(p => p.Name), Is.EquivalentTo(new[] { "Doohickey", "Whatchamacallit" }));
    }

    [Test]
    public async Task Async_AverageDynamic() =>
        Assert.That(await ToAsyncEnumerable(Products).AverageDynamic("p => (double)p.Price"), Is.EqualTo(102.99).Within(0.01));

    [Test]
    public async Task Async_MinDynamic() =>
        Assert.That(await ToAsyncEnumerable(Products).MinDynamic<Product, decimal>("p => p.Price"), Is.EqualTo(4.99m));

    [Test]
    public async Task Async_MaxDynamic() =>
        Assert.That(await ToAsyncEnumerable(Products).MaxDynamic<Product, decimal>("p => p.Price"), Is.EqualTo(299.99m));

    [Test]
    public async Task Async_Chain_WhereSelectCount()
    {
        var names = new List<string>();
        await foreach (var name in ToAsyncEnumerable(Products)
            .WhereDynamic("p => p.InStock")
            .SelectDynamic<Product, string>("p => p.Name"))
            names.Add(name);
        Assert.That(names, Has.Count.EqualTo(4));
        Assert.That(names, Does.Not.Contain("Doohickey"));
    }

    [Test]
    public async Task Async_ComplexPredicate_WithNestedProperties()
    {
        var result = new List<Customer>();
        await foreach (var c in ToAsyncEnumerable(Customers)
            .WhereDynamic("c => c.Address != null && c.Address.Country == @0 && c.Orders.Count > @1", "US", 1))
            result.Add(c);
        Assert.That(result.Single().Name, Is.EqualTo("Bob"));
    }

    [Test]
    public async Task Async_StringConcat_InSelector()
    {
        var labels = new List<string>();
        await foreach (var label in ToAsyncEnumerable(Products)
            .SelectDynamic<Product, string>("""p => p.Category + ": " + p.Name"""))
            labels.Add(label);
        Assert.That(labels, Does.Contain("Tools: Widget"));
    }
    }

    #endregion
}
