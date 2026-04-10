using Alder.Binding;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;
using Binder = Alder.Binding.Binder;

namespace Alder.Runtime.Semantics;

internal static class PatternRuntime
{
    public static bool MatchPattern(
        object? value,
        Pattern pattern,
        AlderContext context,
        CancellationToken cancellationToken)
    {
        return MatchPatternCore(value, pattern, new PatternRuntimeContext(context, context.Config, cancellationToken));
    }

    private static bool MatchPatternCore(object? value, Pattern pattern, PatternRuntimeContext runtime)
    {
        while (true)
        {
            switch (pattern)
            {
                case ConstantPattern constantPattern:
                {
                    var constantValue = runtime.EvaluatePatternExpression(constantPattern.Value);
                    if (constantValue is Type typeValue) return TypeHelpers.IsType(value, typeValue);
                    return TypeHelpers.RequireBoolean(Operators.Equals(value, constantValue));
                }

                case TypePattern typePattern:
                {
                    var targetType = runtime.Context.TypeResolver.ResolveType(typePattern.TypeToken.Lexeme);
                    var isMatch = TypeHelpers.IsType(value, targetType);
                    if (isMatch && typePattern.VariableName != null) runtime.Context.DefineNew(typePattern.VariableName.Value.Lexeme, value, targetType);
                    return isMatch;
                }

                case VarPattern varPattern:
                {
                    var runtimeType = value?.GetType() ?? typeof(object);
                    runtime.Context.DefineNew(varPattern.VariableName.Lexeme, value, runtimeType);
                    return true;
                }

                case DiscardPattern:
                    return true;

                case NotPattern notPattern:
                    return !MatchPatternCore(value, notPattern.Operand, runtime);

                case AndPattern andPattern:
                    return MatchPatternCore(value, andPattern.Left, runtime) && MatchPatternCore(value, andPattern.Right, runtime);

                case OrPattern orPattern:
                    return MatchPatternCore(value, orPattern.Left, runtime) || MatchPatternCore(value, orPattern.Right, runtime);

                case ParenthesizedPattern parenthesizedPattern:
                    pattern = parenthesizedPattern.Inner;
                    continue;

                case PositionalPattern positionalPattern:
                {
                    if (value is not System.Runtime.CompilerServices.ITuple tuple) return false;
                    if (tuple.Length != positionalPattern.Subpatterns.Count) return false;
                    for (var i = 0; i < positionalPattern.Subpatterns.Count; i++)
                    {
                        if (!MatchPatternCore(tuple[i], positionalPattern.Subpatterns[i], runtime)) return false;
                    }

                    return true;
                }

                case RelationalPattern relationalPattern:
                {
                    var operand = runtime.EvaluatePatternExpression(relationalPattern.Operand);
                    return relationalPattern.Operator.Type switch
                    {
                        TokenType.Less => TypeHelpers.RequireBoolean(Operators.LessThan(value, operand, runtime.Config.StringComparison)),
                        TokenType.LessEqual => TypeHelpers.RequireBoolean(Operators.LessThanOrEqual(value, operand, runtime.Config.StringComparison)),
                        TokenType.Greater => TypeHelpers.RequireBoolean(Operators.GreaterThan(value, operand, runtime.Config.StringComparison)),
                        TokenType.GreaterEqual => TypeHelpers.RequireBoolean(Operators.GreaterThanOrEqual(value, operand, runtime.Config.StringComparison)),
                        _ => throw new AlderException(DiagnosticDescriptors.UnknownRelationalPatternOperator, relationalPattern.Operator.Lexeme)
                    };
                }

                case ListPattern listPattern:
                    return MatchListPattern(value, listPattern, runtime);

                case PropertyPattern propertyPattern:
                {
                    if (propertyPattern.TypeToken != null)
                    {
                        var propertyTargetType = runtime.Context.TypeResolver.ResolveType(propertyPattern.TypeToken.Value.Lexeme);
                        if (!TypeHelpers.IsType(value, propertyTargetType)) return false;
                    }

                    if (value == null) return false;

                    foreach (var (name, subPattern) in propertyPattern.Properties)
                    {
                        var propertyValue = MemberAccess.GetMember(value, name.Lexeme, false, runtime.Context);
                        if (!MatchPatternCore(propertyValue, subPattern, runtime)) return false;
                    }

                    if (propertyPattern.VariableName != null)
                    {
                        var runtimeType = value.GetType();
                        runtime.Context.DefineNew(propertyPattern.VariableName.Value.Lexeme, value, runtimeType);
                    }

                    return true;
                }

                default:
                    throw new AlderException(DiagnosticDescriptors.PatternNotImplemented, pattern.GetType().Name);
            }

            break;
        }
    }

    private static bool MatchListPattern(object? value, ListPattern listPattern, PatternRuntimeContext runtime)
    {
        if (value == null) return false;

        var length = GetCountableLength(value);
        if (length < 0) return false;

        var patterns = listPattern.Patterns;
        var sliceIndex = -1;
        for (var i = 0; i < patterns.Count; i++)
        {
            if (patterns[i] is SlicePattern)
            {
                sliceIndex = i;
                break;
            }
        }

        if (sliceIndex < 0)
        {
            // No slice: exact length match required
            if (length != patterns.Count) return false;
            for (var i = 0; i < patterns.Count; i++)
            {
                if (!MatchPatternCore(GetIndexedElement(value, i), patterns[i], runtime))
                    return false;
            }
            return true;
        }

        // Has slice: prefix patterns before slice, suffix patterns after
        var prefixCount = sliceIndex;
        var suffixCount = patterns.Count - sliceIndex - 1;
        if (length < prefixCount + suffixCount) return false;

        // Match prefix
        for (var i = 0; i < prefixCount; i++)
        {
            if (!MatchPatternCore(GetIndexedElement(value, i), patterns[i], runtime))
                return false;
        }

        // Match suffix (from end)
        for (var i = 0; i < suffixCount; i++)
        {
            if (!MatchPatternCore(GetIndexedElement(value, length - suffixCount + i), patterns[sliceIndex + 1 + i], runtime))
                return false;
        }

        // Handle slice subpattern if present
        var slice = (SlicePattern)patterns[sliceIndex];
        if (slice.Subpattern != null)
        {
            var sliceLength = length - prefixCount - suffixCount;
            var sliceArray = new object?[sliceLength];
            for (var i = 0; i < sliceLength; i++)
                sliceArray[i] = GetIndexedElement(value, prefixCount + i);
            if (!MatchPatternCore(sliceArray, slice.Subpattern, runtime))
                return false;
        }

        return true;
    }

    private static int GetCountableLength(object value)
    {
        // C# list patterns require Length or Count property and int indexer.
        // IList covers arrays, List<T>, and most collections.
        if (value is IList list) return list.Count;
        // String has Length and indexer
        if (value is string s) return s.Length;
        return -1;
    }

    private static object? GetIndexedElement(object value, int index)
    {
        if (value is IList list) return list[index];
        if (value is string s) return s[index];
        throw new InvalidOperationException($"Cannot index into {value.GetType().Name}");
    }

    private sealed class PatternRuntimeContext(
        AlderContext context,
        AlderConfig config,
        CancellationToken cancellationToken)
    {
        private BoundEvaluator? _evaluator;

        public AlderContext Context { get; } = context;
        public AlderConfig Config { get; } = config;
        public CancellationToken CancellationToken { get; } = cancellationToken;

        public object? EvaluatePatternExpression(Expr expression)
        {
            var binder = new Binder();
            var boundExpression = binder.Bind(expression, new BindingContext(Context));
            _evaluator ??= new BoundEvaluator(Context);
            return _evaluator.Evaluate(boundExpression);
        }
    }
}
