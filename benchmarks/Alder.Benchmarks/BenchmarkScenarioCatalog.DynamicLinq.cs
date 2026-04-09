using System.Linq.Dynamic.Core;
using Alder.Compiled;

namespace Alder.Benchmarks;

public static partial class BenchmarkScenarioCatalog
{
    public static IReadOnlyList<DynamicLinqScenario> GetDynamicLinqScenarios() =>
    [
        new(
            "DynLINQ/WhereCount",
            g => g.Numbers.Where(x => x > 500).Count(),
            g => g.Numbers.WhereDynamic<int>("x => x > 500").Count(),
            g => g.Numbers.AsQueryable().Where("it > 500").Count()),
        new(
            "DynLINQ/SelectSum",
            g => g.Numbers.Select(x => x * 2).Sum(),
            g => g.Numbers.SelectDynamic<int, int>("x => x * 2").Sum(),
            g => (int)g.Numbers.AsQueryable().Select("it * 2").Sum()),
        new(
            "DynLINQ/WhereSelectSum",
            g => g.Numbers.Where(x => x > 100).Select(x => x * x).Sum(),
            g => g.Numbers.WhereDynamic<int>("x => x > 100").SelectDynamic<int, int>("x => x * x").Sum(),
            g => (int)g.Numbers.AsQueryable().Where("it > 100").Select("it * it").Sum()),
        new(
            "DynLINQ/AnyPredicate",
            g => g.Numbers.Any(x => x > 999),
            g => g.Numbers.AnyDynamic<int>("x => x > 999"),
            g => g.Numbers.AsQueryable().Any("it > 999")),
        new(
            "DynLINQ/OrderByFirst",
            g => g.Numbers.OrderByDescending(x => x).First(),
            g => g.Numbers.OrderByDescendingDynamic<int, int>("x => x").First(),
            g => g.Numbers.AsQueryable().OrderBy("it descending").First()),
        new(
            "DynLINQ/WhereCount/Order",
            g => g.Orders.Where(o => o.Quantity > 3).Count(),
            g => g.Orders.WhereDynamic<Order>("o => o.Quantity > 3").Count(),
            g => g.Orders.AsQueryable().Where("Quantity > 3").Count()),
        new(
            "DynLINQ/OrderByFirst/Order",
            g => g.Orders.OrderByDescending(o => o.UnitPrice).First(),
            g => g.Orders.OrderByDescendingDynamic<Order, decimal>("o => o.UnitPrice").First(),
            g => g.Orders.AsQueryable().OrderBy("UnitPrice descending").First()),
    ];
}
