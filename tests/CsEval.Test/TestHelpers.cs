using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Test;

/// <summary>
/// Static test utilities.
/// </summary>
public static class TestHelpers
{
    private static readonly ScriptOptions DefaultScriptOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Collections.Generic", "System.Linq")
        .WithLanguageVersion(LanguageVersion.CSharp12);

    public static IDictionary<string, object?> CreateItem(string name, double price)
    {
        IDictionary<string, object?> item = new ExpandoObject();
        item["Name"] = name;
        item["Price"] = price;
        return item;
    }

    /// <summary>
    /// Evaluates C# code using Roslyn scripting.
    /// </summary>
    public static async Task<object?> EvaluateCSharpAsync(string code)
    {
        return await CSharpScript.EvaluateAsync(code, DefaultScriptOptions);
    }

    /// <summary>
    /// Evaluates C# code using Roslyn scripting with variables.
    /// Variables are serialized as C# declarations prepended to the script.
    /// </summary>
    /// <param name="code">The expression to evaluate</param>
    /// <param name="variables">Variables to inject into the script</param>
    /// <returns>The evaluation result, or null if serialization fails</returns>
    public static async Task<object?> EvaluateCSharpAsync(string code, Dictionary<string, object?>? variables)
    {
        if (variables == null || variables.Count == 0)
            return await CSharpScript.EvaluateAsync(code, DefaultScriptOptions);

        var declarations = new StringBuilder();
        foreach (var (name, value) in variables)
        {
            var literal = TrySerializeValue(value);
            if (literal == null)
                return null; // Can't serialize this type, skip Roslyn comparison

            declarations.AppendLine($"var {name} = {literal};");
        }

        var fullScript = declarations + code;
        return await CSharpScript.EvaluateAsync(fullScript, DefaultScriptOptions);
    }

    /// <summary>
    /// Tries to serialize a value as a C# literal.
    /// Returns null if the type cannot be serialized.
    /// </summary>
    private static string? TrySerializeValue(object? value)
    {
        return value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            char c => $"'{EscapeChar(c)}'",
            string s => $"\"{EscapeString(s)}\"",
            byte n => $"(byte){n}",
            sbyte n => $"(sbyte){n}",
            short n => $"(short){n}",
            ushort n => $"(ushort){n}",
            int n => n.ToString(),
            uint n => $"{n}u",
            long n => $"{n}L",
            ulong n => $"{n}UL",
            float n => FormatFloat(n),
            double n => FormatDouble(n),
            decimal n => $"{n.ToString(CultureInfo.InvariantCulture)}m",
            IList list => TrySerializeList(list),
            _ => null // Unsupported type
        };
    }

    private static string? TrySerializeList(IList list)
    {
        if (list.Count == 0)
        {
            var elementType = GetListElementType(list);
            return elementType != null
                ? $"new List<{GetTypeName(elementType)}>()"
                : "new List<object>()";
        }

        var elements = new List<string>();
        Type? inferredType = null;

        foreach (var item in list)
        {
            var serialized = TrySerializeValue(item);
            if (serialized == null)
                return null;
            elements.Add(serialized);

            if (item != null)
                inferredType ??= item.GetType();
        }

        var listType = GetListElementType(list) ?? inferredType ?? typeof(object);
        return $"new List<{GetTypeName(listType)}> {{ {string.Join(", ", elements)} }}";
    }

    private static Type? GetListElementType(IList list)
    {
        var listType = list.GetType();
        if (listType.IsGenericType)
            return listType.GetGenericArguments().FirstOrDefault();
        if (listType.IsArray)
            return listType.GetElementType();
        return null;
    }

    private static string GetTypeName(Type type)
    {
        return type switch
        {
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(short) => "short",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(sbyte) => "sbyte",
            _ when type == typeof(uint) => "uint",
            _ when type == typeof(ulong) => "ulong",
            _ when type == typeof(ushort) => "ushort",
            _ when type == typeof(float) => "float",
            _ when type == typeof(double) => "double",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(char) => "char",
            _ when type == typeof(string) => "string",
            _ when type == typeof(object) => "object",
            _ => type.Name
        };
    }

    private static string FormatFloat(float n)
    {
        if (float.IsNaN(n)) return "float.NaN";
        if (float.IsPositiveInfinity(n)) return "float.PositiveInfinity";
        if (float.IsNegativeInfinity(n)) return "float.NegativeInfinity";
        return $"{n.ToString("G9", CultureInfo.InvariantCulture)}f";
    }

    private static string FormatDouble(double n)
    {
        if (double.IsNaN(n)) return "double.NaN";
        if (double.IsPositiveInfinity(n)) return "double.PositiveInfinity";
        if (double.IsNegativeInfinity(n)) return "double.NegativeInfinity";
        return n.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static string EscapeString(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s)
            sb.Append(EscapeChar(c));
        return sb.ToString();
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '"' => "\\\"",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\0' => "\\0",
            '\a' => "\\a",
            '\b' => "\\b",
            '\f' => "\\f",
            '\v' => "\\v",
            _ when c < 32 || c > 126 => $"\\u{(int)c:X4}",
            _ => c.ToString()
        };
    }
}
