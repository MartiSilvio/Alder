using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Alder.Benchmarks;

[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CrossEngineWarmBenchmarks
{
    [ParamsSource(nameof(MatrixRows))]
    public BenchmarkMatrixRow Row { get; set; } = null!;

    public IEnumerable<BenchmarkMatrixRow> MatrixRows() =>
        MatrixCatalogBuilder.BuildSupportedRowsByCategory(BenchmarkLane.Warm, "HeadToHead");

    public string Suite => Row.Suite;
    public string CategoryTag => Row.Category;
    public string Lane => Row.LaneName;
    public string CaseId => Row.CaseId;
    public string EvaluatorId => Row.EvaluatorId;
    public int Scale => Row.ScaleFactor;

    private BenchmarkData _data = null!;
    private PreparedRow _prepared = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
        _prepared = WarmRunner.Prepare(Row, _data);

        var parity = ParityRunner.VerifyRow(Row, _data);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _prepared?.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory("HeadToHead/Warm")]
    public object Execute() => WarmRunner.Execute(_prepared)!;
}

[Config(typeof(SteadyStateConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CapabilityCrossEngineWarmBenchmarks
{
    [ParamsSource(nameof(MatrixRows))]
    public BenchmarkMatrixRow Row { get; set; } = null!;

    public IEnumerable<BenchmarkMatrixRow> MatrixRows() =>
        MatrixCatalogBuilder.BuildSupportedRowsByCategory(BenchmarkLane.Warm, "Capability");

    public string Suite => Row.Suite;
    public string CategoryTag => Row.Category;
    public string Lane => Row.LaneName;
    public string CaseId => Row.CaseId;
    public string EvaluatorId => Row.EvaluatorId;
    public int Scale => Row.ScaleFactor;

    private BenchmarkData _data = null!;
    private PreparedRow _prepared = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = BenchmarkData.CreateStandard();
        _prepared = WarmRunner.Prepare(Row, _data);

        var parity = ParityRunner.VerifyRow(Row, _data);
        if (!parity.IsSuccess)
            throw new InvalidOperationException(parity.Message);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _prepared?.Dispose();
    }

    [Benchmark]
    [BenchmarkCategory("Capability/Warm")]
    public object Execute() => WarmRunner.Execute(_prepared)!;
}
