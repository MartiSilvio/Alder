namespace Alder.Aot;

/// <summary>
/// Alder's built-in AOT type context.
/// It covers core primitives and common BCL types so most applications get AOT dispatch support without extra setup.
/// Add your own generated contexts when you need coverage for application-specific types.
/// </summary>
public partial class AlderBuiltInContext : AlderTypeContext;
