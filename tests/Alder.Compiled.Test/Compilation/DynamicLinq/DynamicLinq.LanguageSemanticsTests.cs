namespace Alder.Test.Compilation.DynamicLinq;

public partial class DynamicLinqTests
{
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
            var nongeneric = new[] { product }.SelectDynamic(selector).Cast<string>().Single();
            Assert.That(result, Is.EqualTo(expected));
            Assert.That(nongeneric, Is.EqualTo(result));
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
            var nongeneric = Customers
                .WhereDynamic("c => c.Address != null")
                .SelectDynamic("""c => c.Address!.PostalCode ?? "N/A" """)
                .Cast<string>()
                .ToList();
            Assert.That(codes, Does.Contain("N/A"));
            Assert.That(codes, Does.Contain("EC1A 1BB"));
            Assert.That(nongeneric, Is.EqualTo(codes));
        }

        [Test]
        public void StringConcat_InSelector()
        {
            var labels = Products
                .SelectDynamic<Product, string>("""p => p.Category + "/" + p.Name""")
                .ToList();
            var nongeneric = Products.SelectDynamic("""p => p.Category + "/" + p.Name""").Cast<string>().ToList();
            Assert.That(labels, Does.Contain("Tools/Widget"));
            Assert.That(labels, Does.Contain("Premium/Whatchamacallit"));
            Assert.That(nongeneric, Is.EqualTo(labels));
        }

        [Test]
        public void NestedPropertyAccess()
        {
            var cities = Customers
                .WhereDynamic("c => c.Address != null")
                .SelectDynamic<Customer, string>("c => c.Address!.City")
                .ToList();
            var nongeneric = Customers
                .WhereDynamic("c => c.Address != null")
                .SelectDynamic("c => c.Address!.City")
                .Cast<string>()
                .ToList();
            Assert.That(cities, Is.EquivalentTo(new[] { "London", "New York", "Berlin", "Tokyo" }));
            Assert.That(nongeneric, Is.EqualTo(cities));
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
            var nongeneric = Products.SelectDynamic("p => Math.Round((double)p.Price, 0)").Cast<double>().ToList();
            Assert.That(rounded, Does.Contain(10.0));
            Assert.That(rounded, Does.Contain(300.0));
            Assert.That(nongeneric, Is.EqualTo(rounded));
        }

        [Test]
        public void Cast_InSelector()
        {
            var intPrices = Products
                .SelectDynamic<Product, int>("p => (int)p.Price")
                .ToList();
            var nongeneric = Products.SelectDynamic("p => (int)p.Price").Cast<int>().ToList();
            Assert.That(intPrices, Does.Contain(9));
            Assert.That(intPrices, Does.Contain(299));
            Assert.That(nongeneric, Is.EqualTo(intPrices));
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
            var nongeneric = Products.SelectDynamic("p => p.Price * 1.1m + 5m").Cast<decimal>().ToList();
            var widgetAdjusted = 9.99m * 1.1m + 5m;
            Assert.That(adjusted, Does.Contain(widgetAdjusted));
            Assert.That(nongeneric, Is.EqualTo(adjusted));
        }

        [Test]
        public void SubstringAndLength()
        {
            var result = Products
                .WhereDynamic("p => p.Name.Length > 6")
                .SelectDynamic<Product, string>("p => p.Name.Substring(0, 3)")
                .ToList();
            var nongeneric = Products
                .WhereDynamic("p => p.Name.Length > 6")
                .SelectDynamic("p => p.Name.Substring(0, 3)")
                .Cast<string>()
                .ToList();
            Assert.That(result, Does.Contain("Doo"));
            Assert.That(result, Does.Contain("Wha"));
            Assert.That(nongeneric, Is.EqualTo(result));
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
            var nongeneric = Customers
                .WhereDynamic("c => c.Orders.Count > 0")
                .SelectDynamic("c => c.Orders.Count")
                .Cast<int>()
                .ToList();
            Assert.That(orderCounts, Does.Contain(2));
            Assert.That(orderCounts, Does.Contain(3));
            Assert.That(nongeneric, Is.EqualTo(orderCounts));
        }

        [Test]
        public void NestedProperty_WithNullGuard()
        {
            var postalCodes = Customers
                .WhereDynamic("c => c.Address != null && c.Address.PostalCode != null")
                .SelectDynamic<Customer, string>("c => c.Address.PostalCode")
                .ToList();
            var nongeneric = Customers
                .WhereDynamic("c => c.Address != null && c.Address.PostalCode != null")
                .SelectDynamic("c => c.Address.PostalCode")
                .Cast<string>()
                .ToList();
            Assert.That(postalCodes, Does.Contain("EC1A 1BB"));
            Assert.That(postalCodes, Does.Contain("10001"));
            Assert.That(nongeneric, Is.EqualTo(postalCodes));
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
            var nongeneric = Products
                .SelectDynamic("""
                    p => p.Price > 200m ? "Premium"
                       : p.Price > 50m  ? "Mid-Range"
                       : "Budget"
                    """)
                .Cast<string>()
                .ToList();
            Assert.That(tiers.Count(t => t == "Budget"), Is.EqualTo(3));
            Assert.That(tiers.Count(t => t == "Mid-Range"), Is.EqualTo(1));
            Assert.That(tiers.Count(t => t == "Premium"), Is.EqualTo(1));
            Assert.That(nongeneric, Is.EqualTo(tiers));
        }

        [Test]
        public void StringConcat_MultiField()
        {
            var labels = Products
                .SelectDynamic<Product, string>("""p => p.Name + " (" + p.Category + ")" """)
                .ToList();
            var nongeneric = Products.SelectDynamic("""p => p.Name + " (" + p.Category + ")" """).Cast<string>().ToList();
            Assert.That(labels, Does.Contain("Widget (Tools)"));
            Assert.That(nongeneric, Is.EqualTo(labels));
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
            var nongeneric = Customers
                .WhereDynamic("c => c.Address != null && c.Orders.Count > 0")
                .SelectDynamic("c => c.Orders.Count")
                .Cast<int>()
                .OrderByDynamic("count => count")
                .ToList();

            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result[0], Is.LessThanOrEqualTo(result[^1]));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void MultipleStringOperations()
        {
            var result = Products
                .WhereDynamic("""p => p.Name.StartsWith("W") || p.Name.EndsWith("et")""")
                .SelectDynamic<Product, string>("p => p.Name.ToUpper()")
                .ToList();
            var nongeneric = Products
                .WhereDynamic("""p => p.Name.StartsWith("W") || p.Name.EndsWith("et")""")
                .SelectDynamic("p => p.Name.ToUpper()")
                .Cast<string>()
                .ToList();
            Assert.That(result, Does.Contain("WIDGET"));
            Assert.That(result, Does.Contain("GADGET"));
            Assert.That(result, Does.Contain("WHATCHAMACALLIT"));
            Assert.That(nongeneric, Is.EqualTo(result));
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
            var nongeneric = Customers
                .WhereDynamic("c => c.Orders.Count > @0", 1)
                .SelectDynamic("c => c.Name")
                .Cast<string>()
                .ToList();
            Assert.That(result, Is.EquivalentTo(new[] { "Alice", "Bob", "Eve" }));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void InlineVariable_WithAge()
        {
            var result = Customers
                .WhereDynamic("c => c.Age >= @0", 30)
                .SelectDynamic<Customer, string>("c => c.Name")
                .ToList();
            var nongeneric = Customers
                .WhereDynamic("c => c.Age >= @0", 30)
                .SelectDynamic("c => c.Name")
                .Cast<string>()
                .ToList();
            Assert.That(result, Is.EquivalentTo(new[] { "Alice", "Cara", "Eve" }));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void GroupBy_ThenAggregate_DynamicKey()
        {
            var categoryTotals = Products
                .GroupByDynamic<Product, string>("p => p.Category")
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Price));
            var nongeneric = Products
                .GroupByDynamic("p => p.Category")
                .Cast<IGrouping<string, Product>>()
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Price));
            Assert.That(categoryTotals["Tools"], Is.EqualTo(14.98m));
            Assert.That(categoryTotals["Electronics"], Is.EqualTo(199.98m));
            Assert.That(nongeneric, Is.EqualTo(categoryTotals));
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
            var nongeneric = Customers
                .WhereDynamic("c => c.Address != null")
                .OrderByDynamic("c => c.Address!.Country")
                .ThenByDynamic("c => c.Name")
                .SelectDynamic("c => c.Name")
                .Cast<string>()
                .ToList();
            Assert.That(result[0], Is.EqualTo("Cara"));
            Assert.That(result[^1], Is.EqualTo("Bob"));
            Assert.That(nongeneric, Is.EqualTo(result));
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
            var nongeneric = Products
                .OrderByDescendingDynamic("p => p.Price")
                .FirstDynamic("p => p.InStock");
            Assert.That(mostExpensive.Name, Is.EqualTo("Whatchamacallit"));
            Assert.That(nongeneric.Name, Is.EqualTo(mostExpensive.Name));
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
            Assert.That(result, Does.Contain("Mouse"));
            Assert.That(result, Does.Contain("Cable"));
        }

        [Test]
        public void StringConcat_WithVariables()
        {
            var result = Products
                .SelectDynamic<Product, string>("""p => p.Name + " [" + p.Category + "]" """)
                .ToList();
            var nongeneric = Products.SelectDynamic("""p => p.Name + " [" + p.Category + "]" """).Cast<string>().ToList();
            Assert.That(result, Does.Contain("Widget [Tools]"));
            Assert.That(nongeneric, Is.EqualTo(result));
        }

        [Test]
        public void Conditional_NullGuard_WithTernary()
        {
            var countryOrUnknown = Customers
                .SelectDynamic<Customer, string>("""c => c.Address != null ? c.Address.Country : "Unknown" """)
                .ToList();
            var nongeneric = Customers
                .SelectDynamic("""c => c.Address != null ? c.Address.Country : "Unknown" """)
                .Cast<string>()
                .ToList();
            Assert.That(countryOrUnknown, Does.Contain("Unknown"));
            Assert.That(countryOrUnknown, Does.Contain("UK"));
            Assert.That(countryOrUnknown, Does.Contain("JP"));
            Assert.That(nongeneric, Is.EqualTo(countryOrUnknown));
        }
    }
}
