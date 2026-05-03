namespace Alder.Benchmarks;

public enum BenchmarkRunProfile
{
    Custom,
    Validate,
    PerfSmoke,
    Publish,
    Exhaustive
}

public enum BenchmarkMeasurementMode
{
    ShortRun,
    Default
}

public enum BenchmarkMatrixScope
{
    Default,
    Exhaustive
}

public sealed record BenchmarkProfileDefinition(
    BenchmarkRunProfile Profile,
    BenchmarkMeasurementMode MeasurementMode,
    BenchmarkMatrixScope DynamicLinqQueryScope,
    IReadOnlyList<int> DynamicLinqScaleFactors,
    IReadOnlyList<int> CollectionPipelineScaleFactors,
    IReadOnlyList<int> BusinessRuleCounts,
    IReadOnlyList<int> BusinessRuleEntityCounts,
    IReadOnlyList<int> CompilationReuseCounts,
    IReadOnlyList<int> ThroughputThreadCounts)
{
    public bool IsPublishable => Profile is BenchmarkRunProfile.Publish or BenchmarkRunProfile.Exhaustive;

    public static BenchmarkProfileDefinition For(BenchmarkRunProfile profile) =>
        profile switch
        {
            BenchmarkRunProfile.PerfSmoke => new(
                profile,
                BenchmarkMeasurementMode.ShortRun,
                BenchmarkMatrixScope.Default,
                [10_000],
                [1_000],
                [5],
                [100],
                [1, 100],
                [1, 4]),

            BenchmarkRunProfile.Publish => new(
                profile,
                BenchmarkMeasurementMode.Default,
                BenchmarkMatrixScope.Default,
                [10_000],
                [1_000, 10_000],
                [5, 25],
                [100, 1_000],
                [1, 10, 100, 1_000],
                [1, 4, 8]),

            BenchmarkRunProfile.Exhaustive => new(
                profile,
                BenchmarkMeasurementMode.Default,
                BenchmarkMatrixScope.Exhaustive,
                [100, 1_000, 10_000, 100_000],
                [100, 1_000, 10_000, 100_000],
                [5, 10, 25],
                [100, 1_000],
                [1, 5, 10, 50, 100, 500, 1_000],
                [1, 2, 4, 8]),

            _ => new(
                profile,
                BenchmarkMeasurementMode.Default,
                BenchmarkMatrixScope.Default,
                [10_000],
                [1_000],
                [5],
                [100],
                [1, 100],
                [1, 4])
        };
}

public enum BenchmarkCommandKind
{
    ShowHelp,
    Validate,
    RunBenchmarks
}

public sealed record BenchmarkCommand(
    BenchmarkCommandKind Kind,
    BenchmarkRunProfile Profile,
    string[] BenchmarkDotNetArgs)
{
    private static readonly HashSet<string> BenchmarkDotNetArgumentsWithValue = new(StringComparer.OrdinalIgnoreCase)
    {
        "--filter",
        "--list"
    };

    public static BenchmarkCommand Parse(string[] args)
    {
        if (args.Length == 0)
            return new BenchmarkCommand(BenchmarkCommandKind.ShowHelp, BenchmarkRunProfile.Custom, []);

        var profile = BenchmarkRunProfile.Custom;
        var benchmarkDotNetArgs = new List<string>(args.Length);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!IsFlag(arg, "--profile"))
            {
                benchmarkDotNetArgs.Add(arg);
                continue;
            }

            if (i + 1 >= args.Length)
                throw new ArgumentException("Missing value for --profile.", nameof(args));

            profile = ParseProfile(args[++i]);
        }

        if (profile == BenchmarkRunProfile.Custom)
            throw new ArgumentException("Benchmark runs require an explicit --profile value.", nameof(args));

        ValidateBenchmarkDotNetArgs(profile, benchmarkDotNetArgs);

        var kind = profile == BenchmarkRunProfile.Validate
            ? BenchmarkCommandKind.Validate
            : BenchmarkCommandKind.RunBenchmarks;

        return new BenchmarkCommand(kind, profile, benchmarkDotNetArgs.ToArray());
    }

    internal static BenchmarkRunProfile ParseProfile(string value) =>
        value.ToLowerInvariant() switch
        {
            "validate" => BenchmarkRunProfile.Validate,
            "perf-smoke" or "perfsmoke" => BenchmarkRunProfile.PerfSmoke,
            "publish" => BenchmarkRunProfile.Publish,
            "exhaustive" => BenchmarkRunProfile.Exhaustive,
            "custom" => BenchmarkRunProfile.Custom,
            _ => throw new ArgumentException($"Unknown benchmark profile '{value}'.")
        };

    private static void ValidateBenchmarkDotNetArgs(
        BenchmarkRunProfile profile,
        IReadOnlyList<string> benchmarkDotNetArgs)
    {
        if (profile == BenchmarkRunProfile.Validate && benchmarkDotNetArgs.Count > 0)
            throw new ArgumentException("The validate profile does not accept BenchmarkDotNet arguments.");

        for (var i = 0; i < benchmarkDotNetArgs.Count; i++)
        {
            var argument = benchmarkDotNetArgs[i];
            if (!BenchmarkDotNetArgumentsWithValue.Contains(argument))
                throw new ArgumentException(
                    $"BenchmarkDotNet argument '{argument}' is not allowed. Profiles own benchmark run policy; only --filter and --list are accepted.");

            if (i + 1 >= benchmarkDotNetArgs.Count)
                throw new ArgumentException($"Missing value for BenchmarkDotNet argument '{argument}'.");

            i++;
        }
    }

    private static bool IsFlag(string value, string flag) =>
        string.Equals(value, flag, StringComparison.OrdinalIgnoreCase);
}

public static class BenchmarkProfileContext
{
    public const string EnvironmentVariable = "ALDER_BENCHMARK_PROFILE";

    public static BenchmarkRunProfile Current
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariable);
            if (string.IsNullOrWhiteSpace(value))
                return BenchmarkRunProfile.Custom;

            try
            {
                return BenchmarkCommand.ParseProfile(value);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Invalid {EnvironmentVariable} value '{value}'.",
                    ex);
            }
        }
    }

    public static bool IsPublishable =>
        CurrentDefinition.IsPublishable;

    public static BenchmarkProfileDefinition CurrentDefinition =>
        BenchmarkProfileDefinition.For(Current);

    public static bool UsesShortRun =>
        CurrentDefinition.MeasurementMode == BenchmarkMeasurementMode.ShortRun;
}
