using System.Runtime.CompilerServices;

namespace Alder.Compiled;

public static class AlderCompiledExtensions
{
    public static AlderOptions UseCompiler(this AlderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ThrowIfDynamicCodeNotSupported();
        options.Compiler = CompiledProvider.Instance;
        return options;
    }

    public static AlderOptions UseCompiler(this AlderOptions options, IExpressionCompiler expressionCompiler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(expressionCompiler);
        ThrowIfDynamicCodeNotSupported();
        options.Compiler = CompiledProvider.Instance;
        options.ExpressionCompiler = expressionCompiler;
        return options;
    }

    private static void ThrowIfDynamicCodeNotSupported()
    {
#if NET7_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported)
            throw new PlatformNotSupportedException(
                "Expression compilation requires a JIT compiler and is not available in AOT environments. " +
                "Remove the UseCompiler() call — the interpreter with AOT metadata provides the best performance on this platform.");
#endif
    }
}
