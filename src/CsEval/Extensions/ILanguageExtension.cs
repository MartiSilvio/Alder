using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Extensions;

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
    IReadOnlyDictionary<string, Func<List<object?>, object?[], CsEvalContext, (bool, object?)>> LinqHandlers { get; }

    /// <summary>
    /// Binary operators from this language.
    /// Example: JavaScript ===, Python "in"
    /// </summary>
    IReadOnlyDictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>> BinaryOperators { get; }
}
