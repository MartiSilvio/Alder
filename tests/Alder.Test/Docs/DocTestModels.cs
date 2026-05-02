using System.Data;

namespace Alder.Test.Docs;

public sealed record DocProduct(
    string Name,
    decimal Price,
    string Category,
    bool InStock);

public sealed record DocWarehouseStock(string Category, int Count);

public sealed record DocOrder(string Product, int Quantity, decimal UnitPrice);

public sealed record DocCustomer(string Name, List<DocOrder> Orders);

public sealed record DocOrderRow(decimal Total, DocCustomerInfo Customer, bool IsActive = true);

public sealed record DocCustomerInfo(string Name);

public sealed class DocProductSummaryDto
{
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
}

public sealed class DocPricingService
{
    public Task<decimal> GetMinimumAsync(string category) =>
        Task.FromResult(category == "Specialty" ? 250m : 50m);

    public Task<int> ComputeAsync(int left, int right) =>
        Task.FromResult(left + right);
}

public sealed class DocStatefulModule
{
    public int Calls { get; private set; }

    public int Next()
    {
        Calls++;
        return Calls;
    }
}

public sealed class DocMathTools
{
    public double CircleArea(double radius) => Math.PI * radius * radius;
    public static double Tau => Math.PI * 2;
}

public sealed class DocAccountModule
{
    [Alder.Attributes.AlderFunction]
    public bool IsActive(int accountId) => accountId > 0;

    public string InternalToken => "hidden";
}

public sealed class DocGlobalHelpers
{
    [Alder.Attributes.AlderFunction("greet")]
    public string Greet(string name) => $"Hello, {name}!";

    [Alder.Attributes.AlderFunction]
    public int Add(int left, int right = 0) => left + right;
}

internal static class DocSamples
{
    internal static readonly List<DocProduct> Products =
    [
        new("Widget", 9.99m, "Tools", true),
        new("Gadget", 49.99m, "Electronics", true),
        new("Doohickey", 149.99m, "Electronics", false),
        new("Thingamajig", 4.99m, "Tools", true),
        new("Whatchamacallit", 299.99m, "Specialty", true)
    ];

    internal static readonly List<DocCustomer> Customers =
    [
        new("Ada", [new("Widget", 2, 9.99m), new("Gadget", 1, 49.99m)]),
        new("Grace", [new("Doohickey", 1, 149.99m)]),
        new("Linus", [])
    ];

    internal static readonly List<DocWarehouseStock> Stock =
    [
        new("Tools", 12),
        new("Electronics", 5),
        new("Specialty", 1)
    ];

    internal static DataTable CreateCityTable()
    {
        var table = new DataTable();
        table.Columns.Add("City", typeof(string));
        table.Columns.Add("Size", typeof(int));
        table.Rows.Add("Seattle", 3);
        table.Rows.Add("Paris", 2);
        table.Rows.Add("Seattle", 5);
        return table;
    }

    internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
