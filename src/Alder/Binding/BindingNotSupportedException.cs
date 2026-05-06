namespace Alder.Binding;

internal sealed class BindingNotSupportedException : Exception
{
    public BindingNotSupportedException(string message) : base(message)
    {
    }
}
