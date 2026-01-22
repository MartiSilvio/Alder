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

    #region Literals

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

    #endregion

    #region Arithmetic

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

    #endregion

    #region Comparison

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

    #endregion

    #region Logical

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

    #endregion

    #region Unary

    [Test]
    public void Eval_Unary_Negation()
    {
        Assert.That(Eval("-5"), Is.EqualTo(-5L));
        Assert.That(Eval("-3.14"), Is.EqualTo(-3.14));
    }

    #endregion

    #region Strings

    [Test]
    public void Eval_StringConcatenation()
    {
        Assert.That(Eval("\"Hello\" + \" \" + \"World\""), Is.EqualTo("Hello World"));
    }

    [Test]
    public void Eval_InterpolatedString()
    {
        var context = new EvalContext();
        context.Define("name", "World");

        Assert.That(Eval("$\"Hello {name}!\"", context), Is.EqualTo("Hello World!"));
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

    #endregion

    #region Variables and Context

    [Test]
    public void Eval_Variable_FromContext()
    {
        var context = new EvalContext();
        context.Define("x", 10L);

        Assert.That(Eval("x", context), Is.EqualTo(10L));
        Assert.That(Eval("x + 5", context), Is.EqualTo(15L));
    }

    #endregion

    #region Member Access

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
    public void Eval_CaseSensitive_MemberAccess()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["Name"] = "John";
        context.Define("user", obj);

        Assert.That(Eval("user.Name", context), Is.EqualTo("John"));
        Assert.Throws<EvalException>(() => Eval("user.name", context));
    }

    #endregion

    #region Index Access

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

    #endregion

    #region Null Handling

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

    #endregion

    #region Arrays

    [Test]
    public void Eval_ArrayLiteral()
    {
        var result = Eval("[1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1L));
    }

    [Test]
    public void Eval_ArrayLiteral_Multiline()
    {
        var result = Eval(@"[
    ""one"",
    ""two"",
    ""three""
]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo("one"));
        Assert.That(result[1], Is.EqualTo("two"));
        Assert.That(result[2], Is.EqualTo("three"));
    }

    [Test]
    public void Eval_ArrayLiteral_CRLF()
    {
        var result = Eval("[\r\n    \"one\"\r\n]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result![0], Is.EqualTo("one"));
    }

    #endregion

    #region Objects

    [Test]
    public void Eval_AnonymousObject()
    {
        var result = Eval("new { Name = \"John\", Age = 30 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30L));
    }

    #endregion

    #region Ternary

    [Test]
    public void Eval_Ternary_True()
    {
        Assert.That(Eval("true ? 1 : 2"), Is.EqualTo(1L));
    }

    [Test]
    public void Eval_Ternary_False()
    {
        Assert.That(Eval("false ? 1 : 2"), Is.EqualTo(2L));
    }

    [Test]
    public void Eval_Ternary_WithExpression()
    {
        var context = new EvalContext();
        context.Define("x", 10L);
        Assert.That(Eval("x > 5 ? \"big\" : \"small\"", context), Is.EqualTo("big"));
    }

    [Test]
    public void Eval_Ternary_WithArrays()
    {
        var result = Eval("true ? [] : [1, 2, 3]") as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    #endregion

    #region Blocks

    [Test]
    public void Eval_Block_WithReturn()
    {
        var result = Eval("{ var x = 5; return x * 2; }");
        Assert.That(result, Is.EqualTo(10L));
    }

    #endregion

    #region Lambdas and LINQ

    [Test]
    public void Eval_Lambda_Where()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where((x) => x > 2)", context) as List<object?>;
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public void Eval_Lambda_Where_WithoutParens()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2, 3, 4, 5 });

        var result = Eval("numbers.Where(x => x > 2)", context) as List<object?>;
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
    public void Eval_Lambda_Select_WithoutParens()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L });

        var result = Eval("numbers.Select(x => x * 2)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { 2L, 4L, 6L }));
    }

    [Test]
    public void Eval_Lambda_Select_WithMemberAccess()
    {
        var context = new EvalContext();
        context.Define("items", new List<object?> {
            new { Name = "Alice" },
            new { Name = "Bob" }
        });

        var result = Eval("items.Select(x => x.Name)", context) as List<object?>;
        Assert.That(result, Is.EqualTo(new List<object?> { "Alice", "Bob" }));
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

    #endregion

    #region Built-in Proxies

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

    #endregion

    #region Dictionary Merge

    [Test]
    public void Eval_DictionaryMerge_WithPlusOperator()
    {
        var result = Eval("new { A = 1 } + new { B = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_DictionaryMerge_RightOverwritesLeft()
    {
        var result = Eval("new { A = 1, B = 2 } + new { B = 3 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(3L));
    }

    [Test]
    public void Eval_DictionaryMerge_WithVariables()
    {
        var context = new EvalContext();
        IDictionary<string, object?> left = new ExpandoObject();
        left["Name"] = "John";
        IDictionary<string, object?> right = new ExpandoObject();
        right["Age"] = 30;
        context.Define("left", left);
        context.Define("right", right);

        var result = Eval("left + right", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    [Test]
    public void Eval_DictionaryMerge_ChainedOperations()
    {
        var result = Eval("new { A = 1 } + new { B = 2 } + new { C = 3 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
        Assert.That(result["C"], Is.EqualTo(3L));
    }

    [Test]
    public void Eval_DictionaryMerge_CaseSensitive_KeepsBothKeys()
    {
        // Default is case-sensitive, so 'a' and 'A' are different keys
        var result = Eval("new { a = 1 } + new { A = 2 }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result["a"], Is.EqualTo(1L));
        Assert.That(result["A"], Is.EqualTo(2L));
    }

    #endregion

    #region Typed Object Merge

    [Test]
    public void Eval_TypedObjectMerge_WithPlusOperator()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("person + new { City = \"NYC\" }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_TypedObjectMerge_RightOverwritesLeft()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("person + new { Age = 40 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(40L)); // Right side overwrites
    }

    [Test]
    public void Eval_TypedObjectMerge_WithBlock()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval(@"{
            var p = person;
            return p + new { City = ""NYC"", Country = ""USA"" };
        }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    [Test]
    public void Eval_TypedObjectMerge_InSelect()
    {
        var context = new EvalContext();
        context.Define("people", new List<object?>
        {
            new TestPerson { Name = "John", Age = 30 },
            new TestPerson { Name = "Jane", Age = 25 }
        });

        var result = Eval("people.Select(p => p + new { Status = \"Active\" })", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(2));

        var first = result![0] as IDictionary<string, object?>;
        Assert.That(first, Is.Not.Null);
        Assert.That(first!["Name"], Is.EqualTo("John"));
        Assert.That(first["Status"], Is.EqualTo("Active"));

        var second = result[1] as IDictionary<string, object?>;
        Assert.That(second, Is.Not.Null);
        Assert.That(second!["Name"], Is.EqualTo("Jane"));
        Assert.That(second["Status"], Is.EqualTo("Active"));
    }

    [Test]
    public void Eval_TypedObjectMerge_ChainedWithDictionary()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("person + new { City = \"NYC\" } + new { Country = \"USA\" }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    [Test]
    public void Eval_TypedObjectMerge_WithNestedObject()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });
        context.Define("address", new TestAddress { City = "NYC", Country = "USA" });

        var result = Eval("person + new { Address = address }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        var addr = result["Address"] as TestAddress;
        Assert.That(addr, Is.Not.Null);
        Assert.That(addr!.City, Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_DictionaryPlusTypedObject()
    {
        var context = new EvalContext();
        IDictionary<string, object?> dict = new ExpandoObject();
        dict["Extra"] = "Value";
        context.Define("dict", dict);
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("dict + person", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Extra"], Is.EqualTo("Value"));
        Assert.That(result["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
    }

    [Test]
    public void Eval_TwoTypedObjects()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });
        context.Define("address", new TestAddress { City = "NYC", Country = "USA" });

        var result = Eval("person + address", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
        Assert.That(result["Country"], Is.EqualTo("USA"));
    }

    #endregion

    #region If Statements

    [Test]
    public void Eval_IfStatement_EarlyReturn_WhenConditionTrue()
    {
        var result = Eval("{ var x = null; if (x == null) return 42; return 0; }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_IfStatement_EarlyReturn_WhenConditionFalse()
    {
        var result = Eval("{ var x = 10; if (x == null) return 42; return 0; }");
        Assert.That(result, Is.EqualTo(0L));
    }

    [Test]
    public void Eval_IfStatement_WithElse_TakesThenBranch()
    {
        var result = Eval("{ var x = true; if (x) return 1; else return 2; }");
        Assert.That(result, Is.EqualTo(1L));
    }

    [Test]
    public void Eval_IfStatement_WithElse_TakesElseBranch()
    {
        var result = Eval("{ var x = false; if (x) return 1; else return 2; }");
        Assert.That(result, Is.EqualTo(2L));
    }

    [Test]
    public void Eval_IfStatement_WithBlock()
    {
        var result = Eval(@"{
            var x = 5;
            if (x > 3) {
                var y = x * 2;
                return y;
            }
            return 0;
        }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_IfStatement_WithElseBlock()
    {
        var result = Eval(@"{
            var x = 1;
            if (x > 3) {
                return 100;
            } else {
                return 200;
            }
        }");
        Assert.That(result, Is.EqualTo(200L));
    }

    [Test]
    public void Eval_IfStatement_NestedIf()
    {
        var result = Eval(@"{
            var x = 10;
            var y = 20;
            if (x > 5) {
                if (y > 15) return 1;
                else return 2;
            }
            return 3;
        }");
        Assert.That(result, Is.EqualTo(1L));
    }

    [Test]
    public void Eval_IfStatement_ElseIf()
    {
        var result = Eval(@"{
            var x = 2;
            if (x == 1) return ""one"";
            else if (x == 2) return ""two"";
            else return ""other"";
        }");
        Assert.That(result, Is.EqualTo("two"));
    }

    [Test]
    public void Eval_IfStatement_NoReturn_ContinuesExecution()
    {
        // When if doesn't return, execution continues to the next statement
        var result = Eval(@"{
            var x = 5;
            if (x < 3) {
                return 0;
            }
            return x * 2;
        }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_IfStatement_NullCheck_Pattern()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval(@"{
            var p = person;
            if (p == null) return null;
            return p + new { Extra = ""test"" };
        }", context) as IDictionary<string, object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Extra"], Is.EqualTo("test"));
    }

    #endregion

    #region Null Coalescing Assignment

    [Test]
    public void Eval_NullCoalesceAssign_AssignsWhenNull()
    {
        var result = Eval(@"{
            var x = null;
            x ??= 42;
            return x;
        }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_KeepsValueWhenNotNull()
    {
        var result = Eval(@"{
            var x = 10;
            x ??= 42;
            return x;
        }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_ReturnsAssignedValue()
    {
        var result = Eval(@"{
            var x = null;
            return x ??= 42;
        }");
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_ReturnsExistingValue()
    {
        var result = Eval(@"{
            var x = ""hello"";
            return x ??= ""world"";
        }");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Eval_NullCoalesceAssign_WithExpression()
    {
        var result = Eval(@"{
            var x = null;
            x ??= 5 + 5;
            return x;
        }");
        Assert.That(result, Is.EqualTo(10L));
    }

    [Test]
    public void Eval_NullCoalesceAssign_InIfStatement()
    {
        var result = Eval(@"{
            var x = null;
            if (true) {
                x ??= 100;
            }
            return x;
        }");
        Assert.That(result, Is.EqualTo(100L));
    }

    #endregion

    #region Spread Operator

    [Test]
    public void Eval_ArraySpread_SingleArray()
    {
        var context = new EvalContext();
        context.Define("arr", new List<object?> { 1L, 2L, 3L });

        var result = Eval("[...arr]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo(1L));
        Assert.That(result[1], Is.EqualTo(2L));
        Assert.That(result[2], Is.EqualTo(3L));
    }

    [Test]
    public void Eval_ArraySpread_WithOtherElements()
    {
        var context = new EvalContext();
        context.Define("arr", new List<object?> { 2L, 3L });

        var result = Eval("[1, ...arr, 4]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result![0], Is.EqualTo(1L));
        Assert.That(result[1], Is.EqualTo(2L));
        Assert.That(result[2], Is.EqualTo(3L));
        Assert.That(result[3], Is.EqualTo(4L));
    }

    [Test]
    public void Eval_ArraySpread_MultipleArrays()
    {
        var context = new EvalContext();
        context.Define("arr1", new List<object?> { 1L, 2L });
        context.Define("arr2", new List<object?> { 3L, 4L });

        var result = Eval("[...arr1, ...arr2]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(4));
        Assert.That(result![0], Is.EqualTo(1L));
        Assert.That(result[1], Is.EqualTo(2L));
        Assert.That(result[2], Is.EqualTo(3L));
        Assert.That(result[3], Is.EqualTo(4L));
    }

    [Test]
    public void Eval_ArraySpread_WithNativeArray()
    {
        var context = new EvalContext();
        context.Define("arr", new[] { "a", "b", "c" });

        var result = Eval("[...arr]", context) as List<object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result![0], Is.EqualTo("a"));
    }

    [Test]
    public void Eval_ObjectSpread_SingleObject()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        context.Define("obj", obj);

        var result = Eval("new { ...obj }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_ObjectSpread_WithOtherProperties()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        context.Define("obj", obj);

        var result = Eval("new { ...obj, B = 2 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_ObjectSpread_OverridesEarlierProperties()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1L;
        obj["B"] = 2L;
        context.Define("obj", obj);

        var result = Eval("new { ...obj, B = 99 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(99L));
    }

    [Test]
    public void Eval_ObjectSpread_MultipleObjects()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 2L;
        context.Define("obj1", obj1);
        context.Define("obj2", obj2);

        var result = Eval("new { ...obj1, ...obj2 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(2L));
    }

    [Test]
    public void Eval_ObjectSpread_FromTypedObject()
    {
        var context = new EvalContext();
        context.Define("person", new TestPerson { Name = "John", Age = 30 });

        var result = Eval("new { ...person, City = \"NYC\" }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["Name"], Is.EqualTo("John"));
        Assert.That(result["Age"], Is.EqualTo(30));
        Assert.That(result["City"], Is.EqualTo("NYC"));
    }

    [Test]
    public void Eval_ObjectSpread_LaterSpreadOverridesEarlier()
    {
        var context = new EvalContext();
        IDictionary<string, object?> obj1 = new ExpandoObject();
        obj1["A"] = 1L;
        obj1["B"] = 2L;
        IDictionary<string, object?> obj2 = new ExpandoObject();
        obj2["B"] = 99L;
        obj2["C"] = 3L;
        context.Define("obj1", obj1);
        context.Define("obj2", obj2);

        var result = Eval("new { ...obj1, ...obj2 }", context) as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1L));
        Assert.That(result["B"], Is.EqualTo(99L)); // obj2 overwrites
        Assert.That(result["C"], Is.EqualTo(3L));
    }

    #endregion

    #region Helpers

    private static IDictionary<string, object?> CreateItem(string name, double price)
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

    #endregion
}
