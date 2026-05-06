namespace Alder.Binding;

internal sealed record BindingReceiver(
    Type ReceiverType,
    string ReceiverName,
    bool EnableImplicitReceiver = true);
