namespace Alder.Binding.Services;

internal static class NamedTargetBindingService
{
    internal readonly record struct NamedTargetBinding(BoundType StaticType, int? LocalId);

    internal static NamedTargetBinding Resolve(string name, BindingContext context, BoundType fallbackType)
    {
        if (context.TryGetLocal(name, out var localType, out var localId))
            return new NamedTargetBinding(localType, localId);

        var runtimeProbe = new RuntimeBindingProbeService(context.RuntimeContext);
        if (runtimeProbe.TryGetValueType(name, out var runtimeType))
            return new NamedTargetBinding(runtimeType, LocalId: null);

        return new NamedTargetBinding(fallbackType, LocalId: null);
    }
}
