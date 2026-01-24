using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    /// <summary>
    /// Maps token types to their corresponding CLR types.
    /// </summary>
    private static readonly Dictionary<TokenType, Type> TokenTypeToClrType = new()
    {
        [TokenType.Sbyte] = typeof(sbyte),
        [TokenType.Byte] = typeof(byte),
        [TokenType.Short] = typeof(short),
        [TokenType.Ushort] = typeof(ushort),
        [TokenType.Int] = typeof(int),
        [TokenType.Uint] = typeof(uint),
        [TokenType.Long] = typeof(long),
        [TokenType.Ulong] = typeof(ulong),
        [TokenType.Float] = typeof(float),
        [TokenType.Double] = typeof(double),
        [TokenType.Decimal] = typeof(decimal),
        [TokenType.Bool] = typeof(bool),
        [TokenType.Char] = typeof(char),
        [TokenType.StringType] = typeof(string),
        [TokenType.Object] = typeof(object),
    };

    /// <summary>
    /// C# implicit numeric conversions table.
    /// Key: source type, Value: set of types it can implicitly convert to.
    /// Based on ECMA-334 (C# Language Specification).
    /// </summary>
    private static readonly Dictionary<Type, HashSet<Type>> ImplicitConversions = new()
    {
        [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(float)] = [typeof(double)],
        [typeof(char)] = [typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
    };

    private static object? ValidateAndCoerceType(Token typeToken, object? value, string varName)
    {
        // Object accepts anything
        if (typeToken.Type == TokenType.Object)
            return value;

        if (!TokenTypeToClrType.TryGetValue(typeToken.Type, out var targetType))
            throw new EvalException($"Unknown type '{typeToken.Lexeme}'");

        // Null check for value types
        if (value == null)
        {
            if (targetType.IsValueType)
                throw new EvalException($"Cannot assign null to {typeToken.Lexeme} variable '{varName}'");
            return null;
        }

        var sourceType = value.GetType();

        // Identity conversion - same type
        if (sourceType == targetType)
            return value;

        // Check if implicit conversion is allowed
        if (ImplicitConversions.TryGetValue(sourceType, out var allowedTargets) && allowedTargets.Contains(targetType))
        {
            // Use Convert.ChangeType for the actual conversion
            return Convert.ChangeType(value, targetType);
        }

        // Special case: char from single-character string
        if (targetType == typeof(char) && value is string { Length: 1 } s)
            return s[0];

        throw new EvalException($"Cannot assign {sourceType.Name} to {typeToken.Lexeme} variable '{varName}'");
    }
}
