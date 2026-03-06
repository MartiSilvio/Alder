using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace CsEval.Benchmarks;

public sealed class BenchmarkSuiteConfig : ManualConfig
{
    public BenchmarkSuiteConfig()
    {
        AddJob(Job.Default
            .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)
            .WithWarmupCount(4)
            .WithIterationCount(12));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(MarkdownExporter.GitHub);
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
    }
}
