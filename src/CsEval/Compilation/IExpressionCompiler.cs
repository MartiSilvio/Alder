using CsEval.Parsing;

namespace CsEval.Compilation;

/// <summary>
/// Interface for expression compiler backends.
/// The default implementation uses System.Linq.Expressions to compile AST to IL.
/// Alternative implementations (e.g., FastExpressionCompiler) can be provided
/// via CsEvalEngine constructor or CsEvalOptions.
/// </summary>
public interface IExpressionCompiler
{
    /// <summary>
    /// Attempts to compile an AST to a native delegate.
    /// Returns a CompiledExpressionInfo with the delegate on success,
    /// or with IsCompilable=false and a FailureReason on failure.
    /// </summary>
    CompiledExpressionInfo TryCompile(Expr ast);
}
