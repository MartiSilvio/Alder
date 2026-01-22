using System.Dynamic;
using CsEval.Evaluation;
using CsEval.Parsing;

namespace CsEval.Test;

public abstract class EvaluatorTestBase
{
    protected static object? Eval(string source, EvalContext? context = null)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();

        var builtIns = new Dictionary<string, Func<object?[], object?>>(StringComparer.Ordinal);

        var ctx = context ?? new EvalContext();
        ctx.Define("Math", new MathProxy());
        ctx.Define("String", new StringProxy());

        var evaluator = new CsEval.Evaluation.Evaluator(ctx, builtIns);
        return evaluator.Evaluate(ast);
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
