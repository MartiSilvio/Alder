using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Alder.Runtime;

namespace Alder.Benchmarks;

[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ProjectionMaterializationBenchmarks
{
    private static readonly ConstructorInfo ProductSummaryDtoCtor =
        typeof(ProductSummaryDto).GetConstructor(Type.EmptyTypes)!;

    private static readonly PropertyInfo ProductSummaryDtoNameProperty =
        typeof(ProductSummaryDto).GetProperty(nameof(ProductSummaryDto.Name))!;

    private static readonly PropertyInfo ProductSummaryDtoPriceProperty =
        typeof(ProductSummaryDto).GetProperty(nameof(ProductSummaryDto.Price))!;

    private static readonly ConstructorInfo ProductSummaryRecordCtor =
        typeof(ProductSummaryRecord).GetConstructors().Single();

    private static readonly ConstructorInfo ProductEnvelopeDtoCtor =
        typeof(ProductEnvelopeDto).GetConstructor(Type.EmptyTypes)!;

    private static readonly PropertyInfo ProductEnvelopeDtoProductProperty =
        typeof(ProductEnvelopeDto).GetProperty(nameof(ProductEnvelopeDto.Product))!;

    private StructuralObjectValue _flatProjection = null!;
    private StructuralObjectValue _nestedProjection = null!;

    [GlobalSetup]
    public void Setup()
    {
        _flatProjection = StructuralObjectTypeFactory.Create(
            ["Name", "Price"],
            [typeof(string), typeof(decimal)],
            ["Widget", 9.99m]);

        var nestedProduct = StructuralObjectTypeFactory.Create(
            ["Name", "Price"],
            [typeof(string), typeof(decimal)],
            ["Widget", 9.99m]);

        _nestedProjection = StructuralObjectTypeFactory.Create(
            ["Product"],
            [typeof(object)],
            [nestedProduct]);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Flat")]
    public ProductSummaryDto Manual_Flat()
    {
        return new ProductSummaryDto
        {
            Name = (string)_flatProjection["Name"]!,
            Price = (decimal)_flatProjection["Price"]!
        };
    }

    [Benchmark]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Flat")]
    public ProductSummaryDto Reflection_Flat()
    {
        var dto = (ProductSummaryDto)ProductSummaryDtoCtor.Invoke([]);
        ProductSummaryDtoNameProperty.SetValue(dto, _flatProjection["Name"]);
        ProductSummaryDtoPriceProperty.SetValue(dto, _flatProjection["Price"]);
        return dto;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Flat")]
    public ProductSummaryDto Alder_Flat()
    {
        return AlderProjectionMaterializer.Materialize<ProductSummaryDto>(_flatProjection)!;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Record")]
    public ProductSummaryRecord Manual_Record()
    {
        return new ProductSummaryRecord(
            (string)_flatProjection["Name"]!,
            (decimal)_flatProjection["Price"]!);
    }

    [Benchmark]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Record")]
    public ProductSummaryRecord Reflection_Record()
    {
        return (ProductSummaryRecord)ProductSummaryRecordCtor.Invoke(
            [_flatProjection["Name"], _flatProjection["Price"]]);
    }

    [Benchmark]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Record")]
    public ProductSummaryRecord Alder_Record()
    {
        return AlderProjectionMaterializer.Materialize<ProductSummaryRecord>(_flatProjection)!;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Nested")]
    public ProductEnvelopeDto Manual_Nested()
    {
        var product = (StructuralObjectValue)_nestedProjection["Product"]!;
        return new ProductEnvelopeDto
        {
            Product = new ProductSummaryDto
            {
                Name = (string)product["Name"]!,
                Price = (decimal)product["Price"]!
            }
        };
    }

    [Benchmark]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Nested")]
    public ProductEnvelopeDto Reflection_Nested()
    {
        var product = (StructuralObjectValue)_nestedProjection["Product"]!;
        var inner = (ProductSummaryDto)ProductSummaryDtoCtor.Invoke([]);
        ProductSummaryDtoNameProperty.SetValue(inner, product["Name"]);
        ProductSummaryDtoPriceProperty.SetValue(inner, product["Price"]);

        var envelope = (ProductEnvelopeDto)ProductEnvelopeDtoCtor.Invoke([]);
        ProductEnvelopeDtoProductProperty.SetValue(envelope, inner);
        return envelope;
    }

    [Benchmark]
    [BenchmarkCategory("Operational/ProjectionMaterialization/Nested")]
    public ProductEnvelopeDto Alder_Nested()
    {
        return AlderProjectionMaterializer.Materialize<ProductEnvelopeDto>(_nestedProjection)!;
    }

    public sealed class ProductSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public sealed record ProductSummaryRecord(string Name, decimal Price);

    public sealed class ProductEnvelopeDto
    {
        public ProductSummaryDto Product { get; set; } = null!;
    }
}
