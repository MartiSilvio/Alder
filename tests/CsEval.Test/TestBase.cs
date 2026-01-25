using System.Dynamic;
using CsEval.Parsing;

namespace CsEval.Test;

public abstract class TestBase
{
    protected CsEvalEngine CreateEngine(CompilationMode mode)
    {
        return CreateEngine(CsEvalOptions.Default with { CompilationMode = mode });
    }
    
    protected static CsEvalEngine CreateEngine(CsEvalOptions options)
    {
        return new CsEvalEngine(options);
    }
    
    protected static IDictionary<string, object?> CreateItem(string name, double price)
    {
        IDictionary<string, object?> item = new ExpandoObject();
        item["Name"] = name;
        item["Price"] = price;
        return item;
    }

    public class TestPerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public class TestAddress
    {
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
    }
}
