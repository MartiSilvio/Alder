using Alder.Compilation;
using Alder.Compiled.Compilation;
using Alder.Binding;
using Alder.Parsing;
using Alder.Runtime;

namespace Alder.Compiled;

internal sealed class CompiledProvider : ICompiledProvider
{
    internal static readonly CompiledProvider Instance = new();

    private CompiledProvider() { }

    public CompiledExpressionInfo TryCompile(Expr ast, AlderConfig config)
        => ILExpressionCompiler.TryCompile(ast, config);

    public CompiledExpressionInfo TryCompile(BoundExpr bound, AlderConfig config)
        => ILExpressionCompiler.TryCompile(bound, config);
}
