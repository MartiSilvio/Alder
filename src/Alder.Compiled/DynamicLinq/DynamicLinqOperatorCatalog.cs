namespace Alder.Compiled.DynamicLinq;

internal readonly record struct DynamicLinqOperatorDescriptor(
    string ExtensionName,
    bool RequireEnumerableSource,
    bool RequireQueryableSource,
    bool RequireAsyncSource,
    bool RequireUntypedSequenceResult,
    bool RequireUntypedScalarResult,
    DynamicQueryOperatorKind? DispatcherOperatorKind,
    DynamicLinqProbeType DispatcherProbeType);

internal static partial class DynamicLinqOperatorCatalog
{
    internal static Type? ResolveProbeType(DynamicLinqProbeType probeType) =>
        probeType switch
        {
            DynamicLinqProbeType.None => null,
            DynamicLinqProbeType.Boolean => typeof(bool),
            DynamicLinqProbeType.Int32 => typeof(int),
            DynamicLinqProbeType.Int64 => typeof(long),
            DynamicLinqProbeType.Decimal => typeof(decimal),
            DynamicLinqProbeType.String => typeof(string),
            DynamicLinqProbeType.Object => typeof(object),
            _ => throw new InvalidOperationException($"Unsupported probe type '{probeType}'.")
        };
}
