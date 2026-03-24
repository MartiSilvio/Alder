using System.Collections.Immutable;

namespace Alder.Runtime;

internal enum ParameterSourceKind : byte
{
    Argument,
    Default,
    ParamsRange
}

internal readonly struct ParameterSource
{
    public ParameterSourceKind Kind { get; }
    public int ArgumentIndex { get; }
    public int ParamsStartIndex { get; }
    public int ParamsCount { get; }

    private ParameterSource(ParameterSourceKind kind, int argumentIndex, int paramsStartIndex, int paramsCount)
    {
        Kind = kind;
        ArgumentIndex = argumentIndex;
        ParamsStartIndex = paramsStartIndex;
        ParamsCount = paramsCount;
    }

    public static ParameterSource FromArgument(int argIndex) =>
        new(ParameterSourceKind.Argument, argIndex, 0, 0);

    public static ParameterSource FromDefault() =>
        new(ParameterSourceKind.Default, 0, 0, 0);

    public static ParameterSource FromParamsRange(int start, int count) =>
        new(ParameterSourceKind.ParamsRange, 0, start, count);
}

internal readonly struct ArgumentParameterMap
{
    public ImmutableArray<ParameterSource> Sources { get; }
    public int ParamsParameterIndex { get; }

    public ArgumentParameterMap(ImmutableArray<ParameterSource> sources, int paramsParameterIndex)
    {
        Sources = sources;
        ParamsParameterIndex = paramsParameterIndex;
    }

    public static ArgumentParameterMap Empty { get; } =
        new(ImmutableArray<ParameterSource>.Empty, -1);
}
