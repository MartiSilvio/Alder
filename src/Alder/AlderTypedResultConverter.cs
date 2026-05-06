using System.Diagnostics.CodeAnalysis;
using Alder.Diagnostics;
using Alder.Runtime;

namespace Alder;

internal static class AlderTypedResultConverter
{
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Projection DTO binding is a narrow structural-projection conversion path. Scalar and delegate conversions remain unchanged.")]
    internal static T? Convert<T>(object? result)
    {
        return result switch
        {
            null => default,
            T typed => typed,
            _ when LambdaDelegateConverter.IsSupportedDelegateType(typeof(T)) =>
                (T)(object)(LambdaDelegateConverter.TryConvert(result, typeof(T))
                    ?? throw new AlderException(
                        DiagnosticDescriptors.DelegateConversionFailed, result.GetType().Name, typeof(T).Name)),
            StructuralObjectValue projection => AlderProjectionMaterializer.Materialize<T>(projection),
            _ => (T)System.Convert.ChangeType(result, typeof(T))
        };
    }
}
