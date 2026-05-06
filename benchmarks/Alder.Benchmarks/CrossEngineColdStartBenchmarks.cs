using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

[Config(typeof(ColdStartConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CrossEngineColdStartBenchmarks
{
    [ParamsSource(nameof(MatrixRows))]
    public BenchmarkMatrixRow Row { get; set; } = null!;

    public IEnumerable<BenchmarkMatrixRow> MatrixRows() =>
        MatrixCatalogBuilder.BuildSupportedRowsByCategory(BenchmarkLane.Cold, "HeadToHead");

    public string Suite => Row.Suite;
    public string CategoryTag => Row.Category;
    public string Lane => Row.LaneName;
    public string CaseId => Row.CaseId;
    public string EvaluatorId => Row.EvaluatorId;
    public int Scale => Row.ScaleFactor;

    private readonly BenchmarkData _data = BenchmarkData.CreateStandard();

    [Benchmark]
    [BenchmarkCategory("HeadToHead/Cold")]
    public object Execute() => ColdRunner.Execute(Row, _data)!;
}

[Config(typeof(ColdStartConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CapabilityCrossEngineColdStartBenchmarks
{
    [ParamsSource(nameof(MatrixRows))]
    public BenchmarkMatrixRow Row { get; set; } = null!;

    public IEnumerable<BenchmarkMatrixRow> MatrixRows() =>
        MatrixCatalogBuilder.BuildSupportedRowsByCategory(BenchmarkLane.Cold, "Capability");

    public string Suite => Row.Suite;
    public string CategoryTag => Row.Category;
    public string Lane => Row.LaneName;
    public string CaseId => Row.CaseId;
    public string EvaluatorId => Row.EvaluatorId;
    public int Scale => Row.ScaleFactor;

    private readonly BenchmarkData _data = BenchmarkData.CreateStandard();

    [Benchmark]
    [BenchmarkCategory("Capability/Cold")]
    public object Execute() => ColdRunner.Execute(Row, _data)!;
}
