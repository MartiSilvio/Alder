using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
namespace Alder.Benchmarks;

/// <summary>
/// Default config for claim-grade steady-state measurements.
/// </summary>
public sealed class SteadyStateConfig : ManualConfig
{
    public SteadyStateConfig()
    {
        var quick = Environment.GetEnvironmentVariable("BDN_QUICK");
        AddJob(quick != null
            ? Job.ShortRun.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            : Job.Default
                .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
                .WithLaunchCount(2));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(CsvMeasurementsExporter.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(
            StatisticColumn.Median,
            StatisticColumn.StdDev,
            StatisticColumn.Min,
            StatisticColumn.Max);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}

/// <summary>
/// Cold-start config uses a fresh process per measurement to capture process,
/// JIT, metadata, and first-evaluation costs without in-process warmup.
/// </summary>
public sealed class ColdStartConfig : ManualConfig
{
    public ColdStartConfig()
    {
        AddJob(Job.Default
            .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            .WithStrategy(RunStrategy.ColdStart)
            .WithLaunchCount(10)
            .WithWarmupCount(0)
            .WithIterationCount(1));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(CsvMeasurementsExporter.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(
            StatisticColumn.Median,
            StatisticColumn.StdDev,
            StatisticColumn.Min,
            StatisticColumn.Max);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}

/// <summary>
/// Monitoring config is reserved for batch workloads where the benchmark method
/// intentionally owns the inner loop and represents the production unit of work.
/// </summary>
public sealed class MonitoringConfig : ManualConfig
{
    public MonitoringConfig()
    {
        AddJob(Job.Default
            .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            .WithStrategy(RunStrategy.Monitoring)
            .WithLaunchCount(3)
            .WithWarmupCount(3)
            .WithIterationCount(15));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(CsvMeasurementsExporter.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(
            StatisticColumn.Median,
            StatisticColumn.StdDev,
            StatisticColumn.Min,
            StatisticColumn.Max);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}
