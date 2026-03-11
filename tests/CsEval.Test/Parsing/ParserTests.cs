using CsEval.Parsing;

namespace CsEval.Test.Parsing;

[TestFixture]
public class ParserTests
{
    private static Expr Parse(string source, LanguageMode languageMode = LanguageMode.Extended)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = ExpressionParser.CreateForSubExpression(tokens, languageMode);
        return parser.Parse();
    }

    [Test]
    public void Parse_Literal_ReturnsLiteralExpr()
    {
        var expr = Parse("42");
        Assert.That(expr, Is.InstanceOf<LiteralExpr>());
        Assert.That(((LiteralExpr)expr).Value, Is.EqualTo(42));
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
    public void Parse_IdentifierLessIdentifier_IsComparisonExpression()
    {
        var expr = Parse("a < b");
        Assert.That(expr, Is.InstanceOf<BinaryExpr>());
        var binary = (BinaryExpr)expr;
        Assert.That(binary.Op.Type, Is.EqualTo(TokenType.Less));
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
        Assert.That(multiply.Left, Is.InstanceOf<BinaryExpr>());
        var addition = (BinaryExpr)multiply.Left;
        Assert.That(addition.Op.Type, Is.EqualTo(TokenType.Plus));
    }

    [Test]
    public void Parse_Lambda_SingleParameter()
    {
        var expr = Parse("(x) => x * 2");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("x"));
    }

    [Test]
    public void Parse_Lambda_SingleParameterWithoutParens()
    {
        var expr = Parse("x => x * 2");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("x"));
    }

    [Test]
    public void Parse_Lambda_SingleParameterWithoutParens_MemberAccess()
    {
        var expr = Parse("d => d.Name");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("d"));
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
    public void Parse_TypedLambda_SingleParameter()
    {
        var expr = Parse("(double x) => x * x");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].TypeName, Is.EqualTo("double"));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("x"));
    }

    [Test]
    public void Parse_TypedLambda_MultipleParameters()
    {
        var expr = Parse("(int a, int b) => a + b");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(2));
        Assert.That(lambda.Parameters[0].TypeName, Is.EqualTo("int"));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("a"));
        Assert.That(lambda.Parameters[1].TypeName, Is.EqualTo("int"));
        Assert.That(lambda.Parameters[1].Name.Lexeme, Is.EqualTo("b"));
    }

    [Test]
    public void Parse_TypedLambda_GenericParameter()
    {
        var expr = Parse("(Func<double, double> f) => f(1.0)");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].TypeName, Is.EqualTo("Func<double, double>"));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("f"));
    }

    [Test]
    public void Parse_TypedLambda_MixedGenericAndKeyword()
    {
        var expr = Parse("(Func<double, double> f, double a, double b, int n) => a + b");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(4));
        Assert.That(lambda.Parameters[0].TypeName, Is.EqualTo("Func<double, double>"));
        Assert.That(lambda.Parameters[1].TypeName, Is.EqualTo("double"));
        Assert.That(lambda.Parameters[2].TypeName, Is.EqualTo("double"));
        Assert.That(lambda.Parameters[3].TypeName, Is.EqualTo("int"));
    }

    [Test]
    public void Parse_FuncTypeDecl_TwoGenericArgs()
    {
        var expr = Parse("{ Func<int, int> f = x => x * 2; return f(5); }");
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());

        var decl = (VariableDeclExpr)block.Statements[0];
        Assert.That(decl.DeclaredType!.Value.Lexeme, Is.EqualTo("Func<int, int>"));
        Assert.That(decl.Name.Lexeme, Is.EqualTo("f"));
    }

    [Test]
    public void Parse_FuncTypeDecl_ThreeGenericArgs()
    {
        var expr = Parse("{ Func<int, int, int> fn = (int a, int b) => a + b; return fn(10, 20); }");
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());

        var decl = (VariableDeclExpr)block.Statements[0];
        Assert.That(decl.DeclaredType!.Value.Lexeme, Is.EqualTo("Func<int, int, int>"));
        Assert.That(decl.Name.Lexeme, Is.EqualTo("fn"));
        Assert.That(decl.Initializer, Is.InstanceOf<LambdaExpr>());
    }

    [Test]
    public void Parse_UntypedLambda_HasNullTypeNames()
    {
        var expr = Parse("(x, y) => x + y");
        Assert.That(expr, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)expr;
        Assert.That(lambda.Parameters[0].TypeName, Is.Null);
        Assert.That(lambda.Parameters[1].TypeName, Is.Null);
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
    public void Parse_ImplicitItCallArgument_LowersToLambda()
    {
        var expr = Parse("numbers.Where(it > 2)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.InstanceOf<LambdaExpr>());
        var lambda = (LambdaExpr)call.Arguments[0];
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("it"));
    }

    [Test]
    public void Parse_DiscardIdentifier_IsNotImplicitPlaceholder()
    {
        // _ is C#'s discard identifier — it should NOT be lowered to a lambda
        var expr = Parse("numbers.Select(_ * 2)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.Not.InstanceOf<LambdaExpr>());
    }

    [Test]
    public void Parse_ExplicitItLambdaArgument_IsPreserved()
    {
        var expr = Parse("numbers.Where(it => it > 2)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)call.Arguments[0];
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("it"));
        Assert.That(lambda.Body, Is.Not.InstanceOf<LambdaExpr>());
    }

    [Test]
    public void Parse_ExplicitDiscardLambdaArgument_IsPreserved()
    {
        var expr = Parse("numbers.Select(_ => _ * 2)");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var call = (CallExpr)expr;
        Assert.That(call.Arguments, Has.Count.EqualTo(1));
        Assert.That(call.Arguments[0], Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)call.Arguments[0];
        Assert.That(lambda.Parameters, Has.Count.EqualTo(1));
        Assert.That(lambda.Parameters[0].Name.Lexeme, Is.EqualTo("_"));
        Assert.That(lambda.Body, Is.Not.InstanceOf<LambdaExpr>());
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
    public void Parse_Block_EmptyStatement_IsIgnored()
    {
        var expr = Parse("{ int x = 1; ; return x; }", LanguageMode.Standard);
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements, Has.Count.EqualTo(2));
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());
        Assert.That(block.Statements[1], Is.InstanceOf<ReturnExpr>());
    }

    [Test]
    public void Parse_Block_LocalFunctionDeclaration_DesugarsToLambdaVariable()
    {
        var expr = Parse("{ int i = 0; int Next() { i = i + 1; return i; } return Next() * 10 + Next(); }",
            LanguageMode.Standard);
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements, Has.Count.EqualTo(3));
        Assert.That(block.Statements[1], Is.InstanceOf<VariableDeclExpr>());

        var decl = (VariableDeclExpr)block.Statements[1];
        Assert.That(decl.Name.Lexeme, Is.EqualTo("Next"));
        Assert.That(decl.Initializer, Is.InstanceOf<LambdaExpr>());

        var lambda = (LambdaExpr)decl.Initializer;
        Assert.That(lambda.Parameters, Has.Count.EqualTo(0));
        Assert.That(lambda.Body, Is.InstanceOf<BlockExpr>());
    }

    [Test]
    public void Parse_Block_ConstDeclaration_MarksVariableAsConst()
    {
        var expr = Parse("{ const int x = 42; return x; }", LanguageMode.Standard);
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements, Has.Count.EqualTo(2));
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());

        var decl = (VariableDeclExpr)block.Statements[0];
        Assert.That(decl.IsConst, Is.True);
        Assert.That(decl.DeclaredType, Is.Not.Null);
        Assert.That(decl.DeclaredType!.Value.Lexeme, Is.EqualTo("int"));
        Assert.That(decl.Name.Lexeme, Is.EqualTo("x"));
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

    [Test]
    public void Parse_NotIn_LowersToCanonicalNotPlusIn()
    {
        var expr = Parse("x not in y");
        Assert.That(expr, Is.InstanceOf<UnaryExpr>());

        var unary = (UnaryExpr)expr;
        Assert.That(unary.Op.Type, Is.EqualTo(TokenType.Bang));
        Assert.That(unary.Right, Is.InstanceOf<BinaryExpr>());

        var binary = (BinaryExpr)unary.Right;
        Assert.That(binary.Op.Type, Is.EqualTo(TokenType.In));
    }

    [Test]
    public void Parse_NotLike_LowersToCanonicalNotPlusLike()
    {
        var expr = Parse("x not like y");
        Assert.That(expr, Is.InstanceOf<UnaryExpr>());

        var unary = (UnaryExpr)expr;
        Assert.That(unary.Op.Type, Is.EqualTo(TokenType.Bang));
        Assert.That(unary.Right, Is.InstanceOf<BinaryExpr>());

        var binary = (BinaryExpr)unary.Right;
        Assert.That(binary.Op.Type, Is.EqualTo(TokenType.Like));
    }

    [Test]
    public void Parse_LetIn_LowersToBlockExpr()
    {
        var expr = Parse("let x = 5 in x * x");
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements, Has.Count.EqualTo(1));
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());
        Assert.That(block.ReturnExpr, Is.Not.Null);

        var decl = (VariableDeclExpr)block.Statements[0];
        Assert.That(decl.Name.Lexeme, Is.EqualTo("x"));
    }

    [Test]
    public void Parse_IfExpression_LowersToConditionalExpr()
    {
        var expr = Parse("if (x > 0) x else -x");
        Assert.That(expr, Is.InstanceOf<ConditionalExpr>());
    }

    [Test]
    public void Parse_Comprehension_LowersToMethodChain()
    {
        var expr = Parse("[x * x for x in 1..10 if x % 2 == 0]");
        Assert.That(expr, Is.InstanceOf<CallExpr>());

        var toArrayCall = (CallExpr)expr;
        Assert.That(toArrayCall.Callee, Is.InstanceOf<MemberAccessExpr>());
        var member = (MemberAccessExpr)toArrayCall.Callee;
        Assert.That(member.Name.Lexeme, Is.EqualTo("ToArray"));
    }

    [Test]
    public void Parse_LetInDestructuring_LowersToTempAndMembers()
    {
        var expr = Parse("let { Name, Age } = person in Name + \":\" + Age");
        Assert.That(expr, Is.InstanceOf<BlockExpr>());

        var block = (BlockExpr)expr;
        Assert.That(block.Statements, Has.Count.EqualTo(3));
        Assert.That(block.Statements[0], Is.InstanceOf<VariableDeclExpr>());
        Assert.That(block.Statements[1], Is.InstanceOf<VariableDeclExpr>());
        Assert.That(block.Statements[2], Is.InstanceOf<VariableDeclExpr>());
        Assert.That(block.ReturnExpr, Is.Not.Null);
    }
}
