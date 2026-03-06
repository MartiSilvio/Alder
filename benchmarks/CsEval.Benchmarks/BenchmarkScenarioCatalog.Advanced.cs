namespace CsEval.Benchmarks;

public static partial class BenchmarkScenarioCatalog
{
    public static IReadOnlyList<AdvancedScenario> GetAdvancedLanguageScenarios() =>
    [
        new(
            "Advanced/NestedMath",
            "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)",
            "Math.Abs((X - Y) * (Z + 2)) + Math.Max(X, Z)",
            "Math.Abs((x - y) * (z + 2)) + Math.Max(x, z)",
            "Abs((x - y) * (z + 2)) + Max(x, z)"),
        new(
            "Advanced/NestedConditional",
            "x > y ? (y > z ? y : z) : x",
            "X > Y ? (Y > Z ? Y : Z) : X",
            "x > y ? (y > z ? y : z) : x",
            "if(x > y, if(y > z, y, z), x)"),
        new(
            "Advanced/StringPredicate",
            "text.StartsWith(\"a\") && text.Length > 3",
            "Text.StartsWith(\"a\") && Text.Length > 3",
            "text.StartsWith(\"a\") && text.Length > 3",
            "text.StartsWith(\"a\") and text.Length > 3"),
        new(
            "Advanced/CollectionProperties",
            "numbers.Count > 500 && orders.Count == 5",
            "Numbers.Count > 500 && Orders.Count == 5",
            "numbers.Count > 500 && orders.Count == 5",
            "numbers.Count > 500 and orders.Count = 5"),
        new(
            "Advanced/ObjectGraphAccess",
            "orders[0].Quantity + orders[1].Quantity + orders.Count",
            "Orders[0].Quantity + Orders[1].Quantity + Orders.Count",
            "orders[0].Quantity + orders[1].Quantity + orders.Count",
            "orders[0].Quantity + orders[1].Quantity + orders.Count")
    ];
}
