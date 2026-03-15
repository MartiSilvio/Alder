// Polyfill for netstandard2.0 to support records and init-only properties
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
