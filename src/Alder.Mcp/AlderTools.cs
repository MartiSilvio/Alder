using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Alder;
using Alder.Tracing;
using ModelContextProtocol;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class AlderTools
{
    [McpServerTool(Name = "evaluate", Title = "Evaluate C# Expression", Destructive = false, ReadOnly = true, OpenWorld = false, Idempotent = true),
     Description(
        "Execute a C# expression and return its result. " +
        "Supports: arithmetic, string interpolation, LINQ (Select, Where, Aggregate, etc.), lambdas, ternary (?:), " +
        "null-coalescing (??), pattern matching (is), object creation (new), method calls on .NET types (Math.Round, string.Join, etc.), " +
        "multi-statement blocks (semicolons between statements — last expression is the return value), " +
        "and variable injection from the 'variables' parameter. " +
        "Does NOT support: class/struct/enum/record declarations, using directives, async/await, or file I/O. " +
        "Execution limits: 100M statements, 100M loop iterations, 30s timeout. " +
        "Output format: '<value> (<TypeName>)' e.g. '42 (Int32)', '\"hello\" (String)', '[1, 2, 3] (List`1)'. " +
        "Null results return 'null'. Collections are formatted as '[elem, elem, ...]'. " +
        "Dictionaries are formatted as '{key: value, ...}'.")]
    public static string Evaluate(
        AlderEngine engine,
        [Description(
            "The C# expression to evaluate. " +
            "Single expression: '2 + 2', 'Math.Sqrt(144)', 'new[] {1,2,3}.Where(x => x > 1).Sum()'. " +
            "Multi-statement: 'var x = 10; var y = 20; x + y' (last expression is the return value). " +
            "String interpolation: '$\"Hello {name}, you are {age} years old\"'. " +
            "LINQ: 'items.Where(x => x > 0).Select(x => x * 2).ToList()'. " +
            "Ternary: 'score >= 90 ? \"A\" : score >= 80 ? \"B\" : \"C\"'.")]
        string expression,
        [Description(
            "JSON object whose keys become C# variables available in the expression. " +
            "Keys must be valid C# identifiers (not keywords like 'class', 'int', etc.). " +
            "Values are deserialized as: strings → string, integers → int (or long if > 2^31), " +
            "numbers with decimals → double, booleans → bool, null → null, " +
            "arrays → List<object?>, objects → Dictionary<string, object?>. " +
            "Example: {\"name\": \"Alice\", \"scores\": [95, 87, 92], \"threshold\": 90}")]
        string? variables = null,
        CancellationToken cancellationToken = default)
    {
        ValidateExpression(expression);
        var vars = ParseVariables(variables);

        try
        {
            var result = engine.Evaluate(expression, vars, cancellationToken);
            return FormatResult(result);
        }
        catch (AlderException ex)
        {
            throw new McpException(FormatError(ex));
        }
        catch (Exception ex)
        {
            throw new McpException($"Runtime error ({ex.GetType().Name}): {ex.Message}");
        }
    }

    [McpServerTool(Name = "validate", Title = "Validate C# Expression", Destructive = false, ReadOnly = true, OpenWorld = false, Idempotent = true),
     Description(
        "Check whether a C# expression is syntactically and semantically valid WITHOUT executing it. " +
        "Use this to verify an expression before calling 'evaluate', or to diagnose syntax errors. " +
        "Returns 'Valid' if the expression would evaluate successfully, or a list of diagnostics with " +
        "C#-compatible error codes and messages (e.g. 'CS0103: The name 'foo' does not exist in the current context'). " +
        "This tool never executes the expression — it only parses and binds it. Safe to call on untrusted input. " +
        "Note: cannot validate expressions that reference injected variables — use 'evaluate' for those.")]
    public static string Validate(
        AlderEngine engine,
        [Description(
            "The C# expression to validate. Same syntax rules as the 'evaluate' tool. " +
            "Example: 'Math.Sqrt(x) > threshold' — will report CS0103 if 'x' or 'threshold' are not provided as variables.")]
        string expression)
    {
        ValidateExpression(expression);

        if (engine.TryValidate(expression, out var diagnostics))
            return "Valid";

        var sb = new StringBuilder();
        sb.AppendLine("Invalid:");
        foreach (var d in diagnostics)
        {
            sb.Append(d.FormattedCode ?? "error");
            sb.Append(": ");
            sb.AppendLine(d.Message);
        }
        return sb.ToString().TrimEnd();
    }

    [McpServerTool(Name = "evaluate_with_trace", Title = "Evaluate C# Expression with Trace", Destructive = false, ReadOnly = true, OpenWorld = false, Idempotent = true),
     Description(
        "Execute a C# expression and return both the result AND a step-by-step evaluation trace showing how " +
        "each sub-expression was computed. Use this ONLY when the user needs to understand the evaluation process, " +
        "debug unexpected results, or see intermediate values. For simple evaluations, prefer 'evaluate' instead. " +
        "Output format: 'Result: <value>' followed by an indented trace tree where each line shows " +
        "'<NodeKind>: <source> → <value>' (e.g. 'BinaryExpression: 2 + 3 → 5'). " +
        "Failed nodes show '✗ <error>' instead of a value. " +
        "Nested sub-expressions are indented under their parent.")]
    public static string EvaluateWithTrace(
        AlderEngine engine,
        [Description(
            "The C# expression to trace. Same syntax as 'evaluate'. " +
            "Example: '(1 + 2) * 3' will show the inner addition evaluated first, then the multiplication.")]
        string expression,
        [Description(
            "JSON object whose keys become C# variables. Same format as the 'evaluate' tool's variables parameter.")]
        string? variables = null,
        CancellationToken cancellationToken = default)
    {
        ValidateExpression(expression);
        var vars = ParseVariables(variables);

        try
        {
            var trace = engine.EvaluateWithTrace(expression, vars, cancellationToken);
            var sb = new StringBuilder();
            sb.Append("Result: ");
            sb.AppendLine(FormatValue(trace.Result));
            sb.AppendLine();
            sb.AppendLine("Trace:");
            AppendTrace(sb, trace.Tree, indent: 0);
            return sb.ToString().TrimEnd();
        }
        catch (AlderException ex)
        {
            throw new McpException(FormatError(ex));
        }
        catch (Exception ex)
        {
            throw new McpException($"Runtime error ({ex.GetType().Name}): {ex.Message}");
        }
    }

    static void ValidateExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new McpException("'expression' parameter is required and must not be empty.");
    }

    static IDictionary<string, object?>? ParseVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new McpException(
                $"Invalid JSON in 'variables' parameter: {ex.Message}. " +
                "Expected a JSON object like {\"x\": 5, \"name\": \"Alice\"}.");
        }

        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new McpException(
                $"'variables' must be a JSON object, got {doc.RootElement.ValueKind}. " +
                "Expected format: {\"x\": 5, \"name\": \"Alice\"}.");
        }

        var vars = new Dictionary<string, object?>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            vars[prop.Name] = ConvertJsonElement(prop.Value);
        }
        return vars;
    }

    static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt32(out var i) => i,
        JsonValueKind.Number when element.TryGetInt64(out var l) => l,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
        _ => element.GetRawText(),
    };

    static string FormatResult(object? result)
    {
        var sb = new StringBuilder();
        sb.Append(FormatValue(result));
        if (result is not null)
        {
            sb.Append(" (");
            sb.Append(result.GetType().Name);
            sb.Append(')');
        }
        return sb.ToString();
    }

    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        char c => $"'{c}'",
        bool b => b ? "true" : "false",
        IDictionary dict => FormatDictionary(dict),
        IEnumerable enumerable when value is not string => FormatEnumerable(enumerable),
        _ => value.ToString() ?? "null",
    };

    static string FormatEnumerable(IEnumerable enumerable)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        var first = true;
        foreach (var item in enumerable)
        {
            if (!first) sb.Append(", ");
            sb.Append(FormatValue(item));
            first = false;
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string FormatDictionary(IDictionary dict)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        var first = true;
        foreach (DictionaryEntry entry in dict)
        {
            if (!first) sb.Append(", ");
            sb.Append(FormatValue(entry.Key));
            sb.Append(": ");
            sb.Append(FormatValue(entry.Value));
            first = false;
        }
        sb.Append('}');
        return sb.ToString();
    }

    static string FormatError(AlderException ex)
    {
        var sb = new StringBuilder();
        sb.Append("Error");
        if (ex.FormattedCode is not null)
        {
            sb.Append(' ');
            sb.Append(ex.FormattedCode);
        }
        sb.Append(": ");
        sb.Append(ex.Message);

        if (ex is AlderExecutionLimitException limit)
        {
            sb.Append(" [Limit: ");
            sb.Append(limit.LimitType);
            sb.Append(" — ");
            sb.Append(limit.ActualValue);
            sb.Append('/');
            sb.Append(limit.LimitValue);
            sb.Append(']');
        }

        return sb.ToString();
    }

    static void AppendTrace(StringBuilder sb, TraceNode node, int indent)
    {
        sb.Append(new string(' ', indent * 2));
        sb.Append(node.NodeKind);
        sb.Append(": ");
        sb.Append(node.Source);

        if (node.ErrorCode is not null)
        {
            sb.Append(" ✗ ");
            sb.Append(node.ErrorCode);
            sb.Append(": ");
            sb.Append(node.ErrorMessage);
        }
        else if (node.Value is not null)
        {
            sb.Append(" → ");
            sb.Append(FormatValue(node.Value));
        }
        sb.AppendLine();

        foreach (var child in node.Children)
        {
            AppendTrace(sb, child, indent + 1);
        }
    }
}
