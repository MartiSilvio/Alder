using CsEval.Parsing;

namespace CsEval.Evaluation;

public sealed partial class Evaluator
{
    private static object? ValidateAndCoerceType(Token typeToken, object? value, string varName)
    {
        return typeToken.Type switch
        {
            TokenType.Int => CoerceToInt(value, varName),
            TokenType.Long => CoerceToLong(value, varName),
            TokenType.Double => CoerceToDouble(value, varName),
            TokenType.Float => CoerceToFloat(value, varName),
            TokenType.Decimal => CoerceToDecimal(value, varName),
            TokenType.StringType => CoerceToString(value, varName),
            TokenType.Bool => CoerceToBool(value, varName),
            TokenType.Object => value, // object accepts anything
            TokenType.Sbyte => CoerceToSbyte(value, varName),
            TokenType.Byte => CoerceToByte(value, varName),
            TokenType.Short => CoerceToShort(value, varName),
            TokenType.Ushort => CoerceToUshort(value, varName),
            TokenType.Uint => CoerceToUint(value, varName),
            TokenType.Ulong => CoerceToUlong(value, varName),
            TokenType.Char => CoerceToChar(value, varName),
            _ => throw new EvalException($"Unknown type '{typeToken.Lexeme}'")
        };
    }

    private static int CoerceToInt(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to int variable '{varName}'"),
            int i => i,
            long l when l >= int.MinValue && l <= int.MaxValue => (int)l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to int variable '{varName}'")
        };
    }

    private static long CoerceToLong(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to long variable '{varName}'"),
            long l => l,
            int i => i,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            uint ui => ui,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to long variable '{varName}'")
        };
    }

    private static double CoerceToDouble(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to double variable '{varName}'"),
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            uint ui => ui,
            ulong ul => ul,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to double variable '{varName}'")
        };
    }

    private static float CoerceToFloat(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to float variable '{varName}'"),
            float f => f,
            int i => i,
            long l => l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to float variable '{varName}'")
        };
    }

    private static decimal CoerceToDecimal(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to decimal variable '{varName}'"),
            decimal m => m,
            int i => i,
            long l => l,
            sbyte sb => sb,
            byte b => b,
            short s => s,
            ushort us => us,
            uint ui => ui,
            ulong ul => ul,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to decimal variable '{varName}'")
        };
    }

    private static string CoerceToString(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to string variable '{varName}'"),
            string s => s,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to string variable '{varName}'")
        };
    }

    private static bool CoerceToBool(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to bool variable '{varName}'"),
            bool b => b,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to bool variable '{varName}'")
        };
    }

    private static sbyte CoerceToSbyte(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to sbyte variable '{varName}'"),
            sbyte sb => sb,
            int i when i >= sbyte.MinValue && i <= sbyte.MaxValue => (sbyte)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to sbyte variable '{varName}'")
        };
    }

    private static byte CoerceToByte(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to byte variable '{varName}'"),
            byte b => b,
            int i when i >= byte.MinValue && i <= byte.MaxValue => (byte)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to byte variable '{varName}'")
        };
    }

    private static short CoerceToShort(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to short variable '{varName}'"),
            short s => s,
            sbyte sb => sb,
            byte b => b,
            int i when i >= short.MinValue && i <= short.MaxValue => (short)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to short variable '{varName}'")
        };
    }

    private static ushort CoerceToUshort(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to ushort variable '{varName}'"),
            ushort us => us,
            byte b => b,
            int i when i >= ushort.MinValue && i <= ushort.MaxValue => (ushort)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to ushort variable '{varName}'")
        };
    }

    private static uint CoerceToUint(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to uint variable '{varName}'"),
            uint ui => ui,
            byte b => b,
            ushort us => us,
            int i when i >= 0 => (uint)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to uint variable '{varName}'")
        };
    }

    private static ulong CoerceToUlong(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to ulong variable '{varName}'"),
            ulong ul => ul,
            byte b => b,
            ushort us => us,
            uint ui => ui,
            int i when i >= 0 => (ulong)i,
            long l when l >= 0 => (ulong)l,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to ulong variable '{varName}'")
        };
    }

    private static char CoerceToChar(object? value, string varName)
    {
        return value switch
        {
            null => throw new EvalException($"Cannot assign null to char variable '{varName}'"),
            char c => c,
            string s when s.Length == 1 => s[0],
            int i when i >= char.MinValue && i <= char.MaxValue => (char)i,
            _ => throw new EvalException($"Cannot assign {value.GetType().Name} to char variable '{varName}'")
        };
    }
}
