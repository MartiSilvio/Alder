using System.Dynamic;

namespace CsEval.Test;

/// <summary>
/// Static test utilities.
/// </summary>
public static class TestHelpers
{
    public static IDictionary<string, object?> CreateItem(string name, double price)
    {
        IDictionary<string, object?> item = new ExpandoObject();
        item["Name"] = name;
        item["Price"] = price;
        return item;
    }
}
