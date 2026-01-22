using System.Dynamic;
using CsEval.Evaluation;
using CsEval.Parsing;
using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class EvaluatorTests
{
    private static object? Eval(string source, EvalContext? context = null)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();

        var builtIns = new Dictionary<string, Func<object?[], object?>>(StringComparer.Ordinal);

        var ctx = context ?? new EvalContext();
        ctx.Define("Math", new MathProxy());
        ctx.Define("String", new StringProxy());

        var evaluator = new Evaluator(ctx, builtIns);
        return evaluator.Evaluate(ast);
    }

    [Test]
    public void Eval_Number_ReturnsNumber()
    {
        var result = Eval("42");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_String_ReturnsString()
    {
        var result = Eval("\"hello\"");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_Boolean_ReturnsBoolean()
    {
        Assert.That(Eval("true"), Is.EqualTo(true));
        Assert.That(Eval("false"), Is.EqualTo(false));
    }

    [Test]
    public void Eval_Null_ReturnsNull()
    {
        Assert.That(Eval("null"), Is.Null);
    }

    [Test]
    public void Eval_Arithmetic_Addition()
    {
        Assert.That(Eval("1 + 2"), Is.EqualTo(3L));
        Assert.That(Eval("1.5 + 2.5"), Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_Arithmetic_Subtraction()
    {
        Assert.That(Eval("5 - 3"), Is.EqualTo(2L));
    }

    [Test]
    public void Eval_Arithmetic_Multiplication()
    {
        Assert.That(Eval("3 * 4"), Is.EqualTo(12L));
    }

    [Test]
    public void Eval_Arithmetic_Division()
    {
        Assert.That(Eval("10 / 4"), Is.EqualTo(2.5));
    }

    [Test]
    public void Eval_Arithmetic_Modulo()
    {
        Assert.That(Eval("10 % 3"), Is.EqualTo(1L));
    }

    [Test]
    public void Eval_Arithmetic_Precedence()
    {
        Assert.That(Eval("1 + 2 * 3"), Is.EqualTo(7L));
        Assert.That(Eval("(1 + 2) * 3"), Is.EqualTo(9L));
    }

    [Test]
    public void Eval_Comparison_Equals()
    {
        Assert.That(Eval("1 == 1"), Is.True);
        Assert.That(Eval("1 == 2"), Is.False);
        Assert.That(Eval("\"a\" == \"a\""), Is.True);
    }

    [Test]
    public void Eval_Comparison_NotEquals()
    {
        Assert.That(Eval("1 != 2"), Is.True);
        Assert.That(Eval("1 != 1"), Is.False);
    }

    [Test]
    public void Eval_Comparison_LessThan()
    {
        Assert.That(Eval("1 < 2"), Is.True);
        Assert.That(Eval("2 < 1"), Is.False);
    }

    [Test]
    public void Eval_Comparison_GreaterThan()
    {
        Assert.That(Eval("2 > 1"), Is.True);
        Assert.That(Eval("1 > 2"), Is.False);
    }

    [Test]
    public void Eval_Logical_And()
    {
        Assert.That(Eval("true && true"), Is.True);
        Assert.That(Eval("true && false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Or()
    {
        Assert.That(Eval("true || false"), Is.True);
        Assert.That(Eval("false || false"), Is.False);
    }

    [Test]
    public void Eval_Logical_Not()
    {
        Assert.That(Eval("!true"), Is.False);
        Assert.That(Eval("!false"), Is.True);
    }

    [Test]
    public void Eval_Unary_Negation()
    {
        Assert.That(Eval("-5"), Is.EqualTo(-5L));
        Assert.That(Eval("-3.14"), Is.EqualTo(-3.14));
    }

    [Test]
    public void Eval_StringConcatenation()
    {
        Assert.That(Eval("\"Hello\" + \" \" + \"World\""), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Eval_Variable_FromContext()
    {
        var context = new EvalContext();
        context.Define("x", 10L);

        Assert.That(Eval("x", context), Is.EqualTo(10L));
        Assert.That(Eval("x + 5", context), Is.EqualTo(15L));
    }

    [Test]
    public void Eval_MemberAccess_OnExpandoObject()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        obj["Age"] = 30;
        context.Define("user", obj);

        Assert.That(Eval("user.Name", context), Is.EqualTo("John"));
        Assert.That(Eval("user.Age", context), Is.EqualTo(30));
    }

    [Test]
    public void Eval_IndexAccess_OnList()
    {
        var context = new EvalContext();
        context.Define("arr", new List<object?> { 1, 2, 3 });

        Assert.That(Eval("arr[0]", context), Is.EqualTo(1));
        Assert.That(Eval("arr[2]", context), Is.EqualTo(3));
    }

    [Test]
    public void Eval_IndexAccess_OnDictionary()
    {
        var context = new EvalContext();
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["key"] = "value";
        context.Define("dict", dict);

        Assert.That(Eval("dict[\"key\"]", context), Is.EqualTo("value"));
    }

    [Test]
    public void Eval_NullCoalesce()
    {
        var context = new EvalContext();
        context.Define("x", null);
        context.Define("y", "default");

        Assert.That(Eval("x ?? y", context), Is.EqualTo("default"));
        Assert.That(Eval("y ?? \"other\"", context), Is.EqualTo("default"));
    }

    [Test]
    public void Eval_NullSafeAccess()
    {
        var context = new EvalContext();
        context.Define("obj", null);

        Assert.That(Eval("obj?.Name", context), Is.Null);
    }

    [Test]
    public void Eval_InterpolatedString()
    {
        var context = new EvalContext();
        context.Define("name", "World");

        Assert.That(Eval("$\"Hello {name}!\"", context), Is.EqualTo("Hello World!"));
    }

    [Test]
    public void Eval_ArrayLiteral()
    {
        var result = Eval("[1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1L));
    }

    [Test]
    public void Eval_AnonymousObject()
    {
        var result = Eval("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30L));
    }

    [Test]
    public void Eval_Block_WithReturn()
    {
        var result = Eval("{ var x = 5; return x * 2; }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_Lambda_Where()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where((x) => x > 2)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Eval_Lambda_Select()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L });

        var result = Eval("numbers.Select((x) => x * 2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 2L, 4L, 6L }));
    }

    [Test]
    public void Eval_Lambda_Aggregate()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L, 4L });

        var result = Eval("numbers.Aggregate((acc, x) => acc + x, 0)", context);
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_MathProxy_Abs()
    {
        var result = Eval("Math.Abs(-5)");
        Assert.That(result, Is.EqualTo(5.0));
    }

    [Test]
    public void Eval_MathProxy_Sqrt()
    {
        var result = Eval("Math.Sqrt(16)");
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void Eval_StringMethod_ToLower()
    {
        var context = new EvalContext();
        context.Define("s", "HELLO");

        var result = Eval("s.ToLower()", context);
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_StringMethod_ToUpper()
    {
        var context = new EvalContext();
        context.Define("s", "hello");

        var result = Eval("s.ToUpper()", context);
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Eval_StringProperty_Length()
    {
        var context = new EvalContext();
        context.Define("s", "hello");

        var result = Eval("s.Length", context);
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Eval_ChainedMemberAccess()
    {
        var context = new EvalContext();
        IDictionary<string, object?> user = new ExpandoObject();
        IDictionary<string, object?> address = new ExpandoObject();
        address["City"] = "New York";
        user["Address"] = address;
        context.Define("user", user);

        Assert.That(Eval("user.Address.City", context), Is.EqualTo("New York"));
    }

    [Test]
    public void Eval_ComplexExpression_WithLinq()
    {
        var context = new EvalContext();
        context.Define("items", new List<object?>
        {
            CreateItem("Apple", 1.5),
            CreateItem("Banana", 0.75),
            CreateItem("Orange", 2.0)
        });

        var result = Eval("items.Where((x) => x.Price > 1).Select((x) => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Apple", "Orange" }));
    }

    [Test]
    public void Eval_CaseSensitive_MemberAccess()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        context.Define("user", obj);

        Assert.That(Eval("user.Name", context), Is.EqualTo("John"));
        Assert.Throws<EvalException>(() => Eval("user.name", context));
    }

    private static IDictionary<string, object?> CreateItem(string name, double price)
    {
        IDictionary<string, object?> item = new ExpandoObject();
        item["Name"] = name;
        item["Price"] = price;
        return item;
    }
}