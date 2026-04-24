namespace Alder.Test.Compilation.DynamicLinq;

public record Product(string Name, decimal Price, string Category, bool InStock);
public record WarehouseStock(string Category, int Count);
public record ProductSummaryRecord(string name, decimal price);
public sealed class ProductSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
public sealed class ProductEnvelopeDto
{
    public ProductSummaryDto Product { get; set; } = null!;
}

public record Customer(string Name, int Age, Address? Address, List<Order> Orders);
public record Address(string City, string Country, string? PostalCode);
public record Order(string Product, int Quantity, decimal UnitPrice, DateTime OrderDate, string? Notes);

[TestFixture]
[NonParallelizable]
public partial class DynamicLinqTests
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
                {
                    return;
                }

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
}
