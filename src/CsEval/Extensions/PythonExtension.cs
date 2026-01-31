using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Extensions;

/// <summary>
/// Python language extension for CsEval.
/// </summary>
public sealed class PythonExtension : ILanguageExtension
{
    public static PythonExtension Instance { get; } = new();

    public string Name => "Python";

    public IReadOnlyDictionary<string, Func<List<object?>, object?[], CsEvalContext, CsEvalOptions, CancellationToken, (bool, object?)>> LinqHandlers { get; } =
        new Dictionary<string, Func<List<object?>, object?[], CsEvalContext, CsEvalOptions, CancellationToken, (bool, object?)>>();

    public IReadOnlyDictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>> BinaryOperators { get; } =
        new Dictionary<TokenType, Func<object?, object?, CsEvalOptions, object?>>
        {
            [TokenType.In] = (l, r, opts) => Operators.Contains(r, l, opts),
        };

    public IReadOnlyDictionary<TokenType, ILBinaryOperator> ILBinaryOperators { get; } =
        new Dictionary<TokenType, ILBinaryOperator>
        {
            [TokenType.In] = new(
                typeof(Operators).GetMethod(nameof(Operators.Contains))!,
                SwapOperands: true,
                PassOptions: true,
                BoxResult: true),
        };
}
