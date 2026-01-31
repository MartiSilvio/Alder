using System.Dynamic;
using System.Linq;
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
        .AddImports("System", "System.Collections.Generic", "System.Linq");

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
}
