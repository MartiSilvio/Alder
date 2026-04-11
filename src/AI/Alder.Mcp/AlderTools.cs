using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Alder;
using Alder.Tracing;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Alder.Mcp;

[McpServerToolType]
internal sealed class AlderTools
{
    private const int MaxFormatDepth = 5;
    private const int MaxCollectionItems = 100;
    private const int MaxTraceDepth = 10;

    [McpServerTool(Name = "evaluate", Title = "Evaluate C# Expression",
        Destructive = false, ReadOnly = false, OpenWorld = false, Idempotent = false),
     Description(
        "Execute a C# expression and return its result. " +
        "Supports: arithmetic, string interpolation, LINQ (Select, Where, Aggregate, etc.), lambdas, ternary (?:), " +
        "null-coalescing (??), pattern matching (is), object creation (new), method calls on .NET types (Math.Round, string.Join, etc.), " +
        "multi-statement blocks (semicolons between statements, last expression is the return value), " +
        "and variable injection from the 'variables' parameter. " +
        "Does NOT support: class/struct/enum/record declarations, or using directives. " +
        "Pre-imported: System, System.Collections.Generic, System.Linq, System.Threading.Tasks, " +
        "System.Text, System.Text.RegularExpressions, System.Text.Json. " +
        "Execution limits: 100M statements, 100M loop iterations, 30s timeout. " +
        "Output format: '<value> (<TypeName>)' e.g. '42 (Int32)', '\"hello\" (String)', '[1, 2, 3] (List<Int32>)'. " +
        "Null results return 'null'. Collections truncate at 100 elements with '...'.")]
    public static CallToolResult Evaluate(
        AlderEngine engine,
        [Description(
            "The C# expression to evaluate. " +
            "Single expression: '2 + 2', 'Math.Sqrt(144)', 'new[] {1,2,3}.Where(x => x > 1).Sum()'. " +
            "Multi-statement: 'var x = 10; var y = 20; return x + y;' (use explicit return). " +
            "String interpolation: '$\"Hello {name}, you are {age} years old\"'. " +
            "LINQ: 'items.Where(x => x > 0).Select(x => x * 2).ToList()'. " +
            "Regex: 'Regex.IsMatch(input, @\"^\\d+$\")'.")]
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
        if (string.IsNullOrWhiteSpace(expression))
            return ErrorResult("'expression' is required and must not be empty.");

        if (!TryParseVariables(variables, out var vars, out var parseError))
            return ErrorResult(parseError!);

        try
        {
            var result = engine.Evaluate(expression, vars, cancellationToken);
            return OkResult(FormatResult(result));
        }
        catch (OperationCanceledException)
        {
            return ErrorResult("Evaluation timed out or was cancelled.");
        }
        catch (AlderException ex)
        {
            return ErrorResult(FormatError(ex));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Runtime error ({ex.GetType().Name}): {ex.Message}");
        }
    }

    [McpServerTool(Name = "validate", Title = "Validate C# Expression",
        Destructive = false, ReadOnly = true, OpenWorld = false, Idempotent = true),
     Description(
        "Check whether a C# expression is syntactically and semantically valid WITHOUT executing it. " +
        "Use this to verify an expression before calling 'evaluate', or to diagnose syntax errors. " +
        "Returns 'Valid' if the expression would evaluate successfully, or a list of diagnostics with " +
        "C#-compatible error codes and messages (e.g. 'CS0103: The name 'foo' does not exist in the current context'). " +
        "Note: cannot validate expressions that reference injected variables because those variables are unknown at validation time.")]
    public static CallToolResult Validate(
        AlderEngine engine,
        [Description(
            "The C# expression to validate. Same syntax rules as the 'evaluate' tool. " +
            "Example: 'Math.Sqrt(x) > threshold' (reports CS0103 if 'x' or 'threshold' are not in scope).")]
        string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return ErrorResult("'expression' is required and must not be empty.");

        if (engine.TryValidate(expression, out var diagnostics))
            return OkResult("Valid");

        var sb = new StringBuilder();
        sb.AppendLine("Invalid:");
        foreach (var d in diagnostics)
        {
            sb.Append(d.FormattedCode ?? "error");
            sb.Append(": ");
            sb.AppendLine(d.Message);
        }
        return OkResult(sb.ToString().TrimEnd());
    }

    [McpServerTool(Name = "evaluate_with_trace", Title = "Evaluate C# Expression with Trace",
        Destructive = false, ReadOnly = false, OpenWorld = false, Idempotent = false),
     Description(
        "Execute a C# expression and return both the result AND a step-by-step evaluation trace showing how " +
        "each sub-expression was computed. Use this ONLY when the user needs to understand the evaluation process, " +
        "debug unexpected results, or see intermediate values. For simple evaluations, prefer 'evaluate' instead. " +
        "Output: 'Result: <value>' followed by an indented trace tree where each line shows " +
        "'<NodeKind>: <source> → <value>' (e.g. 'BinaryExpression: 2 + 3 → 5'). " +
        "Failed nodes show '✗ <ErrorCode>: <message>'. Nested sub-expressions are indented under their parent.")]
    public static CallToolResult EvaluateWithTrace(
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
        if (string.IsNullOrWhiteSpace(expression))
            return ErrorResult("'expression' is required and must not be empty.");

        if (!TryParseVariables(variables, out var vars, out var parseError))
            return ErrorResult(parseError!);

        try
        {
            var trace = engine.EvaluateWithTrace(expression, vars, cancellationToken);
            var sb = new StringBuilder();
            sb.Append("Result: ");
            sb.AppendLine(FormatValue(trace.Result));
            sb.AppendLine();
            sb.AppendLine("Trace:");
            AppendTrace(sb, trace.Tree, indent: 0);
            return OkResult(sb.ToString().TrimEnd());
        }
        catch (OperationCanceledException)
        {
            return ErrorResult("Evaluation timed out or was cancelled.");
        }
        catch (AlderException ex)
        {
            return ErrorResult(FormatError(ex));
        }
        catch (Exception ex)
        {
            return ErrorResult($"Runtime error ({ex.GetType().Name}): {ex.Message}");
        }
    }

    [McpServerTool(Name = "get_info", Title = "Get Engine Configuration",
        Destructive = false, ReadOnly = true, OpenWorld = false, Idempotent = true),
     Description(
        "Returns the active engine configuration: sandbox level and permitted operations, language mode, " +
        "execution limits, and available namespaces. Use this to discover what operations and types are " +
        "available before crafting expressions, or when an expression fails due to a missing namespace.")]
    public static CallToolResult GetInfo(McpServerConfig config)
    {
        var sb = new StringBuilder();

        sb.Append("Sandbox: ");
        sb.AppendLine(config.SandboxPreset);
        var s = config.Sandbox;
        sb.Append("  Method calls: "); sb.AppendLine(s.AllowMethodCalls ? "allowed" : "denied");
        sb.Append("  Object construction (new): "); sb.AppendLine(s.AllowConstruction ? "allowed" : "denied");
        sb.Append("  Property read: "); sb.AppendLine(s.AllowPropertyRead ? "allowed" : "denied");
        sb.Append("  Property write: "); sb.AppendLine(s.AllowPropertySet ? "allowed" : "denied");
        sb.Append("  Variable assignment: "); sb.AppendLine(s.AllowAssignment ? "allowed" : "denied");

        sb.AppendLine();
        sb.Append("Language mode: ");
        sb.AppendLine(config.LanguageMode.ToString().ToLowerInvariant());

        sb.AppendLine();
        sb.AppendLine("Execution limits:");
        sb.Append("  Max statements: ");
        sb.AppendLine(config.MaxStatements is { } ms ? ms.ToString("N0") : "unlimited");
        sb.Append("  Max loop iterations: ");
        sb.AppendLine(config.MaxLoopIterations is { } ml ? ml.ToString("N0") : "unlimited");
        sb.Append("  Timeout: ");
        sb.AppendLine(config.MaxTimeout is { } mt ? $"{mt.TotalSeconds}s" : "unlimited");

        sb.AppendLine();
        sb.AppendLine("Pre-imported namespaces (short names available):");
        foreach (var ns in new[]
        {
            "System", "System.Collections.Generic", "System.Linq", "System.Threading.Tasks",
            "System.Text", "System.Text.RegularExpressions", "System.Text.Json",
            "System.Numerics", "System.Globalization",
        })
        {
            sb.Append("  ");
            sb.AppendLine(ns);
        }

        sb.AppendLine();
        sb.AppendLine("Available by fully-qualified name (no short-name import):");
        sb.AppendLine("  System.Security.Cryptography (SHA256, Aes, RSA, RandomNumberGenerator, ...)");
        sb.AppendLine("  Add via --namespace System.Security.Cryptography for short-name access.");

        if (config.ExtraNamespaces.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Extra namespaces:");
            foreach (var ns in config.ExtraNamespaces)
            {
                sb.Append("  ");
                sb.AppendLine(ns);
            }
        }

        if (config.ExtraAssemblies.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Extra assemblies:");
            foreach (var path in config.ExtraAssemblies)
            {
                sb.Append("  ");
                sb.AppendLine(path);
            }
        }

        return OkResult(sb.ToString().TrimEnd());
    }

    static CallToolResult OkResult(string text) => new()
    {
        Content = [new TextContentBlock { Text = text }],
    };

    static CallToolResult ErrorResult(string text) => new()
    {
        Content = [new TextContentBlock { Text = text }],
        IsError = true,
    };

    static bool TryParseVariables(string? json, out IDictionary<string, object?>? vars, out string? error)
    {
        vars = null;
        error = null;

        if (string.IsNullOrWhiteSpace(json))
            return true;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            error = $"Invalid JSON in 'variables': {ex.Message}. " +
                    """Expected a JSON object like {"x": 5, "name": "Alice"}.""";
            return false;
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = $"'variables' must be a JSON object, got {doc.RootElement.ValueKind}. " +
                        """Expected format: {"x": 5, "name": "Alice"}.""";
                return false;
            }

            var result = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                result[prop.Name] = ConvertJsonElement(prop.Value);

            vars = result;
            return true;
        }
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
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
        _ => element.GetRawText(),
    };

    static string FormatResult(object? result)
    {
        if (result is null)
            return "null";
        var sb = new StringBuilder();
        sb.Append(FormatValue(result));
        sb.Append(" (");
        sb.Append(GetFriendlyTypeName(result.GetType()));
        sb.Append(')');
        return sb.ToString();
    }

    static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;
        var name = type.GetGenericTypeDefinition().Name;
        name = name[..name.IndexOf('`')];
        var args = type.GetGenericArguments();
        return $"{name}<{string.Join(", ", args.Select(GetFriendlyTypeName))}>";
    }

    static string FormatValue(object? value, int depth = 0) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        char c => $"'{c}'",
        bool b => b ? "true" : "false",
        IDictionary dict when depth < MaxFormatDepth => FormatDictionary(dict, depth),
        IDictionary => "{...}",
        IEnumerable enumerable when depth < MaxFormatDepth => FormatEnumerable(enumerable, depth),
        IEnumerable => "[...]",
        _ => value.ToString() ?? "null",
    };

    static string FormatEnumerable(IEnumerable enumerable, int depth)
    {
        var sb = new StringBuilder();
        sb.Append('[');
        var first = true;
        var count = 0;
        foreach (var item in enumerable)
        {
            if (count >= MaxCollectionItems)
            {
                if (!first) sb.Append(", ");
                sb.Append("...");
                break;
            }
            if (!first) sb.Append(", ");
            sb.Append(FormatValue(item, depth + 1));
            first = false;
            count++;
        }
        sb.Append(']');
        return sb.ToString();
    }

    static string FormatDictionary(IDictionary dict, int depth)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        var first = true;
        var count = 0;
        foreach (DictionaryEntry entry in dict)
        {
            if (count >= MaxCollectionItems)
            {
                if (!first) sb.Append(", ");
                sb.Append("...");
                break;
            }
            if (!first) sb.Append(", ");
            sb.Append(FormatValue(entry.Key, depth + 1));
            sb.Append(": ");
            sb.Append(FormatValue(entry.Value, depth + 1));
            first = false;
            count++;
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
            sb.Append(": ");
            sb.Append(limit.ActualValue);
            sb.Append('/');
            sb.Append(limit.LimitValue);
            sb.Append(']');
        }

        return sb.ToString();
    }

    static void AppendTrace(StringBuilder sb, TraceNode node, int indent)
    {
        if (indent >= MaxTraceDepth)
        {
            sb.Append(new string(' ', indent * 2));
            sb.AppendLine("...");
            return;
        }

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
            AppendTrace(sb, child, indent + 1);
    }
}
