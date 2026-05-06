using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Alder.Generators.Tests;

internal static class GeneratedContractSnapshotHelper
{
    internal static void AssertMatchesSnapshot(string snapshotName, string actual)
    {
        var snapshotPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Snapshots", snapshotName);
        Assert.That(File.Exists(snapshotPath), Is.True, $"Missing snapshot file: {snapshotPath}");

        var expected = NormalizeNewlines(File.ReadAllText(snapshotPath)).TrimEnd();
        var normalizedActual = NormalizeNewlines(actual).TrimEnd();

        Assert.That(normalizedActual, Is.EqualTo(expected),
            $"Snapshot '{snapshotName}' does not match.\nActual:\n{normalizedActual}");
    }

    internal static string BuildContextContractSnapshot(string generatedSource, string contextClassName)
    {
        var classSource = ExtractClass(generatedSource, $"partial class {contextClassName}");
        var builder = new StringBuilder();

        builder.AppendLine($"Context: {contextClassName}");
        builder.AppendLine($"Default singleton: {Contains(classSource, $"public static {contextClassName} Default {{ get; }} = new();")}");
        builder.AppendLine("Metadata entries:");
        foreach (var entry in ExtractEntries(classSource, "s_metadata"))
            builder.AppendLine($"- {entry}");

        builder.AppendLine("Generic static dispatch:");
        foreach (var entry in ExtractEntries(classSource, "s_genericStaticDispatch"))
            builder.AppendLine($"- {entry}");

        builder.AppendLine("Overrides:");
        if (Contains(classSource, "GetTypeMetadata()"))
            builder.AppendLine("- GetTypeMetadata");
        if (Contains(classSource, "GetGenericStaticDispatch()"))
            builder.AppendLine("- GetGenericStaticDispatch");

        builder.AppendLine("Task.FromResult roots:");
        foreach (Match match in Regex.Matches(classSource, @"requestedType == typeof\(([^)]+)\)"))
            builder.AppendLine($"- {match.Groups[1].Value}");

        return builder.ToString().TrimEnd();
    }

    internal static string BuildTypedDispatchContractSnapshot(string generatedSource, string metadataClassName)
    {
        var classSource = ExtractClass(generatedSource, $"file sealed class {metadataClassName}");
        var builder = new StringBuilder();

        builder.AppendLine($"Typed dispatch: {metadataClassName}");
        builder.AppendLine($"Type: {MatchValue(classSource, @"Type => typeof\(([^)]+)\);")}");
        AppendNamedMembers(builder, classSource, "TryGet", "TryGet members");
        AppendNamedMembers(builder, classSource, "TrySet", "TrySet members");
        AppendNamedMembers(builder, classSource, "TryGetStatic", "TryGetStatic members");
        AppendIndexers(builder, classSource, "TryGetIndex", "TryGetIndex key types");
        AppendIndexers(builder, classSource, "TrySetIndex", "TrySetIndex key types");
        AppendConstructorArities(builder, classSource);
        AppendInvocationContract(builder, classSource, "TryInvoke", "TryInvoke");
        AppendInvocationContract(builder, classSource, "TryInvokeStatic", "TryInvokeStatic");

        return builder.ToString().TrimEnd();
    }

    private static void AppendNamedMembers(StringBuilder builder, string classSource, string methodName, string heading)
    {
        builder.AppendLine($"{heading}:");
        var methodSource = ExtractOverrideMethod(classSource, methodName);
        foreach (Match match in Regex.Matches(methodSource, "case \\\"([^\\\"]+)\\\":"))
            builder.AppendLine($"- {match.Groups[1].Value}");
    }

    private static void AppendIndexers(StringBuilder builder, string classSource, string methodName, string heading)
    {
        builder.AppendLine($"{heading}:");
        var methodSource = ExtractOverrideMethod(classSource, methodName);
        foreach (Match match in Regex.Matches(methodSource, @"if \(key is ([^)]+)\)"))
            builder.AppendLine($"- {match.Groups[1].Value}");
    }

    private static void AppendConstructorArities(StringBuilder builder, string classSource)
    {
        builder.AppendLine("TryCreate arities:");
        var methodSource = ExtractOverrideMethod(classSource, "TryCreate");
        foreach (Match match in Regex.Matches(methodSource, @"case (\d+):"))
            builder.AppendLine($"- {match.Groups[1].Value}");
    }

    private static void AppendInvocationContract(StringBuilder builder, string classSource, string methodName, string heading)
    {
        builder.AppendLine($"{heading}:");
        var methodSource = ExtractOverrideMethod(classSource, methodName);
        foreach (var methodCase in ExtractSwitchCases(methodSource))
        {
            var details = new List<string>();
            var fixedArities = Regex.Matches(methodCase.Body, @"case (\d+):")
                .Select(m => m.Groups[1].Value)
                .ToArray();

            if (fixedArities.Length > 0)
                details.Add($"arities [{string.Join(", ", fixedArities)}]");

            var paramsMatch = Regex.Match(methodCase.Body, @"args\.Length >= (\d+)");
            if (paramsMatch.Success)
                details.Add($"params >= {paramsMatch.Groups[1].Value}");

            var outSlots = Regex.Matches(methodCase.Body, @"args\[(\d+)\] = __out\d+;")
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToArray();

            if (outSlots.Length > 0)
                details.Add($"out [{string.Join(", ", outSlots)}]");

            builder.AppendLine($"- {methodCase.Name}: {(details.Count == 0 ? "direct" : string.Join("; ", details))}");
        }
    }

    private static IReadOnlyList<string> ExtractEntries(string classSource, string fieldName)
    {
        var fieldSource = ExtractFieldInitializer(classSource, fieldName);
        return Regex.Matches(fieldSource, @"new ([A-Za-z0-9_]+)\(\),")
            .Select(m => m.Groups[1].Value)
            .ToArray();
    }

    private static string ExtractFieldInitializer(string source, string fieldName)
    {
        var marker = $"private static readonly global::";
        var fieldIndex = source.IndexOf(fieldName, StringComparison.Ordinal);
        if (fieldIndex < 0)
            return string.Empty;

        var start = source.LastIndexOf(marker, fieldIndex, StringComparison.Ordinal);
        if (start < 0)
            start = fieldIndex;

        var open = source.IndexOf('[', fieldIndex);
        if (open < 0)
            return string.Empty;

        var close = source.IndexOf("];", open, StringComparison.Ordinal);
        if (close < 0)
            return string.Empty;

        return source.Substring(start, close - start + 2);
    }

    private static string ExtractOverrideMethod(string source, string methodName)
    {
        var patterns = new[]
        {
            $"public override bool {methodName}(",
            $"public override global::System.Collections.Generic.IReadOnlyList<global::Alder.Aot.TypedDispatch> {methodName}(",
            $"public override global::System.Collections.Generic.IReadOnlyList<global::Alder.Aot.GenericStaticDispatch>? {methodName}(",
        };

        foreach (var pattern in patterns)
        {
            var method = ExtractBlockFromMarker(source, pattern);
            if (!string.IsNullOrEmpty(method))
                return method;
        }

        return string.Empty;
    }

    private static string ExtractClass(string source, string classMarker)
        => ExtractBlockFromMarker(source, classMarker);

    private static string ExtractBlockFromMarker(string source, string marker)
    {
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
            return string.Empty;

        var openBrace = source.IndexOf('{', index);
        if (openBrace < 0)
            return string.Empty;

        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(index, i - index + 1);
        }

        return source.Substring(index);
    }

    private static IReadOnlyList<(string Name, string Body)> ExtractSwitchCases(string methodSource)
    {
        var results = new List<(string Name, string Body)>();
        var matches = Regex.Matches(methodSource, "case \\\"([^\\\"]+)\\\":");

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : methodSource.Length;
            results.Add((matches[i].Groups[1].Value, methodSource.Substring(start, end - start)));
        }

        return results;
    }

    private static string MatchValue(string source, string pattern)
    {
        var match = Regex.Match(source, pattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static bool Contains(string source, string value)
        => source.Contains(value, StringComparison.Ordinal);

    private static string NormalizeNewlines(string text)
        => text.Replace("\r\n", "\n");
}
