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
/// Default configuration for steady-state claim benchmarks.
/// Use this for measurements that are intended to represent warmed, repeatable execution rather than process startup.
/// </summary>
public sealed class SteadyStateConfig : ManualConfig
{
    public SteadyStateConfig()
    {
        AddJob(BenchmarkProfileContext.UsesShortRun
            ? Job.ShortRun.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            : Job.Default.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80));

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
/// Configuration for cold-start measurements.
/// Each measurement runs in a fresh process so process startup, JIT, metadata load, and first evaluation stay inside the sample.
/// </summary>
public sealed class ColdStartConfig : ManualConfig
{
    public ColdStartConfig()
    {
        AddJob(BenchmarkProfileContext.UsesShortRun
            ? Job.ShortRun.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            : Job.Default
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

public sealed class DynamicLinqConfig : ManualConfig
{
    public DynamicLinqConfig()
    {
        AddJob(BenchmarkProfileContext.IsPublishable && !BenchmarkProfileContext.UsesShortRun
            ? Job.Default.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            : Job.ShortRun.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80));

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

public sealed class DynamicLinqColdStartConfig : ManualConfig
{
    public DynamicLinqColdStartConfig()
    {
        var publishable = BenchmarkProfileContext.IsPublishable;
        AddJob(BenchmarkProfileContext.UsesShortRun
            ? Job.ShortRun.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            : Job.Default
                .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
                .WithStrategy(RunStrategy.ColdStart)
                .WithLaunchCount(publishable ? 10 : 3)
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
/// Configuration for batch workloads where the benchmark method intentionally owns the inner loop.
/// Use this only when one benchmark invocation is itself the unit of work being measured.
/// </summary>
public sealed class MonitoringConfig : ManualConfig
{
    public MonitoringConfig()
    {
        AddJob(BenchmarkProfileContext.UsesShortRun
            ? Job.ShortRun.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            : Job.Default
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
