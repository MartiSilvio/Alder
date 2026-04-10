namespace Alder.Benchmarks;

public sealed record Product(
    int Id,
    string Name,
    string Category,
    decimal Price,
    int Stock,
    double Rating,
    bool IsActive,
    DateTime CreatedDate);

public sealed record OrderLine(
    int OrderId,
    string ProductName,
    string Category,
    int Quantity,
    decimal UnitPrice,
    decimal Discount);

public sealed record Employee(
    int Id,
    string Name,
    string Department,
    decimal Salary,
    int YearsOfService,
    bool IsManager);

public sealed record Order(string Category, int Quantity, decimal UnitPrice);

public sealed class InvocationTarget
{
    public int Overload(int value) => value + 1;
    public long Overload(long value) => value + 1L;
    public double ConsumeDouble(double value) => value / 2.0;

    public int Sum(params int[] values)
    {
        var total = 0;
        for (var i = 0; i < values.Length; i++)
            total += values[i];
        return total;
    }

    public int WithOptional(int value, int extra = 10) => value + extra;
    public string Normalize(string input) => input.Trim().ToUpperInvariant();
    public int Transform(int value, Func<int, int> fn) => fn(value);
}

/// <summary>
/// Deterministic benchmark data with realistic domain models.
/// All data is generated from a fixed seed (42) for reproducibility.
/// </summary>
public sealed class BenchmarkData
{
    // Scalar variables (competitor-compatible)
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }
    public int Value { get; init; }
    public string Text { get; init; } = string.Empty;

    // Collections
    public List<int> Numbers { get; init; } = [];
    public List<Order> Orders { get; init; } = [];
    public List<Product> Products { get; init; } = [];
    public List<OrderLine> OrderLines { get; init; } = [];
    public List<Employee> Employees { get; init; } = [];

    private static readonly string[] Categories =
        ["Electronics", "Books", "Clothing", "Food", "Home", "Sports", "Toys", "Health", "Garden", "Auto"];

    private static readonly string[] Departments =
        ["Engineering", "Sales", "Marketing", "Finance", "HR", "Operations", "Support", "Legal", "Product", "Research"];

    private static readonly string[] FirstNames =
        ["Alice", "Bob", "Carol", "Dave", "Eve", "Frank", "Grace", "Henry", "Iris", "Jack",
         "Kate", "Leo", "Maya", "Nick", "Olivia", "Pete", "Quinn", "Rose", "Sam", "Tina"];

    public static BenchmarkData CreateStandard() => Create();

    public static BenchmarkData Create(
        int numberCount = 1_000,
        int productCount = 10_000,
        int orderLineCount = 0,
        int employeeCount = 0)
    {
        const int seed = 42;
        var rng = new Random(seed);

        var products = GenerateProducts(rng, productCount);
        if (orderLineCount == 0) orderLineCount = productCount / 2;
        var orderLines = GenerateOrderLines(rng, products, orderLineCount);
        if (employeeCount == 0) employeeCount = Math.Max(100, productCount / 10);
        var employees = GenerateEmployees(rng, employeeCount);

        return new BenchmarkData
        {
            X = 10,
            Y = 3,
            Z = 7,
            Value = 42,
            Text = "alpha",
            Numbers = Enumerable.Range(1, numberCount).ToList(),
            Orders =
            [
                new("A", 4, 12.5m),
                new("B", 1, 99.9m),
                new("C", 6, 8.25m),
                new("D", 2, 19.0m),
                new("E", 8, 3.5m)
            ],
            Products = products,
            OrderLines = orderLines,
            Employees = employees
        };
    }

    public static List<int> CreateNumbers(int count) => Enumerable.Range(1, count).ToList();

    public static List<Product> CreateProducts(int count)
    {
        var rng = new Random(42);
        return GenerateProducts(rng, count);
    }

    private static List<Product> GenerateProducts(Random rng, int count)
    {
        var products = new List<Product>(count);
        var baseDate = new DateTime(2020, 1, 1);
        for (int i = 0; i < count; i++)
        {
            products.Add(new Product(
                Id: i + 1,
                Name: $"Product_{i + 1:D5}",
                Category: Categories[i % Categories.Length],
                Price: Math.Round((decimal)(rng.NextDouble() * 999.0 + 1.0), 2),
                Stock: rng.Next(0, 1000),
                Rating: Math.Round(rng.NextDouble() * 4.0 + 1.0, 1),
                IsActive: rng.NextDouble() > 0.1,
                CreatedDate: baseDate.AddDays(rng.Next(0, 2000))));
        }
        return products;
    }

    private static List<OrderLine> GenerateOrderLines(Random rng, List<Product> products, int count)
    {
        var lines = new List<OrderLine>(count);
        for (int i = 0; i < count; i++)
        {
            var product = products[rng.Next(products.Count)];
            lines.Add(new OrderLine(
                OrderId: i / 3 + 1,
                ProductName: product.Name,
                Category: product.Category,
                Quantity: rng.Next(1, 20),
                UnitPrice: product.Price,
                Discount: rng.NextDouble() < 0.3 ? Math.Round((decimal)(rng.NextDouble() * 0.25), 2) : 0m));
        }
        return lines;
    }

    private static List<Employee> GenerateEmployees(Random rng, int count)
    {
        var employees = new List<Employee>(count);
        for (int i = 0; i < count; i++)
        {
            employees.Add(new Employee(
                Id: i + 1,
                Name: $"{FirstNames[rng.Next(FirstNames.Length)]}_{i + 1:D4}",
                Department: Departments[i % Departments.Length],
                Salary: Math.Round(40_000m + (decimal)(rng.NextDouble() * 160_000.0), 2),
                YearsOfService: rng.Next(0, 30),
                IsManager: rng.NextDouble() > 0.85));
        }
        return employees;
    }
}
