using CsEval.Parsing;
using NUnit.Framework;

namespace CsEval.Test;

[TestFixture]
public class ParserTests
{
    private static Expr Parse(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        return parser.Parse();
    }

    [Test]
    public void Parse_Literal_ReturnsLiteralExpr()
    {
        var expr = Parse("42");
        Assert.That(expr, Is.InstanceOf<LiteralExpr>());
        Assert.That(((LiteralExpr)expr).Value, Is.EqualTo(42L));
    }

    [Test]
    public void Parse_BinaryExpression_ReturnsBinaryExpr()
    {
        var expr = Parse("1 + 2");
        Assert.That(expr, Is.InstanceOf<BinaryExpr>());

        var binary = (BinaryExpr)expr;
        Assert.That(binary.Op.Type, Is.EqualTo(TokenType.Plus));
    }

    [Test]
    public void Parse_Precedence_MultiplyBeforeAdd()
    {
        var expr = Parse("1 + 2 * 3");
        Assert.That(expr, Is.InstanceOf<BinaryExpr>());

        var add = (BinaryExpr)expr;
        Assert.That(add.Op.Type, Is.EqualTo(TokenType.Plus));
        Assert.That(add.Right, Is.InstanceOf<BinaryExpr>());

        var multiply = (BinaryExpr)add.Right;
        Assert.That(multiply.Op.Type, Is.EqualTo(TokenType.Star));
    }

    [Test]
    public void Parse_Grouping_OverridesPrecedence()
    {
        var expr = Parse("(1 + 2) * 3");
        Assert.That(expr, Is.InstanceOf<BinaryExpr>());

        var multiply = (BinaryExpr)expr;
        Assert.That(multiply.Op.Type, Is.EqualTo(TokenType.Star));
        Assert.That(multiply.Left, Is.InstanceOf<GroupingExpr>());
    }

    [Test]
    public void Parse_Lambda_SingleParameter()
    {
        var expr = Parse("(x) => x * 2");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Lexeme, Is.EqualTo("x"));
    }

    [Test]
    public void Parse_Lambda_SingleParameterWithoutParens()
    {
        var expr = Parse("x => x * 2");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Lexeme, Is.EqualTo("x"));
    }

    [Test]
    public void Parse_Lambda_SingleParameterWithoutParens_MemberAccess()
    {
        var expr = Parse("d => d.Name");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Lexeme, Is.EqualTo("d"));
        Assert.That(lambda.Body, Is.InstanceOf<MemberAccessExpr>());
    }

    [Test]
    public void Parse_Lambda_WithNestedFunctionCallContainingComma()
    {
        var expr = Parse("items.Where(d => Func(d.A, d.B) == null)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.InstanceOf<LambdaExpr>());
    }

    [Test]
    public void Parse_Lambda_WithMultipleNestedMemberAccess()
    {
        var expr = Parse("items.Where(d => Util.Process(d.First, d.Second) == null)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.InstanceOf<LambdaExpr>());
    }

    [Test]
    public void Parse_Lambda_MultipleParameters()
    {
        var expr = Parse("(a, b) => a + b");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_MemberAccess_ReturnsMemberAccessExpr()
    {
        var expr = Parse("obj.Property");
        Assert.That(expr, Is.InstanceOf<MemberAccessExpr>());

        var access = (MemberAccessExpr)expr;
        Assert.That(access.Name.Lexeme, Is.EqualTo("Property"));
        Assert.That(access.NullSafe, Is.False);
    }

    [Test]
    public void Parse_NullSafeMemberAccess_ReturnsNullSafeExpr()
    {
        var expr = Parse("obj?.Property");
        Assert.That(expr, Is.InstanceOf<MemberAccessExpr>());

        var access = (MemberAccessExpr)expr;
        Assert.That(access.NullSafe, Is.True);
    }

    [Test]
    public void Parse_IndexAccess_ReturnsIndexAccessExpr()
    {
        var expr = Parse("arr[0]");
        Assert.That(expr, Is.InstanceOf<IndexAccessExpr>());
    }

    [Test]
    public void Parse_FunctionCall_ReturnsCallExpr()
    {
        var expr = Parse("func(1, 2, 3)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_MethodCall_ReturnsCallExpr()
    {
        var expr = Parse("obj.Method(x)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Callee, Is.InstanceOf<MemberAccessExpr>());
    }

    [Test]
    public void Parse_ArrayLiteral_ReturnsArrayLiteralExpr()
    {
        var expr = Parse("[1, 2, 3]");
        Assert.That(expr, Is.InstanceOf<ArrayLiteralExpr>());

        var array = (ArrayLiteralExpr)expr;
        Assert.That(array.Elements, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_AnonymousObject_ReturnsNewExpr()
    {
        var expr = Parse("new { Name = \"John\", Age = 30 }");
        Assert.That(expr, Is.InstanceOf<NewExpr>());

        var newExpr = (NewExpr)expr;
        Assert.That(newExpr.Initializer, Is.InstanceOf<ObjectLiteralExpr>());

        var obj = (ObjectLiteralExpr)newExpr.Initializer;
        Assert.That(obj.Properties, Has.Count.EqualTo(2));
    }

    [Test]
    public void Parse_Block_ReturnsBlockExpr()
    {
        var expr = Parse("{ var x = 1; return x + 1; }");
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements, Has.Count.EqualTo(2));
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());
        Assert.That(block.Statements[1], Is.InstanceOf<ReturnExpr>());
    }

    [Test]
    public void Parse_NullCoalesce_ReturnsNullCoalesceExpr()
    {
        var expr = Parse("x ?? y");
        Assert.That(expr, Is.InstanceOf<NullCoalesceExpr>());
    }

    [Test]
    public void Parse_InterpolatedString_ReturnsInterpolatedStringExpr()
    {
        var expr = Parse("$\"Hello {name}!\"");
        Assert.That(expr, Is.InstanceOf<InterpolatedStringExpr>());

        var interp = (InterpolatedStringExpr)expr;
        Assert.That(interp.Parts, Has.Count.EqualTo(3));
    }

    [Test]
    public void Parse_Logical_And_Or()
    {
        var expr = Parse("a && b || c");
        Assert.That(expr, Is.InstanceOf<LogicalExpr>());
    }

    [Test]
    public void Parse_Unary_Negation()
    {
        var expr = Parse("-x");
        Assert.That(expr, Is.InstanceOf<UnaryExpr>());

        var unary = (UnaryExpr)expr;
        Assert.That(unary.Op.Type, Is.EqualTo(TokenType.Minus));
    }

    [Test]
    public void Parse_Unary_Not()
    {
        var expr = Parse("!x");
        Assert.That(expr, Is.InstanceOf<UnaryExpr>());

        var unary = (UnaryExpr)expr;
        Assert.That(unary.Op.Type, Is.EqualTo(TokenType.Bang));
    }
}