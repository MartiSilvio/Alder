using CsEval.Binding;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;

namespace CsEval.Runtime;

internal static class PatternRuntime
{
    public static bool MatchPattern(
        object? value,
        Pattern pattern,
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken)
    {
        return MatchPatternCore(value, pattern, new PatternRuntimeContext(context, options, cancellationToken));
    }

    private static bool MatchPatternCore(object? value, Pattern pattern, PatternRuntimeContext runtime)
    {
        switch (pattern)
        {
            case ConstantPattern constantPattern:
            {
                var constantValue = runtime.EvaluatePatternExpression(constantPattern.Value);
                if (constantValue is Type typeValue)
                    return TypeHelpers.IsType(value, typeValue);
                return TypeHelpers.RequireBoolean(Operators.Equals(value, constantValue));
            }

            case TypePattern typePattern:
            {
                var targetType = runtime.Context.TypeResolver.ResolveType(typePattern.TypeToken.Lexeme);
                var isMatch = TypeHelpers.IsType(value, targetType);
                if (isMatch && typePattern.VariableName != null)
                    runtime.Context.DefineNew(typePattern.VariableName.Value.Lexeme, value, targetType);
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
                return MatchPatternCore(value, andPattern.Left, runtime) &&
                       MatchPatternCore(value, andPattern.Right, runtime);

            case OrPattern orPattern:
                return MatchPatternCore(value, orPattern.Left, runtime) ||
                       MatchPatternCore(value, orPattern.Right, runtime);

            case ParenthesizedPattern parenthesizedPattern:
                return MatchPatternCore(value, parenthesizedPattern.Inner, runtime);

            case PositionalPattern positionalPattern:
            {
                if (value is not System.Runtime.CompilerServices.ITuple tuple)
                    return false;
                if (tuple.Length != positionalPattern.Subpatterns.Count)
                    return false;
                for (var i = 0; i < positionalPattern.Subpatterns.Count; i++)
                {
                    if (!MatchPatternCore(tuple[i], positionalPattern.Subpatterns[i], runtime))
                        return false;
                }
                return true;
            }

            case RelationalPattern relationalPattern:
            {
                var operand = runtime.EvaluatePatternExpression(relationalPattern.Operand);
                return relationalPattern.Operator.Type switch
                {
                    TokenType.Less => TypeHelpers.RequireBoolean(Operators.LessThan(value, operand, runtime.Options)),
                    TokenType.LessEqual => TypeHelpers.RequireBoolean(Operators.LessThanOrEqual(value, operand, runtime.Options)),
                    TokenType.Greater => TypeHelpers.RequireBoolean(Operators.GreaterThan(value, operand, runtime.Options)),
                    TokenType.GreaterEqual => TypeHelpers.RequireBoolean(Operators.GreaterThanOrEqual(value, operand, runtime.Options)),
                    _ => throw new CsEvalException(
                        $"Unknown relational pattern operator '{relationalPattern.Operator.Lexeme}'")
                };
            }

            case PropertyPattern propertyPattern:
            {
                if (propertyPattern.TypeToken != null)
                {
                    var propertyTargetType = runtime.Context.TypeResolver.ResolveType(propertyPattern.TypeToken.Value.Lexeme);
                    if (!TypeHelpers.IsType(value, propertyTargetType))
                        return false;
                }

                if (value == null)
                    return false;

                foreach (var (name, subPattern) in propertyPattern.Properties)
                {
                    var propertyValue = MemberAccess.GetMember(value, name.Lexeme, runtime.Options, nullSafe: false, runtime.Context);
                    if (!MatchPatternCore(propertyValue, subPattern, runtime))
                        return false;
                }

                if (propertyPattern.VariableName != null)
                {
                    var runtimeType = value.GetType();
                    runtime.Context.DefineNew(propertyPattern.VariableName.Value.Lexeme, value, runtimeType);
                }

                return true;
            }

            default:
                throw new CsEvalException(DiagnosticDescriptors.PatternNotImplemented, pattern.GetType().Name);
        }
    }

    private sealed class PatternRuntimeContext(
        CsEvalContext context,
        CsEvalOptions options,
        CancellationToken cancellationToken)
    {
        private BoundEvaluator? _evaluator;

        public CsEvalContext Context { get; } = context;
        public CsEvalOptions Options { get; } = options;
        public CancellationToken CancellationToken { get; } = cancellationToken;

        public object? EvaluatePatternExpression(Expr expression)
        {
            AstDepthValidator.EnsureWithinLimit(expression, Options.MaxExpressionDepth);
            var binder = new CsEval.Binding.Binder();
            var boundExpression = binder.Bind(expression, new BindingContext(Context));
            _evaluator ??= new BoundEvaluator(Context, Options, CancellationToken);
            return _evaluator.Evaluate(boundExpression);
        }
    }
}
