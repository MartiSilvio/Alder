namespace Alder.Benchmarks;

public sealed class BenchmarkGlobalData
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }
    public int Value { get; init; }
    public string Text { get; init; } = string.Empty;
    public List<int> Numbers { get; init; } = [];
    public List<Order> Orders { get; init; } = [];

    public static BenchmarkGlobalData CreateDefault()
    {
        return new BenchmarkGlobalData
        {
            X = 10,
            Y = 3,
            Z = 7,
            Value = 42,
            Text = "alpha",
            Numbers = Enumerable.Range(1, 1_000).ToList(),
            Orders =
            [
                new Order("A", 4, 12.5m),
                new Order("B", 1, 99.9m),
                new Order("C", 6, 8.25m),
                new Order("D", 2, 19.0m),
                new Order("E", 8, 3.5m)
            ]
        };
    }
}

public sealed record Order(string Category, int Quantity, decimal UnitPrice);
