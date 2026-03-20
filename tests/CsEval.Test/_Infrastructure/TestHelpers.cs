using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CsEval.Test._Infrastructure;

/// <summary>
/// Static test utilities.
/// </summary>
public static class TestHelpers
{
    private static readonly Regex RoslynCodeRegex = new(@"\bCS\d{4}\b", RegexOptions.Compiled);

    private static readonly ScriptOptions DefaultScriptOptions = ScriptOptions.Default
        .AddReferences(typeof(object).Assembly, typeof(Enumerable).Assembly)
        .AddImports("System", "System.Collections.Generic", "System.Linq")
        .WithLanguageVersion(LanguageVersion.CSharp12);

    #region Parity Test Helpers

    /// <summary>
    /// Runs a C# parity test: evaluates expression in both CsEval and Roslyn, asserts results and types match.
    /// </summary>
    public static Task RunCSharpParityTestAsync(string expr, object? expected, CompilationMode mode)
        => RunCSharpParityTestAsync(expr, null, expected, mode);

    /// <summary>
    /// Runs a C# parity test with variables.
    /// </summary>
    public static async Task RunCSharpParityTestAsync(string expr, Dictionary<string, object?>? variables, object? expected, CompilationMode mode)
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default with { LanguageMode = LanguageMode.Extended });
        if (variables != null)
            foreach (var (name, value) in variables)
                engine.SetVariable(name, value);

        var result = engine.Evaluate(expr);
        var csharpResult = await EvaluateCSharpAsync(expr, variables);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    /// <summary>
    /// Runs a C# parity test without expected value.
    /// </summary>
    public static Task RunCSharpParityTestAsync(string expr, CompilationMode mode)
        => RunCSharpParityTestAsync(expr, mode switch
        {
            CompilationMode.Compiled => CsEvalOptions.Default.UseCompiler(),
            CompilationMode.CompiledFec => CsEvalOptions.Default.UseCompiler(new FastExpressionCompilerAdapter()),
            _ => CsEvalOptions.Default
        });

    /// <summary>
    /// Runs a C# parity test with custom options.
    /// </summary>
    public static async Task RunCSharpParityTestAsync(string expr, CsEvalOptions options)
    {
        var csharpResult = await EvaluateCSharpAsync(expr);

        var engine = new CsEvalEngine(options);
        var result = engine.Evaluate(expr);

        Assert.That(result, Is.EqualTo(csharpResult), $"Value mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    #endregion

    public static string NormalizeExceptionKey(Exception ex) =>
        ex switch
        {
            CsEvalException { ErrorCode: not null } cs => $"{cs.FormattedCode}:{ex.GetType().Name}",
            _ => TryGetRoslynErrorCode(ex.Message) is { } code ? $"{code}:{ex.GetType().Name}" : ex.GetType().Name
        };

    #region Test Data Loading

    private static string? _testDataDirectory;

    public static string LoadTestExpression(string relativePath)
    {
        _testDataDirectory ??= Path.Combine(
            TestContext.CurrentContext.TestDirectory, "TestData");
        var fullPath = Path.Combine(_testDataDirectory, relativePath);
        return File.ReadAllText(fullPath).Trim();
    }

    #endregion

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
        return await CSharpScript.EvaluateAsync<object>(code, DefaultScriptOptions);
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

            var typeName = GetTypeName(value?.GetType() ?? typeof(object));
            declarations.AppendLine($"{typeName} {name} = {literal};");
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
            null => "(object?)null",
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

    private static string? TryGetRoslynErrorCode(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        var match = RoslynCodeRegex.Match(message);
        return match.Success ? match.Value : null;
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
        // Handle primitive types
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(short)) return "short";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(char)) return "char";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";

        // Handle generic types like List<int>
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();
            var baseName = genericDef.Name.Split('`')[0];
            var argNames = string.Join(", ", genericArgs.Select(GetTypeName));
            return $"{baseName}<{argNames}>";
        }

        // Handle arrays
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return $"{GetTypeName(elementType)}[]";
        }

        return type.Name;
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
