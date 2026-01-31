using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Extensions;

/// <summary>
/// Describes how to compile a binary operator to IL.
/// </summary>
public readonly record struct ILBinaryOperator(
    MethodInfo Method,
    bool SwapOperands = false,
    bool PassOptions = true,
    bool BoxResult = false);

/// <summary>
/// Interface for language extensions. Implement to add syntax from other languages.
/// </summary>
public interface ILanguageExtension
{
    /// <summary>
    /// Display name for the extension (e.g., "JavaScript", "Python").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// LINQ method handlers. Maps method name to handler function.
    /// Use LinqDispatcher handlers for aliases (e.g., "Map" -> LinqDispatcher.HandleSelect)
    /// or provide custom implementations.
    /// </summary>
    IReadOnlyDictionary<string, Func<List<object?>, object?[], CsEvalContext, CsEvalOptions, CancellationToken, (bool, object?)>> LinqHandlers { get; }

    /// <summary>
    /// Binary operators for AST interpretation.
    /// </summary>
    IReadOnlyDictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>> BinaryOperators { get; }

    /// <summary>
    /// Binary operators for IL compilation. Optional - if not provided, operator falls back to AST.
    /// </summary>
    IReadOnlyDictionary<TokenType, ILBinaryOperator> ILBinaryOperators =>
        new Dictionary<TokenType, ILBinaryOperator>();
}
